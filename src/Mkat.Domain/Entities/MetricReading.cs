namespace Mkat.Domain.Entities;

public class MetricReading
{
    public Guid Id { get; set; }
    public Guid MonitorId { get; set; }
    public double Value { get; set; }
    public DateTime RecordedAt { get; set; }
    public bool IsOutOfRange { get; set; }

    public Monitor Monitor { get; set; } = null!;
}
