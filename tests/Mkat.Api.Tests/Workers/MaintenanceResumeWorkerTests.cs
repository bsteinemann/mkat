using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Mkat.Application.Interfaces;
using Mkat.Application.Services;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Workers;
using Xunit;

namespace Mkat.Api.Tests.Workers;

public class MaintenanceResumeWorkerTests
{
    private readonly Mock<IServiceRepository> _serviceRepoMock;
    private readonly Mock<IStateService> _stateServiceMock;
    private readonly MaintenanceResumeWorker _worker;

    public MaintenanceResumeWorkerTests()
    {
        _serviceRepoMock = new Mock<IServiceRepository>();
        _stateServiceMock = new Mock<IStateService>();

        var serviceProvider = BuildServiceProvider();
        var loggerMock = new Mock<ILogger<MaintenanceResumeWorker>>();

        _worker = new MaintenanceResumeWorker(serviceProvider, loggerMock.Object);
    }

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_serviceRepoMock.Object);
        services.AddSingleton(_stateServiceMock.Object);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task CheckMaintenanceWindows_ResumesExpiredAutoResumeService()
    {
        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            State = ServiceState.Paused,
            AutoResume = true,
            PausedUntil = DateTime.UtcNow.AddMinutes(-5) // Expired 5 min ago
        };

        _serviceRepoMock.Setup(r => r.GetPausedServicesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Service> { service });

        await _worker.CheckMaintenanceWindowsAsync(CancellationToken.None);

        _stateServiceMock.Verify(s => s.ResumeServiceAsync(
            service.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckMaintenanceWindows_DoesNotResume_WhenAutoResumeIsFalse()
    {
        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            State = ServiceState.Paused,
            AutoResume = false,
            PausedUntil = DateTime.UtcNow.AddMinutes(-5)
        };

        _serviceRepoMock.Setup(r => r.GetPausedServicesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Service> { service });

        await _worker.CheckMaintenanceWindowsAsync(CancellationToken.None);

        _stateServiceMock.Verify(s => s.ResumeServiceAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckMaintenanceWindows_DoesNotResume_WhenWindowNotExpired()
    {
        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            State = ServiceState.Paused,
            AutoResume = true,
            PausedUntil = DateTime.UtcNow.AddHours(1) // Still in maintenance
        };

        _serviceRepoMock.Setup(r => r.GetPausedServicesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Service> { service });

        await _worker.CheckMaintenanceWindowsAsync(CancellationToken.None);

        _stateServiceMock.Verify(s => s.ResumeServiceAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckMaintenanceWindows_DoesNotResume_WhenNoPausedUntil()
    {
        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            State = ServiceState.Paused,
            AutoResume = true,
            PausedUntil = null // Indefinite pause
        };

        _serviceRepoMock.Setup(r => r.GetPausedServicesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Service> { service });

        await _worker.CheckMaintenanceWindowsAsync(CancellationToken.None);

        _stateServiceMock.Verify(s => s.ResumeServiceAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckMaintenanceWindows_HandlesMultipleServices()
    {
        var services = new List<Service>
        {
            new() { Id = Guid.NewGuid(), Name = "Expired", State = ServiceState.Paused,
                     AutoResume = true, PausedUntil = DateTime.UtcNow.AddMinutes(-10) },
            new() { Id = Guid.NewGuid(), Name = "Not expired", State = ServiceState.Paused,
                     AutoResume = true, PausedUntil = DateTime.UtcNow.AddHours(1) },
            new() { Id = Guid.NewGuid(), Name = "No auto", State = ServiceState.Paused,
                     AutoResume = false, PausedUntil = DateTime.UtcNow.AddMinutes(-5) }
        };

        _serviceRepoMock.Setup(r => r.GetPausedServicesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(services);

        await _worker.CheckMaintenanceWindowsAsync(CancellationToken.None);

        // Only the first should be resumed
        _stateServiceMock.Verify(s => s.ResumeServiceAsync(
            services[0].Id, It.IsAny<CancellationToken>()), Times.Once);
        _stateServiceMock.Verify(s => s.ResumeServiceAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckMaintenanceWindows_HandlesEmptyList()
    {
        _serviceRepoMock.Setup(r => r.GetPausedServicesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Service>());

        await _worker.CheckMaintenanceWindowsAsync(CancellationToken.None);

        _stateServiceMock.Verify(s => s.ResumeServiceAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
