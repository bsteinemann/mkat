using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Workers;
using Xunit;

namespace Mkat.Api.Tests.Workers;

public class AlertDispatchNotificationHookTests
{
    private readonly Mock<IAlertRepository> _alertRepo;
    private readonly Mock<INotificationDispatcher> _dispatcher;
    private readonly Mock<IPeerRepository> _peerRepo;
    private readonly Mock<HttpMessageHandler> _httpHandler;
    private readonly Mock<ILogger<AlertDispatchWorker>> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly AlertDispatchWorker _worker;

    public AlertDispatchNotificationHookTests()
    {
        _alertRepo = new Mock<IAlertRepository>();
        _dispatcher = new Mock<INotificationDispatcher>();
        _peerRepo = new Mock<IPeerRepository>();
        _logger = new Mock<ILogger<AlertDispatchWorker>>();
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
        services.AddSingleton(_alertRepo.Object);
        services.AddSingleton(_dispatcher.Object);
        services.AddSingleton(_peerRepo.Object);
        services.AddSingleton(httpClientFactory.Object);

        _serviceProvider = services.BuildServiceProvider();
        _worker = new AlertDispatchWorker(_serviceProvider, _logger.Object);
    }

    [Fact]
    public async Task DispatchFails_NotifiesPeersWithWebhookFail()
    {
        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            Type = AlertType.Failure,
            Severity = Severity.High,
            Message = "Test alert"
        };

        _alertRepo.Setup(r => r.GetPendingDispatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alert> { alert });

        _dispatcher.Setup(d => d.DispatchAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Telegram unreachable"));

        var peer = new Peer
        {
            Id = Guid.NewGuid(),
            Url = "https://peer.example.com",
            WebhookToken = "wh-token-123",
            HeartbeatToken = "hb-token"
        };
        _peerRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Peer> { peer });

        await _worker.DispatchPendingAlertsAsync(CancellationToken.None);

        _httpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Post &&
                r.RequestUri!.ToString() == "https://peer.example.com/webhook/wh-token-123/fail"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task DispatchSucceedsAfterFailure_NotifiesPeersWithRecover()
    {
        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            Type = AlertType.Failure,
            Severity = Severity.High,
            Message = "Test"
        };

        _alertRepo.Setup(r => r.GetPendingDispatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alert> { alert });

        var peer = new Peer
        {
            Id = Guid.NewGuid(),
            Url = "https://peer.example.com",
            WebhookToken = "wh-token-123",
            HeartbeatToken = "hb-token"
        };
        _peerRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Peer> { peer });

        // First call fails
        _dispatcher.Setup(d => d.DispatchAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("fail"));
        await _worker.DispatchPendingAlertsAsync(CancellationToken.None);

        // Second call succeeds
        _dispatcher.Setup(d => d.DispatchAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        await _worker.DispatchPendingAlertsAsync(CancellationToken.None);

        _httpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.RequestUri!.ToString() == "https://peer.example.com/webhook/wh-token-123/recover"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task DispatchSucceeds_NoPriorFailure_DoesNotNotifyPeers()
    {
        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            Type = AlertType.Failure,
            Severity = Severity.High,
            Message = "Test"
        };

        _alertRepo.Setup(r => r.GetPendingDispatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alert> { alert });
        _dispatcher.Setup(d => d.DispatchAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _peerRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Peer>());

        await _worker.DispatchPendingAlertsAsync(CancellationToken.None);

        _httpHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task DispatchFails_NoPeers_DoesNotThrow()
    {
        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            Type = AlertType.Failure,
            Severity = Severity.High,
            Message = "Test"
        };

        _alertRepo.Setup(r => r.GetPendingDispatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alert> { alert });
        _dispatcher.Setup(d => d.DispatchAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("fail"));
        _peerRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Peer>());

        await _worker.DispatchPendingAlertsAsync(CancellationToken.None); // should not throw
    }

    [Fact]
    public async Task RepeatedFailures_OnlyNotifiesOnce()
    {
        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            Type = AlertType.Failure,
            Severity = Severity.High,
            Message = "Test"
        };

        _alertRepo.Setup(r => r.GetPendingDispatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alert> { alert });
        _dispatcher.Setup(d => d.DispatchAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("fail"));

        var peer = new Peer
        {
            Id = Guid.NewGuid(),
            Url = "https://peer.example.com",
            WebhookToken = "wh-token",
            HeartbeatToken = "hb-token"
        };
        _peerRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Peer> { peer });

        // Multiple failures
        await _worker.DispatchPendingAlertsAsync(CancellationToken.None);
        await _worker.DispatchPendingAlertsAsync(CancellationToken.None);
        await _worker.DispatchPendingAlertsAsync(CancellationToken.None);

        // Should only call /fail once (on first failure)
        _httpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.RequestUri!.ToString().Contains("/fail")),
            ItExpr.IsAny<CancellationToken>());
    }
}
