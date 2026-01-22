## Current State

**Completed:** M1 (Foundation), M2 (Core API), M3 (Monitoring Engine)
**Next:** M4 (Notifications)

**Test suite:** 185 tests passing (56 Domain + 46 Application + 83 API)
**Branch:** `main`, all committed, no uncommitted changes

## To Continue

1. Read `docs/plans/m4-notifications.md` for the implementation plan
2. Read `docs/learnings.md` for known gotchas (especially: Monitor ambiguity, worker test placement, TDD gate enforcement)
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

## Commands

```bash
dotnet build          # Build all projects
dotnet test           # Run all 185 tests
dotnet run --project src/Mkat.Api  # Run the API (needs MKAT_USERNAME/MKAT_PASSWORD env vars)
```

## Remaining Milestones

- **M4:** Notifications (Telegram integration, alert channels, dispatch worker)
- **M5:** Frontend (React UI with TanStack Router/Query)
- **M6:** Polish & Documentation (Docker, error handling, docs)
