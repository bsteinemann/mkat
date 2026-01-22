using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mkat.Application.Interfaces;
using Mkat.Application.Services;
using Mkat.Domain.Enums;

namespace Mkat.Infrastructure.Workers;

public class HeartbeatMonitorWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HeartbeatMonitorWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);

    public HeartbeatMonitorWorker(
        IServiceProvider serviceProvider,
        ILogger<HeartbeatMonitorWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HeartbeatMonitorWorker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckMissedHeartbeatsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HeartbeatMonitorWorker");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("HeartbeatMonitorWorker stopping");
    }

    public async Task CheckMissedHeartbeatsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var monitorRepo = scope.ServiceProvider.GetRequiredService<IMonitorRepository>();
        var serviceRepo = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
        var stateService = scope.ServiceProvider.GetRequiredService<IStateService>();

        var now = DateTime.UtcNow;
        var heartbeatMonitors = await monitorRepo.GetHeartbeatMonitorsDueAsync(now, ct);

        foreach (var monitor in heartbeatMonitors)
        {
            var service = await serviceRepo.GetByIdAsync(monitor.ServiceId, ct);
            if (service == null || service.State == ServiceState.Paused)
                continue;

            if (service.State == ServiceState.Down)
                continue;

            var deadline = (monitor.LastCheckIn ?? monitor.CreatedAt)
                .AddSeconds(monitor.IntervalSeconds + monitor.GracePeriodSeconds);

            if (now > deadline)
            {
                _logger.LogWarning(
                    "Heartbeat missed for service {ServiceId}, last check-in: {LastCheckIn}",
                    monitor.ServiceId, monitor.LastCheckIn);

                await stateService.TransitionToDownAsync(
                    monitor.ServiceId,
                    AlertType.MissedHeartbeat,
                    $"Heartbeat missed. Last check-in: {monitor.LastCheckIn:u}",
                    ct);
            }
        }
    }
}
