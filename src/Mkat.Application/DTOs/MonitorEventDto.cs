namespace Mkat.Application.DTOs;

public record MonitorEventDto
{
    public Guid Id { get; init; }
    public Guid MonitorId { get; init; }
    public Guid ServiceId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public bool Success { get; init; }
    public double? Value { get; init; }
    public bool IsOutOfRange { get; init; }
    public string? Message { get; init; }
    public DateTime CreatedAt { get; init; }
}
