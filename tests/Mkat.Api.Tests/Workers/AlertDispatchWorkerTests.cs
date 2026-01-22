using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Workers;
using Xunit;

namespace Mkat.Api.Tests.Workers;

public class AlertDispatchWorkerTests
{
    private readonly Mock<IAlertRepository> _alertRepoMock;
    private readonly Mock<INotificationDispatcher> _dispatcherMock;
    private readonly AlertDispatchWorker _worker;

    public AlertDispatchWorkerTests()
    {
        _alertRepoMock = new Mock<IAlertRepository>();
        _dispatcherMock = new Mock<INotificationDispatcher>();

        var services = new ServiceCollection();
        services.AddSingleton(_alertRepoMock.Object);
        services.AddSingleton(_dispatcherMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var loggerMock = new Mock<ILogger<AlertDispatchWorker>>();
        _worker = new AlertDispatchWorker(serviceProvider, loggerMock.Object);
    }

    [Fact]
    public async Task DispatchPendingAlerts_DispatchesEachAlert()
    {
        var alerts = new List<Alert>
        {
            new() { Id = Guid.NewGuid(), ServiceId = Guid.NewGuid(), Type = AlertType.Failure, Message = "fail 1" },
            new() { Id = Guid.NewGuid(), ServiceId = Guid.NewGuid(), Type = AlertType.MissedHeartbeat, Message = "missed" }
        };

        _alertRepoMock.Setup(r => r.GetPendingDispatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(alerts);

        await _worker.DispatchPendingAlertsAsync(CancellationToken.None);

        _dispatcherMock.Verify(d => d.DispatchAsync(alerts[0], It.IsAny<CancellationToken>()), Times.Once);
        _dispatcherMock.Verify(d => d.DispatchAsync(alerts[1], It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchPendingAlerts_DoesNothing_WhenNoPending()
    {
        _alertRepoMock.Setup(r => r.GetPendingDispatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alert>());

        await _worker.DispatchPendingAlertsAsync(CancellationToken.None);

        _dispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchPendingAlerts_ContinuesOnDispatcherException()
    {
        var alerts = new List<Alert>
        {
            new() { Id = Guid.NewGuid(), ServiceId = Guid.NewGuid(), Type = AlertType.Failure, Message = "fail 1" },
            new() { Id = Guid.NewGuid(), ServiceId = Guid.NewGuid(), Type = AlertType.Failure, Message = "fail 2" }
        };

        _alertRepoMock.Setup(r => r.GetPendingDispatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(alerts);
        _dispatcherMock.Setup(d => d.DispatchAsync(alerts[0], It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("dispatch error"));

        await _worker.DispatchPendingAlertsAsync(CancellationToken.None);

        // Second alert should still be dispatched
        _dispatcherMock.Verify(d => d.DispatchAsync(alerts[1], It.IsAny<CancellationToken>()), Times.Once);
    }
}
