using Microsoft.Extensions.Logging;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;

namespace Mkat.Application.Services;

public interface IStateService
{
    Task<Alert?> TransitionToUpAsync(Guid serviceId, string reason, CancellationToken ct = default);
    Task<Alert?> TransitionToDownAsync(Guid serviceId, AlertType alertType, string reason, CancellationToken ct = default);
    Task PauseServiceAsync(Guid serviceId, DateTime? until, bool autoResume, CancellationToken ct = default);
    Task ResumeServiceAsync(Guid serviceId, CancellationToken ct = default);
}

public class StateService : IStateService
{
    private readonly IServiceRepository _serviceRepo;
    private readonly IAlertRepository _alertRepo;
    private readonly IMuteWindowRepository _muteRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StateService> _logger;

    public StateService(
        IServiceRepository serviceRepo,
        IAlertRepository alertRepo,
        IMuteWindowRepository muteRepo,
        IUnitOfWork unitOfWork,
        ILogger<StateService> logger)
    {
        _serviceRepo = serviceRepo;
        _alertRepo = alertRepo;
        _muteRepo = muteRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Alert?> TransitionToUpAsync(Guid serviceId, string reason, CancellationToken ct = default)
    {
        var service = await _serviceRepo.GetByIdAsync(serviceId, ct);
        if (service == null) return null;

        if (service.State == ServiceState.Paused) return null;
        if (service.State == ServiceState.Up) return null;

        var previousState = service.State;
        service.State = ServiceState.Up;
        service.PreviousState = previousState;
        await _serviceRepo.UpdateAsync(service, ct);

        _logger.LogInformation(
            "Service {ServiceId} transitioned from {PreviousState} to UP: {Reason}",
            serviceId, previousState, reason);

        if (previousState == ServiceState.Down)
        {
            var alert = new Alert
            {
                Id = Guid.NewGuid(),
                ServiceId = serviceId,
                Type = AlertType.Recovery,
                Severity = service.Severity,
                Message = $"Service '{service.Name}' recovered: {reason}",
                CreatedAt = DateTime.UtcNow
            };

            var isMuted = await _muteRepo.IsServiceMutedAsync(serviceId, DateTime.UtcNow, ct);
            if (!isMuted)
            {
                await _alertRepo.AddAsync(alert, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return isMuted ? null : alert;
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return null;
    }

    public async Task<Alert?> TransitionToDownAsync(
        Guid serviceId,
        AlertType alertType,
        string reason,
        CancellationToken ct = default)
    {
        var service = await _serviceRepo.GetByIdAsync(serviceId, ct);
        if (service == null) return null;

        if (service.State == ServiceState.Paused) return null;
        if (service.State == ServiceState.Down) return null;

        var previousState = service.State;
        service.State = ServiceState.Down;
        service.PreviousState = previousState;
        await _serviceRepo.UpdateAsync(service, ct);

        _logger.LogInformation(
            "Service {ServiceId} transitioned from {PreviousState} to DOWN: {Reason}",
            serviceId, previousState, reason);

        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            Type = alertType,
            Severity = service.Severity,
            Message = $"Service '{service.Name}' failed: {reason}",
            CreatedAt = DateTime.UtcNow
        };

        var isMuted = await _muteRepo.IsServiceMutedAsync(serviceId, DateTime.UtcNow, ct);
        if (!isMuted)
        {
            await _alertRepo.AddAsync(alert, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return isMuted ? null : alert;
    }

    public async Task PauseServiceAsync(
        Guid serviceId,
        DateTime? until,
        bool autoResume,
        CancellationToken ct = default)
    {
        var service = await _serviceRepo.GetByIdAsync(serviceId, ct);
        if (service == null) return;

        service.PreviousState = service.State;
        service.State = ServiceState.Paused;
        service.PausedUntil = until;
        service.AutoResume = autoResume;

        await _serviceRepo.UpdateAsync(service, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Service {ServiceId} paused until {PausedUntil}, autoResume={AutoResume}",
            serviceId, until, autoResume);
    }

    public async Task ResumeServiceAsync(Guid serviceId, CancellationToken ct = default)
    {
        var service = await _serviceRepo.GetByIdAsync(serviceId, ct);
        if (service == null || service.State != ServiceState.Paused) return;

        service.State = ServiceState.Unknown;
        service.PausedUntil = null;
        service.AutoResume = false;

        await _serviceRepo.UpdateAsync(service, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Service {ServiceId} resumed, state set to UNKNOWN", serviceId);
    }
}
