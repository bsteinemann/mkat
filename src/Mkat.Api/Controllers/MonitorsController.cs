using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Mkat.Application.DTOs;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Monitor = Mkat.Domain.Entities.Monitor;

namespace Mkat.Api.Controllers;

[ApiController]
[Route("api/v1/services/{serviceId:guid}/monitors")]
public class MonitorsController : ControllerBase
{
    private readonly IServiceRepository _serviceRepo;
    private readonly IMonitorRepository _monitorRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<AddMonitorRequest> _addValidator;
    private readonly IValidator<UpdateMonitorRequest> _updateValidator;
    private readonly ILogger<MonitorsController> _logger;

    public MonitorsController(
        IServiceRepository serviceRepo,
        IMonitorRepository monitorRepo,
        IUnitOfWork unitOfWork,
        IValidator<AddMonitorRequest> addValidator,
        IValidator<UpdateMonitorRequest> updateValidator,
        ILogger<MonitorsController> logger)
    {
        _serviceRepo = serviceRepo;
        _monitorRepo = monitorRepo;
        _unitOfWork = unitOfWork;
        _addValidator = addValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<MonitorResponse>> Add(
        Guid serviceId,
        [FromBody] AddMonitorRequest request,
        CancellationToken ct = default)
    {
        var validation = await _addValidator.ValidateAsync(request, ct);
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

        var service = await _serviceRepo.GetByIdAsync(serviceId, ct);
        if (service == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = "Service not found",
                Code = "SERVICE_NOT_FOUND"
            });
        }

        var gracePeriod = request.GracePeriodSeconds
            ?? Math.Max(60, request.IntervalSeconds / 10);

        var monitor = new Monitor
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            Type = request.Type,
            Token = Guid.NewGuid().ToString("N"),
            IntervalSeconds = request.IntervalSeconds,
            GracePeriodSeconds = gracePeriod
        };

        if (request.Type == Domain.Enums.MonitorType.Metric)
        {
            monitor.MinValue = request.MinValue;
            monitor.MaxValue = request.MaxValue;
            monitor.ThresholdStrategy = request.ThresholdStrategy;
            monitor.ThresholdCount = request.ThresholdCount;
            monitor.WindowSeconds = request.WindowSeconds;
            monitor.WindowSampleCount = request.WindowSampleCount;
            monitor.RetentionDays = request.RetentionDays;
        }

        await _monitorRepo.AddAsync(monitor, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Added monitor {MonitorId} to service {ServiceId}",
            monitor.Id, serviceId);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return CreatedAtAction(nameof(Add), new { serviceId, monitorId = monitor.Id }, MapToResponse(monitor, baseUrl));
    }

    [HttpPut("{monitorId:guid}")]
    public async Task<ActionResult<MonitorResponse>> Update(
        Guid serviceId,
        Guid monitorId,
        [FromBody] UpdateMonitorRequest request,
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

        var service = await _serviceRepo.GetByIdAsync(serviceId, ct);
        if (service == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = "Service not found",
                Code = "SERVICE_NOT_FOUND"
            });
        }

        var monitor = await _monitorRepo.GetByIdAsync(monitorId, ct);
        if (monitor == null || monitor.ServiceId != serviceId)
        {
            return NotFound(new ErrorResponse
            {
                Error = "Monitor not found",
                Code = "MONITOR_NOT_FOUND"
            });
        }

        monitor.IntervalSeconds = request.IntervalSeconds;
        monitor.GracePeriodSeconds = request.GracePeriodSeconds
            ?? Math.Max(60, request.IntervalSeconds / 10);

        if (monitor.Type == Domain.Enums.MonitorType.Metric)
        {
            monitor.MinValue = request.MinValue;
            monitor.MaxValue = request.MaxValue;
            if (request.ThresholdStrategy.HasValue)
                monitor.ThresholdStrategy = request.ThresholdStrategy.Value;
            monitor.ThresholdCount = request.ThresholdCount;
            monitor.WindowSeconds = request.WindowSeconds;
            monitor.WindowSampleCount = request.WindowSampleCount;
            if (request.RetentionDays.HasValue)
                monitor.RetentionDays = request.RetentionDays.Value;
        }

        await _monitorRepo.UpdateAsync(monitor, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Updated monitor {MonitorId} on service {ServiceId}",
            monitorId, serviceId);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Ok(MapToResponse(monitor, baseUrl));
    }

    [HttpDelete("{monitorId:guid}")]
    public async Task<IActionResult> Delete(
        Guid serviceId,
        Guid monitorId,
        CancellationToken ct = default)
    {
        var service = await _serviceRepo.GetByIdAsync(serviceId, ct);
        if (service == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = "Service not found",
                Code = "SERVICE_NOT_FOUND"
            });
        }

        var monitor = await _monitorRepo.GetByIdAsync(monitorId, ct);
        if (monitor == null || monitor.ServiceId != serviceId)
        {
            return NotFound(new ErrorResponse
            {
                Error = "Monitor not found",
                Code = "MONITOR_NOT_FOUND"
            });
        }

        var monitors = await _monitorRepo.GetByServiceIdAsync(serviceId, ct);
        if (monitors.Count <= 1)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Cannot delete the last monitor on a service",
                Code = "LAST_MONITOR"
            });
        }

        await _monitorRepo.DeleteAsync(monitor, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted monitor {MonitorId} from service {ServiceId}",
            monitorId, serviceId);

        return NoContent();
    }

    private static MonitorResponse MapToResponse(Monitor monitor, string baseUrl)
    {
        return ServicesController.MapMonitorToResponse(monitor, baseUrl);
    }
}
