using Microsoft.EntityFrameworkCore;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Infrastructure.Data;

namespace Mkat.Infrastructure.Repositories;

public class AlertRepository : IAlertRepository
{
    private readonly MkatDbContext _context;

    public AlertRepository(MkatDbContext context)
    {
        _context = context;
    }

    public async Task<Alert?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Alerts
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<IReadOnlyList<Alert>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default)
    {
        return await _context.Alerts
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Alert>> GetByServiceIdAsync(Guid serviceId, CancellationToken ct = default)
    {
        return await _context.Alerts
            .Where(a => a.ServiceId == serviceId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Alert>> GetPendingDispatchAsync(CancellationToken ct = default)
    {
        return await _context.Alerts
            .Include(a => a.Service)
            .Where(a => a.DispatchedAt == null)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        return await _context.Alerts.CountAsync(ct);
    }

    public async Task AddAsync(Alert alert, CancellationToken ct = default)
    {
        alert.CreatedAt = DateTime.UtcNow;
        await _context.Alerts.AddAsync(alert, ct);
    }

    public Task UpdateAsync(Alert alert, CancellationToken ct = default)
    {
        _context.Alerts.Update(alert);
        return Task.CompletedTask;
    }
}
