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

    // Health check monitor fields
    public string? HealthCheckUrl { get; init; }
    public string? HttpMethod { get; init; }
    public string? ExpectedStatusCodes { get; init; }
    public int? TimeoutSeconds { get; init; }
    public string? BodyMatchRegex { get; init; }
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

    // Health check monitor fields
    public string? HealthCheckUrl { get; init; }
    public string? HttpMethod { get; init; }
    public string? ExpectedStatusCodes { get; init; }
    public int? TimeoutSeconds { get; init; }
    public string? BodyMatchRegex { get; init; }
}
