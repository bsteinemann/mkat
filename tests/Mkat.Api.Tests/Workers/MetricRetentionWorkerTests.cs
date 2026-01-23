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

public class MetricRetentionWorkerTests
{
    private readonly Mock<IMonitorRepository> _monitorRepo;
    private readonly Mock<IMetricReadingRepository> _readingRepo;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ILogger<MetricRetentionWorker>> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly MetricRetentionWorker _worker;

    public MetricRetentionWorkerTests()
    {
        _monitorRepo = new Mock<IMonitorRepository>();
        _readingRepo = new Mock<IMetricReadingRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<ILogger<MetricRetentionWorker>>();

        var services = new ServiceCollection();
        services.AddSingleton(_monitorRepo.Object);
        services.AddSingleton(_readingRepo.Object);
        services.AddSingleton(_unitOfWork.Object);
        _serviceProvider = services.BuildServiceProvider();

        _worker = new MetricRetentionWorker(_serviceProvider, _logger.Object);
    }

    [Fact]
    public async Task CleanupAsync_DeletesOldReadings()
    {
        var monitorId = Guid.NewGuid();
        var monitors = new List<Monitor>
        {
            new()
            {
                Id = monitorId,
                Type = MonitorType.Metric,
                RetentionDays = 7,
                Token = "test-token"
            }
        };

        _monitorRepo.Setup(r => r.GetAllMetricMonitorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(monitors);

        await _worker.CleanupAsync(CancellationToken.None);

        _readingRepo.Verify(r => r.DeleteOlderThanAsync(
            monitorId,
            It.Is<DateTime>(d => d < DateTime.UtcNow.AddDays(-6) && d > DateTime.UtcNow.AddDays(-8)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CleanupAsync_UsesMonitorRetentionDays()
    {
        var monitorId = Guid.NewGuid();
        var monitors = new List<Monitor>
        {
            new()
            {
                Id = monitorId,
                Type = MonitorType.Metric,
                RetentionDays = 30,
                Token = "test-token"
            }
        };

        _monitorRepo.Setup(r => r.GetAllMetricMonitorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(monitors);

        await _worker.CleanupAsync(CancellationToken.None);

        _readingRepo.Verify(r => r.DeleteOlderThanAsync(
            monitorId,
            It.Is<DateTime>(d => d < DateTime.UtcNow.AddDays(-29) && d > DateTime.UtcNow.AddDays(-31)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CleanupAsync_NoMetricMonitors_DoesNothing()
    {
        _monitorRepo.Setup(r => r.GetAllMetricMonitorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor>());

        await _worker.CleanupAsync(CancellationToken.None);

        _readingRepo.Verify(r => r.DeleteOlderThanAsync(
            It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CleanupAsync_MultipleMonitors_CleansEach()
    {
        var monitors = new List<Monitor>
        {
            new() { Id = Guid.NewGuid(), Type = MonitorType.Metric, RetentionDays = 7, Token = "t1" },
            new() { Id = Guid.NewGuid(), Type = MonitorType.Metric, RetentionDays = 14, Token = "t2" },
            new() { Id = Guid.NewGuid(), Type = MonitorType.Metric, RetentionDays = 30, Token = "t3" }
        };

        _monitorRepo.Setup(r => r.GetAllMetricMonitorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(monitors);

        await _worker.CleanupAsync(CancellationToken.None);

        _readingRepo.Verify(r => r.DeleteOlderThanAsync(
            It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task CleanupAsync_SavesChanges()
    {
        var monitors = new List<Monitor>
        {
            new() { Id = Guid.NewGuid(), Type = MonitorType.Metric, RetentionDays = 7, Token = "t1" }
        };

        _monitorRepo.Setup(r => r.GetAllMetricMonitorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(monitors);

        await _worker.CleanupAsync(CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
