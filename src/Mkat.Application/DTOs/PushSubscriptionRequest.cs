namespace Mkat.Application.DTOs;

public record PushSubscriptionRequest
{
    public string Endpoint { get; init; } = string.Empty;
    public PushSubscriptionKeys Keys { get; init; } = new();
}

public record PushSubscriptionKeys
{
    public string P256dh { get; init; } = string.Empty;
    public string Auth { get; init; } = string.Empty;
}

public record PushUnsubscribeRequest
{
    public string Endpoint { get; init; } = string.Empty;
}
