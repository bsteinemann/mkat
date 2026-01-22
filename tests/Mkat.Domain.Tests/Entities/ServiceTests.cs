using Mkat.Domain.Entities;
using Mkat.Domain.Enums;

namespace Mkat.Domain.Tests.Entities;

public class ServiceTests
{
    [Fact]
    public void NewService_HasDefaultState_Unknown()
    {
        var service = new Service();
        Assert.Equal(ServiceState.Unknown, service.State);
    }

    [Fact]
    public void NewService_HasDefaultSeverity_Medium()
    {
        var service = new Service();
        Assert.Equal(Severity.Medium, service.Severity);
    }

    [Fact]
    public void NewService_HasEmptyName()
    {
        var service = new Service();
        Assert.Equal(string.Empty, service.Name);
    }

    [Fact]
    public void NewService_HasNullDescription()
    {
        var service = new Service();
        Assert.Null(service.Description);
    }

    [Fact]
    public void NewService_HasNullPreviousState()
    {
        var service = new Service();
        Assert.Null(service.PreviousState);
    }

    [Fact]
    public void NewService_HasNullPausedUntil()
    {
        var service = new Service();
        Assert.Null(service.PausedUntil);
    }

    [Fact]
    public void NewService_HasAutoResume_False()
    {
        var service = new Service();
        Assert.False(service.AutoResume);
    }

    [Fact]
    public void NewService_HasCreatedAt_SetToUtcNow()
    {
        var before = DateTime.UtcNow;
        var service = new Service();
        var after = DateTime.UtcNow;

        Assert.InRange(service.CreatedAt, before, after);
    }

    [Fact]
    public void NewService_HasUpdatedAt_SetToUtcNow()
    {
        var before = DateTime.UtcNow;
        var service = new Service();
        var after = DateTime.UtcNow;

        Assert.InRange(service.UpdatedAt, before, after);
    }

    [Fact]
    public void NewService_HasEmptyMonitorsCollection()
    {
        var service = new Service();
        Assert.Empty(service.Monitors);
    }

    [Fact]
    public void NewService_HasEmptyAlertsCollection()
    {
        var service = new Service();
        Assert.Empty(service.Alerts);
    }

    [Fact]
    public void NewService_HasEmptyMuteWindowsCollection()
    {
        var service = new Service();
        Assert.Empty(service.MuteWindows);
    }

    [Fact]
    public void Service_CanSetProperties()
    {
        var id = Guid.NewGuid();
        var service = new Service
        {
            Id = id,
            Name = "Test Service",
            Description = "A test service",
            State = ServiceState.Up,
            PreviousState = ServiceState.Unknown,
            Severity = Severity.High,
            AutoResume = true
        };

        Assert.Equal(id, service.Id);
        Assert.Equal("Test Service", service.Name);
        Assert.Equal("A test service", service.Description);
        Assert.Equal(ServiceState.Up, service.State);
        Assert.Equal(ServiceState.Unknown, service.PreviousState);
        Assert.Equal(Severity.High, service.Severity);
        Assert.True(service.AutoResume);
    }
}
