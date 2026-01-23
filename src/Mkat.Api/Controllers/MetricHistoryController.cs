using Microsoft.AspNetCore.Mvc;
using Mkat.Application.Interfaces;
using Mkat.Domain.Enums;

namespace Mkat.Api.Controllers;

[ApiController]
[Route("api/v1/monitors/{monitorId:guid}/metrics")]
public class MetricHistoryController : ControllerBase
{
    private readonly IMonitorRepository _monitorRepo;
    private readonly IMetricReadingRepository _readingRepo;
    private readonly ILogger<MetricHistoryController> _logger;

    public MetricHistoryController(
        IMonitorRepository monitorRepo,
        IMetricReadingRepository readingRepo,
        ILogger<MetricHistoryController> logger)
    {
        _monitorRepo = monitorRepo;
        _readingRepo = readingRepo;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetHistory(
        Guid monitorId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var monitor = await _monitorRepo.GetByIdAsync(monitorId, ct);
        if (monitor == null)
        {
            return NotFound(new { error = "Monitor not found", code = "MONITOR_NOT_FOUND" });
        }

        if (monitor.Type != MonitorType.Metric)
        {
            return BadRequest(new { error = "Monitor is not a metric monitor", code = "INVALID_MONITOR_TYPE" });
        }

        var readings = await _readingRepo.GetByMonitorIdAsync(monitorId, from, to, limit, ct);

        return Ok(new
        {
            monitorId,
            readings = readings.Select(r => new
            {
                id = r.Id,
                value = r.Value,
                recordedAt = r.RecordedAt,
                outOfRange = r.IsOutOfRange
            })
        });
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest(
        Guid monitorId,
        CancellationToken ct = default)
    {
        var monitor = await _monitorRepo.GetByIdAsync(monitorId, ct);
        if (monitor == null)
        {
            return NotFound(new { error = "Monitor not found", code = "MONITOR_NOT_FOUND" });
        }

        if (monitor.Type != MonitorType.Metric)
        {
            return BadRequest(new { error = "Monitor is not a metric monitor", code = "INVALID_MONITOR_TYPE" });
        }

        var reading = await _readingRepo.GetLatestByMonitorIdAsync(monitorId, ct);
        if (reading == null)
        {
            return NoContent();
        }

        return Ok(new
        {
            value = reading.Value,
            recordedAt = reading.RecordedAt,
            outOfRange = reading.IsOutOfRange,
            monitorId
        });
    }
}
