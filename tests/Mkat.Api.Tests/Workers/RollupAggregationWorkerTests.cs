using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Workers;
using Xunit;
using Monitor = Mkat.Domain.Entities.Monitor;

namespace Mkat.Api.Tests.Workers;

public class RollupAggregationWorkerTests
{
    private readonly Mock<IMonitorRepository> _monitorRepoMock;
    private readonly Mock<IMonitorEventRepository> _eventRepoMock;
    private readonly Mock<IMonitorRollupRepository> _rollupRepoMock;
    private readonly Mock<IRollupCalculator> _calculatorMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly RollupAggregationWorker _worker;

    public RollupAggregationWorkerTests()
    {
        _monitorRepoMock = new Mock<IMonitorRepository>();
        _eventRepoMock = new Mock<IMonitorEventRepository>();
        _rollupRepoMock = new Mock<IMonitorRollupRepository>();
        _calculatorMock = new Mock<IRollupCalculator>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        var serviceProvider = BuildServiceProvider();
        var loggerMock = new Mock<ILogger<RollupAggregationWorker>>();

        _worker = new RollupAggregationWorker(serviceProvider, loggerMock.Object);
    }

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_monitorRepoMock.Object);
        services.AddSingleton(_eventRepoMock.Object);
        services.AddSingleton(_rollupRepoMock.Object);
        services.AddSingleton(_calculatorMock.Object);
        services.AddSingleton(_unitOfWorkMock.Object);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task ComputeRollups_NoMonitors_SavesAndReturns()
    {
        _monitorRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor>());

        await _worker.ComputeRollupsAsync(CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ComputeRollups_WithEvents_ComputesHourlyRollup()
    {
        var monitorId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var monitor = new Monitor
        {
            Id = monitorId,
            ServiceId = serviceId,
            Type = MonitorType.HealthCheck,
            Token = "test"
        };

        _monitorRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor> { monitor });

        var events = new List<MonitorEvent>
        {
            new() { Id = Guid.NewGuid(), MonitorId = monitorId, ServiceId = serviceId, EventType = EventType.HealthCheckPerformed, Success = true, Value = 100, CreatedAt = DateTime.UtcNow.AddMinutes(-30) }
        };

        _eventRepoMock
            .Setup(r => r.GetByMonitorIdInWindowAsync(monitorId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        _rollupRepoMock
            .Setup(r => r.GetByMonitorIdAsync(monitorId, It.IsAny<Granularity?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MonitorRollup>());

        var rollup = new MonitorRollup { Id = Guid.NewGuid(), MonitorId = monitorId, ServiceId = serviceId };
        _calculatorMock
            .Setup(c => c.Compute(events, monitorId, serviceId, Granularity.Hourly, It.IsAny<DateTime>()))
            .Returns(rollup);

        await _worker.ComputeRollupsAsync(CancellationToken.None);

        _rollupRepoMock.Verify(r => r.UpsertAsync(rollup, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ComputeRollups_NoEventsInWindow_SkipsRollup()
    {
        var monitorId = Guid.NewGuid();
        var monitor = new Monitor
        {
            Id = monitorId,
            ServiceId = Guid.NewGuid(),
            Type = MonitorType.HealthCheck,
            Token = "test"
        };

        _monitorRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor> { monitor });

        _eventRepoMock
            .Setup(r => r.GetByMonitorIdInWindowAsync(monitorId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MonitorEvent>());

        _rollupRepoMock
            .Setup(r => r.GetByMonitorIdAsync(monitorId, It.IsAny<Granularity?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MonitorRollup>());

        await _worker.ComputeRollupsAsync(CancellationToken.None);

        _calculatorMock.Verify(
            c => c.Compute(It.IsAny<IReadOnlyList<MonitorEvent>>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Granularity>(), It.IsAny<DateTime>()),
            Times.Never);
    }
}
