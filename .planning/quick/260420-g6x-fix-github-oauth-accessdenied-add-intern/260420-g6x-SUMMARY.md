---
phase: quick
plan: 260420-g6x
subsystem: auth
tags: [oauth, docker, signalr, fix]
key-files:
  modified:
    - frontend/auth.ts
    - docker-compose.yml
decisions:
  - INTERNAL_API_URL set as a server-only env var (no NEXT_PUBLIC_ prefix) — not exposed to the browser bundle
tech-stack:
  patterns:
    - Three-level env var fallback: INTERNAL_API_URL > NEXT_PUBLIC_API_URL > hardcoded default
completed: "2026-04-20"
duration_minutes: 5
tasks_completed: 2
files_changed: 2
commits:
  - hash: 474c226
    message: "fix(260420-g6x): add INTERNAL_API_URL to frontend service in docker-compose"
  - hash: 17760be
    message: "fix(260420-g6x): use INTERNAL_API_URL fallback chain in signIn callback"
---

# Quick Fix 260420-g6x: Fix GitHub OAuth AccessDenied — Add INTERNAL_API_URL

**One-liner:** Added `INTERNAL_API_URL=http://api:5000` Docker env var and three-level fallback in auth.ts signIn callback so the server-side SSO sync fetch routes to the api container instead of the container's own loopback.

## Root Cause

The `signIn` callback in `frontend/auth.ts` ran server-side inside the `nodefy-frontend` container. It resolved `apiUrl` from `NEXT_PUBLIC_API_URL`, which is set to `http://localhost:5000` for browser use. Inside the container, `localhost` is the container's own loopback — nothing listens there on port 5000. The fetch to `/sso/sync` failed silently, `signIn` returned `false`, and Auth.js emitted an `AccessDenied` redirect.

## Fix

### Task 1 — docker-compose.yml

Added `INTERNAL_API_URL: http://api:5000` immediately after `NEXT_PUBLIC_API_URL` in the `frontend` service `environment` block. `api` resolves as a hostname within the Docker Compose bridge network to `nodefy-api` (the backend container on port 5000). `NEXT_PUBLIC_API_URL` is unchanged — it must remain `http://localhost:5000` for browser-side fetches that run on the host.

### Task 2 — frontend/auth.ts

Replaced the single `NEXT_PUBLIC_API_URL` lookup on line 15 with a three-level fallback:

```typescript
const apiUrl =
  process.env.INTERNAL_API_URL ??
  process.env.NEXT_PUBLIC_API_URL ??
  "http://localhost:5000"
```

Fallback rationale:
1. `INTERNAL_API_URL` — present in Docker; routes correctly to `http://api:5000`
2. `NEXT_PUBLIC_API_URL` — present in `.env.local` for local dev with a running backend
3. `"http://localhost:5000"` — bare last-resort default (no env file at all)

No other lines in `auth.ts` were changed.

## Verification

- `docker-compose.yml` line 44: `INTERNAL_API_URL: http://api:5000` present under `frontend.environment`
- `frontend/auth.ts` lines 15-18: three-level fallback present in signIn callback
- `NEXT_PUBLIC_API_URL` still referenced in fallback — browser-side fetches unaffected
- Only 2 files modified — no scope creep

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

None.

## Threat Flags

None — no new network endpoints or auth paths introduced. The fetch target changes from an unreachable loopback to the correct internal service; the security surface is identical.

## Self-Check: PASSED

- `frontend/auth.ts` exists and contains `INTERNAL_API_URL` on line 16
- `docker-compose.yml` exists and contains `INTERNAL_API_URL: http://api:5000` on line 44
- Commits 474c226 and 17760be present in git log
