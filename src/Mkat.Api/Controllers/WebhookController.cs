using Microsoft.AspNetCore.Mvc;
using Mkat.Application.Interfaces;
using Mkat.Application.Services;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;

namespace Mkat.Api.Controllers;

[ApiController]
[Route("webhook")]
public class WebhookController : ControllerBase
{
    private readonly IMonitorRepository _monitorRepo;
    private readonly IMonitorEventRepository _eventRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStateService _stateService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IMonitorRepository monitorRepo,
        IMonitorEventRepository eventRepo,
        IUnitOfWork unitOfWork,
        IStateService stateService,
        ILogger<WebhookController> logger)
    {
        _monitorRepo = monitorRepo;
        _eventRepo = eventRepo;
        _unitOfWork = unitOfWork;
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

        var failEvent = new MonitorEvent
        {
            Id = Guid.NewGuid(),
            MonitorId = monitor.Id,
            ServiceId = monitor.ServiceId,
            EventType = EventType.WebhookReceived,
            Success = false,
            Message = "Failure webhook received",
            CreatedAt = DateTime.UtcNow
        };
        await _eventRepo.AddAsync(failEvent, ct);
        await _unitOfWork.SaveChangesAsync(ct);

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

        var recoverEvent = new MonitorEvent
        {
            Id = Guid.NewGuid(),
            MonitorId = monitor.Id,
            ServiceId = monitor.ServiceId,
            EventType = EventType.WebhookReceived,
            Success = true,
            Message = "Recovery webhook received",
            CreatedAt = DateTime.UtcNow
        };
        await _eventRepo.AddAsync(recoverEvent, ct);
        await _unitOfWork.SaveChangesAsync(ct);

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
