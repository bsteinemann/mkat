# Product Requirements Document (PRD)
## Project: mkat - Modular Healthcheck & Notification Service

---

## 1. Overview

mkat is a self-hosted, lightweight healthcheck and monitoring service designed for homelabs and small web projects. The system monitors services using webhooks, automated health checks, and scheduled heartbeats, and delivers notifications through extensible notification channels (Telegram initially).

The system is designed to be:
- Simple to operate
- Cost-effective (free-first)
- Phone-centric
- Extensible by design

---

## 2. Goals & Non-Goals

### Goals
- Monitor multiple independent services
- Detect failures, recoveries, and missed heartbeats
- Deliver interactive notifications to a phone
- Support multiple notification channels via a pluggable architecture
- Provide a web UI for configuration and visibility

### Non-Goals
- Enterprise SLA reporting
- Multi-tenant RBAC (single admin initially)
- High-availability clustering
- Complex analytics dashboards

---

## 3. Target Users
- Homelab operators
- Indie developers
- Small project maintainers
- Solo operators

---

## 4. Core Concepts

### 4.1 Service
A monitored unit such as an API, cron job, backup task, or container.

Each service has:
- Name
- Description
- One or more Monitors
- Severity
- State
- Notification behavior

### 4.2 Monitor
A monitoring configuration attached to a service.

Monitor types:
- **Webhook** - External system calls mkat to report failure/recovery
- **Heartbeat** - Service must ping mkat within a configured interval
- **Health Check** - mkat actively polls an HTTP endpoint (Phase 2)

### 4.3 Alert
A record of a state change event (failure, recovery, missed heartbeat).

### 4.4 NotificationChannel
A configured notification target (Telegram, Email, Webhook, etc.).

---

## 5. State Management

### 5.1 Service States

| State | Description |
|-------|-------------|
| `UNKNOWN` | Initial state, not yet evaluated |
| `UP` | Service is healthy |
| `DOWN` | Service has failed |
| `PAUSED` | Monitoring suspended (maintenance mode) |

### 5.2 State Transitions

```
                         ┌────────────────────────────────────┐
                         │                                    │
                         ▼                                    │
                   ┌─────────┐                                │
          ┌──────►│ UNKNOWN │◄──────── (service created)     │
          │       └────┬────┘                                 │
          │            │                                      │
          │            │ first successful check               │
          │            ▼                                      │
          │       ┌─────────┐        failure         ┌───────────┐
          │       │   UP    │───────────────────────►│   DOWN    │
          │       └────┬────┘                        └─────┬─────┘
          │            ▲                                   │
          │            │         recovery                  │
          │            └───────────────────────────────────┘
          │
          │       ┌─────────────────────────────┐
          └───────│          PAUSED             │◄─── (manual from any state)
                  │  • optional end time        │
                  │  • auto-resume (optional)   │
                  └─────────────────────────────┘
                         │
                         │ resume → UNKNOWN (re-evaluate)
                         ▼
```

### 5.3 State Rules

- New services start as `UNKNOWN`
- First successful check transitions to `UP`
- Failures transition to `DOWN`
- Recovery transitions back to `UP`
- `PAUSED` can be entered from any state (manual action)
- `PAUSED` supports optional end time for scheduled maintenance
- Auto-resume from `PAUSED` is optional (configurable per pause)
- Resume goes to `UNKNOWN` and re-evaluates
- **Alerts are sent only on state transitions** (not repeated for same state)

---

## 6. Functional Requirements

### 6.1 Service Management
- Create, update, delete services
- Enable or disable monitoring
- Unique identifier per service
- Pause/resume with optional maintenance window

---

### 6.2 Webhook-Based Monitoring

#### Failure Webhook
- `POST /webhook/{token}/fail`
- Marks service as `DOWN`
- Triggers failure alert

#### Recovery Webhook
- `POST /webhook/{token}/recover`
- Marks service as `UP`
- Triggers recovery alert

#### Security
- Unguessable tokens (UUID v4)
- No authentication required on webhook endpoints

---

### 6.3 Heartbeat Monitoring

Services must call the heartbeat endpoint within a configured interval.

#### Endpoint
- `POST /heartbeat/{token}`
- Returns `200 OK` on success

#### Intervals
- Minimum: 30 seconds
- Supported units: seconds, minutes, hours, days
- Custom intervals allowed

#### Grace Period
- Default: 10% of interval, minimum 1 minute
- Configurable per monitor
- Minimum grace period: 1 minute

#### Failure Detection
- Missed heartbeat (interval + grace period exceeded) triggers `DOWN` state
- Subsequent heartbeat triggers recovery

---

### 6.4 Automated Health Checks (Phase 2)

Periodic HTTP checks performed by mkat.

Configurable:
- URL
- HTTP method
- Expected status code(s)
- Timeout
- Optional response body match

---

### 6.5 Mute Functionality

Suppress alerts for a service.

Options:
- 15 minutes
- 1 hour
- 24 hours
- Custom duration

Behavior:
- Monitoring continues during mute
- State transitions still recorded
- Alerts suppressed until mute expires

---

### 6.6 Alert Acknowledgment

- Mark alerts as acknowledged
- Acknowledging does not affect monitoring
- Acknowledgment recorded with timestamp

---

## 7. Notification Channels

### 7.1 Overview

Notifications are delivered through pluggable channels. The core system emits alerts which are dispatched to enabled channels.

### 7.2 Alert Payload

| Field | Description |
|-------|-------------|
| `AlertId` | Unique identifier |
| `ServiceId` | Associated service |
| `ServiceName` | Human-readable name |
| `Severity` | Alert severity level |
| `AlertType` | Failure, Recovery, MissedHeartbeat |
| `Timestamp` | When the event occurred |
| `Message` | Alert message |
| `Metadata` | Additional context |

### 7.3 Channel Interface

All channels implement:
- `SendAsync(alert)` - Deliver the alert
- `IsEnabled` - Check if channel is active
- `ValidateConfigurationAsync()` - Verify channel config

### 7.4 Telegram Channel (Phase 1)

#### Notifications
- Alert messages on state transitions
- Formatted with service name, state, severity, timestamp

#### Inline Buttons
- **Acknowledge** - Mark alert as acknowledged
- **Mute 15m** - Mute service for 15 minutes
- **Mute 1h** - Mute service for 1 hour
- **Mute 24h** - Mute service for 24 hours

#### Bot Commands
- `/status` - Overview of all services (counts by state)
- `/list` - List all services with current states
- `/mute <service> <duration>` - Mute a service from Telegram

#### Configuration
- Bot token via environment variable
- Chat ID via environment variable

#### Delivery
- Retry on failure (configurable retry count/interval)

### 7.5 Future Channels
- Email (SMTP)
- SMS (provider-based)
- Custom Webhook (POST to external URL)
- Slack
- Discord

---

## 8. User Interface (UI)

### 8.1 Dashboard
- Overview of service states (counts and indicators)
- Active alerts list
- Recent events timeline

### 8.2 Service Management
- List all services with state indicators
- Create / edit service forms
- Detail page with:
  - Current state
  - Monitor configuration
  - Alert history
  - Copyable webhook/heartbeat URLs
- Pause/resume with optional end time

### 8.3 Alerts View
- Global timeline of all alerts
- Filter by service, type, state
- Acknowledge and mute actions

### 8.4 Settings
- Notification channel configuration
- Test notification button

---

## 9. Authentication & Security

### 9.1 UI & API Authentication
- **Basic Auth** (Phase 1)
- Single user
- Credentials from environment variables
- Stateless (validate each request)

### 9.2 Webhook Security
- Unguessable tokens (UUID v4)
- No authentication on webhook endpoints
- HMAC validation (future)

### 9.3 General Security
- HTTPS required (via reverse proxy)
- No sensitive data in logs

---

## 10. API Design

### 10.1 Versioning
- URL-based: `/api/v1/...`

### 10.2 Endpoints

```
# Services
GET    /api/v1/services              List services
POST   /api/v1/services              Create service
GET    /api/v1/services/{id}         Get service
PUT    /api/v1/services/{id}         Update service
DELETE /api/v1/services/{id}         Delete service
POST   /api/v1/services/{id}/pause   Pause service
POST   /api/v1/services/{id}/resume  Resume service
POST   /api/v1/services/{id}/mute    Mute service

# Alerts
GET    /api/v1/alerts                List alerts
GET    /api/v1/alerts/{id}           Get alert
POST   /api/v1/alerts/{id}/ack       Acknowledge alert

# Channels
GET    /api/v1/channels              List channels
PUT    /api/v1/channels/{id}         Update channel config
POST   /api/v1/channels/{id}/test    Send test notification

# Webhooks (no auth)
POST   /webhook/{token}/fail         Report failure
POST   /webhook/{token}/recover      Report recovery
POST   /heartbeat/{token}            Record heartbeat

# Health
GET    /health                       Health check
GET    /health/ready                 Readiness check
```

### 10.3 Pagination
- Default page size: 20
- Maximum page size: 100
- Offset-based pagination

### 10.4 Error Response Format
```json
{
  "error": "Human-readable message",
  "code": "ERROR_CODE",
  "details": {}
}
```

---

## 11. Persistence

### 11.1 Entities

| Entity | Description |
|--------|-------------|
| `Service` | Monitored unit |
| `Monitor` | Monitoring configuration |
| `Alert` | State change record |
| `NotificationChannel` | Channel configuration |
| `MuteWindow` | Active mute period |

### 11.2 Database
- SQLite (default)
- PostgreSQL (optional, future)

### 11.3 Retention
- Alerts retained indefinitely (cleanup scheduled for future)

---

## 12. Technical Stack

### Frontend
- React
- TanStack Router
- TanStack Query
- Tailwind CSS
- Vite

### Backend
- ASP.NET Core (.NET 8+)
- Clean Architecture
- FluentValidation
- Entity Framework Core
- Serilog (logging)

### Storage
- SQLite (default)
- PostgreSQL (optional)

### Deployment
- Single Docker container
- Volume-mounted data directory
- Reverse proxy for TLS

---

## 13. Phased Delivery

### Phase 1: MVP
- [x] Service CRUD (API + UI)
- [x] Webhook failure/recovery endpoints
- [x] Heartbeat monitoring with configurable intervals
- [x] State machine with PAUSED/maintenance mode
- [x] Telegram notifications with inline buttons
- [x] Telegram bot commands (`/status`, `/list`, `/mute`)
- [x] Basic dashboard
- [x] Basic auth (single user, env var)
- [x] API versioning (`/api/v1/...`)
- [x] Alert acknowledgment and muting

### Phase 2: Enhanced Monitoring
- [ ] Automated HTTP health checks
- [ ] Integration management UI
- [ ] Enhanced Telegram interactions
- [ ] Response time tracking

### Future
- [ ] Email notifications
- [ ] SMS notifications
- [ ] Custom webhook notifications
- [ ] Multi-user support
- [ ] Scheduled cleanup jobs
- [ ] Advanced analytics

---

## 14. Success Criteria

- Alerts delivered within seconds of state change
- No duplicate alerts for same state
- Operator can manage services in under 2 clicks
- System recovers gracefully from restarts
- Clear feedback on all user actions

---

## 15. Glossary

| Term | Definition |
|------|------------|
| **Service** | A monitored unit (API, job, container) |
| **Monitor** | A monitoring rule attached to a service |
| **Alert** | A notification record of a state change |
| **NotificationChannel** | A delivery target for alerts |
| **Heartbeat** | A periodic "I'm alive" signal from a service |
| **Webhook** | An HTTP callback to report state changes |
| **Mute** | Temporary suppression of alerts |
| **Maintenance Mode** | PAUSED state with optional auto-resume |

---

**Status:** Approved
**Audience:** Developers and AI agents
**Intent:** Implementation-ready specification
**Updated:** 2026-01-22
