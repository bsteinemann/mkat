## Current State

**Completed:** M1 (Foundation), M2 (Core API), M3 (Monitoring Engine), M4 (Notifications)
**Next:** M5 (Frontend)

**Test suite:** 237 tests passing (56 Domain + 55 Application + 126 API)
**Branch:** `main`, all committed, no uncommitted changes

## To Continue

1. Read `docs/plans/m5-frontend.md` for the implementation plan
2. Read `docs/learnings.md` for known gotchas
3. Follow TDD workflow defined in `docs/workflow.md` Phase 3
4. Commit after each feature/bugfix using conventional commits (`feat:`, `fix:`, `test:`, etc.)

## Key Patterns Established

- **Integration tests:** Per-test `WebApplicationFactory` with unique `InMemoryDatabase` names, `[Collection("BasicAuth")]` for env var isolation
- **Monitor ambiguity:** Always use `using Monitor = Mkat.Domain.Entities.Monitor;` when both namespaces are in scope
- **InMemory DB:** Program.cs wraps migration code in try/catch, falls back to `EnsureCreated()`
- **global.json:** Uses `rollForward: latestMajor` (SDK is .NET 10 RC, target is net8.0)
- **Validators:** Registered individually in Program.cs DI, not via assembly scanning
- **Background workers:** Use `IServiceProvider.CreateScope()` pattern; make check methods public for testability
- **Worker tests:** Place in Api.Tests (not Application.Tests) due to Infrastructure dependency
- **State machine:** StateService handles all transitions, creates alerts, checks mute windows
- **Webhook/Heartbeat:** No auth required (bypassed in BasicAuthMiddleware)
- **Telegram.Bot v22.8.1:** Methods are `SendMessage`, `GetMe`, `AnswerCallbackQuery` (not async-suffixed)
- **TelegramBotClient construction:** Validates token format eagerly — wrap in try/catch
- **Notification DI:** TelegramChannel is singleton, NotificationDispatcher is scoped, workers are hosted services

## Commands

```bash
dotnet build          # Build all projects
dotnet test           # Run all 237 tests
dotnet run --project src/Mkat.Api  # Run the API (needs MKAT_USERNAME/MKAT_PASSWORD env vars)
```

## Remaining Milestones

- **M5:** Frontend (React UI with TanStack Router/Query)
- **M6:** Polish & Documentation (Docker, error handling, docs)
