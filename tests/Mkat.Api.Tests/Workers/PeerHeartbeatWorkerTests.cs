using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Infrastructure.Workers;
using Xunit;

namespace Mkat.Api.Tests.Workers;

public class PeerHeartbeatWorkerTests
{
    private readonly Mock<IPeerRepository> _peerRepo;
    private readonly Mock<ILogger<PeerHeartbeatWorker>> _logger;
    private readonly Mock<HttpMessageHandler> _httpHandler;
    private readonly IServiceProvider _serviceProvider;
    private readonly PeerHeartbeatWorker _worker;

    public PeerHeartbeatWorkerTests()
    {
        _peerRepo = new Mock<IPeerRepository>();
        _logger = new Mock<ILogger<PeerHeartbeatWorker>>();
        _httpHandler = new Mock<HttpMessageHandler>();

        _httpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(_httpHandler.Object);
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var services = new ServiceCollection();
        services.AddSingleton(_peerRepo.Object);
        services.AddSingleton(httpClientFactory.Object);

        _serviceProvider = services.BuildServiceProvider();
        _worker = new PeerHeartbeatWorker(_serviceProvider, _logger.Object);
    }

    [Fact]
    public async Task SendHeartbeats_NoPeers_DoesNothing()
    {
        _peerRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Peer>());

        await _worker.SendHeartbeatsAsync(CancellationToken.None);

        _httpHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendHeartbeats_PeerDue_SendsHeartbeat()
    {
        var peer = new Peer
        {
            Id = Guid.NewGuid(),
            Name = "Test Peer",
            Url = "https://peer.example.com",
            HeartbeatToken = "hb-token-123",
            HeartbeatIntervalSeconds = 30
        };

        _peerRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Peer> { peer });

        await _worker.SendHeartbeatsAsync(CancellationToken.None);

        _httpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Post &&
                r.RequestUri!.ToString() == "https://peer.example.com/heartbeat/hb-token-123"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendHeartbeats_HttpFailure_DoesNotThrow()
    {
        var peer = new Peer
        {
            Id = Guid.NewGuid(),
            Url = "https://peer.example.com",
            HeartbeatToken = "hb-token",
            HeartbeatIntervalSeconds = 30
        };

        _peerRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Peer> { peer });

        _httpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        await _worker.SendHeartbeatsAsync(CancellationToken.None); // should not throw
    }

    [Fact]
    public async Task SendHeartbeats_MultiplePeers_SendsToAll()
    {
        var peers = new List<Peer>
        {
            new() { Id = Guid.NewGuid(), Url = "https://peer1.example.com", HeartbeatToken = "t1", HeartbeatIntervalSeconds = 30 },
            new() { Id = Guid.NewGuid(), Url = "https://peer2.example.com", HeartbeatToken = "t2", HeartbeatIntervalSeconds = 30 },
        };

        _peerRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(peers);

        await _worker.SendHeartbeatsAsync(CancellationToken.None);

        _httpHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(2),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendHeartbeats_RespectsInterval_SkipsIfNotDue()
    {
        var peer = new Peer
        {
            Id = Guid.NewGuid(),
            Url = "https://peer.example.com",
            HeartbeatToken = "hb-token",
            HeartbeatIntervalSeconds = 300 // 5 minutes
        };

        _peerRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Peer> { peer });

        // First call should send
        await _worker.SendHeartbeatsAsync(CancellationToken.None);
        // Second call immediately should skip (interval not elapsed)
        await _worker.SendHeartbeatsAsync(CancellationToken.None);

        _httpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }
}
