using Microsoft.EntityFrameworkCore;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Infrastructure.Data;

namespace Mkat.Infrastructure.Repositories;

public class MuteWindowRepository : IMuteWindowRepository
{
    private readonly MkatDbContext _context;

    public MuteWindowRepository(MkatDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsServiceMutedAsync(Guid serviceId, DateTime at, CancellationToken ct = default)
    {
        return await _context.MuteWindows
            .AnyAsync(m => m.ServiceId == serviceId && m.StartsAt <= at && m.EndsAt > at, ct);
    }

    public async Task<MuteWindow> AddAsync(MuteWindow mute, CancellationToken ct = default)
    {
        mute.CreatedAt = DateTime.UtcNow;
        await _context.MuteWindows.AddAsync(mute, ct);
        return mute;
    }

    public async Task<IReadOnlyList<MuteWindow>> GetActiveForServiceAsync(Guid serviceId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _context.MuteWindows
            .Where(m => m.ServiceId == serviceId && m.StartsAt <= now && m.EndsAt > now)
            .OrderBy(m => m.EndsAt)
            .ToListAsync(ct);
    }
}
