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
