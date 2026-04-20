# Phase 2: Core Product - Research

**Researched:** 2026-04-17
**Domain:** Kanban Board — dnd-kit DnD, TanStack Query optimistic updates, EF Core migrations, fractional indexing, activity log, shadcn Sheet panel
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Fixed left sidebar listing pipelines (240px expanded / 48px collapsed). Layout: WorkspaceTopNav top, sidebar left, Kanban board right.
- **D-02:** Hover on pipeline in sidebar shows `...` DropdownMenu (Rename, Delete) — admin only. No separate settings page.
- **D-03:** Sidebar collapsible via toggle (ChevronLeft/ChevronRight). Board expands to fill space.
- **D-04:** "+ Novo pipeline" footer button → Popover with inline name input.
- **D-05:** Stage management inline on board. Column header `...` → Rename, Delete (admin only). "+ Adicionar estágio" after last column. Stage reorder = drag column header (fractional indexing).

### Claude's Discretion

- Card visual design (compact card fields): Claude decided title + assignee avatar + monetary value + stage-age badge (see UI-SPEC).
- Card detail UX: Claude decided side panel (shadcn Sheet, right anchor) — keeps board visible.
- Empty states: Claude decides visual for empty stages and empty board.
- DnD visual feedback: Claude decides ghost, placeholder, rollback animation.
- Fractional index rebalance threshold: Claude decides — UI-SPEC establishes `< 1e-9` or `> 1e15` as trigger (see Interaction Contracts).

### Deferred Ideas (OUT OF SCOPE)

- Real-time SignalR broadcast on card move — Phase 3 (REAL-01, REAL-02)
- Card search and filters — Phase 3 (DISC-01, DISC-02)
- Drag-and-drop between pipelines — Out of scope for v1
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| PIPE-01 | Admin can create pipelines within a workspace | POST /pipelines endpoint; Pipeline entity + EF Core migration |
| PIPE-02 | Admin can rename and delete pipelines | PATCH /pipelines/{id} + DELETE /pipelines/{id}; cascade delete stages/cards |
| PIPE-03 | Admin can add, rename, and delete stages in a pipeline | CRUD /stages endpoints; Stage entity; cascade delete cards on stage delete |
| PIPE-04 | Admin can reorder stages | PATCH /stages/{id}/position; fractional indexing; rebalance when threshold hit |
| PIPE-05 | Each column header shows live card count and monetary value sum | Board load endpoint returns aggregates per stage via EF Core GroupBy projection |
| CARD-01 | Member can create a card with title, description, monetary value, assignee, close date | POST /cards; Card entity extended with ADD COLUMN migration; currency_locked enforcement |
| CARD-02 | Member can edit card fields | PATCH /cards/{id}; inline edit in Sheet panel; optimistic update with TanStack Query |
| CARD-03 | Member can archive (soft-delete) card | PATCH /cards/{id}/archive sets archived_at; EF Core global filter excludes archived |
| CARD-04 | Member can drag card to different stage; UI updates immediately, rolls back on failure | dnd-kit DndContext + SortableContext; TanStack Query onMutate/onError rollback; backend PATCH /cards/{id}/move |
| CARD-05 | Card shows time in current stage ("23 days in this stage") | stage_entered_at TIMESTAMPTZ computed in frontend; reset on PATCH move |
| CARD-06 | Card shows chronological activity log (creation, moves, edits) | Append-only activity_logs table; CARD-06 backend writes on every mutation |
</phase_requirements>

---

## Summary

Phase 2 delivers the full Kanban product: pipeline/stage CRUD with inline management, and a card system with drag-and-drop, optimistic updates, soft-delete, and activity logging. The tech stack is already decided (dnd-kit, TanStack Query v5, shadcn/ui, EF Core 9, PostgreSQL) and Phase 1 scaffolding is complete.

The largest risk area is the dnd-kit multi-container setup. Cross-column DnD requires a specific orchestration: a single `DndContext` wrapping multiple `SortableContext` providers (one per column), `onDragOver` for live column-crossing detection and temporary state update, `onDragEnd` for commit or cancel, and `DragOverlay` for the ghost card. Failing to use `DragOverlay` causes ID collision issues during scroll.

The second risk area is TanStack Query optimistic update integration with DnD. The board data lives in a single query cache entry (keyed by pipelineId). On `onDragStart`, snapshot the cache. On `onDragOver`, mutate the cache in-place for live visual feedback. On `onDragEnd`, fire the real mutation with `onMutate`/`onError`/`onSettled`. On failure, `setQueryData` restores the snapshot and a toast appears.

The backend work is additive: EF Core migration adds columns to the `cards` stub table and creates `pipelines`, `stages`, `activity_logs` tables. All new endpoints follow the flat `Endpoints/` file pattern established in Phase 1. The board load endpoint aggregates (card count + sum per stage) in a single query to avoid N+1.

**Primary recommendation:** Implement in wave order — DB migration first, backend CRUD second, then board UI. Fractional indexing is a pure arithmetic midpoint; implement `RebalancePositions` as a server utility called when any position crosses the thresholds defined in UI-SPEC (`< 1e-9` or `> 1e15`).

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Pipeline/Stage CRUD | API / Backend | Database | Business logic (tenant isolation, cascade) belongs server-side |
| Card CRUD + move | API / Backend | Database | Monetary value, stage_entered_at reset, activity log writes — all server |
| Card position (fractional index) | API / Backend | — | Server computes new position; client sends only target stageId + neighbour card IDs |
| Board load (pipelines + stages + cards + aggregates) | API / Backend | — | Single aggregated query; N+1 prevention |
| Append-only activity log | Database / Storage | API | Append-only INSERT; never UPDATE/DELETE |
| Optimistic drag-and-drop state | Browser / Client | — | Local cache mutation via TanStack Query setQueryData; zero server involvement until drop |
| Stage-age display | Browser / Client | — | Calculated from stage_entered_at timestamp returned by API; pure arithmetic |
| Sidebar collapse state | Browser / Client | — | Zustand + localStorage persistence |
| Card detail side panel (Sheet) | Browser / Client | — | URL query param `?card={id}` drives open state; shadcn Sheet |
| Admin-only UI gates | Browser / Client | API | Frontend gates UI; backend re-validates role on every write request |
| Multi-tenancy enforcement | API / Backend | Database | EF Core global query filter + PostgreSQL RLS (two-layer, Phase 1 pattern) |

---

## Standard Stack

### Core (locked by CLAUDE.md and Phase 1)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `@dnd-kit/core` | 6.3.1 | DnD context, sensors, collision detection, DragOverlay | Replaces archived react-beautiful-dnd; React 18 + strict mode compatible |
| `@dnd-kit/sortable` | 10.0.0 | `useSortable` hook + `SortableContext` for ordered lists | Pairs with core; handles sort order within and between containers |
| `@dnd-kit/utilities` | 3.2.2 | `CSS.Transform.toString()` for transform style | Required companion for smooth DnD animation |
| `@tanstack/react-query` | 5.99.0 | Server-state cache, optimistic updates, rollback | Already installed in Phase 1 |
| `zustand` | 5.0.12 | Local UI state: draggingCardId, sidebarCollapsed, activePipelineId, openCardId | Already installed in Phase 1 |
| `shadcn/ui` | (component copies) | Sheet (side panel), Popover (inline inputs), ScrollArea, Tooltip | Already initialized; Phase 2 adds 4 new components |
| EF Core 9 (`Microsoft.EntityFrameworkCore`) | 9.0.4 | ORM + migrations + global query filters | Already in use |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 9.0.4 | PostgreSQL provider for EF Core | Already in use |

[VERIFIED: npm registry — @dnd-kit/core@6.3.1, @dnd-kit/sortable@10.0.0, @dnd-kit/utilities@3.2.2, @tanstack/react-query@5.99.0, zustand@5.0.12]
[VERIFIED: csproj — Microsoft.EntityFrameworkCore@9.0.4, Npgsql.EntityFrameworkCore.PostgreSQL@9.0.4]

### Supporting (new installs for Phase 2)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `@dnd-kit/core` | 6.3.1 | (see above — new install) | Board DnD |
| `@dnd-kit/sortable` | 10.0.0 | (see above — new install) | Board DnD |
| `@dnd-kit/utilities` | 3.2.2 | (see above — new install) | Board DnD |

**New shadcn components (run in `frontend/`):**
```bash
npx shadcn@latest add scroll-area sheet tooltip popover
```

**Installation (frontend/):**
```bash
npm install @dnd-kit/core @dnd-kit/sortable @dnd-kit/utilities
```

**Version verification:**
```
@dnd-kit/core      6.3.1   — verified 2026-04-17
@dnd-kit/sortable  10.0.0  — verified 2026-04-17
@dnd-kit/utilities 3.2.2   — verified 2026-04-17
```

[VERIFIED: npm registry 2026-04-17]

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `@dnd-kit/sortable` | `useDraggable` + `useDroppable` raw | More control but far more boilerplate; sortable is the right abstraction for ordered lists |
| `closestCorners` collision | `closestCenter` | closestCenter misfires on nested containers (column container vs card droppables); closestCorners is the recommended choice for Kanban |
| Inline board aggregates | Separate `/stages/{id}/stats` endpoint | N+1 calls on board load; aggregate in the board load response |

---

## Architecture Patterns

### System Architecture Diagram

```
Browser (User action)
    │
    ├── DRAG START
    │   └─► Zustand: set draggingCardId
    │       TanStack Query: snapshot board cache
    │
    ├── DRAG OVER column
    │   └─► DndContext.onDragOver
    │       TanStack Query: setQueryData (temp visual move)
    │
    ├── DROP (onDragEnd)
    │   └─► TanStack Query useMutation
    │         onMutate  ─► cancelQueries + snapshot + setQueryData (optimistic)
    │         mutationFn ─► PATCH /pipelines/{pid}/cards/{cid}/move
    │         onError   ─► setQueryData (rollback) + toast
    │         onSettled ─► invalidateQueries (sync)
    │
    └── CLICK card
        └─► URL ?card={id} → Sheet opens
            ├── GET /cards/{id} (or from board cache)
            ├── Inline edit → PATCH /cards/{id}
            └── Archive → PATCH /cards/{id}/archive

Next.js App Router (Server Component)
    └─► WorkspaceLayout (RSC)
        └─► BoardShell (Client Component — DnD requires client)
            ├── Sidebar (Client — Zustand for collapse state)
            └── KanbanBoard (Client — DnD context lives here)

ASP.NET Core API
    ├── GET  /workspaces/{wid}/pipelines          → list pipelines
    ├── POST /workspaces/{wid}/pipelines          → create pipeline (PIPE-01)
    ├── PATCH /pipelines/{pid}                    → rename pipeline (PIPE-02)
    ├── DELETE /pipelines/{pid}                   → delete + cascade (PIPE-02)
    │
    ├── GET  /pipelines/{pid}/board               → stages + cards + aggregates (PIPE-05)
    ├── POST /pipelines/{pid}/stages              → add stage (PIPE-03)
    ├── PATCH /stages/{sid}                       → rename stage (PIPE-03)
    ├── DELETE /stages/{sid}                      → delete stage + cascade cards (PIPE-03)
    ├── PATCH /stages/{sid}/position              → reorder stage (PIPE-04)
    │
    ├── POST /pipelines/{pid}/cards               → create card (CARD-01)
    ├── PATCH /cards/{cid}                        → edit card fields (CARD-02)
    ├── PATCH /cards/{cid}/archive               → soft-delete (CARD-03)
    ├── PATCH /cards/{cid}/move                  → move card + reset stage_entered_at (CARD-04)
    └── GET  /cards/{cid}/activity               → activity log (CARD-06)

PostgreSQL (via EF Core)
    ├── pipelines      (tenant_id, name, position, created_at)
    ├── stages         (tenant_id, pipeline_id, name, position, created_at)
    ├── cards          (extended from stub: + title, description, monetary_value,
    │                   assignee_id, close_date, pipeline_id, stage_id, archived_at)
    └── activity_logs  (id, tenant_id, card_id, actor_id, action, payload JSONB, created_at)
```

### Recommended Project Structure (additions to Phase 1)

**Backend additions:**
```
api/Nodefy.Api/
├── Data/
│   └── Entities/
│       ├── Pipeline.cs          (new)
│       ├── Stage.cs             (new)
│       └── ActivityLog.cs       (new)
│       // Card.cs — ADD new properties
├── Endpoints/
│   ├── PipelineEndpoints.cs     (new)
│   ├── StageEndpoints.cs        (new)
│   ├── CardEndpoints.cs         (new)
│   └── ActivityLogEndpoints.cs  (new)
└── Migrations/
    └── 20260418000000_Phase2Schema.cs  (new — EF Core migration)
```

**Frontend additions:**
```
frontend/src/
├── app/workspace/[id]/
│   └── pipeline/
│       └── [pipelineId]/
│           └── page.tsx         (Kanban board page — Client Component)
├── components/
│   ├── board/
│   │   ├── BoardShell.tsx       (DndContext wrapper, layout)
│   │   ├── KanbanColumn.tsx     (SortableContext + droppable column)
│   │   ├── KanbanCard.tsx       (useSortable card)
│   │   ├── CardDragOverlay.tsx  (DragOverlay ghost card)
│   │   └── CardDetailPanel.tsx  (shadcn Sheet side panel)
│   └── sidebar/
│       ├── PipelineSidebar.tsx  (collapsible sidebar)
│       └── PipelineListItem.tsx (hover menu, active state)
├── hooks/
│   ├── use-board.ts             (TanStack Query — board data fetch + DnD mutation)
│   ├── use-pipelines.ts         (TanStack Query — pipeline CRUD mutations)
│   └── use-stages.ts            (TanStack Query — stage CRUD mutations)
├── store/
│   └── ui-store.ts              (extend: activePipelineId, sidebarCollapsed, draggingCardId, openCardId)
└── types/
    └── api.ts                   (extend: Pipeline, Stage, Card, ActivityLog, BoardData)
```

---

### Pattern 1: dnd-kit Multi-Container Kanban (cross-column DnD)

**What:** Single `DndContext` wraps multiple `SortableContext` (one per stage column). `onDragOver` updates local state for live visual feedback; `onDragEnd` commits or cancels.

**When to use:** Whenever cards must be draggable between stage columns.

**Sensor setup:**
```typescript
// Source: dndkit.com/extend/sensors + GitHub discussions #476
import {
  DndContext, DragOverlay, closestCorners,
  PointerSensor, KeyboardSensor, useSensor, useSensors
} from "@dnd-kit/core"
import { sortableKeyboardCoordinates } from "@dnd-kit/sortable"

const sensors = useSensors(
  useSensor(PointerSensor, {
    activationConstraint: { distance: 4 }, // 4px tolerance prevents click→drag misfire
  }),
  useSensor(KeyboardSensor, {
    coordinateGetter: sortableKeyboardCoordinates,
  })
)
```

**Board component skeleton:**
```typescript
// Source: dndkit.com official docs + multiple containers pattern
<DndContext
  sensors={sensors}
  collisionDetection={closestCorners}  // NOT closestCenter — misfires on nested containers
  onDragStart={handleDragStart}
  onDragOver={handleDragOver}   // updates local optimistic state for live column highlighting
  onDragEnd={handleDragEnd}     // fires TanStack Query mutation or cancels
>
  {stages.map(stage => (
    <SortableContext
      key={stage.id}
      items={cardIdsByStage[stage.id]}   // MUST be sorted in render order
      strategy={verticalListSortingStrategy}
    >
      <KanbanColumn stage={stage} cards={cardsByStage[stage.id]} />
    </SortableContext>
  ))}
  <DragOverlay>
    {activeCard ? <CardDragOverlay card={activeCard} /> : null}
  </DragOverlay>
</DndContext>
```

**Key rule:** `items` prop of `SortableContext` MUST contain the IDs in the same order as they are rendered. Violation causes unpredictable sorting behavior. [VERIFIED: dndkit.com/presets/sortable official docs]

**onDragOver (cross-column live preview):**
```typescript
// Source: dndkit official multiple containers pattern
function handleDragOver(event: DragOverEvent) {
  const { active, over } = event
  if (!over || active.id === over.id) return

  const activeStageId = active.data.current?.stageId
  const overStageId = over.data.current?.stageId ?? over.id // over can be column or card

  if (activeStageId !== overStageId) {
    // Move card visually between columns — update local board state (NOT query cache yet)
    setLocalBoardState(prev => moveBetweenColumns(prev, active.id, activeStageId, overStageId))
  }
}
```

**onDragEnd (commit via TanStack Query):**
```typescript
function handleDragEnd(event: DragEndEvent) {
  const { active, over } = event
  setActiveDraggingCard(null) // clear DragOverlay

  if (!over || active.id === over.id) {
    resetLocalBoardState() // cancelled — snap back
    return
  }

  moveMutation.mutate({
    cardId: active.id as string,
    targetStageId: over.data.current?.stageId ?? (over.id as string),
    position: computeNewPosition(boardState, active.id, over.id),
  })
}
```

[VERIFIED: dndkit.com docs — closestCorners recommended for stacked/nested containers]

---

### Pattern 2: TanStack Query v5 Optimistic Card Move

**What:** Snapshot board cache before mutation, update optimistically, roll back on error, invalidate on settle.

**When to use:** Every card mutation that needs immediate visual feedback (move, rename, archive).

```typescript
// Source: tanstack.com/query/v5/docs/framework/react/guides/optimistic-updates
const queryClient = useQueryClient()

const moveMutation = useMutation({
  mutationFn: ({ cardId, targetStageId, position }: MoveCardRequest) =>
    apiFetch(`/cards/${cardId}/move`, {
      method: "PATCH",
      body: JSON.stringify({ targetStageId, position }),
      tenantId: workspaceId,
    }),

  onMutate: async (variables) => {
    // 1. Cancel in-flight refetches to prevent overwriting optimistic update
    await queryClient.cancelQueries({ queryKey: ["board", pipelineId] })

    // 2. Snapshot current board state
    const previousBoard = queryClient.getQueryData<BoardData>(["board", pipelineId])

    // 3. Apply optimistic move to cache
    queryClient.setQueryData<BoardData>(["board", pipelineId], (old) =>
      applyCardMove(old!, variables)
    )

    return { previousBoard } // returned value goes to onError and onSettled
  },

  onError: (_err, _variables, context) => {
    // Roll back to snapshot
    if (context?.previousBoard) {
      queryClient.setQueryData(["board", pipelineId], context.previousBoard)
    }
    toast.error("Não foi possível mover o card. Tente novamente.")
  },

  onSettled: () => {
    // Always re-sync with server
    queryClient.invalidateQueries({ queryKey: ["board", pipelineId] })
  },
})
```

**CRITICAL v5 API note:** `onMutate` receives `(variables)` — the third argument to `onError` is `context` (the return value of `onMutate`). Access `queryClient` via closure (`useQueryClient()` at hook level), NOT via a `context.client` parameter — that is TanStack DB, not TanStack Query. [VERIFIED: tanstack.com/query/v5/docs/react/guides/mutations + community cross-check]

---

### Pattern 3: EF Core Migration — ADD COLUMN to cards stub

**What:** Phase 2 must extend the `cards` table (stub from Phase 1) and create `pipelines`, `stages`, `activity_logs`. Done via a single EF Core `dotnet ef migrations add` command.

**When to use:** First task of the backend plan. Must run before any endpoint code is written.

**Entity extensions:**
```csharp
// Card.cs — add to existing entity
public string Title { get; set; } = "";
public string? Description { get; set; }
public decimal? MonetaryValue { get; set; }
public Guid PipelineId { get; set; }
public Guid StageId { get; set; }
public Guid? AssigneeId { get; set; }
public DateTimeOffset? CloseDate { get; set; }
public DateTimeOffset? ArchivedAt { get; set; }
```

**AppDbContext — Card mapping extension:**
```csharp
// Extend OnModelCreating for Card entity
b.Property(e => e.Title).HasColumnName("title");
b.Property(e => e.Description).HasColumnName("description");
b.Property(e => e.MonetaryValue).HasColumnName("monetary_value").HasColumnType("numeric(15,2)");
b.Property(e => e.PipelineId).HasColumnName("pipeline_id");
b.Property(e => e.StageId).HasColumnName("stage_id");
b.Property(e => e.AssigneeId).HasColumnName("assignee_id");
b.Property(e => e.CloseDate).HasColumnName("close_date");
b.Property(e => e.ArchivedAt).HasColumnName("archived_at");

// Soft delete global filter — extends existing TenantId filter
b.HasQueryFilter(c => c.TenantId == _tenantId && c.ArchivedAt == null);
```

[VERIFIED: EF Core 9 docs — HasQueryFilter with multiple predicates (AND-chained in lambda)]

**Migration command:**
```bash
cd api && dotnet ef migrations add Phase2Schema --project Nodefy.Api --startup-project Nodefy.Api
```

---

### Pattern 4: Board Load Endpoint (aggregate query)

**What:** Single endpoint returns pipeline → stages → cards + per-stage count and monetary sum. Avoids N+1.

**When to use:** Board mount, and after mutations via TanStack Query invalidation.

```csharp
// GET /pipelines/{pipelineId}/board
group.MapGet("/{id}/board", async (Guid id, AppDbContext db) =>
{
    var stages = await db.Stages
        .Where(s => s.PipelineId == id)
        .OrderBy(s => s.Position)
        .Select(s => new StageDto(
            s.Id,
            s.Name,
            s.Position,
            db.Cards
                .Where(c => c.StageId == s.Id)
                .OrderBy(c => c.Position)
                .Select(c => new CardDto(c.Id, c.Title, c.MonetaryValue, c.AssigneeId, c.StageEnteredAt, c.Position))
                .ToList(),
            db.Cards.Count(c => c.StageId == s.Id),              // card count
            db.Cards.Where(c => c.StageId == s.Id).Sum(c => (decimal?)c.MonetaryValue) ?? 0  // monetary sum
        ))
        .ToListAsync();

    return Results.Ok(stages);
});
```

**Alternative (cleaner):** Load stages + cards separately with GroupBy for aggregates. EF Core 9 fully translates `GroupBy` + `Count()` + `Sum()` to SQL. [VERIFIED: learn.microsoft.com/en-us/ef/core/querying/complex-query-operators]

---

### Pattern 5: Fractional Indexing — Position Computation

**What:** New card/stage position = midpoint of two neighbours. Rebalance if positions collapse below threshold.

**Algorithm:**
```csharp
// Source: standard fractional indexing; ASSUMED (training knowledge, well-known algorithm)
public static class FractionalIndex
{
    // Insert between prev and next. Pass 0 or double.MaxValue for list boundaries.
    public static double Between(double? prev, double? next)
    {
        double lo = prev ?? 0.0;
        double hi = next ?? 1_000_000.0;
        return (lo + hi) / 2.0;
    }

    // Insert at end: position after last item
    public static double After(double last) => last + 1_000_000.0;

    // Insert at start: position before first item
    public static double Before(double first) => first / 2.0;

    // Rebalance check — thresholds from UI-SPEC Interaction Contracts
    public static bool NeedsRebalance(IEnumerable<double> positions) =>
        positions.Any(p => p < 1e-9 || p > 1e15);

    // Evenly redistribute N items across [1..N] * 1_000_000
    public static double[] Rebalance(int count) =>
        Enumerable.Range(1, count).Select(i => (double)i * 1_000_000.0).ToArray();
}
```

**Thresholds (from UI-SPEC, D-35):**
- Trigger rebalance when any position in a stage/pipeline `< 1e-9` or `> 1e15`
- Backend rebalances the full list, returns all updated positions in the same response
- Frontend applies returned positions to query cache — no extra fetch needed

**Initial positions:**
- First card in a stage: `position = 1_000_000`
- Second card: `position = 2_000_000` (or midpoint if there is an existing card above/below)

[ASSUMED — midpoint algorithm is standard; thresholds confirmed by UI-SPEC D-35]

---

### Pattern 6: Append-Only Activity Log

**What:** Every card mutation appends a row to `activity_logs`. Never UPDATE or DELETE.

**Schema:**
```sql
CREATE TABLE activity_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    card_id UUID NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
    actor_id UUID NOT NULL REFERENCES users(id),
    action VARCHAR(50) NOT NULL,  -- 'created' | 'moved' | 'edited' | 'archived'
    payload JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_activity_logs_card ON activity_logs(card_id, created_at);
```

**Payload examples:**
```json
// action = 'moved'
{ "from_stage": "Prospecção", "to_stage": "Proposta" }

// action = 'edited'
{ "field": "monetary_value", "old_value": 1000, "new_value": 1500 }
```

**C# write pattern (called from every card-mutating endpoint):**
```csharp
// ActivityLogService or inline in endpoint
private static async Task LogActivity(AppDbContext db, Guid tenantId, Guid cardId,
    Guid actorId, string action, object payload)
{
    db.ActivityLogs.Add(new ActivityLog
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        CardId = cardId,
        ActorId = actorId,
        Action = action,
        Payload = JsonSerializer.Serialize(payload),
        CreatedAt = DateTimeOffset.UtcNow,
    });
    // Caller calls db.SaveChangesAsync() — do not double-save
}
```

[ASSUMED — pattern is standard PostgreSQL append-only log; JSONB payload is well-established]

---

### Pattern 7: shadcn Sheet Side Panel

**What:** Right-anchored slide-in panel for card detail. Triggered by card click, driven by URL `?card={id}`.

```typescript
// Source: ui.shadcn.com/docs/components/sheet
import {
  Sheet, SheetContent, SheetHeader, SheetTitle
} from "@/components/ui/sheet"

// URL-driven: open when searchParams has ?card=
const searchParams = useSearchParams()
const openCardId = searchParams.get("card")
const router = useRouter()

function closePanel() {
  // Remove ?card= param while preserving other params
  const params = new URLSearchParams(searchParams.toString())
  params.delete("card")
  router.replace(`?${params.toString()}`)
}

<Sheet open={!!openCardId} onOpenChange={(open) => { if (!open) closePanel() }}>
  <SheetContent side="right" className="w-[480px] sm:w-[480px]">
    <SheetHeader>
      <SheetTitle>{card?.title}</SheetTitle>
    </SheetHeader>
    {/* Card detail fields, activity log */}
  </SheetContent>
</Sheet>
```

[VERIFIED: ui.shadcn.com/docs/components/sheet — side prop accepts "top"|"right"|"bottom"|"left"]

---

### Pattern 8: Stage-Age Calculation (client-side)

**What:** Compute days/hours since `stage_entered_at` for display in card preview badge and side panel.

```typescript
// Pure client calculation — no server roundtrip needed
function getStageAge(stageEnteredAt: string): { label: string; level: "neutral" | "warning" | "critical" } {
  const enteredDate = new Date(stageEnteredAt)
  const now = new Date()
  const diffMs = now.getTime() - enteredDate.getTime()
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))
  const diffHours = Math.floor(diffMs / (1000 * 60 * 60))

  const label = diffDays >= 1 ? `${diffDays}d` : `${diffHours}h`
  const level = diffDays > 30 ? "critical" : diffDays >= 7 ? "warning" : "neutral"
  return { label, level }
}
```

Badge colors (from UI-SPEC):
- `< 7 days`: `bg-slate-100 text-slate-500`
- `7-30 days`: `bg-amber-100 text-amber-700`
- `> 30 days`: `bg-red-100 text-red-600`

---

### Anti-Patterns to Avoid

- **`react-beautiful-dnd`:** Archived by Atlassian 2023; no React 18 strict mode support. Forbidden by CLAUDE.md.
- **`SortableContext` without `DragOverlay`:** Causes ID collision during scroll for sortable lists — the dragged item's ID exists in two places (source list + overlay). Always use `DragOverlay`. [CITED: dndkit.com/presets/sortable]
- **`closestCenter` for Kanban:** Misfires on parent column droppable vs. card droppables in nested containers. Use `closestCorners`. [CITED: dndkit.com/api-documentation/context-provider/collision-detection-algorithms]
- **`context.client` in TanStack Query v5 `onMutate`:** This is TanStack DB API, not TanStack Query. Use `const queryClient = useQueryClient()` closure.
- **`IgnoreQueryFilters()` on card queries:** Forbidden by CLAUDE.md and Phase 1 architectural decision. Archived cards MUST use the global filter (`archived_at == null` in `HasQueryFilter`), not manual WHERE.
- **Resetting `stage_entered_at` in the frontend:** Frontend MUST NOT set `stage_entered_at`. The backend sets it on every `PATCH /cards/{id}/move` call with `DateTimeOffset.UtcNow`.
- **Calling `db.SaveChangesAsync()` inside `LogActivity`:** The activity log helper must be called before `SaveChangesAsync` in the endpoint — not double-saving.
- **`items` prop of `SortableContext` out of order:** Must match the render order exactly. Using a different sort key for the `items` array than for the rendered list causes items to jump.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Drag-and-drop with touch + keyboard | Custom pointer events | `@dnd-kit/core` + `@dnd-kit/sortable` | Touch, keyboard, scroll containers, screen readers — 100+ edge cases |
| Ghost/drag overlay rendering | Cloned DOM node at cursor | `DragOverlay` component | Avoids ID collision, enables portal rendering outside scroll container |
| Collision detection (which droppable am I over?) | Bounding box math | `closestCorners` built-in | Handles nested containers, partial overlap correctly |
| Side panel animation + accessibility | CSS transitions + focus trap | shadcn `Sheet` (Radix Dialog) | Focus management, Escape key, aria-modal, scroll lock handled |
| Inline popover (pipeline/stage name input) | Custom absolutely positioned div | shadcn `Popover` (Radix) | Positioning engine, outside click dismiss, keyboard navigation |
| Activity log JSONB queries | Custom text column | PostgreSQL JSONB | Indexable, filterable in future without schema change |
| Fractional position midpoint | Integer re-ordering (shift all) | `(prev + next) / 2` midpoint | O(1) single-row update per move vs O(N) shift |

**Key insight:** DnD from scratch takes 2-3x longer than implementing features — touch events, scroll containers, accessibility, and keyboard navigation are non-trivial. dnd-kit solves all of them.

---

## Common Pitfalls

### Pitfall 1: `SortableContext` items array out of sync with render order

**What goes wrong:** Items jump to wrong positions after a drag. Sort appears reversed or scrambled.
**Why it happens:** dnd-kit uses the `items` array to compute projected sort order. If `items` is `[id3, id1, id2]` but the UI renders them in position order `[id1, id2, id3]`, the collision math uses the wrong sequence.
**How to avoid:** Sort by the same `position` field used for rendering before passing to both `SortableContext` and the column renderer.
**Warning signs:** Cards "jump" on drop. Drag produces incorrect sorted result.

### Pitfall 2: Forgetting `DragOverlay` causes ID collision

**What goes wrong:** Console error "Duplicate droppable" or card disappears during drag in scrollable columns.
**Why it happens:** Without `DragOverlay`, the dragged card stays in the DOM at its original position while dnd-kit also creates a drag proxy. Both have the same ID.
**How to avoid:** Always render an `<DragOverlay>` with a presentational clone (not the `useSortable` card). In `onDragStart`, save the active card to state; clear in `onDragEnd`.
**Warning signs:** Error in console about duplicate IDs; card disappears at scroll boundaries.

### Pitfall 3: `onMutate` not cancelling in-flight queries — race condition

**What goes wrong:** Optimistic update is overwritten by a stale server response arriving after `setQueryData`.
**Why it happens:** A background refetch triggered before the mutation returns stale data and clobbers the optimistic state.
**How to avoid:** Always `await queryClient.cancelQueries({ queryKey: ["board", pipelineId] })` as the FIRST line in `onMutate`.
**Warning signs:** Card snaps back to original position briefly then jumps to correct position.

### Pitfall 4: `stage_entered_at` not reset on card move

**What goes wrong:** Stage-age badge shows incorrect time — continues from when card was first created, not from when it entered the current stage.
**Why it happens:** Developer updates `stage_id` on card move but forgets to set `stage_entered_at = DateTimeOffset.UtcNow`.
**How to avoid:** `PATCH /cards/{id}/move` endpoint MUST always update BOTH `StageId` AND `StageEnteredAt` atomically.
**Warning signs:** "23d in stage" shown on a card that just moved 1 minute ago.

### Pitfall 5: EF Core global filter not including `archived_at == null`

**What goes wrong:** Archived cards appear on the board.
**Why it happens:** Updating `HasQueryFilter` for `Card` to add `archived_at` check after the fact, or forgetting to chain it with the tenant filter.
**How to avoid:** Phase 2 migration must update `HasQueryFilter` to: `c => c.TenantId == _tenantId && c.ArchivedAt == null`. The filter is AND-chained in a single lambda.
**Warning signs:** Archived cards still visible on board. `db.Cards.CountAsync()` count does not decrease after archive.

### Pitfall 6: Cascade delete not configured — delete pipeline leaves orphan stages/cards

**What goes wrong:** `DELETE /pipelines/{id}` succeeds but stages and cards remain in DB, causing FK constraint errors on future queries.
**Why it happens:** EF Core does not auto-configure cascade if the relationship is not explicitly modeled.
**How to avoid:** Configure `OnDelete(DeleteBehavior.Cascade)` in `OnModelCreating` for: Stage → Pipeline, Card → Stage, Card → Pipeline, ActivityLog → Card.
**Warning signs:** `DELETE pipeline` returns 200 but subsequent board loads return FK errors.

### Pitfall 7: Activity log double-save or missing save

**What goes wrong:** Activity log entry not written, or written twice.
**Why it happens:** `LogActivity` adds to `db.ActivityLogs` but the helper calls `SaveChangesAsync` separately from the endpoint, causing either double-save or the helper's changes being included in a wrong transaction.
**How to avoid:** `LogActivity` is a helper that ONLY does `db.ActivityLogs.Add(...)`. The calling endpoint adds all changes (card mutation + log entry) then calls `SaveChangesAsync` ONCE.

### Pitfall 8: `PointerSensor` without `activationConstraint` fires on every click

**What goes wrong:** Clicking a card to open the detail panel triggers a drag operation instead. `onDragEnd` fires with no movement.
**Why it happens:** Without `activationConstraint`, any pointer down + up is treated as a drag.
**How to avoid:** Always set `activationConstraint: { distance: 4 }` on `PointerSensor`. The 4px threshold means the user must move 4 pixels before drag activates — click-sized movements never trigger DnD.
**Warning signs:** Card detail panel never opens; every click fires `onDragEnd`.

---

## Code Examples

### Board data TypeScript types
```typescript
// Extend frontend/src/types/api.ts
export interface Pipeline {
  id: string
  name: string
  position: number
  createdAt: string
}

export interface Stage {
  id: string
  pipelineId: string
  name: string
  position: number
  cardCount: number         // PIPE-05 aggregate
  monetarySum: number       // PIPE-05 aggregate
}

export interface Card {
  id: string
  stageId: string
  pipelineId: string
  title: string
  description: string | null
  monetaryValue: number | null
  assigneeId: string | null
  closeDate: string | null
  position: number
  stageEnteredAt: string
  createdAt: string
}

export interface BoardData {
  pipeline: Pipeline
  stages: (Stage & { cards: Card[] })[]
}

export interface ActivityLog {
  id: string
  action: "created" | "moved" | "edited" | "archived"
  payload: Record<string, unknown>
  actorName: string
  createdAt: string
}
```

### Zustand store extensions
```typescript
// Extend frontend/src/store/ui-store.ts
interface UIState {
  // Phase 1 (existing)
  activeWorkspaceId: string | null
  setActiveWorkspace: (id: string | null) => void

  // Phase 2 additions
  activePipelineId: string | null
  setActivePipelineId: (id: string | null) => void

  sidebarCollapsed: boolean
  setSidebarCollapsed: (collapsed: boolean) => void

  draggingCardId: string | null
  setDraggingCardId: (id: string | null) => void
}
```

Note: `sidebarCollapsed` MUST be persisted to `localStorage` with key `nodefy_sidebar_collapsed` (from UI-SPEC Interaction Contracts D-03).

### useSortable card component pattern
```typescript
// Source: dndkit.com/presets/sortable
import { useSortable } from "@dnd-kit/sortable"
import { CSS } from "@dnd-kit/utilities"

function KanbanCard({ card, stageId }: { card: Card; stageId: string }) {
  const {
    attributes, listeners, setNodeRef,
    transform, transition, isDragging,
  } = useSortable({
    id: card.id,
    data: { card, stageId },  // pass stageId for cross-column detection in onDragOver
  })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.4 : 1,  // source card fades (ghost stays in column)
  }

  return (
    <div ref={setNodeRef} style={style} {...attributes} {...listeners}
      className="cursor-grab active:cursor-grabbing">
      {/* card content */}
    </div>
  )
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `react-beautiful-dnd` | `@dnd-kit/*` | 2023 (rbd archived) | dnd-kit: touch support, React 18 strict mode, no jQuery dependency |
| Integer position ordering (shift all on reorder) | Fractional indexing (DOUBLE PRECISION midpoint) | ~2020 (adopted by Notion, Linear) | O(1) single-row update per move |
| Separate `/cards` fetch per stage | Board load endpoint with aggregates | Established pattern | Eliminates N+1 HTTP calls on board mount |
| TanStack Query v4 `onMutate` with separate variables | TanStack Query v5 — same API, improved TypeScript inference | 2023 | v5 types are stricter; `context` return type inferred from `onMutate` return |
| Modal for card detail (blocks board view) | Side panel (Sheet) — board stays visible | CRM UX standard (Salesforce, HubSpot, Pipefy) | Users can reference board while editing card |

**Deprecated/outdated:**
- `react-beautiful-dnd`: Archived, no React 18 strict mode support. Forbidden in CLAUDE.md.
- `NextAuth.js v4`: Maintenance-only. Using Auth.js v5 (`next-auth@beta`) from Phase 1.
- `@dnd-kit/sortable` `arrayMove` for cross-container: Only handles within-container reorder. Cross-container requires manual array manipulation in `onDragOver`.

---

## Runtime State Inventory

**Step 2.5: SKIPPED** — Phase 2 is a greenfield phase (new entities, new endpoints, new components). No rename/refactor/migration of existing string values. The `cards` table is a stub with no user data yet (Phase 1 stub was created before any card creation capability existed).

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Docker | PostgreSQL (db), API container | Yes | 29.3.0 | — |
| .NET SDK 9.x | EF Core migrations, backend build | Yes | 9.0.202 (+ 9.0.116) | — |
| .NET Runtime 9.x | Backend execution | Yes | 9.0.15 | — |
| Node.js | Frontend build, npm install | Yes | 22.11.0 | — |
| npm | Package installation | Yes | (bundled with Node 22) | — |

**All required tools available. No blocking gaps.**

---

## Validation Architecture

### Test Framework (Backend — xUnit + TestContainers)

| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.2 + FluentAssertions 6.12.2 |
| Config file | `Nodefy.Tests/Nodefy.Tests.csproj` |
| Fixture | `PostgresFixture` (TestContainers PostgreSQL 4.11.0) + `ApiFactory` (WebApplicationFactory) |
| Quick run command | `cd api && dotnet test --filter "Category=Phase2" --no-build` |
| Full suite command | `cd api && dotnet test` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Test File | Notes |
|--------|----------|-----------|-----------|-------|
| PIPE-01 | POST /workspaces/{id}/pipelines creates pipeline | Integration | `PipelineTests.cs` | Wave 0 gap |
| PIPE-02 | PATCH + DELETE pipeline | Integration | `PipelineTests.cs` | Wave 0 gap |
| PIPE-03 | Stage CRUD | Integration | `StageTests.cs` | Wave 0 gap |
| PIPE-04 | Stage reorder — position updates correctly | Integration | `StageTests.cs` | Wave 0 gap |
| PIPE-05 | Board load returns correct count + sum per stage | Integration | `BoardTests.cs` | Wave 0 gap |
| CARD-01 | POST card — returns 201, card in DB | Integration | `CardTests.cs` | Wave 0 gap |
| CARD-02 | PATCH card — fields updated | Integration | `CardTests.cs` | Wave 0 gap |
| CARD-03 | Archive card — excluded from board query | Integration | `CardTests.cs` | Wave 0 gap |
| CARD-04 | PATCH move — stage_id + stage_entered_at + position updated | Integration | `CardTests.cs` | Wave 0 gap |
| CARD-05 | stage_entered_at resets on move (verified via CARD-04 test) | Integration | `CardTests.cs` | Covered by CARD-04 test |
| CARD-06 | Activity log appended on create/move/edit | Integration | `ActivityLogTests.cs` | Wave 0 gap |
| Tenant isolation | Card query filtered by tenant_id | Integration | extend `TenantIsolationTests.cs` | Extend existing Phase 1 file |
| Fractional index rebalance | Rebalance triggered when position < 1e-9 | Unit | `FractionalIndexTests.cs` | Wave 0 gap |

**Pattern:** All integration tests use the Phase 1 `PostgresFixture` + `ApiFactory` pattern (TestContainers PostgreSQL, `WebApplicationFactory<Program>`, `TestAuthHandler` via `X-Test-*` headers). Follow exactly the pattern from `WorkspaceTests.cs`.

### Wave 0 Gaps (test infrastructure to create before implementation)

- [ ] `Nodefy.Tests/Integration/PipelineTests.cs` — covers PIPE-01, PIPE-02
- [ ] `Nodefy.Tests/Integration/StageTests.cs` — covers PIPE-03, PIPE-04
- [ ] `Nodefy.Tests/Integration/BoardTests.cs` — covers PIPE-05
- [ ] `Nodefy.Tests/Integration/CardTests.cs` — covers CARD-01, CARD-02, CARD-03, CARD-04, CARD-05
- [ ] `Nodefy.Tests/Integration/ActivityLogTests.cs` — covers CARD-06
- [ ] `Nodefy.Tests/Unit/FractionalIndexTests.cs` — covers rebalance threshold logic

*(No new test framework install needed — xUnit + TestContainers already in `Nodefy.Tests.csproj`)*

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes (carry-forward) | Auth.js v5 session + JWT on API — established Phase 1 |
| V3 Session Management | yes (carry-forward) | HttpOnly cookie — established Phase 1 |
| V4 Access Control | yes — admin-only CRUD | Role check from `WorkspaceMember.Role` on every write; frontend gates are UI only |
| V5 Input Validation | yes | Pipeline/stage/card name length limits; monetary value range check (non-negative) |
| V6 Cryptography | no new crypto | No new crypto in Phase 2 |

### Phase 2 Threat Patterns

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Non-admin creates pipeline | Elevation of Privilege | Backend checks `WorkspaceMember.Role == "admin"` before INSERT — frontend gate alone is insufficient |
| Cross-tenant card access via direct PATCH /cards/{id}/move | Information Disclosure | EF Core global query filter (`TenantId == _tenantId`) prevents cross-tenant access at DB layer; RLS is second layer |
| Card move to stage in different pipeline | Tampering | Backend validates `targetStageId` belongs to same `pipelineId` (and same tenant) before updating |
| Activity log bypass (direct DB write) | Repudiation | Activity log writes are co-located with card mutations in same `SaveChangesAsync` call — no separate write path |
| Archive-restored card via direct DB | Tampering | No restore endpoint in Phase 2; `archived_at` exclusion is enforced by global filter |
| Monetary value injection | Tampering | Input: `decimal?` type in C# (not string); validate `>= 0` server-side |

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Fractional index midpoint `(prev + next) / 2` is the implementation — no specific library required | Pattern 5 | Low risk — algorithm is universally correct; only thresholds could differ from UI-SPEC |
| A2 | Activity log `payload` stored as JSONB string in C# entity using `JsonSerializer.Serialize` | Pattern 6 | Medium — if EF Core JSON column feature used instead, mapping config differs |
| A3 | Initial position for first card = 1,000,000; increment by 1,000,000 for subsequent appends | Pattern 5 | Low — specific initial value doesn't matter as long as there's ample room on both sides |
| A4 | `LogActivity` helper added to endpoints — not a separate service | Pattern 6 | Low — either works; service is cleaner but inline is consistent with existing endpoint pattern |

---

## Open Questions (RESOLVED)

1. **Frontend API client for mutations (client-side fetches)** (RESOLVED)
   - What we know: `apiFetch` in `src/lib/api.ts` uses `auth()` which is a server-side Next.js call and cannot run in client components.
   - What's unclear: Phase 2 board mutations (card move, rename, create) fire from Client Components. Need a client-side fetch utility that includes the JWT token obtained from Auth.js session.
   - Recommendation: Create `src/lib/client-api.ts` that calls `fetch` with a token obtained via `getSession()` (Auth.js v5 client-side) or store the JWT in a session cookie readable by client JS. Research needed before Plan 2.3 implementation.
   - **Resolution:** Route Handler proxy pattern. Client Components call `/api/*/route.ts` Next.js Route Handlers via plain `fetch`. Route Handlers call the upstream .NET API using `apiFetch` (server-side Auth.js session). No client-side token exposure needed. Implemented in Plan 02-03 Task 1.

2. **Currency formatting locale** (RESOLVED)
   - What we know: `currency` field on workspace is `BRL|USD|EUR`; column aggregate format is `{N} cards · R$ 0,00`.
   - What's unclear: Exact formatting implementation — `Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })` or a simpler approach. STATE.md lists this as an open question.
   - Recommendation: Use `Intl.NumberFormat` with workspace currency. For BRL: `'pt-BR'` locale. For USD/EUR: `'en-US'`/`'de-DE'`. No external library needed.
   - **Resolution:** `new Intl.NumberFormat('pt-BR', { style: 'currency', currency: workspace.currency }).format(value)` in `KanbanColumn`. Locale mapped per currency: BRL→`pt-BR`, USD→`en-US`, EUR→`de-DE`. No library install required.

3. **Board route structure: `/workspace/[id]/pipeline/[pipelineId]` or query param?** (RESOLVED)
   - What we know: CONTEXT.md implies URL routing between pipelines. UI-SPEC references `router.push` to a pipeline route.
   - What's unclear: Exact Next.js route structure is not locked. Could be `/workspace/[id]/pipeline/[pipelineId]` (nested folder) or `/workspace/[id]?pipeline=[id]` (query param).
   - Recommendation: Use `/workspace/[id]/pipeline/[pipelineId]` — cleaner deep links, easier to cache per-pipeline board queries in TanStack Query by pipelineId.
   - **Resolution:** `/workspace/[id]/pipeline/[pipelineId]` nested folder route. Implemented as `frontend/src/app/workspace/[id]/pipeline/[pipelineId]/page.tsx` in Plan 02-03 Task 2.

---

## Sources

### Primary (HIGH confidence)
- `dndkit.com/presets/sortable` — SortableContext, useSortable, items ordering requirement, DragOverlay
- `dndkit.com/extend/sensors` — PointerSensor activationConstraint distance
- `dndkit.com/api-documentation/context-provider/collision-detection-algorithms` — closestCorners vs closestCenter for nested containers
- `tanstack.com/query/v5/docs/framework/react/guides/optimistic-updates` — onMutate/onError/onSettled pattern
- `ui.shadcn.com/docs/components/sheet` — Sheet component, side prop, SheetContent API
- `learn.microsoft.com/en-us/ef/core/querying/filters` — HasQueryFilter, IgnoreQueryFilters
- `learn.microsoft.com/en-us/ef/core/querying/complex-query-operators` — GroupBy + aggregate translation

### Secondary (MEDIUM confidence)
- `blog.logrocket.com/build-kanban-board-dnd-kit-react/` — Practical Kanban DnD implementation
- `hollos.dev/blog/fractional-indexing-a-solution-to-sorting/` — Fractional indexing precision analysis (53-54 halvings)
- `github.com/clauderic/dnd-kit/discussions/476` — Click vs drag disambiguation
- `amarozka.dev/ef-core-global-filter-advanced-features/` — Multiple HasQueryFilter predicates

### Tertiary (LOW confidence / ASSUMED)
- Fractional index initial value = 1,000,000 [ASSUMED — common convention]
- LogActivity as inline helper vs service [ASSUMED — matches Phase 1 endpoint style]

---

## Metadata

**Confidence breakdown:**
- Standard stack (dnd-kit, TanStack Query, EF Core): HIGH — versions verified from npm registry and csproj; APIs from official docs
- Architecture patterns (DnD orchestration, optimistic updates): HIGH — verified from official dnd-kit + TanStack Query docs
- Fractional indexing: MEDIUM — algorithm is universal; thresholds locked in UI-SPEC; initial values assumed
- Activity log pattern: MEDIUM — well-established PostgreSQL pattern; JSONB approach is assumed but low-risk
- Client-side API fetch utility: MEDIUM — open question identified; resolution needed before Plan 2.3

**Research date:** 2026-04-17
**Valid until:** 2026-05-17 (stable libraries; dnd-kit v6.3.1 and @dnd-kit/sortable v10 have been stable since mid-2024)
