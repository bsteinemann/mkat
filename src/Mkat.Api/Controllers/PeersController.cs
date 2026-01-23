using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Mkat.Application.DTOs;
using Mkat.Application.Interfaces;
using Mkat.Application.Services;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Monitor = Mkat.Domain.Entities.Monitor;

namespace Mkat.Api.Controllers;

[ApiController]
[Route("api/v1/peers")]
public class PeersController : ControllerBase
{
    private readonly IPeerRepository _peerRepo;
    private readonly IServiceRepository _serviceRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPairingService _pairingService;
    private readonly IValidator<PeerInitiateRequest> _initiateValidator;
    private readonly IValidator<PeerAcceptRequest> _acceptValidator;
    private readonly IValidator<PeerCompleteRequest> _completeValidator;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PeersController> _logger;

    public PeersController(
        IPeerRepository peerRepo,
        IServiceRepository serviceRepo,
        IUnitOfWork unitOfWork,
        IPairingService pairingService,
        IValidator<PeerInitiateRequest> initiateValidator,
        IValidator<PeerAcceptRequest> acceptValidator,
        IValidator<PeerCompleteRequest> completeValidator,
        IHttpClientFactory httpClientFactory,
        ILogger<PeersController> logger)
    {
        _peerRepo = peerRepo;
        _serviceRepo = serviceRepo;
        _unitOfWork = unitOfWork;
        _pairingService = pairingService;
        _initiateValidator = initiateValidator;
        _acceptValidator = acceptValidator;
        _completeValidator = completeValidator;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpPost("pair/initiate")]
    public async Task<ActionResult<PeerInitiateResponse>> Initiate(
        [FromBody] PeerInitiateRequest request,
        CancellationToken ct = default)
    {
        var validation = await _initiateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Validation failed",
                Code = "VALIDATION_ERROR",
                Details = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var token = _pairingService.GenerateToken(baseUrl, request.Name);

        _logger.LogInformation("Generated pairing token for instance {Name}", request.Name);

        return Ok(new PeerInitiateResponse { Token = token });
    }

    [HttpPost("pair/accept")]
    public async Task<ActionResult<PeerAcceptResponse>> Accept(
        [FromBody] PeerAcceptRequest request,
        CancellationToken ct = default)
    {
        var validation = await _acceptValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Validation failed",
                Code = "VALIDATION_ERROR",
                Details = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }

        if (!_pairingService.ValidateSecret(request.Secret))
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "Invalid or expired pairing secret",
                Code = "INVALID_SECRET"
            });
        }

        // Create a service representing this peer
        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = $"Peer: {request.Name}",
            Severity = Mkat.Domain.Enums.Severity.High,
            State = ServiceState.Unknown
        };

        // Create heartbeat monitor (peer sends heartbeats to us)
        var heartbeatMonitor = new Monitor
        {
            Id = Guid.NewGuid(),
            ServiceId = service.Id,
            Type = MonitorType.Heartbeat,
            Token = Guid.NewGuid().ToString("N"),
            IntervalSeconds = 30,
            GracePeriodSeconds = 60
        };

        // Create webhook monitor (peer reports notification failures to us)
        var webhookMonitor = new Monitor
        {
            Id = Guid.NewGuid(),
            ServiceId = service.Id,
            Type = MonitorType.Webhook,
            Token = Guid.NewGuid().ToString("N"),
            IntervalSeconds = 300,
            GracePeriodSeconds = 60
        };

        service.Monitors.Add(heartbeatMonitor);
        service.Monitors.Add(webhookMonitor);

        // Create peer entity
        var peer = new Peer
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Url = request.Url.TrimEnd('/'),
            HeartbeatToken = string.Empty, // Will be set by the completing side
            WebhookToken = string.Empty,   // Will be set by the completing side
            ServiceId = service.Id,
            PairedAt = DateTime.UtcNow,
            HeartbeatIntervalSeconds = 30
        };

        await _serviceRepo.AddAsync(service, ct);
        await _peerRepo.AddAsync(peer, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Accepted pairing from {PeerName} ({PeerUrl}), created service {ServiceId}",
            request.Name, request.Url, service.Id);

        return Ok(new PeerAcceptResponse
        {
            HeartbeatToken = heartbeatMonitor.Token,
            WebhookToken = webhookMonitor.Token,
            HeartbeatIntervalSeconds = 30
        });
    }

    [HttpPost("pair/complete")]
    public async Task<ActionResult<PeerResponse>> Complete(
        [FromBody] PeerCompleteRequest request,
        CancellationToken ct = default)
    {
        var validation = await _completeValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Validation failed",
                Code = "VALIDATION_ERROR",
                Details = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }

        var tokenData = _pairingService.DecodeToken(request.Token);
        if (tokenData == null)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Invalid token format",
                Code = "INVALID_TOKEN"
            });
        }

        if (tokenData.ExpiresAt < DateTime.UtcNow)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Token has expired",
                Code = "TOKEN_EXPIRED"
            });
        }

        // Call the remote instance's accept endpoint
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var acceptRequest = new PeerAcceptRequest
        {
            Secret = tokenData.Secret,
            Url = baseUrl,
            Name = "Local Instance"
        };

        PeerAcceptResponse? acceptResponse;
        try
        {
            var client = _httpClientFactory.CreateClient("PeerPairing");
            var remoteUrl = $"{tokenData.Url.TrimEnd('/')}/api/v1/peers/pair/accept";
            var response = await client.PostAsJsonAsync(remoteUrl, acceptRequest, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Remote accept failed with {StatusCode}: {Body}",
                    response.StatusCode, errorBody);
                return BadRequest(new ErrorResponse
                {
                    Error = "Remote instance rejected the pairing",
                    Code = "REMOTE_REJECTED"
                });
            }

            acceptResponse = await response.Content.ReadFromJsonAsync<PeerAcceptResponse>(cancellationToken: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to contact remote instance at {Url}", tokenData.Url);
            return BadRequest(new ErrorResponse
            {
                Error = "Could not reach the remote instance",
                Code = "REMOTE_UNREACHABLE"
            });
        }

        if (acceptResponse == null)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Invalid response from remote instance",
                Code = "REMOTE_INVALID_RESPONSE"
            });
        }

        // Create local service for the remote peer
        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = $"Peer: {tokenData.Name}",
            Severity = Mkat.Domain.Enums.Severity.High,
            State = ServiceState.Unknown
        };

        var heartbeatMonitor = new Monitor
        {
            Id = Guid.NewGuid(),
            ServiceId = service.Id,
            Type = MonitorType.Heartbeat,
            Token = Guid.NewGuid().ToString("N"),
            IntervalSeconds = acceptResponse.HeartbeatIntervalSeconds,
            GracePeriodSeconds = 60
        };

        var webhookMonitor = new Monitor
        {
            Id = Guid.NewGuid(),
            ServiceId = service.Id,
            Type = MonitorType.Webhook,
            Token = Guid.NewGuid().ToString("N"),
            IntervalSeconds = 300,
            GracePeriodSeconds = 60
        };

        service.Monitors.Add(heartbeatMonitor);
        service.Monitors.Add(webhookMonitor);

        var peer = new Peer
        {
            Id = Guid.NewGuid(),
            Name = tokenData.Name,
            Url = tokenData.Url.TrimEnd('/'),
            HeartbeatToken = acceptResponse.HeartbeatToken,
            WebhookToken = acceptResponse.WebhookToken,
            ServiceId = service.Id,
            PairedAt = DateTime.UtcNow,
            HeartbeatIntervalSeconds = acceptResponse.HeartbeatIntervalSeconds
        };

        await _serviceRepo.AddAsync(service, ct);
        await _peerRepo.AddAsync(peer, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Completed pairing with {PeerName} ({PeerUrl}), created service {ServiceId}",
            tokenData.Name, tokenData.Url, service.Id);

        return Ok(MapToResponse(peer));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PeerResponse>>> List(CancellationToken ct = default)
    {
        var peers = await _peerRepo.GetAllAsync(ct);
        return Ok(peers.Select(MapToResponse).ToList());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Unpair(Guid id, CancellationToken ct = default)
    {
        var peer = await _peerRepo.GetByIdAsync(id, ct);
        if (peer == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = "Peer not found",
                Code = "PEER_NOT_FOUND"
            });
        }

        // Delete the linked service (cascades to monitors)
        if (peer.Service != null)
        {
            await _serviceRepo.DeleteAsync(peer.Service, ct);
        }

        await _peerRepo.DeleteAsync(peer, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Best-effort notify remote peer
        try
        {
            var client = _httpClientFactory.CreateClient("PeerPairing");
            await client.PostAsJsonAsync(
                $"{peer.Url}/api/v1/peers/pair/unpair",
                new { url = $"{Request.Scheme}://{Request.Host}" },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify peer {PeerName} of unpair", peer.Name);
        }

        _logger.LogInformation("Unpaired from {PeerName} ({PeerUrl})", peer.Name, peer.Url);

        return NoContent();
    }

    [HttpPost("pair/unpair")]
    public async Task<IActionResult> RemoteUnpair(
        [FromBody] JsonElement body,
        CancellationToken ct = default)
    {
        // Called by remote peer during unpair - no auth needed beyond the fact
        // that only a paired peer would know our URL structure
        var url = body.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
        if (string.IsNullOrEmpty(url))
        {
            return BadRequest();
        }

        var peers = await _peerRepo.GetAllAsync(ct);
        var peer = peers.FirstOrDefault(p =>
            p.Url.Equals(url.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));

        if (peer == null)
        {
            return NotFound();
        }

        if (peer.Service != null)
        {
            await _serviceRepo.DeleteAsync(peer.Service, ct);
        }

        await _peerRepo.DeleteAsync(peer, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Remote unpair from {PeerUrl}", url);

        return Ok();
    }

    private static PeerResponse MapToResponse(Peer peer)
    {
        return new PeerResponse
        {
            Id = peer.Id,
            Name = peer.Name,
            Url = peer.Url,
            ServiceId = peer.ServiceId,
            PairedAt = peer.PairedAt,
            HeartbeatIntervalSeconds = peer.HeartbeatIntervalSeconds,
            ServiceState = peer.Service?.State
        };
    }
}
