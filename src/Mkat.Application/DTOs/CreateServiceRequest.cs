using Mkat.Domain.Enums;

namespace Mkat.Application.DTOs;

public record CreateServiceRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Severity Severity { get; init; } = Severity.Medium;
    public List<CreateMonitorRequest> Monitors { get; init; } = new();
}

public record CreateMonitorRequest
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
