# Phase 1: Foundation - Context

**Gathered:** 2026-04-16
**Status:** Ready for planning

<domain>
## Phase Boundary

This phase delivers the multi-tenant foundation: SSO authentication (GitHub, Google, Microsoft), workspace creation and management, member invite/accept flow, and member list management. It includes the first PostgreSQL migration (with RLS, fractional indexing, and `stage_entered_at`), Docker Compose setup, .NET 9 backend (SSO handlers, EF Core global query filters, workspace/membership API), and Next.js frontend (login page, workspace selector, workspace creation, member management).

No pipeline or card features are in scope — those are Phase 2.

</domain>

<decisions>
## Implementation Decisions

### Monetary Value Locale
- **D-01:** Currency is configurable per workspace — not hardcoded globally
- **D-02:** Supported currencies in v1: BRL, USD, EUR (full ISO list is out of scope for v1)
- **D-03:** Default currency for newly created workspaces: BRL
- **D-04:** Currency is locked after the first card is created in the workspace (backend must enforce this guard on the workspace settings PATCH endpoint)
- **D-05:** Admins change currency via a workspace settings page

### Multi-Workspace Model
- **D-06:** A user can belong to multiple workspaces (as member and/or admin in each)
- **D-07:** Any authenticated user can create multiple workspaces — no "one workspace per user" restriction
- **D-08:** After login, if the user has multiple workspaces, show a workspace selector screen before entering any workspace
- **D-09:** After first login with no workspaces, auto-redirect to the "Create workspace" page — no dead end
- **D-10:** Workspace creation form requires only the workspace name; slug is auto-generated from the name

### Login & First-Run UX
- **D-11:** Login page layout: centered card with product name/logo at top, then three SSO buttons stacked vertically — "Continue with GitHub", "Continue with Google", "Continue with Microsoft"
- **D-12:** SSO auth failure: show error message inline on the login page (not a redirect to a separate error page)
- **D-13:** After workspace creation, user lands on an empty pipeline board with a "Create your first pipeline" CTA — no onboarding wizard
- **D-14:** Light mode only in v1 — no dark mode, no system preference toggle

### Claude's Discretion
- **Invite flow delivery:** Not discussed. Claude decides between email-based SMTP delivery and in-app shareable link. Consider that SMTP adds a mail service dependency to Docker Compose — a shareable link avoids that for v1.
- **Workspace selector UI detail:** Layout and visual treatment of the workspace selector screen (list vs. card grid, last-used highlighting) — Claude decides.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Technology Stack
- `CLAUDE.md` — Full technology stack decisions: Auth.js v5, Next.js App Router, shadcn/ui, TanStack Query, Zustand, @microsoft/signalr, .NET 9, EF Core global query filters, PostgreSQL, Docker. Includes the "What NOT to Use" table.

### Project Requirements
- `.planning/REQUIREMENTS.md` — All v1 requirements with IDs. Phase 1 covers: AUTH-01 through AUTH-05, WORK-01 through WORK-06, TEST-01.
- `.planning/ROADMAP.md` — Phase 1 success criteria and planned deliverables.

### No External Specs Yet
No ADRs or external spec files exist — the project is greenfield. Requirements are fully captured in REQUIREMENTS.md and decisions above.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- None — project is greenfield. No existing components, hooks, or utilities.

### Established Patterns
- None yet — Phase 1 establishes the patterns that subsequent phases will follow.

### Integration Points
- Phase 1 creates the foundation all subsequent phases depend on: DB schema, auth session, workspace context in request pipeline, EF Core DbContext with global query filters, Docker Compose services.

</code_context>

<specifics>
## Specific Ideas

- **SignalR BoardHub scaffolding:** The hub must be scaffolded in Phase 1 even if not fully wired until Phase 3. This is explicitly decided in STATE.md.
- **GitHub SSO:** Requires a separate `/user/emails` API call to get the verified email — the primary email field can be null. This must be handled in the GitHub OAuth handler.
- **DB role constraint:** The `nodefy_app` PostgreSQL role must NOT be a superuser — RLS is bypassed for superusers.
- **Fractional indexing:** `position DOUBLE PRECISION` and `stage_entered_at TIMESTAMPTZ` must be in the first migration — they are not retrofittable without data migration.
- **Currency schema:** The `workspaces` table needs a `currency` column (VARCHAR(3), default 'BRL') and a `currency_locked` boolean (set to true when the first card is created). The backend must guard the currency PATCH with this flag.

</specifics>

<deferred>
## Deferred Ideas

- **Invite flow delivery:** Not decided in this discussion — left to Claude's discretion. If email SMTP is chosen, a mail service (e.g., MailHog for dev, SMTP relay for prod) must be added to Docker Compose.
- **Monetary value locale (full ISO list):** Only BRL/USD/EUR in v1. Full ISO 4217 currency list deferred to v2.
- **Dark mode:** Deferred to v2 — requires a full design pass on the color system.
- **Workspace onboarding wizard:** Deferred to v2 — post-creation UX is empty board + CTA, not a guided checklist.

</deferred>

---

*Phase: 01-foundation*
*Context gathered: 2026-04-16*
