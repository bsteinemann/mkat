using Microsoft.Extensions.Logging;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;

namespace Mkat.Application.Services;

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly IServiceRepository _serviceRepo;
    private readonly IAlertRepository _alertRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        IEnumerable<INotificationChannel> channels,
        IServiceRepository serviceRepo,
        IAlertRepository alertRepo,
        IUnitOfWork unitOfWork,
        ILogger<NotificationDispatcher> logger)
    {
        _channels = channels;
        _serviceRepo = serviceRepo;
        _alertRepo = alertRepo;
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

        var enabledChannels = _channels.Where(c => c.IsEnabled).ToList();
        if (!enabledChannels.Any())
        {
            _logger.LogWarning("No enabled notification channels, alert {AlertId} not sent", alert.Id);
            return;
        }

        var allSucceeded = true;
        foreach (var channel in enabledChannels)
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

        if (allSucceeded)
        {
            alert.DispatchedAt = DateTime.UtcNow;
            await _alertRepo.UpdateAsync(alert, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
