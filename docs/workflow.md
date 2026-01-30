# Implementation Workflow

This workflow applies to every feature or fix implemented in mkat.
Each phase has a Definition of Done (DoD) that must be satisfied before proceeding.

---

## Phase 1: Understand

**Goal:** Ensure you know what to build and why.

### Steps

1. Read the relevant milestone plan (`docs/plans/m{N}-*.md`)
2. Read `docs/learnings.md` for known patterns and gotchas
3. Cross-reference with `docs/architecture.md` for layer boundaries
4. Identify related existing code (if any) to understand current patterns
5. Clarify ambiguities by referencing the PRD

### DoD

- [ ] Can articulate what the feature does in one sentence
- [ ] Know which layer(s) will be modified
- [ ] Identified any dependencies on prior work

---

## Phase 2: Plan

**Goal:** Define the approach before writing code.

### Steps

1. List the files to create or modify
2. Identify interfaces and contracts first
3. Determine test cases upfront
4. Check if an ADR is needed (see "When to write an ADR" below)
5. If the task is large (5+ files), sketch a brief implementation order

### DoD

- [ ] File list documented (even if just mental model for small tasks)
- [ ] Test cases identified
- [ ] No architectural ambiguity remaining

---

## Phase 3: Implement (TDD)

**Goal:** Build features test-first using Red-Green-Refactor.

### Steps

1. Start from the Domain layer outward (Domain -> Application -> Infrastructure -> API)
2. For each behavior/unit of work:
   a. **RED:** Write a failing test that describes the expected behavior
   b. **GREEN:** Write the minimum implementation to make the test pass
   c. **REFACTOR:** Clean up code while keeping tests green
   d. **COMMIT:** Commit when a logical unit is complete and tests pass
3. Create interfaces before implementations (test against interfaces)
4. Follow naming conventions from CLAUDE.md
5. Register new services in DI (Program.cs or extension methods)
6. Add EF migrations if schema changes

### TDD Rules

- NEVER write implementation without a failing test first
- One test at a time - don't write multiple failing tests
- Run `dotnet test` after each green step
- Commit after each completed feature/behavior (tests green)
- If fixing a bug: write reproducing test first, then fix

### TDD Verification Gate

For each unit of work, the sequence MUST be:
1. Write test → run → observe failure output (RED confirmed)
2. Write implementation → run → observe pass (GREEN confirmed)
3. Refactor → run → still passing

If you cannot show a failing test run before implementation, you are not doing TDD.
This gate exists because previous milestones claimed TDD but wrote implementation first.

### Commit Cadence

- Commit after each entity/enum with its tests
- Commit after each interface + implementation with tests
- Commit after each endpoint with integration tests
- Commit after infrastructure additions (Docker, config, migrations)
- Use conventional commits: `feat:`, `fix:`, `refactor:`, `test:`, `chore:`

### Rules

- One concern per file
- No skipping layers (controllers call Application, never Infrastructure directly)
- Validators for every new command/query DTO
- Structured logging at key decision points
- CancellationToken on all async signatures

### DoD

- [ ] All code written test-first (no implementation without a prior failing test)
- [ ] Code compiles without warnings
- [ ] All tests pass
- [ ] DI registration complete
- [ ] Migrations created if needed
- [ ] Atomic commits for each completed unit of work
- [ ] No TODO comments left (except deliberate Phase 2+ markers)

---

## Phase 4: Verify

**Goal:** Confirm full suite passes and no regressions.

### Steps

1. Run the full test suite (`dotnet test`) - all tests must pass
2. Run `dotnet build` - warnings are errors (`TreatWarningsAsErrors` is enabled), so a clean build means no warnings
3. For frontend changes: run `npm run lint` and `npm run format:check` in `src/mkat-ui/`
4. Manual smoke test if it is a user-facing feature
5. Verify no regressions from other milestones

### DoD

- [ ] Full test suite passes
- [ ] Build succeeds (warnings = errors, so success means zero warnings)
- [ ] Frontend lint and format checks pass (if frontend was modified)
- [ ] Edge cases covered (nulls, empty inputs, invalid states)

---

## Phase 5: Document

**Goal:** Keep documentation in sync with reality.

### Steps

1. Update `docs/changelog.md` with a brief entry
2. If a significant decision was made, create an ADR in `docs/adr/`
3. If the implementation diverges from the plan, update the plan file
4. Add inline comments for non-obvious logic only

### DoD

- [ ] Changelog entry added
- [ ] ADR created if needed
- [ ] No stale documentation left behind

---

## Phase 6: Retrospective (Feedback Loop)

**Goal:** Improve the system for next time. This step is NOT optional.

### When to Write Learnings

- **Immediately** when you solve a non-obvious problem (don't wait until end of milestone)
- **After each feature** if you encountered friction or discovered a pattern
- **After each milestone** as a summary review

Do NOT batch learnings. If you just debugged a tricky issue, add it to learnings.md NOW before continuing.

### Steps

1. **Reflect:** What went smoothly? What was confusing or slow?
2. **Identify:** Any patterns discovered? Any anti-patterns to avoid?
3. **Update:** Add findings to `docs/learnings.md`
4. **Adjust:** If CLAUDE.md conventions need updating, make the change

### Entry Format for learnings.md

```
### [Date] - [Feature/Task Name]
**Context:** What was being implemented
**Went well:** What patterns or approaches worked
**Tripped up:** What was confusing or caused rework
**Pattern:** (optional) Reusable pattern discovered
**Anti-pattern:** (optional) Approach to avoid in future
**Action:** Changes made to CLAUDE.md, workflow, or conventions
```

### DoD

- [ ] Entry added to `docs/learnings.md`
- [ ] CLAUDE.md updated if needed

---

## When to Write an ADR

Create an Architecture Decision Record (`docs/adr/NNN-title.md`) when:

- Choosing between two or more reasonable approaches
- Deviating from the documented architecture
- Introducing a new library or framework
- Making a decision that would be hard to reverse
- Future-you would ask "why did we do it this way?"

Use the template at `docs/adr/000-template.md`. Number sequentially (001, 002, ...).

---

## Task Templates

### New API Endpoint

1. DTO in `Mkat.Application/DTOs/`
2. Validator in `Mkat.Application/Validators/`
3. Interface method in repository (if new data operation)
4. Repository implementation in Infrastructure
5. Controller action in API
6. Tests: unit for validator, integration for endpoint

### New Background Worker

1. Interface in Application (if needed)
2. Worker class in `Mkat.Infrastructure/Workers/`
3. Register as `IHostedService` in DI
4. Unit test for worker logic
5. Integration test for timing/scheduling

### New Notification Channel

1. Implement `INotificationChannel` in `Mkat.Infrastructure/Channels/`
2. Add channel-specific options class
3. Register in DI
4. Add to `NotificationDispatcher`
5. Integration test with mock/stub

### New Frontend Page

1. Route definition (TanStack Router)
2. API types matching backend DTOs
3. Query hooks (TanStack Query)
4. Page component
5. Sub-components as needed

---

## Workflow Diagram

```
Phase 1         Phase 2         Phase 3              Phase 4         Phase 5         Phase 6
Understand  --> Plan        --> Implement (TDD)  --> Verify      --> Document    --> Retrospective
                                                                                        |
Read plans      List files      RED: failing test    Full suite      Changelog       Update learnings.md
Read learnings  Identify tests  GREEN: min code      No warnings     ADR (maybe)     Update CLAUDE.md
Check arch      Check ADR need  REFACTOR: clean up   Smoke test      Fix stale docs  (if needed)
                                COMMIT: atomic                                           |
                                Repeat per behavior                                      v
                                                                                Next task benefits
```
