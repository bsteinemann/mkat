# Alerts & Notifications

## Alerts

An **alert** is created whenever a service changes state. Alerts record what happened, when, and at what severity.

### Alert Types

| Type | Trigger |
|------|---------|
| Failure | Webhook `/fail` called or health check failed |
| Recovery | Webhook `/recover` called or service recovered |
| Missed Heartbeat | Heartbeat not received within interval + grace period |
| Failed Health Check | Active health check poll returned unexpected response |

### Alert Lifecycle

1. **Created** — State transition detected, alert record saved
2. **Dispatched** — Notification sent to configured channels
3. **Acknowledged** — User marks the alert as seen (optional)

### Acknowledging Alerts

Acknowledge via the API or directly from a Telegram notification button:

```bash
curl -u admin:password -X POST http://localhost:8080/mkat/api/v1/alerts/{id}/ack
```

## Notification Channels

Alerts are delivered through **contacts** and their **channels**.

### Contacts

A contact is a notification recipient. Each contact can have multiple delivery channels (e.g., Telegram + email).

- **Default contact** — Receives alerts for all services unless a service has specific contacts assigned
- **Service-specific contacts** — Override the default for individual services

### Channel Types

| Type | Status |
|------|--------|
| Telegram | Supported |
| Email | Planned |

### Mute Windows

Temporarily suppress notifications without pausing monitoring:

```bash
curl -u admin:password -X POST http://localhost:8080/mkat/api/v1/services/{id}/mute \
  -H "Content-Type: application/json" \
  -d '{"durationMinutes": 60, "reason": "Deploying update"}'
```

Unlike pause, muting keeps monitoring active — state still changes, but no notifications are sent. Use this for short maintenance windows where you want to track state changes without being alerted.
