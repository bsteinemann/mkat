using Microsoft.EntityFrameworkCore;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Infrastructure.Data;

namespace Mkat.Infrastructure.Repositories;

public class ServiceRepository : IServiceRepository
{
    private readonly MkatDbContext _context;

    public ServiceRepository(MkatDbContext context)
    {
        _context = context;
    }

    public async Task<Service?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Services
            .Include(s => s.Monitors)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<IReadOnlyList<Service>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default)
    {
        return await _context.Services
            .OrderBy(s => s.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        return await _context.Services.CountAsync(ct);
    }

    public async Task AddAsync(Service service, CancellationToken ct = default)
    {
        service.CreatedAt = DateTime.UtcNow;
        service.UpdatedAt = DateTime.UtcNow;
        await _context.Services.AddAsync(service, ct);
    }

    public Task UpdateAsync(Service service, CancellationToken ct = default)
    {
        service.UpdatedAt = DateTime.UtcNow;
        _context.Services.Update(service);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Service service, CancellationToken ct = default)
    {
        _context.Services.Remove(service);
        return Task.CompletedTask;
    }
}
