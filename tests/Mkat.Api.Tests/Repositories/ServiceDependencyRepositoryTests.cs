using Microsoft.EntityFrameworkCore;
using Mkat.Domain.Entities;
using Mkat.Infrastructure.Data;
using Mkat.Infrastructure.Repositories;
using Xunit;

namespace Mkat.Api.Tests.Repositories;

public class ServiceDependencyRepositoryTests
{
    private static MkatDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<MkatDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new MkatDbContext(options);
    }

    private static async Task<(Service a, Service b, Service c)> SeedThreeServices(MkatDbContext ctx)
    {
        var a = new Service { Id = Guid.NewGuid(), Name = "A" };
        var b = new Service { Id = Guid.NewGuid(), Name = "B" };
        var c = new Service { Id = Guid.NewGuid(), Name = "C" };
        ctx.Services.AddRange(a, b, c);
        await ctx.SaveChangesAsync();
        return (a, b, c);
    }

    [Fact]
    public async Task AddAndGetDependencies_Works()
    {
        var dbName = $"dep_test_{Guid.NewGuid()}";
        await using var ctx = CreateContext(dbName);
        var (a, b, _) = await SeedThreeServices(ctx);
        var repo = new ServiceDependencyRepository(ctx);

        var dep = new ServiceDependency
        {
            Id = Guid.NewGuid(),
            DependentServiceId = a.Id,
            DependencyServiceId = b.Id
        };
        await repo.AddAsync(dep);
        await ctx.SaveChangesAsync();

        var deps = await repo.GetDependenciesAsync(a.Id);
        Assert.Single(deps);
        Assert.Equal(b.Id, deps[0].DependencyServiceId);
    }

    [Fact]
    public async Task GetDependents_ReturnsCorrectDirection()
    {
        var dbName = $"dep_test_{Guid.NewGuid()}";
        await using var ctx = CreateContext(dbName);
        var (a, b, _) = await SeedThreeServices(ctx);
        var repo = new ServiceDependencyRepository(ctx);

        var dep = new ServiceDependency
        {
            Id = Guid.NewGuid(),
            DependentServiceId = a.Id,
            DependencyServiceId = b.Id
        };
        await repo.AddAsync(dep);
        await ctx.SaveChangesAsync();

        var dependents = await repo.GetDependentsAsync(b.Id);
        Assert.Single(dependents);
        Assert.Equal(a.Id, dependents[0].DependentServiceId);
    }

    [Fact]
    public async Task WouldCreateCycle_ReturnsFalse_WhenNoCycle()
    {
        var dbName = $"dep_test_{Guid.NewGuid()}";
        await using var ctx = CreateContext(dbName);
        var (a, b, _) = await SeedThreeServices(ctx);
        var repo = new ServiceDependencyRepository(ctx);

        var result = await repo.WouldCreateCycleAsync(a.Id, b.Id);
        Assert.False(result);
    }

    [Fact]
    public async Task WouldCreateCycle_ReturnsTrue_ForDirectCycle()
    {
        var dbName = $"dep_test_{Guid.NewGuid()}";
        await using var ctx = CreateContext(dbName);
        var (a, b, _) = await SeedThreeServices(ctx);
        var repo = new ServiceDependencyRepository(ctx);

        ctx.ServiceDependencies.Add(new ServiceDependency
        {
            Id = Guid.NewGuid(),
            DependentServiceId = a.Id,
            DependencyServiceId = b.Id
        });
        await ctx.SaveChangesAsync();

        var result = await repo.WouldCreateCycleAsync(b.Id, a.Id);
        Assert.True(result);
    }

    [Fact]
    public async Task WouldCreateCycle_ReturnsTrue_ForTransitiveCycle()
    {
        var dbName = $"dep_test_{Guid.NewGuid()}";
        await using var ctx = CreateContext(dbName);
        var (a, b, c) = await SeedThreeServices(ctx);
        var repo = new ServiceDependencyRepository(ctx);

        ctx.ServiceDependencies.AddRange(
            new ServiceDependency { Id = Guid.NewGuid(), DependentServiceId = a.Id, DependencyServiceId = b.Id },
            new ServiceDependency { Id = Guid.NewGuid(), DependentServiceId = b.Id, DependencyServiceId = c.Id }
        );
        await ctx.SaveChangesAsync();

        var result = await repo.WouldCreateCycleAsync(c.Id, a.Id);
        Assert.True(result);
    }

    [Fact]
    public async Task GetTransitiveDependentIds_ReturnsFullChain()
    {
        var dbName = $"dep_test_{Guid.NewGuid()}";
        await using var ctx = CreateContext(dbName);
        var (a, b, c) = await SeedThreeServices(ctx);
        var repo = new ServiceDependencyRepository(ctx);

        ctx.ServiceDependencies.AddRange(
            new ServiceDependency { Id = Guid.NewGuid(), DependentServiceId = a.Id, DependencyServiceId = b.Id },
            new ServiceDependency { Id = Guid.NewGuid(), DependentServiceId = b.Id, DependencyServiceId = c.Id }
        );
        await ctx.SaveChangesAsync();

        var dependents = await repo.GetTransitiveDependentIdsAsync(c.Id);
        Assert.Contains(b.Id, dependents);
        Assert.Contains(a.Id, dependents);
        Assert.Equal(2, dependents.Count);
    }

    [Fact]
    public async Task Delete_RemovesDependency()
    {
        var dbName = $"dep_test_{Guid.NewGuid()}";
        await using var ctx = CreateContext(dbName);
        var (a, b, _) = await SeedThreeServices(ctx);
        var repo = new ServiceDependencyRepository(ctx);

        var dep = new ServiceDependency
        {
            Id = Guid.NewGuid(),
            DependentServiceId = a.Id,
            DependencyServiceId = b.Id
        };
        await repo.AddAsync(dep);
        await ctx.SaveChangesAsync();

        await repo.DeleteAsync(dep);
        await ctx.SaveChangesAsync();

        var deps = await repo.GetDependenciesAsync(a.Id);
        Assert.Empty(deps);
    }
}
