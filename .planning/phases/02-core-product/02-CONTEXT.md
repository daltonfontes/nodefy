# Phase 2: Core Product - Context

**Gathered:** 2026-04-17
**Status:** Ready for planning

<domain>
## Phase Boundary

This phase delivers a fully functional Kanban board: pipeline CRUD (create, rename, delete), stage management (add, rename, reorder, delete), card CRUD (create, edit, soft-delete), and drag-and-drop between stages with optimistic updates and rollback. Each card displays a stage-age indicator and a chronological activity log. Column headers display live card count and monetary value sum.

No real-time SignalR sync is in scope — that is Phase 3. No search/filtering — also Phase 3.

</domain>

<decisions>
## Implementation Decisions

### Board & Pipeline Navigation
- **D-01:** Sidebar lateral fixa — lista de pipelines na esquerda, sempre visível (salvo quando colapsada). Layout: WorkspaceTopNav no topo, sidebar à esquerda, board kanban à direita.
- **D-02:** Hover sobre um pipeline na sidebar exibe um botão `…` com menu de contexto (Renomear, Excluir) — admin only. Gestão de pipeline não redireciona para página separada.
- **D-03:** Sidebar é collapsible via toggle (botão ou ícone), expandindo o board para tela cheia quando colapsada.
- **D-04:** Botão "＋ New pipeline" no rodapé da sidebar abre inline input ou pequeno dialog para nomear o pipeline.

### Stage Management (inline no board)
- **D-05:** Gestão de estágios acontece diretamente no board, sem página de settings separada.
  - Cabeçalho de cada coluna tem botão `…` → Renomear, Excluir (admin only).
  - Botão `+ Add stage` ao final das colunas (após a última coluna).
  - Reordenamento de estágios: arrastar o cabeçalho da coluna (PIPE-04 com fractional indexing).

### Claude's Discretion
- **Card visual design:** Claude decide quais campos aparecem no card dentro da coluna (quais dos campos title, assignee, monetary_value, close_date, stage-age indicator ficam visíveis no card compacto). Recomendado: título + avatar do responsável + valor monetário + badge de tempo no estágio.
- **Card detail UX:** Claude decide onde abre o detalhe completo do card (side panel, modal, ou página). Recomendado: side panel à direita — mantém o board visível, UX padrão para CRMs.
- **Empty states:** Claude decide o visual das colunas sem cards e do board sem pipelines (o CTA "Create your first pipeline" pós-criação de workspace foi decidido na Phase 1).
- **DnD visual feedback:** Claude decide o ghost card, placeholder de destino e UX de rollback (ex: snap back + toast de erro).
- **Fractional index rebalance threshold:** Claude define o threshold para rebalanceamento (ex: quando `position` < 1e-10 ou > 1e10).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Technology Stack
- `CLAUDE.md` — Stack completo: Next.js App Router, dnd-kit (DnD), TanStack Query (server state + optimistic updates), Zustand (local UI state: DnD in-flight, modals, active pipeline, filters), shadcn/ui + Tailwind CSS v3, .NET 9, EF Core global query filters, PostgreSQL, Docker. Inclui a tabela "What NOT to Use".

### Project Requirements
- `.planning/REQUIREMENTS.md` — Phase 2 covers: PIPE-01 through PIPE-05, CARD-01 through CARD-06.
- `.planning/ROADMAP.md` — Phase 2 success criteria e planned deliverables (3 plans: Pipeline & Stage API, Card API, Board UI).

### Database Schema
- `db/init.sql` — Migration Phase 1: tabela `cards` já existe com `position DOUBLE PRECISION` e `stage_entered_at TIMESTAMPTZ`. Phase 2 ADD COLUMNs: `title`, `description`, `monetary_value`, `pipeline_id`, `stage_id`, `assignee_id`, `close_date`, `archived_at`. Pipelines e stages são tabelas novas em Phase 2.

### Phase 1 Context (decisions that carry forward)
- `.planning/phases/01-foundation/01-CONTEXT.md` — Currency decisions (D-01 through D-05): configurable per workspace (BRL/USD/EUR), default BRL, locked after first card. Light mode only (D-14). WorkspaceTopNav exists as integration point.

### No External Specs
No ADRs beyond what's captured above. All requirements are in REQUIREMENTS.md.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `frontend/src/components/ui/card.tsx` — shadcn Card component: reuse for card preview in columns.
- `frontend/src/components/ui/dialog.tsx` — Modal dialog: available for pipeline creation form or card detail (Claude decides).
- `frontend/src/components/ui/dropdown-menu.tsx` — Context menu: use for `…` menus on pipeline sidebar and stage column headers.
- `frontend/src/components/ui/input.tsx`, `select.tsx`, `label.tsx` — Form primitives for card create/edit form.
- `frontend/src/components/ui/avatar.tsx`, `badge.tsx` — Assignee avatar and status/age badges for card preview.
- `frontend/src/components/WorkspaceTopNav.tsx` — Top navigation bar; sidebar integrates below/beside it.
- `frontend/src/store/ui-store.ts` — Zustand store; extend for: active pipeline ID, sidebar collapsed state, DnD in-flight card state, open card ID.
- `frontend/src/types/api.ts` — API type definitions; extend with Pipeline, Stage, Card, ActivityLog types.

### Established Patterns
- **Server state:** TanStack Query owns all API-fetched data (pipelines, stages, cards). Optimistic updates via `useMutation` + `onMutate`/`onError` rollback.
- **Local UI state:** Zustand for ephemeral state (DnD in-progress, sidebar collapsed, active pipeline, open card panel).
- **Styling:** Tailwind CSS v3 + shadcn/ui — no runtime CSS-in-JS. Light mode only in v1.
- **Auth session:** Available via Auth.js v5 session on all pages. WorkspaceMember role (admin/member) drives UI permission gates.
- **Multi-tenancy:** EF Core global query filters + RLS — never call IgnoreQueryFilters() except in the two designated endpoints.

### Integration Points
- **DB:** Cards stub table already exists. Phase 2 adds columns (ALTER TABLE) + new tables: `pipelines`, `stages`. `stage_entered_at` reset on card move — do NOT forget.
- **Backend:** New endpoint files follow the flat Endpoints/ pattern (PipelineEndpoints.cs, StageEndpoints.cs, CardEndpoints.cs, ActivityLogEndpoints.cs).
- **BoardHub (SignalR):** Already scaffolded in Phase 1. Phase 2 does NOT need to broadcast — that's Phase 3. Do not add hub calls to Phase 2 card move logic (Phase 3 adds them).
- **Frontend routing:** Board lives at `/workspace/[id]/pipeline/[pipelineId]` (or similar). Sidebar links navigate between pipeline routes.

</code_context>

<specifics>
## Specific Ideas

- **Sidebar layout:** The mockup chosen by the user: left sidebar with "PIPELINES" header, active pipeline highlighted with `>`, `[+ New pipeline]` at bottom. Board area shows pipeline name + `[+ Add]` card button in the top right.
- **Stage reorder:** Uses fractional indexing (`position DOUBLE PRECISION` on stages table) — same pattern as card ordering. Only the moved stage updates its `position`.
- **Stage-age indicator (CARD-05):** Shows how long the card has been in its current stage, calculated from `stage_entered_at`. Example: "23 days in this stage". Reset `stage_entered_at = NOW()` every time a card moves to a new stage.
- **Column header aggregates (PIPE-05):** Each column header shows `{count} cards · {sum} BRL` (or workspace currency). Computed server-side via aggregate query on the stage endpoint or as part of the board load.
- **Activity log (CARD-06):** Append-only writes: card created, card moved (from → to), card edited (field changed). Displayed in chronological order in the card detail view.
- **Soft delete (CARD-03):** Cards have `archived_at TIMESTAMPTZ`. Archived cards are excluded from board view via EF Core global filter extension or explicit WHERE in queries.

</specifics>

<deferred>
## Deferred Ideas

- **Card design detail (fields shown on card preview):** Left to Claude's discretion — not discussed.
- **Card detail UX (side panel vs modal vs page):** Left to Claude's discretion — not discussed.
- **Real-time sync (SignalR broadcast on card move):** Phase 3 — REAL-01, REAL-02.
- **Card search and filters:** Phase 3 — DISC-01, DISC-02.
- **Drag-and-drop between pipelines:** Out of scope for v1.

</deferred>

---

*Phase: 02-core-product*
*Context gathered: 2026-04-17*
