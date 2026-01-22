using Mkat.Domain.Entities;

namespace Mkat.Application.Interfaces;

public interface IServiceRepository
{
    Task<Service?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Service>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default);
    Task<int> GetCountAsync(CancellationToken ct = default);
    Task AddAsync(Service service, CancellationToken ct = default);
    Task UpdateAsync(Service service, CancellationToken ct = default);
    Task DeleteAsync(Service service, CancellationToken ct = default);
    Task<IReadOnlyList<Service>> GetPausedServicesAsync(CancellationToken ct = default);
}
