using Microsoft.EntityFrameworkCore;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Data;
using Mkat.Infrastructure.Repositories;
using Xunit;
using Monitor = Mkat.Domain.Entities.Monitor;

namespace Mkat.Api.Tests.Repositories;

public class MonitorRollupRepositoryTests : IDisposable
{
    private readonly MkatDbContext _context;
    private readonly MonitorRollupRepository _repository;
    private readonly Guid _serviceId = Guid.NewGuid();
    private readonly Guid _monitorId = Guid.NewGuid();

    public MonitorRollupRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MkatDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _context = new MkatDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

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

        _repository = new MonitorRollupRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_AddsRollupToDatabase()
    {
        var rollup = CreateRollup(Granularity.Hourly, DateTime.UtcNow);

        await _repository.AddAsync(rollup);
        await _context.SaveChangesAsync();

        var result = await _context.MonitorRollups.FindAsync(rollup.Id);
        Assert.NotNull(result);
        Assert.Equal(Granularity.Hourly, result.Granularity);
    }

    [Fact]
    public async Task UpsertAsync_InsertsNewRollup()
    {
        var periodStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var rollup = CreateRollup(Granularity.Hourly, periodStart);
        rollup.Count = 10;

        await _repository.UpsertAsync(rollup);
        await _context.SaveChangesAsync();

        var result = await _context.MonitorRollups
            .FirstOrDefaultAsync(r => r.MonitorId == _monitorId
                && r.Granularity == Granularity.Hourly
                && r.PeriodStart == periodStart);
        Assert.NotNull(result);
        Assert.Equal(10, result.Count);
    }

    [Fact]
    public async Task UpsertAsync_UpdatesExistingRollup()
    {
        var periodStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var rollup1 = CreateRollup(Granularity.Hourly, periodStart);
        rollup1.Count = 10;

        await _repository.UpsertAsync(rollup1);
        await _context.SaveChangesAsync();

        var rollup2 = CreateRollup(Granularity.Hourly, periodStart);
        rollup2.Count = 20;

        await _repository.UpsertAsync(rollup2);
        await _context.SaveChangesAsync();

        var all = await _context.MonitorRollups
            .Where(r => r.MonitorId == _monitorId
                && r.Granularity == Granularity.Hourly
                && r.PeriodStart == periodStart)
            .ToListAsync();
        Assert.Single(all);
        Assert.Equal(20, all[0].Count);
    }

    [Fact]
    public async Task GetByMonitorIdAsync_FiltersCorrectly()
    {
        await SeedRollups();

        var results = await _repository.GetByMonitorIdAsync(
            _monitorId, Granularity.Hourly, null, null);

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task GetByMonitorIdAsync_FiltersByDateRange()
    {
        await SeedRollups();

        var from = new DateTime(2026, 1, 1, 1, 0, 0, DateTimeKind.Utc);
        var until = new DateTime(2026, 1, 1, 2, 0, 0, DateTimeKind.Utc);

        var results = await _repository.GetByMonitorIdAsync(
            _monitorId, Granularity.Hourly, from, until);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task GetByMonitorIdAndPeriodAsync_ReturnsMatch()
    {
        await SeedRollups();

        var periodStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = await _repository.GetByMonitorIdAndPeriodAsync(
            _monitorId, Granularity.Hourly, periodStart);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetByServiceIdAsync_ReturnsServiceRollups()
    {
        await SeedRollups();

        var results = await _repository.GetByServiceIdAsync(
            _serviceId, Granularity.Hourly, null, null);

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task DeleteOlderThanAsync_RemovesOldRollups()
    {
        await SeedRollups();

        var threshold = new DateTime(2026, 1, 1, 1, 0, 0, DateTimeKind.Utc);
        await _repository.DeleteOlderThanAsync(Granularity.Hourly, threshold);
        await _context.SaveChangesAsync();

        var remaining = await _context.MonitorRollups.ToListAsync();
        Assert.Equal(2, remaining.Count);
    }

    private MonitorRollup CreateRollup(Granularity granularity, DateTime periodStart)
    {
        return new MonitorRollup
        {
            Id = Guid.NewGuid(),
            MonitorId = _monitorId,
            ServiceId = _serviceId,
            Granularity = granularity,
            PeriodStart = periodStart,
            Count = 5,
            SuccessCount = 4,
            FailureCount = 1
        };
    }

    private async Task SeedRollups()
    {
        var rollups = new[]
        {
            CreateRollup(Granularity.Hourly, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateRollup(Granularity.Hourly, new DateTime(2026, 1, 1, 1, 0, 0, DateTimeKind.Utc)),
            CreateRollup(Granularity.Hourly, new DateTime(2026, 1, 1, 2, 0, 0, DateTimeKind.Utc))
        };

        _context.MonitorRollups.AddRange(rollups);
        await _context.SaveChangesAsync();
    }
}
