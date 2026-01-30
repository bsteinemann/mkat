using Mkat.Application.DTOs;
using Mkat.Application.Validators;
using Mkat.Domain.Enums;
using Xunit;

namespace Mkat.Application.Tests.Validators;

public class AddMonitorValidatorTests
{
    private readonly AddMonitorValidator _validator = new();

    [Fact]
    public async Task ValidRequest_Passes()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.Heartbeat,
            IntervalSeconds = 300,
            GracePeriodSeconds = 60
        };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task IntervalBelowMinimum_Fails()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.Heartbeat,
            IntervalSeconds = 29
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "IntervalSeconds");
    }

    [Fact]
    public async Task IntervalAtMinimum_Passes()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.Heartbeat,
            IntervalSeconds = 30
        };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task IntervalAboveMaximum_Fails()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.Heartbeat,
            IntervalSeconds = 604801
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "IntervalSeconds");
    }

    [Fact]
    public async Task IntervalAtMaximum_Passes()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.Heartbeat,
            IntervalSeconds = 604800
        };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GracePeriodBelowMinimum_Fails()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.Heartbeat,
            IntervalSeconds = 300,
            GracePeriodSeconds = 59
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "GracePeriodSeconds");
    }

    [Fact]
    public async Task GracePeriodNull_Passes()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.Heartbeat,
            IntervalSeconds = 300,
            GracePeriodSeconds = null
        };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task HealthCheckType_WithoutUrl_Fails()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.HealthCheck,
            IntervalSeconds = 300
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "HealthCheckUrl");
    }

    [Fact]
    public async Task HealthCheck_WithValidUrl_Passes()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.HealthCheck,
            IntervalSeconds = 60,
            HealthCheckUrl = "https://example.com/health"
        };
        var result = await _validator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task HealthCheck_WithInvalidUrl_Fails()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.HealthCheck,
            IntervalSeconds = 60,
            HealthCheckUrl = "not-a-url"
        };
        var result = await _validator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task HealthCheck_WithInvalidHttpMethod_Fails()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.HealthCheck,
            IntervalSeconds = 60,
            HealthCheckUrl = "https://example.com/health",
            HttpMethod = "DELETE"
        };
        var result = await _validator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task HealthCheck_WithInvalidTimeout_Fails()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.HealthCheck,
            IntervalSeconds = 60,
            HealthCheckUrl = "https://example.com/health",
            TimeoutSeconds = 0
        };
        var result = await _validator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task HealthCheck_WithInvalidRegex_Fails()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.HealthCheck,
            IntervalSeconds = 60,
            HealthCheckUrl = "https://example.com/health",
            BodyMatchRegex = "[invalid"
        };
        var result = await _validator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task HealthCheck_WithInvalidStatusCodes_Fails()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.HealthCheck,
            IntervalSeconds = 60,
            HealthCheckUrl = "https://example.com/health",
            ExpectedStatusCodes = "abc,def"
        };
        var result = await _validator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task WebhookType_Passes()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.Webhook,
            IntervalSeconds = 300
        };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }
}

public class UpdateMonitorValidatorTests
{
    private readonly UpdateMonitorValidator _validator = new();

    [Fact]
    public async Task ValidRequest_Passes()
    {
        var request = new UpdateMonitorRequest
        {
            IntervalSeconds = 300,
            GracePeriodSeconds = 60
        };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task IntervalBelowMinimum_Fails()
    {
        var request = new UpdateMonitorRequest
        {
            IntervalSeconds = 29
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "IntervalSeconds");
    }

    [Fact]
    public async Task IntervalAboveMaximum_Fails()
    {
        var request = new UpdateMonitorRequest
        {
            IntervalSeconds = 604801
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "IntervalSeconds");
    }

    [Fact]
    public async Task GracePeriodBelowMinimum_Fails()
    {
        var request = new UpdateMonitorRequest
        {
            IntervalSeconds = 300,
            GracePeriodSeconds = 59
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "GracePeriodSeconds");
    }

    [Fact]
    public async Task GracePeriodNull_Passes()
    {
        var request = new UpdateMonitorRequest
        {
            IntervalSeconds = 300,
            GracePeriodSeconds = null
        };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }
}
