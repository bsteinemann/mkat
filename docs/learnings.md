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
