using Microsoft.Extensions.Logging;
using Moq;
using Mkat.Application.Interfaces;
using Mkat.Application.Services;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Xunit;

namespace Mkat.Application.Tests.Services;

public class ContactRoutingDispatcherTests
{
    private readonly Mock<IContactRepository> _contactRepo = new();
    private readonly Mock<IServiceRepository> _serviceRepo = new();
    private readonly Mock<IAlertRepository> _alertRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IContactChannelSender> _channelSender = new();
    private readonly Mock<ILogger<NotificationDispatcher>> _logger = new();
    private readonly List<Mock<INotificationChannel>> _fallbackChannels = new();

    private NotificationDispatcher CreateDispatcher()
    {
        return new NotificationDispatcher(
            _fallbackChannels.Select(m => m.Object),
            _serviceRepo.Object,
            _alertRepo.Object,
            _contactRepo.Object,
            _channelSender.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    private (Alert alert, Service service) CreateAlertAndService()
    {
        var serviceId = Guid.NewGuid();
        var service = new Service { Id = serviceId, Name = "Test", Severity = Severity.High };
        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            Type = AlertType.Failure,
            Message = "Service down"
        };
        _serviceRepo.Setup(r => r.GetByIdAsync(serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);
        return (alert, service);
    }

    [Fact]
    public async Task Dispatch_WithAssignedContacts_UsesContactChannels()
    {
        var (alert, service) = CreateAlertAndService();

        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            Name = "On-call",
            Channels = new List<ContactChannel>
            {
                new() { Id = Guid.NewGuid(), Type = ChannelType.Telegram, Configuration = "{}", IsEnabled = true }
            }
        };

        _contactRepo.Setup(r => r.GetByServiceIdAsync(alert.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Contact> { contact });
        _channelSender.Setup(s => s.SendAlertAsync(It.IsAny<ContactChannel>(), alert, service, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await CreateDispatcher().DispatchAsync(alert);

        _channelSender.Verify(s => s.SendAlertAsync(
            It.IsAny<ContactChannel>(), alert, service, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Dispatch_WithNoAssignedContacts_FallsBackToDefaultContact()
    {
        var (alert, service) = CreateAlertAndService();

        _contactRepo.Setup(r => r.GetByServiceIdAsync(alert.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Contact>());

        var defaultContact = new Contact
        {
            Id = Guid.NewGuid(),
            Name = "Default",
            IsDefault = true,
            Channels = new List<ContactChannel>
            {
                new() { Id = Guid.NewGuid(), Type = ChannelType.Telegram, Configuration = "{}", IsEnabled = true }
            }
        };
        _contactRepo.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultContact);
        _channelSender.Setup(s => s.SendAlertAsync(It.IsAny<ContactChannel>(), alert, service, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await CreateDispatcher().DispatchAsync(alert);

        _channelSender.Verify(s => s.SendAlertAsync(
            It.IsAny<ContactChannel>(), alert, service, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Dispatch_WithNoContactsAndNoDefault_FallsBackToDIChannels()
    {
        var (alert, service) = CreateAlertAndService();

        _contactRepo.Setup(r => r.GetByServiceIdAsync(alert.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Contact>());
        _contactRepo.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        var fallback = new Mock<INotificationChannel>();
        fallback.Setup(c => c.IsEnabled).Returns(true);
        fallback.Setup(c => c.SendAlertAsync(alert, service, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fallbackChannels.Add(fallback);

        await CreateDispatcher().DispatchAsync(alert);

        fallback.Verify(c => c.SendAlertAsync(alert, service, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Dispatch_SkipsDisabledChannels()
    {
        var (alert, service) = CreateAlertAndService();

        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            Name = "Team",
            Channels = new List<ContactChannel>
            {
                new() { Id = Guid.NewGuid(), Type = ChannelType.Telegram, Configuration = "{}", IsEnabled = true },
                new() { Id = Guid.NewGuid(), Type = ChannelType.Telegram, Configuration = "{}", IsEnabled = false }
            }
        };

        _contactRepo.Setup(r => r.GetByServiceIdAsync(alert.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Contact> { contact });
        _channelSender.Setup(s => s.SendAlertAsync(It.IsAny<ContactChannel>(), alert, service, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await CreateDispatcher().DispatchAsync(alert);

        _channelSender.Verify(s => s.SendAlertAsync(
            It.IsAny<ContactChannel>(), alert, service, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Dispatch_MarksAlertDispatched_WhenAllSucceed()
    {
        var (alert, service) = CreateAlertAndService();

        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            Name = "Team",
            Channels = new List<ContactChannel>
            {
                new() { Id = Guid.NewGuid(), Type = ChannelType.Telegram, Configuration = "{}", IsEnabled = true }
            }
        };

        _contactRepo.Setup(r => r.GetByServiceIdAsync(alert.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Contact> { contact });
        _channelSender.Setup(s => s.SendAlertAsync(It.IsAny<ContactChannel>(), alert, service, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await CreateDispatcher().DispatchAsync(alert);

        Assert.NotNull(alert.DispatchedAt);
        _alertRepo.Verify(r => r.UpdateAsync(alert, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Dispatch_DoesNotMarkDispatched_WhenAnySenderFails()
    {
        var (alert, service) = CreateAlertAndService();

        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            Name = "Team",
            Channels = new List<ContactChannel>
            {
                new() { Id = Guid.NewGuid(), Type = ChannelType.Telegram, Configuration = "{}", IsEnabled = true }
            }
        };

        _contactRepo.Setup(r => r.GetByServiceIdAsync(alert.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Contact> { contact });
        _channelSender.Setup(s => s.SendAlertAsync(It.IsAny<ContactChannel>(), alert, service, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await CreateDispatcher().DispatchAsync(alert);

        Assert.Null(alert.DispatchedAt);
    }
}
