# ADR-006: Unified Event Logging and Rollup Aggregation Strategy

## Status

Accepted

## Context

mkat originally stored metric data in a `MetricReading` entity, which only tracked metric monitor values. Other monitor types (webhook, heartbeat, health check) had no historical data beyond state transitions stored as alerts.

We needed:
- Historical data for all monitor types, not just metrics
- Long-term trend analysis without unbounded storage growth
- Per-monitor uptime percentage calculations
- Charts on the service detail page showing monitor activity over time

## Decision

Replace `MetricReading` with two unified entities:

### MonitorEvent
A single event table that records all monitor activity across all types. Each event captures:
- Monitor and service references
- Event type (WebhookReceived, HeartbeatReceived, HealthCheckPerformed, MetricIngested, StateChanged)
- Success/failure status
- Optional numeric value (response time for health checks, metric value for metrics)
- Whether the value is out of configured range

### MonitorRollup
Pre-aggregated statistics computed from events at multiple granularity levels:
- **Hourly**: computed from raw events, retained 30 days
- **Daily**: computed from hourly rollups, retained 1 year
- **Weekly**: computed from daily rollups, retained 2 years
- **Monthly**: computed from weekly rollups, retained forever

Each rollup stores: count, success/failure counts, min, max, mean, median, P80, P90, P95, standard deviation, and uptime percentage.

### Data Source Selection
The frontend auto-selects the data source based on the requested time range:
- 1h, 6h, 24h: raw events
- 7d, 30d: hourly rollups
- 90d, 1y: daily rollups

### Retention Policy
- Raw events: 7 days
- Hourly rollups: 30 days
- Daily rollups: 1 year
- Weekly rollups: 2 years
- Monthly rollups: kept forever

## Consequences

### Positive
- All monitor types now have historical data and charts
- Storage is bounded: raw events are pruned after 7 days, rollups provide long-term trends
- Uptime percentage is efficiently computed from rollup data
- MetricEvaluator's threshold strategies (ConsecutiveCount, TimeDurationAverage, SampleCountAverage) work against MonitorEvent instead of MetricReading, unifying the data model
- Single retention worker handles all cleanup

### Negative
- Hourly rollup computation requires querying all events in the window (mitigated by 7-day event retention)
- Rollup worker runs as a background service; if it fails, rollups may have gaps
- Migration required from MetricReading to MonitorEvent for existing data

### Risks
- Rollup accuracy depends on the worker running consistently; missed runs could leave gaps in aggregated data
- 7-day event retention means raw data is lost relatively quickly; if the rollup worker was down for >7 days, that period's data would be permanently lost
