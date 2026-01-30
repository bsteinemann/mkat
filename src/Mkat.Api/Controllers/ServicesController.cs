using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Mkat.Application.DTOs;
using Mkat.Application.Interfaces;
using Mkat.Application.Services;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Monitor = Mkat.Domain.Entities.Monitor;

namespace Mkat.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly IServiceRepository _serviceRepo;
    private readonly IMuteWindowRepository _muteRepo;
    private readonly IContactRepository _contactRepo;
    private readonly IServiceDependencyRepository _depRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStateService _stateService;
    private readonly IValidator<CreateServiceRequest> _createValidator;
    private readonly IValidator<UpdateServiceRequest> _updateValidator;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(
        IServiceRepository serviceRepo,
        IMuteWindowRepository muteRepo,
        IContactRepository contactRepo,
        IServiceDependencyRepository depRepo,
        IUnitOfWork unitOfWork,
        IStateService stateService,
        IValidator<CreateServiceRequest> createValidator,
        IValidator<UpdateServiceRequest> updateValidator,
        ILogger<ServicesController> logger)
    {
        _serviceRepo = serviceRepo;
        _muteRepo = muteRepo;
        _contactRepo = contactRepo;
        _depRepo = depRepo;
        _unitOfWork = unitOfWork;
        _stateService = stateService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ServiceResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var skip = (page - 1) * pageSize;
        var services = await _serviceRepo.GetAllAsync(skip, pageSize, ct);
        var totalCount = await _serviceRepo.GetCountAsync(ct);

        var mappedItems = new List<ServiceResponse>(services.Count);
        foreach (var service in services)
        {
            mappedItems.Add(await MapToResponseAsync(service, ct));
        }

        var response = new PagedResponse<ServiceResponse>
        {
            Items = mappedItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceResponse>> GetById(Guid id, CancellationToken ct = default)
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

        return Ok(await MapToResponseAsync(service, ct));
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResponse>> Create(
        [FromBody] CreateServiceRequest request,
        CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Validation failed",
                Code = "VALIDATION_ERROR",
                Details = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }

        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Severity = request.Severity,
            State = ServiceState.Unknown
        };

        foreach (var monitorReq in request.Monitors)
        {
            var gracePeriod = monitorReq.GracePeriodSeconds
                ?? Math.Max(60, monitorReq.IntervalSeconds / 10);

            var monitor = new Monitor
            {
                Id = Guid.NewGuid(),
                ServiceId = service.Id,
                Type = monitorReq.Type,
                Token = Guid.NewGuid().ToString("N"),
                IntervalSeconds = monitorReq.IntervalSeconds,
                GracePeriodSeconds = gracePeriod
            };

            if (monitorReq.Type == MonitorType.Metric)
            {
                monitor.MinValue = monitorReq.MinValue;
                monitor.MaxValue = monitorReq.MaxValue;
                monitor.ThresholdStrategy = monitorReq.ThresholdStrategy;
                monitor.ThresholdCount = monitorReq.ThresholdCount;
                monitor.WindowSeconds = monitorReq.WindowSeconds;
                monitor.WindowSampleCount = monitorReq.WindowSampleCount;
                monitor.RetentionDays = monitorReq.RetentionDays;
            }

            if (monitorReq.Type == MonitorType.HealthCheck)
            {
                monitor.HealthCheckUrl = monitorReq.HealthCheckUrl;
                monitor.HttpMethod = monitorReq.HttpMethod ?? "GET";
                monitor.ExpectedStatusCodes = monitorReq.ExpectedStatusCodes ?? "200";
                monitor.TimeoutSeconds = monitorReq.TimeoutSeconds ?? 10;
                monitor.BodyMatchRegex = monitorReq.BodyMatchRegex;
            }

            service.Monitors.Add(monitor);
        }

        await _serviceRepo.AddAsync(service, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Created service {ServiceId} with name {ServiceName}",
            service.Id, service.Name);

        return CreatedAtAction(nameof(GetById), new { id = service.Id }, await MapToResponseAsync(service, ct));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ServiceResponse>> Update(
        Guid id,
        [FromBody] UpdateServiceRequest request,
        CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Validation failed",
                Code = "VALIDATION_ERROR",
                Details = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }

        var service = await _serviceRepo.GetByIdAsync(id, ct);
        if (service == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = "Service not found",
                Code = "SERVICE_NOT_FOUND"
            });
        }

        service.Name = request.Name;
        service.Description = request.Description;
        service.Severity = request.Severity;

        await _serviceRepo.UpdateAsync(service, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Updated service {ServiceId}", service.Id);

        return Ok(await MapToResponseAsync(service, ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
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

        await _serviceRepo.DeleteAsync(service, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted service {ServiceId}", id);

        return NoContent();
    }

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

    [HttpPost("{id:guid}/mute")]
    public async Task<IActionResult> Mute(
        Guid id,
        [FromBody] MuteRequest request,
        CancellationToken ct = default)
    {
        var service = await _serviceRepo.GetByIdAsync(id, ct);
        if (service == null)
        {
            return NotFound(new ErrorResponse { Error = "Service not found", Code = "SERVICE_NOT_FOUND" });
        }

        var mute = new MuteWindow
        {
            Id = Guid.NewGuid(),
            ServiceId = id,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddMinutes(request.DurationMinutes),
            Reason = request.Reason,
            CreatedAt = DateTime.UtcNow
        };

        await _muteRepo.AddAsync(mute, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Ok(new { muted = true, until = mute.EndsAt });
    }

    [HttpPut("{id:guid}/contacts")]
    public async Task<IActionResult> SetContacts(
        Guid id,
        [FromBody] SetServiceContactsRequest request,
        [FromServices] IValidator<SetServiceContactsRequest> validator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var service = await _serviceRepo.GetByIdAsync(id, ct);
        if (service == null)
            return NotFound();

        await _contactRepo.SetServiceContactsAsync(id, request.ContactIds, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Ok(new { assigned = request.ContactIds.Count });
    }

    [HttpGet("{id:guid}/contacts")]
    public async Task<IActionResult> GetContacts(Guid id, CancellationToken ct)
    {
        var service = await _serviceRepo.GetByIdAsync(id, ct);
        if (service == null)
            return NotFound();

        var contacts = await _contactRepo.GetByServiceIdAsync(id, ct);
        var responses = contacts.Select(c => new ContactResponse
        {
            Id = c.Id,
            Name = c.Name,
            IsDefault = c.IsDefault,
            CreatedAt = c.CreatedAt,
            Channels = c.Channels.Select(ch => new ContactChannelResponse
            {
                Id = ch.Id,
                Type = ch.Type,
                Configuration = ch.Configuration,
                IsEnabled = ch.IsEnabled,
                CreatedAt = ch.CreatedAt
            }).ToList(),
            ServiceCount = c.ServiceContacts.Count
        }).ToList();

        return Ok(responses);
    }

    // --- Dependency Endpoints ---

    [HttpPost("{id:guid}/dependencies")]
    public async Task<IActionResult> AddDependency(
        Guid id,
        [FromBody] AddDependencyRequest request,
        CancellationToken ct = default)
    {
        // Reject self-references
        if (id == request.DependencyServiceId)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "A service cannot depend on itself",
                Code = "SELF_DEPENDENCY"
            });
        }

        // Verify both services exist
        var dependentService = await _serviceRepo.GetByIdAsync(id, ct);
        if (dependentService == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = "Service not found",
                Code = "SERVICE_NOT_FOUND"
            });
        }

        var dependencyService = await _serviceRepo.GetByIdAsync(request.DependencyServiceId, ct);
        if (dependencyService == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = "Dependency service not found",
                Code = "SERVICE_NOT_FOUND"
            });
        }

        // Check for existing dependency
        var existing = await _depRepo.GetAsync(id, request.DependencyServiceId, ct);
        if (existing != null)
        {
            return Conflict(new ErrorResponse
            {
                Error = "Dependency already exists",
                Code = "DEPENDENCY_EXISTS"
            });
        }

        // Check for cycles
        var wouldCycle = await _depRepo.WouldCreateCycleAsync(id, request.DependencyServiceId, ct);
        if (wouldCycle)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Adding this dependency would create a cycle",
                Code = "DEPENDENCY_CYCLE"
            });
        }

        var dependency = new ServiceDependency
        {
            Id = Guid.NewGuid(),
            DependentServiceId = id,
            DependencyServiceId = request.DependencyServiceId,
            CreatedAt = DateTime.UtcNow
        };

        await _depRepo.AddAsync(dependency, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Added dependency: service {DependentId} depends on {DependencyId}",
            id, request.DependencyServiceId);

        var responseDto = new DependencyResponse
        {
            Id = dependencyService.Id,
            Name = dependencyService.Name
        };

        return Created($"/api/v1/services/{id}/dependencies", responseDto);
    }

    [HttpDelete("{id:guid}/dependencies/{dependencyId:guid}")]
    public async Task<IActionResult> RemoveDependency(Guid id, Guid dependencyId, CancellationToken ct = default)
    {
        var existing = await _depRepo.GetAsync(id, dependencyId, ct);
        if (existing == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = "Dependency not found",
                Code = "DEPENDENCY_NOT_FOUND"
            });
        }

        await _depRepo.DeleteAsync(existing, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Removed dependency: service {DependentId} no longer depends on {DependencyId}",
            id, dependencyId);

        return NoContent();
    }

    [HttpGet("{id:guid}/dependencies")]
    public async Task<ActionResult<List<DependencyResponse>>> GetDependencies(Guid id, CancellationToken ct = default)
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

        var dependencies = await _depRepo.GetDependenciesAsync(id, ct);
        var responses = dependencies.Select(d => new DependencyResponse
        {
            Id = d.DependencyService.Id,
            Name = d.DependencyService.Name
        }).ToList();

        return Ok(responses);
    }

    [HttpGet("{id:guid}/dependents")]
    public async Task<ActionResult<List<DependencyResponse>>> GetDependents(Guid id, CancellationToken ct = default)
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

        var dependents = await _depRepo.GetDependentsAsync(id, ct);
        var responses = dependents.Select(d => new DependencyResponse
        {
            Id = d.DependentService.Id,
            Name = d.DependentService.Name
        }).ToList();

        return Ok(responses);
    }

    [HttpGet("graph")]
    public async Task<ActionResult<DependencyGraphResponse>> GetGraph(CancellationToken ct = default)
    {
        var totalCount = await _serviceRepo.GetCountAsync(ct);
        var services = await _serviceRepo.GetAllAsync(0, Math.Max(totalCount, 1), ct);
        var allDependencies = await _depRepo.GetAllAsync(ct);

        var nodes = services.Select(s => new DependencyGraphNode
        {
            Id = s.Id,
            Name = s.Name,
            State = s.State.ToString(),
            IsSuppressed = s.IsSuppressed,
            SuppressionReason = s.SuppressionReason
        }).ToList();

        var edges = allDependencies.Select(d => new DependencyGraphEdge
        {
            DependentId = d.DependentServiceId,
            DependencyId = d.DependencyServiceId
        }).ToList();

        return Ok(new DependencyGraphResponse
        {
            Nodes = nodes,
            Edges = edges
        });
    }

    internal static MonitorResponse MapMonitorToResponse(Monitor monitor, string baseUrl)
    {
        return new MonitorResponse
        {
            Id = monitor.Id,
            Type = monitor.Type,
            Token = monitor.Token,
            IntervalSeconds = monitor.IntervalSeconds,
            GracePeriodSeconds = monitor.GracePeriodSeconds,
            LastCheckIn = monitor.LastCheckIn,
            WebhookFailUrl = $"{baseUrl}/webhook/{monitor.Token}/fail",
            WebhookRecoverUrl = $"{baseUrl}/webhook/{monitor.Token}/recover",
            HeartbeatUrl = $"{baseUrl}/heartbeat/{monitor.Token}",
            MetricUrl = $"{baseUrl}/metric/{monitor.Token}",
            MinValue = monitor.MinValue,
            MaxValue = monitor.MaxValue,
            ThresholdStrategy = monitor.Type == MonitorType.Metric ? monitor.ThresholdStrategy : null,
            ThresholdCount = monitor.ThresholdCount,
            WindowSeconds = monitor.WindowSeconds,
            WindowSampleCount = monitor.WindowSampleCount,
            RetentionDays = monitor.Type == MonitorType.Metric ? monitor.RetentionDays : null,
            LastMetricValue = monitor.LastMetricValue,
            LastMetricAt = monitor.LastMetricAt,
            HealthCheckUrl = monitor.HealthCheckUrl,
            HttpMethod = monitor.HttpMethod,
            ExpectedStatusCodes = monitor.ExpectedStatusCodes,
            TimeoutSeconds = monitor.TimeoutSeconds,
            BodyMatchRegex = monitor.BodyMatchRegex
        };
    }

    private async Task<ServiceResponse> MapToResponseAsync(Service service, CancellationToken ct)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

        var dependencies = await _depRepo.GetDependenciesAsync(service.Id, ct);
        var dependents = await _depRepo.GetDependentsAsync(service.Id, ct);

        return new ServiceResponse
        {
            Id = service.Id,
            Name = service.Name,
            Description = service.Description,
            State = service.State,
            Severity = service.Severity,
            PausedUntil = service.PausedUntil,
            CreatedAt = service.CreatedAt,
            UpdatedAt = service.UpdatedAt,
            IsSuppressed = service.IsSuppressed,
            SuppressionReason = service.SuppressionReason,
            DependsOn = dependencies.Select(d => new DependencyResponse
            {
                Id = d.DependencyService.Id,
                Name = d.DependencyService.Name
            }).ToList(),
            DependedOnBy = dependents.Select(d => new DependencyResponse
            {
                Id = d.DependentService.Id,
                Name = d.DependentService.Name
            }).ToList(),
            Monitors = service.Monitors.Select(m => MapMonitorToResponse(m, baseUrl)).ToList()
        };
    }
}
