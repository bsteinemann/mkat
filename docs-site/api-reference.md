# API Reference

Base URL: `/api/v1`

All endpoints require **Basic Authentication** unless otherwise noted. Webhooks, heartbeats, metrics submission, and health checks do not require authentication.

## Enum Reference

### Severity

| Value | Name |
|-------|------|
| `0` | Low |
| `1` | Medium |
| `2` | High |
| `3` | Critical |

### Service State

| Value | Name |
|-------|------|
| `0` | Unknown |
| `1` | Up |
| `2` | Down |
| `3` | Paused |

### Monitor Type

| Value | Name |
|-------|------|
| `0` | Webhook |
| `1` | Heartbeat |
| `2` | HealthCheck |
| `3` | Metric |

### Alert Type

| Value | Name |
|-------|------|
| `0` | Failure |
| `1` | Recovery |
| `2` | MissedHeartbeat |
| `3` | FailedHealthCheck |

### Threshold Strategy

| Value | Name | Description |
|-------|------|-------------|
| `0` | Immediate | Alert on the first out-of-range value |
| `1` | ConsecutiveCount | Alert after N consecutive out-of-range values |
| `2` | TimeDurationAverage | Alert when average over a time window is out of range |
| `3` | SampleCountAverage | Alert when average over N samples is out of range |

### Channel Type

| Value | Name |
|-------|------|
| `0` | Telegram |
| `1` | Email |

---

## Services

### List Services

```
GET /api/v1/services?page=1&pageSize=20
```

**Auth:** Required

**Query parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | int | `1` | Page number (min 1) |
| `pageSize` | int | `20` | Items per page (1-100) |

**Response:**

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "My API",
      "description": "Production API server",
      "state": 1,
      "severity": 2,
      "pausedUntil": null,
      "createdAt": "2026-01-22T12:00:00Z",
      "updatedAt": "2026-01-22T12:00:00Z",
      "monitors": [
        {
          "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
          "type": 1,
          "token": "abc123def456",
          "intervalSeconds": 300,
          "gracePeriodSeconds": 60,
          "lastCheckIn": "2026-01-22T12:00:00Z",
          "webhookFailUrl": "https://mkat.example.com/webhook/abc123def456/fail",
          "webhookRecoverUrl": "https://mkat.example.com/webhook/abc123def456/recover",
          "heartbeatUrl": "https://mkat.example.com/heartbeat/abc123def456",
          "metricUrl": "https://mkat.example.com/metric/abc123def456",
          "minValue": null,
          "maxValue": null,
          "thresholdStrategy": null,
          "thresholdCount": null,
          "windowSeconds": null,
          "windowSampleCount": null,
          "retentionDays": null,
          "lastMetricValue": null,
          "lastMetricAt": null,
          "healthCheckUrl": null,
          "httpMethod": null,
          "expectedStatusCodes": null,
          "timeoutSeconds": null,
          "bodyMatchRegex": null
        }
      ]
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 5,
  "totalPages": 1,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

### Get Service

```
GET /api/v1/services/{id}
```

**Auth:** Required

**Response:** A single `ServiceResponse` object (same shape as items in the list response).

### Create Service

```
POST /api/v1/services
```

**Auth:** Required

**Body:**

```json
{
  "name": "My API",
  "description": "Production API server",
  "severity": 2,
  "monitors": [
    {
      "type": 1,
      "intervalSeconds": 300,
      "gracePeriodSeconds": 60
    }
  ]
}
```

Each monitor in the `monitors` array accepts these fields:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `type` | int | Yes | Monitor type (see enum) |
| `intervalSeconds` | int | Yes | Check-in interval |
| `gracePeriodSeconds` | int | No | Grace period before alerting (default: max(60, interval/10)) |
| `minValue` | double | No | Metric: minimum acceptable value |
| `maxValue` | double | No | Metric: maximum acceptable value |
| `thresholdStrategy` | int | No | Metric: threshold strategy (see enum) |
| `thresholdCount` | int | No | Metric: consecutive count for ConsecutiveCount strategy |
| `windowSeconds` | int | No | Metric: time window for TimeDurationAverage strategy |
| `windowSampleCount` | int | No | Metric: sample count for SampleCountAverage strategy |
| `retentionDays` | int | No | Metric: how long to keep readings (default: 7) |
| `healthCheckUrl` | string | No | HealthCheck: URL to probe |
| `httpMethod` | string | No | HealthCheck: HTTP method (default: "GET") |
| `expectedStatusCodes` | string | No | HealthCheck: expected status codes (default: "200") |
| `timeoutSeconds` | int | No | HealthCheck: request timeout (default: 10) |
| `bodyMatchRegex` | string | No | HealthCheck: regex to match in response body |

**Response:** `201 Created` with the created `ServiceResponse`.

### Update Service

```
PUT /api/v1/services/{id}
```

**Auth:** Required

**Body:**

```json
{
  "name": "My API (updated)",
  "description": "Updated description",
  "severity": 3
}
```

**Response:** `200 OK` with the updated `ServiceResponse`.

### Delete Service

```
DELETE /api/v1/services/{id}
```

**Auth:** Required

**Response:** `204 No Content`

### Pause Service

```
POST /api/v1/services/{id}/pause
```

**Auth:** Required

**Body (optional):**

```json
{
  "until": "2026-02-01T00:00:00Z",
  "autoResume": true
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `until` | datetime | No | When the pause should end |
| `autoResume` | bool | No | Whether to auto-resume at `until` (default: false) |

**Response:**

```json
{
  "paused": true,
  "until": "2026-02-01T00:00:00Z"
}
```

### Resume Service

```
POST /api/v1/services/{id}/resume
```

**Auth:** Required

**Response:**

```json
{
  "resumed": true
}
```

Returns `400` with code `SERVICE_NOT_PAUSED` if the service is not currently paused.

### Mute Service

```
POST /api/v1/services/{id}/mute
```

**Auth:** Required

**Body:**

```json
{
  "durationMinutes": 60,
  "reason": "Planned maintenance"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `durationMinutes` | int | Yes | How long to mute alerts |
| `reason` | string | No | Reason for muting |

**Response:**

```json
{
  "muted": true,
  "until": "2026-01-22T13:00:00Z"
}
```

### Set Service Contacts

```
PUT /api/v1/services/{id}/contacts
```

**Auth:** Required

**Body:**

```json
{
  "contactIds": [
    "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "7ba91e32-1234-5678-abcd-ef1234567890"
  ]
}
```

**Response:**

```json
{
  "assigned": 2
}
```

### Get Service Contacts

```
GET /api/v1/services/{id}/contacts
```

**Auth:** Required

**Response:**

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "On-Call Team",
    "isDefault": true,
    "createdAt": "2026-01-22T12:00:00Z",
    "channels": [
      {
        "id": "c1d2e3f4-5678-9012-abcd-ef1234567890",
        "type": 0,
        "configuration": "{\"chatId\": \"-100123456789\"}",
        "isEnabled": true,
        "createdAt": "2026-01-22T12:00:00Z"
      }
    ],
    "serviceCount": 3
  }
]
```

---

## Monitors

Monitors are nested under a service. All routes are prefixed with `/api/v1/services/{serviceId}/monitors`.

### Add Monitor

```
POST /api/v1/services/{serviceId}/monitors
```

**Auth:** Required

**Body:**

```json
{
  "type": 1,
  "intervalSeconds": 300,
  "gracePeriodSeconds": 60
}
```

The request body accepts the same monitor fields as `CreateMonitorRequest` (see the [Create Service](#create-service) monitor fields table).

**Response:** `201 Created` with the created `MonitorResponse`.

### Update Monitor

```
PUT /api/v1/services/{serviceId}/monitors/{monitorId}
```

**Auth:** Required

**Body:**

```json
{
  "intervalSeconds": 600,
  "gracePeriodSeconds": 120
}
```

Fields available for update:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `intervalSeconds` | int | Yes | Check-in interval |
| `gracePeriodSeconds` | int | No | Grace period (default: max(60, interval/10)) |
| `minValue` | double | No | Metric: minimum acceptable value |
| `maxValue` | double | No | Metric: maximum acceptable value |
| `thresholdStrategy` | int | No | Metric: threshold strategy |
| `thresholdCount` | int | No | Metric: consecutive count |
| `windowSeconds` | int | No | Metric: time window |
| `windowSampleCount` | int | No | Metric: sample count |
| `retentionDays` | int | No | Metric: reading retention |
| `healthCheckUrl` | string | No | HealthCheck: URL to probe |
| `httpMethod` | string | No | HealthCheck: HTTP method |
| `expectedStatusCodes` | string | No | HealthCheck: expected status codes |
| `timeoutSeconds` | int | No | HealthCheck: request timeout |
| `bodyMatchRegex` | string | No | HealthCheck: body match regex |

**Response:** `200 OK` with the updated `MonitorResponse`.

### Delete Monitor

```
DELETE /api/v1/services/{serviceId}/monitors/{monitorId}
```

**Auth:** Required

**Response:** `204 No Content`

Returns `400` with code `LAST_MONITOR` if attempting to delete the only monitor on a service.

---

## Alerts

### List Alerts

```
GET /api/v1/alerts?page=1&pageSize=20
```

**Auth:** Required

**Query parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | int | `1` | Page number (min 1) |
| `pageSize` | int | `20` | Items per page (1-100) |

**Response:**

```json
{
  "items": [
    {
      "id": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
      "serviceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "type": 0,
      "severity": 2,
      "message": "Failure webhook received",
      "createdAt": "2026-01-22T12:05:00Z",
      "acknowledgedAt": null,
      "dispatchedAt": "2026-01-22T12:05:01Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

### Get Alert

```
GET /api/v1/alerts/{id}
```

**Auth:** Required

**Response:** A single `AlertResponse` object (same shape as items in the list response).

### Acknowledge Alert

```
POST /api/v1/alerts/{id}/ack
```

**Auth:** Required

**Response:**

```json
{
  "acknowledged": true
}
```

Returns `400` with code `ALREADY_ACKNOWLEDGED` if the alert was already acknowledged.

---

## Contacts

### Create Contact

```
POST /api/v1/contacts
```

**Auth:** Required

**Body:**

```json
{
  "name": "On-Call Team"
}
```

**Response:** `201 Created`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "On-Call Team",
  "isDefault": false,
  "createdAt": "2026-01-22T12:00:00Z",
  "channels": [],
  "serviceCount": 0
}
```

### List Contacts

```
GET /api/v1/contacts
```

**Auth:** Required

**Response:** An array of `ContactResponse` objects.

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "On-Call Team",
    "isDefault": true,
    "createdAt": "2026-01-22T12:00:00Z",
    "channels": [],
    "serviceCount": 3
  }
]
```

### Get Contact

```
GET /api/v1/contacts/{id}
```

**Auth:** Required

**Response:** A single `ContactResponse` object.

### Update Contact

```
PUT /api/v1/contacts/{id}
```

**Auth:** Required

**Body:**

```json
{
  "name": "Updated Team Name"
}
```

**Response:** `200 OK` with the updated `ContactResponse`.

### Delete Contact

```
DELETE /api/v1/contacts/{id}
```

**Auth:** Required

**Response:** `204 No Content`

Returns `400` if the contact is the default contact or is the only contact assigned to any service.

### Add Channel to Contact

```
POST /api/v1/contacts/{id}/channels
```

**Auth:** Required

**Body:**

```json
{
  "type": 0,
  "configuration": "{\"chatId\": \"-100123456789\"}"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `type` | int | Yes | Channel type (`0` = Telegram, `1` = Email) |
| `configuration` | string | Yes | JSON string with channel-specific config |

**Response:** `201 Created`

```json
{
  "id": "c1d2e3f4-5678-9012-abcd-ef1234567890",
  "type": 0,
  "configuration": "{\"chatId\": \"-100123456789\"}",
  "isEnabled": true,
  "createdAt": "2026-01-22T12:00:00Z"
}
```

### Update Channel

```
PUT /api/v1/contacts/{id}/channels/{channelId}
```

**Auth:** Required

**Body:**

```json
{
  "configuration": "{\"chatId\": \"-100987654321\"}",
  "isEnabled": true
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `configuration` | string | Yes | Updated channel configuration |
| `isEnabled` | bool | Yes | Whether the channel is active |

**Response:** `200 OK` with the updated `ContactChannelResponse`.

### Delete Channel

```
DELETE /api/v1/contacts/{id}/channels/{channelId}
```

**Auth:** Required

**Response:** `204 No Content`

### Test Channel

```
POST /api/v1/contacts/{id}/channels/{channelId}/test
```

**Auth:** Required

**Response:**

```json
{
  "success": true,
  "message": "Test notification sent via Telegram"
}
```

---

## Peers

Peer-to-peer monitoring allows two mkat instances to monitor each other. The pairing flow has three steps: initiate, complete (which calls accept on the remote), and then both instances monitor each other via heartbeats and webhooks.

### Initiate Pairing

```
POST /api/v1/peers/pair/initiate
```

**Auth:** Required

Generates a pairing token that can be shared with the remote instance.

**Body:**

```json
{
  "name": "Home Server"
}
```

**Response:**

```json
{
  "token": "eyJhbGciOi..."
}
```

The token encodes the instance URL, name, a secret, and an expiry time. Share this token with the remote instance.

### Complete Pairing

```
POST /api/v1/peers/pair/complete
```

**Auth:** Required

Called by the instance that received the pairing token. This endpoint decodes the token and calls the remote instance's accept endpoint automatically.

**Body:**

```json
{
  "token": "eyJhbGciOi..."
}
```

**Response:**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Home Server",
  "url": "https://remote.example.com",
  "serviceId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "pairedAt": "2026-01-22T12:00:00Z",
  "heartbeatIntervalSeconds": 30,
  "serviceState": 0
}
```

Possible error codes: `INVALID_TOKEN`, `TOKEN_EXPIRED`, `REMOTE_REJECTED`, `REMOTE_UNREACHABLE`, `REMOTE_INVALID_RESPONSE`.

### Accept Pairing (Internal)

```
POST /api/v1/peers/pair/accept
```

**Auth:** Required

This endpoint is called automatically by the remote instance during the complete step. It validates the pairing secret and creates the local peer and service records.

**Body:**

```json
{
  "secret": "random-secret-string",
  "url": "https://caller.example.com",
  "name": "Remote Instance"
}
```

**Response:**

```json
{
  "heartbeatToken": "abc123def456",
  "webhookToken": "789ghi012jkl",
  "heartbeatIntervalSeconds": 30
}
```

### List Peers

```
GET /api/v1/peers
```

**Auth:** Required

**Response:**

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Home Server",
    "url": "https://remote.example.com",
    "serviceId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "pairedAt": "2026-01-22T12:00:00Z",
    "heartbeatIntervalSeconds": 30,
    "serviceState": 1
  }
]
```

### Unpair

```
DELETE /api/v1/peers/{id}
```

**Auth:** Required

Removes the peer and its associated service. Also sends a best-effort notification to the remote instance to clean up its side.

**Response:** `204 No Content`

### Remote Unpair (Internal)

```
POST /api/v1/peers/pair/unpair
```

**Auth:** Required

Called by a remote peer during unpair to notify this instance. Removes the peer and associated service matching the given URL.

**Body:**

```json
{
  "url": "https://remote.example.com"
}
```

**Response:** `200 OK`

---

## Webhooks :badge[No Auth]{type="tip"}

Webhook endpoints do not require authentication. They are called by external services to report failures and recoveries. The `{token}` is the monitor token returned when creating a service or adding a monitor.

### Report Failure

```
POST /webhook/{token}/fail
```

**Auth:** None

**Response:**

```json
{
  "received": true,
  "alertCreated": true
}
```

### Report Recovery

```
POST /webhook/{token}/recover
```

**Auth:** None

**Response:**

```json
{
  "received": true,
  "alertCreated": true
}
```

---

## Heartbeats :badge[No Auth]{type="tip"}

Heartbeat endpoints do not require authentication. Services send periodic heartbeats to indicate they are alive.

### Send Heartbeat

```
POST /heartbeat/{token}
```

**Auth:** None

**Response:**

```json
{
  "received": true,
  "nextExpectedBefore": "2026-01-22T12:05:00Z",
  "alertCreated": false
}
```

---

## Metrics :badge[No Auth]{type="tip"}

### Submit Metric Value

```
POST /metric/{token}
```

**Auth:** None

Submit a metric value for evaluation. The value can be provided either in the request body or as a query parameter.

**Body (option 1):**

```json
{
  "value": 42.5
}
```

**Query parameter (option 2):**

```
POST /metric/{token}?value=42.5
```

**Response:**

```json
{
  "received": true,
  "value": 42.5,
  "outOfRange": false,
  "violation": false
}
```

| Field | Type | Description |
|-------|------|-------------|
| `received` | bool | Always `true` on success |
| `value` | double | The submitted value |
| `outOfRange` | bool | Whether the value falls outside min/max bounds |
| `violation` | bool | Whether the threshold strategy triggered an alert |

### Get Metric History

```
GET /api/v1/monitors/{monitorId}/metrics
```

**Auth:** Required

**Query parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `from` | datetime | None | Start of time range (inclusive) |
| `to` | datetime | None | End of time range (inclusive) |
| `limit` | int | `100` | Maximum number of readings to return |

**Response:**

```json
{
  "monitorId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "readings": [
    {
      "id": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
      "value": 42.5,
      "recordedAt": "2026-01-22T12:00:00Z",
      "outOfRange": false
    },
    {
      "id": "b2c3d4e5-f678-9012-abcd-ef1234567890",
      "value": 95.2,
      "recordedAt": "2026-01-22T12:05:00Z",
      "outOfRange": true
    }
  ]
}
```

Returns `400` with code `INVALID_MONITOR_TYPE` if the monitor is not a metric monitor.

### Get Latest Metric

```
GET /api/v1/monitors/{monitorId}/metrics/latest
```

**Auth:** Required

**Response:**

```json
{
  "value": 42.5,
  "recordedAt": "2026-01-22T12:05:00Z",
  "outOfRange": false,
  "monitorId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

Returns `204 No Content` if no readings exist yet.

---

## Health :badge[No Auth]{type="tip"}

Health check endpoints do not require authentication.

### Basic Health

```
GET /health
```

**Auth:** None

**Response:**

```json
{
  "status": "healthy",
  "timestamp": "2026-01-22T12:00:00Z"
}
```

### Readiness

```
GET /health/ready
```

**Auth:** None

**Response:**

```json
{
  "status": "ready",
  "timestamp": "2026-01-22T12:00:00Z"
}
```

Returns `503 Service Unavailable` (no body) if the database connection fails.

---

## Events

### SSE Event Stream

```
GET /api/v1/events/stream
```

**Auth:** Required

Opens a Server-Sent Events (SSE) stream. The connection stays open and the server pushes events as they occur. Each event has a `type` and JSON `payload`.

**Headers:**

```
Content-Type: text/event-stream
Cache-Control: no-cache
Connection: keep-alive
```

**Event format:**

```
event: state_change
data: {"serviceId":"3fa85f64-...","state":2,"previousState":1}

event: alert_created
data: {"alertId":"f47ac10b-...","serviceId":"3fa85f64-...","type":0,"severity":2}
```

**Usage with JavaScript:**

```js
const events = new EventSource('/api/v1/events/stream', {
  headers: { 'Authorization': 'Basic ' + btoa('user:pass') }
});

events.addEventListener('state_change', (e) => {
  const data = JSON.parse(e.data);
  console.log('State changed:', data);
});

events.addEventListener('alert_created', (e) => {
  const data = JSON.parse(e.data);
  console.log('New alert:', data);
});
```

---

## Push Notifications

Web Push notification management for browser-based alerts.

### Subscribe

```
POST /api/v1/push/subscribe
```

**Auth:** Required

**Body:**

```json
{
  "endpoint": "https://fcm.googleapis.com/fcm/send/...",
  "keys": {
    "p256dh": "BNcRdreALRFXTkOOUHK1...",
    "auth": "tBHItJI5svbpC7htR..."
  }
}
```

**Response:** `201 Created` (or `200 OK` if already subscribed).

### Unsubscribe

```
POST /api/v1/push/unsubscribe
```

**Auth:** Required

**Body:**

```json
{
  "endpoint": "https://fcm.googleapis.com/fcm/send/..."
}
```

**Response:** `204 No Content`

### Get VAPID Public Key

```
GET /api/v1/push/vapid-public-key
```

**Auth:** Required

Returns the VAPID public key needed to subscribe to push notifications on the client.

**Response:**

```json
{
  "publicKey": "BEl62iUYgUivxIkv69yViEuiBIa..."
}
```

---

## Error Response Format

All errors follow a consistent format:

```json
{
  "error": "Human readable message",
  "code": "ERROR_CODE",
  "details": {
    "field": ["Validation error message"]
  }
}
```

The `details` field is only present for validation errors.

### Common Error Codes

| Code | Description |
|------|-------------|
| `VALIDATION_ERROR` | Request body validation failed |
| `SERVICE_NOT_FOUND` | Service ID does not exist |
| `SERVICE_NOT_PAUSED` | Cannot resume a service that is not paused |
| `ALERT_NOT_FOUND` | Alert ID does not exist |
| `ALREADY_ACKNOWLEDGED` | Alert was already acknowledged |
| `MONITOR_NOT_FOUND` | Monitor ID does not exist |
| `LAST_MONITOR` | Cannot delete the last monitor on a service |
| `PEER_NOT_FOUND` | Peer ID does not exist |
| `INVALID_SECRET` | Invalid or expired pairing secret |
| `INVALID_TOKEN` | Invalid pairing token format |
| `TOKEN_EXPIRED` | Pairing token has expired |
| `REMOTE_REJECTED` | Remote instance rejected the pairing |
| `REMOTE_UNREACHABLE` | Could not reach the remote instance |
| `REMOTE_INVALID_RESPONSE` | Invalid response from remote instance |
| `INTERNAL_ERROR` | Unexpected server error |
