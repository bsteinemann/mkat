namespace Mkat.Application.DTOs;

public record PauseRequest
{
    public DateTime? Until { get; init; }
    public bool AutoResume { get; init; }
}
