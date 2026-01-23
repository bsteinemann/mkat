using Moq;
using Mkat.Application.Interfaces;
using Mkat.Application.Services;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Xunit;
using Monitor = Mkat.Domain.Entities.Monitor;

namespace Mkat.Application.Tests.Services;

public class MetricEvaluatorTests
{
    private readonly Mock<IMetricReadingRepository> _readingRepo;
    private readonly MetricEvaluator _evaluator;

    public MetricEvaluatorTests()
    {
        _readingRepo = new Mock<IMetricReadingRepository>();
        _evaluator = new MetricEvaluator(_readingRepo.Object);
    }

    private static Monitor CreateMonitor(
        ThresholdStrategy strategy = ThresholdStrategy.Immediate,
        double? minValue = null,
        double? maxValue = null,
        int? thresholdCount = null,
        int? windowSeconds = null,
        int? windowSampleCount = null)
    {
        return new Monitor
        {
            Id = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            Type = MonitorType.Metric,
            MinValue = minValue,
            MaxValue = maxValue,
            ThresholdStrategy = strategy,
            ThresholdCount = thresholdCount,
            WindowSeconds = windowSeconds,
            WindowSampleCount = windowSampleCount
        };
    }

    // --- IsOutOfRange tests ---

    [Fact]
    public void IsOutOfRange_ValueBelowMin_ReturnsTrue()
    {
        var monitor = CreateMonitor(minValue: 10.0);
        Assert.True(MetricEvaluator.IsOutOfRange(5.0, monitor));
    }

    [Fact]
    public void IsOutOfRange_ValueAboveMax_ReturnsTrue()
    {
        var monitor = CreateMonitor(maxValue: 100.0);
        Assert.True(MetricEvaluator.IsOutOfRange(150.0, monitor));
    }

    [Fact]
    public void IsOutOfRange_ValueAtMin_ReturnsFalse()
    {
        var monitor = CreateMonitor(minValue: 10.0);
        Assert.False(MetricEvaluator.IsOutOfRange(10.0, monitor));
    }

    [Fact]
    public void IsOutOfRange_ValueAtMax_ReturnsFalse()
    {
        var monitor = CreateMonitor(maxValue: 100.0);
        Assert.False(MetricEvaluator.IsOutOfRange(100.0, monitor));
    }

    [Fact]
    public void IsOutOfRange_ValueWithinRange_ReturnsFalse()
    {
        var monitor = CreateMonitor(minValue: 10.0, maxValue: 100.0);
        Assert.False(MetricEvaluator.IsOutOfRange(50.0, monitor));
    }

    [Fact]
    public void IsOutOfRange_NoBoundsSet_ReturnsFalse()
    {
        var monitor = CreateMonitor();
        Assert.False(MetricEvaluator.IsOutOfRange(50.0, monitor));
    }

    [Fact]
    public void IsOutOfRange_OnlyMinSet_ValueAbove_ReturnsFalse()
    {
        var monitor = CreateMonitor(minValue: 0.0);
        Assert.False(MetricEvaluator.IsOutOfRange(999.0, monitor));
    }

    [Fact]
    public void IsOutOfRange_OnlyMaxSet_ValueBelow_ReturnsFalse()
    {
        var monitor = CreateMonitor(maxValue: 100.0);
        Assert.False(MetricEvaluator.IsOutOfRange(-999.0, monitor));
    }

    // --- Immediate strategy ---

    [Fact]
    public async Task EvaluateAsync_Immediate_OutOfRange_ReturnsTrue()
    {
        var monitor = CreateMonitor(strategy: ThresholdStrategy.Immediate, maxValue: 90.0);
        var result = await _evaluator.EvaluateAsync(monitor, 95.0);
        Assert.True(result);
    }

    [Fact]
    public async Task EvaluateAsync_Immediate_InRange_ReturnsFalse()
    {
        var monitor = CreateMonitor(strategy: ThresholdStrategy.Immediate, maxValue: 90.0);
        var result = await _evaluator.EvaluateAsync(monitor, 80.0);
        Assert.False(result);
    }

    // --- ConsecutiveCount strategy ---

    [Fact]
    public async Task EvaluateAsync_ConsecutiveCount_AllOutOfRange_ReturnsTrue()
    {
        var monitor = CreateMonitor(
            strategy: ThresholdStrategy.ConsecutiveCount,
            maxValue: 90.0,
            thresholdCount: 3);

        var readings = new List<MetricReading>
        {
            new() { Value = 95.0, IsOutOfRange = true, RecordedAt = DateTime.UtcNow.AddSeconds(-2) },
            new() { Value = 92.0, IsOutOfRange = true, RecordedAt = DateTime.UtcNow.AddSeconds(-1) }
        };

        _readingRepo.Setup(r => r.GetLastNByMonitorIdAsync(monitor.Id, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings);

        // Current value (95) is out of range, plus 2 previous out-of-range = 3 consecutive
        var result = await _evaluator.EvaluateAsync(monitor, 95.0);
        Assert.True(result);
    }

    [Fact]
    public async Task EvaluateAsync_ConsecutiveCount_NotEnoughConsecutive_ReturnsFalse()
    {
        var monitor = CreateMonitor(
            strategy: ThresholdStrategy.ConsecutiveCount,
            maxValue: 90.0,
            thresholdCount: 3);

        var readings = new List<MetricReading>
        {
            new() { Value = 95.0, IsOutOfRange = true, RecordedAt = DateTime.UtcNow.AddSeconds(-2) },
            new() { Value = 80.0, IsOutOfRange = false, RecordedAt = DateTime.UtcNow.AddSeconds(-1) }
        };

        _readingRepo.Setup(r => r.GetLastNByMonitorIdAsync(monitor.Id, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings);

        var result = await _evaluator.EvaluateAsync(monitor, 95.0);
        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateAsync_ConsecutiveCount_CurrentInRange_ReturnsFalse()
    {
        var monitor = CreateMonitor(
            strategy: ThresholdStrategy.ConsecutiveCount,
            maxValue: 90.0,
            thresholdCount: 3);

        var result = await _evaluator.EvaluateAsync(monitor, 80.0);
        Assert.False(result);
    }

    // --- TimeDurationAverage strategy ---

    [Fact]
    public async Task EvaluateAsync_TimeDurationAverage_AverageOutOfRange_ReturnsTrue()
    {
        var monitor = CreateMonitor(
            strategy: ThresholdStrategy.TimeDurationAverage,
            maxValue: 90.0,
            windowSeconds: 60);

        var readings = new List<MetricReading>
        {
            new() { Value = 95.0, RecordedAt = DateTime.UtcNow.AddSeconds(-30) },
            new() { Value = 92.0, RecordedAt = DateTime.UtcNow.AddSeconds(-20) }
        };

        _readingRepo.Setup(r => r.GetByMonitorIdInWindowAsync(
                monitor.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings);

        // Average of (95, 92, 91) = 92.67 > 90
        var result = await _evaluator.EvaluateAsync(monitor, 91.0);
        Assert.True(result);
    }

    [Fact]
    public async Task EvaluateAsync_TimeDurationAverage_AverageInRange_ReturnsFalse()
    {
        var monitor = CreateMonitor(
            strategy: ThresholdStrategy.TimeDurationAverage,
            maxValue: 90.0,
            windowSeconds: 60);

        var readings = new List<MetricReading>
        {
            new() { Value = 80.0, RecordedAt = DateTime.UtcNow.AddSeconds(-30) },
            new() { Value = 85.0, RecordedAt = DateTime.UtcNow.AddSeconds(-20) }
        };

        _readingRepo.Setup(r => r.GetByMonitorIdInWindowAsync(
                monitor.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings);

        // Average of (80, 85, 88) = 84.33 < 90
        var result = await _evaluator.EvaluateAsync(monitor, 88.0);
        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateAsync_TimeDurationAverage_NoHistory_UsesCurrentOnly()
    {
        var monitor = CreateMonitor(
            strategy: ThresholdStrategy.TimeDurationAverage,
            maxValue: 90.0,
            windowSeconds: 60);

        _readingRepo.Setup(r => r.GetByMonitorIdInWindowAsync(
                monitor.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MetricReading>());

        // Only current value 95 > 90
        var result = await _evaluator.EvaluateAsync(monitor, 95.0);
        Assert.True(result);
    }

    // --- SampleCountAverage strategy ---

    [Fact]
    public async Task EvaluateAsync_SampleCountAverage_AverageOutOfRange_ReturnsTrue()
    {
        var monitor = CreateMonitor(
            strategy: ThresholdStrategy.SampleCountAverage,
            maxValue: 90.0,
            windowSampleCount: 3);

        var readings = new List<MetricReading>
        {
            new() { Value = 95.0, RecordedAt = DateTime.UtcNow.AddSeconds(-2) },
            new() { Value = 92.0, RecordedAt = DateTime.UtcNow.AddSeconds(-1) }
        };

        _readingRepo.Setup(r => r.GetLastNByMonitorIdAsync(monitor.Id, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings);

        // Average of (95, 92, 91) = 92.67 > 90
        var result = await _evaluator.EvaluateAsync(monitor, 91.0);
        Assert.True(result);
    }

    [Fact]
    public async Task EvaluateAsync_SampleCountAverage_AverageInRange_ReturnsFalse()
    {
        var monitor = CreateMonitor(
            strategy: ThresholdStrategy.SampleCountAverage,
            maxValue: 90.0,
            windowSampleCount: 3);

        var readings = new List<MetricReading>
        {
            new() { Value = 80.0, RecordedAt = DateTime.UtcNow.AddSeconds(-2) },
            new() { Value = 85.0, RecordedAt = DateTime.UtcNow.AddSeconds(-1) }
        };

        _readingRepo.Setup(r => r.GetLastNByMonitorIdAsync(monitor.Id, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings);

        // Average of (80, 85, 70) = 78.33 < 90
        var result = await _evaluator.EvaluateAsync(monitor, 70.0);
        Assert.False(result);
    }

    // --- MinValue tests for average strategies ---

    [Fact]
    public async Task EvaluateAsync_TimeDurationAverage_AverageBelowMin_ReturnsTrue()
    {
        var monitor = CreateMonitor(
            strategy: ThresholdStrategy.TimeDurationAverage,
            minValue: 50.0,
            windowSeconds: 60);

        var readings = new List<MetricReading>
        {
            new() { Value = 30.0, RecordedAt = DateTime.UtcNow.AddSeconds(-30) },
            new() { Value = 40.0, RecordedAt = DateTime.UtcNow.AddSeconds(-20) }
        };

        _readingRepo.Setup(r => r.GetByMonitorIdInWindowAsync(
                monitor.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings);

        // Average of (30, 40, 35) = 35 < 50
        var result = await _evaluator.EvaluateAsync(monitor, 35.0);
        Assert.True(result);
    }
}
