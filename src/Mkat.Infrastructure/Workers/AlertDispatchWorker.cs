using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mkat.Application.Interfaces;

namespace Mkat.Infrastructure.Workers;

public class AlertDispatchWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AlertDispatchWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5);
    private bool _notificationHealthy = true;

    public AlertDispatchWorker(
        IServiceProvider serviceProvider,
        ILogger<AlertDispatchWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AlertDispatchWorker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingAlertsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AlertDispatchWorker");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("AlertDispatchWorker stopping");
    }

    public async Task DispatchPendingAlertsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var alertRepo = scope.ServiceProvider.GetRequiredService<IAlertRepository>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();

        var pendingAlerts = await alertRepo.GetPendingDispatchAsync(ct);

        var hadFailure = false;
        foreach (var alert in pendingAlerts)
        {
            try
            {
                await dispatcher.DispatchAsync(alert, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch alert {AlertId}", alert.Id);
                hadFailure = true;
            }
        }

        // Only act on state transitions
        if (hadFailure && _notificationHealthy)
        {
            _notificationHealthy = false;
            await NotifyPeersAsync("fail", ct);
        }
        else if (!hadFailure && !_notificationHealthy && pendingAlerts.Count > 0)
        {
            _notificationHealthy = true;
            await NotifyPeersAsync("recover", ct);
        }
    }

    private async Task NotifyPeersAsync(string action, CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var peerRepo = scope.ServiceProvider.GetRequiredService<IPeerRepository>();
            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

            var peers = await peerRepo.GetAllAsync(ct);
            var client = httpClientFactory.CreateClient("PeerNotification");

            foreach (var peer in peers)
            {
                try
                {
                    var url = $"{peer.Url.TrimEnd('/')}/webhook/{peer.WebhookToken}/{action}";
                    await client.PostAsync(url, null, ct);

                    _logger.LogInformation("Notified peer {PeerName} of notification {Action}",
                        peer.Name, action);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Failed to notify peer {PeerName} of notification {Action}",
                        peer.Name, action);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Error notifying peers of notification {Action}", action);
        }
    }
}
