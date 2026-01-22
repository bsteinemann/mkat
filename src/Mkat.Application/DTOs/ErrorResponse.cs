namespace Mkat.Application.DTOs;

public record ErrorResponse
{
    public string Error { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public Dictionary<string, string[]>? Details { get; init; }
}
