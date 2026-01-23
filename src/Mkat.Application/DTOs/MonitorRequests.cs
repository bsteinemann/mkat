using Mkat.Domain.Enums;

namespace Mkat.Application.DTOs;

public record AddMonitorRequest
{
    public MonitorType Type { get; init; }
    public int IntervalSeconds { get; init; }
    public int? GracePeriodSeconds { get; init; }

    // Metric monitor fields
    public double? MinValue { get; init; }
    public double? MaxValue { get; init; }
    public ThresholdStrategy ThresholdStrategy { get; init; }
    public int? ThresholdCount { get; init; }
    public int? WindowSeconds { get; init; }
    public int? WindowSampleCount { get; init; }
    public int RetentionDays { get; init; } = 7;
}

public record UpdateMonitorRequest
{
    public MonitorType? Type { get; init; }
    public int IntervalSeconds { get; init; }
    public int? GracePeriodSeconds { get; init; }

    // Metric monitor fields
    public double? MinValue { get; init; }
    public double? MaxValue { get; init; }
    public ThresholdStrategy? ThresholdStrategy { get; init; }
    public int? ThresholdCount { get; init; }
    public int? WindowSeconds { get; init; }
    public int? WindowSampleCount { get; init; }
    public int? RetentionDays { get; init; }
}
