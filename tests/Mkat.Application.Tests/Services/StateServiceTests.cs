using Microsoft.Extensions.Logging;
using Moq;
using Mkat.Application.Interfaces;
using Mkat.Application.Services;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Xunit;

namespace Mkat.Application.Tests.Services;

public class StateServiceTests
{
    private readonly Mock<IServiceRepository> _serviceRepoMock;
    private readonly Mock<IAlertRepository> _alertRepoMock;
    private readonly Mock<IMuteWindowRepository> _muteRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly StateService _stateService;

    public StateServiceTests()
    {
        _serviceRepoMock = new Mock<IServiceRepository>();
        _alertRepoMock = new Mock<IAlertRepository>();
        _muteRepoMock = new Mock<IMuteWindowRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<ILogger<StateService>>();

        _stateService = new StateService(
            _serviceRepoMock.Object,
            _alertRepoMock.Object,
            _muteRepoMock.Object,
            _unitOfWorkMock.Object,
            loggerMock.Object);
    }

    // --- TransitionToUpAsync ---

    [Fact]
    public async Task TransitionToUpAsync_ReturnsNull_WhenServiceNotFound()
    {
        _serviceRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Service?)null);

        var result = await _stateService.TransitionToUpAsync(Guid.NewGuid(), "test");

        Assert.Null(result);
    }

    [Fact]
    public async Task TransitionToUpAsync_ReturnsNull_WhenServiceIsPaused()
    {
        var service = CreateService(ServiceState.Paused);
        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        var result = await _stateService.TransitionToUpAsync(service.Id, "test");

        Assert.Null(result);
        _serviceRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransitionToUpAsync_ReturnsNull_WhenAlreadyUp()
    {
        var service = CreateService(ServiceState.Up);
        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        var result = await _stateService.TransitionToUpAsync(service.Id, "test");

        Assert.Null(result);
        _serviceRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransitionToUpAsync_TransitionsFromUnknown_NoAlert()
    {
        var service = CreateService(ServiceState.Unknown);
        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        var result = await _stateService.TransitionToUpAsync(service.Id, "test");

        Assert.Null(result);
        Assert.Equal(ServiceState.Up, service.State);
        Assert.Equal(ServiceState.Unknown, service.PreviousState);
        _serviceRepoMock.Verify(r => r.UpdateAsync(service, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransitionToUpAsync_TransitionsFromDown_CreatesRecoveryAlert()
    {
        var service = CreateService(ServiceState.Down);
        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);
        _muteRepoMock.Setup(r => r.IsServiceMutedAsync(service.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _stateService.TransitionToUpAsync(service.Id, "recovered");

        Assert.NotNull(result);
        Assert.Equal(AlertType.Recovery, result.Type);
        Assert.Contains("recovered", result.Message);
        Assert.Equal(service.Id, result.ServiceId);
        _alertRepoMock.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransitionToUpAsync_TransitionsFromDown_SkipsAlertWhenMuted()
    {
        var service = CreateService(ServiceState.Down);
        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);
        _muteRepoMock.Setup(r => r.IsServiceMutedAsync(service.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _stateService.TransitionToUpAsync(service.Id, "recovered");

        Assert.Null(result);
        Assert.Equal(ServiceState.Up, service.State);
        _alertRepoMock.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // --- TransitionToDownAsync ---

    [Fact]
    public async Task TransitionToDownAsync_ReturnsNull_WhenServiceNotFound()
    {
        _serviceRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Service?)null);

        var result = await _stateService.TransitionToDownAsync(Guid.NewGuid(), AlertType.Failure, "test");

        Assert.Null(result);
    }

    [Fact]
    public async Task TransitionToDownAsync_ReturnsNull_WhenServiceIsPaused()
    {
        var service = CreateService(ServiceState.Paused);
        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        var result = await _stateService.TransitionToDownAsync(service.Id, AlertType.Failure, "test");

        Assert.Null(result);
        _serviceRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransitionToDownAsync_ReturnsNull_WhenAlreadyDown()
    {
        var service = CreateService(ServiceState.Down);
        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        var result = await _stateService.TransitionToDownAsync(service.Id, AlertType.Failure, "test");

        Assert.Null(result);
        _serviceRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransitionToDownAsync_TransitionsFromUp_CreatesFailureAlert()
    {
        var service = CreateService(ServiceState.Up);
        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);
        _muteRepoMock.Setup(r => r.IsServiceMutedAsync(service.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _stateService.TransitionToDownAsync(service.Id, AlertType.Failure, "connection refused");

        Assert.NotNull(result);
        Assert.Equal(AlertType.Failure, result.Type);
        Assert.Contains("connection refused", result.Message);
        Assert.Equal(ServiceState.Down, service.State);
        Assert.Equal(ServiceState.Up, service.PreviousState);
        _alertRepoMock.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransitionToDownAsync_TransitionsFromUnknown_CreatesAlert()
    {
        var service = CreateService(ServiceState.Unknown);
        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);
        _muteRepoMock.Setup(r => r.IsServiceMutedAsync(service.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _stateService.TransitionToDownAsync(service.Id, AlertType.MissedHeartbeat, "missed");

        Assert.NotNull(result);
        Assert.Equal(AlertType.MissedHeartbeat, result.Type);
        Assert.Equal(ServiceState.Down, service.State);
        Assert.Equal(ServiceState.Unknown, service.PreviousState);
    }

    [Fact]
    public async Task TransitionToDownAsync_SkipsAlertWhenMuted()
    {
        var service = CreateService(ServiceState.Up);
        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);
        _muteRepoMock.Setup(r => r.IsServiceMutedAsync(service.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _stateService.TransitionToDownAsync(service.Id, AlertType.Failure, "test");

        Assert.Null(result);
        Assert.Equal(ServiceState.Down, service.State);
        _alertRepoMock.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // --- PauseServiceAsync ---

    [Fact]
    public async Task PauseServiceAsync_DoesNothing_WhenServiceNotFound()
    {
        _serviceRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Service?)null);

        await _stateService.PauseServiceAsync(Guid.NewGuid(), null, false);

        _serviceRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PauseServiceAsync_SetsStateAndProperties()
    {
        var service = CreateService(ServiceState.Up);
        var until = DateTime.UtcNow.AddHours(1);
        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        await _stateService.PauseServiceAsync(service.Id, until, true);

        Assert.Equal(ServiceState.Paused, service.State);
        Assert.Equal(ServiceState.Up, service.PreviousState);
        Assert.Equal(until, service.PausedUntil);
        Assert.True(service.AutoResume);
        _serviceRepoMock.Verify(r => r.UpdateAsync(service, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- ResumeServiceAsync ---

    [Fact]
    public async Task ResumeServiceAsync_DoesNothing_WhenServiceNotFound()
    {
        _serviceRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Service?)null);

        await _stateService.ResumeServiceAsync(Guid.NewGuid());

        _serviceRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResumeServiceAsync_DoesNothing_WhenNotPaused()
    {
        var service = CreateService(ServiceState.Up);
        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        await _stateService.ResumeServiceAsync(service.Id);

        _serviceRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResumeServiceAsync_SetsStateToUnknown_ClearsPauseProperties()
    {
        var service = CreateService(ServiceState.Paused);
        service.PausedUntil = DateTime.UtcNow.AddHours(1);
        service.AutoResume = true;
        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        await _stateService.ResumeServiceAsync(service.Id);

        Assert.Equal(ServiceState.Unknown, service.State);
        Assert.Null(service.PausedUntil);
        Assert.False(service.AutoResume);
        _serviceRepoMock.Verify(r => r.UpdateAsync(service, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Helpers ---

    private static Service CreateService(ServiceState state)
    {
        return new Service
        {
            Id = Guid.NewGuid(),
            Name = "Test Service",
            State = state,
            Severity = Severity.Medium
        };
    }
}
