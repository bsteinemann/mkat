using Mkat.Domain.Entities;
using Xunit;
using Monitor = Mkat.Domain.Entities.Monitor;

namespace Mkat.Domain.Tests.Entities;

public class MetricReadingTests
{
    [Fact]
    public void NewMetricReading_HasDefaultId()
    {
        var reading = new MetricReading();
        Assert.Equal(Guid.Empty, reading.Id);
    }

    [Fact]
    public void NewMetricReading_HasDefaultMonitorId()
    {
        var reading = new MetricReading();
        Assert.Equal(Guid.Empty, reading.MonitorId);
    }

    [Fact]
    public void NewMetricReading_HasDefaultValue()
    {
        var reading = new MetricReading();
        Assert.Equal(0.0, reading.Value);
    }

    [Fact]
    public void NewMetricReading_HasDefaultRecordedAt()
    {
        var reading = new MetricReading();
        Assert.Equal(default(DateTime), reading.RecordedAt);
    }

    [Fact]
    public void NewMetricReading_IsOutOfRange_IsFalse()
    {
        var reading = new MetricReading();
        Assert.False(reading.IsOutOfRange);
    }

    [Fact]
    public void MetricReading_CanSetProperties()
    {
        var id = Guid.NewGuid();
        var monitorId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var reading = new MetricReading
        {
            Id = id,
            MonitorId = monitorId,
            Value = 42.5,
            RecordedAt = now,
            IsOutOfRange = true
        };

        Assert.Equal(id, reading.Id);
        Assert.Equal(monitorId, reading.MonitorId);
        Assert.Equal(42.5, reading.Value);
        Assert.Equal(now, reading.RecordedAt);
        Assert.True(reading.IsOutOfRange);
    }

    [Fact]
    public void MetricReading_HasNavigationProperty_Monitor()
    {
        var monitor = new Monitor { Id = Guid.NewGuid() };
        var reading = new MetricReading
        {
            MonitorId = monitor.Id,
            Monitor = monitor
        };

        Assert.Same(monitor, reading.Monitor);
    }
}
