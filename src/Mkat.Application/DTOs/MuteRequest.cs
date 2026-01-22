namespace Mkat.Application.DTOs;

public record MuteRequest
{
    public int DurationMinutes { get; init; }
    public string? Reason { get; init; }
}
