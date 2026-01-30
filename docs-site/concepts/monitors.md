# Monitors

A **monitor** defines how mkat checks whether a service is healthy. Every service has at least one monitor. Each monitor type works differently.

## Monitor Types

### Webhook

Your service actively tells mkat about its state by calling two endpoints:

```bash
# Report failure
curl -X POST http://mkat-host/mkat/webhook/{token}/fail

# Report recovery
curl -X POST http://mkat-host/mkat/webhook/{token}/recover
```

No authentication needed — the token in the URL serves as the credential.

**Use case:** Error handlers, deployment scripts, CI/CD pipelines.

### Heartbeat

Your service sends periodic "I'm alive" signals. If a heartbeat is missed beyond the grace period, the service is marked DOWN.

```bash
# Send heartbeat
curl -X POST http://mkat-host/mkat/heartbeat/{token}
```

| Setting | Description |
|---------|-------------|
| Interval | How often a heartbeat is expected (seconds) |
| Grace Period | Extra time allowed after the interval before alerting (seconds) |

**Use case:** Cron jobs, scheduled tasks, background workers.

### Health Check

mkat actively polls an HTTP endpoint at regular intervals and validates the response.

| Setting | Description |
|---------|-------------|
| URL | The endpoint to poll |
| HTTP Method | GET, POST, etc. |
| Expected Status Codes | Which HTTP status codes indicate success |
| Timeout | How long to wait for a response (seconds) |
| Body Match Regex | Optional regex the response body must match |
| Interval | How often to check (seconds) |
| Grace Period | How many consecutive failures before alerting (seconds) |

**Use case:** Web APIs, public websites, internal services.

### Metric

Submit numeric values and alert when they go out of range.

```bash
# Submit a metric value
curl -X POST "http://mkat-host/mkat/metric/{token}?value=85.5"
```

| Setting | Description |
|---------|-------------|
| Min Value | Minimum acceptable value |
| Max Value | Maximum acceptable value |
| Threshold Strategy | How violations are evaluated (see below) |
| Retention Days | How long to keep metric history |

#### Threshold Strategies

| Strategy | Behavior |
|----------|----------|
| Immediate | A single out-of-range value triggers an alert |
| Consecutive Count | N consecutive violations required before alerting |
| Time Duration Average | Average over a time window must be out of range |
| Sample Count Average | Average over N samples must be out of range |

**Use case:** CPU usage, memory, disk space, response times, queue depth.

## Token Security

Each monitor gets a unique token when created. This token is the only credential needed to submit data to webhook, heartbeat, and metric endpoints. Treat tokens like passwords — anyone with the token can change your service's state.
