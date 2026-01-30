using Mkat.Domain.Enums;

namespace Mkat.Application.DTOs;

public record ServiceResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public ServiceState State { get; init; }
    public Severity Severity { get; init; }
    public DateTime? PausedUntil { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<MonitorResponse> Monitors { get; init; } = new();
}

public record MonitorResponse
{
    public Guid Id { get; init; }
    public MonitorType Type { get; init; }
    public string Token { get; init; } = string.Empty;
    public int IntervalSeconds { get; init; }
    public int GracePeriodSeconds { get; init; }
    public DateTime? LastCheckIn { get; init; }
    public string WebhookFailUrl { get; init; } = string.Empty;
    public string WebhookRecoverUrl { get; init; } = string.Empty;
    public string HeartbeatUrl { get; init; } = string.Empty;
    public string MetricUrl { get; init; } = string.Empty;

    // Metric monitor fields
    public double? MinValue { get; init; }
    public double? MaxValue { get; init; }
    public ThresholdStrategy? ThresholdStrategy { get; init; }
    public int? ThresholdCount { get; init; }
    public int? WindowSeconds { get; init; }
    public int? WindowSampleCount { get; init; }
    public int? RetentionDays { get; init; }
    public double? LastMetricValue { get; init; }
    public DateTime? LastMetricAt { get; init; }

    // Health check monitor fields
    public string? HealthCheckUrl { get; init; }
    public string? HttpMethod { get; init; }
    public string? ExpectedStatusCodes { get; init; }
    public int? TimeoutSeconds { get; init; }
    public string? BodyMatchRegex { get; init; }
}
