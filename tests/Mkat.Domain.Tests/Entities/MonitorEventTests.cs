using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Xunit;

namespace Mkat.Domain.Tests.Entities;

public class MonitorEventTests
{
    [Fact]
    public void NewMonitorEvent_HasDefaultId()
    {
        var evt = new MonitorEvent();
        Assert.Equal(Guid.Empty, evt.Id);
    }

    [Fact]
    public void NewMonitorEvent_HasDefaultMonitorId()
    {
        var evt = new MonitorEvent();
        Assert.Equal(Guid.Empty, evt.MonitorId);
    }

    [Fact]
    public void NewMonitorEvent_HasDefaultServiceId()
    {
        var evt = new MonitorEvent();
        Assert.Equal(Guid.Empty, evt.ServiceId);
    }

    [Fact]
    public void NewMonitorEvent_HasDefaultSuccess_False()
    {
        var evt = new MonitorEvent();
        Assert.False(evt.Success);
    }

    [Fact]
    public void NewMonitorEvent_HasNullValue()
    {
        var evt = new MonitorEvent();
        Assert.Null(evt.Value);
    }

    [Fact]
    public void NewMonitorEvent_HasDefaultIsOutOfRange_False()
    {
        var evt = new MonitorEvent();
        Assert.False(evt.IsOutOfRange);
    }

    [Fact]
    public void NewMonitorEvent_HasNullMessage()
    {
        var evt = new MonitorEvent();
        Assert.Null(evt.Message);
    }

    [Fact]
    public void NewMonitorEvent_HasCreatedAt_SetToUtcNow()
    {
        var before = DateTime.UtcNow;
        var evt = new MonitorEvent();
        var after = DateTime.UtcNow;

        Assert.InRange(evt.CreatedAt, before, after);
    }

    [Fact]
    public void MonitorEvent_CanSetAllProperties()
    {
        var id = Guid.NewGuid();
        var monitorId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var evt = new MonitorEvent
        {
            Id = id,
            MonitorId = monitorId,
            ServiceId = serviceId,
            EventType = EventType.HealthCheckPerformed,
            Success = true,
            Value = 123.45,
            IsOutOfRange = true,
            Message = "Health check passed",
            CreatedAt = now
        };

        Assert.Equal(id, evt.Id);
        Assert.Equal(monitorId, evt.MonitorId);
        Assert.Equal(serviceId, evt.ServiceId);
        Assert.Equal(EventType.HealthCheckPerformed, evt.EventType);
        Assert.True(evt.Success);
        Assert.Equal(123.45, evt.Value);
        Assert.True(evt.IsOutOfRange);
        Assert.Equal("Health check passed", evt.Message);
        Assert.Equal(now, evt.CreatedAt);
    }

    [Theory]
    [InlineData(EventType.WebhookReceived)]
    [InlineData(EventType.HeartbeatReceived)]
    [InlineData(EventType.HealthCheckPerformed)]
    [InlineData(EventType.MetricIngested)]
    [InlineData(EventType.StateChanged)]
    public void MonitorEvent_SupportsAllEventTypes(EventType eventType)
    {
        var evt = new MonitorEvent { EventType = eventType };
        Assert.Equal(eventType, evt.EventType);
    }
}
