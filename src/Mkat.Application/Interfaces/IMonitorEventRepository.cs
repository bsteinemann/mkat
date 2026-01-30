using Mkat.Domain.Entities;
using Mkat.Domain.Enums;

namespace Mkat.Application.Interfaces;

public interface IMonitorEventRepository
{
    Task AddAsync(MonitorEvent monitorEvent, CancellationToken ct = default);
    Task<IReadOnlyList<MonitorEvent>> GetByMonitorIdAsync(Guid monitorId, DateTime? from, DateTime? until, EventType? eventType, int limit = 100, CancellationToken ct = default);
    Task<IReadOnlyList<MonitorEvent>> GetByServiceIdAsync(Guid serviceId, DateTime? from, DateTime? until, EventType? eventType, int limit = 100, CancellationToken ct = default);
    Task<IReadOnlyList<MonitorEvent>> GetByMonitorIdInWindowAsync(Guid monitorId, DateTime windowStart, DateTime windowEnd, CancellationToken ct = default);
    Task DeleteOlderThanAsync(DateTime threshold, CancellationToken ct = default);
}
