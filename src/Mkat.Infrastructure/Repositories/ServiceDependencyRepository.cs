using Microsoft.EntityFrameworkCore;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Infrastructure.Data;

namespace Mkat.Infrastructure.Repositories;

public class ServiceDependencyRepository : IServiceDependencyRepository
{
    private readonly MkatDbContext _context;

    public ServiceDependencyRepository(MkatDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceDependency?> GetAsync(Guid dependentServiceId, Guid dependencyServiceId, CancellationToken ct = default)
    {
        return await _context.ServiceDependencies
            .FirstOrDefaultAsync(d => d.DependentServiceId == dependentServiceId && d.DependencyServiceId == dependencyServiceId, ct);
    }

    public async Task<IReadOnlyList<ServiceDependency>> GetDependenciesAsync(Guid serviceId, CancellationToken ct = default)
    {
        return await _context.ServiceDependencies
            .Include(d => d.DependencyService)
            .Where(d => d.DependentServiceId == serviceId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ServiceDependency>> GetDependentsAsync(Guid serviceId, CancellationToken ct = default)
    {
        return await _context.ServiceDependencies
            .Include(d => d.DependentService)
            .Where(d => d.DependencyServiceId == serviceId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ServiceDependency>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.ServiceDependencies
            .Include(d => d.DependentService)
            .Include(d => d.DependencyService)
            .ToListAsync(ct);
    }

    public async Task AddAsync(ServiceDependency dependency, CancellationToken ct = default)
    {
        dependency.CreatedAt = DateTime.UtcNow;
        await _context.ServiceDependencies.AddAsync(dependency, ct);
    }

    public Task DeleteAsync(ServiceDependency dependency, CancellationToken ct = default)
    {
        _context.ServiceDependencies.Remove(dependency);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Guid>> GetTransitiveDependentIdsAsync(Guid serviceId, CancellationToken ct = default)
    {
        var allEdges = await _context.ServiceDependencies
            .Select(d => new { d.DependentServiceId, d.DependencyServiceId })
            .ToListAsync(ct);

        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(serviceId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var directDependents = allEdges
                .Where(e => e.DependencyServiceId == current)
                .Select(e => e.DependentServiceId);

            foreach (var dep in directDependents)
            {
                if (visited.Add(dep))
                {
                    queue.Enqueue(dep);
                }
            }
        }

        return visited.ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetTransitiveDependencyIdsAsync(Guid serviceId, CancellationToken ct = default)
    {
        var allEdges = await _context.ServiceDependencies
            .Select(d => new { d.DependentServiceId, d.DependencyServiceId })
            .ToListAsync(ct);

        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(serviceId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var directDeps = allEdges
                .Where(e => e.DependentServiceId == current)
                .Select(e => e.DependencyServiceId);

            foreach (var dep in directDeps)
            {
                if (visited.Add(dep))
                {
                    queue.Enqueue(dep);
                }
            }
        }

        return visited.ToList();
    }

    public async Task<bool> WouldCreateCycleAsync(Guid dependentServiceId, Guid dependencyServiceId, CancellationToken ct = default)
    {
        if (dependentServiceId == dependencyServiceId) return true;

        var transitiveDeps = await GetTransitiveDependencyIdsAsync(dependencyServiceId, ct);
        return transitiveDeps.Contains(dependentServiceId);
    }
}
