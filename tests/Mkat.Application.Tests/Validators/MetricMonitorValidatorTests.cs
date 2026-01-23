using Mkat.Application.DTOs;
using Mkat.Application.Validators;
using Mkat.Domain.Enums;
using Xunit;

namespace Mkat.Application.Tests.Validators;

public class MetricMonitorValidatorTests
{
    private readonly AddMonitorValidator _addValidator = new();
    private readonly UpdateMonitorValidator _updateValidator = new();

    private static AddMonitorRequest ValidMetricRequest(
        double? minValue = null,
        double? maxValue = 100.0,
        ThresholdStrategy strategy = ThresholdStrategy.Immediate,
        int? thresholdCount = null,
        int? windowSeconds = null,
        int? windowSampleCount = null,
        int retentionDays = 7)
    {
        return new AddMonitorRequest
        {
            Type = MonitorType.Metric,
            IntervalSeconds = 60,
            MinValue = minValue,
            MaxValue = maxValue,
            ThresholdStrategy = strategy,
            ThresholdCount = thresholdCount,
            WindowSeconds = windowSeconds,
            WindowSampleCount = windowSampleCount,
            RetentionDays = retentionDays
        };
    }

    // --- Range bound validation ---

    [Fact]
    public async Task Metric_NoBoundsSet_Fails()
    {
        var request = ValidMetricRequest(minValue: null, maxValue: null);
        var result = await _addValidator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Metric_OnlyMinSet_Passes()
    {
        var request = ValidMetricRequest(minValue: 10.0, maxValue: null);
        var result = await _addValidator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Metric_OnlyMaxSet_Passes()
    {
        var request = ValidMetricRequest(minValue: null, maxValue: 100.0);
        var result = await _addValidator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Metric_BothBoundsSet_MinLessThanMax_Passes()
    {
        var request = ValidMetricRequest(minValue: 10.0, maxValue: 100.0);
        var result = await _addValidator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Metric_MinGreaterThanMax_Fails()
    {
        var request = ValidMetricRequest(minValue: 100.0, maxValue: 10.0);
        var result = await _addValidator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Metric_MinEqualToMax_Fails()
    {
        var request = ValidMetricRequest(minValue: 50.0, maxValue: 50.0);
        var result = await _addValidator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    // --- ThresholdCount for ConsecutiveCount ---

    [Fact]
    public async Task Metric_ConsecutiveCount_WithThresholdCount_Passes()
    {
        var request = ValidMetricRequest(
            strategy: ThresholdStrategy.ConsecutiveCount,
            thresholdCount: 3);
        var result = await _addValidator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Metric_ConsecutiveCount_NoThresholdCount_Fails()
    {
        var request = ValidMetricRequest(
            strategy: ThresholdStrategy.ConsecutiveCount,
            thresholdCount: null);
        var result = await _addValidator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Metric_ConsecutiveCount_ThresholdCountZero_Fails()
    {
        var request = ValidMetricRequest(
            strategy: ThresholdStrategy.ConsecutiveCount,
            thresholdCount: 0);
        var result = await _addValidator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    // --- WindowSeconds for TimeDurationAverage ---

    [Fact]
    public async Task Metric_TimeDurationAverage_WithWindowSeconds_Passes()
    {
        var request = ValidMetricRequest(
            strategy: ThresholdStrategy.TimeDurationAverage,
            windowSeconds: 60);
        var result = await _addValidator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Metric_TimeDurationAverage_NoWindowSeconds_Fails()
    {
        var request = ValidMetricRequest(
            strategy: ThresholdStrategy.TimeDurationAverage,
            windowSeconds: null);
        var result = await _addValidator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Metric_TimeDurationAverage_WindowSecondsZero_Fails()
    {
        var request = ValidMetricRequest(
            strategy: ThresholdStrategy.TimeDurationAverage,
            windowSeconds: 0);
        var result = await _addValidator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    // --- WindowSampleCount for SampleCountAverage ---

    [Fact]
    public async Task Metric_SampleCountAverage_WithWindowSampleCount_Passes()
    {
        var request = ValidMetricRequest(
            strategy: ThresholdStrategy.SampleCountAverage,
            windowSampleCount: 5);
        var result = await _addValidator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Metric_SampleCountAverage_NoWindowSampleCount_Fails()
    {
        var request = ValidMetricRequest(
            strategy: ThresholdStrategy.SampleCountAverage,
            windowSampleCount: null);
        var result = await _addValidator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Metric_SampleCountAverage_WindowSampleCountZero_Fails()
    {
        var request = ValidMetricRequest(
            strategy: ThresholdStrategy.SampleCountAverage,
            windowSampleCount: 0);
        var result = await _addValidator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    // --- RetentionDays ---

    [Fact]
    public async Task Metric_RetentionDays_Valid_Passes()
    {
        var request = ValidMetricRequest(retentionDays: 30);
        var result = await _addValidator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Metric_RetentionDays_Zero_Fails()
    {
        var request = ValidMetricRequest(retentionDays: 0);
        var result = await _addValidator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Metric_RetentionDays_Above365_Fails()
    {
        var request = ValidMetricRequest(retentionDays: 366);
        var result = await _addValidator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Metric_RetentionDays_AtBoundaries_Passes()
    {
        var request1 = ValidMetricRequest(retentionDays: 1);
        var result1 = await _addValidator.ValidateAsync(request1);
        Assert.True(result1.IsValid);

        var request365 = ValidMetricRequest(retentionDays: 365);
        var result365 = await _addValidator.ValidateAsync(request365);
        Assert.True(result365.IsValid);
    }

    // --- Non-Metric types ignore metric validation ---

    [Fact]
    public async Task NonMetric_NoBounds_Passes()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.Heartbeat,
            IntervalSeconds = 60
        };
        var result = await _addValidator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }

    // --- UpdateMonitorRequest metric validation ---

    [Fact]
    public async Task Update_Metric_NoBounds_Fails()
    {
        var request = new UpdateMonitorRequest
        {
            IntervalSeconds = 60,
            Type = MonitorType.Metric,
            MinValue = null,
            MaxValue = null
        };
        var result = await _updateValidator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Update_Metric_ValidConfig_Passes()
    {
        var request = new UpdateMonitorRequest
        {
            IntervalSeconds = 60,
            Type = MonitorType.Metric,
            MaxValue = 100.0,
            ThresholdStrategy = ThresholdStrategy.Immediate,
            RetentionDays = 7
        };
        var result = await _updateValidator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }
}
