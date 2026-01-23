using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mkat.Application.Interfaces;

namespace Mkat.Infrastructure.Workers;

public class PeerHeartbeatWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PeerHeartbeatWorker> _logger;
    private readonly ConcurrentDictionary<Guid, DateTime> _lastSentTimes = new();

    public PeerHeartbeatWorker(IServiceProvider serviceProvider, ILogger<PeerHeartbeatWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendHeartbeatsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in PeerHeartbeatWorker");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    public async Task SendHeartbeatsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var peerRepo = scope.ServiceProvider.GetRequiredService<IPeerRepository>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        var peers = await peerRepo.GetAllAsync(ct);

        foreach (var peer in peers)
        {
            if (!IsDue(peer.Id, peer.HeartbeatIntervalSeconds))
                continue;

            var url = $"{peer.Url.TrimEnd('/')}/heartbeat/{peer.HeartbeatToken}";

            try
            {
                var client = httpClientFactory.CreateClient("PeerHeartbeat");
                var response = await client.PostAsync(url, null, ct);

                _logger.LogDebug("Sent heartbeat to peer {PeerName} ({PeerUrl}): {StatusCode}",
                    peer.Name, peer.Url, response.StatusCode);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to send heartbeat to peer {PeerName} ({PeerUrl})",
                    peer.Name, peer.Url);
            }

            _lastSentTimes[peer.Id] = DateTime.UtcNow;
        }
    }

    private bool IsDue(Guid peerId, int intervalSeconds)
    {
        if (!_lastSentTimes.TryGetValue(peerId, out var lastSent))
            return true;

        return (DateTime.UtcNow - lastSent).TotalSeconds >= intervalSeconds;
    }
}
