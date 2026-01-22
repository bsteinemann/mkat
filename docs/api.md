# API Reference

Base URL: `/api/v1`

All endpoints require Basic Authentication except webhooks and heartbeats.

## Services

### List Services

```
GET /services?page=1&pageSize=20
```

Response:
```json
{
  "items": [...],
  "page": 1,
  "pageSize": 20,
  "totalCount": 5
}
```

### Create Service

```
POST /services
```

Body:
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

Monitor types:
- `0` - Webhook
- `1` - Heartbeat

Severity levels:
- `0` - Low
- `1` - Medium
- `2` - High
- `3` - Critical

### Get Service

```
GET /services/{id}
```

### Update Service

```
PUT /services/{id}
```

### Delete Service

```
DELETE /services/{id}
```

### Pause Service

```
POST /services/{id}/pause
```

Body:
```json
{
  "until": "2025-01-01T00:00:00Z",
  "autoResume": true
}
```

### Resume Service

```
POST /services/{id}/resume
```

### Mute Service

```
POST /services/{id}/mute
```

Body:
```json
{
  "durationMinutes": 60,
  "reason": "Planned maintenance"
}
```

## Webhooks (No Auth Required)

### Report Failure

```
POST /webhook/{token}/fail
```

### Report Recovery

```
POST /webhook/{token}/recover
```

## Heartbeat (No Auth Required)

```
POST /heartbeat/{token}
```

## Alerts

### List Alerts

```
GET /alerts?page=1&pageSize=20
```

### Get Alert

```
GET /alerts/{id}
```

### Acknowledge Alert

```
POST /alerts/{id}/ack
```

## Health Checks

### Basic Health

```
GET /health
```

Response:
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

Response:
```json
{
  "status": "ready",
  "timestamp": "2026-01-22T12:00:00Z"
}
```

## Error Response Format

```json
{
  "error": "Human readable message",
  "code": "ERROR_CODE",
  "details": {
    "field": ["Validation error message"]
  }
}
```

Common error codes:
- `SERVICE_NOT_FOUND` - Service ID doesn't exist
- `ALERT_NOT_FOUND` - Alert ID doesn't exist
- `ALREADY_ACKNOWLEDGED` - Alert was already acknowledged
- `VALIDATION_ERROR` - Request body validation failed
- `INTERNAL_ERROR` - Unexpected server error
