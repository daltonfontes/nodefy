---
phase: 02-core-product
fixed_at: 2026-04-20T00:00:00Z
review_path: .planning/phases/02-core-product/02-REVIEW.md
iteration: 1
findings_in_scope: 8
fixed: 8
skipped: 0
status: all_fixed
---

# Phase 02: Code Review Fix Report

**Fixed at:** 2026-04-20
**Source review:** .planning/phases/02-core-product/02-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 8
- Fixed: 8
- Skipped: 0

## Fixed Issues

### CR-01: Cross-Tenant Read Race in `/pipelines/{id}/board` (TOCTOU)

**Files modified:** `api/Nodefy.Api/Endpoints/PipelineEndpoints.cs`
**Commit:** 0f9b5fe
**Applied fix:** Changed the board endpoint bootstrap read from `db.Pipelines.FirstOrDefaultAsync(...)` to `db.Pipelines.IgnoreQueryFilters().FirstOrDefaultAsync(...)` so the pipeline is fetched before the global query filter is armed, matching the pattern used by all other endpoints (PATCH, DELETE). `SetTenant` is now called on the unfiltered result before any subsequent tenant-scoped queries run.

---

### CR-02: Raw JSON Payload Rendered as Readable String in Activity Panel (Potential XSS / Data Integrity)

**Files modified:** `frontend/src/components/board/CardDetailPanel.tsx`
**Commit:** 4f5f143
**Applied fix:** Replaced the raw `${log.payload}` string interpolation in `activityLabel` with a `JSON.parse` call wrapped in try/catch. Each action case now extracts structured fields (`from_stage`, `to_stage`, `field`) from the parsed object and renders a human-readable message. The `default` branch now returns `log.action` (the action type string) instead of the raw payload.

---

### WR-01: Card Creation Does Not Invalidate Board Cache

**Files modified:** `frontend/src/components/board/BoardShell.tsx`
**Commit:** 51a27e8
**Applied fix:** Added `const qc = useQueryClient()` inside `BoardShell` and called `qc.invalidateQueries({ queryKey: ["board", pipelineId] })` at the end of `handleCreateCard` after the successful fetch. Also added the `useQueryClient` import from `@tanstack/react-query`.

---

### WR-02: Drag-and-Drop Silently Drops Same-Stage Reorders

**Files modified:** `frontend/src/components/board/BoardShell.tsx`
**Commit:** 9d80a25
**Applied fix:** Removed the early-return guard `if (sourceStageId === targetStageId) return`. The drag-end handler now branches: for same-stage reorders it computes `prevPosition`/`nextPosition` from the neighbors of the card being dropped over (using `overData?.type === "card"` to detect card-over-card drops), then calls `moveMutation` with `targetStageId = sourceStageId`. Cross-stage moves retain the original behavior of placing at the end of the target column. Note: requires human verification of the position-computation logic for edge cases.
**Status:** fixed: requires human verification

---

### WR-03: `FractionalIndex.Before(0)` Produces a Negative Position

**Files modified:** `api/Nodefy.Api/Lib/FractionalIndex.cs`
**Commit:** 0b36777
**Applied fix:** Changed `Before(double first)` from `first / 2.0` to `first > 1e-9 ? first / 2.0 : first - 1_000_000.0`. Also updated `NeedsRebalance` to detect negative positions by adding `p < 0` to the predicate (previously only `p < 1e-9` was checked, which missed the case introduced by the new `Before` fallback producing large negative values).

---

### WR-04: `workspaceId` Can Be Empty String in Board Proxy — Silent Misbehavior

**Files modified:** `frontend/src/app/api/pipelines/[id]/board/route.ts`
**Commit:** 1da0553
**Applied fix:** Changed `url.searchParams.get("workspaceId") ?? ""` to a strict null check. When `workspaceId` is absent or empty, the route now returns `NextResponse.json({ error: "workspaceId is required" }, { status: 400 })` before proxying to the backend.

---

### WR-05: Hardcoded Currency in Card Detail Panel

**Files modified:** `frontend/src/components/board/CardDetailPanel.tsx`, `frontend/src/components/board/BoardShell.tsx`
**Commit:** e7df04a
**Applied fix:** Added `workspaceCurrency: "BRL" | "USD" | "EUR"` to `CardDetailPanelProps`, destructured it in the component function signature, and replaced the hardcoded `currency: "BRL"` with `currency: workspaceCurrency` in the `Intl.NumberFormat` call. Updated the `CardDetailPanel` usage in `BoardShell` to pass `workspaceCurrency={workspace.currency}`.

---

### WR-06: Title Validation Does Not Reject Whitespace-Only Strings

**Files modified:** `api/Nodefy.Api/Endpoints/CardEndpoints.cs`, `api/Nodefy.Api/Endpoints/PipelineEndpoints.cs`, `api/Nodefy.Api/Endpoints/StageEndpoints.cs`
**Commit:** 43bb356
**Applied fix:** In all three endpoints (card create, pipeline create/rename, stage create/rename), replaced the `string.IsNullOrWhiteSpace(req.X) || req.X.Length < 2` pattern with `var trimmedX = req.X?.Trim() ?? ""; if (trimmedX.Length < 2)`. The trimmed value is now persisted to the database (`Name = trimmedName`, `Title = trimmedTitle`) so leading/trailing whitespace is never stored.

---

## Skipped Issues

None — all findings were fixed.

---

_Fixed: 2026-04-20_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
