---
phase: "01"
plan: "01"
subsystem: infrastructure
tags:
  - database
  - docker
  - multi-tenancy
  - rls
  - postgresql

dependency_graph:
  requires: []
  provides:
    - postgres-schema-v1
    - docker-compose-stack
    - rls-tenant-isolation
    - fractional-indexing-columns
    - currency-columns
  affects:
    - "01-02: Backend Auth & Tenant API (consumes DB connection string, role, RLS session var)"
    - "01-03: Frontend Auth & Workspace UI (consumes env var names from .env.example)"

tech_stack:
  added:
    - "postgres:17-alpine — primary database"
    - "mcr.microsoft.com/dotnet/sdk:9.0-alpine — API build image"
    - "mcr.microsoft.com/dotnet/aspnet:9.0-alpine — API runtime image"
    - "node:22-alpine — frontend build and runtime image"
  patterns:
    - "PostgreSQL RLS via current_setting('app.current_tenant', true)::UUID"
    - "Docker multi-stage builds with || true for scaffold-phase tolerance"
    - "docker-compose.override.yml for dev bind-mounts (auto-loaded by docker compose)"

key_files:
  created:
    - db/init.sql
    - db/README.md
    - docker-compose.yml
    - docker-compose.override.yml
    - .env.example
    - .gitignore
    - api/Dockerfile
    - frontend/Dockerfile
  modified: []

decisions:
  - "Automated verification check forbids 'SUPERUSER'/'BYPASSRLS' strings anywhere in init.sql — comments reworded to use 'elevated privileges' instead; intent preserved in db/README.md"
  - "Dockerfile build steps use '|| true' to allow docker compose build to succeed before Plans 02/03 add application code"
  - "init.sql volume mount uses :ro flag per T-01-03 threat mitigation"
  - "current_setting('app.current_tenant', true) uses second arg 'true' so RLS returns NULL instead of erroring during EF Core schema introspection"

metrics:
  duration_seconds: 203
  completed_date: "2026-04-17"
  tasks_completed: 3
  tasks_total: 3
  files_created: 8
  files_modified: 0
---

# Phase 01 Plan 01: DB Schema & Docker Stack Summary

**One-liner:** PostgreSQL 17 schema with RLS tenant isolation, fractional-indexing stub cards table, and Docker Compose three-service stack (db, api, frontend).

## What Was Built

### Task 1 — db/init.sql with full schema, RLS, and fractional-indexing fields

Created `db/init.sql` as the first migration, mounted read-only into the Postgres container at `/docker-entrypoint-initdb.d/init.sql`. The file establishes:

- **5 tables:** `users` (global), `workspaces` (tenant root), `workspace_members`, `invitations`, `cards` (stub)
- **RLS enabled** on `workspace_members`, `invitations`, `cards` with `tenant_isolation_policy` referencing `current_setting('app.current_tenant', true)::UUID`
- **Fractional-indexing columns** on `cards`: `position DOUBLE PRECISION NOT NULL DEFAULT 0.5` and `stage_entered_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`
- **Currency columns** on `workspaces`: `currency VARCHAR(3) NOT NULL DEFAULT 'BRL'` with `CHECK (currency IN ('BRL', 'USD', 'EUR'))` and `currency_locked BOOLEAN NOT NULL DEFAULT false`
- **Role constraints:** `role IN ('admin', 'member')` on both `workspace_members` and `invitations`
- **Indexes:** tenant and user indexes on `workspace_members`; tenant and token indexes on `invitations`; tenant index on `cards`

Also created `db/README.md` documenting the migration purpose and the requirement that `nodefy_app` must remain a standard login role without elevated privileges.

**Commit:** `0155984`

### Task 2 — Docker Compose stack, Dockerfiles, env template, gitignore

Created all infrastructure scaffold files:

- **`docker-compose.yml`:** Three services (`db`, `api`, `frontend`). The `db` service uses `postgres:17-alpine`, mounts `db/init.sql` as `:ro`, has a healthcheck on `pg_isready`, and exposes port 5432. The `api` service depends on `db` with `condition: service_healthy`. The `frontend` service depends on `api`.
- **`docker-compose.override.yml`:** Auto-loaded in dev; adds bind-mount volumes and hot-reload commands (`dotnet watch run` for api, `npm run dev` for frontend).
- **`api/Dockerfile`:** Multi-stage .NET 9 SDK alpine build with `|| true` on restore/publish so scaffold builds succeed before Plan 02 adds the .csproj.
- **`frontend/Dockerfile`:** Multi-stage Node 22 alpine build with `|| true` on install/build steps for the same reason.
- **`.env.example`:** Template with all required env vars — `DB_PASSWORD`, `AUTH_SECRET`, `AUTH_GITHUB_ID/SECRET`, `AUTH_GOOGLE_ID/SECRET`, `AUTH_MICROSOFT_ENTRA_ID_ID/SECRET/ISSUER`, `FRONTEND_URL`, `NEXT_PUBLIC_API_URL` — with OAuth callback URL comments for each provider.
- **`.gitignore`:** Covers `.env*`, `.NET` build artifacts (`bin/`, `obj/`), Node artifacts (`node_modules/`, `.next/`), IDE files, OS files, and `docker-compose.local.yml`.

**Commit:** `3541e42`

### Task 3 — Human Verify (checkpoint — APPROVED)

Human operator ran all verification steps and typed "approved". Results confirmed:

| Check | Expected | Verified |
|-------|----------|---------|
| `rolsuper, rolbypassrls` for nodefy_app | `f \| f` | Yes |
| Tables present (`\dt`) | users, workspaces, workspace_members, invitations, cards | Yes (5 tables) |
| `cards.position` column type | `double precision` | Yes |
| `cards.stage_entered_at` column type | `timestamp with time zone` | Yes |
| RLS enabled on 3 tables | `rowsecurity = t` for workspace_members, invitations, cards | Yes |
| Cross-tenant isolation (tenant A context) | `COUNT(*) = 1` | Yes |
| Cross-tenant isolation (tenant B context) | `COUNT(*) = 1` | Yes |

**Operator response:** "approved"

## Key Interfaces Established for Plan 02

| Interface | Value |
|-----------|-------|
| DB connection string format | `Host=db;Database=nodefy;Username=nodefy_app;Password=${DB_PASSWORD}` |
| RLS session variable | `SET app.current_tenant = '<uuid>'` (must be set by TenantDbConnectionInterceptor on every connection) |
| Postgres role | `nodefy_app` — standard login role, no elevated privileges |
| init.sql path | `./db/init.sql` → mounted at `/docker-entrypoint-initdb.d/init.sql:ro` |
| API port | 5000 (internal and host) |
| Frontend port | 3000 (internal and host) |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Automated verification grep rejects 'superuser'/'bypassrls' case-insensitively**
- **Found during:** Task 1 verification
- **Issue:** The plan's acceptance criteria states `! grep -qi "SUPERUSER"` but the plan also specifies comment text containing `-- We must NOT make it a SUPERUSER and NOT BYPASSRLS`. The word "superuser" also appears naturally in "non-superuser". The `-i` grep flag matches case-insensitively, so "non-superuser", "non-superuser", and any mention of the role attribute names would fail.
- **Fix:** Rewrote all comments to use "elevated privileges", "standard login role", and "no elevated privileges" instead of the specific attribute names. The same security intent is preserved in `db/README.md` with plain-language explanation.
- **Files modified:** `db/init.sql`
- **Commit:** Included in `0155984`

## Known Stubs

| Stub | File | Reason |
|------|------|--------|
| `cards` table has only id, tenant_id, position, stage_entered_at, created_at | `db/init.sql` | Intentional Phase 1 stub — Plan 02 (Phase 2) adds title, description, monetary_value, assignee_id, close_date, pipeline_id, stage_id, archived_at |
| `api/Dockerfile` build steps use `|| true` | `api/Dockerfile` | No .csproj exists yet — Plan 02 creates the .NET project |
| `frontend/Dockerfile` build steps use `|| true` | `frontend/Dockerfile` | No Next.js project exists yet — Plan 03 creates the frontend |

## Threat Surface

All threats from the plan's threat register are mitigated:

| Threat | Mitigation Applied |
|--------|-------------------|
| T-01-01: nodefy_app role elevation | Role created by POSTGRES_USER default (non-elevated); comments guard against changes; verified in Task 3 |
| T-01-02: RLS tenant isolation | `ENABLE ROW LEVEL SECURITY` + `tenant_isolation_policy` on 3 tables; verified in Task 3 |
| T-01-03: init.sql tampering via volume | Volume mount declared `:ro` in docker-compose.yml |
| T-01-04: .env secrets leakage | `.gitignore` excludes `.env`, `.env.local`, `.env.*.local`; `.env.example` committed with empty values |
| T-01-05: invalid currency codes | `CHECK (currency IN ('BRL', 'USD', 'EUR'))` constraint on workspaces |
| T-01-06: invalid role values | `CHECK (role IN ('admin', 'member'))` on workspace_members and invitations |

## Self-Check: PASSED

- [x] `db/init.sql` exists and passes all automated verification checks
- [x] `db/README.md` exists
- [x] `docker-compose.yml` exists with postgres:17-alpine, nodefy_app, init.sql mount, service_healthy
- [x] `docker-compose.override.yml` exists
- [x] `.env.example` exists with all required vars
- [x] `.gitignore` exists with .env, bin/, obj/, node_modules/, .next/
- [x] `api/Dockerfile` exists with FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine
- [x] `frontend/Dockerfile` exists with FROM node:22-alpine
- [x] Commit `0155984` exists (Task 1)
- [x] Commit `3541e42` exists (Task 2)
- [x] Task 3 human checkpoint: APPROVED by operator (all psql verification steps confirmed)
