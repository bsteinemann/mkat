using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mkat.Application.Interfaces;
using Mkat.Application.Services;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;

namespace Mkat.Infrastructure.Workers;

public class HealthCheckWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HealthCheckWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);

    public HealthCheckWorker(
        IServiceProvider serviceProvider,
        ILogger<HealthCheckWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HealthCheckWorker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckHealthChecksAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HealthCheckWorker");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("HealthCheckWorker stopping");
    }

    public async Task CheckHealthChecksAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var monitorRepo = scope.ServiceProvider.GetRequiredService<IMonitorRepository>();
        var serviceRepo = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
        var stateService = scope.ServiceProvider.GetRequiredService<IStateService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        var now = DateTime.UtcNow;
        var monitors = await monitorRepo.GetHealthCheckMonitorsDueAsync(now, ct);
        var eventRepo = scope.ServiceProvider.GetRequiredService<IMonitorEventRepository>();

        foreach (var monitor in monitors)
        {
            var service = await serviceRepo.GetByIdAsync(monitor.ServiceId, ct);
            if (service == null || service.State == ServiceState.Paused)
                continue;

            var stopwatch = Stopwatch.StartNew();
            var (success, reason) = await ExecuteHealthCheckAsync(monitor, httpFactory, ct);
            stopwatch.Stop();

            var monitorEvent = new MonitorEvent
            {
                Id = Guid.NewGuid(),
                MonitorId = monitor.Id,
                ServiceId = monitor.ServiceId,
                EventType = EventType.HealthCheckPerformed,
                Success = success,
                Value = stopwatch.Elapsed.TotalMilliseconds,
                Message = success ? null : reason,
                CreatedAt = DateTime.UtcNow
            };
            await eventRepo.AddAsync(monitorEvent, ct);

            monitor.LastCheckIn = DateTime.UtcNow;
            await monitorRepo.UpdateAsync(monitor, ct);
            await unitOfWork.SaveChangesAsync(ct);

            if (success)
            {
                await stateService.TransitionToUpAsync(monitor.ServiceId, "Health check passed", ct);
            }
            else
            {
                _logger.LogWarning(
                    "Health check failed for service {ServiceId}: {Reason}",
                    monitor.ServiceId, reason);

                await stateService.TransitionToDownAsync(
                    monitor.ServiceId,
                    AlertType.FailedHealthCheck,
                    reason,
                    ct);
            }
        }
    }

    private static async Task<(bool Success, string Reason)> ExecuteHealthCheckAsync(
        Domain.Entities.Monitor monitor,
        IHttpClientFactory httpFactory,
        CancellationToken ct)
    {
        var client = httpFactory.CreateClient("HealthCheck");
        client.Timeout = TimeSpan.FromSeconds(monitor.TimeoutSeconds ?? 10);

        var method = (monitor.HttpMethod?.ToUpperInvariant()) switch
        {
            "HEAD" => HttpMethod.Head,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            _ => HttpMethod.Get
        };

        try
        {
            using var request = new HttpRequestMessage(method, monitor.HealthCheckUrl);
            using var response = await client.SendAsync(request, ct);

            var statusCode = (int)response.StatusCode;
            var expectedCodes = ParseExpectedStatusCodes(monitor.ExpectedStatusCodes ?? "200");

            if (!expectedCodes.Contains(statusCode))
            {
                return (false, $"Unexpected status code: {statusCode}. Expected: {monitor.ExpectedStatusCodes ?? "200"}");
            }

            if (!string.IsNullOrEmpty(monitor.BodyMatchRegex))
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                if (!Regex.IsMatch(body, monitor.BodyMatchRegex))
                {
                    return (false, $"Body did not match pattern: {monitor.BodyMatchRegex}");
                }
            }

            return (true, string.Empty);
        }
        catch (TaskCanceledException)
        {
            return (false, $"Health check timed out after {monitor.TimeoutSeconds ?? 10}s");
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }

    private static HashSet<int> ParseExpectedStatusCodes(string codes)
    {
        var result = new HashSet<int>();
        foreach (var code in codes.Split(','))
        {
            if (int.TryParse(code.Trim(), out var parsed))
                result.Add(parsed);
        }
        return result.Count > 0 ? result : new HashSet<int> { 200 };
    }
}
