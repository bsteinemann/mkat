using Mkat.Domain.Enums;

namespace Mkat.Domain.Entities;

public class Service
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ServiceState State { get; set; } = ServiceState.Unknown;
    public ServiceState? PreviousState { get; set; }
    public Severity Severity { get; set; } = Severity.Medium;
    public DateTime? PausedUntil { get; set; }
    public bool AutoResume { get; set; }
    public bool IsSuppressed { get; set; }
    public string? SuppressionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Monitor> Monitors { get; set; } = new List<Monitor>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    public ICollection<MuteWindow> MuteWindows { get; set; } = new List<MuteWindow>();
    public ICollection<ServiceContact> ServiceContacts { get; set; } = new List<ServiceContact>();
    public ICollection<ServiceDependency> DependsOn { get; set; } = new List<ServiceDependency>();
    public ICollection<ServiceDependency> DependedOnBy { get; set; } = new List<ServiceDependency>();
}
