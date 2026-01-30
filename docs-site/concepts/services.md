# Services

A **service** represents something you want to monitor — an API, a cron job, a backup task, a Docker container, or any process that should be running.

## Properties

| Property | Description |
|----------|-------------|
| Name | Human-readable identifier |
| Description | Optional details about what the service does |
| Severity | How critical the service is: Low, Medium, High, or Critical |
| State | Current status (see below) |

## States

Services follow this state machine:

```
UNKNOWN → UP ↔ DOWN
Any state → PAUSED (manual)
PAUSED → UNKNOWN (resume)
```

- **Unknown** — Initial state. No data received yet.
- **Up** — The service is healthy. Heartbeats are arriving, health checks pass, or a recovery webhook was received.
- **Down** — The service has failed. A heartbeat was missed, a health check failed, or a failure webhook was received.
- **Paused** — Monitoring is suspended. No alerts are sent. You can pause with an optional auto-resume time.

Alerts are sent **only on state transitions**. If a service is already DOWN and another failure arrives, no duplicate alert is sent.

## Severity Levels

| Level | Value | Use for |
|-------|-------|---------|
| Low | 0 | Non-critical background tasks |
| Medium | 1 | Important but not urgent services |
| High | 2 | Production services that need prompt attention |
| Critical | 3 | Core infrastructure that needs immediate action |

Severity is included in alert notifications so you can prioritize your response.

## Pause & Resume

Pause a service during planned maintenance to suppress alerts:

```bash
# Pause with auto-resume in 2 hours
curl -u admin:password -X POST http://localhost:8080/mkat/api/v1/services/{id}/pause \
  -H "Content-Type: application/json" \
  -d '{"until": "2026-01-30T14:00:00Z", "autoResume": true}'

# Resume manually
curl -u admin:password -X POST http://localhost:8080/mkat/api/v1/services/{id}/resume
```

When resumed, the service returns to **Unknown** and re-evaluates based on the next incoming data.
