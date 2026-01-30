using Microsoft.EntityFrameworkCore;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Data;

namespace Mkat.Infrastructure.Repositories;

public class MonitorRollupRepository : IMonitorRollupRepository
{
    private readonly MkatDbContext _context;

    public MonitorRollupRepository(MkatDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(MonitorRollup rollup, CancellationToken ct = default)
    {
        await _context.MonitorRollups.AddAsync(rollup, ct);
    }

    public async Task UpsertAsync(MonitorRollup rollup, CancellationToken ct = default)
    {
        var existing = await _context.MonitorRollups
            .FirstOrDefaultAsync(r =>
                r.MonitorId == rollup.MonitorId
                && r.Granularity == rollup.Granularity
                && r.PeriodStart == rollup.PeriodStart, ct);

        if (existing != null)
        {
            existing.Count = rollup.Count;
            existing.SuccessCount = rollup.SuccessCount;
            existing.FailureCount = rollup.FailureCount;
            existing.Min = rollup.Min;
            existing.Max = rollup.Max;
            existing.Mean = rollup.Mean;
            existing.Median = rollup.Median;
            existing.P80 = rollup.P80;
            existing.P90 = rollup.P90;
            existing.P95 = rollup.P95;
            existing.StdDev = rollup.StdDev;
            existing.UptimePercent = rollup.UptimePercent;
        }
        else
        {
            await _context.MonitorRollups.AddAsync(rollup, ct);
        }
    }

    public async Task<IReadOnlyList<MonitorRollup>> GetByMonitorIdAsync(Guid monitorId, Granularity? granularity, DateTime? from, DateTime? until, CancellationToken ct = default)
    {
        var query = _context.MonitorRollups.Where(r => r.MonitorId == monitorId);

        if (granularity.HasValue)
            query = query.Where(r => r.Granularity == granularity.Value);

        if (from.HasValue)
            query = query.Where(r => r.PeriodStart >= from.Value);

        if (until.HasValue)
            query = query.Where(r => r.PeriodStart <= until.Value);

        return await query
            .OrderBy(r => r.PeriodStart)
            .ToListAsync(ct);
    }

    public async Task<MonitorRollup?> GetByMonitorIdAndPeriodAsync(Guid monitorId, Granularity granularity, DateTime periodStart, CancellationToken ct = default)
    {
        return await _context.MonitorRollups
            .FirstOrDefaultAsync(r =>
                r.MonitorId == monitorId
                && r.Granularity == granularity
                && r.PeriodStart == periodStart, ct);
    }

    public async Task<IReadOnlyList<MonitorRollup>> GetByServiceIdAsync(Guid serviceId, Granularity granularity, DateTime? from, DateTime? until, CancellationToken ct = default)
    {
        var query = _context.MonitorRollups
            .Where(r => r.ServiceId == serviceId && r.Granularity == granularity);

        if (from.HasValue)
            query = query.Where(r => r.PeriodStart >= from.Value);

        if (until.HasValue)
            query = query.Where(r => r.PeriodStart <= until.Value);

        return await query
            .OrderBy(r => r.PeriodStart)
            .ToListAsync(ct);
    }

    public async Task DeleteOlderThanAsync(Granularity granularity, DateTime threshold, CancellationToken ct = default)
    {
        var oldRollups = await _context.MonitorRollups
            .Where(r => r.Granularity == granularity && r.PeriodStart < threshold)
            .ToListAsync(ct);

        _context.MonitorRollups.RemoveRange(oldRollups);
    }
}
