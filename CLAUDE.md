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

## Pre-Task Checklist (MANDATORY)

Before writing ANY code, complete these steps and confirm each one:

1. **Read `docs/learnings.md`** - Mention at least one relevant entry (or confirm none apply)
2. **Read the milestone plan** - `docs/plans/m{N}-*.md` for current milestone
3. **Identify the first failing test** - What test will you write FIRST? Name it.
4. **Confirm layer** - Which layer(s) will be modified? (Domain/Application/Infrastructure/API)

If you are a sub-agent: your prompt MUST include the contents of `docs/learnings.md` and the relevant plan file. If they are not in your prompt, request them before proceeding.

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
- `Monitor` conflicts with `System.Threading.Monitor` under ImplicitUsings — always add `using Monitor = Mkat.Domain.Entities.Monitor;` in files that import the domain entity
- Use `StringComparison.Ordinal` with `StartsWith`/`EndsWith` and `CultureInfo.InvariantCulture` with `Parse`/`ToString` — the build enforces this via analyzers

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

## ASP.NET Middleware Order

The SPA + API middleware must be registered in this order:

```
UseDefaultFiles → UseStaticFiles → UsePathBase → auth middleware → MapControllers → MapFallback
```

Static files must come before auth or they get blocked. `MapFallback` must be last.

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

# Lint & Format (frontend)
cd src/mkat-ui && npm run lint
cd src/mkat-ui && npm run format:check
cd src/mkat-ui && npm run format          # auto-fix

# Database Migrations
dotnet ef migrations add <Name> -p src/Mkat.Infrastructure -s src/Mkat.Api
dotnet ef database update -p src/Mkat.Infrastructure -s src/Mkat.Api
```

## Test-Driven Development (TDD)

**TDD is mandatory for all implementation work.** Follow Red-Green-Refactor:

1. **Red:** Write a failing test that describes the desired behavior
2. **Green:** Write the minimum code to make the test pass
3. **Refactor:** Clean up while keeping tests green

### TDD Gate (Enforcement)

Before writing implementation code, you MUST:
1. Write the test file/method first
2. Run `dotnet test` and observe the failure (compile error or assertion failure)
3. Show/log the failing output
4. ONLY THEN write the implementation

This is a gate, not a suggestion. If you find yourself writing implementation code and realize no failing test exists yet, STOP, delete the implementation, write the test, confirm it fails, then re-implement.

### TDD Rules

- NEVER write implementation code without a failing test first
- Tests define the API contract - write them from the consumer's perspective
- One test at a time: write one failing test, make it pass, then next test
- Run `dotnet test` after each green step to confirm no regressions
- If a bug is found, write a test that reproduces it BEFORE fixing it

### Test Structure

- Unit tests for all domain logic and validators
- Integration tests for API endpoints (use `WebApplicationFactory`)
- Test file naming: `{ClassName}Tests.cs`
- Arrange/Act/Assert pattern
- Test one behavior per test method
- Cover edge cases: nulls, empty inputs, invalid state transitions
- Tests that reference Infrastructure types belong in `Api.Tests` (transitive access via Api project), not `Application.Tests`

### TDD Workflow Per Feature

```
1. Write test for first behavior → run → RED
2. Implement just enough code → run → GREEN
3. Refactor if needed → run → GREEN
4. Write test for next behavior → run → RED
5. Repeat until feature complete
6. Run full suite → ALL GREEN
7. Commit
```

## Git Commit Discipline

**Commit after every completed feature, behavior, or bugfix.** Small, atomic commits.

### Commit Rules

- Commit as soon as a logical unit of work is complete and tests pass
- Each commit must leave the codebase in a buildable, tests-passing state
- Never batch multiple unrelated changes into one commit
- Use conventional commit messages: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`

### Commit Granularity Examples

```
feat: add ServiceState enum                    (single entity/enum)
feat: add Service entity                       (single entity)
feat: add IServiceRepository interface         (single interface)
feat: add ServiceRepository implementation     (single implementation)
feat: add health endpoints                     (one feature)
fix: handle null service in GetById            (one bugfix)
refactor: extract validation to FluentValidator (one refactor)
test: add integration tests for health endpoint (test addition)
chore: add Docker configuration                (infrastructure)
```

### When to Commit

- After making a new test pass (if it represents a complete behavior)
- After completing a logical group of related tests + implementation
- After a refactoring step (tests still green)
- After adding infrastructure (Docker, config, migrations)
- After fixing a bug (with its regression test)

## Implementation Workflow

Follow `docs/workflow.md` for every task. Summary:

1. **Understand** - Read plans, learnings, architecture
2. **Plan** - List files, identify tests FIRST, check if ADR needed
3. **Implement (TDD)** - Write failing test → make it pass → refactor → commit
4. **Verify** - Run full test suite, confirm all green
5. **Document** - Changelog entry, ADR if needed, fix stale docs
6. **Retrospective** - Add to learnings.md, update CLAUDE.md if needed

The retrospective step is NOT optional. It makes future tasks more efficient.
Write learnings IMMEDIATELY when a problem is solved, not at the end of a milestone.

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
- DO NOT write implementation code before writing a failing test
- DO NOT batch multiple features into a single commit
- DO NOT commit code that leaves tests failing

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

**Note:** The app is always served at `/mkat`. To serve at root, use reverse proxy URL rewriting.
