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
}
