# Track a Metric

Use a **metric monitor** to track numeric values over time and alert when they go out of range.

## Create the service

```bash
curl -u admin:password \
  -X POST http://localhost:8080/mkat/api/v1/services \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Server Disk Usage",
    "description": "Root partition usage percentage",
    "severity": 2,
    "monitors": [{
      "type": 3,
      "intervalSeconds": 300,
      "gracePeriodSeconds": 600,
      "minValue": 0,
      "maxValue": 90,
      "thresholdStrategy": 1,
      "thresholdCount": 3,
      "retentionDays": 30
    }]
  }'
```

This alerts when disk usage exceeds 90% for 3 consecutive readings.

Save the `token` from the response.

## Submit metric values

```bash
# Submit current disk usage
USAGE=$(df / --output=pcent | tail -1 | tr -d ' %')
curl -X POST "http://mkat-host/mkat/metric/YOUR_TOKEN?value=$USAGE"
```

Run this on a cron schedule matching your interval.

## Threshold strategies

### Immediate (0)

Alert on the first out-of-range value. Good for hard limits.

```json
{ "thresholdStrategy": 0 }
```

### Consecutive Count (1)

Require N consecutive violations. Reduces noise from transient spikes.

```json
{ "thresholdStrategy": 1, "thresholdCount": 3 }
```

### Time Duration Average (2)

Average over a time window. Good for gradual trends.

```json
{ "thresholdStrategy": 2, "windowSeconds": 3600 }
```

### Sample Count Average (3)

Average over N most recent samples.

```json
{ "thresholdStrategy": 3, "windowSampleCount": 10 }
```

## View metric history

```bash
# Last 24 hours
curl -u admin:password \
  "http://localhost:8080/mkat/api/v1/monitors/{monitorId}/metrics?from=2026-01-29T00:00:00Z&to=2026-01-30T00:00:00Z"

# Latest reading
curl -u admin:password \
  "http://localhost:8080/mkat/api/v1/monitors/{monitorId}/metrics/latest"
```
