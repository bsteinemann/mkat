using Mkat.Domain.Enums;

namespace Mkat.Domain.Entities;

public class Alert
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public AlertType Type { get; set; }
    public Severity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public string? Metadata { get; set; }

    public Service Service { get; set; } = null!;
}
