using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Mkat.Application.Interfaces;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Workers;
using Xunit;
using Monitor = Mkat.Domain.Entities.Monitor;

namespace Mkat.Api.Tests.Workers;

public class EventRetentionWorkerTests
{
    private readonly Mock<IMonitorEventRepository> _eventRepoMock;
    private readonly Mock<IMonitorRollupRepository> _rollupRepoMock;
    private readonly Mock<IMonitorRepository> _monitorRepoMock;
    private readonly Mock<IMetricReadingRepository> _readingRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly EventRetentionWorker _worker;

    public EventRetentionWorkerTests()
    {
        _eventRepoMock = new Mock<IMonitorEventRepository>();
        _rollupRepoMock = new Mock<IMonitorRollupRepository>();
        _monitorRepoMock = new Mock<IMonitorRepository>();
        _readingRepoMock = new Mock<IMetricReadingRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        var services = new ServiceCollection();
        services.AddSingleton(_eventRepoMock.Object);
        services.AddSingleton(_rollupRepoMock.Object);
        services.AddSingleton(_monitorRepoMock.Object);
        services.AddSingleton(_readingRepoMock.Object);
        services.AddSingleton(_unitOfWorkMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var loggerMock = new Mock<ILogger<EventRetentionWorker>>();
        _worker = new EventRetentionWorker(serviceProvider, loggerMock.Object);
    }

    [Fact]
    public async Task Cleanup_PurgesEventsOlderThan7Days()
    {
        _monitorRepoMock
            .Setup(r => r.GetAllMetricMonitorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor>());

        await _worker.CleanupAsync(CancellationToken.None);

        _eventRepoMock.Verify(r => r.DeleteOlderThanAsync(
            It.Is<DateTime>(d => d < DateTime.UtcNow.AddDays(-6)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cleanup_PurgesHourlyRollupsOlderThan30Days()
    {
        _monitorRepoMock
            .Setup(r => r.GetAllMetricMonitorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor>());

        await _worker.CleanupAsync(CancellationToken.None);

        _rollupRepoMock.Verify(r => r.DeleteOlderThanAsync(
            Granularity.Hourly,
            It.Is<DateTime>(d => d < DateTime.UtcNow.AddDays(-29)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cleanup_PurgesDailyRollupsOlderThan1Year()
    {
        _monitorRepoMock
            .Setup(r => r.GetAllMetricMonitorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor>());

        await _worker.CleanupAsync(CancellationToken.None);

        _rollupRepoMock.Verify(r => r.DeleteOlderThanAsync(
            Granularity.Daily,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cleanup_PurgesWeeklyRollupsOlderThan2Years()
    {
        _monitorRepoMock
            .Setup(r => r.GetAllMetricMonitorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor>());

        await _worker.CleanupAsync(CancellationToken.None);

        _rollupRepoMock.Verify(r => r.DeleteOlderThanAsync(
            Granularity.Weekly,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cleanup_DoesNotPurgeMonthlyRollups()
    {
        _monitorRepoMock
            .Setup(r => r.GetAllMetricMonitorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor>());

        await _worker.CleanupAsync(CancellationToken.None);

        _rollupRepoMock.Verify(r => r.DeleteOlderThanAsync(
            Granularity.Monthly,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cleanup_SavesChanges()
    {
        _monitorRepoMock
            .Setup(r => r.GetAllMetricMonitorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor>());

        await _worker.CleanupAsync(CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
