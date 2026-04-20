---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
current_phase: 02
status: phase-complete
stopped_at: "Plan 02-04 complete — Phase 02 all 4 plans done. Ready for Phase 03."
last_updated: "2026-04-20T12:00:00.000Z"
progress:
  total_phases: 4
  completed_phases: 2
  total_plans: 9
  completed_plans: 8
  percent: 89
---

# Project State

**Project:** Nodefy
**Current Phase:** 02
**Status:** Phase 02 Complete — All 4 plans done

---

## Phase Progress

| Phase | Status | Plans Done | Plans Total |
|-------|--------|-----------|-------------|
| 1 - Foundation | Complete | 3 | 3 |
| 2 - Core Product | Complete | 4 | 4 |
| 3 - Collaboration & Discovery | Not started | 0 | 2 |
| 4 - Quality & Hardening | Not started | 0 | 1 |

**Overall:** 8/9 plans complete

---

## Current Position

Phase: 02 (core-product) — COMPLETE
Plan: 4 of 4
**Phase:** 2 — Core Product
**Plan:** 02-04 COMPLETE — gap closure (workspace home), human verification approved
**Focus:** Pipeline & Stage API → Card API → Board UI → Workspace Home Gap Closure

Progress: `[██████████] 100%` (Phase 2 complete — all 4 plans done and human-verified)

---

## Project Reference

See: `.planning/PROJECT.md`

**Core value:** Qualquer membro de um workspace consegue ver e mover cards entre estágios do pipeline em tempo real, sem atrito.

**Current focus:** Phase 02 — core-product

---

## Performance Metrics

| Metric | Value |
|--------|-------|
| Phases completed | 0/4 |
| Requirements delivered | 1/28 |
| Plans executed | 1/9 |
| Session count | 1 |

---
| Phase 02 P01 | 705 | 3 tasks | 19 files |
| Phase 02 P02 | 258 | 2 tasks | 4 files |
| Phase 02 P04 | 120 | 2 tasks | 2 files |

## Accumulated Context

### Key Decisions Made

- Fractional indexing (`position DOUBLE PRECISION`) and `stage_entered_at TIMESTAMPTZ` must be in the first migration — not retrofittable
- RLS enforced via PostgreSQL + EF Core global query filters (two-layer enforcement)
- DB role `nodefy_app` must NOT be superuser (RLS is bypassed for superusers)
- SignalR `BoardHub` scaffolded in Phase 1 even if not fully used until Phase 3
- Optimistic DnD with TanStack Query rollback: card state modeled as `idle | moving | error`
- Auth.js v5 (not v4 — v4 is maintenance-only) for Next.js App Router compatibility
- Cookie: HttpOnly + Secure + SameSite=Strict — never localStorage
- .NET 10 SDK creates .slnx format (not .sln) — Dockerfile references .csproj directly and is unaffected
- TenantMiddleware resolves tenant from 4 sources: JWT claim > workspaceId route > X-Tenant-Id header > id route
- IgnoreQueryFilters() allowed only in InviteEndpoints.cs (cross-tenant token lookup) and WorkspaceEndpoints.cs (user multi-workspace query)
- IgnoreQueryFilters used for tenant bootstrap on PATCH/DELETE /pipelines/{id} and /stages/{id} — immediately guarded by IsAdmin check; no data returned without authorization
- AppDbContextFactory with NullTenantService added for EF Core design-time migration tooling — not used at runtime
- init.sql updated to full Phase 2 schema (pipelines, stages, cards, activity_logs) — test containers use init.sql directly, not EF migrations
- shadcn base-nova style replaced with Radix UI default style — base-nova uses @base-ui/react incompatible with locked Tailwind v3 + HSL stack
- Login page wrapped in Suspense boundary — Next.js 16 App Router requires Suspense for useSearchParams() in statically analyzed pages
- tsconfig @/auth and @/auth.config aliases added for root-level auth files — create-next-app @/* only covers src/*
- Member-level auth for all card endpoints (not admin-only) — any workspace member can create/edit/move/archive cards
- Card mutation pattern: IgnoreQueryFilters to bootstrap tenant → SetTenant → member check → mutate → LogActivity → SaveChangesAsync (atomic audit trail)
- Partial PATCH on cards emits one activity log entry per changed field for fine-grained audit trail
- Cross-pipeline guard on PATCH /cards/{id}/move: targetStageId must belong to same pipelineId — returns 400 if mismatch
- Direct fetch in FirstPipelineForm (not usePipelines hook) — empty-state is one-shot; no TanStack Query cache needed; avoids QueryClient dependency on a nearly-static path
- Workspace page discovers role via second apiFetch<Workspace[]>("/workspaces") — consistent with pipeline page pattern; layout cannot expose data to RSC page children

### Architecture Notes

- Multi-tenancy: shared schema + RLS + EF Core global query filters
- Card ordering: fractional indexing (DOUBLE PRECISION) — only moved card updated on reorder
- SignalR groups: `pipeline:{pipelineId}` with tenant verification on join before broadcast
- Frontend state: TanStack Query (server state) + Zustand (local UI state: DnD in-flight, modals, filters)
- OAuth: Register localhost + staging + prod callback URLs in all 3 providers before writing code
- GitHub SSO: requires separate `/user/emails` call for verified email (null email breaks invite flow)

### Open Questions

- Invite flow delivery: email-based (requires SMTP in v1) vs. shareable link in-app?
- Monetary value formatting: BRL (pt-BR) locale confirmed or configurable?
- Fractional index rebalance threshold: define before Phase 2 card move implementation

### Quick Tasks Completed

| # | Description | Date | Commit | Directory |
|---|-------------|------|--------|-----------|
| 260420-g6x | Fix GitHub OAuth AccessDenied: add INTERNAL_API_URL for server-side fetch in signIn callback | 2026-04-20 | 17760be | [260420-g6x-fix-github-oauth-accessdenied-add-intern](.planning/quick/260420-g6x-fix-github-oauth-accessdenied-add-intern/) |

### Blockers

None

### Todos

- [ ] Decide on invite delivery mechanism (email SMTP vs. in-app link) before Plan 1.2
- [ ] Register OAuth callback URLs for all 3 providers before writing auth code
- [ ] Confirm BRL formatting requirement

---

## Session Continuity

**Last session:** 2026-04-20T12:00:00.000Z
**Stopped at:** Plan 02-04 complete — Phase 02 all 4 plans done
**Next action:** Phase 2 is complete. Begin Phase 3 — Collaboration & Discovery (SignalR BoardHub real-time sync + search & filters).

---
*Last updated: 2026-04-20 — quick task 260420-g6x: Fix GitHub OAuth AccessDenied (INTERNAL_API_URL)*
