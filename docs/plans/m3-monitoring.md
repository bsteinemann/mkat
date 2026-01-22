# Implementation Plan: M3 Monitoring Engine

**Milestone:** 3 - Monitoring Engine
**Goal:** Webhook and heartbeat monitoring with state machine
**Dependencies:** M2 Core API

---

## 1. State Machine Service

### 1.1 State Transition Logic

**File:** `src/Mkat.Application/Services/StateService.cs`
```csharp
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

        // Skip if paused
        if (service.State == ServiceState.Paused) return null;

        // Skip if already UP
        if (service.State == ServiceState.Up) return null;

        var previousState = service.State;
        service.State = ServiceState.Up;
        service.PreviousState = previousState;
        await _serviceRepo.UpdateAsync(service, ct);

        _logger.LogInformation(
            "Service {ServiceId} transitioned from {PreviousState} to UP: {Reason}",
            serviceId, previousState, reason);

        // Create recovery alert if was DOWN
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

            // Check if muted
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

        // Skip if paused
        if (service.State == ServiceState.Paused) return null;

        // Skip if already DOWN
        if (service.State == ServiceState.Down) return null;

        var previousState = service.State;
        service.State = ServiceState.Down;
        service.PreviousState = previousState;
        await _serviceRepo.UpdateAsync(service, ct);

        _logger.LogInformation(
            "Service {ServiceId} transitioned from {PreviousState} to DOWN: {Reason}",
            serviceId, previousState, reason);

        // Create failure alert
        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            Type = alertType,
            Severity = service.Severity,
            Message = $"Service '{service.Name}' failed: {reason}",
            CreatedAt = DateTime.UtcNow
        };

        // Check if muted
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
```

---

## 2. Webhook Controller

**File:** `src/Mkat.Api/Controllers/WebhookController.cs`
```csharp
namespace Mkat.Api.Controllers;

[ApiController]
[Route("webhook")]
public class WebhookController : ControllerBase
{
    private readonly IMonitorRepository _monitorRepo;
    private readonly IStateService _stateService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IMonitorRepository monitorRepo,
        IStateService stateService,
        ILogger<WebhookController> logger)
    {
        _monitorRepo = monitorRepo;
        _stateService = stateService;
        _logger = logger;
    }

    [HttpPost("{token}/fail")]
    public async Task<IActionResult> ReportFailure(string token, CancellationToken ct = default)
    {
        var monitor = await _monitorRepo.GetByTokenAsync(token, ct);
        if (monitor == null)
        {
            _logger.LogWarning("Webhook failure received for unknown token: {Token}", token);
            return NotFound();
        }

        if (monitor.Type != MonitorType.Webhook)
        {
            return BadRequest(new { error = "Invalid monitor type for this endpoint" });
        }

        var alert = await _stateService.TransitionToDownAsync(
            monitor.ServiceId,
            AlertType.Failure,
            "Failure webhook received",
            ct);

        _logger.LogInformation(
            "Failure webhook received for service {ServiceId} via monitor {MonitorId}",
            monitor.ServiceId, monitor.Id);

        return Ok(new { received = true, alertCreated = alert != null });
    }

    [HttpPost("{token}/recover")]
    public async Task<IActionResult> ReportRecovery(string token, CancellationToken ct = default)
    {
        var monitor = await _monitorRepo.GetByTokenAsync(token, ct);
        if (monitor == null)
        {
            _logger.LogWarning("Webhook recovery received for unknown token: {Token}", token);
            return NotFound();
        }

        if (monitor.Type != MonitorType.Webhook)
        {
            return BadRequest(new { error = "Invalid monitor type for this endpoint" });
        }

        var alert = await _stateService.TransitionToUpAsync(
            monitor.ServiceId,
            "Recovery webhook received",
            ct);

        _logger.LogInformation(
            "Recovery webhook received for service {ServiceId} via monitor {MonitorId}",
            monitor.ServiceId, monitor.Id);

        return Ok(new { received = true, alertCreated = alert != null });
    }
}
```

---

## 3. Heartbeat Controller

**File:** `src/Mkat.Api/Controllers/HeartbeatController.cs`
```csharp
namespace Mkat.Api.Controllers;

[ApiController]
[Route("heartbeat")]
public class HeartbeatController : ControllerBase
{
    private readonly IMonitorRepository _monitorRepo;
    private readonly IStateService _stateService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HeartbeatController> _logger;

    public HeartbeatController(
        IMonitorRepository monitorRepo,
        IStateService stateService,
        IUnitOfWork unitOfWork,
        ILogger<HeartbeatController> logger)
    {
        _monitorRepo = monitorRepo;
        _stateService = stateService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpPost("{token}")]
    public async Task<IActionResult> RecordHeartbeat(string token, CancellationToken ct = default)
    {
        var monitor = await _monitorRepo.GetByTokenAsync(token, ct);
        if (monitor == null)
        {
            _logger.LogWarning("Heartbeat received for unknown token: {Token}", token);
            return NotFound();
        }

        if (monitor.Type != MonitorType.Heartbeat)
        {
            return BadRequest(new { error = "Invalid monitor type for this endpoint" });
        }

        // Update last check-in
        monitor.LastCheckIn = DateTime.UtcNow;
        await _monitorRepo.UpdateAsync(monitor, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Transition to UP if needed
        var alert = await _stateService.TransitionToUpAsync(
            monitor.ServiceId,
            "Heartbeat received",
            ct);

        _logger.LogDebug(
            "Heartbeat received for service {ServiceId} via monitor {MonitorId}",
            monitor.ServiceId, monitor.Id);

        var nextExpected = DateTime.UtcNow.AddSeconds(monitor.IntervalSeconds);

        return Ok(new
        {
            received = true,
            nextExpectedBefore = nextExpected,
            alertCreated = alert != null
        });
    }
}
```

---

## 4. Pause/Resume Endpoints

**Add to:** `src/Mkat.Api/Controllers/ServicesController.cs`
```csharp
[HttpPost("{id:guid}/pause")]
public async Task<IActionResult> Pause(
    Guid id,
    [FromBody] PauseRequest? request,
    CancellationToken ct = default)
{
    var service = await _serviceRepo.GetByIdAsync(id, ct);
    if (service == null)
    {
        return NotFound(new ErrorResponse
        {
            Error = "Service not found",
            Code = "SERVICE_NOT_FOUND"
        });
    }

    await _stateService.PauseServiceAsync(
        id,
        request?.Until,
        request?.AutoResume ?? false,
        ct);

    return Ok(new { paused = true, until = request?.Until });
}

[HttpPost("{id:guid}/resume")]
public async Task<IActionResult> Resume(Guid id, CancellationToken ct = default)
{
    var service = await _serviceRepo.GetByIdAsync(id, ct);
    if (service == null)
    {
        return NotFound(new ErrorResponse
        {
            Error = "Service not found",
            Code = "SERVICE_NOT_FOUND"
        });
    }

    if (service.State != ServiceState.Paused)
    {
        return BadRequest(new ErrorResponse
        {
            Error = "Service is not paused",
            Code = "SERVICE_NOT_PAUSED"
        });
    }

    await _stateService.ResumeServiceAsync(id, ct);

    return Ok(new { resumed = true });
}
```

**File:** `src/Mkat.Application/DTOs/PauseRequest.cs`
```csharp
namespace Mkat.Application.DTOs;

public record PauseRequest
{
    public DateTime? Until { get; init; }
    public bool AutoResume { get; init; }
}
```

---

## 5. Background Workers

### 5.1 Heartbeat Monitor Worker

**File:** `src/Mkat.Infrastructure/Workers/HeartbeatMonitorWorker.cs`
```csharp
namespace Mkat.Infrastructure.Workers;

public class HeartbeatMonitorWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HeartbeatMonitorWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);

    public HeartbeatMonitorWorker(
        IServiceProvider serviceProvider,
        ILogger<HeartbeatMonitorWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HeartbeatMonitorWorker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckMissedHeartbeatsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HeartbeatMonitorWorker");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("HeartbeatMonitorWorker stopping");
    }

    private async Task CheckMissedHeartbeatsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var monitorRepo = scope.ServiceProvider.GetRequiredService<IMonitorRepository>();
        var serviceRepo = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
        var stateService = scope.ServiceProvider.GetRequiredService<IStateService>();

        // Find heartbeat monitors that are overdue
        var now = DateTime.UtcNow;
        var heartbeatMonitors = await monitorRepo.GetHeartbeatMonitorsDueAsync(now, ct);

        foreach (var monitor in heartbeatMonitors)
        {
            var service = await serviceRepo.GetByIdAsync(monitor.ServiceId, ct);
            if (service == null || service.State == ServiceState.Paused)
                continue;

            // Skip if already DOWN
            if (service.State == ServiceState.Down)
                continue;

            // Calculate deadline
            var deadline = (monitor.LastCheckIn ?? monitor.CreatedAt)
                .AddSeconds(monitor.IntervalSeconds + monitor.GracePeriodSeconds);

            if (now > deadline)
            {
                _logger.LogWarning(
                    "Heartbeat missed for service {ServiceId}, last check-in: {LastCheckIn}",
                    monitor.ServiceId, monitor.LastCheckIn);

                await stateService.TransitionToDownAsync(
                    monitor.ServiceId,
                    AlertType.MissedHeartbeat,
                    $"Heartbeat missed. Last check-in: {monitor.LastCheckIn:u}",
                    ct);
            }
        }
    }
}
```

### 5.2 Maintenance Resume Worker

**File:** `src/Mkat.Infrastructure/Workers/MaintenanceResumeWorker.cs`
```csharp
namespace Mkat.Infrastructure.Workers;

public class MaintenanceResumeWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MaintenanceResumeWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(60);

    public MaintenanceResumeWorker(
        IServiceProvider serviceProvider,
        ILogger<MaintenanceResumeWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MaintenanceResumeWorker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckMaintenanceWindowsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MaintenanceResumeWorker");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("MaintenanceResumeWorker stopping");
    }

    private async Task CheckMaintenanceWindowsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var serviceRepo = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
        var stateService = scope.ServiceProvider.GetRequiredService<IStateService>();

        var now = DateTime.UtcNow;
        var pausedServices = await serviceRepo.GetPausedServicesAsync(ct);

        foreach (var service in pausedServices)
        {
            if (service.AutoResume &&
                service.PausedUntil.HasValue &&
                service.PausedUntil.Value <= now)
            {
                _logger.LogInformation(
                    "Auto-resuming service {ServiceId} after maintenance window",
                    service.Id);

                await stateService.ResumeServiceAsync(service.Id, ct);
            }
        }
    }
}
```

### 5.3 Register Workers

**Update:** `src/Mkat.Api/Program.cs`
```csharp
// Register background workers
builder.Services.AddHostedService<HeartbeatMonitorWorker>();
builder.Services.AddHostedService<MaintenanceResumeWorker>();
```

---

## 6. Additional Repository Methods

### 6.1 Monitor Repository Updates

**Add to:** `src/Mkat.Application/Interfaces/IMonitorRepository.cs`
```csharp
Task<IReadOnlyList<Monitor>> GetHeartbeatMonitorsDueAsync(DateTime now, CancellationToken ct = default);
```

**Implement in:** `src/Mkat.Infrastructure/Repositories/MonitorRepository.cs`
```csharp
public async Task<IReadOnlyList<Monitor>> GetHeartbeatMonitorsDueAsync(
    DateTime now,
    CancellationToken ct = default)
{
    return await _context.Monitors
        .Include(m => m.Service)
        .Where(m => m.Type == MonitorType.Heartbeat)
        .Where(m => m.Service.State != ServiceState.Paused)
        .ToListAsync(ct);
}
```

### 6.2 Service Repository Updates

**Add to:** `src/Mkat.Application/Interfaces/IServiceRepository.cs`
```csharp
Task<IReadOnlyList<Service>> GetPausedServicesAsync(CancellationToken ct = default);
```

**Implement in:** `src/Mkat.Infrastructure/Repositories/ServiceRepository.cs`
```csharp
public async Task<IReadOnlyList<Service>> GetPausedServicesAsync(CancellationToken ct = default)
{
    return await _context.Services
        .Where(s => s.State == ServiceState.Paused)
        .ToListAsync(ct);
}
```

### 6.3 Mute Window Repository

**File:** `src/Mkat.Application/Interfaces/IMuteWindowRepository.cs`
```csharp
namespace Mkat.Application.Interfaces;

public interface IMuteWindowRepository
{
    Task<bool> IsServiceMutedAsync(Guid serviceId, DateTime at, CancellationToken ct = default);
    Task<MuteWindow> AddAsync(MuteWindow mute, CancellationToken ct = default);
    Task<IReadOnlyList<MuteWindow>> GetActiveForServiceAsync(Guid serviceId, CancellationToken ct = default);
}
```

---

## 7. Verification Checklist

- [ ] `POST /webhook/{token}/fail` transitions service to DOWN
- [ ] `POST /webhook/{token}/recover` transitions service to UP
- [ ] `POST /heartbeat/{token}` updates LastCheckIn
- [ ] Heartbeat creates recovery alert if service was DOWN
- [ ] Missed heartbeat triggers DOWN state after interval + grace
- [ ] `POST /api/v1/services/{id}/pause` pauses service
- [ ] `POST /api/v1/services/{id}/resume` resumes service
- [ ] Paused services don't receive alerts
- [ ] Auto-resume works when maintenance window expires
- [ ] State transitions are logged
- [ ] Duplicate state transitions don't create duplicate alerts

---

## 8. Test Scenarios

### 8.1 Webhook Flow
```bash
# Create service with webhook monitor
curl -X POST http://localhost:8080/api/v1/services \
  -u admin:changeme \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test API",
    "monitors": [{"type": 0, "intervalSeconds": 300}]
  }'

# Get the token from response, then:

# Report failure
curl -X POST http://localhost:8080/webhook/{token}/fail

# Check service state (should be DOWN)
curl http://localhost:8080/api/v1/services/{id} -u admin:changeme

# Report recovery
curl -X POST http://localhost:8080/webhook/{token}/recover

# Check service state (should be UP)
curl http://localhost:8080/api/v1/services/{id} -u admin:changeme
```

### 8.2 Heartbeat Flow
```bash
# Create service with heartbeat monitor (60s interval)
curl -X POST http://localhost:8080/api/v1/services \
  -u admin:changeme \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Cron Job",
    "monitors": [{"type": 1, "intervalSeconds": 60}]
  }'

# Send heartbeat
curl -X POST http://localhost:8080/heartbeat/{token}

# Wait 2+ minutes without heartbeat
# Check service state (should be DOWN)
```

### 8.3 Pause/Resume Flow
```bash
# Pause with 1 hour maintenance window
curl -X POST http://localhost:8080/api/v1/services/{id}/pause \
  -u admin:changeme \
  -H "Content-Type: application/json" \
  -d '{"until": "2026-01-22T15:00:00Z", "autoResume": true}'

# Manual resume
curl -X POST http://localhost:8080/api/v1/services/{id}/resume \
  -u admin:changeme
```

---

## 9. Files to Create/Update

| File | Action | Purpose |
|------|--------|---------|
| `src/Mkat.Application/Services/StateService.cs` | Create | State machine logic |
| `src/Mkat.Api/Controllers/WebhookController.cs` | Create | Webhook endpoints |
| `src/Mkat.Api/Controllers/HeartbeatController.cs` | Create | Heartbeat endpoint |
| `src/Mkat.Api/Controllers/ServicesController.cs` | Update | Add pause/resume |
| `src/Mkat.Application/DTOs/PauseRequest.cs` | Create | Pause request model |
| `src/Mkat.Infrastructure/Workers/HeartbeatMonitorWorker.cs` | Create | Background worker |
| `src/Mkat.Infrastructure/Workers/MaintenanceResumeWorker.cs` | Create | Background worker |
| `src/Mkat.Application/Interfaces/IMuteWindowRepository.cs` | Create | Mute interface |
| `src/Mkat.Infrastructure/Repositories/MuteWindowRepository.cs` | Create | Mute implementation |

---

**Status:** Ready for implementation
**Estimated complexity:** High
