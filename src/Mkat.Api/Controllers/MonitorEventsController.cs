using Microsoft.AspNetCore.Mvc;
using Mkat.Application.DTOs;
using Mkat.Application.Interfaces;
using Mkat.Domain.Enums;

namespace Mkat.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class MonitorEventsController : ControllerBase
{
    private readonly IMonitorRepository _monitorRepo;
    private readonly IMonitorEventRepository _eventRepo;
    private readonly IServiceRepository _serviceRepo;

    public MonitorEventsController(
        IMonitorRepository monitorRepo,
        IMonitorEventRepository eventRepo,
        IServiceRepository serviceRepo)
    {
        _monitorRepo = monitorRepo;
        _eventRepo = eventRepo;
        _serviceRepo = serviceRepo;
    }

    [HttpGet("monitors/{monitorId:guid}/events")]
    public async Task<IActionResult> GetMonitorEvents(
        Guid monitorId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? eventType,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var monitor = await _monitorRepo.GetByIdAsync(monitorId, ct);
        if (monitor == null)
            return NotFound(new { error = "Monitor not found" });

        EventType? parsedType = null;
        if (!string.IsNullOrEmpty(eventType) && Enum.TryParse<EventType>(eventType, true, out var et))
            parsedType = et;

        var events = await _eventRepo.GetByMonitorIdAsync(monitorId, from, to, parsedType, limit, ct);

        var dtos = events.Select(e => new MonitorEventDto
        {
            Id = e.Id,
            MonitorId = e.MonitorId,
            ServiceId = e.ServiceId,
            EventType = e.EventType.ToString(),
            Success = e.Success,
            Value = e.Value,
            IsOutOfRange = e.IsOutOfRange,
            Message = e.Message,
            CreatedAt = e.CreatedAt
        });

        return Ok(dtos);
    }

    [HttpGet("services/{serviceId:guid}/events")]
    public async Task<IActionResult> GetServiceEvents(
        Guid serviceId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? eventType,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var service = await _serviceRepo.GetByIdAsync(serviceId, ct);
        if (service == null)
            return NotFound(new { error = "Service not found" });

        EventType? parsedType = null;
        if (!string.IsNullOrEmpty(eventType) && Enum.TryParse<EventType>(eventType, true, out var et))
            parsedType = et;

        var events = await _eventRepo.GetByServiceIdAsync(serviceId, from, to, parsedType, limit, ct);

        var dtos = events.Select(e => new MonitorEventDto
        {
            Id = e.Id,
            MonitorId = e.MonitorId,
            ServiceId = e.ServiceId,
            EventType = e.EventType.ToString(),
            Success = e.Success,
            Value = e.Value,
            IsOutOfRange = e.IsOutOfRange,
            Message = e.Message,
            CreatedAt = e.CreatedAt
        });

        return Ok(dtos);
    }
}
