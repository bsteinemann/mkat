# Plan: AI Agent Workflow System for mkat

## Summary

Create a self-improving AI agent workflow system with 5 files that establish instructions, process, and a feedback loop.

---

## Files to Create

| File | Purpose |
|------|---------|
| `CLAUDE.md` | Primary agent instructions (auto-loaded by Claude Code) |
| `docs/workflow.md` | 6-phase implementation process with quality gates |
| `docs/learnings.md` | Feedback loop accumulator (patterns & anti-patterns) |
| `docs/adr/000-template.md` | Architecture Decision Record template |
| `docs/changelog.md` | Running log of changes |

---

## File Details

### 1. `CLAUDE.md` (root, ~150-200 lines)

Concise, scannable, auto-loaded every session. Contains:
- Project summary and tech stack
- Architecture overview (4-layer Clean Architecture)
- Project structure with directory tree
- Pointers to key documents (PRD, architecture, roadmap, plans, learnings)
- Coding conventions (.NET and React)
- Commands (build, run, test, migrate)
- Testing expectations
- "What NOT to do" section
- Environment variables table

### 2. `docs/workflow.md`

6-phase workflow for every implementation task:
1. **Understand** - Read plans, learnings, architecture; clarify ambiguities
2. **Plan** - List files, identify tests, check if ADR needed
3. **Implement** - Domain-outward, follow conventions, register DI
4. **Test** - Unit + integration tests, run full suite
5. **Document** - Changelog entry, ADR if needed, update stale docs
6. **Retrospective** - Reflect, add to learnings.md, flag CLAUDE.md updates

Each phase has Definition of Done checkboxes.

Also includes:
- "When to write an ADR" criteria
- Task templates (New Endpoint, New Worker, New Channel, New Page)

### 3. `docs/learnings.md`

Starts nearly empty, grows with each task. Entry format:
```
### [Date] - [Task Name]
**Context:** What was being implemented
**Went well:** What worked
**Tripped up:** What caused issues
**Pattern:** Reusable approach (optional)
**Anti-pattern:** What to avoid (optional)
**Action:** Changes needed to CLAUDE.md or workflow (optional)
```

Agents must read this file before starting any task.

### 4. `docs/adr/000-template.md`

Lightweight ADR template:
- Problem statement
- Options considered (with pros/cons)
- Decision and reasoning
- Consequences

### 5. `docs/changelog.md`

Simple format: date + milestone + description. Pre-1.0 so no semver categories.

---

## Feedback Loop Mechanism

```
Start task → Read CLAUDE.md (auto) → Read learnings.md
    ↓
Follow workflow.md phases 1-4 (Understand → Plan → Implement → Test)
    ↓
Phase 5: Document → changelog.md entry + ADR if needed
    ↓
Phase 6: Retrospective → learnings.md entry
    ↓
If significant learning → promote to CLAUDE.md
    ↓
Next task benefits from accumulated knowledge
```

---

## Implementation Order

1. `docs/adr/000-template.md` (standalone)
2. `docs/changelog.md` (standalone)
3. `docs/learnings.md` (standalone)
4. `docs/workflow.md` (references above)
5. `CLAUDE.md` (references everything)

---

## Verification

After creation:
- Start a new Claude Code session and verify CLAUDE.md is loaded
- Walk through the workflow mentally against a sample task (e.g., M1 Foundation)
- Confirm all file cross-references resolve correctly
