---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
current_phase: 01
status: executing
stopped_at: "Checkpoint Task 4 (01-03): awaiting human verify — end-to-end SSO + workspace + invite + member flow"
last_updated: "2026-04-17T18:28:05.653Z"
progress:
  total_phases: 4
  completed_phases: 1
  total_plans: 3
  completed_plans: 3
  percent: 100
---

# Project State

**Project:** Nodefy
**Current Phase:** 01
**Status:** Executing Phase 01

---

## Phase Progress

| Phase | Status | Plans Done | Plans Total |
|-------|--------|-----------|-------------|
| 1 - Foundation | In Progress | 3 | 3 |
| 2 - Core Product | Not started | 0 | 3 |
| 3 - Collaboration & Discovery | Not started | 0 | 2 |
| 4 - Quality & Hardening | Not started | 0 | 1 |

**Overall:** 3/9 plans complete

---

## Current Position

Phase: 01 (foundation) — EXECUTING
Plan: 3 of 3
**Phase:** 1 — Foundation
**Plan:** 01-03 tasks 1-3 complete — checkpoint at Task 4 (human verify — end-to-end SSO + workspace + invite + member flow)
**Focus:** DB Schema & Docker → Backend Auth & Tenant API → Frontend Auth & Workspace UI

Progress: `[██████████] 100%` (Phase 1 all plans complete, pending human verify checkpoint)

---

## Project Reference

See: `.planning/PROJECT.md`

**Core value:** Qualquer membro de um workspace consegue ver e mover cards entre estágios do pipeline em tempo real, sem atrito.

**Current focus:** Phase 01 — foundation

---

## Performance Metrics

| Metric | Value |
|--------|-------|
| Phases completed | 0/4 |
| Requirements delivered | 1/28 |
| Plans executed | 1/9 |
| Session count | 1 |

---

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
- shadcn base-nova style replaced with Radix UI default style — base-nova uses @base-ui/react incompatible with locked Tailwind v3 + HSL stack
- Login page wrapped in Suspense boundary — Next.js 16 App Router requires Suspense for useSearchParams() in statically analyzed pages
- tsconfig @/auth and @/auth.config aliases added for root-level auth files — create-next-app @/* only covers src/*

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

### Blockers

None

### Todos

- [ ] Decide on invite delivery mechanism (email SMTP vs. in-app link) before Plan 1.2
- [ ] Register OAuth callback URLs for all 3 providers before writing auth code
- [ ] Confirm BRL formatting requirement

---

## Session Continuity

**Last session:** 2026-04-17T18:27:54.244Z
**Stopped at:** Checkpoint Task 4 (01-03): awaiting human verify — end-to-end SSO + workspace + invite + member flow
**Next action:** Start Docker Desktop, run `docker compose up -d --build`, verify all three services healthy, then complete the 11 end-to-end verification steps in 01-03-PLAN.md Task 4 using two browser windows with two SSO accounts. Type "approved" to complete both pending checkpoints (01-02 Task 4 and 01-03 Task 4).

---
*Last updated: 2026-04-17 after plan 01-01 completion*
