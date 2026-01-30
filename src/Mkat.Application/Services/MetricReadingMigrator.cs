using Mkat.Domain.Entities;
using Mkat.Domain.Enums;

namespace Mkat.Application.Services;

public static class MetricReadingMigrator
{
    public static IReadOnlyList<MonitorEvent> Convert(IReadOnlyList<MetricReading> readings)
    {
        return readings.Select(r => new MonitorEvent
        {
            Id = Guid.NewGuid(),
            MonitorId = r.MonitorId,
            ServiceId = r.Monitor.ServiceId,
            EventType = EventType.MetricIngested,
            Success = !r.IsOutOfRange,
            Value = r.Value,
            IsOutOfRange = r.IsOutOfRange,
            CreatedAt = r.RecordedAt
        }).ToList();
    }
}
