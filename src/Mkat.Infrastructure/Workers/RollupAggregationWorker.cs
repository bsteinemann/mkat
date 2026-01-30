using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mkat.Application.Interfaces;
using Mkat.Domain.Enums;

namespace Mkat.Infrastructure.Workers;

public class RollupAggregationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RollupAggregationWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public RollupAggregationWorker(
        IServiceProvider serviceProvider,
        ILogger<RollupAggregationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RollupAggregationWorker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ComputeRollupsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RollupAggregationWorker");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("RollupAggregationWorker stopping");
    }

    public async Task ComputeRollupsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var monitorRepo = scope.ServiceProvider.GetRequiredService<IMonitorRepository>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IMonitorEventRepository>();
        var rollupRepo = scope.ServiceProvider.GetRequiredService<IMonitorRollupRepository>();
        var calculator = scope.ServiceProvider.GetRequiredService<IRollupCalculator>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var monitors = await monitorRepo.GetAllAsync(ct);
        var now = DateTime.UtcNow;

        foreach (var monitor in monitors)
        {
            // Compute hourly rollup for the previous hour
            var hourStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(-1);
            var hourEnd = hourStart.AddHours(1);

            var events = await eventRepo.GetByMonitorIdInWindowAsync(monitor.Id, hourStart, hourEnd, ct);

            if (events.Count > 0)
            {
                var hourlyRollup = calculator.Compute(events, monitor.Id, monitor.ServiceId, Granularity.Hourly, hourStart);
                await rollupRepo.UpsertAsync(hourlyRollup, ct);
            }

            // Compute daily rollup for yesterday
            var dayStart = now.Date.AddDays(-1);
            var dayEnd = dayStart.AddDays(1);

            var hourlyRollups = await rollupRepo.GetByMonitorIdAsync(monitor.Id, Granularity.Hourly, dayStart, dayEnd.AddTicks(-1), ct);

            if (hourlyRollups.Count > 0)
            {
                var dayEvents = await eventRepo.GetByMonitorIdInWindowAsync(monitor.Id, dayStart, dayEnd, ct);

                if (dayEvents.Count > 0)
                {
                    var dailyRollup = calculator.Compute(dayEvents, monitor.Id, monitor.ServiceId, Granularity.Daily, dayStart);
                    await rollupRepo.UpsertAsync(dailyRollup, ct);
                }
            }

            // Compute weekly rollup (Monday-based)
            var dayOfWeek = (int)now.DayOfWeek;
            var mondayOffset = dayOfWeek == 0 ? -6 : -(dayOfWeek - 1);
            var weekStart = now.Date.AddDays(mondayOffset - 7);
            var weekEnd = weekStart.AddDays(7);

            var dailyRollups = await rollupRepo.GetByMonitorIdAsync(monitor.Id, Granularity.Daily, weekStart, weekEnd.AddTicks(-1), ct);

            if (dailyRollups.Count > 0)
            {
                var weekEvents = await eventRepo.GetByMonitorIdInWindowAsync(monitor.Id, weekStart, weekEnd, ct);

                if (weekEvents.Count > 0)
                {
                    var weeklyRollup = calculator.Compute(weekEvents, monitor.Id, monitor.ServiceId, Granularity.Weekly, weekStart);
                    await rollupRepo.UpsertAsync(weeklyRollup, ct);
                }
            }

            // Compute monthly rollup for previous month
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1);
            var monthEnd = monthStart.AddMonths(1);

            var monthDailyRollups = await rollupRepo.GetByMonitorIdAsync(monitor.Id, Granularity.Daily, monthStart, monthEnd.AddTicks(-1), ct);

            if (monthDailyRollups.Count > 0)
            {
                var monthEvents = await eventRepo.GetByMonitorIdInWindowAsync(monitor.Id, monthStart, monthEnd, ct);

                if (monthEvents.Count > 0)
                {
                    var monthlyRollup = calculator.Compute(monthEvents, monitor.Id, monitor.ServiceId, Granularity.Monthly, monthStart);
                    await rollupRepo.UpsertAsync(monthlyRollup, ct);
                }
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Rollup aggregation completed for {Count} monitors", monitors.Count);
    }
}
