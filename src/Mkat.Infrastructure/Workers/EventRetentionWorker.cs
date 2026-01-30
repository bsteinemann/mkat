using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mkat.Application.Interfaces;
using Mkat.Domain.Enums;

namespace Mkat.Infrastructure.Workers;

public class EventRetentionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventRetentionWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public EventRetentionWorker(
        IServiceProvider serviceProvider,
        ILogger<EventRetentionWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EventRetentionWorker starting");

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
                _logger.LogError(ex, "Error in EventRetentionWorker");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("EventRetentionWorker stopping");
    }

    public async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IMonitorEventRepository>();
        var rollupRepo = scope.ServiceProvider.GetRequiredService<IMonitorRollupRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTime.UtcNow;

        // Purge MonitorEvents older than 7 days
        await eventRepo.DeleteOlderThanAsync(now.AddDays(-7), ct);

        // Purge hourly rollups older than 30 days
        await rollupRepo.DeleteOlderThanAsync(Granularity.Hourly, now.AddDays(-30), ct);

        // Purge daily rollups older than 1 year
        await rollupRepo.DeleteOlderThanAsync(Granularity.Daily, now.AddYears(-1), ct);

        // Purge weekly rollups older than 2 years
        await rollupRepo.DeleteOlderThanAsync(Granularity.Weekly, now.AddYears(-2), ct);

        // Monthly rollups: keep forever (no purge)

        await unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Event retention cleanup completed");
    }
}
