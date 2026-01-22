using Microsoft.EntityFrameworkCore;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Data;

namespace Mkat.Infrastructure.Repositories;

public class MonitorRepository : IMonitorRepository
{
    private readonly MkatDbContext _context;

    public MonitorRepository(MkatDbContext context)
    {
        _context = context;
    }

    public async Task<Monitor?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Monitors
            .Include(m => m.Service)
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<Monitor?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        return await _context.Monitors
            .Include(m => m.Service)
            .FirstOrDefaultAsync(m => m.Token == token, ct);
    }

    public async Task<IReadOnlyList<Monitor>> GetByServiceIdAsync(Guid serviceId, CancellationToken ct = default)
    {
        return await _context.Monitors
            .Where(m => m.ServiceId == serviceId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Monitor>> GetHeartbeatMonitorsDueAsync(DateTime threshold, CancellationToken ct = default)
    {
        return await _context.Monitors
            .Include(m => m.Service)
            .Where(m => m.Type == MonitorType.Heartbeat)
            .Where(m => m.LastCheckIn == null || m.LastCheckIn < threshold)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Monitor monitor, CancellationToken ct = default)
    {
        monitor.CreatedAt = DateTime.UtcNow;
        monitor.UpdatedAt = DateTime.UtcNow;
        await _context.Monitors.AddAsync(monitor, ct);
    }

    public Task UpdateAsync(Monitor monitor, CancellationToken ct = default)
    {
        monitor.UpdatedAt = DateTime.UtcNow;
        _context.Monitors.Update(monitor);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Monitor monitor, CancellationToken ct = default)
    {
        _context.Monitors.Remove(monitor);
        return Task.CompletedTask;
    }
}
