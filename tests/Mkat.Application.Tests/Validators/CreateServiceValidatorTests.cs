using Mkat.Application.DTOs;
using Mkat.Application.Validators;
using Mkat.Domain.Enums;
using Xunit;

namespace Mkat.Application.Tests.Validators;

public class CreateServiceValidatorTests
{
    private readonly CreateServiceValidator _validator = new();

    [Fact]
    public async Task ValidRequest_Passes()
    {
        var request = new CreateServiceRequest
        {
            Name = "My Service",
            Description = "A test service",
            Severity = Severity.Medium,
            Monitors = new List<CreateMonitorRequest>
            {
                new() { Type = MonitorType.Heartbeat, IntervalSeconds = 300 }
            }
        };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task EmptyName_Fails()
    {
        var request = new CreateServiceRequest
        {
            Name = "",
            Monitors = new List<CreateMonitorRequest>
            {
                new() { Type = MonitorType.Heartbeat, IntervalSeconds = 300 }
            }
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task NameTooLong_Fails()
    {
        var request = new CreateServiceRequest
        {
            Name = new string('a', 101),
            Monitors = new List<CreateMonitorRequest>
            {
                new() { Type = MonitorType.Heartbeat, IntervalSeconds = 300 }
            }
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task DescriptionTooLong_Fails()
    {
        var request = new CreateServiceRequest
        {
            Name = "Valid Name",
            Description = new string('a', 501),
            Monitors = new List<CreateMonitorRequest>
            {
                new() { Type = MonitorType.Heartbeat, IntervalSeconds = 300 }
            }
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task EmptyMonitors_Fails()
    {
        var request = new CreateServiceRequest
        {
            Name = "Valid Name",
            Monitors = new List<CreateMonitorRequest>()
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Monitors");
    }

    [Fact]
    public async Task MonitorIntervalTooShort_Fails()
    {
        var request = new CreateServiceRequest
        {
            Name = "Valid Name",
            Monitors = new List<CreateMonitorRequest>
            {
                new() { Type = MonitorType.Heartbeat, IntervalSeconds = 10 }
            }
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("IntervalSeconds"));
    }

    [Fact]
    public async Task MonitorIntervalTooLong_Fails()
    {
        var request = new CreateServiceRequest
        {
            Name = "Valid Name",
            Monitors = new List<CreateMonitorRequest>
            {
                new() { Type = MonitorType.Heartbeat, IntervalSeconds = 700000 }
            }
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("IntervalSeconds"));
    }

    [Fact]
    public async Task HealthCheckMonitorType_Fails()
    {
        var request = new CreateServiceRequest
        {
            Name = "Valid Name",
            Monitors = new List<CreateMonitorRequest>
            {
                new() { Type = MonitorType.HealthCheck, IntervalSeconds = 300 }
            }
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Type"));
    }

    [Fact]
    public async Task GracePeriodTooShort_Fails()
    {
        var request = new CreateServiceRequest
        {
            Name = "Valid Name",
            Monitors = new List<CreateMonitorRequest>
            {
                new() { Type = MonitorType.Heartbeat, IntervalSeconds = 300, GracePeriodSeconds = 30 }
            }
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("GracePeriodSeconds"));
    }

    [Fact]
    public async Task GracePeriodNull_Passes()
    {
        var request = new CreateServiceRequest
        {
            Name = "Valid Name",
            Monitors = new List<CreateMonitorRequest>
            {
                new() { Type = MonitorType.Heartbeat, IntervalSeconds = 300, GracePeriodSeconds = null }
            }
        };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }
}
