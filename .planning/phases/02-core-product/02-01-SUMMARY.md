---
phase: 02-core-product
plan: "01"
subsystem: backend-pipeline
tags: [ef-core, migrations, pipeline, stage, fractional-index, endpoints, rls]
dependency_graph:
  requires: []
  provides:
    - pipeline-crud-api
    - stage-crud-api
    - board-load-api
    - fractional-index-utility
    - phase2-schema-migration
  affects:
    - plan-02-02-card-api
    - plan-02-03-board-ui
tech_stack:
  added:
    - Microsoft.EntityFrameworkCore.Design 9.0.4
    - Microsoft.EntityFrameworkCore.Relational 9.0.4
  patterns:
    - EF Core global query filter per entity (HasQueryFilter)
    - Fractional indexing for ordered collections (double precision)
    - IDesignTimeDbContextFactory for migration tooling
    - ActivityLog internal helper (no SaveChangesAsync — caller responsibility)
    - IgnoreQueryFilters bootstrap-then-IsAdmin pattern for route-only tenant resolution
key_files:
  created:
    - api/Nodefy.Api/Data/Entities/Pipeline.cs
    - api/Nodefy.Api/Data/Entities/Stage.cs
    - api/Nodefy.Api/Data/Entities/ActivityLog.cs
    - api/Nodefy.Api/Data/AppDbContextFactory.cs
    - api/Nodefy.Api/Lib/FractionalIndex.cs
    - api/Nodefy.Api/Endpoints/PipelineEndpoints.cs
    - api/Nodefy.Api/Endpoints/StageEndpoints.cs
    - api/Nodefy.Api/Endpoints/ActivityLogEndpoints.cs
    - api/Nodefy.Api/Migrations/20260418150403_Phase2Schema.cs
    - api/Nodefy.Tests/Integration/PipelineTests.cs
    - api/Nodefy.Tests/Integration/StageTests.cs
    - api/Nodefy.Tests/Integration/BoardTests.cs
    - api/Nodefy.Tests/Unit/FractionalIndexTests.cs
  modified:
    - api/Nodefy.Api/Data/Entities/Card.cs
    - api/Nodefy.Api/Data/AppDbContext.cs
    - api/Nodefy.Api/Program.cs
    - db/init.sql
    - api/Nodefy.Api/Nodefy.Api.csproj
    - api/Nodefy.Tests/Nodefy.Tests.csproj
decisions:
  - "IgnoreQueryFilters used only for bootstrap tenant lookup on PATCH/DELETE /pipelines/{id} and /stages/{id} — immediately followed by IsAdmin check. No cross-tenant data exposure."
  - "AppDbContextFactory added for EF Core design-time tooling (NullTenantService) — not used at runtime."
  - "init.sql updated with full Phase 2 schema (pipelines, stages, full cards, activity_logs) including RLS policies — test containers use init.sql, not EF migrations."
  - "Microsoft.EntityFrameworkCore.Relational 9.0.4 added explicitly to Nodefy.Tests to resolve transitive version conflict with Design package."
  - "Cards table in init.sql uses full Phase 2 schema (pipeline_id/stage_id NOT NULL) — no stub migration needed."
metrics:
  duration_seconds: 705
  completed_date: "2026-04-18"
  tasks_completed: 3
  tasks_total: 3
  files_created: 13
  files_modified: 6
---

# Phase 02 Plan 01: Pipeline & Stage Backend Summary

**One-liner:** Pipeline CRUD + Stage CRUD with fractional-index reordering + board load endpoint + EF Core migration adding pipelines/stages/activity_logs tables and extending cards schema.

## Tasks Completed

| Task | Name | Commit | Key Files |
|------|------|--------|-----------|
| 1 | Create test stubs (TDD RED) | e671d50 | PipelineTests.cs, StageTests.cs, BoardTests.cs, FractionalIndexTests.cs |
| 2 | EF Core entities, FractionalIndex, AppDbContext, migration | 25bdfb0 | Pipeline.cs, Stage.cs, ActivityLog.cs, FractionalIndex.cs, AppDbContext.cs, Phase2Schema migration |
| 3 | Pipeline/Stage/ActivityLog endpoints + registration | e3ee1b2 | PipelineEndpoints.cs, StageEndpoints.cs, ActivityLogEndpoints.cs, Program.cs, init.sql |

## Test Results

- **FractionalIndexTests:** 9/9 unit tests GREEN (no Docker)
- **PipelineTests:** 4/4 integration tests GREEN
- **StageTests:** 4/4 integration tests GREEN
- **BoardTests:** 1/1 integration test GREEN
- **Full suite:** 44/44 tests GREEN (includes all Phase 1 regression tests)

## Decisions Made

1. **IgnoreQueryFilters for tenant bootstrap** — PATCH/DELETE `/pipelines/{id}` and `/stages/{id}` use `IgnoreQueryFilters()` only to load the entity and read its `TenantId`, then immediately call `tenant.SetTenant(...)` and `IsAdmin(db, tenantId, userId)`. No data is returned without the admin guard. This is the same pattern used in `WorkspaceEndpoints` and `InviteEndpoints`.

2. **AppDbContextFactory** — Added `IDesignTimeDbContextFactory<AppDbContext>` with a `NullTenantService` to allow `dotnet ef migrations add` to work without the full app startup (which requires `AUTH_JWT_SECRET`).

3. **Full cards schema in init.sql** — The test containers mount `db/init.sql` directly. The Phase 1 stub (cards with no pipeline_id/stage_id) was replaced with the full Phase 2 cards schema so integration tests work without a migration runner.

4. **EF Core Relational 9.0.4 explicit pin** — Adding `Microsoft.EntityFrameworkCore.Design 9.0.4` to `Nodefy.Api` caused a transitive version conflict: `Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4` in `Nodefy.Tests` pulled in Relational 9.0.1. Fixed by explicitly pinning `Microsoft.EntityFrameworkCore.Relational 9.0.4` in `Nodefy.Tests.csproj`.

5. **ReorderStage response shape** — The plan's test stub used `StageDto` for the reorder response, but the endpoint returns `ReorderStageResponse(StageDto Stage, List<ReorderedPositionDto> RebalancedPositions)`. Updated `StageTests` to deserialize `ReorderStageResponse` and assert on `body.Stage.Position`.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added Microsoft.EntityFrameworkCore.Design 9.0.4**
- **Found during:** Task 2 (migration step)
- **Issue:** `dotnet ef migrations add` failed — `Nodefy.Api` did not reference `Microsoft.EntityFrameworkCore.Design`
- **Fix:** Added the package at version 9.0.4 matching existing EF Core version
- **Files modified:** api/Nodefy.Api/Nodefy.Api.csproj
- **Commit:** 25bdfb0

**2. [Rule 3 - Blocking] Added AppDbContextFactory for design-time context**
- **Found during:** Task 2 (migration step)
- **Issue:** `dotnet ef` could not create DbContext because Program.cs requires `AUTH_JWT_SECRET` env var
- **Fix:** Created `AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>` with `NullTenantService`
- **Files modified:** api/Nodefy.Api/Data/AppDbContextFactory.cs (new)
- **Commit:** 25bdfb0

**3. [Rule 3 - Blocking] Pinned Microsoft.EntityFrameworkCore.Relational 9.0.4 in Nodefy.Tests**
- **Found during:** Task 2 (post-migration build)
- **Issue:** CS1705 assembly version conflict — Design package bumped Relational to 9.0.4 but Npgsql pulled in 9.0.1
- **Fix:** Explicitly added `Microsoft.EntityFrameworkCore.Relational 9.0.4` to `Nodefy.Tests.csproj`
- **Files modified:** api/Nodefy.Tests/Nodefy.Tests.csproj
- **Commit:** 25bdfb0

**4. [Rule 1 - Bug] Updated init.sql with full Phase 2 schema**
- **Found during:** Task 3 (integration test run)
- **Issue:** Test containers mount `db/init.sql`; old stub cards table had no `pipeline_id`/`stage_id`, so `pipelines` relation didn't exist and tests failed with `42P01: relation "pipelines" does not exist`
- **Fix:** Replaced cards stub in init.sql with full Phase 2 schema (pipelines, stages, cards with all FKs, activity_logs), added RLS policies for all new tables
- **Files modified:** db/init.sql
- **Commit:** e3ee1b2

**5. [Rule 1 - Bug] Fixed StageTests reorder response deserialization**
- **Found during:** Task 3 verification
- **Issue:** Plan test stub read `StageDto` directly from reorder response, but endpoint returns `ReorderStageResponse` wrapper
- **Fix:** Added `ReorderStageResponse` record to test and updated assertion to `body.Stage.Position`
- **Files modified:** api/Nodefy.Tests/Integration/StageTests.cs
- **Commit:** e3ee1b2

## Known Stubs

None. All endpoints return real data from the database.

## Threat Flags

No new security surface beyond what the plan's threat model covers. All write endpoints enforce `IsAdmin` before mutation. EF Core global filters (`HasQueryFilter`) applied to Pipeline, Stage, ActivityLog, and Cards (upgraded to include `ArchivedAt == null`). RLS policies added in `init.sql` for all new tables.

## Self-Check: PASSED
