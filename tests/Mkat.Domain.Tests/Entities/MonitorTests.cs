using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Xunit;
using Monitor = Mkat.Domain.Entities.Monitor;

namespace Mkat.Domain.Tests.Entities;

public class MonitorTests
{
    [Fact]
    public void NewMonitor_HasEmptyToken()
    {
        var monitor = new Monitor();
        Assert.Equal(string.Empty, monitor.Token);
    }

    [Fact]
    public void NewMonitor_HasNullConfigJson()
    {
        var monitor = new Monitor();
        Assert.Null(monitor.ConfigJson);
    }

    [Fact]
    public void NewMonitor_HasNullLastCheckIn()
    {
        var monitor = new Monitor();
        Assert.Null(monitor.LastCheckIn);
    }

    [Fact]
    public void NewMonitor_HasCreatedAt_SetToUtcNow()
    {
        var before = DateTime.UtcNow;
        var monitor = new Monitor();
        var after = DateTime.UtcNow;

        Assert.InRange(monitor.CreatedAt, before, after);
    }

    [Fact]
    public void NewMonitor_HasUpdatedAt_SetToUtcNow()
    {
        var before = DateTime.UtcNow;
        var monitor = new Monitor();
        var after = DateTime.UtcNow;

        Assert.InRange(monitor.UpdatedAt, before, after);
    }

    [Fact]
    public void Monitor_CanSetProperties()
    {
        var id = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var lastCheckIn = DateTime.UtcNow;

        var monitor = new Monitor
        {
            Id = id,
            ServiceId = serviceId,
            Type = MonitorType.Heartbeat,
            Token = "test-token-123",
            IntervalSeconds = 60,
            GracePeriodSeconds = 30,
            ConfigJson = "{\"url\":\"http://example.com\"}",
            LastCheckIn = lastCheckIn
        };

        Assert.Equal(id, monitor.Id);
        Assert.Equal(serviceId, monitor.ServiceId);
        Assert.Equal(MonitorType.Heartbeat, monitor.Type);
        Assert.Equal("test-token-123", monitor.Token);
        Assert.Equal(60, monitor.IntervalSeconds);
        Assert.Equal(30, monitor.GracePeriodSeconds);
        Assert.Equal("{\"url\":\"http://example.com\"}", monitor.ConfigJson);
        Assert.Equal(lastCheckIn, monitor.LastCheckIn);
    }

    [Fact]
    public void Monitor_DefaultIntervalSeconds_IsZero()
    {
        var monitor = new Monitor();
        Assert.Equal(0, monitor.IntervalSeconds);
    }

    [Fact]
    public void Monitor_DefaultGracePeriodSeconds_IsZero()
    {
        var monitor = new Monitor();
        Assert.Equal(0, monitor.GracePeriodSeconds);
    }
}
