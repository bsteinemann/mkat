using Microsoft.AspNetCore.Mvc;
using Mkat.Application.Interfaces;
using Mkat.Application.Services;
using Mkat.Domain.Enums;

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

        monitor.LastCheckIn = DateTime.UtcNow;
        await _monitorRepo.UpdateAsync(monitor, ct);
        await _unitOfWork.SaveChangesAsync(ct);

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
