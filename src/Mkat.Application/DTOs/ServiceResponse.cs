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
}
