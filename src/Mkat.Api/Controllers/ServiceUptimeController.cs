using Microsoft.AspNetCore.Mvc;
using Mkat.Application.DTOs;
using Mkat.Application.Interfaces;

namespace Mkat.Api.Controllers;

[ApiController]
[Route("api/v1/services/{serviceId:guid}/uptime")]
public class ServiceUptimeController : ControllerBase
{
    private readonly IServiceRepository _serviceRepo;
    private readonly IMonitorEventRepository _eventRepo;

    public ServiceUptimeController(
        IServiceRepository serviceRepo,
        IMonitorEventRepository eventRepo)
    {
        _serviceRepo = serviceRepo;
        _eventRepo = eventRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetUptime(
        Guid serviceId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        var service = await _serviceRepo.GetByIdAsync(serviceId, ct);
        if (service == null)
            return NotFound(new { error = "Service not found" });

        var rangeFrom = from ?? DateTime.UtcNow.AddDays(-30);
        var rangeTo = to ?? DateTime.UtcNow;

        var events = await _eventRepo.GetByServiceIdAsync(serviceId, rangeFrom, rangeTo, null, int.MaxValue, ct);

        var total = events.Count;
        var successCount = events.Count(e => e.Success);
        var failureCount = total - successCount;
        var uptimePercent = total > 0 ? Math.Round((double)successCount / total * 100, 2) : 0;

        return Ok(new ServiceUptimeDto
        {
            ServiceId = serviceId,
            UptimePercent = uptimePercent,
            TotalEvents = total,
            SuccessEvents = successCount,
            FailureEvents = failureCount,
            From = rangeFrom,
            To = rangeTo
        });
    }
}
