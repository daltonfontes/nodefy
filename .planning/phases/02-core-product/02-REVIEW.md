---
phase: 02-core-product
reviewed: 2026-04-20T00:00:00Z
depth: standard
files_reviewed: 46
files_reviewed_list:
  - api/Nodefy.Api/Data/Entities/Pipeline.cs
  - api/Nodefy.Api/Data/Entities/Stage.cs
  - api/Nodefy.Api/Data/Entities/ActivityLog.cs
  - api/Nodefy.Api/Data/Entities/Card.cs
  - api/Nodefy.Api/Data/AppDbContext.cs
  - api/Nodefy.Api/Data/AppDbContextFactory.cs
  - api/Nodefy.Api/Lib/FractionalIndex.cs
  - api/Nodefy.Api/Endpoints/PipelineEndpoints.cs
  - api/Nodefy.Api/Endpoints/StageEndpoints.cs
  - api/Nodefy.Api/Endpoints/ActivityLogEndpoints.cs
  - api/Nodefy.Api/Endpoints/CardEndpoints.cs
  - api/Nodefy.Api/Program.cs
  - api/Nodefy.Api/Migrations/20260418150403_Phase2Schema.cs
  - api/Nodefy.Tests/Integration/PipelineTests.cs
  - api/Nodefy.Tests/Integration/StageTests.cs
  - api/Nodefy.Tests/Integration/BoardTests.cs
  - api/Nodefy.Tests/Integration/CardTests.cs
  - api/Nodefy.Tests/Integration/ActivityLogTests.cs
  - api/Nodefy.Tests/Unit/FractionalIndexTests.cs
  - db/init.sql
  - frontend/src/types/api.ts
  - frontend/src/store/ui-store.ts
  - frontend/src/hooks/use-board.ts
  - frontend/src/hooks/use-pipelines.ts
  - frontend/src/hooks/use-stages.ts
  - frontend/src/lib/stage-age.ts
  - frontend/src/components/board/BoardShell.tsx
  - frontend/src/components/board/KanbanColumn.tsx
  - frontend/src/components/board/KanbanCard.tsx
  - frontend/src/components/board/CardDragOverlay.tsx
  - frontend/src/components/board/CardDetailPanel.tsx
  - frontend/src/components/sidebar/PipelineSidebar.tsx
  - frontend/src/components/sidebar/PipelineListItem.tsx
  - frontend/src/app/workspace/[id]/pipeline/[pipelineId]/page.tsx
  - frontend/src/app/api/workspaces/[id]/pipelines/route.ts
  - frontend/src/app/api/pipelines/[id]/route.ts
  - frontend/src/app/api/pipelines/[id]/board/route.ts
  - frontend/src/app/api/pipelines/[id]/stages/route.ts
  - frontend/src/app/api/pipelines/[id]/cards/route.ts
  - frontend/src/app/api/stages/[id]/route.ts
  - frontend/src/app/api/stages/[id]/position/route.ts
  - frontend/src/app/api/cards/[id]/route.ts
  - frontend/src/app/api/cards/[id]/move/route.ts
  - frontend/src/app/api/cards/[id]/archive/route.ts
  - frontend/src/components/Providers.tsx
  - frontend/src/components/FirstPipelineForm.tsx
  - frontend/src/app/workspace/[id]/page.tsx
findings:
  critical: 2
  warning: 6
  info: 5
  total: 13
status: issues_found
---

# Phase 02: Code Review Report

**Reviewed:** 2026-04-20
**Depth:** standard
**Files Reviewed:** 46
**Status:** issues_found

## Summary

This phase delivers the core product: pipelines, stages, cards, drag-and-drop, activity logs, and the board view. The architecture is solid — multi-tenancy via EF Core global query filters backed by Postgres RLS is correctly applied across all new entities. The fractional-index ordering system is correct and well-tested. Optimistic updates on TanStack Query are properly rolled back on error.

Two critical issues were found: a TOCTOU (time-of-check/time-of-use) race in the board endpoint that allows cross-tenant data reads before the tenant guard fires, and a reflected XSS vector in the activity panel where raw JSON payloads from the server are rendered directly into the DOM via `innerHTML`-equivalent string interpolation in JSX.

Six warnings cover logic gaps: card creation silently ignores board cache invalidation, drag-and-drop drops same-stage reorders, hardcoded BRL currency in the detail panel, missing guard on empty `workspaceId` in the board API proxy, an unguarded `FractionalIndex.Before(0)` that produces a negative position, and a Title-validation gap that allows single-character titles of only whitespace to pass server-side.

---

## Critical Issues

### CR-01: Cross-Tenant Read Race in `/pipelines/{id}/board` (TOCTOU)

**File:** `api/Nodefy.Api/Endpoints/PipelineEndpoints.cs:63-68`

**Issue:** The board endpoint first loads the pipeline using bare `db.Pipelines.FirstOrDefaultAsync(p => p.Id == id)` (no `IgnoreQueryFilters`, but the global filter has not yet been armed — `tenant.SetTenant` is called on line 67 only after the pipeline is fetched). At this point `_tenantId` in the `DbContext` is `Guid.Empty` (the value injected by `NullTenantService` or the previous request's value). The pipeline query therefore runs against whatever tenant context is currently set on the scoped `DbContext`, not the tenant that owns the requested pipeline.

A user from Tenant A can request `GET /pipelines/{id}/board` where `id` belongs to Tenant B. Because the pipeline fetch on line 64 occurs before `SetTenant`, the global query filter is either empty or set to A's tenant. If the pipeline entity is returned anyway (because EF hasn't applied a filter yet or the previous tenant matches), `SetTenant` is called with Tenant B's id, and all subsequent stage and card queries return Tenant B's data — a full cross-tenant data leak.

The same pattern is used in `PATCH /pipelines/{id}` (line 99) and `DELETE /pipelines/{id}` (line 117), but those both call `IgnoreQueryFilters()` explicitly and set the tenant before the authorization check, which is the correct pattern. The board endpoint does not use `IgnoreQueryFilters()` but also does not have the tenant set yet.

**Fix:** Mirror the pattern used in all other endpoints — use `IgnoreQueryFilters()` for the bootstrap read, then set the tenant, then re-query or re-validate under the active filter:

```csharp
// GET /pipelines/{id}/board
var pipeline = await db.Pipelines.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
if (pipeline is null) return Results.NotFound();

tenant.SetTenant(pipeline.TenantId);

// Now stages and cards queries run under the correct tenant filter
var stages = await db.Stages
    .Where(s => s.PipelineId == id)
    .OrderBy(s => s.Position)
    .ToListAsync();
// ... rest unchanged
```

---

### CR-02: Raw JSON Payload Rendered as Readable String in Activity Panel (Potential XSS / Data Integrity)

**File:** `frontend/src/components/board/CardDetailPanel.tsx:44-51`

**Issue:** The `activityLabel` function renders the raw `log.payload` JSON string directly into the UI for the `moved` and `edited` actions:

```typescript
case "moved": return `Card movido: ${log.payload}`
case "edited": return `Campo editado: ${log.payload}`
default: return log.payload
```

`log.payload` is a raw JSON string (e.g. `{"from_stage":"Prospecção","to_stage":"Proposta"}`). It is rendered verbatim, resulting in output like "Card movido: {"from_stage":"Prospecção","to_stage":"Proposta"}". This is both a UX bug (raw JSON exposed to users) and a security concern: if any future code path uses `dangerouslySetInnerHTML` or the payload is injected into an `innerHTML` context, stored XSS becomes possible since the payload content is attacker-controlled (stage names, field values set by users).

Even with React's default text-node escaping, the raw JSON output is incorrect behavior — it should parse the payload and render a human-readable message.

**Fix:** Parse the payload and build a proper label:

```typescript
function activityLabel(log: ActivityLog) {
  let parsed: Record<string, unknown> = {}
  try { parsed = JSON.parse(log.payload) } catch { /* ignore */ }

  switch (log.action) {
    case "created": return "Card criado"
    case "moved":
      return `Card movido: ${parsed.from_stage ?? "?"} → ${parsed.to_stage ?? "?"}`
    case "edited":
      return `Campo editado: ${parsed.field ?? "?"}`
    case "archived": return "Card arquivado"
    default: return log.action
  }
}
```

---

## Warnings

### WR-01: Card Creation Does Not Invalidate Board Cache

**File:** `frontend/src/components/board/BoardShell.tsx:128-138`

**Issue:** `handleCreateCard` calls the API directly via `fetch` without going through any TanStack Query mutation. After a successful card creation, the board cache (`["board", pipelineId]`) is never invalidated. The new card will not appear until the 30-second stale window expires or the user manually refreshes.

```typescript
async function handleCreateCard(stageId: string) {
  const title = newCardTitle.trim()
  if (!title) return
  await fetch(`/api/pipelines/${pipelineId}/cards`, { ... })
  // No cache invalidation here
  setNewCardTitle("")
  setNewCardStageId(null)
}
```

**Fix:** Move card creation into a TanStack Query mutation that invalidates `["board", pipelineId]` on settled, or at minimum call `qc.invalidateQueries({ queryKey: ["board", pipelineId] })` after the successful fetch. Consider extracting a `useCards(pipelineId)` hook.

---

### WR-02: Drag-and-Drop Silently Drops Same-Stage Reorders

**File:** `frontend/src/components/board/BoardShell.tsx:99-100`

**Issue:** `handleDragEnd` returns early when `sourceStageId === targetStageId`:

```typescript
if (sourceStageId === targetStageId) return
```

This means reordering cards within the same column is silently ignored — no API call is made and the card snaps back to its original position. For a CRM where ordering represents priority, this is a functional gap rather than just a missing feature.

**Fix:** When `sourceStageId === targetStageId`, compute the new position relative to the card being dropped over and call `moveMutation` (or a dedicated reorder mutation) with `TargetStageId = sourceStageId` and updated `prevPosition`/`nextPosition`. The backend `PATCH /cards/{id}/move` already supports same-stage moves since it only checks `targetStage.PipelineId === card.PipelineId`.

---

### WR-03: `FractionalIndex.Before(0)` Produces a Negative Position

**File:** `api/Nodefy.Api/Lib/FractionalIndex.cs:14`

**Issue:** `Before(first)` is implemented as `first / 2.0`. When `first` is `0.0` (which is the default `Card.Position` value), this returns `0.0 / 2 = 0.0`, not a position before it. When called with very small positive values approaching zero (possible after many repeated `Before()` calls), it eventually converges to zero or rounds to zero in `double` precision. There is also no guard against `first = 0` or negative inputs.

Additionally, if `FractionalIndex.Before(0.0)` were intentionally called, it would return `0.0` — the same value as the anchor — causing a duplicate position rather than a strictly smaller one.

**Fix:** Add a guard and use an additive fallback when the input is at or near zero:

```csharp
public static double Before(double first) =>
    first > 1e-9 ? first / 2.0 : first - 1_000_000.0;
```

Also update `NeedsRebalance` to detect negative positions: `p < 0 || p < 1e-9 || p > 1e15`.

---

### WR-04: `workspaceId` Can Be Empty String in Board Proxy — Silent Misbehavior

**File:** `frontend/src/app/api/pipelines/[id]/board/route.ts:7-8`

**Issue:** The route proxy reads `workspaceId` from the query string with a fallback of empty string:

```typescript
const workspaceId = url.searchParams.get("workspaceId") ?? ""
```

If `workspaceId` is absent or empty, `apiFetch` is called with `tenantId: ""`. The downstream API will set the tenant to `Guid.Empty`, which means the RLS policy `tenant_id = current_setting('app.current_tenant')::UUID` will silently evaluate `tenant_id = '00000000-...'` and return no results (empty board) rather than failing with a clear error.

**Fix:** Return a 400 before proxying when `workspaceId` is missing:

```typescript
const workspaceId = url.searchParams.get("workspaceId")
if (!workspaceId) {
  return NextResponse.json({ error: "workspaceId is required" }, { status: 400 })
}
```

---

### WR-05: Hardcoded Currency in Card Detail Panel

**File:** `frontend/src/components/board/CardDetailPanel.tsx:202`

**Issue:** The monetary value in the detail panel is always formatted as BRL, ignoring the workspace's actual currency setting:

```typescript
new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(card.monetaryValue)
```

The `BoardShell` passes `workspace.currency` down to `KanbanColumn` and `KanbanCard` correctly, but `CardDetailPanel` only receives `workspaceId` and `pipelineId` — not the currency. This means a USD or EUR workspace displays card values as BRL in the detail panel.

**Fix:** Add a `workspaceCurrency` prop to `CardDetailPanel` and pass it from `BoardShell`:

```typescript
// CardDetailPanel
interface CardDetailPanelProps {
  workspaceId: string
  pipelineId: string
  workspaceCurrency: "BRL" | "USD" | "EUR"
}
// Use workspaceCurrency instead of hardcoded "BRL"
```

---

### WR-06: Title Validation Does Not Reject Whitespace-Only Strings

**File:** `api/Nodefy.Api/Endpoints/CardEndpoints.cs:46-47`

**Issue:** The title validation checks `string.IsNullOrWhiteSpace(req.Title) || req.Title.Length < 2`. However, `req.Title.Length < 2` checks the raw (untrimmed) length. A title of `"  "` (two spaces) passes both checks — `IsNullOrWhiteSpace` is false because the string is not null and is not whitespace... wait, actually `IsNullOrWhiteSpace("  ")` is `true`, so that case is caught. But a title of `" a"` (space + one letter) has length 2 and passes `IsNullOrWhiteSpace` = false, yet its trimmed length is 1. The card would be saved with title `" a"` (leading whitespace), since the title is not trimmed before persisting.

**Fix:** Trim the title before validation and persist the trimmed version:

```csharp
var trimmedTitle = req.Title?.Trim() ?? "";
if (trimmedTitle.Length < 2)
    return Results.ValidationProblem(...);
// ...
card.Title = trimmedTitle;
```

Apply the same pattern to pipeline and stage name validation in `PipelineEndpoints.cs` and `StageEndpoints.cs`.

---

## Info

### IN-01: `formatCurrency` Is Duplicated Across Three Components

**File:** `frontend/src/components/board/KanbanCard.tsx:15-22`, `frontend/src/components/board/KanbanColumn.tsx:39-46`, `frontend/src/components/board/CardDragOverlay.tsx:11-18`

**Issue:** The `formatCurrency` function is copy-pasted identically in three components. Any future change (e.g., adding a new currency) must be applied in three places.

**Fix:** Extract to `frontend/src/lib/format-currency.ts` and import from all three components.

---

### IN-02: `getStageAge` Returns `0h` for Cards Entered in the Last Hour

**File:** `frontend/src/lib/stage-age.ts:4-5`

**Issue:** When `diffDays < 1` and `diffHours = 0`, the label returns `"0h"`. This occurs for cards that entered a stage less than an hour ago, producing an unhelpful badge. Cards in this state should show minutes or "just now".

**Fix:**
```typescript
const label =
  diffDays >= 1 ? `${diffDays}d`
  : diffHours >= 1 ? `${diffHours}h`
  : diffMinutes >= 1 ? `${diffMinutes}m`
  : "agora"
```

---

### IN-03: Board Endpoint Has N+1 Query Pattern

**File:** `api/Nodefy.Api/Endpoints/PipelineEndpoints.cs:69-89`

**Issue:** The board endpoint loads stages in one query, then issues one `db.Cards` query per stage inside a `foreach` loop. With many stages (e.g., 20), this is 21 round-trips to the database. Note: this is flagged as Info because it is a code quality concern, not a correctness issue, and performance is out of v1 scope per review guidelines — but it is worth noting for imminent growth.

**Fix:** Replace with a single query that fetches all cards for the pipeline at once, then group in memory:

```csharp
var allCards = await db.Cards
    .Where(c => c.PipelineId == id)
    .OrderBy(c => c.Position)
    .Select(c => new CardSummaryDto(...))
    .ToListAsync();

var cardsByStage = allCards
    .GroupBy(c => c.StageId)  // requires StageId in CardSummaryDto
    .ToDictionary(g => g.Key, g => g.ToList());
```

---

### IN-04: `Pipeline` Type in `api.ts` Includes `createdAt` but the API Never Returns It

**File:** `frontend/src/types/api.ts:31-36`

**Issue:** The `Pipeline` interface includes `createdAt: string`, but `PipelineDto` on the backend is defined as `record PipelineDto(Guid Id, string Name, double Position)` — `createdAt` is not included in any pipeline response. The field will always be `undefined` at runtime, which TypeScript believes is a `string`. Any code that reads `pipeline.createdAt` will silently get `undefined`.

**Fix:** Remove `createdAt` from the `Pipeline` frontend type, or update the backend `PipelineDto` to include it if it is needed.

---

### IN-05: `AppDbContextFactory` Contains a Hardcoded Credential

**File:** `api/Nodefy.Api/Data/AppDbContextFactory.cs:18-19`

**Issue:** The design-time factory has a hardcoded connection string with a password `changeme_local_dev`. While the comment documents that this is for design-time only and not used at runtime, the password appears in source control. This is acceptable for a local-only dev credential that is also in `Program.cs` as a fallback, but it should be noted that the same password string also appears as the default fallback in `Program.cs:22`.

**Fix:** Replace the hardcoded password with a placeholder that cannot accidentally be used as a real credential, or load it from `appsettings.Development.json` which can be gitignored:

```csharp
optionsBuilder.UseNpgsql(
    "Host=localhost;Database=nodefy;Username=nodefy_app;Password=DESIGN_TIME_ONLY");
```

---

_Reviewed: 2026-04-20_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
