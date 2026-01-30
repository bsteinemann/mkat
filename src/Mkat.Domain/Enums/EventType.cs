namespace Mkat.Domain.Enums;

public enum EventType
{
    WebhookReceived = 0,
    HeartbeatReceived = 1,
    HealthCheckPerformed = 2,
    MetricIngested = 3,
    StateChanged = 4
}
