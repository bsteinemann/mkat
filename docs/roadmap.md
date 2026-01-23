# mkat Development Roadmap

This document outlines the implementation roadmap for mkat Phase 1 (MVP).

---

## Overview

Phase 1 is divided into 6 milestones, each building on the previous. Each milestone produces working, testable functionality.

```
M1: Foundation → M2: Core API → M3: Monitoring → M4: Notifications → M5: Frontend → M6: Polish → M7: Metrics → M8: Peers → M9: Contacts
```

---

## Milestone 1: Foundation

**Goal:** Project structure, database, and basic infrastructure

### Deliverables
- [ ] Solution structure (Clean Architecture layers)
- [ ] Domain entities and value objects
- [ ] EF Core DbContext and migrations
- [ ] SQLite configuration
- [ ] Basic logging (Serilog)
- [ ] Health endpoints (`/health`, `/health/ready`)
- [ ] Docker setup (Dockerfile, docker-compose.dev.yml)

### Components
| Component | Layer | Description |
|-----------|-------|-------------|
| `Mkat.Domain` | Domain | Entities, enums, value objects |
| `Mkat.Application` | Application | Interfaces, DTOs (stubs) |
| `Mkat.Infrastructure` | Infrastructure | DbContext, migrations, repositories |
| `Mkat.Api` | API | Program.cs, health endpoints |

### Definition of Done
- Solution builds and runs
- Database created on startup
- Health endpoint returns 200
- Docker container runs successfully

---

## Milestone 2: Core API

**Goal:** Service CRUD operations with authentication

### Deliverables
- [ ] Basic Auth middleware
- [ ] Service entity and repository
- [ ] Service CRUD endpoints
- [ ] FluentValidation for service commands
- [ ] API versioning (`/api/v1/...`)
- [ ] Error response format
- [ ] Pagination support

### API Endpoints
```
POST   /api/v1/services          Create service
GET    /api/v1/services          List services (paginated)
GET    /api/v1/services/{id}     Get service
PUT    /api/v1/services/{id}     Update service
DELETE /api/v1/services/{id}     Delete service
```

### Dependencies
- Milestone 1 (Foundation)

### Definition of Done
- All CRUD endpoints functional
- Basic auth required for all endpoints
- Validation errors return proper format
- Pagination works correctly
- Integration tests pass

---

## Milestone 3: Monitoring Engine

**Goal:** Webhook and heartbeat monitoring with state machine

### Deliverables
- [ ] Monitor entity and repository
- [ ] State machine implementation
- [ ] Webhook endpoints (no auth)
- [ ] Heartbeat endpoint (no auth)
- [ ] HeartbeatMonitorWorker (background service)
- [ ] Pause/resume functionality
- [ ] MaintenanceResumeWorker (auto-resume)

### API Endpoints
```
POST   /webhook/{token}/fail      Report failure
POST   /webhook/{token}/recover   Report recovery
POST   /heartbeat/{token}         Record heartbeat
POST   /api/v1/services/{id}/pause   Pause service
POST   /api/v1/services/{id}/resume  Resume service
```

### Background Workers
| Worker | Interval | Responsibility |
|--------|----------|----------------|
| `HeartbeatMonitorWorker` | 10s | Check for missed heartbeats |
| `MaintenanceResumeWorker` | 60s | Auto-resume paused services |

### State Transitions
```
UNKNOWN → UP (first success)
UP → DOWN (failure)
DOWN → UP (recovery)
* → PAUSED (manual)
PAUSED → UNKNOWN (resume)
```

### Dependencies
- Milestone 2 (Core API)

### Definition of Done
- Webhook triggers state changes
- Heartbeat endpoint registers check-in
- Missed heartbeat triggers DOWN state
- Pause/resume works with optional end time
- Auto-resume triggers when maintenance ends
- State transitions logged correctly

---

## Milestone 4: Notifications

**Goal:** Alert system and Telegram integration

### Deliverables
- [ ] Alert entity and repository
- [ ] NotificationChannel entity and repository
- [ ] MuteWindow entity and repository
- [ ] INotificationChannel interface
- [ ] TelegramChannel implementation
- [ ] AlertDispatchWorker (background service)
- [ ] Mute functionality
- [ ] Alert acknowledgment
- [ ] Telegram inline buttons
- [ ] Telegram bot commands

### API Endpoints
```
GET    /api/v1/alerts             List alerts (paginated)
GET    /api/v1/alerts/{id}        Get alert
POST   /api/v1/alerts/{id}/ack    Acknowledge alert
POST   /api/v1/services/{id}/mute Mute service
GET    /api/v1/channels           List channels
PUT    /api/v1/channels/{id}      Update channel config
POST   /api/v1/channels/{id}/test Send test notification
```

### Telegram Features
| Feature | Description |
|---------|-------------|
| Alert messages | Formatted alert on state change |
| Inline buttons | Ack, Mute 15m, Mute 1h, Mute 24h |
| `/status` | Service overview |
| `/list` | List all services |
| `/mute <svc> <dur>` | Mute from Telegram |

### Background Workers
| Worker | Interval | Responsibility |
|--------|----------|----------------|
| `AlertDispatchWorker` | 5s | Send pending alerts to channels |

### Dependencies
- Milestone 3 (Monitoring Engine)

### Definition of Done
- Alerts created on state transitions
- Telegram receives notifications
- Inline buttons work (ack, mute)
- Bot commands respond correctly
- Muted services don't generate alerts
- Delivery failures logged and retried

---

## Milestone 5: Frontend

**Goal:** React UI for service management and monitoring

### Deliverables
- [ ] React project setup (Vite, TanStack, Tailwind)
- [ ] Basic auth login flow
- [ ] Dashboard page
- [ ] Services list page
- [ ] Service detail page
- [ ] Service create/edit forms
- [ ] Alerts page
- [ ] API integration (TanStack Query)
- [ ] Static asset serving from API

### Pages
| Page | Route | Features |
|------|-------|----------|
| Dashboard | `/` | State overview, active alerts, recent events |
| Services | `/services` | List, search, state indicators |
| Service Detail | `/services/{id}` | Config, history, webhook URLs |
| Create Service | `/services/new` | Form with validation |
| Edit Service | `/services/{id}/edit` | Pre-filled form |
| Alerts | `/alerts` | Timeline, filters, ack/mute actions |

### UI Components
- ServiceCard (state indicator, quick actions)
- AlertItem (with ack/mute buttons)
- StateIndicator (UP/DOWN/PAUSED/UNKNOWN)
- CopyableUrl (webhook/heartbeat URLs)
- PauseDialog (with optional end time)
- MuteDialog (duration picker)

### Dependencies
- Milestone 4 (Notifications) - for full functionality
- Can start in parallel with M3/M4 using mock data

### Definition of Done
- All pages functional
- Forms validate input
- API calls work correctly
- Auth redirects to login
- Responsive layout
- Served from API container

---

## Milestone 6: Polish & Documentation

**Goal:** Production readiness

### Deliverables
- [ ] Environment variable documentation
- [ ] Docker production image
- [ ] docker-compose.yml for deployment
- [ ] README with setup instructions
- [ ] API documentation (OpenAPI/Swagger)
- [ ] Error handling improvements
- [ ] Logging improvements
- [ ] Performance review
- [ ] Security review

### Documentation
| Document | Content |
|----------|---------|
| README.md | Quick start, configuration, deployment |
| docs/deployment.md | Production deployment guide |
| docs/api.md | API reference (or Swagger) |
| docs/telegram-setup.md | Telegram bot configuration |

### Dependencies
- All previous milestones

### Definition of Done
- Documentation complete
- Docker image builds and runs
- No console errors in production
- Logs are structured and useful
- Security checklist passed

---

## Milestone 7: Metrics Monitor

**Goal:** Metric value ingestion with configurable range thresholds

### Deliverables
- [ ] `MetricReading` entity and repository
- [ ] `MonitorType.Metric` enum value and `ThresholdStrategy` enum
- [ ] Metric config fields on Monitor (range, threshold, retention)
- [ ] Push endpoint (`/metric/{token}`)
- [ ] Threshold evaluation logic (Immediate, ConsecutiveCount, TimeDurationAverage, SampleCountAverage)
- [ ] Metric history query endpoints
- [ ] `MetricRetentionWorker` (background cleanup)
- [ ] FluentValidation for metric monitor config
- [ ] Frontend: metric monitor form, history view, latest value display
- [ ] Unit and integration tests

### API Endpoints
```
POST   /metric/{token}                          Push metric value (no auth)
GET    /api/v1/monitors/{id}/metrics            Metric history (paginated)
GET    /api/v1/monitors/{id}/metrics/latest     Latest value + status
```

### Dependencies
- Milestone 3 (Monitoring Engine)
- Milestone 5 (Frontend)

### Definition of Done
- Push endpoint stores readings and evaluates thresholds
- Out-of-range triggers DOWN, recovery triggers UP
- All 4 threshold strategies work correctly
- History queryable with time range
- Retention worker cleans old data
- Frontend displays metric config and history
- All tests pass

---

## Milestone 8: Peer Monitoring

**Goal:** Mutual availability monitoring and notification failure detection between mkat instances

### Deliverables
- [ ] `Peer` entity and repository
- [ ] Token-based pairing flow (initiate, complete, accept)
- [ ] Auto-creation of Service + Heartbeat Monitor + Webhook Monitor during pairing
- [ ] `PeerHeartbeatWorker` (sends heartbeats to peers)
- [ ] Notification failure hook in `AlertDispatchWorker`
- [ ] Unpair cleanup logic
- [ ] Frontend: peers page, pair dialog, peer status on dashboard
- [ ] Unit and integration tests

### API Endpoints
```
POST   /api/v1/peers/pair/initiate    Generate pairing token
POST   /api/v1/peers/pair/complete    Accept token and establish connection
POST   /api/v1/peers/pair/accept      Called between instances during pairing
GET    /api/v1/peers                   List paired instances
DELETE /api/v1/peers/{id}              Unpair
```

### Dependencies
- Milestone 3 (Monitoring Engine)
- Milestone 4 (Notifications)
- Milestone 5 (Frontend)

### Definition of Done
- Two instances can pair via token exchange
- Both send heartbeats and detect peer downtime
- Notification failures reported to peer via webhook
- Unpairing cleans up both sides
- Frontend shows peer status and pairing flow
- All tests pass

---

## Milestone 9: Contacts & Notification Routing

**Goal:** Per-service notification routing through named contacts with multiple channels

### Deliverables
- [ ] `Contact` and `ContactChannel` entities and repositories
- [ ] `ServiceContact` join entity (many-to-many)
- [ ] EF Core migration (new tables + data migration from global channel)
- [ ] Contact CRUD and channel management endpoints
- [ ] Service-contact assignment endpoints
- [ ] Updated `AlertDispatchWorker` to route via contacts
- [ ] Default contact creation and fallback logic
- [ ] Frontend: contacts page, channel management, service contact picker
- [ ] Unit and integration tests

### API Endpoints
```
POST   /api/v1/contacts                            Create contact
GET    /api/v1/contacts                            List contacts
GET    /api/v1/contacts/{id}                       Get contact (with channels)
PUT    /api/v1/contacts/{id}                       Update contact
DELETE /api/v1/contacts/{id}                       Delete contact
POST   /api/v1/contacts/{id}/channels              Add channel
PUT    /api/v1/contacts/{id}/channels/{chId}       Update channel
DELETE /api/v1/contacts/{id}/channels/{chId}       Remove channel
POST   /api/v1/contacts/{id}/channels/{chId}/test  Test notification
PUT    /api/v1/services/{id}/contacts              Set contacts for service
GET    /api/v1/services/{id}/contacts              Get contacts for service
```

### Dependencies
- Milestone 4 (Notifications)
- Milestone 5 (Frontend)

### Definition of Done
- Contacts with multiple channels can be created and managed
- Services can be assigned one or more contacts
- Alerts route to all enabled channels of all assigned contacts
- Default contact provides backward compatibility
- Cannot leave a service with zero contacts
- All tests pass

---

## Dependency Graph

```
M1 Foundation
    │
    ▼
M2 Core API
    │
    ▼
M3 Monitoring ─────┬────► M5 Frontend
    │              │         │
    ▼              │         │
M4 Notifications ──┘         │
    │                        │
    └────────────────────────┘
              │
              ▼
         M6 Polish
              │
              ▼
    M7 Metrics Monitor ◄── M3 + M5
              │
              ▼
    M8 Peer Monitoring ◄── M3 + M4 + M5
              │
              ▼
    M9 Contacts ◄── M4 + M5
```

---

## Component Summary

### Backend Projects
| Project | Layer | Purpose |
|---------|-------|---------|
| `Mkat.Domain` | Domain | Entities, value objects, enums |
| `Mkat.Application` | Application | Use cases, interfaces, DTOs, validators |
| `Mkat.Infrastructure` | Infrastructure | EF Core, repositories, Telegram, workers |
| `Mkat.Api` | API | Controllers, middleware, DI wiring |

### Frontend
| Component | Technology | Purpose |
|-----------|------------|---------|
| `mkat-ui` | React + Vite | Web interface |

### Shared
| Component | Purpose |
|-----------|---------|
| Dockerfile | Container image |
| docker-compose.yml | Production deployment |
| docker-compose.dev.yml | Development environment |

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Telegram API complexity | Start with simple polling, upgrade to webhooks later |
| State machine bugs | Comprehensive unit tests, state transition logging |
| Background worker reliability | Idempotent operations, proper error handling |
| Frontend/backend integration | Define API contracts early, use TypeScript |

---

## Success Metrics

- [ ] All Phase 1 features functional
- [ ] < 5 second alert delivery
- [ ] Zero duplicate alerts
- [ ] Clean startup/shutdown
- [ ] Graceful error handling
- [ ] Comprehensive logging

---

**Status:** Draft
**Updated:** 2026-01-22
