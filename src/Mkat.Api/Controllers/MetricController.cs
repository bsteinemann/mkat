using Microsoft.AspNetCore.Mvc;
using Mkat.Application.Interfaces;
using Mkat.Application.Services;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;

namespace Mkat.Api.Controllers;

[ApiController]
[Route("metric")]
public class MetricController : ControllerBase
{
    private readonly IMonitorRepository _monitorRepo;
    private readonly IMonitorEventRepository _eventRepo;
    private readonly IMetricEvaluator _evaluator;
    private readonly IStateService _stateService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MetricController> _logger;

    public MetricController(
        IMonitorRepository monitorRepo,
        IMonitorEventRepository eventRepo,
        IMetricEvaluator evaluator,
        IStateService stateService,
        IUnitOfWork unitOfWork,
        ILogger<MetricController> logger)
    {
        _monitorRepo = monitorRepo;
        _eventRepo = eventRepo;
        _evaluator = evaluator;
        _stateService = stateService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpPost("{token}")]
    public async Task<IActionResult> SubmitMetric(
        string token,
        [FromQuery] double? value,
        [FromBody] MetricSubmitRequest? body,
        CancellationToken ct = default)
    {
        var metricValue = body?.Value ?? value;
        if (metricValue == null)
        {
            return BadRequest(new { error = "Value is required. Provide in body or query parameter." });
        }

        var monitor = await _monitorRepo.GetByTokenAsync(token, ct);
        if (monitor == null)
        {
            _logger.LogWarning("Metric received for unknown token: {Token}", token);
            return NotFound();
        }

        if (monitor.Type != MonitorType.Metric)
        {
            return BadRequest(new { error = "Invalid monitor type for this endpoint" });
        }

        var val = metricValue.Value;
        var isOutOfRange = MetricEvaluator.IsOutOfRange(val, monitor);
        var now = DateTime.UtcNow;

        // Store as MonitorEvent
        var monitorEvent = new MonitorEvent
        {
            Id = Guid.NewGuid(),
            MonitorId = monitor.Id,
            ServiceId = monitor.ServiceId,
            EventType = EventType.MetricIngested,
            Success = !isOutOfRange,
            Value = val,
            IsOutOfRange = isOutOfRange,
            CreatedAt = now
        };
        await _eventRepo.AddAsync(monitorEvent, ct);

        // Update monitor's last metric info
        monitor.LastMetricValue = val;
        monitor.LastMetricAt = now;
        await _monitorRepo.UpdateAsync(monitor, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Evaluate threshold strategy
        var isViolation = await _evaluator.EvaluateAsync(monitor, val, ct);

        if (isViolation && monitor.Service.State != ServiceState.Down)
        {
            await _stateService.TransitionToDownAsync(
                monitor.ServiceId,
                AlertType.Failure,
                $"Metric out of range (value: {val})",
                ct);
        }
        else if (!isViolation && monitor.Service.State == ServiceState.Down)
        {
            await _stateService.TransitionToUpAsync(
                monitor.ServiceId,
                $"Metric back in range (value: {val})",
                ct);
        }

        _logger.LogInformation(
            "Metric received for monitor {MonitorId}: value={Value}, outOfRange={OutOfRange}",
            monitor.Id, val, isOutOfRange);

        return Ok(new
        {
            received = true,
            value = val,
            outOfRange = isOutOfRange,
            violation = isViolation
        });
    }
}

public record MetricSubmitRequest
{
    public double? Value { get; init; }
}
