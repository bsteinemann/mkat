using Microsoft.AspNetCore.Mvc;
using Mkat.Application.Interfaces;
using Mkat.Application.Services;
using Mkat.Domain.Enums;

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
