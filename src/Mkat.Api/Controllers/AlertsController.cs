using Microsoft.AspNetCore.Mvc;
using Mkat.Application.DTOs;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;

namespace Mkat.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly IAlertRepository _alertRepo;
    private readonly IUnitOfWork _unitOfWork;

    public AlertsController(
        IAlertRepository alertRepo,
        IUnitOfWork unitOfWork)
    {
        _alertRepo = alertRepo;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<AlertResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var skip = (page - 1) * pageSize;
        var alerts = await _alertRepo.GetAllAsync(skip, pageSize, ct);
        var totalCount = await _alertRepo.GetCountAsync(ct);

        return Ok(new PagedResponse<AlertResponse>
        {
            Items = alerts.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AlertResponse>> GetById(Guid id, CancellationToken ct = default)
    {
        var alert = await _alertRepo.GetByIdAsync(id, ct);
        if (alert == null)
        {
            return NotFound(new ErrorResponse { Error = "Alert not found", Code = "ALERT_NOT_FOUND" });
        }

        return Ok(MapToResponse(alert));
    }

    [HttpPost("{id:guid}/ack")]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken ct = default)
    {
        var alert = await _alertRepo.GetByIdAsync(id, ct);
        if (alert == null)
        {
            return NotFound(new ErrorResponse { Error = "Alert not found", Code = "ALERT_NOT_FOUND" });
        }

        if (alert.AcknowledgedAt.HasValue)
        {
            return BadRequest(new ErrorResponse { Error = "Alert already acknowledged", Code = "ALREADY_ACKNOWLEDGED" });
        }

        alert.AcknowledgedAt = DateTime.UtcNow;
        await _alertRepo.UpdateAsync(alert, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Ok(new { acknowledged = true });
    }

    private static AlertResponse MapToResponse(Alert alert) => new()
    {
        Id = alert.Id,
        ServiceId = alert.ServiceId,
        Type = alert.Type,
        Severity = alert.Severity,
        Message = alert.Message,
        CreatedAt = alert.CreatedAt,
        AcknowledgedAt = alert.AcknowledgedAt,
        DispatchedAt = alert.DispatchedAt
    };
}
