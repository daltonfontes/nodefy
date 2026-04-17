---
phase: "01"
plan: "03"
subsystem: frontend
tags:
  - frontend
  - nextjs
  - auth
  - ui
  - shadcn

dependency_graph:
  requires:
    - dotnet-api-v1
    - workspace-crud-endpoints
    - invite-flow-endpoints
    - member-management-endpoints
    - sso-sync-endpoint
  provides:
    - nextjs-frontend-v1
    - login-page-sso
    - workspace-selector-flow
    - workspace-creation-flow
    - invite-accept-flow
    - member-management-ui
  affects:
    - "Phase 2: Pipeline UI (consumes workspace context, auth session)"

tech_stack:
  added:
    - "Next.js 16.2.4 — App Router, TypeScript, src/ layout"
    - "next-auth@beta (Auth.js v5) — JWT sessions, GitHub/Google/MicrosoftEntraID providers"
    - "@tanstack/react-query@5.99.0 — server state, optimistic mutations"
    - "zustand@5.0.12 — local UI state (activeWorkspaceId)"
    - "@microsoft/signalr@10.0.0 — real-time client (Phase 3)"
    - "jose@5 — HS256 JWT minting for backend Bearer auth"
    - "shadcn/ui (default style, Radix UI primitives) — 13 components"
    - "tailwindcss@3.4.17 — Tailwind v3 (pinned, not v4)"
    - "lucide-react — icon library"
  patterns:
    - "useState(() => new QueryClient()) in Providers.tsx — prevents SSR cross-user cache leak (Pitfall 4)"
    - "auth.config.ts + auth.ts split — Edge-safe middleware uses authConfig only, avoids DB-driver pulls"
    - "Server proxy routes /api/workspaces/proxy, /api/workspaces/[id]/*, /api/invites/[token]/accept — AUTH_SECRET never sent to client"
    - "apiFetch() mints fresh HS256 JWT per server request with tenant_id claim for RLS"
    - "TanStack Query onMutate/onError rollback for optimistic member role toggle and removal"
    - "Suspense boundary wrapping useSearchParams() in LoginPage — required by Next.js 16 App Router"

key_files:
  created:
    - frontend/package.json
    - frontend/tsconfig.json
    - frontend/next.config.ts
    - frontend/tailwind.config.ts
    - frontend/postcss.config.mjs
    - frontend/components.json
    - frontend/.eslintrc.json
    - frontend/.env.local.example
    - frontend/auth.ts
    - frontend/auth.config.ts
    - frontend/proxy.ts
    - frontend/src/app/globals.css
    - frontend/src/app/layout.tsx
    - frontend/src/app/page.tsx
    - frontend/src/app/api/auth/[...nextauth]/route.ts
    - frontend/src/app/(auth)/login/page.tsx
    - frontend/src/app/(auth)/workspace/select/page.tsx
    - frontend/src/app/(auth)/workspace/new/page.tsx
    - frontend/src/app/workspace/[id]/page.tsx
    - frontend/src/app/workspace/[id]/layout.tsx
    - frontend/src/app/workspace/[id]/settings/members/page.tsx
    - frontend/src/app/workspace/[id]/settings/members/MemberTable.tsx
    - frontend/src/app/workspace/[id]/settings/members/invite/page.tsx
    - frontend/src/app/invite/[token]/page.tsx
    - frontend/src/components/Providers.tsx
    - frontend/src/components/Logo.tsx
    - frontend/src/components/SsoButton.tsx
    - frontend/src/components/LogoutButton.tsx
    - frontend/src/components/WorkspaceTopNav.tsx
    - frontend/src/components/ui/button.tsx
    - frontend/src/components/ui/card.tsx
    - frontend/src/components/ui/input.tsx
    - frontend/src/components/ui/label.tsx
    - frontend/src/components/ui/badge.tsx
    - frontend/src/components/ui/avatar.tsx
    - frontend/src/components/ui/separator.tsx
    - frontend/src/components/ui/dialog.tsx
    - frontend/src/components/ui/alert.tsx
    - frontend/src/components/ui/select.tsx
    - frontend/src/components/ui/table.tsx
    - frontend/src/components/ui/dropdown-menu.tsx
    - frontend/src/components/ui/alert-dialog.tsx
    - frontend/src/lib/api.ts
    - frontend/src/lib/api-token.ts
    - frontend/src/lib/slug.ts
    - frontend/src/lib/utils.ts
    - frontend/src/store/ui-store.ts
    - frontend/src/types/api.ts
    - frontend/src/app/api/workspaces/proxy/route.ts
    - frontend/src/app/api/workspaces/[id]/members/route.ts
    - frontend/src/app/api/workspaces/[id]/members/[userId]/route.ts
    - frontend/src/app/api/workspaces/[id]/invites/route.ts
    - frontend/src/app/api/invites/[token]/accept/route.ts
  modified: []

decisions:
  - "shadcn base-nova style (using @base-ui/react) replaced with classic default style (Radix UI primitives) — base-nova requires @base-ui/react which conflicts with the locked Tailwind v3 + HSL CSS variables stack"
  - "Login page wrapped in Suspense boundary — Next.js 16 App Router requires Suspense for useSearchParams() in static pages"
  - "tsconfig paths extended with @/auth and @/auth.config aliases pointing to root-level files — create-next-app @/* alias only covers src/*"
  - "GitHub email fallback uses verified=true guard — only accepts verified emails to prevent spoofing (T-03-01)"

metrics:
  duration_seconds: 3600
  completed_date: "2026-04-17"
  tasks_completed: 4
  tasks_total: 4
  files_created: 52
  files_modified: 0
---

# Phase 01 Plan 03: Frontend Auth & Workspace UI Summary

**One-liner:** Next.js 16 App Router frontend with Auth.js v5 SSO (GitHub /user/emails fallback, Google, Microsoft Entra), workspace selector/creation flow, member management with optimistic mutations, and shareable invite link with copy-to-clipboard.

## What Was Built

### Task 1 — Bootstrap Next.js 16 + shadcn + Auth.js + TanStack Query + Zustand

Scaffolded the full frontend project from scratch (create-next-app refused due to existing Dockerfile):

- `package.json` with all locked versions: Next.js 16.2.4, next-auth@beta, @tanstack/react-query@5.99.0, zustand@5.0.12, @microsoft/signalr@10.0.0, jose@5, tailwindcss@3.4.17 (pinned, not v4)
- `tailwind.config.ts` with `darkMode: "class"` (D-14: light mode default, dark never activated without .dark class on html)
- `components.json` updated to default style (slate, CSS variables)
- 13 shadcn UI components rewritten with Radix UI primitives (shadcn installed base-nova style using @base-ui/react — replaced in full)
- `Providers.tsx` with `useState(() => new QueryClient())` — prevents SSR cross-user cache leak
- `src/lib/slug.ts` — diacritic-stripping slug generator matching backend Slug.cs
- `src/store/ui-store.ts` — Zustand store for local UI state only
- `src/types/api.ts` — TypeScript interfaces matching backend DTOs

**Commit:** `42b5aae`

### Task 2 — Auth.js v5 SSO, middleware, JWT minting, API client, all SSO pages

- `auth.config.ts`: GitHub provider with `/user/emails` fallback + `verified=true` guard (T-03-01), Google, MicrosoftEntraID; `authorized` callback for middleware
- `auth.ts`: `signIn` callback calls `/sso/sync` to get canonical `users.id`; replaces `user.id` with backend UUID
- `proxy.ts`: Edge-safe middleware (`export { auth: proxy }`) protecting all routes except `/login`, `/invite/*`, `/api/auth/*`
- `api-token.ts`: mints HS256 JWT — issuer `nodefy-frontend`, audience `nodefy-api`, 1h expiry, from AUTH_SECRET
- `api.ts`: `apiFetch<T>()` server-side wrapper — mints fresh JWT per call with optional `tenant_id` claim
- Login page: three SSO buttons with exact UI-SPEC copy, inline destructive Alert on error (D-12), Suspense boundary for `useSearchParams()`
- Workspace select: redirects to `/workspace/new` (0 workspaces, D-09), `/workspace/{id}` (1 workspace fast path), card grid (2+, D-08)
- Workspace new: name form with slug preview, `Criar workspace` CTA (D-10, D-13)
- Workspace layout: fetches workspace list, finds matching ID, renders `WorkspaceTopNav`
- Workspace home: empty board with disabled "Crie seu primeiro pipeline" CTA (D-13)
- Invite accept: validates token server-side, shows workspace name + role, redirects unauthenticated to `/login?callbackUrl`

**Commit:** `4b7de3d`

### Task 3 — Member management UI

- `MembersPage`: server-rendered with member count, "Convidar membro" button
- `MemberTable` (client): TanStack Query with `onMutate` optimistic update + `onError` rollback for role toggle and remove; Dialog (not browser confirm) for remove confirmation with exact UI-SPEC copy
- Invite page: email + role select form, generates shareable invite URL with copy-to-clipboard (`navigator.clipboard.writeText`)
- Server proxy routes for all member/invite operations — include `tenantId` so JWT carries `tenant_id` claim unlocking RLS

**Commit:** `3f633a9`

### Task 4 — Checkpoint: Human Verify (APPROVED)

Human operator completed end-to-end verification with two browser windows and two SSO provider accounts. All 11 verification steps passed:

1. Stack started cleanly (`docker compose up -d --build`) — db, api, frontend all healthy
2. GitHub SSO with private email — `/user/emails` fallback returned verified email; HttpOnly cookie confirmed in DevTools
3. Workspace creation — slug preview displayed correctly; redirected to empty board with "Comece criando seu primeiro pipeline" CTA
4. Session persistence — page refresh kept session active
5. Invite generation — shareable URL appeared in read-only input; Copy button icon flipped to checkmark on click
6. Invite accept (second browser/SSO account) — redirected to `/login?callbackUrl`, authenticated, accepted, redirected to workspace
7. Member list — two members listed with correct Admin/Membro badges
8. Role toggle (optimistic) — badge updated immediately; backend confirmed; last-admin guard returned 409 and badge rolled back
9. Remove member — Dialog opened with correct copy; row disappeared on confirm
10. Logout — session cleared; subsequent page load stayed on `/login`
11. D-12 inline error — `/login?error=AccessDenied` rendered destructive Alert inline

## shadcn Components Added

| Component | File |
|-----------|------|
| button | src/components/ui/button.tsx |
| card | src/components/ui/card.tsx |
| input | src/components/ui/input.tsx |
| label | src/components/ui/label.tsx |
| badge | src/components/ui/badge.tsx |
| avatar | src/components/ui/avatar.tsx |
| separator | src/components/ui/separator.tsx |
| dialog | src/components/ui/dialog.tsx |
| alert | src/components/ui/alert.tsx |
| select | src/components/ui/select.tsx |
| table | src/components/ui/table.tsx |
| dropdown-menu | src/components/ui/dropdown-menu.tsx |
| alert-dialog | src/components/ui/alert-dialog.tsx |

## File Counts by Category

| Category | Count |
|----------|-------|
| Pages (app routes) | 9 |
| API route handlers (proxy) | 6 |
| Components (shared) | 7 |
| shadcn UI components | 13 |
| Lib utilities | 4 |
| Store | 1 |
| Types | 1 |
| Config files | 8 |
| Auth files (root) | 3 |
| **Total** | **52** |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] shadcn init generates base-nova style using @base-ui/react instead of default style with Radix UI**
- **Found during:** Task 1 — shadcn@4.3.0 changed default style to "base-nova" which uses @base-ui/react primitives
- **Issue:** Plan specifies default style with Radix UI (`--style default --base-color slate`); the `--style` flag no longer exists in shadcn@4.3.0; base-nova components use incompatible APIs (`ButtonPrimitive.Props` instead of `React.ButtonHTMLAttributes`)
- **Fix:** Manually rewrote all 13 shadcn components with classic Radix UI default-style implementations; updated components.json to `"style": "default"`; removed `@base-ui/react`, `tw-animate-css`, `shadcn` from dependencies
- **Files modified:** All 13 src/components/ui/*.tsx files, package.json, components.json
- **Commit:** Included in `42b5aae`

**2. [Rule 1 - Bug] useSearchParams() in login page crashes build without Suspense boundary**
- **Found during:** Task 2 — `npm run build` failed with "useSearchParams() should be wrapped in a suspense boundary at page /login"
- **Issue:** Next.js 16 App Router requires Suspense for `useSearchParams()` in statically analyzed pages
- **Fix:** Extracted `LoginContent` component containing `useSearchParams()` usage; wrapped in `<Suspense>` in the exported `LoginPage`
- **Files modified:** `src/app/(auth)/login/page.tsx`
- **Commit:** Included in `4b7de3d`

**3. [Rule 1 - Bug] @/* tsconfig alias maps to ./src/* — root-level auth.ts unreachable as @/auth**
- **Found during:** Task 2 — build error "Module not found: Can't resolve '@/auth'"
- **Issue:** create-next-app sets `"@/*": ["./src/*"]` but plan puts auth.ts at repo root `frontend/auth.ts`
- **Fix:** Added explicit path aliases `"@/auth": ["./auth.ts"]` and `"@/auth.config": ["./auth.config.ts"]` to tsconfig.json
- **Files modified:** tsconfig.json
- **Commit:** Included in `4b7de3d`

## Known Stubs

| Stub | File | Reason |
|------|------|--------|
| "Crie seu primeiro pipeline" button is disabled | `src/app/workspace/[id]/page.tsx` | Pipeline feature is Phase 2 |
| `@microsoft/signalr` installed but not wired | `package.json` | SignalR client connection is Phase 3 |

## Threat Surface

All threats from the plan's threat register are mitigated:

| Threat | Mitigation Applied |
|--------|-------------------|
| T-03-01: GitHub null email | auth.config.ts /user/emails fallback with verified=true guard; signIn returns false if still null |
| T-03-02: Auth cookie JS-readable | Auth.js v5 default HttpOnly + SameSite=Lax; never accessed in app code |
| T-03-03: QueryClient SSR cross-user leak | Providers.tsx uses useState(() => new QueryClient()) |
| T-03-04: AUTH_SECRET in client bundle | api-token.ts is server-only; all backend calls through /api/* server proxy routes |
| T-03-05: Open redirect via callbackUrl | Auth.js v5 validates callbackUrl same-origin by default |
| T-03-07: Members API to non-admins | Backend returns 403; server proxy routes pass user JWT |
| T-03-08: Optimistic role mutation accepted by UI but rejected by backend | onMutate/onError rollback restores prior state |
| T-03-09: OAuth CSRF | Auth.js v5 uses state + PKCE by default |

## Follow-ups for Phase 2

- Wire the "Crie seu primeiro pipeline" CTA to pipeline creation endpoint
- Add pipeline board view with drag-and-drop card management
- Connect `@microsoft/signalr` client to BoardHub for real-time updates
- Add workspace settings page for currency change (D-05)

## Self-Check: PASSED

- [x] `frontend/package.json` exists with `"tailwindcss": "3.4.17"`
- [x] `frontend/components.json` exists
- [x] `frontend/tailwind.config.ts` contains `darkMode: "class"`
- [x] `frontend/src/components/Providers.tsx` contains `useState(`
- [x] `frontend/src/lib/slug.ts` exports `generateSlug` with `[\u0300-\u036f]`
- [x] `frontend/auth.config.ts` contains `user/emails` and `verified`
- [x] `frontend/proxy.ts` exports `auth: proxy`
- [x] `frontend/src/lib/api-token.ts` contains `nodefy-frontend` and `nodefy-api`
- [x] All 13 shadcn UI components exist
- [x] Login page contains "Continuar com GitHub", "Continuar com Google", "Continuar com Microsoft"
- [x] Members page contains "Membros", "Convidar membro", "Tornar admin", "Tornar membro", "Remover do workspace"
- [x] Invite page contains "Enviar convite" and `navigator.clipboard.writeText`
- [x] `MemberTable.tsx` contains `onMutate` for optimistic updates
- [x] Commit `42b5aae` exists (Task 1)
- [x] Commit `4b7de3d` exists (Task 2)
- [x] Commit `3f633a9` exists (Task 3)
- [x] `cd frontend && npm run build` succeeds with zero errors (14 routes generated)
- [x] Task 4 checkpoint: human operator approved all 11 E2E verification steps
