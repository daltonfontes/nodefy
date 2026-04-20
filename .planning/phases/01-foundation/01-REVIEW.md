---
phase: 01-foundation
reviewed: 2026-04-16T00:00:00Z
depth: standard
files_reviewed: 92
files_reviewed_list:
  - .env.example
  - .gitignore
  - api/Dockerfile
  - api/Nodefy.Api/Auth/CurrentUserAccessor.cs
  - api/Nodefy.Api/Auth/JwtConfig.cs
  - api/Nodefy.Api/Data/AppDbContext.cs
  - api/Nodefy.Api/Data/Entities/Card.cs
  - api/Nodefy.Api/Data/Entities/Invitation.cs
  - api/Nodefy.Api/Data/Entities/User.cs
  - api/Nodefy.Api/Data/Entities/Workspace.cs
  - api/Nodefy.Api/Data/Entities/WorkspaceMember.cs
  - api/Nodefy.Api/Data/TenantDbConnectionInterceptor.cs
  - api/Nodefy.Api/Endpoints/InviteEndpoints.cs
  - api/Nodefy.Api/Endpoints/MemberEndpoints.cs
  - api/Nodefy.Api/Endpoints/SsoSyncEndpoints.cs
  - api/Nodefy.Api/Endpoints/WorkspaceEndpoints.cs
  - api/Nodefy.Api/Hubs/BoardHub.cs
  - api/Nodefy.Api/Lib/Slug.cs
  - api/Nodefy.Api/Middleware/TenantMiddleware.cs
  - api/Nodefy.Api/Nodefy.Api.csproj
  - api/Nodefy.Api/Program.cs
  - api/Nodefy.Api/Tenancy/ITenantService.cs
  - api/Nodefy.Api/Tenancy/TenantService.cs
  - api/Nodefy.Api/appsettings.Development.json
  - api/Nodefy.Api/appsettings.json
  - api/Nodefy.Tests/Fixtures/ApiFactory.cs
  - api/Nodefy.Tests/Fixtures/PostgresFixture.cs
  - api/Nodefy.Tests/Fixtures/TestAuthHandler.cs
  - api/Nodefy.Tests/Integration/InviteTests.cs
  - api/Nodefy.Tests/Integration/MemberTests.cs
  - api/Nodefy.Tests/Integration/SsoSyncTests.cs
  - api/Nodefy.Tests/Integration/TenantIsolationTests.cs
  - api/Nodefy.Tests/Integration/WorkspaceTests.cs
  - api/Nodefy.Tests/Nodefy.Tests.csproj
  - api/Nodefy.Tests/Unit/SlugTests.cs
  - api/Nodefy.slnx
  - db/README.md
  - db/init.sql
  - docker-compose.override.yml
  - docker-compose.yml
  - frontend/.env.local.example
  - frontend/.eslintrc.json
  - frontend/Dockerfile
  - frontend/auth.config.ts
  - frontend/auth.ts
  - frontend/components.json
  - frontend/next.config.ts
  - frontend/package.json
  - frontend/postcss.config.mjs
  - frontend/proxy.ts
  - frontend/src/app/(auth)/login/page.tsx
  - frontend/src/app/(auth)/workspace/new/page.tsx
  - frontend/src/app/(auth)/workspace/select/page.tsx
  - frontend/src/app/api/auth/[...nextauth]/route.ts
  - frontend/src/app/api/invites/[token]/accept/route.ts
  - frontend/src/app/api/invites/[token]/accept/route.ts
  - frontend/src/app/api/workspaces/[id]/invites/route.ts
  - frontend/src/app/api/workspaces/[id]/members/[userId]/route.ts
  - frontend/src/app/api/workspaces/[id]/members/route.ts
  - frontend/src/app/api/workspaces/proxy/route.ts
  - frontend/src/app/globals.css
  - frontend/src/app/invite/[token]/page.tsx
  - frontend/src/app/layout.tsx
  - frontend/src/app/page.tsx
  - frontend/src/app/workspace/[id]/layout.tsx
  - frontend/src/app/workspace/[id]/page.tsx
  - frontend/src/app/workspace/[id]/settings/members/MemberTable.tsx
  - frontend/src/app/workspace/[id]/settings/members/invite/page.tsx
  - frontend/src/app/workspace/[id]/settings/members/page.tsx
  - frontend/src/components/Logo.tsx
  - frontend/src/components/LogoutButton.tsx
  - frontend/src/components/Providers.tsx
  - frontend/src/components/SsoButton.tsx
  - frontend/src/components/WorkspaceTopNav.tsx
  - frontend/src/components/ui/alert-dialog.tsx
  - frontend/src/components/ui/alert.tsx
  - frontend/src/components/ui/avatar.tsx
  - frontend/src/components/ui/badge.tsx
  - frontend/src/components/ui/button.tsx
  - frontend/src/components/ui/card.tsx
  - frontend/src/components/ui/dialog.tsx
  - frontend/src/components/ui/dropdown-menu.tsx
  - frontend/src/components/ui/input.tsx
  - frontend/src/components/ui/label.tsx
  - frontend/src/components/ui/select.tsx
  - frontend/src/components/ui/separator.tsx
  - frontend/src/components/ui/table.tsx
  - frontend/src/lib/api-token.ts
  - frontend/src/lib/api.ts
  - frontend/src/lib/slug.ts
  - frontend/src/lib/utils.ts
  - frontend/src/store/ui-store.ts
  - frontend/src/types/api.ts
  - frontend/tailwind.config.ts
  - frontend/tsconfig.json
findings:
  critical: 4
  warning: 6
  info: 5
  total: 15
status: issues_found
---

# Phase 01: Code Review Report

**Reviewed:** 2026-04-16T00:00:00Z
**Depth:** standard
**Files Reviewed:** 92
**Status:** issues_found

## Summary

This is the Phase 1 foundation of a multi-tenant SaaS CRM. The overall architecture is well thought out: the two-layer tenant isolation (EF Core global query filters + PostgreSQL RLS) is sound, the JWT minting flow is correct, and the `TenantDbConnectionInterceptor` correctly uses a validated `Guid` before string-interpolating into `SET app.current_tenant`. The test suite is good — integration tests with Testcontainers cover the critical tenant-isolation paths.

Four critical issues were found. Two are hardcoded live OAuth credentials committed to the `.env.example` file; one is a tenant-authorization bypass on the `GET /invites/{token}` endpoint that leaks workspace names cross-tenant to unauthenticated callers; and one is a Dockerfile pattern that silently swallows build failures. The warnings cover a race condition in slug generation, a missing CSRF defense on the invite-accept form, missing email validation in the SSO sync endpoint, error-status leakage from Next.js API routes, and an authorization gap in the members endpoint. Info items cover Docker build hygiene and minor code quality points.

---

## Critical Issues

### CR-01: Live GitHub OAuth credentials committed to `.env.example`

**File:** `.env.example:9-10`
**Issue:** Real, active GitHub OAuth App credentials (`AUTH_GITHUB_ID=Ov23li...` and `AUTH_GITHUB_SECRET=6d056905...`) are committed to the repository. Anyone with read access to this repo can use these credentials to impersonate the OAuth app, intercept authorization codes, or exhaust rate limits. Even if these are meant to be "demo" credentials, committed OAuth secrets must be rotated immediately because git history is permanent.
**Fix:** Rotate the GitHub OAuth app credentials immediately via https://github.com/settings/developers. Replace the values in `.env.example` with empty placeholders:
```
AUTH_GITHUB_ID=
AUTH_GITHUB_SECRET=
```
Add a `git filter-repo` or BFG run to scrub the secret from git history.

---

### CR-02: Unauthenticated `GET /invites/{token}` leaks workspace names cross-tenant

**File:** `api/Nodefy.Api/Endpoints/InviteEndpoints.cs:49-59`
**Issue:** The `GET /invites/{token}` endpoint is intentionally public (no `.RequireAuthorization()`), but it responds with the workspace name and invite role for any valid token. This is a cross-tenant information disclosure: an attacker who brute-forces or obtains an invite token can enumerate workspace names and the role being granted, with no authentication required. The endpoint also has no rate-limiting, making token enumeration cheap. More concretely, the response `InviteInfoResponse(ws.Name, invite.Role)` exposes tenant metadata to unauthenticated parties.
**Fix:** The invite landing page (`/invite/[token]`) already requires authentication (it calls `auth()` and redirects to login if not authenticated). The backend endpoint can be restricted to authenticated callers as well, since the frontend will always carry a session before hitting it:
```csharp
app.MapGet("/invites/{token}", async (string token, AppDbContext db) =>
{
    // ...existing logic...
}).RequireAuthorization();
```
If the public-lookup behaviour is intentional for UX (show workspace name before login), at minimum add aggressive rate-limiting (`RequireRateLimiting`) and strip the workspace name from the 410/409 error responses so they leak no tenant data.

---

### CR-03: Invite accept form lacks CSRF protection — open to cross-site POST

**File:** `frontend/src/app/invite/[token]/page.tsx:41`
**Issue:** The invite accept button submits a plain HTML `<form action="/api/invites/{token}/accept" method="post">`. This is a cross-site request forgery (CSRF) vector: any site can construct an `<img>` or `<form>` that silently POSTs to `/api/invites/{token}/accept` while a logged-in user's browser includes the Next-Auth session cookie. The result is that a third party who knows an invite token can force any logged-in Nodefy user to accept that invite on their behalf.
**Fix:** Replace the plain HTML form with a client-side POST using `fetch` (or a Server Action), which the browser's CORS policy will protect, and which Auth.js CSRF protection covers for same-origin requests. Alternatively, use a Next.js Server Action with the `use server` directive — Auth.js v5 CSRF tokens are bound to Server Actions automatically:
```tsx
// page.tsx — convert to a Server Action
import { redirect } from "next/navigation"
import { apiFetch } from "@/lib/api"

async function acceptInvite(token: string) {
  "use server"
  const result = await apiFetch<{ workspaceId: string }>(`/invites/${token}/accept`, { method: "POST" })
  redirect(`/workspace/${result.workspaceId}`)
}

// In JSX:
<form action={() => acceptInvite(token)}>
  <Button type="submit" className="w-full">Aceitar convite</Button>
</form>
```

---

### CR-04: Dockerfiles suppress build failures with `|| true` — broken images ship silently

**File:** `api/Dockerfile:5-6`, `frontend/Dockerfile:5,9`
**Issue:** Both Dockerfiles append `|| true` to `dotnet restore`, `dotnet publish`, and `npm run build`, which means a compilation error, a missing dependency, or a TypeScript error will produce a zero-exit-code build. The resulting image will either be empty (no output binary) or contain a stale partially-built artifact. When this image is deployed, the container crashes at runtime with a cryptic error rather than failing loudly at build time where it should.
**Fix:** Remove all `|| true` guards from build steps. Let `docker build` fail fast on any error:
```dockerfile
# api/Dockerfile
RUN dotnet restore Nodefy.Api/Nodefy.Api.csproj
RUN dotnet publish Nodefy.Api/Nodefy.Api.csproj -c Release -o /out --no-restore

# frontend/Dockerfile
RUN npm ci
RUN npm run build
```
The `|| true` on `npm ci` in the deps stage is also unsafe — if `package-lock.json` is inconsistent the install silently falls back to `npm install`, which may change the lockfile and break reproducibility.

---

## Warnings

### WR-01: Race condition in slug uniqueness check (check-then-act)

**File:** `api/Nodefy.Api/Endpoints/WorkspaceEndpoints.cs:84-90`
**Issue:** `UniqueSlug` performs a read (`AnyAsync`) followed by a write (`SaveChangesAsync`) with no transaction or optimistic concurrency lock. Two concurrent `POST /workspaces` requests with the same name could both read `slug = "acme"`, find it does not exist, and both attempt to insert it. The `slug UNIQUE NOT NULL` constraint in the database will cause one request to throw an unhandled `DbUpdateException`, producing a 500 instead of a clean conflict response.
**Fix:** Wrap the workspace creation in a retry-on-unique-constraint pattern, or catch `DbUpdateException` / `PostgresException` with error code `23505` (unique violation) and return `Results.Conflict`. A simpler alternative is to append a short random suffix to the slug at creation time rather than using a sequential counter, making collisions statistically impossible:
```csharp
catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pg && pg.SqlState == "23505")
{
    return Results.Conflict(new { error = "A workspace with this name already exists." });
}
```

---

### WR-02: `POST /sso/sync` allows callers to register arbitrary provider identities

**File:** `api/Nodefy.Api/Endpoints/SsoSyncEndpoints.cs:15-44`
**Issue:** The `/sso/sync` endpoint is protected by `RequireAuthorization()`, meaning any authenticated JWT bearer can call it with any `provider` + `providerAccountId` pair. A compromised or forged JWT (e.g., minted with a stolen `AUTH_SECRET`) allows a caller to create or overwrite a user record with arbitrary provider credentials, effectively hijacking another user's account. There is no validation that the `provider`/`providerAccountId` in the request body matches the claims in the calling JWT.
**Fix:** Validate that `req.Provider` and `req.ProviderAccountId` match the claims the JWT was minted with. In `auth.ts`, the JWT already carries `token.provider` and `token.providerAccountId`. Include them in the `mintApiToken` call and validate server-side:
```csharp
// In SsoSyncEndpoints — compare req fields against JWT claims
var jwtProvider = user.HttpContext?.User?.FindFirst("provider")?.Value;
var jwtAccountId = user.HttpContext?.User?.FindFirst("providerAccountId")?.Value;
if (req.Provider != jwtProvider || req.ProviderAccountId != jwtAccountId)
    return Results.Forbid();
```
Alternatively, drop the request-body `provider`/`providerAccountId` fields entirely and derive them from the JWT claims, making spoofing structurally impossible.

---

### WR-03: Missing email format validation in `/sso/sync` — any string accepted

**File:** `api/Nodefy.Api/Endpoints/SsoSyncEndpoints.cs:17-18`
**Issue:** The endpoint checks `string.IsNullOrWhiteSpace(req.Email)` but accepts any non-empty string as a valid email. A caller can register a user with `email = "not-an-email"` or `email = "'; DROP TABLE users; --"`. While the latter is not an injection risk (EF Core parameterises everything), an invalid email breaks downstream logic that may assume the stored email is valid (e.g., future email-send flows, invitation matching by email).
**Fix:** Add a format check using `System.Net.Mail.MailAddress` or a regex:
```csharp
if (!System.Net.Mail.MailAddress.TryCreate(req.Email, out _))
    return Results.BadRequest(new { error = "Invalid email format" });
```

---

### WR-04: `GET /workspaces/{id}/members` — non-member can infer workspace existence

**File:** `api/Nodefy.Api/Endpoints/MemberEndpoints.cs:19-27`
**Issue:** The endpoint calls `WorkspaceEndpoints.IsAdmin(db, id, user.UserId)` which returns `false` for any user who is not an admin — including users who are not members of the workspace at all. In both cases the endpoint returns `403 Forbidden`. This means an authenticated user can infer that a workspace with a given ID exists (403 = "I know this workspace but you're not admin") vs. does not exist (would also return 403, so the oracle is limited). The more concrete issue: a legitimate non-admin member of the workspace gets a 403 when accessing the members list, but the `MemberTable.tsx` component fetches `/api/workspaces/${workspaceId}/members` on the client-side without the user's role context — a regular member visiting `/workspace/{id}/settings/members` will see a 403 response with no helpful UI message.
**Fix:** Separate "not a member" (404 or redirect to workspace select) from "is a member but not admin" (403). Returning 404 for non-members is safer because it avoids workspace-existence oracle. For the UX issue, `MemberTable.tsx` should handle the 403 gracefully in its `queryFn` instead of silently erroring.

---

### WR-05: `TenantMiddleware` accepts tenant from unauthenticated `X-Tenant-Id` header

**File:** `api/Nodefy.Api/Middleware/TenantMiddleware.cs:18-19`
**Issue:** The middleware resolves the active tenant from the `X-Tenant-Id` header even before authentication runs (middleware runs before `UseAuthentication` in the pipeline — actually authentication runs first in Program.cs, but the header is trusted unconditionally from any request, including unauthenticated ones). If an unauthenticated request reaches an endpoint that is not protected by `RequireAuthorization()` (e.g., `GET /invites/{token}` or `GET /health`), the `X-Tenant-Id` header sets the RLS session variable to an arbitrary tenant. While the RLS policy and EF Core global filters still enforce isolation at query time, this unnecessarily widens the trust surface and could cause confusion if future unauthenticated endpoints perform writes.
**Fix:** Only honour the `X-Tenant-Id` header when the request is authenticated. Check `ctx.User?.Identity?.IsAuthenticated == true` before accepting the header value:
```csharp
if (string.IsNullOrEmpty(claim) && ctx.User?.Identity?.IsAuthenticated == true
    && ctx.Request.Headers.TryGetValue("X-Tenant-Id", out var hdr))
    claim = hdr.ToString();
```

---

### WR-06: Next.js API routes for member operations return HTTP 500 for all backend errors

**File:** `frontend/src/app/api/workspaces/[id]/members/route.ts:12`, `frontend/src/app/api/workspaces/proxy/route.ts:10`
**Issue:** The `GET /api/workspaces/{id}/members` and `POST /api/workspaces/proxy` routes catch all errors and return `{ status: 500 }`, even when the backend returned a 403, 404, or 409. The `MemberTable.tsx` `queryFn` (line 17) calls `fetch(...).json()` without checking `res.ok`, meaning a 500 from the Next.js route is silently treated as data and may throw a JSON parse error or render corrupt state. The `members/route.ts` also does not pass through the backend status code.
**Fix:** In the GET members route, pass through the backend status code using `ApiError`:
```typescript
// frontend/src/app/api/workspaces/[id]/members/route.ts
import { apiFetch, ApiError } from "@/lib/api"

export async function GET(_req: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params
  try {
    const data = await apiFetch(`/workspaces/${id}/members`, { tenantId: id })
    return NextResponse.json(data)
  } catch (e: any) {
    const status = e instanceof ApiError ? e.status : 500
    return NextResponse.json({ error: e.message }, { status })
  }
}
```
In `MemberTable.tsx`, check `res.ok` before calling `.json()`:
```typescript
queryFn: async () => {
  const res = await fetch(`/api/workspaces/${workspaceId}/members`)
  if (!res.ok) throw new Error(`Failed to load members: ${res.status}`)
  return res.json()
},
```

---

## Info

### IN-01: `AUTH_SECRET` used as both Auth.js session secret and JWT signing key — shared secret risk

**File:** `api/Nodefy.Api/Auth/JwtConfig.cs:11`, `frontend/src/lib/api-token.ts:5`
**Issue:** The API falls back to `AUTH_SECRET` when `AUTH_JWT_SECRET` is not set (line 11 of JwtConfig.cs), and the frontend always signs tokens using `AUTH_SECRET`. This means the same secret signs Auth.js session cookies and the backend API JWTs. If the key is ever rotated on one side without updating the other, all active sessions break. Sharing a key between two cryptographic purposes also violates key separation best practices.
**Fix:** Set a separate `AUTH_JWT_SECRET` in both the `.env.example` and `docker-compose.yml`, and document that they must match. Remove the `?? cfg["AUTH_SECRET"]` fallback in production to enforce explicit configuration.

---

### IN-02: `BoardHub` accepts arbitrary `pipelineId` string for group join — no membership check

**File:** `api/Nodefy.Api/Hubs/BoardHub.cs:9-13`
**Issue:** `JoinBoard(string pipelineId)` adds the caller to a SignalR group named `pipeline:{pipelineId}` without verifying that the caller is a member of the workspace that owns that pipeline. Any authenticated user can join any pipeline's group and receive future broadcast messages for it. This is a data-leak risk that grows in Phase 2 when real card updates are broadcast.
**Fix:** Before `Groups.AddToGroupAsync`, validate that the caller is a member of the workspace owning the pipeline. Since pipeline-to-workspace mapping will live in the database in Phase 2, this can be deferred but the check must exist before any real-time card updates are broadcast.

---

### IN-03: `apiFetch` uses `(session as any).sub` — unsafe type cast, may use stale id

**File:** `frontend/src/lib/api.ts:10`
**Issue:** The token is minted with `sub: (session as any).sub ?? session.user.id ?? ""`. The `auth.ts` `jwt` callback sets `token.sub = user.id` (the canonical backend ID), and Auth.js exposes `token.sub` as `session.user.id` — not as `session.sub`. The `(session as any).sub` cast will likely be `undefined` in production, causing the code to always fall back to `session.user.id`, which is correct but the dead cast adds confusion and masks a type error.
**Fix:** Remove the ambiguous cast. The `jwt` callback in `auth.ts` already propagates the backend user ID to `token.sub`, which Auth.js surfaces as `session.user.id`. Use it directly:
```typescript
sub: session.user.id ?? "",
```

---

### IN-04: `Slug.Generate` — empty input returns empty string, which would fail DB `NOT NULL` constraint

**File:** `api/Nodefy.Api/Lib/Slug.cs:10-19`
**Issue:** If `name` is entirely composed of non-ASCII, non-alphanumeric characters (e.g., `"!!!"`), `trimmed` will be an empty string after the regex replacement and trim. The `UniqueSlug` loop then tries to insert `""` into the `slug NOT NULL` column. The slug uniqueness check `AnyAsync(w => w.Slug == slug)` with `slug = ""` would find no match on a fresh DB, so the workspace is saved with `slug = ""`, violating the spirit of the unique constraint and creating a record that cannot be navigated to by slug.
**Fix:** Add a guard after `trimmed`:
```csharp
if (trimmed.Length == 0) trimmed = "workspace";
return trimmed.Length > 50 ? trimmed[..50].TrimEnd('-') : trimmed;
```
The workspace name already has a server-side minimum-length check (`req.Name.Length < 2`), but that check does not guarantee a non-empty slug after stripping all non-alphanumeric characters.

---

### IN-05: `next-auth` pinned to `"beta"` — no reproducible version

**File:** `frontend/package.json:27`
**Issue:** `"next-auth": "beta"` is an unpinned dist-tag. Any `npm install` could pull a different (potentially breaking or vulnerable) beta version. The `package-lock.json` pins the current resolution, but after any `npm install --save` operation or a `package-lock.json` deletion, the resolved version could change silently.
**Fix:** Pin to the exact beta version currently in `package-lock.json`:
```json
"next-auth": "5.0.0-beta.25"
```
(Replace with the actual resolved version.) Check `package-lock.json` for the exact version and pin it explicitly.

---

_Reviewed: 2026-04-16T00:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
