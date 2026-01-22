using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Mkat.Application.DTOs;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Monitor = Mkat.Domain.Entities.Monitor;

namespace Mkat.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly IServiceRepository _serviceRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateServiceRequest> _createValidator;
    private readonly IValidator<UpdateServiceRequest> _updateValidator;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(
        IServiceRepository serviceRepo,
        IUnitOfWork unitOfWork,
        IValidator<CreateServiceRequest> createValidator,
        IValidator<UpdateServiceRequest> updateValidator,
        ILogger<ServicesController> logger)
    {
        _serviceRepo = serviceRepo;
        _unitOfWork = unitOfWork;
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

        var response = new PagedResponse<ServiceResponse>
        {
            Items = services.Select(MapToResponse).ToList(),
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

        return Ok(MapToResponse(service));
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

            service.Monitors.Add(new Monitor
            {
                Id = Guid.NewGuid(),
                ServiceId = service.Id,
                Type = monitorReq.Type,
                Token = Guid.NewGuid().ToString("N"),
                IntervalSeconds = monitorReq.IntervalSeconds,
                GracePeriodSeconds = gracePeriod
            });
        }

        await _serviceRepo.AddAsync(service, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Created service {ServiceId} with name {ServiceName}",
            service.Id, service.Name);

        return CreatedAtAction(nameof(GetById), new { id = service.Id }, MapToResponse(service));
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

        return Ok(MapToResponse(service));
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

    private ServiceResponse MapToResponse(Service service)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

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
            Monitors = service.Monitors.Select(m => new MonitorResponse
            {
                Id = m.Id,
                Type = m.Type,
                Token = m.Token,
                IntervalSeconds = m.IntervalSeconds,
                GracePeriodSeconds = m.GracePeriodSeconds,
                LastCheckIn = m.LastCheckIn,
                WebhookFailUrl = $"{baseUrl}/webhook/{m.Token}/fail",
                WebhookRecoverUrl = $"{baseUrl}/webhook/{m.Token}/recover",
                HeartbeatUrl = $"{baseUrl}/heartbeat/{m.Token}"
            }).ToList()
        };
    }
}
