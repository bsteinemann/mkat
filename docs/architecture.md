# Architecture Document
## Project: mkat - Modular Healthcheck & Notification Service

---

## 1. Purpose

This document defines the technical architecture for mkat. It serves as the single source of truth for architectural decisions and guides both human developers and AI agents during implementation.

### 1.1 Design Principles

- **Clean Architecture** - Dependency inversion, testable core
- **High cohesion, low coupling** - Clear module boundaries
- **Extensibility** - Easy to add new notification channels and monitor types
- **Simplicity** - Simple beats clever, fewer features done well
- **Explicit over implicit** - Clear contracts and interfaces

---

## 2. Architectural Style

### 2.1 Overall Style
- **Modular Monolith** (initially)
- Single deployable unit
- Clear internal boundaries
- Designed for future extraction of components

### 2.2 Backend Pattern
- **Clean Architecture**
- Use-case driven application layer
- Dependency Inversion Principle throughout

---

## 3. High-Level System Components

```
┌──────────────────────────────────┐
│           Frontend UI            │
│    React + TanStack + Tailwind   │
└───────────────┬──────────────────┘
                │ REST / JSON
┌───────────────▼──────────────────┐
│          API Layer               │
│   ASP.NET • Auth • Controllers   │
└───────────────┬──────────────────┘
                │ Application Calls
┌───────────────▼──────────────────┐
│       Application Layer          │
│   Use Cases • Validation • DTOs  │
└───────────────┬──────────────────┘
                │ Interfaces
┌───────────────▼──────────────────┐
│         Domain Layer             │
│  Entities • Events • Rules       │
└───────────────┬──────────────────┘
                │ Implementations
┌───────────────▼──────────────────┐
│      Infrastructure Layer        │
│  DB • Channels • Workers         │
└──────────────────────────────────┘
```

---

## 4. Domain Model

### 4.1 Core Entities

| Entity | Description |
|--------|-------------|
| **Service** | A monitored unit (API, cron job, backup task) |
| **Monitor** | A monitoring configuration attached to a Service |
| **Alert** | A record of a state change event (failure, recovery) |
| **NotificationChannel** | A configured notification target (Telegram, etc.) |
| **MuteWindow** | A time-bounded suppression of alerts |
| **MonitorEvent** | A unified event record for all monitor activity (replaces MetricReading) |
| **MonitorRollup** | Pre-aggregated statistics for a monitor over a time period |

### 4.2 Value Objects

- `ServiceId` - Unique identifier for a service
- `MonitorId` - Unique identifier for a monitor
- `Interval` - Time interval with unit (seconds, minutes, hours, days)
- `GracePeriod` - Grace period before failure alert
- `Severity` - Alert severity level

### 4.3 Domain Enums

```csharp
enum ServiceState { Unknown, Up, Down, Paused }
enum MonitorType { Webhook, Heartbeat, HealthCheck, Metric }
enum AlertType { Failure, Recovery, MissedHeartbeat }
enum Severity { Low, Medium, High, Critical }
enum EventType { WebhookReceived, HeartbeatReceived, HealthCheckPerformed, MetricIngested, StateChanged }
enum Granularity { Hourly, Daily, Weekly, Monthly }
```

### 4.4 State Machine

Services follow a defined state machine:

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

**Rules:**
- New services start as `UNKNOWN`
- First successful check transitions to `UP`
- Failures transition to `DOWN`
- Recovery transitions back to `UP`
- `PAUSED` can be entered from any state (manual action)
- Resume from `PAUSED` goes to `UNKNOWN` and re-evaluates
- Alerts only sent on state transitions (not repeated for same state)

---

## 5. Clean Architecture Layers

### 5.1 Domain Layer (Core)

**Purpose:** Business rules and invariants

**Contents:**
- Entities (Service, Monitor, Alert, NotificationChannel, MonitorEvent, MonitorRollup)
- Value Objects (ServiceId, Interval, GracePeriod, Severity)
- Domain Enums (ServiceState, MonitorType, AlertType)
- Domain Events (ServiceFailed, ServiceRecovered)

**Rules:**
- No dependencies on other layers
- No framework or infrastructure code
- Pure business logic

---

### 5.2 Application Layer

**Purpose:** Use cases and orchestration

**Contents:**
- Use Cases / Commands / Queries
- Interfaces (repositories, notification dispatchers)
- DTOs (input/output models)
- FluentValidation validators

**Example Use Cases:**
- `CreateService`
- `UpdateService`
- `DeleteService`
- `RecordHeartbeat`
- `RecordWebhookFailure`
- `RecordWebhookRecovery`
- `PauseService`
- `ResumeService`
- `MuteService`
- `AcknowledgeAlert`
- `DispatchNotification`

**Rules:**
- Depends only on Domain
- No HTTP, DB, or Telegram specifics
- Defines interfaces, does not implement them

---

### 5.3 Infrastructure Layer

**Purpose:** External concerns and implementations

**Contents:**
- EF Core DbContext & migrations
- Repository implementations
- Notification channel implementations (Telegram)
- Background workers (heartbeat monitor, alert dispatcher)
- External service clients

**Rules:**
- Implements Application interfaces
- Can depend on frameworks and SDKs
- Contains all external I/O

---

### 5.4 API Layer

**Purpose:** HTTP transport & composition root

**Contents:**
- ASP.NET controllers (or minimal APIs)
- Request/response models
- Authentication middleware (Basic Auth)
- Dependency injection wiring
- Webhook endpoints

**API Structure:**
```
/api/v1/services          - Service CRUD
/api/v1/services/{id}     - Single service operations
/api/v1/alerts            - Alert listing
/api/v1/channels          - Notification channel config

/webhook/{token}/fail     - Failure webhook
/webhook/{token}/recover  - Recovery webhook
/heartbeat/{token}        - Heartbeat endpoint
```

**Rules:**
- No business logic
- Calls Application layer only
- Handles HTTP concerns (auth, routing, serialization)

---

## 6. Validation Strategy

### 6.1 FluentValidation

- All input validation in Application layer
- One validator per command/query
- Validation runs before use-case execution

**Examples:**
- Service name: required, max 100 chars
- Interval: minimum 30 seconds
- URL: valid format for health checks
- Grace period: minimum 1 minute

---

## 7. Notification Channel Architecture

### 7.1 Core Concept

The system emits **Alerts** which are dispatched to enabled **NotificationChannels** via a common interface.

### 7.2 Key Interfaces

```csharp
interface INotificationChannel
{
    string ChannelType { get; }
    bool IsEnabled { get; }
    Task<bool> SendAsync(Alert alert);
    Task<bool> ValidateConfigurationAsync();
}

interface INotificationDispatcher
{
    Task DispatchAsync(Alert alert);
}
```

### 7.3 Notification Flow

```
State Change Detected
        ↓
Create Alert Record
        ↓
NotificationDispatcher
        ↓
    ┌───┴───┐
    ↓       ↓
Telegram  (Future: Email, SMS, Webhook)
```

### 7.4 Channel Isolation

- Each channel handles delivery failures internally
- One channel failure does not block others
- Retry logic per channel
- Failed deliveries logged for diagnostics

---

## 8. Telegram Integration

### 8.1 Features (Phase 1)

**Notifications:**
- Alert messages on state transitions
- Inline buttons: Acknowledge, Mute 15m, Mute 1h, Mute 24h
- Callback handling for button presses

**Bot Commands:**
- `/status` - Overview of all services
- `/list` - List services with current states
- `/mute <service> <duration>` - Mute a service

### 8.2 Configuration

- Bot token via environment variable
- Chat ID via environment variable
- Polling or webhook mode (configurable)

---

## 9. Background Processing

### 9.1 Hosted Services

Implemented as .NET `IHostedService`:

| Worker | Responsibility |
|--------|----------------|
| `HeartbeatMonitorWorker` | Check for missed heartbeats, trigger alerts |
| `AlertDispatchWorker` | Process pending alerts, send to channels |
| `MaintenanceResumeWorker` | Auto-resume services when maintenance ends |

### 9.2 Worker Rules

- Poll database at configured intervals
- Execute logic via Application layer use cases
- Never bypass use cases or directly modify state
- Structured logging for all operations

---

## 10. Data Access

### 10.1 Database

| Option | Use Case |
|--------|----------|
| SQLite | Default, single-node deployments |
| PostgreSQL | Optional, larger deployments |

### 10.2 Repository Pattern

- Interfaces defined in Application layer
- Implementations in Infrastructure layer
- EF Core for ORM
- Migrations managed via EF Core

### 10.3 Key Tables

```
services
  - id, name, description, state, severity, created_at, updated_at

monitors
  - id, service_id, type, config_json, interval, grace_period

alerts
  - id, service_id, type, severity, message, created_at, acknowledged_at

notification_channels
  - id, type, config_json, enabled

mute_windows
  - id, service_id, starts_at, ends_at, reason
```

---

## 11. Authentication

### 11.1 Phase 1: Basic Auth

- Single user
- Credentials from environment variables: `MKAT_USERNAME`, `MKAT_PASSWORD`
- Stateless authentication (validate each request)
- Applied to all API endpoints except webhooks and heartbeats

### 11.2 Webhook Security

- Unguessable tokens (UUID v4)
- No authentication required (security via obscurity + optional HMAC later)
- Rate limiting (future)

---

## 12. Frontend Architecture

### 12.1 Stack

- React
- TanStack Router
- TanStack Query
- Tailwind CSS
- Vite (build tooling)

### 12.2 Responsibilities

- UI rendering and state
- API communication
- No business logic in frontend

### 12.3 Key Views

- Dashboard (service overview, active alerts)
- Service list (CRUD operations)
- Service detail (history, configuration)
- Alerts timeline
- Settings (notification channels)

---

## 13. Deployment Architecture

### 13.1 Packaging

- Single Docker image
- API serves frontend static assets
- SQLite database in mounted volume

### 13.2 Runtime

```
┌─────────────────────────────────────┐
│           Reverse Proxy             │
│         (TLS termination)           │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│            mkat Container           │
│  ┌─────────────────────────────┐    │
│  │      ASP.NET Core API       │    │
│  │   + Background Workers      │    │
│  │   + Static Frontend Assets  │    │
│  └─────────────────────────────┘    │
│               │                     │
│  ┌────────────▼────────────────┐    │
│  │    SQLite (volume mount)    │    │
│  └─────────────────────────────┘    │
└─────────────────────────────────────┘
```

### 13.3 Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `MKAT_USERNAME` | Yes | Basic auth username |
| `MKAT_PASSWORD` | Yes | Basic auth password |
| `MKAT_TELEGRAM_BOT_TOKEN` | Yes* | Telegram bot token |
| `MKAT_TELEGRAM_CHAT_ID` | Yes* | Telegram chat ID |
| `MKAT_DATABASE_PATH` | No | SQLite file path (default: `/data/mkat.db`) |
| `MKAT_LOG_LEVEL` | No | Log level (default: `Information`) |

*Required if Telegram notifications enabled

---

## 14. Observability

### 14.1 Logging

- Structured logging (Serilog)
- Correlation IDs per request
- Log levels configurable via environment
- Separate log streams for:
  - API requests
  - Background workers
  - Notification delivery

### 14.2 Health Endpoints

- `GET /health` - Basic health check
- `GET /health/ready` - Readiness (DB connected, Telegram reachable)

---

## 15. Architectural Constraints

1. Clean Architecture boundaries must be enforced
2. No Infrastructure references in Domain or Application
3. Notification channels must be pluggable (implement interface)
4. All input validation via FluentValidation
5. All state changes go through use cases
6. Workers never bypass the Application layer

---

## 16. Future Evolution

- Extract workers into separate services
- Add message queue for async processing
- Additional notification channels (Email, SMS, Slack, Discord)
- Multi-user support with RBAC
- PostgreSQL as primary database option
- Prometheus metrics endpoint

---

**Status:** Living document
**Audience:** Developers and AI agents
**Role:** Architectural source of truth
**Updated:** 2026-01-22
