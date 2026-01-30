namespace Mkat.Application.DTOs;

public record ServiceUptimeDto
{
    public Guid ServiceId { get; init; }
    public double UptimePercent { get; init; }
    public int TotalEvents { get; init; }
    public int SuccessEvents { get; init; }
    public int FailureEvents { get; init; }
    public DateTime From { get; init; }
    public DateTime To { get; init; }
}
