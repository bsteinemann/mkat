using Mkat.Domain.Enums;

namespace Mkat.Domain.Entities;

public class MonitorEvent
{
    public Guid Id { get; set; }
    public Guid MonitorId { get; set; }
    public Guid ServiceId { get; set; }
    public EventType EventType { get; set; }
    public bool Success { get; set; }
    public double? Value { get; set; }
    public bool IsOutOfRange { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Monitor Monitor { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
