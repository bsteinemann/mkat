using Microsoft.EntityFrameworkCore;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Infrastructure.Data;

namespace Mkat.Infrastructure.Repositories;

public class MetricReadingRepository : IMetricReadingRepository
{
    private readonly MkatDbContext _context;

    public MetricReadingRepository(MkatDbContext context)
    {
        _context = context;
    }

    public async Task<MetricReading?> GetLatestByMonitorIdAsync(Guid monitorId, CancellationToken ct = default)
    {
        return await _context.MetricReadings
            .Where(r => r.MonitorId == monitorId)
            .OrderByDescending(r => r.RecordedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<MetricReading>> GetByMonitorIdAsync(Guid monitorId, DateTime? from, DateTime? to, int limit = 100, CancellationToken ct = default)
    {
        var query = _context.MetricReadings
            .Where(r => r.MonitorId == monitorId);

        if (from.HasValue)
            query = query.Where(r => r.RecordedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(r => r.RecordedAt <= to.Value);

        return await query
            .OrderByDescending(r => r.RecordedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<MetricReading>> GetLastNByMonitorIdAsync(Guid monitorId, int count, CancellationToken ct = default)
    {
        return await _context.MetricReadings
            .Where(r => r.MonitorId == monitorId)
            .OrderByDescending(r => r.RecordedAt)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<MetricReading>> GetByMonitorIdInWindowAsync(Guid monitorId, DateTime windowStart, CancellationToken ct = default)
    {
        return await _context.MetricReadings
            .Where(r => r.MonitorId == monitorId && r.RecordedAt >= windowStart)
            .OrderByDescending(r => r.RecordedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(MetricReading reading, CancellationToken ct = default)
    {
        await _context.MetricReadings.AddAsync(reading, ct);
    }

    public async Task DeleteOlderThanAsync(Guid monitorId, DateTime threshold, CancellationToken ct = default)
    {
        var oldReadings = await _context.MetricReadings
            .Where(r => r.MonitorId == monitorId && r.RecordedAt < threshold)
            .ToListAsync(ct);

        _context.MetricReadings.RemoveRange(oldReadings);
    }
}
