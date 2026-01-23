# Milestone 7: Metrics Monitor

**Goal:** Metric value ingestion with configurable range thresholds

**Dependencies:** M3 (Monitoring Engine), M5 (Frontend)

---

## Overview

Metrics Monitor is a new monitor type (`MonitorType.Metric`) that lets services push numeric values to mkat. Each metric monitor defines an acceptable range (`min`/`max`, either optional) and a configurable threshold strategy for when to trigger a state transition.

A future Phase 2 will add pull-based metric collection (polling a URL on the service).

---

## Push API

**Endpoints (no auth, token is the secret):**
```
POST /metric/{token}              Body: {"value": 42.5}
POST /metric/{token}?value=42.5   Query param alternative
```

On receipt, mkat:
1. Stores the value in history
2. Evaluates the threshold strategy
3. Transitions the service to DOWN if violated, or back to UP if recovered

---

## Range & Threshold Configuration

### Range Bounds

- `MinValue` (double?, nullable) — lower acceptable bound (inclusive)
- `MaxValue` (double?, nullable) — upper acceptable bound (inclusive)
- At least one must be set
- A value is "out of range" if `value < min` or `value > max`

### Threshold Strategy (enum, per monitor)

| Strategy | Config fields | Triggers DOWN when... |
|----------|--------------|----------------------|
| `Immediate` | — | A single value is out of range |
| `ConsecutiveCount` | `ThresholdCount` (int) | N consecutive readings are out of range |
| `TimeDurationAverage` | `WindowSeconds` (int) | Average of readings in last N seconds is out of range |
| `SampleCountAverage` | `WindowSampleCount` (int) | Average of last N readings is out of range |

### Recovery

The service transitions back to UP when the evaluated condition is back within range. For `ConsecutiveCount`, a single in-range value resets the counter.

---

## Domain Model

### New Entity: `MetricReading`

| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Primary key |
| `MonitorId` | Guid | FK to Monitor |
| `Value` | double | The submitted value |
| `RecordedAt` | DateTime (UTC) | When the value was received |
| `IsOutOfRange` | bool | Pre-computed for quick queries |

Indexed on `(MonitorId, RecordedAt)` for efficient window queries.

### Monitor Entity Additions

| Field | Type | Description |
|-------|------|-------------|
| `MinValue` | double? | Lower range bound |
| `MaxValue` | double? | Upper range bound |
| `ThresholdStrategy` | ThresholdStrategy (enum) | How to evaluate |
| `ThresholdCount` | int? | For ConsecutiveCount strategy |
| `WindowSeconds` | int? | For TimeDurationAverage strategy |
| `WindowSampleCount` | int? | For SampleCountAverage strategy |
| `RetentionDays` | int | History retention (default: 7) |
| `LastMetricValue` | double? | Latest reading value |
| `LastMetricAt` | DateTime? | When latest reading was received |

### New Enum: `ThresholdStrategy`

```csharp
public enum ThresholdStrategy
{
    Immediate,
    ConsecutiveCount,
    TimeDurationAverage,
    SampleCountAverage
}
```

### Existing Enum Update: `MonitorType`

Add `Metric` value.

---

## API Endpoints

### Metric Submission (no auth)

```
POST /metric/{token}              Body: {"value": 42.5}
POST /metric/{token}?value=42.5   Query param alternative
```

### Monitor CRUD (authenticated)

Uses existing monitor endpoints. Create/update request for metric type:

```json
{
  "type": "Metric",
  "name": "CPU Usage",
  "minValue": null,
  "maxValue": 90.0,
  "thresholdStrategy": "ConsecutiveCount",
  "thresholdCount": 3,
  "windowSeconds": null,
  "windowSampleCount": null,
  "retentionDays": 7
}
```

### Metric History (authenticated)

```
GET /api/v1/monitors/{id}/metrics?from=...&to=...   Paginated readings
GET /api/v1/monitors/{id}/metrics/latest            Latest value + status
```

---

## Validation Rules (FluentValidation)

- At least one of `minValue`/`maxValue` required (when type is Metric)
- If both set, `minValue < maxValue`
- `thresholdCount` required and > 0 when strategy is `ConsecutiveCount`
- `windowSeconds` required and > 0 when strategy is `TimeDurationAverage`
- `windowSampleCount` required and > 0 when strategy is `SampleCountAverage`
- `retentionDays` between 1 and 365

---

## Evaluation Logic

Triggered inline on each push (no background worker needed for push):

1. Store the `MetricReading`
2. Update `LastMetricValue` / `LastMetricAt` on the monitor
3. Evaluate based on `ThresholdStrategy`:
   - **Immediate:** Check if value is out of range
   - **ConsecutiveCount:** Query last N readings, check if all are out of range
   - **TimeDurationAverage:** Query readings within window, compute average, check range
   - **SampleCountAverage:** Query last N readings, compute average, check range
4. If condition violated and service is UP → transition to DOWN (triggers alert pipeline)
5. If condition satisfied and service is DOWN → transition to UP (triggers recovery alert)

---

## Background Workers

| Worker | Interval | Responsibility |
|--------|----------|----------------|
| `MetricRetentionWorker` | 1 hour | Delete readings older than `RetentionDays` |

No periodic evaluation worker needed for push. Phase 2 (pull) would add a `MetricPollWorker`.

---

## Frontend

- Monitor create/edit form: "Metric" type option with range, threshold strategy, and retention fields
- Service detail page: latest metric value and in-range/out-of-range status
- Metric history view: table or sparkline chart of recent readings
- Copiable metric submission URL (like webhook/heartbeat tokens)

---

## Deliverables

- [ ] `MetricReading` entity and repository
- [ ] `MonitorType.Metric` enum value
- [ ] `ThresholdStrategy` enum
- [ ] Metric config fields on Monitor entity
- [ ] EF Core migration for new fields and MetricReading table
- [ ] Push endpoint (`/metric/{token}`)
- [ ] Threshold evaluation logic (all 4 strategies)
- [ ] Metric history query endpoint
- [ ] `MetricRetentionWorker`
- [ ] FluentValidation for metric monitor config
- [ ] Frontend: metric monitor form, history view, latest value display
- [ ] Unit tests for evaluation strategies
- [ ] Integration tests for all endpoints

---

## Definition of Done

- Push endpoint stores readings and evaluates thresholds
- Out-of-range triggers DOWN, recovery triggers UP
- All 4 threshold strategies work correctly
- History queryable with time range
- Retention worker cleans old data
- Frontend displays metric config and history
- All tests pass

---

## Future (Phase 2: Pull-Based Metrics)

- `PollUrl` (string) and `PollIntervalSeconds` (int) fields on monitor
- `MetricPollWorker` background service fetches URL, parses numeric response
- Same evaluation logic as push
- Configurable HTTP method, headers, JSON path for value extraction

---

**Status:** Draft
**Created:** 2026-01-23
