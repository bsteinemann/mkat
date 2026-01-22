using Mkat.Domain.Enums;

namespace Mkat.Application.DTOs;

public record UpdateServiceRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Severity Severity { get; init; }
}
