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

        foreach (var alert in pendingAlerts)
        {
            try
            {
                await dispatcher.DispatchAsync(alert, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch alert {AlertId}", alert.Id);
            }
        }
    }
}
