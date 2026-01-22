using Mkat.Domain.Enums;

namespace Mkat.Application.DTOs;

public record AlertResponse
{
    public Guid Id { get; init; }
    public Guid ServiceId { get; init; }
    public AlertType Type { get; init; }
    public Severity Severity { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? AcknowledgedAt { get; init; }
    public DateTime? DispatchedAt { get; init; }
}
