using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Mkat.Application.DTOs;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Infrastructure.Channels;

namespace Mkat.Api.Controllers;

[ApiController]
[Route("api/v1/push")]
public class PushController : ControllerBase
{
    private readonly IPushSubscriptionRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly VapidOptions _vapidOptions;

    public PushController(
        IPushSubscriptionRepository repo,
        IUnitOfWork unitOfWork,
        IOptions<VapidOptions> vapidOptions)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _vapidOptions = vapidOptions.Value;
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionRequest request, CancellationToken ct)
    {
        var existing = (await _repo.GetAllAsync(ct))
            .FirstOrDefault(s => s.Endpoint == request.Endpoint);

        if (existing != null) return Ok();

        await _repo.AddAsync(new PushSubscription
        {
            Endpoint = request.Endpoint,
            P256dhKey = request.Keys.P256dh,
            AuthKey = request.Keys.Auth
        }, ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return StatusCode(201);
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] PushUnsubscribeRequest request, CancellationToken ct)
    {
        await _repo.RemoveByEndpointAsync(request.Endpoint, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("vapid-public-key")]
    public IActionResult GetVapidPublicKey()
    {
        return Ok(new { publicKey = _vapidOptions.PublicKey });
    }
}
