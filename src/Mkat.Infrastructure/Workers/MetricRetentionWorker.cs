using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mkat.Application.Interfaces;

namespace Mkat.Infrastructure.Workers;

public class MetricRetentionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MetricRetentionWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public MetricRetentionWorker(
        IServiceProvider serviceProvider,
        ILogger<MetricRetentionWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MetricRetentionWorker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MetricRetentionWorker");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("MetricRetentionWorker stopping");
    }

    public async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var monitorRepo = scope.ServiceProvider.GetRequiredService<IMonitorRepository>();
        var readingRepo = scope.ServiceProvider.GetRequiredService<IMetricReadingRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var metricMonitors = await monitorRepo.GetAllMetricMonitorsAsync(ct);

        foreach (var monitor in metricMonitors)
        {
            var threshold = DateTime.UtcNow.AddDays(-monitor.RetentionDays);
            await readingRepo.DeleteOlderThanAsync(monitor.Id, threshold, ct);

            _logger.LogDebug(
                "Cleaned readings older than {Threshold} for monitor {MonitorId}",
                threshold, monitor.Id);
        }

        if (metricMonitors.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation(
                "Metric retention cleanup completed for {Count} monitors",
                metricMonitors.Count);
        }
    }
}
