using Microsoft.EntityFrameworkCore;
using Mkat.Domain.Entities;
using Mkat.Infrastructure.Data;
using Mkat.Infrastructure.Repositories;
using Xunit;

namespace Mkat.Api.Tests.Repositories;

public class MuteWindowRepositoryTests : IDisposable
{
    private readonly MkatDbContext _context;
    private readonly MuteWindowRepository _repository;
    private readonly Guid _serviceId = Guid.NewGuid();

    public MuteWindowRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MkatDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _context = new MkatDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        // Seed a service for FK relationships
        _context.Services.Add(new Service
        {
            Id = _serviceId,
            Name = "Test Service"
        });
        _context.SaveChanges();

        _repository = new MuteWindowRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    [Fact]
    public async Task IsServiceMutedAsync_ReturnsTrue_WhenActiveWindowExists()
    {
        var now = DateTime.UtcNow;
        await _context.MuteWindows.AddAsync(new MuteWindow
        {
            Id = Guid.NewGuid(),
            ServiceId = _serviceId,
            StartsAt = now.AddMinutes(-10),
            EndsAt = now.AddMinutes(10)
        });
        await _context.SaveChangesAsync();

        var result = await _repository.IsServiceMutedAsync(_serviceId, now);

        Assert.True(result);
    }

    [Fact]
    public async Task IsServiceMutedAsync_ReturnsFalse_WhenNoActiveWindow()
    {
        var now = DateTime.UtcNow;
        // Window in the past
        await _context.MuteWindows.AddAsync(new MuteWindow
        {
            Id = Guid.NewGuid(),
            ServiceId = _serviceId,
            StartsAt = now.AddHours(-2),
            EndsAt = now.AddHours(-1)
        });
        await _context.SaveChangesAsync();

        var result = await _repository.IsServiceMutedAsync(_serviceId, now);

        Assert.False(result);
    }

    [Fact]
    public async Task IsServiceMutedAsync_ReturnsFalse_WhenWindowNotYetStarted()
    {
        var now = DateTime.UtcNow;
        await _context.MuteWindows.AddAsync(new MuteWindow
        {
            Id = Guid.NewGuid(),
            ServiceId = _serviceId,
            StartsAt = now.AddHours(1),
            EndsAt = now.AddHours(2)
        });
        await _context.SaveChangesAsync();

        var result = await _repository.IsServiceMutedAsync(_serviceId, now);

        Assert.False(result);
    }

    [Fact]
    public async Task IsServiceMutedAsync_ReturnsFalse_WhenDifferentService()
    {
        var now = DateTime.UtcNow;
        var otherServiceId = Guid.NewGuid();
        _context.Services.Add(new Service { Id = otherServiceId, Name = "Other" });
        await _context.MuteWindows.AddAsync(new MuteWindow
        {
            Id = Guid.NewGuid(),
            ServiceId = otherServiceId,
            StartsAt = now.AddMinutes(-10),
            EndsAt = now.AddMinutes(10)
        });
        await _context.SaveChangesAsync();

        var result = await _repository.IsServiceMutedAsync(_serviceId, now);

        Assert.False(result);
    }

    [Fact]
    public async Task AddAsync_AddsMuteWindowToDatabase()
    {
        var mute = new MuteWindow
        {
            Id = Guid.NewGuid(),
            ServiceId = _serviceId,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddHours(1),
            Reason = "Maintenance"
        };

        var result = await _repository.AddAsync(mute);
        await _context.SaveChangesAsync();

        var stored = await _context.MuteWindows.FindAsync(mute.Id);
        Assert.NotNull(stored);
        Assert.Equal("Maintenance", stored.Reason);
        Assert.Equal(mute.Id, result.Id);
    }

    [Fact]
    public async Task GetActiveForServiceAsync_ReturnsOnlyActiveWindows()
    {
        var now = DateTime.UtcNow;
        await _context.MuteWindows.AddRangeAsync(
            new MuteWindow
            {
                Id = Guid.NewGuid(),
                ServiceId = _serviceId,
                StartsAt = now.AddMinutes(-10),
                EndsAt = now.AddMinutes(10)
            },
            new MuteWindow
            {
                Id = Guid.NewGuid(),
                ServiceId = _serviceId,
                StartsAt = now.AddHours(-2),
                EndsAt = now.AddHours(-1) // expired
            },
            new MuteWindow
            {
                Id = Guid.NewGuid(),
                ServiceId = _serviceId,
                StartsAt = now.AddMinutes(-5),
                EndsAt = now.AddMinutes(30) // active
            }
        );
        await _context.SaveChangesAsync();

        var result = await _repository.GetActiveForServiceAsync(_serviceId);

        Assert.Equal(2, result.Count);
    }
}
