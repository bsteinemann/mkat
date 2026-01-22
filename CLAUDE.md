# mkat - Agent Instructions

## Project Summary

mkat is a self-hosted healthcheck and monitoring service for homelabs and small web projects.
It monitors services via webhooks, heartbeats, and health checks, delivering notifications through Telegram (initially).

**Tech Stack:**
- Backend: .NET 8+ (ASP.NET Core, EF Core, FluentValidation, Serilog)
- Frontend: React (TanStack Router, TanStack Query, Tailwind CSS, Vite)
- Database: SQLite (default), PostgreSQL (optional)
- Deployment: Docker (single container)

## Architecture

Modular monolith using Clean Architecture (4 layers, strict dependency direction):

```
Domain (core, no dependencies)
  ↑
Application (use cases, interfaces, DTOs, validators)
  ↑
Infrastructure (EF Core, repositories, Telegram, workers)
  ↑
API (controllers, middleware, DI wiring)
```

**Rule:** Dependencies point inward only. Controllers never call Infrastructure directly.

## Project Structure

```
src/
  Mkat.Domain/              # Entities, enums, value objects
  Mkat.Application/         # Use cases, interfaces, DTOs, validators
  Mkat.Infrastructure/      # EF Core, repositories, channels, workers
  Mkat.Api/                 # Controllers, middleware, DI, Program.cs
  mkat-ui/                  # React frontend (Vite)
tests/
  Mkat.Domain.Tests/
  Mkat.Application.Tests/
  Mkat.Api.Tests/
docs/
  plans/                    # Milestone implementation plans (m1-m6)
  adr/                      # Architecture Decision Records
  workflow.md               # Implementation workflow (6 phases)
  learnings.md              # Accumulated patterns/anti-patterns
  changelog.md              # Running change log
  architecture.md           # Full architecture specification
  roadmap.md                # Milestone breakdown
  telegram_healthcheck_monitoring_prd.md  # Product requirements
```

## Key Documents (Read Before Working)

1. `docs/learnings.md` - Check FIRST for known patterns and gotchas
2. `docs/plans/m{N}-*.md` - Detailed plan for current milestone
3. `docs/architecture.md` - Layer rules and domain model
4. `docs/telegram_healthcheck_monitoring_prd.md` - Full requirements
5. `docs/workflow.md` - Step-by-step implementation process

## Coding Conventions

### C# / .NET

- File-scoped namespaces: `namespace Mkat.Domain.Entities;`
- Records for DTOs: `public record CreateServiceRequest { ... }`
- Async suffix on async methods: `GetByIdAsync`
- `CancellationToken` on all async signatures
- FluentValidation for all input validation (Application layer)
- Repository pattern: interfaces in Application, implementations in Infrastructure
- No business logic in Controllers - delegate to Application layer
- Structured logging with Serilog (semantic templates, not string interpolation)
- `DateTime.UtcNow` for all timestamps
- Use `Guid` for entity IDs

### React / TypeScript

- Functional components only
- TanStack Query for all server state
- TanStack Router for routing
- Tailwind CSS for styling (no CSS modules, no styled-components)
- Keep components small and composable
- Custom hooks for shared logic (prefix with `use`)
- Types in `src/api/types.ts` matching backend DTOs

### General

- Prefer explicit over clever
- Comments explain WHY, not WHAT
- Error messages should be actionable
- One concern per file

## Commands

```bash
# Build & Run
dotnet build
dotnet run --project src/Mkat.Api
cd src/mkat-ui && npm run dev
docker compose -f docker-compose.dev.yml up

# Test
dotnet test
dotnet test tests/Mkat.Domain.Tests
cd src/mkat-ui && npm test

# Database Migrations
dotnet ef migrations add <Name> -p src/Mkat.Infrastructure -s src/Mkat.Api
dotnet ef database update -p src/Mkat.Infrastructure -s src/Mkat.Api
```

## Testing Expectations

- Unit tests for all domain logic and validators
- Integration tests for API endpoints (use `WebApplicationFactory`)
- Test file naming: `{ClassName}Tests.cs`
- Arrange/Act/Assert pattern
- Test one behavior per test method
- Cover edge cases: nulls, empty inputs, invalid state transitions

## Implementation Workflow

Follow `docs/workflow.md` for every task. Summary:

1. **Understand** - Read plans, learnings, architecture
2. **Plan** - List files, identify tests, check if ADR needed
3. **Implement** - Domain-outward, conventions, DI wiring
4. **Test** - Unit + integration, full suite passes
5. **Document** - Changelog entry, ADR if needed, fix stale docs
6. **Retrospective** - Add to learnings.md, update CLAUDE.md if needed

The retrospective step is NOT optional. It makes future tasks more efficient.

## What NOT To Do

- DO NOT add packages without checking if an existing one suffices
- DO NOT put business logic in controllers or infrastructure
- DO NOT skip validation (every command/query needs a validator)
- DO NOT use raw SQL - use EF Core LINQ
- DO NOT hardcode configuration - use environment variables
- DO NOT commit secrets (.env, tokens, passwords)
- DO NOT create god classes - keep files focused and small
- DO NOT skip the retrospective step after implementation
- DO NOT ignore `docs/learnings.md` - check it before starting work
- DO NOT leave stale documentation - update it or delete it

## Domain Terminology

| Term | Meaning |
|------|---------|
| Service | A monitored unit (API, job, container) |
| Monitor | A monitoring rule attached to a service |
| Alert | A notification record of a state change |
| NotificationChannel | A delivery target for alerts (Telegram, etc.) |
| MuteWindow | Time-bounded alert suppression |

## Service States

`UNKNOWN` → `UP` ↔ `DOWN`, any → `PAUSED` (maintenance), `PAUSED` → `UNKNOWN` (resume)

Alerts are sent only on state transitions.

## Environment Variables

| Variable | Required | Purpose |
|----------|----------|---------|
| MKAT_USERNAME | Yes | Basic auth username |
| MKAT_PASSWORD | Yes | Basic auth password |
| MKAT_TELEGRAM_BOT_TOKEN | Conditional | Telegram bot token |
| MKAT_TELEGRAM_CHAT_ID | Conditional | Telegram chat ID |
| MKAT_DATABASE_PATH | No | SQLite path (default: mkat.db) |
| MKAT_LOG_LEVEL | No | Log level (default: Information) |
