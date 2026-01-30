using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;

namespace Mkat.Application.Services;

public class RollupCalculator : IRollupCalculator
{
    public MonitorRollup Compute(IReadOnlyList<MonitorEvent> events, Guid monitorId, Guid serviceId, Granularity granularity, DateTime periodStart)
    {
        var rollup = new MonitorRollup
        {
            Id = Guid.NewGuid(),
            MonitorId = monitorId,
            ServiceId = serviceId,
            Granularity = granularity,
            PeriodStart = periodStart,
            Count = events.Count,
            SuccessCount = events.Count(e => e.Success),
            FailureCount = events.Count(e => !e.Success)
        };

        if (events.Count > 0)
        {
            rollup.UptimePercent = Math.Round((double)rollup.SuccessCount / rollup.Count * 100, 2);
        }

        var values = events.Where(e => e.Value.HasValue).Select(e => e.Value!.Value).ToList();

        if (values.Count > 0)
        {
            values.Sort();
            rollup.Min = values[0];
            rollup.Max = values[^1];
            rollup.Mean = values.Average();
            rollup.Median = ComputeMedian(values);
            rollup.P80 = ComputePercentile(values, 0.80);
            rollup.P90 = ComputePercentile(values, 0.90);
            rollup.P95 = ComputePercentile(values, 0.95);
            rollup.StdDev = ComputeStdDev(values, rollup.Mean.Value);
        }

        return rollup;
    }

    private static double ComputeMedian(List<double> sorted)
    {
        int count = sorted.Count;
        if (count % 2 == 0)
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
        return sorted[count / 2];
    }

    private static double ComputePercentile(List<double> sorted, double percentile)
    {
        double index = percentile * (sorted.Count - 1);
        int lower = (int)Math.Floor(index);
        int upper = (int)Math.Ceiling(index);

        if (lower == upper)
            return sorted[lower];

        double fraction = index - lower;
        return sorted[lower] + fraction * (sorted[upper] - sorted[lower]);
    }

    private static double ComputeStdDev(List<double> values, double mean)
    {
        double sumSquaredDiffs = values.Sum(v => (v - mean) * (v - mean));
        return Math.Sqrt(sumSquaredDiffs / values.Count);
    }
}
