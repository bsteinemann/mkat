using Microsoft.EntityFrameworkCore;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Infrastructure.Data;

namespace Mkat.Infrastructure.Repositories;

public class PeerRepository : IPeerRepository
{
    private readonly MkatDbContext _context;

    public PeerRepository(MkatDbContext context)
    {
        _context = context;
    }

    public async Task<Peer?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Peers
            .Include(p => p.Service)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<IReadOnlyList<Peer>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Peers
            .Include(p => p.Service)
            .OrderBy(p => p.PairedAt)
            .ToListAsync(ct);
    }

    public async Task<Peer?> GetByServiceIdAsync(Guid serviceId, CancellationToken ct = default)
    {
        return await _context.Peers
            .FirstOrDefaultAsync(p => p.ServiceId == serviceId, ct);
    }

    public async Task AddAsync(Peer peer, CancellationToken ct = default)
    {
        await _context.Peers.AddAsync(peer, ct);
    }

    public async Task DeleteAsync(Peer peer, CancellationToken ct = default)
    {
        _context.Peers.Remove(peer);
        await Task.CompletedTask;
    }
}
