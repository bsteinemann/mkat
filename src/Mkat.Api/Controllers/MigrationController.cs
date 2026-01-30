using Microsoft.AspNetCore.Mvc;
using Mkat.Application.Interfaces;
using Mkat.Application.Services;

namespace Mkat.Api.Controllers;

[ApiController]
[Route("api/v1/migration")]
public class MigrationController : ControllerBase
{
    private readonly IMetricReadingRepository _readingRepo;
    private readonly IMonitorEventRepository _eventRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MigrationController> _logger;

    public MigrationController(
        IMetricReadingRepository readingRepo,
        IMonitorEventRepository eventRepo,
        IUnitOfWork unitOfWork,
        ILogger<MigrationController> logger)
    {
        _readingRepo = readingRepo;
        _eventRepo = eventRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpPost("metric-readings")]
    public async Task<IActionResult> MigrateMetricReadings(CancellationToken ct)
    {
        _logger.LogInformation("Starting MetricReading to MonitorEvent migration");

        var readings = await _readingRepo.GetByMonitorIdAsync(
            Guid.Empty, null, null, limit: int.MaxValue, ct: ct);

        // If no readings with empty Guid, get all by iterating
        // For simplicity, use the DbContext directly isn't possible here,
        // so we use the repo method with a high limit
        var allReadings = await _readingRepo.GetByMonitorIdAsync(
            Guid.Empty, null, null, limit: 0, ct: ct);

        if (allReadings.Count == 0)
        {
            return Ok(new { migrated = 0, message = "No MetricReadings found to migrate" });
        }

        var events = MetricReadingMigrator.Convert(allReadings);

        foreach (var evt in events)
        {
            await _eventRepo.AddAsync(evt, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Migrated {Count} MetricReadings to MonitorEvents", events.Count);

        return Ok(new { migrated = events.Count, message = "Migration complete" });
    }
}
