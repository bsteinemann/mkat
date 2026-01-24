using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using WebPush;
using PushSubscription = Mkat.Domain.Entities.PushSubscription;

namespace Mkat.Infrastructure.Channels;

public class WebPushChannel : INotificationChannel
{
    private readonly IPushSubscriptionRepository _subscriptionRepo;
    private readonly VapidOptions _vapidOptions;
    private readonly ILogger<WebPushChannel> _logger;

    public WebPushChannel(
        IPushSubscriptionRepository subscriptionRepo,
        IOptions<VapidOptions> vapidOptions,
        ILogger<WebPushChannel> logger)
    {
        _subscriptionRepo = subscriptionRepo;
        _vapidOptions = vapidOptions.Value;
        _logger = logger;
    }

    public string ChannelType => "WebPush";

    public bool IsEnabled => !string.IsNullOrEmpty(_vapidOptions.PublicKey)
                          && !string.IsNullOrEmpty(_vapidOptions.PrivateKey)
                          && !string.IsNullOrEmpty(_vapidOptions.Subject);

    public async Task<bool> SendAlertAsync(Alert alert, Service service, CancellationToken ct = default)
    {
        if (!IsEnabled) return false;

        var subscriptions = await _subscriptionRepo.GetAllAsync(ct);
        if (!subscriptions.Any()) return true;

        var payload = JsonSerializer.Serialize(new
        {
            title = $"mkat: {service.Name}",
            body = alert.Message,
            url = $"/services/{service.Id}"
        });

        var vapidDetails = new VapidDetails(_vapidOptions.Subject, _vapidOptions.PublicKey, _vapidOptions.PrivateKey);
        var webPushClient = new WebPushClient();

        var allSucceeded = true;
        foreach (var sub in subscriptions)
        {
            try
            {
                var pushSub = new WebPush.PushSubscription(sub.Endpoint, sub.P256dhKey, sub.AuthKey);
                await webPushClient.SendNotificationAsync(pushSub, payload, vapidDetails, ct);
            }
            catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                _logger.LogInformation("Removing expired push subscription {Endpoint}", sub.Endpoint);
                await _subscriptionRepo.RemoveByEndpointAsync(sub.Endpoint, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send push to {Endpoint}", sub.Endpoint);
                allSucceeded = false;
            }
        }

        return allSucceeded;
    }

    public Task<bool> ValidateConfigurationAsync(CancellationToken ct = default)
    {
        return Task.FromResult(IsEnabled);
    }
}
