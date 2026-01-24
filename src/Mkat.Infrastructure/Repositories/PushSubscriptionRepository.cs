using Microsoft.EntityFrameworkCore;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Infrastructure.Data;

namespace Mkat.Infrastructure.Repositories;

public class PushSubscriptionRepository : IPushSubscriptionRepository
{
    private readonly MkatDbContext _db;

    public PushSubscriptionRepository(MkatDbContext db) => _db = db;

    public async Task<IReadOnlyList<PushSubscription>> GetAllAsync(CancellationToken ct = default)
        => await _db.PushSubscriptions.ToListAsync(ct);

    public async Task AddAsync(PushSubscription subscription, CancellationToken ct = default)
        => await _db.PushSubscriptions.AddAsync(subscription, ct);

    public async Task RemoveByEndpointAsync(string endpoint, CancellationToken ct = default)
    {
        var sub = await _db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint, ct);
        if (sub != null) _db.PushSubscriptions.Remove(sub);
    }
}
