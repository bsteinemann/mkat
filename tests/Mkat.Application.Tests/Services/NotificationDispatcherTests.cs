using Microsoft.Extensions.Logging;
using Moq;
using Mkat.Application.Interfaces;
using Mkat.Application.Services;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Xunit;

namespace Mkat.Application.Tests.Services;

public class NotificationDispatcherTests
{
    private readonly Mock<IServiceRepository> _serviceRepoMock;
    private readonly Mock<IAlertRepository> _alertRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<INotificationChannel> _channelMock;

    public NotificationDispatcherTests()
    {
        _serviceRepoMock = new Mock<IServiceRepository>();
        _alertRepoMock = new Mock<IAlertRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _channelMock = new Mock<INotificationChannel>();
        _channelMock.Setup(c => c.ChannelType).Returns("test");
    }

    private NotificationDispatcher CreateDispatcher(params INotificationChannel[] channels)
    {
        var loggerMock = new Mock<ILogger<NotificationDispatcher>>();
        return new NotificationDispatcher(
            channels,
            _serviceRepoMock.Object,
            _alertRepoMock.Object,
            _unitOfWorkMock.Object,
            loggerMock.Object);
    }

    private static Alert CreateAlert(Guid serviceId) => new()
    {
        Id = Guid.NewGuid(),
        ServiceId = serviceId,
        Type = AlertType.Failure,
        Severity = Severity.High,
        Message = "Test failure",
        CreatedAt = DateTime.UtcNow
    };

    private static Service CreateService() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Service",
        State = ServiceState.Down
    };

    [Fact]
    public async Task DispatchAsync_DoesNothing_WhenServiceNotFound()
    {
        _serviceRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Service?)null);
        var dispatcher = CreateDispatcher(_channelMock.Object);
        var alert = CreateAlert(Guid.NewGuid());

        await dispatcher.DispatchAsync(alert);

        _channelMock.Verify(c => c.SendAlertAsync(It.IsAny<Alert>(), It.IsAny<Service>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_DoesNothing_WhenNoEnabledChannels()
    {
        var service = CreateService();
        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);
        _channelMock.Setup(c => c.IsEnabled).Returns(false);
        var dispatcher = CreateDispatcher(_channelMock.Object);
        var alert = CreateAlert(service.Id);

        await dispatcher.DispatchAsync(alert);

        _channelMock.Verify(c => c.SendAlertAsync(It.IsAny<Alert>(), It.IsAny<Service>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_SendsToEnabledChannel()
    {
        var service = CreateService();
        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);
        _channelMock.Setup(c => c.IsEnabled).Returns(true);
        _channelMock.Setup(c => c.SendAlertAsync(It.IsAny<Alert>(), service, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var dispatcher = CreateDispatcher(_channelMock.Object);
        var alert = CreateAlert(service.Id);

        await dispatcher.DispatchAsync(alert);

        _channelMock.Verify(c => c.SendAlertAsync(alert, service, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_SetsDispatchedAt_WhenAllSucceed()
    {
        var service = CreateService();
        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);
        _channelMock.Setup(c => c.IsEnabled).Returns(true);
        _channelMock.Setup(c => c.SendAlertAsync(It.IsAny<Alert>(), service, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var dispatcher = CreateDispatcher(_channelMock.Object);
        var alert = CreateAlert(service.Id);

        await dispatcher.DispatchAsync(alert);

        Assert.NotNull(alert.DispatchedAt);
        _alertRepoMock.Verify(r => r.UpdateAsync(alert, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_DoesNotSetDispatchedAt_WhenChannelFails()
    {
        var service = CreateService();
        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);
        _channelMock.Setup(c => c.IsEnabled).Returns(true);
        _channelMock.Setup(c => c.SendAlertAsync(It.IsAny<Alert>(), service, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var dispatcher = CreateDispatcher(_channelMock.Object);
        var alert = CreateAlert(service.Id);

        await dispatcher.DispatchAsync(alert);

        Assert.Null(alert.DispatchedAt);
        _alertRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_DoesNotSetDispatchedAt_WhenChannelThrows()
    {
        var service = CreateService();
        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);
        _channelMock.Setup(c => c.IsEnabled).Returns(true);
        _channelMock.Setup(c => c.SendAlertAsync(It.IsAny<Alert>(), service, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection failed"));
        var dispatcher = CreateDispatcher(_channelMock.Object);
        var alert = CreateAlert(service.Id);

        await dispatcher.DispatchAsync(alert);

        Assert.Null(alert.DispatchedAt);
    }

    [Fact]
    public async Task DispatchAsync_SendsToMultipleChannels()
    {
        var service = CreateService();
        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        var channel1 = new Mock<INotificationChannel>();
        channel1.Setup(c => c.IsEnabled).Returns(true);
        channel1.Setup(c => c.ChannelType).Returns("ch1");
        channel1.Setup(c => c.SendAlertAsync(It.IsAny<Alert>(), service, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var channel2 = new Mock<INotificationChannel>();
        channel2.Setup(c => c.IsEnabled).Returns(true);
        channel2.Setup(c => c.ChannelType).Returns("ch2");
        channel2.Setup(c => c.SendAlertAsync(It.IsAny<Alert>(), service, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dispatcher = CreateDispatcher(channel1.Object, channel2.Object);
        var alert = CreateAlert(service.Id);

        await dispatcher.DispatchAsync(alert);

        channel1.Verify(c => c.SendAlertAsync(alert, service, It.IsAny<CancellationToken>()), Times.Once);
        channel2.Verify(c => c.SendAlertAsync(alert, service, It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(alert.DispatchedAt);
    }

    [Fact]
    public async Task DispatchAsync_PartialFailure_DoesNotMarkDispatched()
    {
        var service = CreateService();
        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        var channel1 = new Mock<INotificationChannel>();
        channel1.Setup(c => c.IsEnabled).Returns(true);
        channel1.Setup(c => c.ChannelType).Returns("ch1");
        channel1.Setup(c => c.SendAlertAsync(It.IsAny<Alert>(), service, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var channel2 = new Mock<INotificationChannel>();
        channel2.Setup(c => c.IsEnabled).Returns(true);
        channel2.Setup(c => c.ChannelType).Returns("ch2");
        channel2.Setup(c => c.SendAlertAsync(It.IsAny<Alert>(), service, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // This one fails

        var dispatcher = CreateDispatcher(channel1.Object, channel2.Object);
        var alert = CreateAlert(service.Id);

        await dispatcher.DispatchAsync(alert);

        Assert.Null(alert.DispatchedAt);
    }

    [Fact]
    public async Task DispatchAsync_SkipsDisabledChannels()
    {
        var service = CreateService();
        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        var enabledChannel = new Mock<INotificationChannel>();
        enabledChannel.Setup(c => c.IsEnabled).Returns(true);
        enabledChannel.Setup(c => c.ChannelType).Returns("enabled");
        enabledChannel.Setup(c => c.SendAlertAsync(It.IsAny<Alert>(), service, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var disabledChannel = new Mock<INotificationChannel>();
        disabledChannel.Setup(c => c.IsEnabled).Returns(false);
        disabledChannel.Setup(c => c.ChannelType).Returns("disabled");

        var dispatcher = CreateDispatcher(enabledChannel.Object, disabledChannel.Object);
        var alert = CreateAlert(service.Id);

        await dispatcher.DispatchAsync(alert);

        enabledChannel.Verify(c => c.SendAlertAsync(alert, service, It.IsAny<CancellationToken>()), Times.Once);
        disabledChannel.Verify(c => c.SendAlertAsync(It.IsAny<Alert>(), It.IsAny<Service>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
