using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Mkat.Application.Interfaces;
using Mkat.Application.Services;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Workers;
using Xunit;
using Monitor = Mkat.Domain.Entities.Monitor;

namespace Mkat.Api.Tests.Workers;

public class HeartbeatMonitorWorkerTests
{
    private readonly Mock<IMonitorRepository> _monitorRepoMock;
    private readonly Mock<IServiceRepository> _serviceRepoMock;
    private readonly Mock<IStateService> _stateServiceMock;
    private readonly HeartbeatMonitorWorker _worker;

    public HeartbeatMonitorWorkerTests()
    {
        _monitorRepoMock = new Mock<IMonitorRepository>();
        _serviceRepoMock = new Mock<IServiceRepository>();
        _stateServiceMock = new Mock<IStateService>();

        var serviceProvider = BuildServiceProvider();
        var loggerMock = new Mock<ILogger<HeartbeatMonitorWorker>>();

        _worker = new HeartbeatMonitorWorker(serviceProvider, loggerMock.Object);
    }

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_monitorRepoMock.Object);
        services.AddSingleton(_serviceRepoMock.Object);
        services.AddSingleton(_stateServiceMock.Object);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task CheckMissedHeartbeats_TransitionsOverdueService_ToDown()
    {
        var serviceId = Guid.NewGuid();
        var monitor = new Monitor
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            Type = MonitorType.Heartbeat,
            Token = "test-token",
            IntervalSeconds = 60,
            GracePeriodSeconds = 10,
            LastCheckIn = DateTime.UtcNow.AddMinutes(-5), // Way overdue
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
        var service = new Service
        {
            Id = serviceId,
            Name = "Test",
            State = ServiceState.Up
        };

        _monitorRepoMock.Setup(r => r.GetHeartbeatMonitorsDueAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor> { monitor });
        _serviceRepoMock.Setup(r => r.GetByIdAsync(serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        await _worker.CheckMissedHeartbeatsAsync(CancellationToken.None);

        _stateServiceMock.Verify(s => s.TransitionToDownAsync(
            serviceId,
            AlertType.MissedHeartbeat,
            It.Is<string>(msg => msg.Contains("Heartbeat missed")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckMissedHeartbeats_SkipsPausedServices()
    {
        var serviceId = Guid.NewGuid();
        var monitor = new Monitor
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            Type = MonitorType.Heartbeat,
            Token = "test-token",
            IntervalSeconds = 60,
            GracePeriodSeconds = 10,
            LastCheckIn = DateTime.UtcNow.AddMinutes(-5),
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
        var service = new Service
        {
            Id = serviceId,
            Name = "Test",
            State = ServiceState.Paused
        };

        _monitorRepoMock.Setup(r => r.GetHeartbeatMonitorsDueAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor> { monitor });
        _serviceRepoMock.Setup(r => r.GetByIdAsync(serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        await _worker.CheckMissedHeartbeatsAsync(CancellationToken.None);

        _stateServiceMock.Verify(s => s.TransitionToDownAsync(
            It.IsAny<Guid>(), It.IsAny<AlertType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckMissedHeartbeats_SkipsAlreadyDownServices()
    {
        var serviceId = Guid.NewGuid();
        var monitor = new Monitor
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            Type = MonitorType.Heartbeat,
            Token = "test-token",
            IntervalSeconds = 60,
            GracePeriodSeconds = 10,
            LastCheckIn = DateTime.UtcNow.AddMinutes(-5),
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
        var service = new Service
        {
            Id = serviceId,
            Name = "Test",
            State = ServiceState.Down
        };

        _monitorRepoMock.Setup(r => r.GetHeartbeatMonitorsDueAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor> { monitor });
        _serviceRepoMock.Setup(r => r.GetByIdAsync(serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        await _worker.CheckMissedHeartbeatsAsync(CancellationToken.None);

        _stateServiceMock.Verify(s => s.TransitionToDownAsync(
            It.IsAny<Guid>(), It.IsAny<AlertType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckMissedHeartbeats_DoesNotTransition_WhenWithinGracePeriod()
    {
        var serviceId = Guid.NewGuid();
        var monitor = new Monitor
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            Type = MonitorType.Heartbeat,
            Token = "test-token",
            IntervalSeconds = 60,
            GracePeriodSeconds = 30,
            LastCheckIn = DateTime.UtcNow.AddSeconds(-50), // 50s ago, interval+grace = 90s
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
        var service = new Service
        {
            Id = serviceId,
            Name = "Test",
            State = ServiceState.Up
        };

        _monitorRepoMock.Setup(r => r.GetHeartbeatMonitorsDueAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor> { monitor });
        _serviceRepoMock.Setup(r => r.GetByIdAsync(serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        await _worker.CheckMissedHeartbeatsAsync(CancellationToken.None);

        _stateServiceMock.Verify(s => s.TransitionToDownAsync(
            It.IsAny<Guid>(), It.IsAny<AlertType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckMissedHeartbeats_UsesCreatedAt_WhenNoLastCheckIn()
    {
        var serviceId = Guid.NewGuid();
        var monitor = new Monitor
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            Type = MonitorType.Heartbeat,
            Token = "test-token",
            IntervalSeconds = 60,
            GracePeriodSeconds = 10,
            LastCheckIn = null, // Never checked in
            CreatedAt = DateTime.UtcNow.AddMinutes(-5) // Created 5 min ago, deadline was 70s after creation
        };
        var service = new Service
        {
            Id = serviceId,
            Name = "Test",
            State = ServiceState.Up
        };

        _monitorRepoMock.Setup(r => r.GetHeartbeatMonitorsDueAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor> { monitor });
        _serviceRepoMock.Setup(r => r.GetByIdAsync(serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        await _worker.CheckMissedHeartbeatsAsync(CancellationToken.None);

        _stateServiceMock.Verify(s => s.TransitionToDownAsync(
            serviceId,
            AlertType.MissedHeartbeat,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckMissedHeartbeats_HandlesEmptyMonitorList()
    {
        _monitorRepoMock.Setup(r => r.GetHeartbeatMonitorsDueAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor>());

        await _worker.CheckMissedHeartbeatsAsync(CancellationToken.None);

        _stateServiceMock.Verify(s => s.TransitionToDownAsync(
            It.IsAny<Guid>(), It.IsAny<AlertType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
