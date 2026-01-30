namespace Mkat.Domain.Entities;

public class ServiceDependency
{
    public Guid Id { get; set; }
    public Guid DependentServiceId { get; set; }
    public Guid DependencyServiceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Service DependentService { get; set; } = null!;
    public Service DependencyService { get; set; } = null!;
}
