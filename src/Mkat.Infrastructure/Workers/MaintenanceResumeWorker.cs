using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mkat.Application.Interfaces;
using Mkat.Application.Services;

namespace Mkat.Infrastructure.Workers;

public class MaintenanceResumeWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MaintenanceResumeWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(60);

    public MaintenanceResumeWorker(
        IServiceProvider serviceProvider,
        ILogger<MaintenanceResumeWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MaintenanceResumeWorker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckMaintenanceWindowsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MaintenanceResumeWorker");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("MaintenanceResumeWorker stopping");
    }

    public async Task CheckMaintenanceWindowsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var serviceRepo = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
        var stateService = scope.ServiceProvider.GetRequiredService<IStateService>();

        var now = DateTime.UtcNow;
        var pausedServices = await serviceRepo.GetPausedServicesAsync(ct);

        foreach (var service in pausedServices)
        {
            if (service.AutoResume &&
                service.PausedUntil.HasValue &&
                service.PausedUntil.Value <= now)
            {
                _logger.LogInformation(
                    "Auto-resuming service {ServiceId} after maintenance window",
                    service.Id);

                await stateService.ResumeServiceAsync(service.Id, ct);
            }
        }
    }
}
