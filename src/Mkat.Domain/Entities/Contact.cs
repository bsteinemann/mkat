namespace Mkat.Domain.Entities;

public class Contact
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ContactChannel> Channels { get; set; } = new List<ContactChannel>();
    public ICollection<ServiceContact> ServiceContacts { get; set; } = new List<ServiceContact>();
}
