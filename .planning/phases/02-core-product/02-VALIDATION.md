---
phase: 2
slug: core-product
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-18
---

# Phase 2 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework (backend)** | xUnit 2.x + TestContainers (already installed in Phase 1) |
| **Framework (frontend)** | None yet — Playwright added in Phase 4; no unit test framework for frontend in v1 |
| **Config file** | `api/Nodefy.Tests/Nodefy.Tests.csproj` |
| **Quick run command** | `dotnet test api/Nodefy.Tests --no-build -q` |
| **Full suite command** | `dotnet test api/Nodefy.Tests -v normal` |
| **Estimated runtime** | ~30–60 seconds (TestContainers spins real PostgreSQL) |

---

## Sampling Rate

- **After every backend task commit:** Run `dotnet test api/Nodefy.Tests --no-build -q`
- **After every plan wave:** Run `dotnet test api/Nodefy.Tests -v normal`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 60 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Secure Behavior | Test Type | Automated Command | Status |
|---------|------|------|-------------|-----------------|-----------|-------------------|--------|
| 02-01-01 | 01 | 1 | PIPE-01 | Pipeline scoped to tenant | unit/integration | `dotnet test --filter Pipeline` | ⬜ pending |
| 02-01-02 | 01 | 1 | PIPE-02 | Rename/delete scoped to tenant | unit/integration | `dotnet test --filter Pipeline` | ⬜ pending |
| 02-01-03 | 01 | 1 | PIPE-03 | Stage CRUD scoped to tenant | unit/integration | `dotnet test --filter Stage` | ⬜ pending |
| 02-01-04 | 01 | 1 | PIPE-04 | Stage reorder with fractional index | unit | `dotnet test --filter StageReorder` | ⬜ pending |
| 02-01-05 | 01 | 1 | PIPE-05 | Aggregate query returns correct count+sum | unit | `dotnet test --filter StageAggregate` | ⬜ pending |
| 02-02-01 | 02 | 1 | CARD-01 | Card creation scoped to tenant | unit/integration | `dotnet test --filter Card` | ⬜ pending |
| 02-02-02 | 02 | 1 | CARD-02 | Card edit scoped to tenant+ownership | unit | `dotnet test --filter CardEdit` | ⬜ pending |
| 02-02-03 | 02 | 1 | CARD-03 | Soft delete sets archived_at; excluded from board | unit | `dotnet test --filter CardArchive` | ⬜ pending |
| 02-02-04 | 02 | 1 | CARD-04 | Card move updates position+stage_entered_at | unit | `dotnet test --filter CardMove` | ⬜ pending |
| 02-02-05 | 02 | 1 | CARD-05 | Stage age derived from stage_entered_at | unit | `dotnet test --filter StageAge` | ⬜ pending |
| 02-02-06 | 02 | 1 | CARD-06 | Activity log appended on create/move/edit | unit | `dotnet test --filter ActivityLog` | ⬜ pending |
| 02-03-01 | 03 | 2 | CARD-04 | DnD optimistic update + rollback on error | manual | Board E2E drag test | ⬜ pending |
| 02-03-02 | 03 | 2 | CARD-05 | Stage-age badge shows correct duration | manual | Visual inspection | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `api/Nodefy.Tests/Integration/PipelineTests.cs` — covers PIPE-01, PIPE-02
- [ ] `api/Nodefy.Tests/Integration/StageTests.cs` — covers PIPE-03, PIPE-04
- [ ] `api/Nodefy.Tests/Integration/BoardTests.cs` — covers PIPE-05
- [ ] `api/Nodefy.Tests/Integration/CardTests.cs` — covers CARD-01, CARD-02, CARD-03, CARD-04, CARD-05
- [ ] `api/Nodefy.Tests/Integration/ActivityLogTests.cs` — covers CARD-06
- [ ] `api/Nodefy.Tests/Unit/FractionalIndexTests.cs` — covers rebalance threshold logic
- [ ] xUnit + TestContainers already installed from Phase 1 — no new installs needed

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| DnD optimistic update + visual rollback | CARD-04 | No browser automation until Phase 4 (Playwright) | Drag card, disconnect network, verify card snaps back + error toast appears |
| Stage-age badge displays correctly | CARD-05 | Requires real time elapsed | Set `stage_entered_at` to past date in DB, verify badge shows correct duration |
| Sidebar collapse/expand | D-03 | CSS/UI interaction | Click toggle, verify board expands; click again, verify sidebar returns |
| Inline pipeline rename | D-02 | UI interaction | Hover pipeline in sidebar, click …, select Rename, verify inline input appears |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
