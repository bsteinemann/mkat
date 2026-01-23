using Mkat.Domain.Entities;

namespace Mkat.Application.Interfaces;

public interface IPeerRepository
{
    Task<Peer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Peer>> GetAllAsync(CancellationToken ct = default);
    Task<Peer?> GetByServiceIdAsync(Guid serviceId, CancellationToken ct = default);
    Task AddAsync(Peer peer, CancellationToken ct = default);
    Task DeleteAsync(Peer peer, CancellationToken ct = default);
}
