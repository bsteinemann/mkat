namespace Mkat.Domain.Entities;

public class Peer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string HeartbeatToken { get; set; } = string.Empty;
    public string WebhookToken { get; set; } = string.Empty;
    public Guid ServiceId { get; set; }
    public DateTime PairedAt { get; set; } = DateTime.UtcNow;
    public int HeartbeatIntervalSeconds { get; set; } = 30;

    public Service? Service { get; set; }
}
