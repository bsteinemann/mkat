using Mkat.Domain.Entities;
using Monitor = Mkat.Domain.Entities.Monitor;

namespace Mkat.Application.Interfaces;

public interface IMonitorRepository
{
    Task<Monitor?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Monitor?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<IReadOnlyList<Monitor>> GetByServiceIdAsync(Guid serviceId, CancellationToken ct = default);
    Task<IReadOnlyList<Monitor>> GetHeartbeatMonitorsDueAsync(DateTime threshold, CancellationToken ct = default);
    Task<IReadOnlyList<Monitor>> GetAllMetricMonitorsAsync(CancellationToken ct = default);
    Task AddAsync(Monitor monitor, CancellationToken ct = default);
    Task UpdateAsync(Monitor monitor, CancellationToken ct = default);
    Task DeleteAsync(Monitor monitor, CancellationToken ct = default);
}
