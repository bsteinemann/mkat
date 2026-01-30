using Microsoft.EntityFrameworkCore;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Data;
using Mkat.Infrastructure.Repositories;
using Xunit;
using Monitor = Mkat.Domain.Entities.Monitor;

namespace Mkat.Api.Tests.Repositories;

public class MonitorEventRepositoryTests : IDisposable
{
    private readonly MkatDbContext _context;
    private readonly MonitorEventRepository _repository;
    private readonly Guid _serviceId = Guid.NewGuid();
    private readonly Guid _monitorId = Guid.NewGuid();

    public MonitorEventRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MkatDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _context = new MkatDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        // Seed a service and monitor
        var service = new Service { Id = _serviceId, Name = "Test Service" };
        var monitor = new Monitor
        {
            Id = _monitorId,
            ServiceId = _serviceId,
            Type = MonitorType.HealthCheck,
            Token = Guid.NewGuid().ToString()
        };
        _context.Services.Add(service);
        _context.Monitors.Add(monitor);
        _context.SaveChanges();

        _repository = new MonitorEventRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_AddsEventToDatabase()
    {
        var evt = new MonitorEvent
        {
            Id = Guid.NewGuid(),
            MonitorId = _monitorId,
            ServiceId = _serviceId,
            EventType = EventType.HealthCheckPerformed,
            Success = true,
            Value = 120.5,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(evt);
        await _context.SaveChangesAsync();

        var result = await _context.MonitorEvents.FindAsync(evt.Id);
        Assert.NotNull(result);
        Assert.Equal(EventType.HealthCheckPerformed, result.EventType);
        Assert.Equal(120.5, result.Value);
    }

    [Fact]
    public async Task GetByMonitorIdAsync_ReturnsFilteredEvents()
    {
        var now = DateTime.UtcNow;
        await SeedEvents(now);

        var results = await _repository.GetByMonitorIdAsync(_monitorId, null, null, null);

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task GetByMonitorIdAsync_FiltersByDateRange()
    {
        var now = DateTime.UtcNow;
        await SeedEvents(now);

        var results = await _repository.GetByMonitorIdAsync(
            _monitorId, now.AddHours(-1.5), now.AddMinutes(-30), null);

        Assert.Single(results);
    }

    [Fact]
    public async Task GetByMonitorIdAsync_FiltersByEventType()
    {
        var now = DateTime.UtcNow;
        await SeedEvents(now);

        var results = await _repository.GetByMonitorIdAsync(
            _monitorId, null, null, EventType.StateChanged);

        Assert.Single(results);
    }

    [Fact]
    public async Task GetByMonitorIdAsync_RespectsLimit()
    {
        var now = DateTime.UtcNow;
        await SeedEvents(now);

        var results = await _repository.GetByMonitorIdAsync(
            _monitorId, null, null, null, limit: 2);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task GetByServiceIdAsync_ReturnsAllServiceEvents()
    {
        var now = DateTime.UtcNow;
        await SeedEvents(now);

        var results = await _repository.GetByServiceIdAsync(
            _serviceId, null, null, null);

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task GetByMonitorIdInWindowAsync_ReturnsEventsInWindow()
    {
        var now = DateTime.UtcNow;
        await SeedEvents(now);

        var results = await _repository.GetByMonitorIdInWindowAsync(
            _monitorId, now.AddHours(-1.5), now);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task DeleteOlderThanAsync_RemovesOldEvents()
    {
        var now = DateTime.UtcNow;
        await SeedEvents(now);

        await _repository.DeleteOlderThanAsync(now.AddMinutes(-30));
        await _context.SaveChangesAsync();

        var remaining = await _context.MonitorEvents.ToListAsync();
        Assert.Single(remaining);
    }

    [Fact]
    public async Task GetLastNByMonitorIdAsync_ReturnsLastNEvents()
    {
        var now = DateTime.UtcNow;
        // Seed 5 metric events with values
        for (int i = 0; i < 5; i++)
        {
            _context.MonitorEvents.Add(new MonitorEvent
            {
                Id = Guid.NewGuid(),
                MonitorId = _monitorId,
                ServiceId = _serviceId,
                EventType = EventType.MetricIngested,
                Success = true,
                Value = 10.0 + i,
                CreatedAt = now.AddMinutes(-5 + i)
            });
        }
        await _context.SaveChangesAsync();

        var results = await _repository.GetLastNByMonitorIdAsync(_monitorId, 3);

        Assert.Equal(3, results.Count);
        // Should be ordered by CreatedAt descending (most recent first)
        Assert.True(results[0].CreatedAt >= results[1].CreatedAt);
        Assert.True(results[1].CreatedAt >= results[2].CreatedAt);
    }

    private async Task SeedEvents(DateTime now)
    {
        var events = new[]
        {
            new MonitorEvent
            {
                Id = Guid.NewGuid(), MonitorId = _monitorId, ServiceId = _serviceId,
                EventType = EventType.HealthCheckPerformed, Success = true, Value = 100,
                CreatedAt = now.AddHours(-2)
            },
            new MonitorEvent
            {
                Id = Guid.NewGuid(), MonitorId = _monitorId, ServiceId = _serviceId,
                EventType = EventType.HealthCheckPerformed, Success = true, Value = 200,
                CreatedAt = now.AddHours(-1)
            },
            new MonitorEvent
            {
                Id = Guid.NewGuid(), MonitorId = _monitorId, ServiceId = _serviceId,
                EventType = EventType.StateChanged, Success = false,
                CreatedAt = now
            }
        };

        _context.MonitorEvents.AddRange(events);
        await _context.SaveChangesAsync();
    }
}
