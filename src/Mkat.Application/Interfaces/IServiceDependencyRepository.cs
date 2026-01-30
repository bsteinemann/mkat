using Mkat.Domain.Entities;

namespace Mkat.Application.Interfaces;

public interface IServiceDependencyRepository
{
    Task<ServiceDependency?> GetAsync(Guid dependentServiceId, Guid dependencyServiceId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceDependency>> GetDependenciesAsync(Guid serviceId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceDependency>> GetDependentsAsync(Guid serviceId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceDependency>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(ServiceDependency dependency, CancellationToken ct = default);
    Task DeleteAsync(ServiceDependency dependency, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetTransitiveDependentIdsAsync(Guid serviceId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetTransitiveDependencyIdsAsync(Guid serviceId, CancellationToken ct = default);
    Task<bool> WouldCreateCycleAsync(Guid dependentServiceId, Guid dependencyServiceId, CancellationToken ct = default);
}
