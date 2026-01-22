# PRD & Architecture Clarifications

This document lists ambiguities, gaps, and open questions identified during PRD review. Items marked **[DECIDED]** have been resolved. Items marked **[OPEN]** require input.

---

## 1. Authentication & Security

### 1.1 Authentication Method
**[DECIDED]** Basic Auth for Phase 1

- [x] **Username/password storage**: **Environment variable only** (single user)
- [x] **Single user or multiple users**: **Single user** in Phase 1
- [ ] **Session handling**: Stateless (check every request) or session-based?

### 1.2 Webhook Security
- [ ] **Webhook URL format**: What length/entropy? (e.g., `/webhook/{uuid}` or `/webhook/{base64-token}`)
- [ ] **Optional HMAC**: Should this be in Phase 1 or Phase 2?
- [ ] **Rate limiting**: Any limits on webhook endpoints?

### 1.3 API Security
- [ ] **CORS policy**: Allow all origins or configurable?
- [ ] **API rate limiting**: Required for Phase 1?

---

## 2. State Management

### 2.1 Service States
**[DECIDED]** Explicit state transition rules with maintenance mode

States: `UP`, `DOWN`, `PAUSED`, `UNKNOWN`

#### State Transitions:

```
                    ┌─────────────────────────────────────┐
                    │                                     │
                    ▼                                     │
              ┌─────────┐                                 │
     ┌───────►│ UNKNOWN │◄────────── (service created)   │
     │        └────┬────┘                                 │
     │             │                                      │
     │             │ first successful check               │
     │             ▼                                      │
     │        ┌─────────┐         failure          ┌─────────┐
     │        │   UP    │─────────────────────────►│  DOWN   │
     │        └────┬────┘                          └────┬────┘
     │             ▲                                    │
     │             │          recovery                  │
     │             └────────────────────────────────────┘
     │
     │        ┌──────────────────────────┐
     └────────│ PAUSED (maintenance mode)│◄──── (manual action from any state)
              │ - optional end time      │
              │ - auto-resume (optional) │
              └──────────────────────────┘
                   │
                   │ resume (manual or auto) → UNKNOWN (re-evaluate)
                   │
                   └──────────────────────────────────────────────►
```

**[DECIDED]:**
- [x] Checks **do not run** while PAUSED (or run but suppress alerts) - TBD implementation detail
- [x] PAUSED services shown differently in dashboard
- [x] When resuming from PAUSED → goes to **UNKNOWN** and re-evaluates
- [x] PAUSED supports **optional end time** for scheduled maintenance
- [x] **Auto-resume is optional** (configurable per pause action)

### 2.2 Initial State
**[DECIDED]** New services start as `UNKNOWN` until first check

---

## 3. Heartbeat Monitoring

### 3.1 Intervals
**[DECIDED]**

- [x] **Minimum interval**: **30 seconds**
- [ ] **Maximum interval**: Any upper limit? (suggest: 7 days)
- [ ] **Custom intervals**: Allow arbitrary values or only presets?

### 3.2 Grace Period
**[DECIDED]**

- [x] **Default grace period**: **10% of interval, minimum 1 minute**
- [x] **Configurable per service**: Yes
- [x] **Minimum grace period**: **1 minute**

### 3.3 Heartbeat Endpoint
- [ ] **Response format**: Just 200 OK, or return JSON with next expected time?
- [ ] **Accept payload**: Should heartbeat accept optional status data?

---

## 4. Health Checks (Phase 2)

### 4.1 Configuration
- [ ] **Default timeout**: 30 seconds? Configurable?
- [ ] **Retry count**: Retry before marking as failed? How many?
- [ ] **Check interval**: Minimum/maximum bounds?

### 4.2 Response Validation
- [ ] **Body match**: Exact string, contains, or regex?
- [ ] **Multiple status codes**: How to specify? (e.g., "200,201,204" or "2xx")
- [ ] **Response time threshold**: Alert if slow but successful?

### 4.3 TLS/SSL
- [ ] **Certificate validation**: Strict or allow self-signed?
- [ ] **Option to ignore cert errors** per service?

---

## 5. Notifications & Alerts

### 5.1 Alert Behavior
**[DECIDED]**

- [ ] **Duplicate prevention window**: How long before re-alerting for same issue?
- [x] **Retry on failure**: **Yes** - retry if Telegram delivery fails
- [ ] **Retry count/interval**: How many retries? What interval?
- [ ] **Batch notifications**: Group multiple failures into one message?

### 5.2 Mute Behavior
- [ ] **Mute scope**: Per-service or global?
- [x] **Mute during maintenance**: Handled via PAUSED state with optional end time
- [ ] **Unmute notification**: Alert when mute expires?

### 5.3 Telegram Specifics
**[DECIDED]**

- [ ] **Message format**: Markdown or HTML?
- [x] **Inline buttons (Phase 1)**:
  - Acknowledge
  - Mute 15 minutes
  - Mute 1 hour
  - Mute 24 hours
- [x] **Bot commands (Phase 1)**:
  - `/status` - Show overview of all services
  - `/list` - List services with states
  - `/mute <service> <duration>` - Mute from Telegram

---

## 6. Data & Persistence

### 6.1 Alert History
**[DECIDED]**

- [x] **Retention period**: **Forever** (scheduled cleanup implemented later)
- [ ] **Alert limit per service**: Max alerts stored?
- [ ] **Automatic cleanup**: Deferred to future phase

### 6.2 Database
- [ ] **SQLite file location**: Configurable via env var?
- [ ] **Backup strategy**: Any built-in export?
- [ ] **Migration path**: SQLite → PostgreSQL documented?

---

## 7. API Design

### 7.1 Versioning
**[DECIDED]** URL versioning: `/api/v1/...`

### 7.2 Pagination
**[DECIDED]**

- [x] **Default page size**: **20**
- [ ] **Max page size**: Limit? (suggest: 100)
- [ ] **Pagination style**: Offset-based or cursor-based?

### 7.3 Error Format
- [ ] **Standard error response structure?**
  ```json
  {
    "error": "...",
    "code": "...",
    "details": {}
  }
  ```

---

## 8. UI/UX

### 8.1 Dashboard
- [ ] **Auto-refresh interval**: 30s? 60s? Configurable?
- [ ] **Sound/browser notifications**: In scope for Phase 1?

### 8.2 Service List
- [ ] **Default sort**: By name? By state? By last alert?
- [ ] **Filtering**: By state? By type?
- [ ] **Search**: In Phase 1?

### 8.3 Timezone
- [ ] **Display timezone**: UTC? Local? Configurable?
- [ ] **Storage timezone**: Always UTC?

---

## 9. Deployment & Operations

### 9.1 Configuration
- [ ] **Configuration method**: Environment variables only? Config file option?
- [ ] **Required vs optional settings**: What's mandatory?
- [x] **Sensitive data handling**: **Env vars only** (auth credentials)

### 9.2 Health Endpoint
- [ ] **Self health check**: `/health` endpoint for the service itself?
- [ ] **Readiness vs liveness**: Separate endpoints?

### 9.3 Logging
- [ ] **Log level**: Configurable via env var?
- [ ] **Log format**: JSON structured? Plain text option?

---

## 10. Terminology
**[DECIDED]** Unified terminology

| Concept | Term |
|---------|------|
| A monitored unit | **Service** |
| A monitoring rule/config | **Monitor** |
| A state change notification record | **Alert** |
| Notification target | **NotificationChannel** |

---

## 11. Scope Boundaries

### 11.1 Phase 1 Scope
**[DECIDED]**

- [x] Service CRUD via API and UI
- [x] Webhook failure/recovery endpoints
- [x] Heartbeat monitoring with configurable intervals
- [x] Telegram notifications with inline buttons (Ack, Mute 15m/1h/24h)
- [x] Telegram bot commands (`/status`, `/list`, `/mute`)
- [x] Basic dashboard showing service states
- [x] Basic auth (single user, env var)
- [x] API versioning (`/api/v1/...`)

### 11.2 Phase 2 Scope
- [ ] Automated HTTP health checks
- [ ] Integration management UI
- [ ] Enhanced Telegram interactions

### 11.3 Future Phases
- [ ] Email/SMS/custom webhook channels
- [ ] Multi-user support
- [ ] Scheduled cleanup jobs
- [ ] Advanced analytics

---

## Decisions Summary

| Topic | Decision |
|-------|----------|
| Authentication | Basic Auth, single user, credentials in env var |
| State Machine | UP/DOWN/PAUSED/UNKNOWN with maintenance mode |
| Maintenance Mode | PAUSED + optional end time + optional auto-resume |
| Resume Behavior | Returns to UNKNOWN, re-evaluates |
| Heartbeat Min Interval | 30 seconds |
| Grace Period | 10% of interval, minimum 1 minute |
| Alert Retention | Forever (cleanup later) |
| API Versioning | `/api/v1/...` |
| Pagination Default | 20 items |
| Telegram Buttons | Acknowledge, Mute 15m/1h/24h |
| Telegram Commands | `/status`, `/list`, `/mute <service> <duration>` |
| Naming: Monitor Config | **Monitor** |
| Naming: State Record | **Alert** |
| Naming: Notification Target | **NotificationChannel** |

---

**Status:** Major decisions complete, minor details open
**Updated:** 2026-01-22
