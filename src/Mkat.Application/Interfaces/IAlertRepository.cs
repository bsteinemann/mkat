using Mkat.Domain.Entities;

namespace Mkat.Application.Interfaces;

public interface IAlertRepository
{
    Task<Alert?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Alert>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default);
    Task<IReadOnlyList<Alert>> GetByServiceIdAsync(Guid serviceId, CancellationToken ct = default);
    Task<IReadOnlyList<Alert>> GetPendingDispatchAsync(CancellationToken ct = default);
    Task<int> GetCountAsync(CancellationToken ct = default);
    Task AddAsync(Alert alert, CancellationToken ct = default);
    Task UpdateAsync(Alert alert, CancellationToken ct = default);
}
