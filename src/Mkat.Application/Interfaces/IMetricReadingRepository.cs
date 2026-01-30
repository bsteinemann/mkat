using Mkat.Domain.Entities;

namespace Mkat.Application.Interfaces;

public interface IMetricReadingRepository
{
    Task<MetricReading?> GetLatestByMonitorIdAsync(Guid monitorId, CancellationToken ct = default);
    Task<IReadOnlyList<MetricReading>> GetByMonitorIdAsync(Guid monitorId, DateTime? from, DateTime? until, int limit = 100, CancellationToken ct = default);
    Task<IReadOnlyList<MetricReading>> GetLastNByMonitorIdAsync(Guid monitorId, int count, CancellationToken ct = default);
    Task<IReadOnlyList<MetricReading>> GetByMonitorIdInWindowAsync(Guid monitorId, DateTime windowStart, CancellationToken ct = default);
    Task AddAsync(MetricReading reading, CancellationToken ct = default);
    Task DeleteOlderThanAsync(Guid monitorId, DateTime threshold, CancellationToken ct = default);
}
