# Phase 3: Collaboration & Discovery — Pattern Map

**Mapped:** 2026-04-20
**Files analyzed:** 8 new/modified files
**Analogs found:** 8 / 8

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `api/Nodefy.Api/Endpoints/CardEndpoints.cs` (modify — add broadcast) | controller | request-response + event-driven | self | exact (modify in place) |
| `api/Nodefy.Api/Endpoints/PipelineEndpoints.cs` (modify — add search endpoint) | controller | request-response | `CardEndpoints.cs` | exact |
| `api/Nodefy.Api/Hubs/BoardHub.cs` (modify — add broadcast methods) | hub | event-driven / pub-sub | self (stub exists) | exact (extend stub) |
| `frontend/src/hooks/use-board.ts` (modify — add SignalR connection) | hook | event-driven + request-response | self | exact (modify in place) |
| `frontend/src/store/ui-store.ts` (modify — add filter state) | store | client-state | self | exact (modify in place) |
| `frontend/src/app/api/pipelines/[id]/cards/search/route.ts` (new) | route | request-response | `frontend/src/app/api/pipelines/[id]/board/route.ts` | exact |
| `frontend/src/components/board/BoardShell.tsx` (modify — add filter bar + search input) | component | request-response + client-state | self | exact (modify in place) |
| `frontend/src/components/board/FilterBar.tsx` (new) | component | client-state | `frontend/src/components/board/KanbanColumn.tsx` | role-match |

---

## Pattern Assignments

### `api/Nodefy.Api/Hubs/BoardHub.cs` (hub, pub-sub — extend stub)

**Analog:** self — `api/Nodefy.Api/Hubs/BoardHub.cs` (current stub, lines 1–14)

**Current stub pattern** (lines 1–14):
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Nodefy.Api.Hubs;

[Authorize]
public class BoardHub : Hub
{
    public Task JoinBoard(string pipelineId)
        => Groups.AddToGroupAsync(Context.ConnectionId, $"pipeline:{pipelineId}");

    public Task LeaveBoard(string pipelineId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"pipeline:{pipelineId}");
}
```

**Broadcast method to add** — inject `IHubContext<BoardHub>` at call sites (endpoints), NOT inside the hub itself. Server-side broadcasts use `IHubContext`, never `Hub` directly:
```csharp
// Pattern for how endpoints will call the hub — NOT code inside BoardHub.cs:
// await hubContext.Clients.Group($"pipeline:{card.PipelineId}").SendAsync("CardMoved", ToDto(card));
// await hubContext.Clients.Group($"pipeline:{card.PipelineId}").SendAsync("CardCreated", ToDto(card));
// await hubContext.Clients.Group($"pipeline:{card.PipelineId}").SendAsync("CardUpdated", ToDto(card));
// await hubContext.Clients.Group($"pipeline:{card.PipelineId}").SendAsync("CardArchived", new { id = card.Id });
```

The group key convention already established: `$"pipeline:{pipelineId}"` (line 10).

---

### `api/Nodefy.Api/Endpoints/CardEndpoints.cs` (modify — inject IHubContext, broadcast after SaveChangesAsync)

**Analog:** self — `api/Nodefy.Api/Endpoints/CardEndpoints.cs`

**Endpoint registration pattern** (lines 29–88) — all handlers follow this shape:
```csharp
app.MapPost("/pipelines/{pipelineId:guid}/cards",
    async (Guid pipelineId, CreateCardRequest req, AppDbContext db,
           CurrentUserAccessor user, ITenantService tenant) =>
    {
        // 1. Resolve tenant via IgnoreQueryFilters()
        // 2. tenant.SetTenant(...)
        // 3. Member-level authz check
        // 4. Validate inputs → Results.ValidationProblem / Results.BadRequest
        // 5. Mutate entity
        // 6. ActivityLogEndpoints.LogActivity(...) before SaveChangesAsync
        // 7. await db.SaveChangesAsync()
        // 8. return Results.Created / Results.Ok
    }).RequireAuthorization();
```

**Broadcast injection pattern** — add `IHubContext<BoardHub> hub` as a lambda parameter (minimal API DI):
```csharp
// Before (existing):
async (Guid id, MoveCardRequest req, AppDbContext db, CurrentUserAccessor user, ITenantService tenant) =>

// After (add hub):
async (Guid id, MoveCardRequest req, AppDbContext db, CurrentUserAccessor user,
       ITenantService tenant, IHubContext<BoardHub> hub) =>
```

**Broadcast call placement** — always AFTER `await db.SaveChangesAsync()`, fire-and-forget is acceptable:
```csharp
// ... existing code:
ActivityLogEndpoints.LogActivity(db, card.TenantId, card.Id, user.UserId!.Value,
    "moved", new { from_stage = fromStage?.Name ?? "", to_stage = targetStage.Name });
await db.SaveChangesAsync();

// ADD after SaveChangesAsync:
await hub.Clients
    .Group($"pipeline:{card.PipelineId}")
    .SendAsync("CardMoved", ToDto(card));

return Results.Ok(ToDto(card));
```

**Tenant resolution pattern** (lines 36–39, 95–98, 112–115, 172–174, 194–196) — always bootstrap tenant before using filtered queries:
```csharp
var card = await db.Cards.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
if (card is null) return Results.NotFound();
tenant.SetTenant(card.TenantId);
```

**Member authz pattern** (lines 42–44, 117–119, 176–178, 199–201):
```csharp
var isMember = await db.WorkspaceMembers.IgnoreQueryFilters()
    .AnyAsync(m => m.TenantId == card.TenantId && m.UserId == user.UserId);
if (!isMember) return Results.Forbid();
```

---

### `api/Nodefy.Api/Endpoints/PipelineEndpoints.cs` (modify — add card search endpoint)

**Analog:** `api/Nodefy.Api/Endpoints/PipelineEndpoints.cs` — the board GET handler (lines 62–94) is the closest model for a new read endpoint on the same pipeline resource.

**Board GET handler as search template** (lines 62–94):
```csharp
app.MapGet("/pipelines/{id:guid}/board",
    async (Guid id, AppDbContext db, ITenantService tenant) =>
{
    var pipeline = await db.Pipelines.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
    if (pipeline is null) return Results.NotFound();

    tenant.SetTenant(pipeline.TenantId);

    // Build query from here — apply filters then project to DTO
    var stages = await db.Stages
        .Where(s => s.PipelineId == id)
        .OrderBy(s => s.Position)
        .ToListAsync();
    // ...
    return Results.Ok(new BoardDto(pipelineDto, stageDtos));
}).RequireAuthorization();
```

**New search endpoint shape** — add after existing board GET:
```csharp
// GET /pipelines/{id}/cards/search?q=&assigneeId=&minValue=&maxValue=&closeBefore=&closeAfter=
app.MapGet("/pipelines/{id:guid}/cards/search",
    async (Guid id, string? q, Guid? assigneeId, decimal? minValue, decimal? maxValue,
           DateTimeOffset? closeBefore, DateTimeOffset? closeAfter,
           AppDbContext db, ITenantService tenant) =>
{
    var pipeline = await db.Pipelines.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
    if (pipeline is null) return Results.NotFound();

    tenant.SetTenant(pipeline.TenantId);

    // Use EF.Functions.ILike for PostgreSQL full-text (or simple ILIKE for v1)
    var query = db.Cards.Where(c => c.PipelineId == id && c.ArchivedAt == null);
    if (!string.IsNullOrWhiteSpace(q))
        query = query.Where(c => EF.Functions.ILike(c.Title, $"%{q}%")
                               || EF.Functions.ILike(c.Description ?? "", $"%{q}%"));
    if (assigneeId.HasValue) query = query.Where(c => c.AssigneeId == assigneeId);
    if (minValue.HasValue)   query = query.Where(c => c.MonetaryValue >= minValue);
    if (maxValue.HasValue)   query = query.Where(c => c.MonetaryValue <= maxValue);
    if (closeBefore.HasValue) query = query.Where(c => c.CloseDate <= closeBefore);
    if (closeAfter.HasValue)  query = query.Where(c => c.CloseDate >= closeAfter);

    var results = await query
        .OrderBy(c => c.StageId).ThenBy(c => c.Position)
        .Select(c => new CardDto(...))
        .ToListAsync();

    return Results.Ok(results);
}).RequireAuthorization();
```

**DTO projection pattern** — follow the existing `ToDto(Card card)` private method (lines 233–236):
```csharp
private static CardDto ToDto(Card card) => new(
    card.Id, card.Title, card.Description, card.MonetaryValue,
    card.StageId, card.PipelineId, card.AssigneeId, card.CloseDate,
    card.StageEnteredAt, card.Position, card.ArchivedAt, card.CreatedAt);
```

---

### `frontend/src/hooks/use-board.ts` (modify — add SignalR connection)

**Analog:** self — `frontend/src/hooks/use-board.ts` (lines 1–70)

**Current imports pattern** (lines 1–3):
```typescript
"use client"
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import type { BoardData, CardSummary } from "@/types/api"
```

**Add SignalR imports** alongside existing:
```typescript
import { useEffect, useRef } from "react"
import * as signalR from "@microsoft/signalr"
```

**TanStack Query invalidation pattern** (line 66 — already used in `onSettled`):
```typescript
onSettled: () => qc.invalidateQueries({ queryKey: ["board", pipelineId] }),
```
SignalR handlers should call the same `qc.invalidateQueries` to refresh board data on remote events.

**SignalR connection lifecycle pattern** — place inside the hook, after the `useQuery` declaration:
```typescript
const connectionRef = useRef<signalR.HubConnection | null>(null)

useEffect(() => {
  const conn = new signalR.HubConnectionBuilder()
    .withUrl("/api/signalr/board")        // proxied through Next.js API route
    .withAutomaticReconnect()
    .build()

  conn.on("CardMoved",    () => qc.invalidateQueries({ queryKey: ["board", pipelineId] }))
  conn.on("CardCreated",  () => qc.invalidateQueries({ queryKey: ["board", pipelineId] }))
  conn.on("CardUpdated",  () => qc.invalidateQueries({ queryKey: ["board", pipelineId] }))
  conn.on("CardArchived", () => qc.invalidateQueries({ queryKey: ["board", pipelineId] }))

  conn.start()
    .then(() => conn.invoke("JoinBoard", pipelineId))
    .catch(console.error)

  connectionRef.current = conn

  return () => {
    conn.invoke("LeaveBoard", pipelineId).catch(() => {})
    conn.stop()
  }
}, [pipelineId, qc])
```

**Return shape extension** (line 69):
```typescript
// Current:
return { board, moveMutation }
// After — no change needed; SignalR is side-effect only
```

---

### `frontend/src/store/ui-store.ts` (modify — add filter state)

**Analog:** self — `frontend/src/store/ui-store.ts` (lines 1–40)

**Current store shape** (lines 4–18):
```typescript
interface UIState {
  activeWorkspaceId: string | null
  setActiveWorkspace: (id: string | null) => void

  activePipelineId: string | null
  setActivePipelineId: (id: string | null) => void

  sidebarCollapsed: boolean
  setSidebarCollapsed: (collapsed: boolean) => void

  draggingCardId: string | null
  setDraggingCardId: (id: string | null) => void
}
```

**Filter state to add** — follow the same interface + setter pattern. Filter state is NOT persisted (no `partialize` entry — only `sidebarCollapsed` is persisted, line 37):
```typescript
// Add to interface:
boardFilter: BoardFilter
setBoardFilter: (filter: Partial<BoardFilter>) => void
clearBoardFilter: () => void

// BoardFilter type (new, add to this file or types/api.ts):
export interface BoardFilter {
  q: string
  assigneeId: string | null
  minValue: number | null
  maxValue: number | null
  closeBefore: string | null   // ISO string
  closeAfter: string | null
}

// Add to create() body:
boardFilter: { q: "", assigneeId: null, minValue: null, maxValue: null, closeBefore: null, closeAfter: null },
setBoardFilter: (filter) => set((s) => ({ boardFilter: { ...s.boardFilter, ...filter } })),
clearBoardFilter: () => set({ boardFilter: { q: "", assigneeId: null, minValue: null, maxValue: null, closeBefore: null, closeAfter: null } }),
```

**Persist middleware pattern** (lines 20–39) — do NOT add `boardFilter` to `partialize`; it's session-only:
```typescript
export const useUIStore = create<UIState>()(
  persist(
    (set) => ({ /* ... */ }),
    {
      name: "nodefy_sidebar_collapsed",
      partialize: (s) => ({ sidebarCollapsed: s.sidebarCollapsed }),
      //                    ^^^ only this field survives localStorage — keep it that way
    }
  )
)
```

---

### `frontend/src/app/api/pipelines/[id]/cards/search/route.ts` (new)

**Analog:** `frontend/src/app/api/pipelines/[id]/board/route.ts` (lines 1–17) — identical structure, GET handler proxying to .NET API.

**Analog pattern** (board route, lines 1–17):
```typescript
import { NextResponse } from "next/server"
import { apiFetch } from "@/lib/api"

export async function GET(req: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params
  const url = new URL(req.url)
  const workspaceId = url.searchParams.get("workspaceId")
  if (!workspaceId) {
    return NextResponse.json({ error: "workspaceId is required" }, { status: 400 })
  }
  try {
    const data = await apiFetch(`/pipelines/${id}/board`, { tenantId: workspaceId })
    return NextResponse.json(data)
  } catch (e: any) {
    return NextResponse.json({ error: e.message }, { status: 500 })
  }
}
```

**New search route** — forward all search query params to the .NET API:
```typescript
import { NextResponse } from "next/server"
import { apiFetch } from "@/lib/api"

export async function GET(req: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params
  const url = new URL(req.url)
  const workspaceId = url.searchParams.get("workspaceId")
  if (!workspaceId) {
    return NextResponse.json({ error: "workspaceId is required" }, { status: 400 })
  }
  // Forward search params: q, assigneeId, minValue, maxValue, closeBefore, closeAfter
  const qs = new URLSearchParams()
  for (const [k, v] of url.searchParams.entries()) {
    if (k !== "workspaceId") qs.set(k, v)
  }
  try {
    const data = await apiFetch(`/pipelines/${id}/cards/search?${qs}`, { tenantId: workspaceId })
    return NextResponse.json(data)
  } catch (e: any) {
    return NextResponse.json({ error: e.message }, { status: 500 })
  }
}
```

**apiFetch auth pattern** (from `lib/api.ts` lines 9–33) — called identically by all route handlers. It reads the Auth.js session, mints a JWT, and sets `X-Tenant-Id` header automatically when `tenantId` is passed:
```typescript
export async function apiFetch<T>(path: string, init?: RequestInit & { tenantId?: string }): Promise<T>
```

---

### `frontend/src/components/board/BoardShell.tsx` (modify — add filter bar + search input)

**Analog:** self — `frontend/src/components/board/BoardShell.tsx` (lines 1–309)

**Current imports pattern** (lines 1–29):
```typescript
"use client"
import { useState, useCallback } from "react"
import { useQueryClient } from "@tanstack/react-query"
// ... dnd-kit, lucide, shadcn/ui imports
import { useBoard } from "@/hooks/use-board"
import { useStages } from "@/hooks/use-stages"
import { useUIStore } from "@/store/ui-store"
import type { BoardData, CardSummary, Workspace } from "@/types/api"
```

**UIStore consumption pattern** (line 41):
```typescript
const { setDraggingCardId } = useUIStore()
// Extend to also destructure filter state:
const { setDraggingCardId, boardFilter, setBoardFilter, clearBoardFilter } = useUIStore()
```

**Board header section** (lines 172–184) — filter bar inserts directly below the existing `<h2>` header div:
```tsx
{/* Board header — existing */}
<div className="flex items-center justify-between px-4 py-2 border-b border-border bg-background">
  <h2 className="text-sm font-semibold">{board.pipeline.name}</h2>
  {/* ... Criar card button */}
</div>

{/* Filter bar — add here, between header and new-card form */}
<FilterBar filter={boardFilter} onChange={setBoardFilter} onClear={clearBoardFilter} />
```

**shadcn/ui component usage pattern** — existing components used: `Button`, `Input`, `Popover/PopoverContent/PopoverTrigger` (lines 19–21). FilterBar will use the same imports from `@/components/ui/*`.

**Conditional render pattern** (lines 187–217 — new card form):
```tsx
{newCardStageId && (
  <div className="flex items-center gap-2 px-4 py-2 border-b border-border bg-muted/30">
    {/* ... */}
  </div>
)}
```
FilterBar uses same border-b pattern to sit flush between header and board.

---

### `frontend/src/components/board/FilterBar.tsx` (new)

**Analog:** `frontend/src/components/board/KanbanColumn.tsx` — same `"use client"`, shadcn/ui imports, local state with `useState`, inline event handler functions.

**KanbanColumn "use client" + imports pattern** (lines 1–28):
```typescript
"use client"
import { useState, useRef } from "react"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
// plus shadcn dropdown, alert-dialog
```

**FilterBar component shape** — pure presentational, receives filter state from BoardShell via props, no data fetching of its own:
```typescript
"use client"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import type { BoardFilter } from "@/store/ui-store"

interface FilterBarProps {
  filter: BoardFilter
  onChange: (patch: Partial<BoardFilter>) => void
  onClear: () => void
}

export function FilterBar({ filter, onChange, onClear }: FilterBarProps) {
  // debounced text search input + assignee/date/value range controls
  // calls onChange({ q: value }) on input change
  // calls onClear() on "Limpar filtros" button
}
```

**Tailwind layout pattern** from BoardShell (lines 172–174):
```tsx
<div className="flex items-center gap-2 px-4 py-2 border-b border-border bg-muted/30">
```
FilterBar uses the same `px-4 py-2 border-b border-border bg-muted/30` shell as the existing new-card bar.

---

## Shared Patterns

### Tenant Bootstrap (Backend)
**Source:** `api/Nodefy.Api/Endpoints/CardEndpoints.cs` lines 36–39 / 95–98
**Apply to:** All new or modified backend endpoints
```csharp
var entity = await db.DbSet.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id);
if (entity is null) return Results.NotFound();
tenant.SetTenant(entity.TenantId);
// From here, all EF queries automatically apply the tenant global filter
```
**Rule:** Never call `.IgnoreQueryFilters()` after `tenant.SetTenant()` in normal code paths.

### SignalR Hub Registration (Backend)
**Source:** `api/Nodefy.Api/Program.cs` line 61
**Apply to:** BoardHub (already registered — do not register again)
```csharp
app.MapHub<BoardHub>("/hubs/board");
```
`builder.Services.AddSignalR()` is already registered at line 38.

### IHubContext Injection (Backend)
**Source:** Pattern — minimal API lambda DI (same as `CurrentUserAccessor`, `ITenantService` in CardEndpoints)
**Apply to:** CardEndpoints handlers that need to broadcast
```csharp
// Add IHubContext<BoardHub> hub to the handler lambda signature
// It is resolved by the DI container automatically — no registration needed
async (..., IHubContext<BoardHub> hub) =>
```

### apiFetch Auth Proxy (Frontend)
**Source:** `frontend/src/lib/api.ts` lines 9–33
**Apply to:** All new Next.js API route handlers
```typescript
// All routes follow this exact try/catch shape:
try {
  const data = await apiFetch(`/backend/path`, { tenantId: workspaceId })
  return NextResponse.json(data)
} catch (e: any) {
  return NextResponse.json({ error: e.message }, { status: 500 })
}
```

### TanStack Query Key Convention (Frontend)
**Source:** `frontend/src/hooks/use-board.ts` lines 9, 40–41, 66
**Apply to:** New search query in use-board.ts or a new use-search hook
```typescript
// Board data:     ["board", pipelineId]
// Search results: ["cards/search", pipelineId, filter]   ← follow same shape
queryKey: ["cards/search", pipelineId, boardFilter],
```

### Error Response Format (Backend)
**Source:** `CardEndpoints.cs` lines 50–51, 56, 122–123
**Apply to:** Search endpoint validation
```csharp
// Validation errors:
return Results.ValidationProblem(new Dictionary<string, string[]> { ["field"] = ["message"] });
// Domain errors:
return Results.BadRequest(new { error = "human-readable message" });
// Not found:
return Results.NotFound();
// Authz:
return Results.Forbid();
```

### shadcn/ui + Tailwind Component Shell (Frontend)
**Source:** `frontend/src/components/board/BoardShell.tsx` lines 163–217
**Apply to:** FilterBar.tsx
```tsx
// All board sub-components:
// - "use client" directive on line 1
// - Tailwind only (no inline styles except calc())
// - shadcn/ui components from @/components/ui/*
// - Portuguese UI text (Criar, Cancelar, Limpar filtros, etc.)
// - Lucide icons from lucide-react
```

---

## No Analog Found

All Phase 3 files have close analogs in the codebase. No files require falling back to RESEARCH.md patterns exclusively.

| File | Note |
|---|---|
| SignalR JS client in `use-board.ts` | Package `@microsoft/signalr` is already listed in CLAUDE.md stack; no existing frontend usage yet — use RESEARCH.md JS client docs |

---

## Metadata

**Analog search scope:** `api/Nodefy.Api/Endpoints/`, `api/Nodefy.Api/Hubs/`, `api/Nodefy.Api/`, `frontend/src/hooks/`, `frontend/src/store/`, `frontend/src/app/api/`, `frontend/src/components/board/`, `frontend/src/lib/`, `frontend/src/types/`
**Files scanned:** 14
**Pattern extraction date:** 2026-04-20
