using Mkat.Application.Services;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Xunit;

namespace Mkat.Application.Tests.Services;

public class RollupCalculatorTests
{
    private readonly RollupCalculator _calculator = new();

    private static MonitorEvent CreateEvent(bool success, double? value = null)
    {
        return new MonitorEvent
        {
            Id = Guid.NewGuid(),
            MonitorId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            EventType = EventType.HealthCheckPerformed,
            Success = success,
            Value = value,
            CreatedAt = DateTime.UtcNow
        };
    }

    // --- Empty list ---

    [Fact]
    public void Compute_EmptyList_ReturnsRollupWithZeroCounts()
    {
        var monitorId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var periodStart = DateTime.UtcNow;

        var rollup = _calculator.Compute(
            new List<MonitorEvent>(), monitorId, serviceId,
            Granularity.Hourly, periodStart);

        Assert.Equal(monitorId, rollup.MonitorId);
        Assert.Equal(serviceId, rollup.ServiceId);
        Assert.Equal(Granularity.Hourly, rollup.Granularity);
        Assert.Equal(periodStart, rollup.PeriodStart);
        Assert.Equal(0, rollup.Count);
        Assert.Equal(0, rollup.SuccessCount);
        Assert.Equal(0, rollup.FailureCount);
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

    // --- Single event ---

    [Fact]
    public void Compute_SingleSuccessEvent_CorrectCounts()
    {
        var events = new List<MonitorEvent> { CreateEvent(true, 100.0) };
        var rollup = _calculator.Compute(events, Guid.NewGuid(), Guid.NewGuid(),
            Granularity.Hourly, DateTime.UtcNow);

        Assert.Equal(1, rollup.Count);
        Assert.Equal(1, rollup.SuccessCount);
        Assert.Equal(0, rollup.FailureCount);
    }

    [Fact]
    public void Compute_SingleFailureEvent_CorrectCounts()
    {
        var events = new List<MonitorEvent> { CreateEvent(false, 50.0) };
        var rollup = _calculator.Compute(events, Guid.NewGuid(), Guid.NewGuid(),
            Granularity.Hourly, DateTime.UtcNow);

        Assert.Equal(1, rollup.Count);
        Assert.Equal(0, rollup.SuccessCount);
        Assert.Equal(1, rollup.FailureCount);
    }

    [Fact]
    public void Compute_SingleEventWithValue_MinMaxMeanMedianAllEqual()
    {
        var events = new List<MonitorEvent> { CreateEvent(true, 42.0) };
        var rollup = _calculator.Compute(events, Guid.NewGuid(), Guid.NewGuid(),
            Granularity.Hourly, DateTime.UtcNow);

        Assert.Equal(42.0, rollup.Min);
        Assert.Equal(42.0, rollup.Max);
        Assert.Equal(42.0, rollup.Mean);
        Assert.Equal(42.0, rollup.Median);
        Assert.Equal(42.0, rollup.P80);
        Assert.Equal(42.0, rollup.P90);
        Assert.Equal(42.0, rollup.P95);
        Assert.Equal(0.0, rollup.StdDev);
    }

    // --- Uptime ---

    [Fact]
    public void Compute_AllSuccess_UptimeIs100()
    {
        var events = new List<MonitorEvent>
        {
            CreateEvent(true, 10.0),
            CreateEvent(true, 20.0),
            CreateEvent(true, 30.0)
        };
        var rollup = _calculator.Compute(events, Guid.NewGuid(), Guid.NewGuid(),
            Granularity.Hourly, DateTime.UtcNow);

        Assert.Equal(100.0, rollup.UptimePercent);
    }

    [Fact]
    public void Compute_MixedResults_CorrectUptime()
    {
        var events = new List<MonitorEvent>
        {
            CreateEvent(true, 10.0),
            CreateEvent(true, 20.0),
            CreateEvent(false, 30.0),
            CreateEvent(true, 40.0)
        };
        var rollup = _calculator.Compute(events, Guid.NewGuid(), Guid.NewGuid(),
            Granularity.Hourly, DateTime.UtcNow);

        Assert.Equal(75.0, rollup.UptimePercent);
    }

    // --- Statistics with multiple values ---

    [Fact]
    public void Compute_MultipleValues_CorrectMinMaxMean()
    {
        var events = new List<MonitorEvent>
        {
            CreateEvent(true, 10.0),
            CreateEvent(true, 20.0),
            CreateEvent(true, 30.0),
            CreateEvent(true, 40.0),
            CreateEvent(true, 50.0)
        };
        var rollup = _calculator.Compute(events, Guid.NewGuid(), Guid.NewGuid(),
            Granularity.Hourly, DateTime.UtcNow);

        Assert.Equal(10.0, rollup.Min);
        Assert.Equal(50.0, rollup.Max);
        Assert.Equal(30.0, rollup.Mean);
    }

    [Fact]
    public void Compute_OddCount_CorrectMedian()
    {
        var events = new List<MonitorEvent>
        {
            CreateEvent(true, 10.0),
            CreateEvent(true, 20.0),
            CreateEvent(true, 30.0)
        };
        var rollup = _calculator.Compute(events, Guid.NewGuid(), Guid.NewGuid(),
            Granularity.Hourly, DateTime.UtcNow);

        Assert.Equal(20.0, rollup.Median);
    }

    [Fact]
    public void Compute_EvenCount_MedianIsAverage()
    {
        var events = new List<MonitorEvent>
        {
            CreateEvent(true, 10.0),
            CreateEvent(true, 20.0),
            CreateEvent(true, 30.0),
            CreateEvent(true, 40.0)
        };
        var rollup = _calculator.Compute(events, Guid.NewGuid(), Guid.NewGuid(),
            Granularity.Hourly, DateTime.UtcNow);

        Assert.Equal(25.0, rollup.Median);
    }

    // --- Percentiles ---

    [Fact]
    public void Compute_TenValues_CorrectPercentiles()
    {
        // Values: 1,2,3,4,5,6,7,8,9,10
        var events = Enumerable.Range(1, 10)
            .Select(i => CreateEvent(true, i))
            .ToList();
        var rollup = _calculator.Compute(events, Guid.NewGuid(), Guid.NewGuid(),
            Granularity.Hourly, DateTime.UtcNow);

        // P80 = value at 80th percentile of [1..10]
        Assert.NotNull(rollup.P80);
        Assert.NotNull(rollup.P90);
        Assert.NotNull(rollup.P95);
        Assert.True(rollup.P80!.Value >= 8.0);
        Assert.True(rollup.P90!.Value >= 9.0);
        Assert.True(rollup.P95!.Value >= 9.0);
    }

    // --- StdDev ---

    [Fact]
    public void Compute_IdenticalValues_StdDevIsZero()
    {
        var events = new List<MonitorEvent>
        {
            CreateEvent(true, 42.0),
            CreateEvent(true, 42.0),
            CreateEvent(true, 42.0)
        };
        var rollup = _calculator.Compute(events, Guid.NewGuid(), Guid.NewGuid(),
            Granularity.Hourly, DateTime.UtcNow);

        Assert.Equal(0.0, rollup.StdDev);
    }

    [Fact]
    public void Compute_KnownValues_CorrectStdDev()
    {
        // Values: 2, 4, 4, 4, 5, 5, 7, 9
        // Mean = 5, Population StdDev ≈ 2.0
        var values = new[] { 2.0, 4.0, 4.0, 4.0, 5.0, 5.0, 7.0, 9.0 };
        var events = values.Select(v => CreateEvent(true, v)).ToList();
        var rollup = _calculator.Compute(events, Guid.NewGuid(), Guid.NewGuid(),
            Granularity.Hourly, DateTime.UtcNow);

        Assert.NotNull(rollup.StdDev);
        Assert.InRange(rollup.StdDev!.Value, 1.9, 2.1);
    }

    // --- Events without values ---

    [Fact]
    public void Compute_EventsWithoutValues_StatisticsAreNull()
    {
        var events = new List<MonitorEvent>
        {
            CreateEvent(true),
            CreateEvent(false)
        };
        var rollup = _calculator.Compute(events, Guid.NewGuid(), Guid.NewGuid(),
            Granularity.Hourly, DateTime.UtcNow);

        Assert.Equal(2, rollup.Count);
        Assert.Equal(1, rollup.SuccessCount);
        Assert.Equal(1, rollup.FailureCount);
        Assert.Null(rollup.Min);
        Assert.Null(rollup.Max);
        Assert.Null(rollup.Mean);
        Assert.Null(rollup.Median);
        Assert.Null(rollup.StdDev);
        Assert.Equal(50.0, rollup.UptimePercent);
    }
}
