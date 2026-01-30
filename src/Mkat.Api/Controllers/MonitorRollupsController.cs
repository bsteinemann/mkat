using Microsoft.AspNetCore.Mvc;
using Mkat.Application.DTOs;
using Mkat.Application.Interfaces;
using Mkat.Domain.Enums;

namespace Mkat.Api.Controllers;

[ApiController]
[Route("api/v1/monitors/{monitorId:guid}/rollups")]
public class MonitorRollupsController : ControllerBase
{
    private readonly IMonitorRepository _monitorRepo;
    private readonly IMonitorRollupRepository _rollupRepo;

    public MonitorRollupsController(
        IMonitorRepository monitorRepo,
        IMonitorRollupRepository rollupRepo)
    {
        _monitorRepo = monitorRepo;
        _rollupRepo = rollupRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetRollups(
        Guid monitorId,
        [FromQuery] string? granularity,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        var monitor = await _monitorRepo.GetByIdAsync(monitorId, ct);
        if (monitor == null)
            return NotFound(new { error = "Monitor not found" });

        Granularity? parsedGranularity = null;
        if (!string.IsNullOrEmpty(granularity) && Enum.TryParse<Granularity>(granularity, true, out var g))
            parsedGranularity = g;

        var rollups = await _rollupRepo.GetByMonitorIdAsync(monitorId, parsedGranularity, from, to, ct);

        var dtos = rollups.Select(r => new MonitorRollupDto
        {
            Id = r.Id,
            MonitorId = r.MonitorId,
            ServiceId = r.ServiceId,
            Granularity = r.Granularity.ToString(),
            PeriodStart = r.PeriodStart,
            Count = r.Count,
            SuccessCount = r.SuccessCount,
            FailureCount = r.FailureCount,
            Min = r.Min,
            Max = r.Max,
            Mean = r.Mean,
            Median = r.Median,
            P80 = r.P80,
            P90 = r.P90,
            P95 = r.P95,
            StdDev = r.StdDev,
            UptimePercent = r.UptimePercent
        });

        return Ok(dtos);
    }
}
