using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Mkat.Application.Interfaces;
using Mkat.Application.Services;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Workers;
using Xunit;
using Monitor = Mkat.Domain.Entities.Monitor;

namespace Mkat.Api.Tests.Workers;

public class HealthCheckWorkerTests
{
    private readonly Mock<IMonitorRepository> _monitorRepoMock;
    private readonly Mock<IServiceRepository> _serviceRepoMock;
    private readonly Mock<IStateService> _stateServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IHttpClientFactory> _httpFactoryMock;
    private readonly HealthCheckWorker _worker;

    public HealthCheckWorkerTests()
    {
        _monitorRepoMock = new Mock<IMonitorRepository>();
        _serviceRepoMock = new Mock<IServiceRepository>();
        _stateServiceMock = new Mock<IStateService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _httpFactoryMock = new Mock<IHttpClientFactory>();

        var serviceProvider = BuildServiceProvider();
        var loggerMock = new Mock<ILogger<HealthCheckWorker>>();

        _worker = new HealthCheckWorker(serviceProvider, loggerMock.Object);
    }

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_monitorRepoMock.Object);
        services.AddSingleton(_serviceRepoMock.Object);
        services.AddSingleton(_stateServiceMock.Object);
        services.AddSingleton(_unitOfWorkMock.Object);
        services.AddSingleton(_httpFactoryMock.Object);
        return services.BuildServiceProvider();
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content = "")
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            });
        var httpClient = new HttpClient(handler.Object);
        _httpFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
    }

    private static (Monitor monitor, Service service) CreateTestMonitorAndService(ServiceState state = ServiceState.Unknown)
    {
        var serviceId = Guid.NewGuid();
        var service = new Service { Id = serviceId, Name = "Test", State = state };
        var monitor = new Monitor
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            Type = MonitorType.HealthCheck,
            Token = Guid.NewGuid().ToString("N"),
            HealthCheckUrl = "https://example.com/health",
            HttpMethod = "GET",
            ExpectedStatusCodes = "200",
            TimeoutSeconds = 10,
            IntervalSeconds = 60,
            Service = service
        };
        return (monitor, service);
    }

    [Fact]
    public async Task CheckHealthChecks_HealthyEndpoint_TransitionsToUp()
    {
        var (monitor, service) = CreateTestMonitorAndService();
        SetupHttpResponse(HttpStatusCode.OK, "healthy");

        _monitorRepoMock.Setup(r => r.GetHealthCheckMonitorsDueAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor> { monitor });
        _serviceRepoMock.Setup(r => r.GetByIdAsync(monitor.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        await _worker.CheckHealthChecksAsync(CancellationToken.None);

        _stateServiceMock.Verify(s => s.TransitionToUpAsync(
            monitor.ServiceId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckHealthChecks_UnhealthyEndpoint_TransitionsToDown()
    {
        var (monitor, service) = CreateTestMonitorAndService(ServiceState.Up);
        SetupHttpResponse(HttpStatusCode.InternalServerError);

        _monitorRepoMock.Setup(r => r.GetHealthCheckMonitorsDueAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor> { monitor });
        _serviceRepoMock.Setup(r => r.GetByIdAsync(monitor.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        await _worker.CheckHealthChecksAsync(CancellationToken.None);

        _stateServiceMock.Verify(s => s.TransitionToDownAsync(
            monitor.ServiceId, AlertType.FailedHealthCheck,
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckHealthChecks_BodyRegexMismatch_TransitionsToDown()
    {
        var (monitor, service) = CreateTestMonitorAndService(ServiceState.Up);
        monitor.BodyMatchRegex = "\"status\":\\s*\"ok\"";
        SetupHttpResponse(HttpStatusCode.OK, "{\"status\": \"error\"}");

        _monitorRepoMock.Setup(r => r.GetHealthCheckMonitorsDueAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor> { monitor });
        _serviceRepoMock.Setup(r => r.GetByIdAsync(monitor.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        await _worker.CheckHealthChecksAsync(CancellationToken.None);

        _stateServiceMock.Verify(s => s.TransitionToDownAsync(
            monitor.ServiceId, AlertType.FailedHealthCheck,
            It.Is<string>(msg => msg.Contains("Body")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckHealthChecks_PausedService_Skipped()
    {
        var (monitor, service) = CreateTestMonitorAndService(ServiceState.Paused);

        _monitorRepoMock.Setup(r => r.GetHealthCheckMonitorsDueAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor> { monitor });
        _serviceRepoMock.Setup(r => r.GetByIdAsync(monitor.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        await _worker.CheckHealthChecksAsync(CancellationToken.None);

        _stateServiceMock.Verify(s => s.TransitionToDownAsync(
            It.IsAny<Guid>(), It.IsAny<AlertType>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _stateServiceMock.Verify(s => s.TransitionToUpAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
