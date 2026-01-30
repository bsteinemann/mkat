using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Monitor = Mkat.Domain.Entities.Monitor;

namespace Mkat.Application.Services;

public interface IMetricEvaluator
{
    Task<bool> EvaluateAsync(Monitor monitor, double currentValue, CancellationToken ct = default);
}

public class MetricEvaluator : IMetricEvaluator
{
    private readonly IMonitorEventRepository _eventRepo;

    public MetricEvaluator(IMonitorEventRepository eventRepo)
    {
        _eventRepo = eventRepo;
    }

    public static bool IsOutOfRange(double value, Monitor monitor)
    {
        if (monitor.MinValue.HasValue && value < monitor.MinValue.Value)
            return true;
        if (monitor.MaxValue.HasValue && value > monitor.MaxValue.Value)
            return true;
        return false;
    }

    public async Task<bool> EvaluateAsync(Monitor monitor, double currentValue, CancellationToken ct = default)
    {
        return monitor.ThresholdStrategy switch
        {
            ThresholdStrategy.Immediate => EvaluateImmediate(monitor, currentValue),
            ThresholdStrategy.ConsecutiveCount => await EvaluateConsecutiveCountAsync(monitor, currentValue, ct),
            ThresholdStrategy.TimeDurationAverage => await EvaluateTimeDurationAverageAsync(monitor, currentValue, ct),
            ThresholdStrategy.SampleCountAverage => await EvaluateSampleCountAverageAsync(monitor, currentValue, ct),
            _ => false
        };
    }

    private static bool EvaluateImmediate(Monitor monitor, double currentValue)
    {
        return IsOutOfRange(currentValue, monitor);
    }

    private async Task<bool> EvaluateConsecutiveCountAsync(Monitor monitor, double currentValue, CancellationToken ct)
    {
        if (!IsOutOfRange(currentValue, monitor))
            return false;

        var count = monitor.ThresholdCount ?? 1;
        if (count <= 1)
            return true;

        // Need count-1 previous events that are also out of range
        var previousEvents = await _eventRepo.GetLastNByMonitorIdAsync(monitor.Id, count - 1, ct);

        if (previousEvents.Count < count - 1)
            return false;

        return previousEvents.All(e => e.IsOutOfRange);
    }

    private async Task<bool> EvaluateTimeDurationAverageAsync(Monitor monitor, double currentValue, CancellationToken ct)
    {
        var windowSeconds = monitor.WindowSeconds ?? 60;
        var windowStart = DateTime.UtcNow.AddSeconds(-windowSeconds);

        var events = await _eventRepo.GetByMonitorIdInWindowAsync(monitor.Id, windowStart, DateTime.UtcNow, ct);

        var allValues = events.Where(e => e.Value.HasValue).Select(e => e.Value!.Value).Append(currentValue).ToList();
        var average = allValues.Average();

        return IsOutOfRange(average, monitor);
    }

    private async Task<bool> EvaluateSampleCountAverageAsync(Monitor monitor, double currentValue, CancellationToken ct)
    {
        var sampleCount = monitor.WindowSampleCount ?? 1;

        // Get sampleCount-1 previous events (current value is the Nth sample)
        var previousEvents = await _eventRepo.GetLastNByMonitorIdAsync(monitor.Id, sampleCount - 1, ct);

        var allValues = previousEvents.Where(e => e.Value.HasValue).Select(e => e.Value!.Value).Append(currentValue).ToList();
        var average = allValues.Average();

        return IsOutOfRange(average, monitor);
    }
}
