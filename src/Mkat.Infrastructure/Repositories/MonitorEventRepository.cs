using Microsoft.EntityFrameworkCore;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Data;

namespace Mkat.Infrastructure.Repositories;

public class MonitorEventRepository : IMonitorEventRepository
{
    private readonly MkatDbContext _context;

    public MonitorEventRepository(MkatDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(MonitorEvent monitorEvent, CancellationToken ct = default)
    {
        await _context.MonitorEvents.AddAsync(monitorEvent, ct);
    }

    public async Task<IReadOnlyList<MonitorEvent>> GetByMonitorIdAsync(Guid monitorId, DateTime? from, DateTime? until, EventType? eventType, int limit = 100, CancellationToken ct = default)
    {
        var query = _context.MonitorEvents.Where(e => e.MonitorId == monitorId);

        if (from.HasValue)
            query = query.Where(e => e.CreatedAt >= from.Value);

        if (until.HasValue)
            query = query.Where(e => e.CreatedAt <= until.Value);

        if (eventType.HasValue)
            query = query.Where(e => e.EventType == eventType.Value);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<MonitorEvent>> GetByServiceIdAsync(Guid serviceId, DateTime? from, DateTime? until, EventType? eventType, int limit = 100, CancellationToken ct = default)
    {
        var query = _context.MonitorEvents.Where(e => e.ServiceId == serviceId);

        if (from.HasValue)
            query = query.Where(e => e.CreatedAt >= from.Value);

        if (until.HasValue)
            query = query.Where(e => e.CreatedAt <= until.Value);

        if (eventType.HasValue)
            query = query.Where(e => e.EventType == eventType.Value);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<MonitorEvent>> GetByMonitorIdInWindowAsync(Guid monitorId, DateTime windowStart, DateTime windowEnd, CancellationToken ct = default)
    {
        return await _context.MonitorEvents
            .Where(e => e.MonitorId == monitorId && e.CreatedAt >= windowStart && e.CreatedAt <= windowEnd)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task DeleteOlderThanAsync(DateTime threshold, CancellationToken ct = default)
    {
        var oldEvents = await _context.MonitorEvents
            .Where(e => e.CreatedAt < threshold)
            .ToListAsync(ct);

        _context.MonitorEvents.RemoveRange(oldEvents);
    }
}
