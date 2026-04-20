---
phase: 02-core-product
plan: "04"
subsystem: frontend
tags: [gap-closure, server-component, client-component, pipeline, empty-state]
dependency_graph:
  requires: [02-03-PLAN.md]
  provides: [workspace-home-functional, first-pipeline-creation-flow]
  affects: [frontend/src/app/workspace/[id]/page.tsx, frontend/src/components/FirstPipelineForm.tsx]
tech_stack:
  added: []
  patterns:
    - RSC parallel apiFetch for pipelines + workspaces
    - Direct fetch in Client Component for empty-state POST (no TanStack Query)
    - Layout-owned auth-guard; page discovers role via second GET /workspaces call
key_files:
  created:
    - frontend/src/components/FirstPipelineForm.tsx
  modified:
    - frontend/src/app/workspace/[id]/page.tsx
decisions:
  - Direct fetch in FirstPipelineForm instead of usePipelines hook — empty-state is a one-shot action; no cache needed; avoids QueryClient dependency on a nearly-static path
  - Layout handles auth-guard; page discovers role via second apiFetch<Workspace[]>("/workspaces") — keeps page self-contained and consistent with pipeline/[id]/page.tsx pattern
  - redirect() placed outside try/catch — Next.js redirect() throws NEXT_REDIRECT internally; catching it would suppress navigation
metrics:
  duration_seconds: 120
  completed_date: "2026-04-20"
  tasks_completed: 3
  tasks_total: 3
  files_changed: 2
---

# Phase 02 Plan 04: Gap Closure — Workspace Home Page Summary

**One-liner:** RSC workspace home that redirects to existing pipeline or renders a functional "create first pipeline" form, closing UAT test 2 gap.

---

## What Was Built

The stub page at `/workspace/[id]` (a disabled button with "Disponível na próxima fase" text) was replaced with a fully functional Server Component and a new Client Component:

**`FirstPipelineForm.tsx`** — Client Component that:
- Renders an `<Input>` + `<Button>` form for the pipeline name
- Validates non-empty name before POSTing to `/api/workspaces/[id]/pipelines`
- Shows `disabled` button with "Criando..." text during the request
- On success: navigates to `/workspace/[id]/pipeline/[newPipelineId]` via `useRouter().push`
- On error: shows `toast.error` with the response text and re-enables the button
- On empty submit: shows `toast.error("Informe um nome para o pipeline")` without sending

**`workspace/[id]/page.tsx`** — Async Server Component that:
- Fetches `Pipeline[]` and `Workspace[]` in parallel via `apiFetch`
- Redirects to `/workspace/select` if workspace ID not found (defensive)
- Redirects to `/workspace/[id]/pipeline/[firstId]` when pipelines already exist (sorted by position)
- For admins with no pipelines: renders card with `FirstPipelineForm`
- For members with no pipelines: renders informational card ("Peça ao administrador...")
- Stub text "Disponível na próxima fase" and `<Button disabled>` fully removed

---

## Gap Closed

UAT Test 2 ("Admin cria o primeiro pipeline a partir da workspace home") was previously blocked because the page was a stub. This plan closes that gap and unblocks UAT tests 3–10 (all of which require at least one pipeline to exist).

---

## Decisions Made

**1. Direct `fetch` in `FirstPipelineForm` instead of `usePipelines` hook**
- The empty-state is a one-shot, rarely-visited path
- No need for TanStack Query cache — after creation the user immediately navigates to the board where the board's own `useQuery` fetches fresh data
- Avoids a circular dependency on `QueryClientProvider` in a nearly-static render path
- Simpler code, easier to reason about

**2. Page discovers `role` via second `apiFetch<Workspace[]>("/workspaces")` call**
- The workspace layout already fetches the workspace list but does not expose it to children (Next.js RSC does not support layout-to-page data passing without a server context)
- The pipeline `[pipelineId]/page.tsx` uses the same double-fetch pattern — consistency maintained
- The double HTTP call is a known, accepted cost documented in the plan

**3. `redirect()` outside of `try/catch`**
- Next.js `redirect()` throws `NEXT_REDIRECT` internally; wrapping in `try/catch` would catch and suppress navigation
- Both redirects (workspace not found; pipelines exist) are at the top level of the function, safe from accidental catch

---

## Deviations from Plan

None — plan executed exactly as written.

---

## Known Stubs

None — all functionality is wired to real API endpoints.

---

## Threat Surface Scan

No new network endpoints, auth paths, or trust boundaries introduced beyond what was documented in the plan's threat model.

| Threat ID | Status |
|-----------|--------|
| T-02-04-01 (Spoofing — no session) | Covered by layout `auth()` |
| T-02-04-02 (Tampering — wrong tenant) | Covered by `apiFetch` JWT + RLS + `workspaces.find` fallback to redirect |
| T-02-04-03 (EoP — member POST) | Accepted; backend enforces admin-only on pipeline creation |
| T-02-04-04 (XSS via pipeline name) | Not applicable; redirect uses `pipeline.id` (UUID), not user input |
| T-02-04-05 (DoS — click flood) | Mitigated by `disabled={submitting}` during in-flight request |

---

## Self-Check

- [x] `frontend/src/components/FirstPipelineForm.tsx` exists
- [x] `frontend/src/app/workspace/[id]/page.tsx` rewritten (no stub text)
- [x] `npx tsc --noEmit` passes with no errors
- [x] `npm run build` completes; `/workspace/[id]` compiled as `ƒ` (dynamic)
- [x] `grep "Disponível na próxima fase"` returns no results
- [x] Task 1 commit: `3b6e781`
- [x] Task 2 commit: `a6bccea`
- [x] Task 3 (human-verify): approved — Cenário A completed successfully (pipeline created, redirected to board)
- [x] UAT test 2 marked `pass` in 02-UAT.md
- [x] UAT tests 3–10 unblocked (changed from `blocked` to `pending`) in 02-UAT.md
- [x] Gap "workspace home entry for first pipeline" marked `closed` in 02-UAT.md

## Self-Check: PASSED
