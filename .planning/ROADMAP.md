# Roadmap: Nodefy

**Phases:** 4 | **Requirements:** 28 | **Coverage:** 100%

---

## Phase Overview

| # | Phase | Goal | Requirements | Plans |
|---|-------|------|--------------|-------|
| 1 | Foundation | 1/3 | In Progress|  |
| 2 | Core Product | Any workspace member can manage pipelines and move cards on a Kanban board with optimistic drag-and-drop | PIPE-01–05, CARD-01–06 | TBD |
| 3 | Collaboration & Discovery | Card moves and edits broadcast in real time to all members; users can find any card via search and filters | REAL-01–02, DISC-01–02 | TBD |
| 4 | Quality & Hardening | All critical user flows are covered by Playwright E2E tests and the product is shippable | TEST-02 | TBD |

---

## Phases

- [ ] **Phase 1: Foundation** - Multi-tenant auth, workspace management, and DB schema with RLS + fractional indexing bootstrapped
- [ ] **Phase 2: Core Product** - Full Kanban board: pipeline CRUD, card CRUD, drag-and-drop with optimistic updates and stage-age indicator
- [ ] **Phase 3: Collaboration & Discovery** - Real-time SignalR board sync + card search and filters
- [ ] **Phase 4: Quality & Hardening** - Playwright E2E coverage of all critical flows; product ready to ship

---

## Phase Details

### Phase 1: Foundation
**Goal:** Authenticated user can create a workspace and invite members, with multi-tenant isolation enforced from the first migration
**Depends on:** Nothing (first phase)
**Requirements:** AUTH-01, AUTH-02, AUTH-03, AUTH-04, AUTH-05, WORK-01, WORK-02, WORK-03, WORK-04, WORK-05, WORK-06, TEST-01

**Success Criteria** (what must be TRUE):
1. User can log in via GitHub, Google, or Microsoft SSO and stay logged in across browser refreshes (HttpOnly cookie)
2. User can log out from any page and their session is invalidated
3. Authenticated user can create a workspace; all data reads are scoped to that tenant — cross-tenant queries return zero rows
4. Admin can invite a member by email; invitee can accept and access the workspace with the assigned role (admin or member)
5. Admin can view, promote/demote, and remove workspace members

**Plans:** 1/3 plans executed

Plans:
- [x] 01-01-PLAN.md — DB Schema & Docker: PostgreSQL schema with RLS + fractional indexing + stage_entered_at and Docker Compose for db/api/frontend
- [x] 01-02-PLAN.md — Backend Auth & Tenant API: .NET 9 JWT auth, EF Core global query filters + RLS interceptor, workspace + invite + member endpoints, xUnit + Testcontainers TDD scaffold
- [ ] 01-03-PLAN.md — Frontend Auth & Workspace UI: Next.js 16 + Auth.js v5 (with GitHub /user/emails fallback), workspace create/select, invite copy-to-clipboard, member management UI

**UI hint**: yes

---

### Phase 2: Core Product
**Goal:** Any workspace member can manage pipelines and move cards on a fully functional Kanban board with optimistic drag-and-drop
**Depends on:** Phase 1
**Requirements:** PIPE-01, PIPE-02, PIPE-03, PIPE-04, PIPE-05, CARD-01, CARD-02, CARD-03, CARD-04, CARD-05, CARD-06

**Success Criteria** (what must be TRUE):
1. Admin can create, rename, reorder, and delete pipelines; admin can add, rename, reorder, and delete stages within a pipeline
2. Each stage column header displays the live card count and sum of monetary values
3. Member can create a card with title, description, monetary value, assignee, and close date; can edit and soft-delete it
4. Member can drag a card to a different stage; the UI updates immediately (optimistic) and rolls back visually on network failure
5. Each card displays how long it has been in its current stage, and a chronological activity log of all moves and edits

**Plans:**
- [ ] Pipeline & Stage API: CRUD endpoints for pipelines and stages, stage reorder with fractional indexing, column aggregate queries (count + SUM)
- [ ] Card API: CRUD endpoints for cards, soft-delete (archive), card move endpoint updating `position` + `stage_entered_at`, activity log append-only writes
- [ ] Board UI: Kanban board with dnd-kit, optimistic DnD with TanStack Query rollback, card detail side panel, stage-age indicator, activity log display, column totals header

**UI hint**: yes

---

### Phase 3: Collaboration & Discovery
**Goal:** Card moves and edits broadcast to all connected workspace members in real time, and any card is findable via search or filters
**Depends on:** Phase 2
**Requirements:** REAL-01, REAL-02, DISC-01, DISC-02

**Success Criteria** (what must be TRUE):
1. When member A moves a card, member B's board reflects the change within 2 seconds without a page reload
2. When member A creates or updates a card, it appears/updates on member B's board in real time without a reload
3. Member can type in a search box and see cards matching the title within the current pipeline
4. Member can filter the board by assignee, close date range, and monetary value range; filters combine with AND logic

**Plans:**
- [ ] SignalR BoardHub: `pipeline:{pipelineId}` groups with tenant verification on join, broadcast on card move/create/update, frontend `@microsoft/signalr` client integration
- [ ] Search & Filters: Full-text title search endpoint (PostgreSQL `ILIKE` / `tsvector`), filter query parameters (assignee, date, value), Zustand filter state + board re-render

**UI hint**: yes

---

### Phase 4: Quality & Hardening
**Goal:** All critical user flows are covered by automated E2E tests and the product can be confidently shipped
**Depends on:** Phase 3
**Requirements:** TEST-02

**Success Criteria** (what must be TRUE):
1. Playwright suite covers SSO login flow end-to-end and passes in CI
2. Playwright suite covers pipeline creation, card creation, and card drag-and-drop move flows end-to-end
3. All E2E tests pass against the Docker Compose stack with zero flaky failures in three consecutive runs

**Plans:**
- [ ] E2E Test Suite: Playwright setup against Docker Compose, test specs for login SSO + create pipeline + create card + move card, CI integration

---

## Progress Table

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation | 0/3 | Not started | - |
| 2. Core Product | 0/3 | Not started | - |
| 3. Collaboration & Discovery | 0/2 | Not started | - |
| 4. Quality & Hardening | 0/1 | Not started | - |

---

## Coverage Map

| Requirement | Phase | Status |
|-------------|-------|--------|
| AUTH-01 | Phase 1 | Pending |
| AUTH-02 | Phase 1 | Pending |
| AUTH-03 | Phase 1 | Pending |
| AUTH-04 | Phase 1 | Pending |
| AUTH-05 | Phase 1 | Pending |
| WORK-01 | Phase 1 | Pending |
| WORK-02 | Phase 1 | Pending |
| WORK-03 | Phase 1 | Pending |
| WORK-04 | Phase 1 | Pending |
| WORK-05 | Phase 1 | Pending |
| WORK-06 | Phase 1 | Pending |
| TEST-01 | Phase 1 | Pending |
| PIPE-01 | Phase 2 | Pending |
| PIPE-02 | Phase 2 | Pending |
| PIPE-03 | Phase 2 | Pending |
| PIPE-04 | Phase 2 | Pending |
| PIPE-05 | Phase 2 | Pending |
| CARD-01 | Phase 2 | Pending |
| CARD-02 | Phase 2 | Pending |
| CARD-03 | Phase 2 | Pending |
| CARD-04 | Phase 2 | Pending |
| CARD-05 | Phase 2 | Pending |
| CARD-06 | Phase 2 | Pending |
| REAL-01 | Phase 3 | Pending |
| REAL-02 | Phase 3 | Pending |
| DISC-01 | Phase 3 | Pending |
| DISC-02 | Phase 3 | Pending |
| TEST-02 | Phase 4 | Pending |

**Mapped:** 28/28 ✓

---
*Roadmap created: 2026-04-16*
