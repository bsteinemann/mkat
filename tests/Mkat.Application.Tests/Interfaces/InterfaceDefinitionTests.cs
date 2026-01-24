using Mkat.Application.DTOs;
using Mkat.Application.Interfaces;
using Xunit;

namespace Mkat.Application.Tests.Interfaces;

public class InterfaceDefinitionTests
{
    [Fact]
    public void IServiceRepository_IsInterface()
    {
        Assert.True(typeof(IServiceRepository).IsInterface);
    }

    [Fact]
    public void IMonitorRepository_IsInterface()
    {
        Assert.True(typeof(IMonitorRepository).IsInterface);
    }

    [Fact]
    public void IAlertRepository_IsInterface()
    {
        Assert.True(typeof(IAlertRepository).IsInterface);
    }

    [Fact]
    public void IUnitOfWork_IsInterface()
    {
        Assert.True(typeof(IUnitOfWork).IsInterface);
    }

    [Fact]
    public void IServiceRepository_HasGetByIdAsync()
    {
        var method = typeof(IServiceRepository).GetMethod("GetByIdAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IServiceRepository_HasGetAllAsync()
    {
        var method = typeof(IServiceRepository).GetMethod("GetAllAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IServiceRepository_HasGetCountAsync()
    {
        var method = typeof(IServiceRepository).GetMethod("GetCountAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IServiceRepository_HasAddAsync()
    {
        var method = typeof(IServiceRepository).GetMethod("AddAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IServiceRepository_HasUpdateAsync()
    {
        var method = typeof(IServiceRepository).GetMethod("UpdateAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IServiceRepository_HasDeleteAsync()
    {
        var method = typeof(IServiceRepository).GetMethod("DeleteAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IMonitorRepository_HasGetByTokenAsync()
    {
        var method = typeof(IMonitorRepository).GetMethod("GetByTokenAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IMonitorRepository_HasGetByServiceIdAsync()
    {
        var method = typeof(IMonitorRepository).GetMethod("GetByServiceIdAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IMonitorRepository_HasGetHeartbeatMonitorsDueAsync()
    {
        var method = typeof(IMonitorRepository).GetMethod("GetHeartbeatMonitorsDueAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IAlertRepository_HasGetPendingDispatchAsync()
    {
        var method = typeof(IAlertRepository).GetMethod("GetPendingDispatchAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IUnitOfWork_HasSaveChangesAsync()
    {
        var method = typeof(IUnitOfWork).GetMethod("SaveChangesAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IEventBroadcaster_HasExpectedMethods()
    {
        var type = typeof(IEventBroadcaster);
        Assert.NotNull(type);
        Assert.True(type.IsInterface);

        var broadcast = type.GetMethod("BroadcastAsync");
        Assert.NotNull(broadcast);
        Assert.Equal(typeof(Task), broadcast!.ReturnType);

        var subscribe = type.GetMethod("Subscribe");
        Assert.NotNull(subscribe);
    }

    [Fact]
    public void ServerEvent_HasExpectedProperties()
    {
        var evt = new ServerEvent
        {
            Type = "alert_created",
            Payload = "{}"
        };
        Assert.Equal("alert_created", evt.Type);
        Assert.Equal("{}", evt.Payload);
        Assert.True(evt.Timestamp <= DateTime.UtcNow);
    }
}
