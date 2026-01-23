using Mkat.Domain.Enums;

namespace Mkat.Domain.Entities;

public class ContactChannel
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public ChannelType Type { get; set; }
    public string Configuration { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Contact? Contact { get; set; }
}
