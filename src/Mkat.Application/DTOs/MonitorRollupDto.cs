namespace Mkat.Application.DTOs;

public record MonitorRollupDto
{
    public Guid Id { get; init; }
    public Guid MonitorId { get; init; }
    public Guid ServiceId { get; init; }
    public string Granularity { get; init; } = string.Empty;
    public DateTime PeriodStart { get; init; }
    public int Count { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public double? Min { get; init; }
    public double? Max { get; init; }
    public double? Mean { get; init; }
    public double? Median { get; init; }
    public double? P80 { get; init; }
    public double? P90 { get; init; }
    public double? P95 { get; init; }
    public double? StdDev { get; init; }
    public double? UptimePercent { get; init; }
}
