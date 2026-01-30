using Mkat.Application.Services;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Xunit;
using Monitor = Mkat.Domain.Entities.Monitor;

namespace Mkat.Application.Tests.Services;

public class MetricReadingMigratorTests
{
    [Fact]
    public void Convert_EmptyList_ReturnsEmptyList()
    {
        var result = MetricReadingMigrator.Convert(new List<MetricReading>());
        Assert.Empty(result);
    }

    [Fact]
    public void Convert_SingleReading_MapsFieldsCorrectly()
    {
        var monitorId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var recordedAt = DateTime.UtcNow;

        var reading = new MetricReading
        {
            Id = Guid.NewGuid(),
            MonitorId = monitorId,
            Value = 42.5,
            RecordedAt = recordedAt,
            IsOutOfRange = true,
            Monitor = new Monitor
            {
                Id = monitorId,
                ServiceId = serviceId,
                Type = MonitorType.Metric,
                Token = "test"
            }
        };

        var result = MetricReadingMigrator.Convert(new List<MetricReading> { reading });

        Assert.Single(result);
        var evt = result[0];
        Assert.Equal(monitorId, evt.MonitorId);
        Assert.Equal(serviceId, evt.ServiceId);
        Assert.Equal(EventType.MetricIngested, evt.EventType);
        Assert.Equal(42.5, evt.Value);
        Assert.True(evt.IsOutOfRange);
        Assert.Equal(recordedAt, evt.CreatedAt);
        Assert.False(evt.Success); // IsOutOfRange = true means failure
    }

    [Fact]
    public void Convert_InRangeReading_SuccessIsTrue()
    {
        var monitorId = Guid.NewGuid();
        var reading = new MetricReading
        {
            Id = Guid.NewGuid(),
            MonitorId = monitorId,
            Value = 10.0,
            RecordedAt = DateTime.UtcNow,
            IsOutOfRange = false,
            Monitor = new Monitor
            {
                Id = monitorId,
                ServiceId = Guid.NewGuid(),
                Type = MonitorType.Metric,
                Token = "test"
            }
        };

        var result = MetricReadingMigrator.Convert(new List<MetricReading> { reading });

        Assert.Single(result);
        Assert.True(result[0].Success);
    }

    [Fact]
    public void Convert_MultipleReadings_ConvertsAll()
    {
        var monitorId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var monitor = new Monitor
        {
            Id = monitorId,
            ServiceId = serviceId,
            Type = MonitorType.Metric,
            Token = "test"
        };

        var readings = new List<MetricReading>
        {
            new() { Id = Guid.NewGuid(), MonitorId = monitorId, Value = 1.0, RecordedAt = DateTime.UtcNow, Monitor = monitor },
            new() { Id = Guid.NewGuid(), MonitorId = monitorId, Value = 2.0, RecordedAt = DateTime.UtcNow, Monitor = monitor },
            new() { Id = Guid.NewGuid(), MonitorId = monitorId, Value = 3.0, RecordedAt = DateTime.UtcNow, Monitor = monitor }
        };

        var result = MetricReadingMigrator.Convert(readings);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Convert_GeneratesNewIds()
    {
        var monitorId = Guid.NewGuid();
        var reading = new MetricReading
        {
            Id = Guid.NewGuid(),
            MonitorId = monitorId,
            Value = 10.0,
            RecordedAt = DateTime.UtcNow,
            Monitor = new Monitor
            {
                Id = monitorId,
                ServiceId = Guid.NewGuid(),
                Type = MonitorType.Metric,
                Token = "test"
            }
        };

        var result = MetricReadingMigrator.Convert(new List<MetricReading> { reading });

        Assert.NotEqual(Guid.Empty, result[0].Id);
        Assert.NotEqual(reading.Id, result[0].Id);
    }
}
