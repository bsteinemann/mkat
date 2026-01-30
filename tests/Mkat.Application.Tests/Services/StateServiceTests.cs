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
    private readonly Mock<IServiceDependencyRepository> _depRepoMock;
    private readonly StateService _stateService;

    public StateServiceTests()
    {
        _serviceRepoMock = new Mock<IServiceRepository>();
        _alertRepoMock = new Mock<IAlertRepository>();
        _muteRepoMock = new Mock<IMuteWindowRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _depRepoMock = new Mock<IServiceDependencyRepository>();
        _depRepoMock.Setup(r => r.GetTransitiveDependentIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());
        _depRepoMock.Setup(r => r.GetTransitiveDependencyIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());
        var loggerMock = new Mock<ILogger<StateService>>();

        _stateService = new StateService(
            _serviceRepoMock.Object,
            _alertRepoMock.Object,
            _muteRepoMock.Object,
            _depRepoMock.Object,
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

    // --- Suppression ---

    [Fact]
    public async Task TransitionToDown_SuppressesTransitiveDependents()
    {
        var root = CreateService(ServiceState.Up);
        var dependentId = Guid.NewGuid();
        var dependent = CreateService(ServiceState.Up, dependentId);

        _serviceRepoMock.Setup(r => r.GetByIdAsync(root.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(root);
        _serviceRepoMock.Setup(r => r.GetByIdAsync(dependentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dependent);
        _muteRepoMock.Setup(r => r.IsServiceMutedAsync(root.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _depRepoMock.Setup(r => r.GetTransitiveDependentIdsAsync(root.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { dependentId });

        await _stateService.TransitionToDownAsync(root.Id, AlertType.Failure, "root failed");

        Assert.True(dependent.IsSuppressed);
        Assert.NotNull(dependent.SuppressionReason);
        Assert.Contains(root.Name, dependent.SuppressionReason, StringComparison.Ordinal);
        _serviceRepoMock.Verify(r => r.UpdateAsync(dependent, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransitionToDown_DoesNotAlertSuppressedService()
    {
        var service = CreateService(ServiceState.Up);
        service.IsSuppressed = true;
        service.SuppressionReason = "Dependency down: SomeRoot";

        _serviceRepoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);
        _depRepoMock.Setup(r => r.GetTransitiveDependentIdsAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        var result = await _stateService.TransitionToDownAsync(service.Id, AlertType.Failure, "suppressed fail");

        Assert.Null(result);
        Assert.Equal(ServiceState.Down, service.State);
        _alertRepoMock.Verify(r => r.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransitionToUp_ClearsSuppression_WhenAllDependenciesUp()
    {
        var root = CreateService(ServiceState.Down);
        var dependentId = Guid.NewGuid();
        var dependent = CreateService(ServiceState.Down, dependentId);
        dependent.IsSuppressed = true;
        dependent.SuppressionReason = "Dependency down: Test Service";

        _serviceRepoMock.Setup(r => r.GetByIdAsync(root.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(root);
        _serviceRepoMock.Setup(r => r.GetByIdAsync(dependentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dependent);
        _muteRepoMock.Setup(r => r.IsServiceMutedAsync(root.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _depRepoMock.Setup(r => r.GetTransitiveDependentIdsAsync(root.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { dependentId });
        _depRepoMock.Setup(r => r.GetTransitiveDependencyIdsAsync(dependentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { root.Id });

        await _stateService.TransitionToUpAsync(root.Id, "root recovered");

        Assert.False(dependent.IsSuppressed);
        Assert.Null(dependent.SuppressionReason);
        _serviceRepoMock.Verify(r => r.UpdateAsync(dependent, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransitionToUp_KeepsSuppression_WhenOtherDependencyStillDown()
    {
        var root = CreateService(ServiceState.Down);
        var otherId = Guid.NewGuid();
        var other = CreateService(ServiceState.Down, otherId);
        other.Name = "Other Service";

        var dependentId = Guid.NewGuid();
        var dependent = CreateService(ServiceState.Up, dependentId);
        dependent.IsSuppressed = true;
        dependent.SuppressionReason = "Dependency down: Test Service";

        _serviceRepoMock.Setup(r => r.GetByIdAsync(root.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(root);
        _serviceRepoMock.Setup(r => r.GetByIdAsync(dependentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dependent);
        _serviceRepoMock.Setup(r => r.GetByIdAsync(otherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(other);
        _muteRepoMock.Setup(r => r.IsServiceMutedAsync(root.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _depRepoMock.Setup(r => r.GetTransitiveDependentIdsAsync(root.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { dependentId });
        _depRepoMock.Setup(r => r.GetTransitiveDependencyIdsAsync(dependentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { root.Id, otherId });

        await _stateService.TransitionToUpAsync(root.Id, "root recovered");

        Assert.True(dependent.IsSuppressed);
        Assert.NotNull(dependent.SuppressionReason);
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

    private static Service CreateService(ServiceState state, Guid id)
    {
        return new Service
        {
            Id = id,
            Name = "Test Service",
            State = state,
            Severity = Severity.Medium
        };
    }
}
