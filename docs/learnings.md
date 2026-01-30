# Learnings & Patterns

This is a living document. After every implementation task, add an entry.
Read this file FIRST before starting any new work -- it prevents repeating mistakes.

---

## How to Use This File

- Scan before starting work (look for patterns related to your task)
- Add an entry after every implementation session (Phase 6 of workflow)
- Keep entries brief and actionable
- Tag entries with relevant keywords for searchability

---

## Entry Format

```
### [Date] - [Task Name]
**Context:** What was being implemented
**Went well:** What worked
**Tripped up:** What caused issues
**Pattern:** Reusable approach (optional)
**Anti-pattern:** What to avoid (optional)
**Action:** Changes needed to CLAUDE.md or workflow (optional)
```

---

## Entries

### 2026-01-22 - Self-Improvement System Effectiveness
**Context:** Analyzing how learnings.md, workflow.md, and TDD instructions performed during M1-M2
**Went well:** Commit discipline followed; plan files provided structure; changelog maintained
**Tripped up:**
- TDD was not actually followed (implementation written before/alongside tests, not after failing tests)
- Retrospective was batched at end of M2 instead of after each feature
- Learnings file has cold-start problem (empty during M1, so no value extracted)
- Sub-agents don't read CLAUDE.md/learnings.md unless explicitly instructed in their prompt
- "NEVER write implementation without a failing test" instruction was ignored without consequence
**Pattern:** Enforcement requires gates, not just instructions. A gate = "show the failing test output before proceeding to implementation"
**Anti-pattern:** Writing instructions that say "always do X" without a verification mechanism. Agents optimize for task completion, not process compliance.
**Action:** Added pre-task checklist to CLAUDE.md, added retrospective timing to workflow.md, added sub-agent context requirements, strengthened TDD gate language

### 2026-01-22 - M1 Foundation & M2 Core API
**Context:** Implementing domain entities, EF Core, repositories, auth, CRUD controller
**Went well:** Clean Architecture separation works well; FluentValidation integration straightforward
**Tripped up:**
- `Monitor` name conflicts with `System.Threading.Monitor` when ImplicitUsings enabled → need `using Monitor = Mkat.Domain.Entities.Monitor;`
- InMemoryDatabase doesn't support `GetPendingMigrations()` → wrap in try/catch with `EnsureCreated()` fallback
- xUnit runs test classes in parallel by default → tests sharing environment variables need `[Collection("...")]`
- `WebApplicationFactory` with `IClassFixture` shares state across tests → per-test factory with unique InMemoryDatabase names for isolation
- .NET 10 RC SDK building net8.0 projects requires `rollForward: latestMajor` in global.json
**Pattern:** Integration tests should create their own `WebApplicationFactory` per test class instance with unique DB names
**Anti-pattern:** Don't use `IClassFixture<WebApplicationFactory>` when tests modify shared state (DB); don't set env vars without `[Collection]` isolation

### 2026-01-22 - M3 Monitoring Engine
**Context:** Implementing state machine, webhook/heartbeat endpoints, background workers
**Went well:**
- TDD gate properly followed this time: wrote tests first, confirmed RED, then implemented GREEN
- StateService unit tests with Moq gave fast, reliable coverage of all state transition logic
- Background workers testable by making `CheckMissedHeartbeatsAsync`/`CheckMaintenanceWindowsAsync` public methods
- Integration tests for webhook/heartbeat endpoints verify full request flow including state changes
**Tripped up:**
- `Monitor` ambiguity hit again in worker test file → always add `using Monitor = Mkat.Domain.Entities.Monitor;` when `Mkat.Domain.Entities` is imported in a file with ImplicitUsings
- Worker tests reference Infrastructure types, so they belong in Api.Tests (not Application.Tests) since Api.Tests has transitive access through the Api project reference
- `ServiceRepository.GetPausedServicesAsync` needed `using Mkat.Domain.Enums;` for `ServiceState` reference
- `IStateService` interface defined in same file as `StateService` implementation (Application/Services/) — works fine but consider splitting if the file grows
**Pattern:** Background workers using `IServiceProvider.CreateScope()` are testable by building a real `ServiceCollection` with mock registrations in tests
**Anti-pattern:** Don't put Infrastructure worker tests in Application.Tests — the project doesn't reference Infrastructure. Use Api.Tests which has full access through transitive references.

### 2026-01-22 - M4 Notifications
**Context:** Implementing notification channels, Telegram integration, alert dispatch, and alerts API
**Went well:**
- TDD gate properly followed: ParseDuration tests written first (RED), then implemented (GREEN)
- NotificationDispatcher unit tests with Moq gave comprehensive coverage of dispatch logic and edge cases
- Integration tests for AlertsController and Mute endpoint use same WebApplicationFactory pattern from M3
- TelegramChannel constructor wrapped in try/catch for token validation (Telegram.Bot validates format eagerly)
- Reusing `TelegramChannel.EscapeMarkdown` as public static method from TelegramBotService avoids duplication
**Tripped up:**
- Telegram.Bot v22.8.1 uses `SendMessage` not `SendTextMessageAsync`, `GetMe` not `GetMeAsync`, `AnswerCallbackQuery` not `AnswerCallbackQueryAsync` — API method names changed from older versions
- `TelegramBotClient("fake-token")` throws `ArgumentException` at construction — must wrap in try/catch in constructor, not just check IsEnabled
- Moq package needed in both test projects (Application.Tests and Api.Tests) — each has its own csproj
- MarkdownV2 requires escaping special chars even in non-formatted text — dots, hyphens, parentheses all need `\` prefix
**Pattern:** For BackgroundServices that depend on external APIs (Telegram), test only the pure logic (ParseDuration) and configuration checks (IsEnabled). Don't try to integration-test the polling loop.
**Anti-pattern:** Don't assume NuGet package method names match documentation from older versions. Check actual API signatures by looking at compile errors.

### 2026-01-22 - M5 Frontend
**Context:** Building React frontend with Vite, Tailwind v4, TanStack Router/Query
**Went well:**
- Tailwind v4 setup is simpler than v3: just `@import "tailwindcss"` in CSS + `@tailwindcss/vite` plugin (no tailwind.config.js, no postcss.config.js)
- TanStack Router v1.154.x route definitions work exactly as documented; auth guard via `beforeLoad` with `throw redirect()`
- Vite proxy configuration cleanly forwards API calls during development
- `MapFallbackToFile("index.html")` after `MapControllers()` gives API routes priority over SPA routing
- TypeScript `--noEmit` check catches type errors before Vite build
**Tripped up:**
- Rollup warns about importing interfaces as values (`"Alert" is not exported`) — fix with `import type { Alert }` for type-only imports
- Vite 7 requires explicit `@tailwindcss/vite` plugin instead of PostCSS-based setup from Tailwind v3
- `UseStaticFiles()` and `UseDefaultFiles()` must come before auth middleware or static files get blocked
- Default Vite template includes App.css, assets/react.svg, public/vite.svg — remove these early to avoid confusion
**Pattern:** For SPA + API in same host: UseDefaultFiles → UseStaticFiles → auth middleware → MapControllers → MapFallbackToFile
**Anti-pattern:** Don't commit wwwroot build artifacts to git in production (add to .gitignore). For this project it's fine since Docker will build in-container.

### 2026-01-23 - Configurable Base Path (Runtime)
**Context:** Adding runtime-configurable URL prefix so mkat can run under a subpath (e.g., `/mkat/`) behind a reverse proxy
**Went well:**
- ASP.NET Core's `UsePathBase()` handles the server-side routing cleanly — strips prefix on incoming, doesn't reject non-prefixed requests
- Vite `base: './'` makes asset paths relative without needing a build-time base path
- TanStack Router's `basepath` option works seamlessly — route definitions don't change
- Injecting `window.__MKAT_BASE_PATH__` via a custom `MapFallback` handler is simple and effective
- `UsePathBase` is transparent to container healthchecks (`/health` still works directly)
**Tripped up:**
- `MapFallbackToFile` can't inject runtime config — had to replace with custom `MapFallback` that reads + modifies HTML
- `window.location.href` for 401 redirects is outside router control — needs manual base path prefixing
- Login page makes a direct `fetch()` call (not through the API client) — easy to miss when updating API base URLs
**Pattern:** For runtime-configurable base path without rebuild: UsePathBase (server) + relative Vite assets + injected window variable (client) + router basepath option
**Anti-pattern:** Don't use `MapFallbackToFile` if you need to inject runtime config into the HTML. Use a custom `MapFallback` handler instead.

### 2026-01-30 - Code Quality Enforcement
**Context:** Adding EditorConfig, Directory.Build.props (TreatWarningsAsErrors), Prettier, and CI enforcement
**Went well:**
- `Directory.Build.props` at repo root applies to all projects; `tests/Directory.Build.props` can import parent and add test-specific suppressions
- `eslint-config-prettier` cleanly disables conflicting ESLint formatting rules — just add it last in the config array
- Prettier `--check` mode is perfect for CI (no file changes, just exit code)
**Tripped up:**
- `AnalysisLevel=latest-recommended` surfaces many more warnings than expected — CA1305 (IFormatProvider), CA1310 (StringComparison), CA1816 (GC.SuppressFinalize), CA1822 (static), CA1859 (return type) all fired across the codebase
- Test projects generate many noisy analyzer warnings (CA1816 Dispose, CA1822 static, CA1859 return types) — suppress these in a dedicated `tests/Directory.Build.props`
- shadcn/ui components trigger `react-refresh/only-export-components` and `react-hooks/purity` — suppress via ESLint config override for `src/components/ui/`
- EF Core auto-generated migrations trigger CA1861 (constant arrays) — suppress via migration-specific `.editorconfig`
**Pattern:** Use layered `Directory.Build.props` (root for global, tests/ for test-specific) and layered `.editorconfig` (root for global, subdirectory for overrides) to manage analyzer strictness
**Anti-pattern:** Don't suppress too many rules globally in the root props — keep suppressions scoped to where they're needed (tests, migrations, UI components)
**Action:** Added Monitor ambiguity gotcha, analyzer culture rules, test placement rule, middleware ordering, and lint/format commands to CLAUDE.md. Updated workflow.md Phase 4 to reflect TreatWarningsAsErrors and frontend lint/format checks.

### 2026-01-30 - Historical Data & Analytics (MonitorEvent/MonitorRollup)
**Context:** Adding unified event logging, rollup aggregation, and Recharts-based charts for all monitor types. Replacing MetricReading with MonitorEvent.
**Went well:**
- TDD gate followed properly: wrote tests first (RED), then implementation (GREEN) for all 11 phases
- Clean Architecture separation made the MetricReading→MonitorEvent migration straightforward — only needed to update the interface dependency in MetricEvaluator and the DI registration
- Recharts integration was smooth; LineChart, BarChart, ScatterChart all worked without issues
- `GetByMonitorIdInWindowAsync` on IMonitorEventRepository needed both start and end DateTime (unlike old IMetricReadingRepository which only took start) — better API design
- Rollup computation (RollupCalculator) with proper population standard deviation and linear interpolation percentiles worked correctly first try
**Tripped up:**
- `IMonitorEventRepository` initially lacked `GetLastNByMonitorIdAsync` needed by MetricEvaluator's ConsecutiveCount and SampleCountAverage strategies — had to add it during Phase 10 cleanup
- MonitorEvent's `Value` is `double?` (nullable) unlike MetricReading's `double` — average strategies need `.Where(e => e.Value.HasValue)` filter before calling `.Average()`
- Removing `using Mkat.Domain.Enums;` when refactoring MetricEvaluator caused `ThresholdStrategy` to become unresolved — the enum was used implicitly in the switch expression
- `dotnet test` from wrong CWD (mkat-ui/) fails with "no project or solution" — always use absolute path or ensure CWD is repo root
**Pattern:** When replacing an entity, add all needed query methods to the new repository interface BEFORE refactoring consumers. Missing methods discovered during consumer refactoring cause backtracking.
**Anti-pattern:** Don't assume new repository interfaces cover all old patterns. Compare old interface methods against usage sites to identify gaps early.
