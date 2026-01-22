namespace Mkat.Domain.Entities;

public class NotificationChannel
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string ConfigJson { get; set; } = "{}";
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
