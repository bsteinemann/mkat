using Mkat.Domain.Enums;

namespace Mkat.Application.DTOs;

public record PeerInitiateRequest
{
    public string Name { get; init; } = string.Empty;
}

public record PeerInitiateResponse
{
    public string Token { get; init; } = string.Empty;
}

public record PeerCompleteRequest
{
    public string Token { get; init; } = string.Empty;
}

public record PeerAcceptRequest
{
    public string Secret { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public record PeerAcceptResponse
{
    public string HeartbeatToken { get; init; } = string.Empty;
    public string WebhookToken { get; init; } = string.Empty;
    public int HeartbeatIntervalSeconds { get; init; }
}

public record PeerResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public Guid ServiceId { get; init; }
    public DateTime PairedAt { get; init; }
    public int HeartbeatIntervalSeconds { get; init; }
    public ServiceState? ServiceState { get; init; }
}
