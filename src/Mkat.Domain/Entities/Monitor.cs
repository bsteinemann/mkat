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

    public Service Service { get; set; } = null!;
}
