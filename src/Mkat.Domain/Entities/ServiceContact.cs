namespace Mkat.Domain.Entities;

public class ServiceContact
{
    public Guid ServiceId { get; set; }
    public Guid ContactId { get; set; }

    public Service? Service { get; set; }
    public Contact? Contact { get; set; }
}
