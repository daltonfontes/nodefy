# Domain Pitfalls — Nodefy

**Researched:** 2026-04-16
**Confidence:** HIGH across all areas

---

## 1. Multi-Tenant Architecture

### CRITICAL 1.1 — Missing Tenant Context Propagation

**What goes wrong:** `tenantId` is extracted from JWT at the API boundary but never enforced at the repository/query layer. Any authenticated user who guesses a valid `pipelineId` from another tenant can read its data.

**Warning signs:**
- Repository methods accept `pipelineId` without also requiring `tenantId`
- Integration tests pass without asserting tenant isolation
- No cross-tenant isolation test suite

**Prevention:**
- Inject `TenantContext` service into every repository. Every query method must accept `tenantId`.
- EF Core global query filter: `modelBuilder.Entity<Card>().HasQueryFilter(c => c.TenantId == _tenantContext.CurrentTenantId)`
- Write integration test for every resource: authenticate as Tenant A, attempt to fetch Tenant B resource — assert 403/404.

**Phase:** Phase 1 (Foundation). Must be baked into data access layer before any business entity is implemented.

---

### CRITICAL 1.2 — Schema-Per-Tenant Migration Complexity

**What goes wrong:** Schema-per-tenant feels clean initially. With 50 tenants, a failed migration mid-run leaves the database in a split-brain state.

**Prevention:** Use **shared schema + row-level isolation** (`tenant_id` column on every table). This is the correct choice for greenfield SaaS v1.

**Phase:** Phase 1. Must be decided before any tables are created.

---

### MODERATE 1.3 — N+1 Queries on Tenant-Scoped Collections

**What goes wrong:** Loading a pipeline loads each stage's cards in a loop. 10 stages × 50 cards = 501 queries instead of 2-3.

**Warning signs:** EF Core SQL logs show the same query repeated N times with different `stage_id` values.

**Prevention:**
- Use `.Include(p => p.Stages).ThenInclude(s => s.Cards)` for pipeline load queries
- Write performance test asserting loading a pipeline with 10 stages × 100 cards fires fewer than 5 queries

**Phase:** Phase 2 (Pipeline visual).

---

## 2. OAuth SSO Integration in .NET

### CRITICAL 2.1 — Silent Token Refresh Failures

**What goes wrong:** OAuth access token expires (Google/Microsoft: 1 hour). Backend makes an API call with stale token and gets 401. App crashes silently or logs user out unexpectedly.

**Prevention:**
- Request offline access explicitly: Google requires `access_type=offline&prompt=consent`, Microsoft requires `offline_access` scope, GitHub tokens are long-lived.
- For Nodefy v1: SSO is used for identity only, not ongoing provider API calls. Set 7-day sliding session and store only user identity claims in the session.

**Phase:** Phase 1 (Authentication).

---

### CRITICAL 2.2 — Callback URL Misconfiguration Across Environments

**What goes wrong:** OAuth provider has `https://myapp.com/auth/callback` whitelisted but not `http://localhost:3000/auth/callback`. Login fails in dev with a cryptic provider error.

**Prevention:**
- Never hardcode callback URLs — always derive from `baseUrl` env var.
- Maintain an OAuth Setup Checklist with every registered redirect URI per environment per provider.

**Phase:** Phase 1. Address during OAuth provider registration, before writing any code.

---

### MODERATE 2.3 — Scope Mismatch Between Providers

**What goes wrong:** GitHub's OAuth only provides email via a separate API call. App silently stores null email for GitHub users, breaking invite flows.

**Prevention:**
- Per-provider normalization handler: after each callback, fetch the provider-specific user info endpoint.
- GitHub: always call `GET /user/emails` with scope `user:email` for the primary verified email.
- Store `provider` + `provider_user_id` as primary identity key. Allow linking multiple provider identities to one Nodefy user.

**Phase:** Phase 1. Must be solved before any user data is written to the database.

---

## 3. Drag-and-Drop Real-Time Sync

### CRITICAL 3.1 — Order Conflict on Concurrent Moves

**What goes wrong:** User A and User B simultaneously move different cards to the same position. Both optimistic updates succeed locally. One overwrites the other and the board re-renders with conflicting order.

**Prevention:**
- Use **fractional indexing** (float-based): moving a card between positions `a` and `b` gets `(a + b) / 2.0`. Only the moved card's row is updated.
- **Must be chosen before the first drag-and-drop implementation.** Switching from integer positions to fractional indexing after data exists requires migrating all card positions.
- On conflict (two cards with identical position), backend detects and triggers a rebalance broadcast.
- Store `stage_entered_at` on cards — updated on every move, required for stage-age tracking differentiator.

**Phase:** Phase 2/3. Fractional indexing must be in the initial data model.

---

### CRITICAL 3.2 — Optimistic Update Desync on Network Failure

**What goes wrong:** User drags a card, UI shows it in new column immediately. API call times out. No rollback mechanism — UI stuck in inconsistent state.

**Prevention:**
- Model card state explicitly: `{ position, pendingPosition, status: 'idle' | 'moving' | 'error' }`
- Animate card back to original position on rollback (not a hard snap). Show toast notification on move failure.
- Use React Query's `onError` rollback in mutation API.

**Phase:** Phase 2. Implement rollback mechanics alongside first DnD implementation.

---

### MODERATE 3.3 — WebSocket Room Leakage Between Tenants

**What goes wrong:** Real-time room named by `pipeline_id` only. Misconfigured client subscribes to another tenant's pipeline and receives all events.

**Prevention:**
- On WebSocket join: verify `pipeline.tenant_id == user.tenant_id` in the database. Reject with `4403` if mismatch.
- Use UUIDs for all IDs — not sequential integers (enumerable).
- Room naming: `tenant:{tenantId}:pipeline:{pipelineId}` — include tenantId as defense-in-depth.

**Phase:** Phase 3 (Real-time). Must be in place before any WebSocket infrastructure ships.

---

## 4. Next.js SaaS App Structure

### CRITICAL 4.1 — Tenant Resolution DB Roundtrip on Every Request

**What goes wrong:** Middleware resolves tenant via DB query on every request. Under load, connection pool saturates.

**Prevention:**
- Cache tenant slug → tenantId in Redis with 5-10 minute TTL. For v1 without Redis: LRU in-memory cache (`lru-cache`, 1000-entry limit).
- Exclude `_next/`, static assets, and health endpoints from tenant resolution middleware.

**Phase:** Phase 1. Establish tenant resolution strategy before any protected routes.

---

### CRITICAL 4.2 — Auth Middleware Not Protecting API Routes

**What goes wrong:** Middleware protects `/dashboard/*` pages but not `/api/*`. Unauthenticated client fetches `/api/pipelines` and receives data.

**Prevention:**
```typescript
export const config = {
  matcher: ['/dashboard/:path*', '/api/:path*'],
};
```
- Defense-in-depth: API handlers must also call `getServerSession()` independently and return 401.
- Write integration tests making unauthenticated requests to every API route, asserting 401.

**Phase:** Phase 1. Establish as part of auth scaffolding.

---

### MODERATE 4.3 — Client-Side Tenant Leakage via Browser Cache

**What goes wrong:** React Query caches by URL path, not user identity. User A's data appears for User B on a shared machine before session check completes.

**Prevention:**
- Include `tenantId` in all React Query cache keys: `useQuery(['pipelines', tenantId])`.
- Set `Cache-Control: no-store` on all authenticated API responses.
- On logout, call `queryClient.clear()`.

**Phase:** Phase 2.

---

## 5. PostgreSQL for SaaS

### CRITICAL 5.1 — PgBouncer Transaction Mode Breaks EF Core Prepared Statements

**What goes wrong:** EF Core uses named prepared statements. PgBouncer transaction pooling mode doesn't support them across connections. `ERROR: prepared statement "p1" already exists` under concurrency.

**Prevention:**
Add `No_Prepare=true` to Npgsql connection string when using PgBouncer transaction mode:
```
Host=pgbouncer;Database=nodefy;...;No_Prepare=true;Maximum Pool Size=20
```
Or use PgBouncer session pooling mode for v1.

**Phase:** Phase 1. Configure before any EF Core migrations run against PgBouncer.

---

### CRITICAL 5.2 — RLS False Sense of Security

**What goes wrong:** Application DB user is a superuser or has `BYPASSRLS`. All RLS policies are silently ignored. Team believes isolation is enforced when it isn't.

**Prevention:**
```sql
CREATE ROLE nodefy_app WITH LOGIN PASSWORD '...' NOINHERIT;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO nodefy_app;
```
- Never use `postgres` superuser for the application connection.
- Write database-level integration test: connect as `nodefy_app`, set tenant context to Tenant A, assert querying a Tenant B card returns zero rows.

**Phase:** Phase 1. Design RLS alongside the data model.

---

### MODERATE 5.3 — Connection Pool Exhaustion Under Burst Load

**What goes wrong:** Multiple .NET replicas × EF Core connections > PostgreSQL `max_connections = 100`. 101st connection fails with `too many clients already`.

**Prevention:**
- Use PgBouncer as connection proxy in front of PostgreSQL.
- Explicit Npgsql pool size: `Maximum Pool Size=20;Minimum Pool Size=2`.
- Monitor: `SELECT count(*) FROM pg_stat_activity`. Alert above 80% of `max_connections`.

**Phase:** Phase 1. Configure before load testing.

---

## 6. Docker Deployment

### CRITICAL 6.1 — .NET App Runs as Root in Container

**Prevention:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "Nodefy.Api.dll"]
```
Add `security_opt: ["no-new-privileges:true"]` in `docker-compose.yml`.

**Phase:** Phase 1. Establish secure Dockerfiles from first commit.

---

### CRITICAL 6.2 — Secrets Committed to Git

**Prevention:**
- All secrets in compose use interpolation: `POSTGRES_PASSWORD=${POSTGRES_PASSWORD}`
- Commit `.env.example` with placeholder values. Add `.env` to `.gitignore` in the very first commit.

**Phase:** Phase 1. Add `.gitignore` before writing any Docker configuration.

---

### MODERATE 6.3 — Startup Order Race Condition

**What goes wrong:** API starts before PostgreSQL is ready. `depends_on: db` only waits for container start, not DB readiness.

**Prevention:**
```yaml
db:
  healthcheck:
    test: ["CMD-SHELL", "pg_isready -U nodefy -d nodefy"]
    interval: 5s
    retries: 10

api:
  depends_on:
    db:
      condition: service_healthy
```
Add Polly `WaitAndRetryAsync` on initial EF Core connection as defense-in-depth.

**Phase:** Phase 1.

---

### MINOR 6.4 — Next.js Build-Time Environment Variable Baking

**What goes wrong:** `NEXT_PUBLIC_API_URL` is baked into the Docker image at build time. Same image can't be promoted from staging to production.

**Prevention:** Use Next.js runtime configuration or a `/api/config` endpoint. Build once, deploy anywhere.

**Phase:** Phase 1.

---

## Phase-Specific Warning Matrix

| Phase | Pitfall | Priority |
|-------|---------|----------|
| 1 | Missing tenant context propagation | CRITICAL |
| 1 | Shared schema (not schema-per-tenant) | CRITICAL |
| 1 | OAuth silent token refresh | CRITICAL |
| 1 | Callback URL misconfiguration | CRITICAL |
| 1 | GitHub email scope mismatch | CRITICAL |
| 1 | PgBouncer + EF Core prepared statements | CRITICAL |
| 1 | RLS with superuser role | CRITICAL |
| 1 | App running as root in Docker | CRITICAL |
| 1 | Secrets committed to git | CRITICAL |
| 1 | Docker startup race condition | MODERATE |
| 1 | Next.js build-time env baking | MODERATE |
| 2 | N+1 queries on board load | MODERATE |
| 2 | API routes not protected by auth middleware | CRITICAL |
| 2 | Browser cache tenant leakage | MODERATE |
| 2 | Optimistic update desync on network failure | CRITICAL |
| 2 | Tenant resolution DB roundtrip per request | MODERATE |
| 3 | Concurrent card move order conflict | CRITICAL |
| 3 | WebSocket room leakage between tenants | CRITICAL |
| 3 | Connection pool exhaustion | MODERATE |
