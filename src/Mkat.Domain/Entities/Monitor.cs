using Mkat.Domain.Enums;

namespace Mkat.Domain.Entities;

public class Monitor
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public MonitorType Type { get; set; }
    public string Token { get; set; } = string.Empty;
    public int IntervalSeconds { get; set; }
    public int GracePeriodSeconds { get; set; }
    public string? ConfigJson { get; set; }
    public DateTime? LastCheckIn { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Metric monitor fields
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public ThresholdStrategy ThresholdStrategy { get; set; } = ThresholdStrategy.Immediate;
    public int? ThresholdCount { get; set; }
    public int? WindowSeconds { get; set; }
    public int? WindowSampleCount { get; set; }
    public int RetentionDays { get; set; } = 7;
    public double? LastMetricValue { get; set; }
    public DateTime? LastMetricAt { get; set; }

    // Health check monitor fields
    public string? HealthCheckUrl { get; set; }
    public string? HttpMethod { get; set; }
    public string? ExpectedStatusCodes { get; set; }
    public int? TimeoutSeconds { get; set; }
    public string? BodyMatchRegex { get; set; }

    public Service Service { get; set; } = null!;
}
