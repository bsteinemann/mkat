using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Mkat.Application.Interfaces;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Workers;
using Xunit;

namespace Mkat.Api.Tests.Workers;

public class EventRetentionWorkerTests
{
    private readonly Mock<IMonitorEventRepository> _eventRepoMock;
    private readonly Mock<IMonitorRollupRepository> _rollupRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly EventRetentionWorker _worker;

    public EventRetentionWorkerTests()
    {
        _eventRepoMock = new Mock<IMonitorEventRepository>();
        _rollupRepoMock = new Mock<IMonitorRollupRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        var services = new ServiceCollection();
        services.AddSingleton(_eventRepoMock.Object);
        services.AddSingleton(_rollupRepoMock.Object);
        services.AddSingleton(_unitOfWorkMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var loggerMock = new Mock<ILogger<EventRetentionWorker>>();
        _worker = new EventRetentionWorker(serviceProvider, loggerMock.Object);
    }

    [Fact]
    public async Task Cleanup_PurgesEventsOlderThan7Days()
    {
        await _worker.CleanupAsync(CancellationToken.None);

        _eventRepoMock.Verify(r => r.DeleteOlderThanAsync(
            It.Is<DateTime>(d => d < DateTime.UtcNow.AddDays(-6)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cleanup_PurgesHourlyRollupsOlderThan30Days()
    {
        await _worker.CleanupAsync(CancellationToken.None);

        _rollupRepoMock.Verify(r => r.DeleteOlderThanAsync(
            Granularity.Hourly,
            It.Is<DateTime>(d => d < DateTime.UtcNow.AddDays(-29)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cleanup_PurgesDailyRollupsOlderThan1Year()
    {
        await _worker.CleanupAsync(CancellationToken.None);

        _rollupRepoMock.Verify(r => r.DeleteOlderThanAsync(
            Granularity.Daily,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cleanup_PurgesWeeklyRollupsOlderThan2Years()
    {
        await _worker.CleanupAsync(CancellationToken.None);

        _rollupRepoMock.Verify(r => r.DeleteOlderThanAsync(
            Granularity.Weekly,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cleanup_DoesNotPurgeMonthlyRollups()
    {
        await _worker.CleanupAsync(CancellationToken.None);

        _rollupRepoMock.Verify(r => r.DeleteOlderThanAsync(
            Granularity.Monthly,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cleanup_SavesChanges()
    {
        await _worker.CleanupAsync(CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
