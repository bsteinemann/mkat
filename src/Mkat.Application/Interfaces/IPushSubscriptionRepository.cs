using Mkat.Domain.Entities;

namespace Mkat.Application.Interfaces;

public interface IPushSubscriptionRepository
{
    Task<IReadOnlyList<PushSubscription>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(PushSubscription subscription, CancellationToken ct = default);
    Task RemoveByEndpointAsync(string endpoint, CancellationToken ct = default);
}
