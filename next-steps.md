## Current State

**Completed:** M1 (Foundation), M2 (Core API), M3 (Monitoring Engine), M4 (Notifications), M5 (Frontend), M6 (Polish & Documentation)
**Next:** M7 (Metrics Monitor)

**Test suite:** 237 tests passing (56 Domain + 55 Application + 126 API)
**Frontend:** React 19 + Vite 7 + Tailwind v4, builds to wwwroot
**Branch:** `main`, all committed, no uncommitted changes

## To Continue

1. Read `docs/plans/m7-metrics-monitor.md` for the implementation plan
2. Read `docs/learnings.md` for known gotchas
3. Follow TDD workflow defined in `docs/workflow.md` Phase 3
4. Commit after each feature/bugfix using conventional commits (`feat:`, `fix:`, `test:`, etc.)

## Key Patterns Established

- **Integration tests:** Per-test `WebApplicationFactory` with unique `InMemoryDatabase` names
- **Monitor ambiguity:** Always use `using Monitor = Mkat.Domain.Entities.Monitor;`
- **Background workers:** Use `IServiceProvider.CreateScope()` pattern; make check methods public for testability
- **Telegram.Bot v22.8.1:** Methods are `SendMessage`, `GetMe`, `AnswerCallbackQuery` (not async-suffixed)
- **Notification DI:** TelegramChannel is singleton, NotificationDispatcher is scoped, workers are hosted services
- **Tailwind v4:** `@import "tailwindcss"` + `@tailwindcss/vite` plugin (no config file needed)
- **SPA serving:** UseDefaultFiles → UseStaticFiles → auth middleware → MapControllers → MapFallbackToFile
- **Type imports:** Use `import type { ... }` for interfaces to avoid Rollup warnings
- **Vite proxy:** Forwards /api, /webhook, /heartbeat, /health to localhost:5000 in dev

## Commands

```bash
dotnet build          # Build all .NET projects
dotnet test           # Run all 237 tests
dotnet run --project src/Mkat.Api  # Run the API (needs MKAT_USERNAME/MKAT_PASSWORD env vars)
cd src/mkat-ui && npm run dev      # Run frontend dev server (port 5173, proxies to API)
cd src/mkat-ui && npm run build    # Build frontend to wwwroot
```

## Remaining Milestones

- **M7:** Metrics Monitor (push-based metric ingestion, configurable thresholds, history)
- **M8:** Peer Monitoring (mutual instance monitoring via heartbeats, notification failure detection)
- **M9:** Contacts & Notification Routing (per-service routing to named contacts with multiple channels)
