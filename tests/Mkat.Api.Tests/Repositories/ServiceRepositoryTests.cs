using Microsoft.EntityFrameworkCore;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Data;
using Mkat.Infrastructure.Repositories;
using Xunit;
using Monitor = Mkat.Domain.Entities.Monitor;

namespace Mkat.Api.Tests.Repositories;

public class ServiceRepositoryTests : IDisposable
{
    private readonly MkatDbContext _context;
    private readonly ServiceRepository _repository;

    public ServiceRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MkatDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _context = new MkatDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _repository = new ServiceRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_AddsServiceToDatabase()
    {
        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = "Test Service"
        };

        await _repository.AddAsync(service);
        await _context.SaveChangesAsync();

        var result = await _context.Services.FindAsync(service.Id);
        Assert.NotNull(result);
        Assert.Equal("Test Service", result.Name);
    }

    [Fact]
    public async Task AddAsync_SetsCreatedAtAndUpdatedAt()
    {
        var before = DateTime.UtcNow;
        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = "Test Service"
        };

        await _repository.AddAsync(service);
        await _context.SaveChangesAsync();
        var after = DateTime.UtcNow;

        Assert.InRange(service.CreatedAt, before, after);
        Assert.InRange(service.UpdatedAt, before, after);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsService_WhenExists()
    {
        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = "Test Service"
        };
        await _context.Services.AddAsync(service);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(service.Id);

        Assert.NotNull(result);
        Assert.Equal(service.Id, result.Id);
        Assert.Equal("Test Service", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesMonitors()
    {
        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = "Test Service"
        };
        var monitor = new Monitor
        {
            Id = Guid.NewGuid(),
            ServiceId = service.Id,
            Token = "test-token",
            Type = MonitorType.Heartbeat,
            IntervalSeconds = 60,
            GracePeriodSeconds = 30
        };
        await _context.Services.AddAsync(service);
        await _context.Monitors.AddAsync(monitor);
        await _context.SaveChangesAsync();

        // Detach to force reload from DB
        _context.ChangeTracker.Clear();

        var result = await _repository.GetByIdAsync(service.Id);

        Assert.NotNull(result);
        Assert.Single(result.Monitors);
        Assert.Equal("test-token", result.Monitors.First().Token);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByName()
    {
        var services = new[]
        {
            new Service { Id = Guid.NewGuid(), Name = "Charlie" },
            new Service { Id = Guid.NewGuid(), Name = "Alpha" },
            new Service { Id = Guid.NewGuid(), Name = "Bravo" }
        };
        await _context.Services.AddRangeAsync(services);
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        Assert.Equal(3, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Bravo", result[1].Name);
        Assert.Equal("Charlie", result[2].Name);
    }

    [Fact]
    public async Task GetAllAsync_RespectsPagination()
    {
        for (int i = 0; i < 5; i++)
        {
            await _context.Services.AddAsync(new Service
            {
                Id = Guid.NewGuid(),
                Name = $"Service {i:D2}"
            });
        }
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync(skip: 1, take: 2);

        Assert.Equal(2, result.Count);
        Assert.Equal("Service 01", result[0].Name);
        Assert.Equal("Service 02", result[1].Name);
    }

    [Fact]
    public async Task GetCountAsync_ReturnsCorrectCount()
    {
        await _context.Services.AddRangeAsync(
            new Service { Id = Guid.NewGuid(), Name = "Service 1" },
            new Service { Id = Guid.NewGuid(), Name = "Service 2" }
        );
        await _context.SaveChangesAsync();

        var count = await _repository.GetCountAsync();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task UpdateAsync_SetsUpdatedAt()
    {
        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = "Original Name"
        };
        await _context.Services.AddAsync(service);
        await _context.SaveChangesAsync();

        var before = DateTime.UtcNow;
        service.Name = "Updated Name";
        await _repository.UpdateAsync(service);
        await _context.SaveChangesAsync();
        var after = DateTime.UtcNow;

        Assert.InRange(service.UpdatedAt, before, after);
    }

    [Fact]
    public async Task DeleteAsync_RemovesServiceFromDatabase()
    {
        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = "To Delete"
        };
        await _context.Services.AddAsync(service);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(service);
        await _context.SaveChangesAsync();

        var result = await _context.Services.FindAsync(service.Id);
        Assert.Null(result);
    }
}
