using Mkat.Domain.Enums;

namespace Mkat.Application.DTOs;

public record AddMonitorRequest
{
    public MonitorType Type { get; init; }
    public int IntervalSeconds { get; init; }
    public int? GracePeriodSeconds { get; init; }
}

public record UpdateMonitorRequest
{
    public int IntervalSeconds { get; init; }
    public int? GracePeriodSeconds { get; init; }
}
