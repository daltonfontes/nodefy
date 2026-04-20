---
phase: 02-core-product
plan: 03
subsystem: ui
tags: [dnd-kit, tanstack-query, zustand, shadcn, kanban, board, drag-and-drop, next.js, sonner]

# Dependency graph
requires:
  - phase: 02-01
    provides: Pipeline, Stage, Card CRUD backend API endpoints
  - phase: 02-02
    provides: Card move, archive, activity log backend endpoints
provides:
  - Full Kanban board UI with dnd-kit drag-and-drop
  - Collapsible pipeline sidebar with persist via Zustand
  - Card detail Sheet panel driven by ?card= URL param
  - Stage-age badge (neutral/warning/critical) per card
  - Column header aggregates (card count + monetary sum)
  - Optimistic card move with TanStack Query rollback
  - Pipeline and stage CRUD with optimistic mutations
  - Route Handler proxies for all board API operations
  - Pipeline page (RSC) with server-side board prefetch
affects: [03-collaboration, 04-quality]

# Tech tracking
tech-stack:
  added:
    - "@dnd-kit/core@6.3.1"
    - "@dnd-kit/sortable@10.0.0"
    - "@dnd-kit/utilities@3.2.2"
    - "sonner (toast notifications)"
    - "shadcn: scroll-area, sheet, tooltip, popover"
  patterns:
    - "Route Handler proxy: apiFetch (server-only) → /api/* proxies → Client Components"
    - "Optimistic mutation: cancelQueries → snapshot → setQueryData → onError rollback → onSettled invalidate"
    - "DndContext wraps all columns; DragOverlay prevents DOM ID collision"
    - "URL-driven panel: ?card={id} searchParam controls Sheet open state"
    - "Zustand persist: sidebarCollapsed persisted to localStorage via partialize"
    - "Stage-age badge: getStageAge(stageEnteredAt) → label + level (neutral/warning/critical)"

key-files:
  created:
    - frontend/src/types/api.ts (extended with Pipeline, Stage, Card, CardSummary, BoardData, ActivityLog)
    - frontend/src/store/ui-store.ts (extended with activePipelineId, sidebarCollapsed, draggingCardId)
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
  modified:
    - frontend/src/components/Providers.tsx (added Toaster)
    - frontend/package.json (dnd-kit packages + sonner)

key-decisions:
  - "DragOverlay renders CardDragOverlay (not KanbanCard) to prevent DOM ID collision during drag"
  - "closestCorners used (not closestCenter) for better cross-column detection in Kanban layout"
  - "activationConstraint distance:4 prevents accidental drag on card click"
  - "?card={id} URL param drives Sheet panel — allows deep-linking to specific cards"
  - "cancelQueries called FIRST in onMutate before setQueryData to prevent race conditions"
  - "Sonner added for toast error on DnD network failure (not inline alert)"
  - "sidebarCollapsed partialize — only this field persisted, not full store state"

patterns-established:
  - "Board DnD: DndContext > columns (useDroppable) > SortableContext > cards (useSortable)"
  - "Cross-column detection: over.data.current.stageId resolves target; fallback to over.id for column droppables"
  - "Optimistic board update: find card in all stages, remove from source, append to target"
  - "Admin-only UI: role prop gates ... menus; backend enforces authorization independently"

requirements-completed:
  - PIPE-01
  - PIPE-02
  - PIPE-03
  - PIPE-04
  - PIPE-05
  - CARD-01
  - CARD-02
  - CARD-03
  - CARD-04
  - CARD-05
  - CARD-06

# Metrics
duration: ~45min
completed: 2026-04-18
---

# Phase 2 Plan 03: Board UI Summary

**Full Kanban board with dnd-kit drag-and-drop, optimistic TanStack Query rollback, collapsible pipeline sidebar, and Sheet card detail panel driven by ?card= URL param**

## Performance

- **Duration:** ~45 min
- **Started:** 2026-04-18T15:20:22Z
- **Completed:** 2026-04-18 (checkpoint reached — awaiting human verification)
- **Tasks:** 2 of 3 complete (Task 3 is human-verify checkpoint)
- **Files modified:** 26

## Accomplishments

- Installed dnd-kit v6/v10, shadcn sheet/popover/scroll-area/tooltip, sonner toast — build passes clean
- Extended types/api.ts (Pipeline, Stage, Card, CardSummary, BoardData, ActivityLog) and ui-store.ts (sidebarCollapsed persisted, activePipelineId, draggingCardId)
- Created 10 Route Handler proxies (pipelines CRUD, stages CRUD, cards CRUD/move/archive/board) following established apiFetch server-only pattern
- Built full board UI: BoardShell (DndContext + DragOverlay), KanbanColumn (SortableContext + aggregates + admin menu), KanbanCard (useSortable + stage-age badge), CardDragOverlay (presentational)
- CardDetailPanel: Sheet side panel driven by ?card= searchParam, inline title edit, activity log, archive AlertDialog
- PipelineSidebar: collapsible (localStorage persist), pipeline list with active highlight, admin hover menu, + Novo pipeline popover
- Hooks: use-board (optimistic move + rollback), use-pipelines (CRUD), use-stages (CRUD + reorder)
- Pipeline RSC page: server-side board prefetch, passes initialData to BoardShell client boundary

## Task Commits

1. **Task 1: Install packages, extend types/store, create Route Handler proxies** - `88f43e2` (feat)
2. **Task 2: Board components, sidebar, and hooks** - `30e74f2` (feat)
3. **Task 3: Human verification checkpoint** - pending

## Files Created/Modified

- `frontend/src/types/api.ts` - Extended with 6 new interfaces (Pipeline, Stage, Card, CardSummary, BoardData, ActivityLog)
- `frontend/src/store/ui-store.ts` - Extended with activePipelineId, sidebarCollapsed (persisted), draggingCardId
- `frontend/src/hooks/use-board.ts` - useBoard with optimistic moveMutation and cancelQueries-first pattern
- `frontend/src/hooks/use-pipelines.ts` - Pipeline CRUD with optimistic updates
- `frontend/src/hooks/use-stages.ts` - Stage CRUD + reorder with optimistic updates
- `frontend/src/lib/stage-age.ts` - getStageAge(stageEnteredAt) → {label, level}
- `frontend/src/components/board/BoardShell.tsx` - DndContext, DragOverlay, PipelineSidebar layout
- `frontend/src/components/board/KanbanColumn.tsx` - SortableContext per column, header aggregates, admin menu
- `frontend/src/components/board/KanbanCard.tsx` - useSortable + stage-age badge colors
- `frontend/src/components/board/CardDragOverlay.tsx` - Presentational drag ghost (rotate-1, scale-105)
- `frontend/src/components/board/CardDetailPanel.tsx` - Sheet panel, inline edit, activity log, archive
- `frontend/src/components/sidebar/PipelineSidebar.tsx` - Collapsible sidebar, pipeline list, footer
- `frontend/src/components/sidebar/PipelineListItem.tsx` - Active highlight, rename/delete admin menu
- `frontend/src/app/workspace/[id]/pipeline/[pipelineId]/page.tsx` - RSC board page
- `frontend/src/app/api/workspaces/[id]/pipelines/route.ts` - GET/POST pipelines proxy
- `frontend/src/app/api/pipelines/[id]/route.ts` - PATCH/DELETE pipeline proxy
- `frontend/src/app/api/pipelines/[id]/board/route.ts` - GET board data proxy
- `frontend/src/app/api/pipelines/[id]/stages/route.ts` - POST stage proxy
- `frontend/src/app/api/pipelines/[id]/cards/route.ts` - POST card proxy
- `frontend/src/app/api/stages/[id]/route.ts` - PATCH/DELETE stage proxy
- `frontend/src/app/api/stages/[id]/position/route.ts` - PATCH stage reorder proxy
- `frontend/src/app/api/cards/[id]/route.ts` - GET/PATCH card proxy
- `frontend/src/app/api/cards/[id]/move/route.ts` - PATCH card move proxy
- `frontend/src/app/api/cards/[id]/archive/route.ts` - PATCH card archive proxy
- `frontend/src/components/Providers.tsx` - Added Toaster from sonner
- `frontend/package.json` - dnd-kit packages + sonner added

## Decisions Made

- DragOverlay renders `CardDragOverlay` (not `KanbanCard`) — prevents DOM ID collision when card is both in column and in overlay simultaneously
- `closestCorners` used (not `closestCenter`) — better detection for vertical card stacks in side-by-side Kanban columns
- `activationConstraint: { distance: 4 }` prevents accidental drag on card click; click handler guarded by `if (isDragging) return`
- `?card={id}` URL param drives Sheet panel — enables browser back-button close + deep-link sharing
- `cancelQueries` called FIRST in `onMutate` before snapshot and `setQueryData` — prevents race condition with in-flight queries overwriting optimistic state
- Sonner added for DnD error toast — better UX than inline Alert component; added to global Providers
- `partialize: (s) => ({ sidebarCollapsed: s.sidebarCollapsed })` — only this field persisted, not activePipelineId or draggingCardId

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Added sonner toast library for DnD error feedback**
- **Found during:** Task 2 (BoardShell implementation)
- **Issue:** Plan referenced `toast.error(...)` from sonner but package not in package.json; no toast = no error feedback on DnD failure (required by must_haves)
- **Fix:** `npm install sonner` + added `<Toaster>` to Providers.tsx
- **Files modified:** frontend/package.json, frontend/src/components/Providers.tsx
- **Verification:** Build passes; toast import resolves correctly
- **Committed in:** `30e74f2` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 missing critical dependency)
**Impact on plan:** Essential for DnD error feedback per must_haves. No scope creep.

## Issues Encountered

None beyond the sonner package deviation above.

## Known Stubs

None — all data flows from the backend API via Route Handler proxies. No hardcoded placeholder values in rendered UI. Activity log fetches from `/api/cards/{id}/activity` (endpoint from Phase 02-02); if activity endpoint not yet implemented, the log section simply renders empty (graceful degradation).

## Threat Flags

No new threat surface beyond what is documented in the plan's threat_model. All Route Handler proxies follow the established apiFetch server-only pattern (T-02-03-05 mitigation confirmed). No apiFetch imports found in any client component or hook.

## User Setup Required

None — no external service configuration required beyond what was set up in Phase 1.

## Next Phase Readiness

- Task 3 (human verification checkpoint) must be approved before plan is complete
- After approval: Phase 3 (Collaboration & Discovery) can begin — SignalR real-time, comments, filters, search
- All board API proxies in place; Phase 3 can add comment endpoints following the same proxy pattern

## Self-Check

- [x] `frontend/src/hooks/use-board.ts` exists with `cancelQueries`, `previousBoard`, `setQueryData`
- [x] `frontend/src/components/board/BoardShell.tsx` exists with `DndContext`, `closestCorners`, `DragOverlay` (5 refs)
- [x] `frontend/src/components/board/KanbanCard.tsx` exists with `useSortable`, stage-age badge colors
- [x] `frontend/src/components/board/CardDetailPanel.tsx` exists with `SheetContent`, `searchParams.get("card")`
- [x] `frontend/src/components/sidebar/PipelineSidebar.tsx` exists with `sidebarCollapsed` (9 refs)
- [x] `frontend/src/store/ui-store.ts` has `persist`, `nodefy_sidebar_collapsed`
- [x] 0 `apiFetch` imports in components/ or hooks/
- [x] 0 `closestCenter` references in board components
- [x] Commits 88f43e2 (Task 1) and 30e74f2 (Task 2) exist in git log
- [x] `npm run build` exits 0 with 0 TypeScript errors

## Self-Check: PASSED

---
*Phase: 02-core-product*
*Completed: 2026-04-18 (partial — checkpoint pending)*
