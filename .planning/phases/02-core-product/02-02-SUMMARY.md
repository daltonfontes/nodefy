---
phase: 02-core-product
plan: "02"
subsystem: api
tags: [ef-core, cards, fractional-index, activity-log, endpoints, rls, tdd]

dependency_graph:
  requires:
    - phase: 02-01
      provides: pipeline-crud-api, stage-crud-api, fractional-index-utility, phase2-schema-migration, ActivityLogEndpoints.LogActivity helper
  provides:
    - card-crud-api
    - card-move-api
    - card-archive-api
    - activity-log-read-api
  affects:
    - plan-02-03-board-ui

tech-stack:
  added: []
  patterns:
    - IgnoreQueryFilters bootstrap-then-member-check pattern for card endpoints (same as pipeline/stage admin pattern)
    - LogActivity called before SaveChangesAsync in every card mutation handler
    - Partial PATCH with per-field activity log entries (one LogActivity call per mutated field)
    - Cross-pipeline guard: load targetStage, verify stage.PipelineId == card.PipelineId before move

key-files:
  created:
    - api/Nodefy.Api/Endpoints/CardEndpoints.cs
    - api/Nodefy.Tests/Integration/CardTests.cs
    - api/Nodefy.Tests/Integration/ActivityLogTests.cs
  modified:
    - api/Nodefy.Api/Program.cs

key-decisions:
  - "Member-level auth for all card endpoints (not admin-only) — workspace members can create/edit/move/archive cards"
  - "IgnoreQueryFilters used to bootstrap tenant from card.TenantId before setting tenant context — same pattern as pipeline/stage endpoints. Immediately followed by member check."
  - "LogActivity called before SaveChangesAsync in every mutation handler — anti-pattern is calling it after. This ensures audit trail is atomic with the mutation."
  - "Partial PATCH emits one activity log entry per changed field (not one per request) — enables fine-grained audit trail"
  - "Cross-pipeline guard on PATCH /cards/{id}/move: targetStageId must belong to same pipelineId as card — returns 400 if mismatch"

patterns-established:
  - "Card mutation pattern: load card with IgnoreQueryFilters → SetTenant → member check → mutate → LogActivity → SaveChangesAsync → return DTO"
  - "GET /cards/{id}: bootstrap tenant via IgnoreQueryFilters, then re-fetch with global filters to enforce ArchivedAt == null"

requirements-completed: [CARD-01, CARD-02, CARD-03, CARD-04, CARD-05, CARD-06]

duration: 4min
completed: "2026-04-18"
---

# Phase 02 Plan 02: Card API Summary

**Card CRUD + move + archive endpoints with fractional-index positioning, atomic stage_entered_at reset, and per-mutation activity log writes covering CARD-01 through CARD-06.**

## Performance

- **Duration:** 4 min
- **Started:** 2026-04-18T15:13:43Z
- **Completed:** 2026-04-18T15:18:01Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments

- POST /pipelines/{pipelineId}/cards: member-level, fractional index position, monetary value validation, LogActivity("created")
- GET /cards/{id}: tenant-scoped retrieval respecting ArchivedAt global filter
- PATCH /cards/{id}: partial update with per-field activity log entries
- PATCH /cards/{id}/archive: soft-delete via ArchivedAt, excluded from board queries via EF Core global filter
- PATCH /cards/{id}/move: atomic stage_id + stage_entered_at + position update, cross-pipeline guard (400 if mismatch), LogActivity("moved") with from/to stage names
- GET /cards/{id}/activity (ActivityLogEndpoints): already implemented in Plan 02-01, used as-is
- 52/52 tests GREEN (8 new: 5 CardTests + 3 ActivityLogTests; 44 prior-plan regression tests all passing)

## Task Commits

1. **Task 1: CardTests and ActivityLogTests stubs (TDD RED)** - `163746e` (test)
2. **Task 2: Card CRUD + move + archive endpoints (TDD GREEN)** - `d35fba5` (feat)

## Files Created/Modified

- `api/Nodefy.Api/Endpoints/CardEndpoints.cs` - 7 endpoints: POST card, GET card, PATCH edit, PATCH archive, PATCH move; includes cross-pipeline guard and full activity log writes
- `api/Nodefy.Api/Program.cs` - Added `app.MapCardEndpoints()` registration
- `api/Nodefy.Tests/Integration/CardTests.cs` - 5 integration tests: create 201, edit 200, archive 200 (excluded from board), move 200 (stage_entered_at reset), negative monetary value 400
- `api/Nodefy.Tests/Integration/ActivityLogTests.cs` - 3 integration tests: create appends "created", move appends "moved" with stage names, edit appends "edited"

## Decisions Made

1. **Member-level auth for card endpoints** — All card mutations use member check (`db.WorkspaceMembers.AnyAsync(m => m.UserId == user.UserId)`) rather than admin check. This follows the plan spec and the core value: any workspace member can create/move cards.

2. **IgnoreQueryFilters bootstrap pattern** — Card endpoints use `IgnoreQueryFilters()` only to load the entity by ID and resolve `card.TenantId`. This is immediately followed by `tenant.SetTenant(card.TenantId)` and a member check. No data is returned from the `IgnoreQueryFilters` query itself.

3. **LogActivity before SaveChangesAsync** — All activity log entries are staged via `ActivityLogEndpoints.LogActivity(...)` before the single `SaveChangesAsync()` call per handler. This ensures the audit trail is atomic with the card mutation (either both commit or both fail).

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## Known Stubs

None. All endpoints return real data from the database.

## Threat Flags

No new security surface beyond what the plan's threat model covers. All threat mitigations implemented:
- T-02-02-01: EF Core global filter enforces `TenantId` + `ArchivedAt == null` — cross-tenant card returns 404
- T-02-02-02: Cross-pipeline guard on move — `targetStage.PipelineId != card.PipelineId` returns 400
- T-02-02-03: EF Core global filter on Stage prevents cross-tenant stage lookup in move endpoint
- T-02-02-04: `req.MonetaryValue < 0` returns 400 BadRequest
- T-02-02-05: LogActivity called on every mutation (create, edit, move, archive)
- T-02-02-06: Member check via `db.WorkspaceMembers.AnyAsync` on every write endpoint
- T-02-02-07: Activity log GET uses `IgnoreQueryFilters()` only to resolve tenant — already in ActivityLogEndpoints from Plan 02-01
- T-02-02-08: Single `SaveChangesAsync` per handler — LogActivity never calls SaveChangesAsync

## Next Phase Readiness

- Board UI (Plan 02-03) can now call all card endpoints: POST /pipelines/{id}/cards, PATCH /cards/{id}/move, PATCH /cards/{id}/archive, PATCH /cards/{id}
- GET /pipelines/{id}/board already returns card summaries (from Plan 02-01)
- All card endpoints return `CardDto` with all fields needed for the Kanban board

## Self-Check: PASSED
