# Changelog

All notable changes to mkat, ordered newest-first.

---

## [Unreleased]

### 2026-01-30 - Historical Data & Analytics

- Added unified `MonitorEvent` entity replacing `MetricReading` for all monitor types
- Added `MonitorRollup` entity for pre-aggregated statistics (hourly, daily, weekly, monthly)
- Added `RollupCalculator` computing min, max, mean, median, P80, P90, P95, standard deviation, uptime %
- Added `RollupAggregationWorker` (hourly background job)
- Added `EventRetentionWorker` with tiered retention (events: 7d, hourly: 30d, daily: 1y, weekly: 2y, monthly: forever)
- Added API endpoints: `GET /monitors/{id}/events`, `GET /monitors/{id}/rollups`, `GET /services/{id}/uptime`, `GET /services/{id}/events`
- All monitor types (webhook, heartbeat, health check, metric) now log `MonitorEvent` records
- Health check events include response time in `Value` field
- Added Recharts-based history charts per monitor type on ServiceDetail page
- Added time range selector (1h–1y) with automatic data source selection (events vs rollups)
- Added uptime badge (color-coded: green >=99%, yellow >=95%, red <95%)
- Added rollup stats table (min, max, mean, median, P80, P90, P95, stdev)
- Removed deprecated `MetricReading` entity, `MetricReadingRepository`, `MetricHistoryController`, `MetricRetentionWorker`
- `MetricEvaluator` now uses `IMonitorEventRepository` for threshold strategies

### 2026-01-22 - M5: Frontend

- Scaffolded React 19 + TypeScript project with Vite 7
- Configured Tailwind CSS v4 with @tailwindcss/vite plugin
- Created API client layer (types, client, services, alerts) matching backend DTOs
- Built Layout with Header, Sidebar, and responsive main content area
- Created components: StateIndicator (with pulse for Down), ServiceCard, ServiceForm, AlertItem, CopyableUrl, Pagination
- Implemented pages: Dashboard (status overview + recent alerts), Services (list + pause/resume), ServiceDetail (monitors + alert history), ServiceCreate, ServiceEdit (with delete), Alerts (list + acknowledge), Login
- Set up TanStack Router with auth guard (redirect to /login when unauthenticated)
- Configured TanStack Query with auto-refresh (30s interval on dashboard/alerts)
- Vite dev proxy forwards /api, /webhook, /heartbeat, /health to backend
- Production build outputs to Mkat.Api/wwwroot
- API serves SPA with UseDefaultFiles, UseStaticFiles, MapFallbackToFile

### 2026-01-22 - M4: Notifications

- Added INotificationChannel and INotificationDispatcher interfaces
- Created NotificationDispatcher service (dispatches to all enabled channels, marks DispatchedAt)
- Created TelegramChannel with MarkdownV2 formatting, inline keyboards, retry logic
- Created TelegramBotService with command handling (/status, /list, /mute) and callback handling (ack, mute buttons)
- Created AlertDispatchWorker (polls pending alerts every 5s)
- Created AlertsController (GET /api/v1/alerts, GET /{id}, POST /{id}/ack)
- Added Mute endpoint to ServicesController (POST /api/v1/services/{id}/mute)
- Added AlertResponse and MuteRequest DTOs
- Wired all notification DI: TelegramOptions, TelegramChannel, NotificationDispatcher, AlertDispatchWorker, TelegramBotService
- 52 new tests (237 total: 56 Domain + 55 Application + 126 API)

### 2026-01-22 - M3: Monitoring Engine

- Added StateService with state machine logic (UP/DOWN/PAUSED/UNKNOWN transitions)
- Created WebhookController (POST /webhook/{token}/fail, /webhook/{token}/recover)
- Created HeartbeatController (POST /heartbeat/{token})
- Added Pause/Resume endpoints to ServicesController
- Created HeartbeatMonitorWorker (detects missed heartbeats, triggers DOWN alerts)
- Created MaintenanceResumeWorker (auto-resumes paused services after window expires)
- Added IMuteWindowRepository for alert suppression during mute windows
- Added GetPausedServicesAsync to IServiceRepository
- Added PauseRequest DTO
- Alert creation on state transitions (with mute window checking)
- Duplicate state transitions produce no duplicate alerts
- 62 new tests (185 total: 56 Domain + 46 Application + 83 API)

### 2026-01-22 - M2: Core API

- Added BasicAuthMiddleware (skips /health, /webhook, /heartbeat; 500 if password not configured)
- Created DTOs: CreateServiceRequest, UpdateServiceRequest, ServiceResponse, MonitorResponse, PagedResponse, ErrorResponse
- Created FluentValidation validators for create/update requests
- Implemented ServicesController with full CRUD (POST, GET, GET/{id}, PUT/{id}, DELETE/{id})
- Pagination support with configurable page size (max 100)
- Monitor URL generation (webhook fail/recover, heartbeat)
- 38 API integration tests, 29 application tests passing

### 2026-01-22 - M1: Foundation

- Set up Clean Architecture project structure (Domain, Application, Infrastructure, Api)
- Created domain entities (Service, Monitor, Alert, NotificationChannel, MuteWindow)
- Created EF Core DbContext and repositories
- Added initial SQLite migration
- Set up dev container configuration
- 56 domain unit tests passing

### 2026-01-22 - Project Documentation

- Created PRD with full requirements specification
- Created consolidated architecture document
- Created 6-milestone roadmap
- Created implementation plans (M1-M6)
- Set up AI agent workflow system (CLAUDE.md, workflow, learnings, ADRs)
