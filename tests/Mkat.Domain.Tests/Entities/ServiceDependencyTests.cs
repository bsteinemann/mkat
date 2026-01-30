using Mkat.Domain.Entities;
using Xunit;

namespace Mkat.Domain.Tests.Entities;

public class ServiceDependencyTests
{
    [Fact]
    public void ServiceDependency_HasRequiredProperties()
    {
        var dep = new ServiceDependency
        {
            Id = Guid.NewGuid(),
            DependentServiceId = Guid.NewGuid(),
            DependencyServiceId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        Assert.NotEqual(Guid.Empty, dep.Id);
        Assert.NotEqual(Guid.Empty, dep.DependentServiceId);
        Assert.NotEqual(Guid.Empty, dep.DependencyServiceId);
    }

    [Fact]
    public void ServiceDependency_HasNavigationProperties()
    {
        var dependent = new Service { Id = Guid.NewGuid(), Name = "App" };
        var dependency = new Service { Id = Guid.NewGuid(), Name = "Database" };

        var dep = new ServiceDependency
        {
            Id = Guid.NewGuid(),
            DependentServiceId = dependent.Id,
            DependencyServiceId = dependency.Id,
            DependentService = dependent,
            DependencyService = dependency,
            CreatedAt = DateTime.UtcNow
        };

        Assert.Equal("App", dep.DependentService.Name);
        Assert.Equal("Database", dep.DependencyService.Name);
    }

    [Fact]
    public void Service_HasDependencyCollections()
    {
        var service = new Service { Id = Guid.NewGuid(), Name = "App" };

        Assert.NotNull(service.DependsOn);
        Assert.NotNull(service.DependedOnBy);
        Assert.Empty(service.DependsOn);
        Assert.Empty(service.DependedOnBy);
    }
}
