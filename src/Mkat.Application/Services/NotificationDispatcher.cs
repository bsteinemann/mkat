using Microsoft.Extensions.Logging;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;

namespace Mkat.Application.Services;

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly IEnumerable<INotificationChannel> _fallbackChannels;
    private readonly IServiceRepository _serviceRepo;
    private readonly IAlertRepository _alertRepo;
    private readonly IContactRepository _contactRepo;
    private readonly IContactChannelSender _channelSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        IEnumerable<INotificationChannel> fallbackChannels,
        IServiceRepository serviceRepo,
        IAlertRepository alertRepo,
        IContactRepository contactRepo,
        IContactChannelSender channelSender,
        IUnitOfWork unitOfWork,
        ILogger<NotificationDispatcher> logger)
    {
        _fallbackChannels = fallbackChannels;
        _serviceRepo = serviceRepo;
        _alertRepo = alertRepo;
        _contactRepo = contactRepo;
        _channelSender = channelSender;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task DispatchAsync(Alert alert, CancellationToken ct = default)
    {
        var service = await _serviceRepo.GetByIdAsync(alert.ServiceId, ct);
        if (service == null)
        {
            _logger.LogWarning("Cannot dispatch alert {AlertId}: service not found", alert.Id);
            return;
        }

        // Resolve contacts for this service
        var contacts = await _contactRepo.GetByServiceIdAsync(alert.ServiceId, ct);

        // Fall back to default contact if none assigned
        if (!contacts.Any())
        {
            var defaultContact = await _contactRepo.GetDefaultAsync(ct);
            if (defaultContact != null)
            {
                contacts = new List<Contact> { defaultContact };
            }
        }

        // Collect all enabled channels from resolved contacts
        var enabledChannels = contacts
            .SelectMany(c => c.Channels)
            .Where(ch => ch.IsEnabled)
            .ToList();

        bool allSucceeded;

        if (enabledChannels.Any())
        {
            // Route via contact channels
            allSucceeded = true;
            foreach (var channel in enabledChannels)
            {
                try
                {
                    var success = await _channelSender.SendAlertAsync(channel, alert, service, ct);
                    if (!success)
                    {
                        _logger.LogWarning(
                            "ContactChannel {ChannelId} ({ChannelType}) failed to send alert {AlertId}",
                            channel.Id, channel.Type, alert.Id);
                        allSucceeded = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error sending alert {AlertId} via ContactChannel {ChannelId}",
                        alert.Id, channel.Id);
                    allSucceeded = false;
                }
            }
        }
        else
        {
            // Fall back to DI-registered channels (backward compatibility)
            var diChannels = _fallbackChannels.Where(c => c.IsEnabled).ToList();
            if (!diChannels.Any())
            {
                _logger.LogWarning("No notification channels available for alert {AlertId}", alert.Id);
                return;
            }

            allSucceeded = true;
            foreach (var channel in diChannels)
            {
                try
                {
                    var success = await channel.SendAlertAsync(alert, service, ct);
                    if (!success)
                    {
                        _logger.LogWarning(
                            "Channel {ChannelType} failed to send alert {AlertId}",
                            channel.ChannelType, alert.Id);
                        allSucceeded = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error sending alert {AlertId} via channel {ChannelType}",
                        alert.Id, channel.ChannelType);
                    allSucceeded = false;
                }
            }
        }

        if (allSucceeded)
        {
            alert.DispatchedAt = DateTime.UtcNow;
            await _alertRepo.UpdateAsync(alert, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
