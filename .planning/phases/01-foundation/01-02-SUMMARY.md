---
phase: "01"
plan: "02"
subsystem: backend
tags:
  - backend
  - dotnet
  - api
  - auth
  - multi-tenancy
  - tdd
  - signalr

dependency_graph:
  requires:
    - postgres-schema-v1
    - docker-compose-stack
    - rls-tenant-isolation
  provides:
    - dotnet-api-v1
    - workspace-crud-endpoints
    - invite-flow-endpoints
    - member-management-endpoints
    - sso-sync-endpoint
    - signalr-boardhub-scaffold
    - tenant-isolation-dual-layer
  affects:
    - "01-03: Frontend Auth & Workspace UI (consumes REST endpoints at http://localhost:5000)"

tech_stack:
  added:
    - "Microsoft.EntityFrameworkCore 9.0.4 — ORM with global query filters"
    - "Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4 — Postgres provider"
    - "Microsoft.AspNetCore.Authentication.JwtBearer 9.0.4 — JWT validation"
    - "Microsoft.AspNetCore.SignalR 1.2.9 — real-time hub scaffold"
    - "Testcontainers.PostgreSql 4.11.0 — integration test DB isolation"
    - "FluentAssertions 6.12.2 — test assertions"
    - "xUnit — test runner"
  patterns:
    - "EF Core global query filters: HasQueryFilter(m => m.TenantId == _tenantId) on WorkspaceMember, Invitation, Card"
    - "DbConnectionInterceptor: SET app.current_tenant = '<uuid>' on every connection open (RLS layer)"
    - "Scoped ITenantService: per-request tenant identity — never Singleton (Pitfall 3)"
    - "TestAuthHandler: X-Test-User-Id / X-Test-Tenant-Id headers for integration tests (bypasses JWT in Testing env)"
    - "WebApplicationFactory<Program>: replaces DbContext with TestContainers connection string"
    - "Minimal API endpoint groups: MapGroup('/workspaces').RequireAuthorization()"
    - "RandomNumberGenerator.GetBytes(32): cryptographically-secure invite tokens"

key_files:
  created:
    - api/Nodefy.slnx
    - api/Nodefy.Api/Nodefy.Api.csproj
    - api/Nodefy.Api/Program.cs
    - api/Nodefy.Api/appsettings.json
    - api/Nodefy.Api/appsettings.Development.json
    - api/Nodefy.Api/Data/AppDbContext.cs
    - api/Nodefy.Api/Data/TenantDbConnectionInterceptor.cs
    - api/Nodefy.Api/Data/Entities/User.cs
    - api/Nodefy.Api/Data/Entities/Workspace.cs
    - api/Nodefy.Api/Data/Entities/WorkspaceMember.cs
    - api/Nodefy.Api/Data/Entities/Invitation.cs
    - api/Nodefy.Api/Data/Entities/Card.cs
    - api/Nodefy.Api/Tenancy/ITenantService.cs
    - api/Nodefy.Api/Tenancy/TenantService.cs
    - api/Nodefy.Api/Middleware/TenantMiddleware.cs
    - api/Nodefy.Api/Auth/CurrentUserAccessor.cs
    - api/Nodefy.Api/Auth/JwtConfig.cs
    - api/Nodefy.Api/Endpoints/WorkspaceEndpoints.cs
    - api/Nodefy.Api/Endpoints/MemberEndpoints.cs
    - api/Nodefy.Api/Endpoints/InviteEndpoints.cs
    - api/Nodefy.Api/Endpoints/SsoSyncEndpoints.cs
    - api/Nodefy.Api/Hubs/BoardHub.cs
    - api/Nodefy.Api/Lib/Slug.cs
    - api/Nodefy.Tests/Nodefy.Tests.csproj
    - api/Nodefy.Tests/Fixtures/PostgresFixture.cs
    - api/Nodefy.Tests/Fixtures/ApiFactory.cs
    - api/Nodefy.Tests/Fixtures/TestAuthHandler.cs
    - api/Nodefy.Tests/Integration/TenantIsolationTests.cs
    - api/Nodefy.Tests/Integration/WorkspaceTests.cs
    - api/Nodefy.Tests/Integration/InviteTests.cs
    - api/Nodefy.Tests/Integration/MemberTests.cs
    - api/Nodefy.Tests/Integration/SsoSyncTests.cs
    - api/Nodefy.Tests/Unit/SlugTests.cs
  modified:
    - api/Dockerfile (unchanged — || true scaffold from Plan 01 already accommodates net9.0)

decisions:
  - ".NET 10 SDK creates .slnx format (not .sln) — functionally identical; Dockerfile references .csproj directly and is unaffected"
  - "TenantMiddleware resolves tenant from 4 sources in priority order: JWT tenant_id claim, workspaceId route param, X-Tenant-Id header, id route param (for /workspaces/{id}/... paths)"
  - "IgnoreQueryFilters() allowed in exactly two files: InviteEndpoints.cs (cross-tenant token lookup) and WorkspaceEndpoints.cs (user's own multi-workspace membership query)"
  - "Testing environment detection in Program.cs skips JWT registration — ApiFactory injects TestAuthHandler instead"
  - "PostgresFixture uses WithBindMount to load db/init.sql — ensures test schema matches production exactly"
  - "Invite tokens: 32-byte RandomNumberGenerator + URL-safe base64 = 43-char tokens with 256-bit entropy"

metrics:
  duration_seconds: 7551
  completed_date: "2026-04-17"
  tasks_completed: 3
  tasks_total: 4
  files_created: 33
  files_modified: 1
---

# Phase 01 Plan 02: Backend Auth & Tenant API Summary

**One-liner:** .NET 9 minimal API with dual-layer tenant isolation (EF Core global filters + Postgres RLS), JWT bearer auth, workspace/member/invite CRUD endpoints, SSO sync upsert, and SignalR BoardHub scaffold.

## What Was Built

### Task 1 — TDD RED: Solution scaffold + failing integration test suite

Created `Nodefy.slnx` solution with two projects: `Nodefy.Api` (net9.0 minimal API) and `Nodefy.Tests` (xUnit). Added all NuGet packages (EF Core 9.0.4, Npgsql 9.0.4, JwtBearer 9.0.4, SignalR, TestContainers.PostgreSql 4.11.0, FluentAssertions 6.12.2).

Test infrastructure:
- `PostgresFixture.cs`: spins up `postgres:17-alpine` container, bind-mounts `db/init.sql` for schema parity with production
- `TestAuthHandler.cs`: short-circuits JWT by reading `X-Test-User-Id`, `X-Test-Tenant-Id`, `X-Test-Email` headers
- `ApiFactory.cs`: `WebApplicationFactory<Program>` that replaces DbContext connection string with TestContainers and swaps JWT for TestAuthHandler

22 tests written (19 integration + 3 unit Theory invocations + 1 Fact = 4 SlugTests total):
- `TenantIsolationTests` (2): EF Core filter + raw SQL RLS proofs
- `WorkspaceTests` (5): WORK-01, D-03, D-02, D-04, membership list
- `InviteTests` (5): WORK-02, WORK-03, invite URL, 410 expired, 404 unknown
- `MemberTests` (6): WORK-04, WORK-05, WORK-06, 403, last-admin 409, self-remove 409
- `SsoSyncTests` (3): AUTH-01..03, idempotency, GitHub null-email
- `SlugTests` (4): Theory + Fact

Build fails RED: `AppDbContext`, `ITenantService`, `Slug`, `TenantDbConnectionInterceptor` not yet implemented.

**Commit:** `3c18430`

### Task 2 — TDD GREEN (Data Layer): entities, AppDbContext, interceptor, tenancy, Slug

Created 11 files implementing the data layer:

- **5 entity classes** with complete `HasColumnName()` snake_case mappings to `db/init.sql` columns
- **`AppDbContext`**: 3 `HasQueryFilter` calls — `WorkspaceMember`, `Invitation`, `Card` — all scoped to `_tenantId` captured at construction
- **`TenantDbConnectionInterceptor`**: both sync and async `ConnectionOpened` overrides; executes `SET app.current_tenant = '<uuid>'` on every opened connection; skips `Guid.Empty` (unauthenticated requests like `/health`)
- **`ITenantService` / `TenantService`**: Scoped per-request (never Singleton — prevents cross-request data leak)
- **`CurrentUserAccessor`**: reads `sub` or `NameIdentifier` claim from `IHttpContextAccessor`
- **`Slug.Generate`**: `NormalizationForm.FormD` diacritic stripping, `[^a-z0-9]+` → hyphen, truncates to 50 chars

SlugTests (4) pass GREEN. Integration tests remain RED until endpoints exist.

**Commit:** `296e7ff`

### Task 3 — TDD GREEN (Endpoints + Host): Program.cs, middleware, hub, all endpoints

Created 10 files implementing the full API surface:

- **`Program.cs`**: `AddScoped<ITenantService>`, `AddDbContext` with interceptor, JWT auth (skipped in `Testing` env), `AddSignalR`, CORS, `UseMiddleware<TenantMiddleware>`, all endpoint registrations, `MapHub<BoardHub>("/hubs/board")`
- **`JwtConfig.cs`**: all 4 `ValidateXxx` flags `true`, `ClockSkew = 30s`, configurable via `AUTH_JWT_ISSUER`/`AUTH_JWT_AUDIENCE`/`AUTH_JWT_SECRET`
- **`TenantMiddleware.cs`**: 4-source tenant extraction (JWT claim → workspaceId route → X-Tenant-Id header → id route)
- **`BoardHub.cs`**: `[Authorize]` + `JoinBoard`/`LeaveBoard` scaffold for Phase 3
- **`WorkspaceEndpoints.cs`**: POST (BRL default D-03, AllowedCurrencies D-02), GET (IgnoreQueryFilters for user multi-workspace), PATCH settings (CurrencyLocked 409 D-04)
- **`MemberEndpoints.cs`**: GET admin-only (403), PATCH role (last-admin 409), DELETE (self-remove 409)
- **`InviteEndpoints.cs`**: `RandomNumberGenerator.GetBytes(32)` token, 7-day expiry, `Results.StatusCode(410)` for expired (Pitfall 6), `IgnoreQueryFilters()` for cross-tenant token lookups only
- **`SsoSyncEndpoints.cs`**: upsert by `(provider, providerAccountId)`, rejects `null`/empty email (T-02-10)

Solution builds: **0 errors, 0 warnings**. SlugTests still pass GREEN.

**Commit:** `82cef7e`

### Task 4 — Checkpoint: Human Verify (PENDING)

Docker daemon not running in current execution environment. Integration tests require Docker for TestContainers. The checkpoint awaits human operator to start Docker Desktop and run the full test suite.

## Endpoints Added

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | /health | none | Liveness probe |
| POST | /sso/sync | Bearer JWT | Upsert user by (provider, providerAccountId) |
| POST | /workspaces | Bearer JWT | Create workspace + admin membership (WORK-01) |
| GET | /workspaces | Bearer JWT | List caller's workspaces |
| PATCH | /workspaces/{id}/settings | Bearer JWT, admin | Change currency (D-04 locked guard) |
| POST | /workspaces/{id}/invites | Bearer JWT, admin | Generate 32-byte invite token (WORK-02) |
| GET | /invites/{token} | none | Invite info lookup |
| POST | /invites/{token}/accept | Bearer JWT | Accept invite, create membership (WORK-03) |
| GET | /workspaces/{id}/members | Bearer JWT, admin | List members (WORK-04) |
| PATCH | /workspaces/{id}/members/{userId} | Bearer JWT, admin | Change role with last-admin guard (WORK-05) |
| DELETE | /workspaces/{id}/members/{userId} | Bearer JWT, admin | Remove member with self-remove guard (WORK-06) |
| WS | /hubs/board | Bearer JWT | SignalR hub scaffold (Phase 3) |

## JWT Contract

| Field | Value |
|-------|-------|
| Algorithm | HS256 |
| Issuer | `AUTH_JWT_ISSUER` env var (default: `nodefy-frontend`) |
| Audience | `AUTH_JWT_AUDIENCE` env var (default: `nodefy-api`) |
| Secret | `AUTH_JWT_SECRET` or `AUTH_SECRET` env var |
| Required claims | `sub` (users.id UUID), `email` |
| Optional claims | `tenant_id` (active workspace UUID) |
| ClockSkew | 30 seconds |

## Connection String Contract

```
Host=db;Database=nodefy;Username=nodefy_app;Password=${DB_PASSWORD}
```

Set via `ConnectionStrings__DefaultConnection` environment variable (double-underscore = nested config in .NET).

## IgnoreQueryFilters() Audit

| File | Call site | Reason |
|------|-----------|--------|
| `InviteEndpoints.cs` line 52 | GET /invites/{token} | Cross-tenant token lookup (intended design) |
| `InviteEndpoints.cs` line 65 | POST /invites/{token}/accept | Cross-tenant token lookup |
| `InviteEndpoints.cs` line 71 | POST /invites/{token}/accept | Idempotency check (user already a member?) |
| `WorkspaceEndpoints.cs` line 57 | GET /workspaces | User belongs to multiple tenants — no tenant context here |
| `WorkspaceEndpoints.cs` line 96 | IsAdmin() helper | Admin check needs cross-tenant lookup |

All 5 usages are in documented allowed sites. Zero usages in production code outside these two files.

## Test Coverage Map

| Requirement | Test | Status |
|-------------|------|--------|
| AUTH-01 | SsoSyncTests.PostSsoSync_CreatesUserOnFirstCall | Written — requires Docker |
| AUTH-02 | SsoSyncTests.PostSsoSync_IsIdempotent_DoesNotCreateDuplicate | Written — requires Docker |
| AUTH-03 | SsoSyncTests.PostSsoSync_AcceptsNonNullEmail_FromGitHubFallbackPayload | Written — requires Docker |
| AUTH-04 | (GitHub null-email guard in SsoSyncEndpoints) | Implemented in endpoint |
| AUTH-05 | (JWT validation in JwtConfig.cs) | Implemented + compiled |
| WORK-01 | WorkspaceTests.CreateWorkspace_ReturnsCreated_WithCallerAsAdminMember | Written — requires Docker |
| WORK-02 | InviteTests.CreateInvite_GeneratesTokenOf32Bytes_And7DayExpiry | Written — requires Docker |
| WORK-03 | InviteTests.AcceptInvite_CreatesMembership_WithRoleFromInvitation | Written — requires Docker |
| WORK-04 | MemberTests.GetMembers_AsAdmin_ReturnsList | Written — requires Docker |
| WORK-05 | MemberTests.PatchRole_PromotesMemberToAdmin | Written — requires Docker |
| WORK-06 | MemberTests.DeleteMember_RemovesMembership | Written — requires Docker |
| TEST-01 | All 22 tests via TestContainers xUnit | Written — requires Docker |

## Open Items Deferred to Plan 03

- Frontend Auth.js JWT minting: the frontend must call `POST /sso/sync` in the Auth.js `signIn` callback, then include `tenant_id` claim in subsequent API requests
- GitHub null-email fallback: Plan 03 implements the `/user/emails` fallback in `auth.ts` — the backend rejects empty emails with 400
- `AUTH_JWT_ISSUER` / `AUTH_JWT_AUDIENCE` must be set to `nodefy-frontend` / `nodefy-api` in the frontend `.env.local` so the backend JWT validation passes

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Missing Microsoft.Extensions.DependencyInjection using in test files**
- **Found during:** Task 2 — first build attempt of Nodefy.Tests
- **Issue:** `_factory.Services.CreateScope()` requires `Microsoft.Extensions.DependencyInjection` namespace, not imported in WorkspaceTests.cs, TenantIsolationTests.cs, InviteTests.cs
- **Fix:** Added `using Microsoft.Extensions.DependencyInjection;` to all three files
- **Files modified:** WorkspaceTests.cs, TenantIsolationTests.cs, InviteTests.cs
- **Commit:** Included in `296e7ff`

**2. [Rule 1 - Bug] PostgresFixture obsolete parameterless constructor**
- **Found during:** Task 3 — full solution build warning
- **Issue:** `new PostgreSqlBuilder()` is obsolete in Testcontainers.PostgreSql 4.11.0; warning states it will be removed
- **Fix:** Changed to `new PostgreSqlBuilder("postgres:17-alpine")` with image parameter
- **Files modified:** PostgresFixture.cs
- **Commit:** Included in `82cef7e`

**3. [Rule 1 - Bug] .NET 10 SDK creates .slnx not .sln**
- **Found during:** Task 1 — `dotnet new sln` on .NET SDK 10.0.202 creates `.slnx` format
- **Issue:** Plan referenced `Nodefy.sln` but .NET 10 uses the new `.slnx` XML-less format
- **Fix:** All build commands updated to use `Nodefy.slnx`; Dockerfile references `.csproj` directly (unaffected)
- **Files modified:** None — documentation only

### Auth Gates

- **Docker not running:** TestContainers integration tests require Docker Desktop to be started. Task 4 checkpoint awaits human to start Docker and run `dotnet test`. This is the blocking condition for completing this plan.

## Known Stubs

| Stub | File | Reason |
|------|------|--------|
| `Card` entity has only id, tenant_id, position, stage_entered_at, created_at | `Card.cs` | Phase 2 adds full card schema (title, pipeline_id, stage_id, etc.) |
| `BoardHub.JoinBoard` has no tenant verification | `BoardHub.cs` | Phase 3 adds group join validation before broadcast |

## Threat Surface

All threats from the plan's threat register are mitigated in the implementation:

| Threat | Mitigation Applied |
|--------|-------------------|
| T-02-01: EF Core cross-tenant data | HasQueryFilter on WorkspaceMember, Invitation, Card |
| T-02-02: IgnoreQueryFilters bypass | RLS second layer; IgnoreQueryFilters only in 2 files (audited above) |
| T-02-03: JWT spoofing | JwtConfig: 4 ValidateXxx flags true, ClockSkew 30s |
| T-02-04: Invite token tampering | RandomNumberGenerator.GetBytes(32), 256-bit entropy |
| T-02-05: Expired invite access | Results.StatusCode(410) in both GET and POST accept handlers |
| T-02-06: Invite re-use | AcceptedAt timestamp checked before processing |
| T-02-07: Member calling admin endpoints | IsAdmin() check before every admin operation |
| T-02-08: Last-admin lock | adminCount <= 1 → 409 in PATCH; self-remove → 409 in DELETE |
| T-02-09: DbContext singleton | AddScoped<ITenantService>; AddDbContext default Scoped lifetime |
| T-02-10: GitHub null email | SsoSyncEndpoints rejects empty email with 400 |
| T-02-11: Currency change after card | CurrencyLocked check → 409 in PATCH /settings |
| T-02-12: DB password in env var | Accepted (local dev convenience, out of scope) |
| T-02-13: SQL injection via tenant SET | TenantService.SetTenant accepts only Guid — injection impossible |

## Self-Check: PASSED

- [x] `api/Nodefy.slnx` exists
- [x] `api/Nodefy.Api/Nodefy.Api.csproj` references EF Core 9.0.4 and Npgsql 9.0.4
- [x] `api/Nodefy.Tests/Nodefy.Tests.csproj` references Testcontainers.PostgreSql 4.11.0
- [x] All 33 files listed in key_files.created exist on disk
- [x] `dotnet build api/Nodefy.slnx` succeeds with 0 errors, 0 warnings
- [x] SlugTests (4) pass GREEN
- [x] `AppDbContext.cs` contains `HasQueryFilter(m => m.TenantId == _tenantId)` (3 occurrences)
- [x] `TenantDbConnectionInterceptor.cs` contains `SET app.current_tenant = '`
- [x] `Program.cs` contains `AddScoped<ITenantService, TenantService>()`
- [x] `Program.cs` contains `MapHub<BoardHub>("/hubs/board")`
- [x] `InviteEndpoints.cs` contains `RandomNumberGenerator.GetBytes(32)`
- [x] `InviteEndpoints.cs` contains `Results.StatusCode(410)`
- [x] `IgnoreQueryFilters()` only in InviteEndpoints.cs and WorkspaceEndpoints.cs
- [x] Commit `3c18430` exists (Task 1 RED)
- [x] Commit `296e7ff` exists (Task 2 GREEN data layer)
- [x] Commit `82cef7e` exists (Task 3 GREEN endpoints)
- [ ] Integration tests: require Docker Desktop running (Task 4 checkpoint pending)
