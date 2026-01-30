using Mkat.Domain.Enums;

namespace Mkat.Domain.Entities;

public class MonitorRollup
{
    public Guid Id { get; set; }
    public Guid MonitorId { get; set; }
    public Guid ServiceId { get; set; }
    public Granularity Granularity { get; set; }
    public DateTime PeriodStart { get; set; }
    public int Count { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
    public double? Mean { get; set; }
    public double? Median { get; set; }
    public double? P80 { get; set; }
    public double? P90 { get; set; }
    public double? P95 { get; set; }
    public double? StdDev { get; set; }
    public double? UptimePercent { get; set; }

    public Monitor Monitor { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
