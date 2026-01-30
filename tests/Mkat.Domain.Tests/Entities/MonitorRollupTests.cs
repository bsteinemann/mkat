using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Xunit;

namespace Mkat.Domain.Tests.Entities;

public class MonitorRollupTests
{
    [Fact]
    public void NewMonitorRollup_HasDefaultId()
    {
        var rollup = new MonitorRollup();
        Assert.Equal(Guid.Empty, rollup.Id);
    }

    [Fact]
    public void NewMonitorRollup_HasDefaultMonitorId()
    {
        var rollup = new MonitorRollup();
        Assert.Equal(Guid.Empty, rollup.MonitorId);
    }

    [Fact]
    public void NewMonitorRollup_HasDefaultServiceId()
    {
        var rollup = new MonitorRollup();
        Assert.Equal(Guid.Empty, rollup.ServiceId);
    }

    [Fact]
    public void NewMonitorRollup_HasDefaultCount_Zero()
    {
        var rollup = new MonitorRollup();
        Assert.Equal(0, rollup.Count);
    }

    [Fact]
    public void NewMonitorRollup_HasDefaultSuccessCount_Zero()
    {
        var rollup = new MonitorRollup();
        Assert.Equal(0, rollup.SuccessCount);
    }

    [Fact]
    public void NewMonitorRollup_HasDefaultFailureCount_Zero()
    {
        var rollup = new MonitorRollup();
        Assert.Equal(0, rollup.FailureCount);
    }

    [Fact]
    public void NewMonitorRollup_HasNullStatistics()
    {
        var rollup = new MonitorRollup();
        Assert.Null(rollup.Min);
        Assert.Null(rollup.Max);
        Assert.Null(rollup.Mean);
        Assert.Null(rollup.Median);
        Assert.Null(rollup.P80);
        Assert.Null(rollup.P90);
        Assert.Null(rollup.P95);
        Assert.Null(rollup.StdDev);
        Assert.Null(rollup.UptimePercent);
    }

    [Fact]
    public void MonitorRollup_CanSetAllProperties()
    {
        var id = Guid.NewGuid();
        var monitorId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var periodStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var rollup = new MonitorRollup
        {
            Id = id,
            MonitorId = monitorId,
            ServiceId = serviceId,
            Granularity = Granularity.Hourly,
            PeriodStart = periodStart,
            Count = 60,
            SuccessCount = 58,
            FailureCount = 2,
            Min = 10.0,
            Max = 500.0,
            Mean = 120.5,
            Median = 100.0,
            P80 = 200.0,
            P90 = 350.0,
            P95 = 450.0,
            StdDev = 80.3,
            UptimePercent = 96.67
        };

        Assert.Equal(id, rollup.Id);
        Assert.Equal(monitorId, rollup.MonitorId);
        Assert.Equal(serviceId, rollup.ServiceId);
        Assert.Equal(Granularity.Hourly, rollup.Granularity);
        Assert.Equal(periodStart, rollup.PeriodStart);
        Assert.Equal(60, rollup.Count);
        Assert.Equal(58, rollup.SuccessCount);
        Assert.Equal(2, rollup.FailureCount);
        Assert.Equal(10.0, rollup.Min);
        Assert.Equal(500.0, rollup.Max);
        Assert.Equal(120.5, rollup.Mean);
        Assert.Equal(100.0, rollup.Median);
        Assert.Equal(200.0, rollup.P80);
        Assert.Equal(350.0, rollup.P90);
        Assert.Equal(450.0, rollup.P95);
        Assert.Equal(80.3, rollup.StdDev);
        Assert.Equal(96.67, rollup.UptimePercent);
    }

    [Theory]
    [InlineData(Granularity.Hourly)]
    [InlineData(Granularity.Daily)]
    [InlineData(Granularity.Weekly)]
    [InlineData(Granularity.Monthly)]
    public void MonitorRollup_SupportsAllGranularities(Granularity granularity)
    {
        var rollup = new MonitorRollup { Granularity = granularity };
        Assert.Equal(granularity, rollup.Granularity);
    }
}
