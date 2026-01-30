using Mkat.Domain.Entities;
using Mkat.Domain.Enums;

namespace Mkat.Application.Interfaces;

public interface IMonitorRollupRepository
{
    Task AddAsync(MonitorRollup rollup, CancellationToken ct = default);
    Task UpsertAsync(MonitorRollup rollup, CancellationToken ct = default);
    Task<IReadOnlyList<MonitorRollup>> GetByMonitorIdAsync(Guid monitorId, Granularity? granularity, DateTime? from, DateTime? until, CancellationToken ct = default);
    Task<MonitorRollup?> GetByMonitorIdAndPeriodAsync(Guid monitorId, Granularity granularity, DateTime periodStart, CancellationToken ct = default);
    Task<IReadOnlyList<MonitorRollup>> GetByServiceIdAsync(Guid serviceId, Granularity granularity, DateTime? from, DateTime? until, CancellationToken ct = default);
    Task DeleteOlderThanAsync(Granularity granularity, DateTime threshold, CancellationToken ct = default);
}
