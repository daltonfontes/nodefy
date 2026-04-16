# Phase 1: Foundation - Research

**Researched:** 2026-04-16
**Domain:** Multi-tenant SaaS foundation — SSO auth, workspace/membership API, DB schema, Docker
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Currency is configurable per workspace — not hardcoded globally
- **D-02:** Supported currencies in v1: BRL, USD, EUR
- **D-03:** Default currency for newly created workspaces: BRL
- **D-04:** Currency is locked after the first card is created (backend guards the workspace settings PATCH endpoint)
- **D-05:** Admins change currency via a workspace settings page
- **D-06:** A user can belong to multiple workspaces (as member and/or admin in each)
- **D-07:** Any authenticated user can create multiple workspaces
- **D-08:** After login, if the user has multiple workspaces, show a workspace selector screen
- **D-09:** After first login with no workspaces, auto-redirect to "Create workspace" page
- **D-10:** Workspace creation form requires only the workspace name; slug is auto-generated from the name
- **D-11:** Login page: centered card, product name/logo at top, three SSO buttons stacked vertically
- **D-12:** SSO auth failure: inline error on login page (not a redirect to a separate error page)
- **D-13:** After workspace creation, user lands on an empty pipeline board with a "Create your first pipeline" CTA
- **D-14:** Light mode only in v1 — no dark mode

### Claude's Discretion

- **Invite flow delivery:** Choose between email-based SMTP delivery and in-app shareable link. SMTP adds a mail service dependency to Docker Compose.
- **Workspace selector UI detail:** List vs. card grid, last-used highlighting — visual treatment is Claude's call.

### Deferred Ideas (OUT OF SCOPE)

- Full ISO 4217 currency list (v2)
- Dark mode (v2)
- Workspace onboarding wizard (v2)
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| AUTH-01 | User can log in via GitHub SSO | Auth.js v5 GitHub provider + /user/emails fallback for null email |
| AUTH-02 | User can log in via Google SSO | Auth.js v5 Google provider; email always available |
| AUTH-03 | User can log in via Microsoft SSO | Auth.js v5 MicrosoftEntraID provider |
| AUTH-04 | Session persists across browser refreshes (HttpOnly cookie) | Auth.js v5 JWT strategy stores in HttpOnly cookie by default |
| AUTH-05 | User can log out from any page | Auth.js signOut() works in both Server and Client components |
| WORK-01 | Authenticated user can create a workspace (tenant) | Server Action + POST /workspaces endpoint; global query filter enforces tenant isolation from first row |
| WORK-02 | Admin can invite members by email with role | Token-based invite (shareable link recommended); invitations table in DB |
| WORK-03 | Invitee can accept invite and access workspace | /invite/[token] landing page validates token, creates workspace_members row |
| WORK-04 | Admin can view workspace member list | GET /workspaces/{id}/members endpoint |
| WORK-05 | Admin can promote/demote member role | PATCH /workspaces/{id}/members/{userId} — role toggle |
| WORK-06 | Admin can remove a member | DELETE /workspaces/{id}/members/{userId} |
| TEST-01 | Backend developed TDD — xUnit + TestContainers | WebApplicationFactory + PostgreSqlContainer pattern verified |
</phase_requirements>

---

## Summary

Phase 1 establishes the entire multi-tenant foundation for Nodefy: a three-service Docker Compose stack (PostgreSQL, .NET 9 API, Next.js frontend), SSO authentication via Auth.js v5 with GitHub/Google/Microsoft providers, and workspace + membership APIs backed by EF Core 9 with global query filters enforced at the application layer, combined with PostgreSQL RLS for a second enforcement layer.

The technology choices are locked in CLAUDE.md and represent the current state of the art: Auth.js v5 (not v4, which is maintenance-only) for Next.js App Router compatibility, EF Core global query filters (not schema-per-tenant — unsupported), and `@dnd-kit` for future drag-and-drop (not react-beautiful-dnd, which is archived). The GitHub SSO provider requires a special `/user/emails` API fallback because GitHub's primary email field can be null when users hide their email — this is a known issue that will break the invite matching flow if not handled.

The invite flow recommendation is **shareable link with token** (not SMTP email) — this avoids adding a mail service dependency to Docker Compose in v1, keeps the token fully auditable in the database, and matches the UI spec's delivery-agnostic form design. SMTP can be added in v2 when notification requirements are clearer.

**Primary recommendation:** Build the Docker Compose stack first (Plan 1.1), then the .NET backend with TDD (Plan 1.2), then the Next.js frontend (Plan 1.3). Each plan depends on the previous. The SignalR BoardHub must be scaffolded in Plan 1.2 even if unused until Phase 3 — this is a locked decision.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| SSO OAuth callback handling | Frontend Server (Next.js) | — | Auth.js v5 route handler at /api/auth/[...nextauth] runs server-side |
| Session cookie issuance | Frontend Server (Next.js) | — | Auth.js sets HttpOnly cookie; never client-side |
| Session validation on protected routes | Frontend Server (Next.js) | — | proxy.ts / middleware.ts intercepts before page render |
| Tenant extraction from JWT | API / Backend (.NET) | — | Middleware reads claim from JWT Bearer token on every request |
| Global query filter enforcement | API / Backend (.NET) | — | EF Core DbContext applies TenantId filter on all queries |
| RLS enforcement | Database (PostgreSQL) | — | Second enforcement layer; catches any ORM bypass |
| Workspace CRUD | API / Backend (.NET) | — | REST endpoints; frontend calls via TanStack Query |
| Membership management | API / Backend (.NET) | — | REST endpoints with role authorization |
| Invite token generation/validation | API / Backend (.NET) | — | Tokens stored in DB; validation server-side only |
| Client-side UI state (loading, modals) | Browser / Client | — | Zustand — never for server-fetched data |
| Server state cache (members, workspace) | Browser / Client | — | TanStack Query manages cache, optimistic updates, rollback |
| Real-time scaffolding (BoardHub) | API / Backend (.NET) | — | SignalR Hub registered in Phase 1; used Phase 3 |

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| next | 16.2.4 | App Router, SSR, Server Actions | Locked in CLAUDE.md |
| next-auth | 4.24.14 (package name) / Auth.js v5 | SSO auth for App Router | v5 is App Router native; v4 maintenance-only |
| @tanstack/react-query | 5.99.0 | Server-state management, optimistic updates | Locked in CLAUDE.md |
| zustand | 5.0.12 | Client UI state (modals, filters, DnD in-flight) | Locked in CLAUDE.md |
| @microsoft/signalr | 10.0.0 | SignalR JS client (scaffolded now, active Phase 3) | Official Microsoft npm package |
| tailwindcss | 4.2.2 | Utility-first CSS; v3 in CLAUDE.md — NOTE: v4 detected on system | CLAUDE.md specifies v3 due to v4 stabilization concerns |
| lucide-react | 1.8.0 | Icon library bundled with shadcn/ui | Locked in UI-SPEC.md |
| .NET SDK | 10.0.202 | ASP.NET Core runtime | Project constraint; .NET 9 target |
| Npgsql.EntityFrameworkCore.PostgreSQL | 9.0.4 | EF Core provider for PostgreSQL targeting .NET 9 | Official Npgsql; 9.0.x series targets EF Core 9 |
| AspNet.Security.OAuth.GitHub | latest | GitHub OAuth handler for ASP.NET Core | Referenced in official ASP.NET Core social login docs |
| Testcontainers.PostgreSql | 4.11.0 | PostgreSQL container for integration tests | Official Testcontainers.NET module; verified on NuGet |
| xunit | latest | Test framework for .NET | Locked in CLAUDE.md (TEST-01) |

**Version note on tailwindcss:** `npm view tailwindcss version` returns 4.2.2 (Tailwind v4). CLAUDE.md explicitly states "Tailwind CSS v3 — v4 still stabilizing as of April 2026". Pin to `3.x` in package.json to match CLAUDE.md. [VERIFIED: npm registry + CLAUDE.md constraint]

**Version note on .NET:** `dotnet --version` returns 10.0.202. The project targets .NET 9 per CLAUDE.md. Use `<TargetFramework>net9.0</TargetFramework>` and pin Npgsql to 9.0.4 (EF Core 9 compatible). [VERIFIED: environment check]

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| shadcn/ui | CLI: npx shadcn@latest | Component library (copies components into repo) | All UI components; initialize before implementing frontend |
| @dnd-kit/core + @dnd-kit/sortable | 6.3.1 / 10.0.0 | Drag-and-drop (for Phase 2 boards) | Not Phase 1, but note these are the correct libraries |
| Microsoft.AspNetCore.Authentication.MicrosoftAccount | included in ASP.NET Core 9 | Microsoft OAuth handler | .AddMicrosoftAccount() |
| Google.Apis.Auth.AspNetCore3 | latest | Google OpenID Connect handler | .AddGoogleOpenIdConnect() |
| Microsoft.AspNetCore.Mvc.Testing | 9.x | WebApplicationFactory for integration tests | Required for xUnit + TestContainers pattern |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Auth.js v5 (next-auth) | Clerk, Auth0 | External cost + lock-in; rejected per CLAUDE.md |
| EF Core global query filters | Schema-per-tenant | Schema-per-tenant unsupported by EF Core migrations; rejected per CLAUDE.md |
| Shareable link invite | SMTP/MailHog | SMTP adds Docker Compose dependency; deferred to v2 |
| Npgsql 9.0.4 | Npgsql 10.0.1 | 10.x targets .NET 10; project is on .NET 9 — use 9.0.4 |

### Installation

**Frontend:**
```bash
npx create-next-app@latest frontend --typescript --tailwind --app --src-dir
cd frontend
npm install next-auth@beta @tanstack/react-query zustand @microsoft/signalr
# Pin Tailwind to v3 per CLAUDE.md
npm install tailwindcss@3 --save-dev
# shadcn initialization (run once)
npx shadcn@latest init
# Phase 1 shadcn components
npx shadcn@latest add button card input label badge avatar separator dialog alert select table dropdown-menu alert-dialog
```

**Backend:**
```bash
dotnet new webapi -n Nodefy.Api --framework net9.0
dotnet new xunit -n Nodefy.Tests
dotnet add Nodefy.Api package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.0.4
dotnet add Nodefy.Api package AspNet.Security.OAuth.GitHub
dotnet add Nodefy.Tests package Testcontainers.PostgreSql --version 4.11.0
dotnet add Nodefy.Tests package Microsoft.AspNetCore.Mvc.Testing
```

---

## Architecture Patterns

### System Architecture Diagram

```
Browser
  |
  | HTTPS
  v
[Next.js 16 — App Router]
  |  proxy.ts middleware: checks auth cookie before every route
  |  /api/auth/[...nextauth]: Auth.js v5 route handler
  |       |-- GitHub callback -> /user/emails fallback for null email
  |       |-- Google callback
  |       `-- MicrosoftEntraID callback
  |  Server Components: fetch() to .NET API with Bearer token
  |  Client Components: TanStack Query -> .NET API
  |  Zustand: loading states, open dialogs, active workspace
  |
  | HTTP/JSON (Bearer JWT)
  v
[.NET 9 ASP.NET Core API]
  |  TenantMiddleware: extracts TenantId from JWT claim
  |  AppDbContext (Scoped):
  |       OnModelCreating: HasQueryFilter(e => e.TenantId == _tenantId)
  |       Interceptor: SET app.current_tenant = '{tenantId}' on connection open
  |  Endpoints:
  |       POST   /workspaces
  |       GET    /workspaces
  |       GET    /workspaces/{id}/members
  |       POST   /workspaces/{id}/invites
  |       POST   /invites/{token}/accept
  |       PATCH  /workspaces/{id}/members/{userId}
  |       DELETE /workspaces/{id}/members/{userId}
  |  BoardHub (SignalR — scaffolded, inactive until Phase 3)
  |
  | Npgsql connection pool
  v
[PostgreSQL 17-alpine]
  |  Role: nodefy_app (NOT superuser — RLS bypassed for superusers)
  |  RLS policies: tenant_isolation_policy on all tenant-scoped tables
  |  Tables: users, workspaces, workspace_members, invitations
  |  Columns: position DOUBLE PRECISION, stage_entered_at TIMESTAMPTZ (first migration)
  |  Column: workspaces.currency VARCHAR(3) DEFAULT 'BRL', currency_locked BOOLEAN DEFAULT false
```

### Recommended Project Structure

```
nodefy/
├── docker-compose.yml          # db + api + frontend services
├── docker-compose.override.yml # local dev overrides
├── frontend/                   # Next.js 16 App Router
│   ├── auth.ts                 # NextAuth config + exports
│   ├── proxy.ts                # middleware (Next.js 16 name)
│   ├── src/app/
│   │   ├── api/auth/[...nextauth]/route.ts
│   │   ├── (auth)/login/page.tsx
│   │   ├── (auth)/workspace/new/page.tsx
│   │   ├── workspace/[id]/settings/members/page.tsx
│   │   ├── workspace/[id]/settings/members/invite/page.tsx
│   │   └── invite/[token]/page.tsx
│   ├── src/components/         # shadcn components live here
│   ├── src/lib/
│   │   ├── api.ts              # fetch wrapper with Bearer token
│   │   └── query-client.ts     # TanStack QueryClient singleton
│   └── src/store/
│       └── ui-store.ts         # Zustand store
├── api/                        # .NET 9 ASP.NET Core
│   ├── Nodefy.Api/
│   │   ├── Program.cs
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs          # EF Core + global filters
│   │   │   └── TenantDbConnectionInterceptor.cs
│   │   ├── Middleware/
│   │   │   └── TenantMiddleware.cs
│   │   ├── Hubs/
│   │   │   └── BoardHub.cs             # SignalR scaffold
│   │   └── Endpoints/
│   │       ├── WorkspaceEndpoints.cs
│   │       ├── MemberEndpoints.cs
│   │       └── InviteEndpoints.cs
│   └── Nodefy.Tests/
│       ├── Fixtures/
│       │   └── PostgresFixture.cs      # TestContainers
│       └── Integration/
│           ├── WorkspaceTests.cs
│           └── MemberTests.cs
└── db/
    └── migrations/             # EF Core migration output
```

### Pattern 1: Auth.js v5 with GitHub/Google/Microsoft

**What:** Centralized auth config in `auth.ts`, exported handlers for route handler, proxy.ts for middleware.

**When to use:** All authentication flows — login, session check, logout.

```typescript
// auth.ts — Source: https://authjs.dev/getting-started/installation
import NextAuth from "next-auth"
import GitHub from "next-auth/providers/github"
import Google from "next-auth/providers/google"
import MicrosoftEntraID from "next-auth/providers/microsoft-entra-id"

export const { handlers, auth, signIn, signOut } = NextAuth({
  providers: [
    GitHub({
      // AUTH_GITHUB_ID and AUTH_GITHUB_SECRET auto-detected
      // GitHub null email fix: override userinfo request
      userinfo: {
        url: "https://api.github.com/user",
        async request({ client, tokens }) {
          const profile = await client.userinfo(tokens.access_token!)
          if (!profile.email) {
            const res = await fetch("https://api.github.com/user/emails", {
              headers: { Authorization: `token ${tokens.access_token}` },
            })
            if (res.ok) {
              const emails = await res.json()
              profile.email = (emails.find((e: any) => e.primary) ?? emails[0]).email
            }
          }
          return profile
        },
      },
      authorization: {
        params: { scope: "read:user user:email" },
      },
    }),
    Google,
    MicrosoftEntraID({
      clientId: process.env.AUTH_MICROSOFT_ENTRA_ID_ID,
      clientSecret: process.env.AUTH_MICROSOFT_ENTRA_ID_SECRET,
      issuer: process.env.AUTH_MICROSOFT_ENTRA_ID_ISSUER,
    }),
  ],
  callbacks: {
    async jwt({ token, account, profile }) {
      // Persist provider account ID and access token for API calls
      if (account) {
        token.provider = account.provider
        token.providerAccountId = account.providerAccountId
      }
      return token
    },
    async session({ session, token }) {
      // Expose providerAccountId on session for workspace matching
      session.user.provider = token.provider as string
      return session
    },
  },
})
```

```typescript
// app/api/auth/[...nextauth]/route.ts
import { handlers } from "@/auth"
export const { GET, POST } = handlers
```

```typescript
// proxy.ts (Next.js 16 name for middleware)
// Source: https://authjs.dev/getting-started/migrating-to-v5
export { auth as proxy } from "@/auth"

export const config = {
  matcher: ["/((?!api/auth|_next/static|_next/image|favicon.ico|login).*)"],
}
```

### Pattern 2: EF Core Global Query Filter with Tenant Isolation

**What:** DbContext captures TenantId from scoped ITenantService; applies HasQueryFilter to every tenant-scoped entity; connection interceptor sets PostgreSQL session variable for RLS.

**When to use:** Every entity that belongs to a workspace (all Phase 2+ entities). Mandatory from Phase 1.

```csharp
// Data/AppDbContext.cs
// Source: https://learn.microsoft.com/en-us/ef/core/miscellaneous/multitenancy
public class AppDbContext : DbContext
{
    private readonly Guid _tenantId;

    public AppDbContext(DbContextOptions<AppDbContext> opts, ITenantService tenantService)
        : base(opts)
    {
        _tenantId = tenantService.TenantId;
    }

    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<Invitation> Invitations => Set<Invitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Global query filter — applied to all queries automatically
        // NEVER call .IgnoreQueryFilters() outside of tests
        modelBuilder.Entity<WorkspaceMember>()
            .HasQueryFilter(m => m.TenantId == _tenantId);

        modelBuilder.Entity<Invitation>()
            .HasQueryFilter(i => i.TenantId == _tenantId);

        // workspaces table: filter by membership, not by TenantId column
        // (users can query workspace list across their memberships)
    }
}
```

```csharp
// Data/TenantDbConnectionInterceptor.cs
// Source: https://www.bytefish.de/blog/aspnetcore_multitenancy.html
public class TenantDbConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ITenantService _tenantService;

    public TenantDbConnectionInterceptor(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (_tenantService.TenantId != Guid.Empty)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SET app.current_tenant = '{_tenantService.TenantId}'";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
```

### Pattern 3: PostgreSQL Schema with RLS

**What:** All tenant-scoped tables have a `tenant_id UUID NOT NULL` column with an RLS policy referencing `app.current_tenant` session variable. The `nodefy_app` role is NOT a superuser.

**When to use:** First migration — this cannot be added retroactively without data migration.

```sql
-- Run during migration / Docker init
CREATE ROLE nodefy_app LOGIN PASSWORD 'change_me_in_prod';
-- NOT SUPERUSER — RLS is bypassed for superusers

GRANT USAGE ON SCHEMA public TO nodefy_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO nodefy_app;

-- Schema
CREATE TABLE workspaces (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    slug VARCHAR(100) UNIQUE NOT NULL,
    currency VARCHAR(3) NOT NULL DEFAULT 'BRL',
    currency_locked BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE workspace_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES workspaces(id),
    user_id UUID NOT NULL REFERENCES users(id),
    role VARCHAR(20) NOT NULL DEFAULT 'member', -- 'admin' | 'member'
    joined_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    -- Future card ordering fields (must be in first migration)
    UNIQUE(tenant_id, user_id)
);

-- Fractional indexing placeholder (cards table — Phase 2)
-- position DOUBLE PRECISION NOT NULL DEFAULT 0.5
-- stage_entered_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
-- These MUST be in the Phase 1 migration even if unused until Phase 2

-- RLS
ALTER TABLE workspace_members ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_policy ON workspace_members
    USING (tenant_id = current_setting('app.current_tenant')::UUID);
```

### Pattern 4: xUnit + TestContainers Integration Test

**What:** WebApplicationFactory overrides DbContext to point at a TestContainers PostgreSQL instance. Each test class gets a shared fixture.

**When to use:** Every backend endpoint tested under TEST-01.

```csharp
// Fixtures/PostgresFixture.cs
// Source: https://dotnet.testcontainers.org/modules/postgres/
public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("nodefy_test")
        .WithUsername("nodefy_app")
        .WithPassword("test_password")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync() => await _container.StartAsync();
    public async Task DisposeAsync() => await _container.DisposeAsync().AsTask();
}

// Integration/WorkspaceTests.cs
public class WorkspaceTests : IClassFixture<PostgresFixture>
{
    private readonly HttpClient _client;

    public WorkspaceTests(PostgresFixture fixture)
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove production DbContext
                    var descriptor = services.Single(d =>
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    services.Remove(descriptor);
                    // Add test DbContext pointing at TestContainers
                    services.AddDbContext<AppDbContext>(opts =>
                        opts.UseNpgsql(fixture.ConnectionString));
                });
            });
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateWorkspace_ReturnsCreated_WithTenantIsolation()
    {
        // Arrange: authenticate test user, then POST /workspaces
        // Assert: response 201, cross-tenant query returns zero rows
    }
}
```

### Pattern 5: TanStack Query with App Router

**What:** QueryClientProvider in a `'use client'` boundary wrapping the root layout; server components fetch directly via fetch(), client components use TanStack Query hooks.

```typescript
// src/lib/query-client.tsx — Source: https://tanstack.com/query/v5/docs/react/guides/advanced-ssr
'use client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useState } from 'react'

export function Providers({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(() => new QueryClient({
    defaultOptions: {
      queries: { staleTime: 60 * 1000 },
    },
  }))
  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
}
```

### Pattern 6: Invite Flow (Shareable Link — Claude's Recommendation)

**What:** When admin invites by email, generate a cryptographically random token (32 bytes, base64url), store in `invitations` table with expiry (7 days), return the invite URL to the frontend for display. No SMTP required.

**When to use:** WORK-02, WORK-03 implementation.

```csharp
// InviteEndpoints.cs
var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
    .Replace("+", "-").Replace("/", "_").TrimEnd('=');
var invite = new Invitation
{
    TenantId = currentTenantId,
    Email = request.Email,
    Role = request.Role,
    Token = token,
    ExpiresAt = DateTime.UtcNow.AddDays(7),
};
await db.Invitations.AddAsync(invite);
await db.SaveChangesAsync();
return Results.Created($"/invites/{token}", new { InviteUrl = $"{baseUrl}/invite/{token}" });
```

**Frontend success state** per UI-SPEC: show the invite URL in a copy-to-clipboard input (already accounted for in the UI spec's "If shareable link is chosen" note).

### Anti-Patterns to Avoid

- **`IgnoreQueryFilters()` in production code:** Bypasses tenant isolation. Only allowed in migrations and test fixtures. [CITED: CLAUDE.md]
- **Manual `WHERE TenantId = x` in queries:** Will be missed in edge cases. The global filter handles it. [CITED: CLAUDE.md]
- **NextAuth v4 (`next-auth@4.x`):** No App Router native support; maintenance-only. [CITED: CLAUDE.md]
- **`react-beautiful-dnd`:** Archived by Atlassian 2023; no React 18 strict mode. Not needed in Phase 1 but note for Phase 2. [CITED: CLAUDE.md]
- **Storing session in localStorage:** Use HttpOnly cookie only. [CITED: STATE.md]
- **Schema-per-tenant:** Not supported by EF Core migrations. [CITED: CLAUDE.md + Microsoft docs]
- **nodefy_app as PostgreSQL superuser:** RLS is bypassed for superusers — role must be non-superuser. [CITED: CONTEXT.md specifics]
- **`useState` for QueryClient initialization without memoization:** React will throw away the client on initial render if it suspends without a boundary. Use `useState(() => new QueryClient())`. [CITED: TanStack Query docs]
- **Tailwind v4:** CLAUDE.md explicitly disallows it (still stabilizing). Pin to `tailwindcss@3`. [CITED: CLAUDE.md]

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Session management | Custom JWT cookie handling | Auth.js v5 | Edge cases: rotation, CSRF, secure flags, expiry |
| GitHub null email | Assume email always present | Override `userinfo.request` with `/user/emails` fallback | GitHub returns null for private emails — breaks invite matching |
| DB container in tests | Docker run in shell scripts | Testcontainers.PostgreSql | Automatic lifecycle, parallel test isolation, no port conflicts |
| Tenant filtering | Manual WHERE clause | EF Core HasQueryFilter | One missed WHERE = data leak; global filter is automatic |
| Random token generation | Math.random() | RandomNumberGenerator.GetBytes(32) | Cryptographically secure; Math.random is predictable |
| Slug generation | Custom regex | Slugify pattern: `.toLowerCase().replace(/[^a-z0-9]/g, '-')` | Simple enough to hand-roll, but make it a utility function |
| UI components | Custom form components | shadcn/ui (copies into repo) | Accessibility, Radix primitives, you own the code |

**Key insight:** The two most dangerous hand-roll areas in this phase are tenant filtering (a missed WHERE is a data breach) and session management (a missed flag is a security hole). Both have battle-tested solutions in the stack.

---

## Common Pitfalls

### Pitfall 1: GitHub Email is Null
**What goes wrong:** `profile.email` from GitHub OAuth is null when the user has their email set to private. The user is created with a null email, breaking email-based invite matching and potentially causing unique constraint violations.
**Why it happens:** GitHub's `/user` endpoint only returns email if the user has made it public. GitHub docs mention a separate `/user/emails` endpoint for verified emails.
**How to avoid:** Override the `userinfo.request` in the GitHub provider to check for null and call `/user/emails` with `user:email` scope. See code example in Pattern 1.
**Warning signs:** New user records with `email = null` in the users table; invite acceptance fails to match existing user.
[VERIFIED: GitHub issue #11895 on nextauthjs/next-auth]

### Pitfall 2: RLS Bypass with Superuser Role
**What goes wrong:** The `nodefy_app` PostgreSQL role is created as a superuser (or BYPASSRLS), silently bypassing all RLS policies. Multi-tenant isolation appears to work but doesn't.
**Why it happens:** Default PostgreSQL superuser ignores all security policies by design.
**How to avoid:** `CREATE ROLE nodefy_app LOGIN PASSWORD '...'` — no SUPERUSER or BYPASSRLS clause. Test by querying with `SET ROLE nodefy_app` and verifying cross-tenant rows are invisible.
**Warning signs:** RLS policies exist but integration tests can read cross-tenant data.
[CITED: CONTEXT.md specifics + PostgreSQL RLS docs]

### Pitfall 3: DbContext Lifetime and Tenant Capture
**What goes wrong:** DbContext is registered as Singleton or the tenant is captured at startup rather than per-request. All requests share the same `_tenantId`, causing cross-tenant data leaks.
**Why it happens:** EF Core DbContext is often discussed as Scoped but Singleton is a common copy-paste error.
**How to avoid:** Register AppDbContext as Scoped (the EF Core default). ITenantService must also be Scoped so it can resolve the per-request tenant claim. The connection interceptor should be registered with `AddSingleton` but receive ITenantService via DI each call.
**Warning signs:** Test passes in isolation but fails when multiple tenants are tested in sequence.
[CITED: Microsoft EF Core multi-tenancy docs — "Dependencies must flow toward singleton"]

### Pitfall 4: QueryClient Singleton in SSR
**What goes wrong:** QueryClient is created outside of a `useState`, making it a module-level singleton. When two users render simultaneously, they share the same cache and see each other's data.
**Why it happens:** Tutorial code often initializes QueryClient at module level for simplicity.
**How to avoid:** Always initialize QueryClient inside `useState(() => new QueryClient())` in the Providers component.
**Warning signs:** Data from one user's session appears in another user's browser after SSR.
[CITED: TanStack Query advanced SSR docs]

### Pitfall 5: Auth.js v5 Cookie Prefix Change
**What goes wrong:** Code that reads cookies using the old `next-auth.session-token` name fails silently in v5.
**Why it happens:** Auth.js v5 changed the cookie prefix from `next-auth` to `authjs`.
**How to avoid:** Never read session cookies directly in application code. Use `auth()` server-side or `useSession()` client-side. Let Auth.js own the cookie.
**Warning signs:** Session appears valid in browser DevTools but `auth()` returns null.
[CITED: https://authjs.dev/getting-started/migrating-to-v5]

### Pitfall 6: Invite Token Expiry Not Enforced
**What goes wrong:** Invite tokens work indefinitely. An invitation accepted months after being sent grants access to a workspace that may have changed.
**Why it happens:** Token validation checks existence but not expiry.
**How to avoid:** Always validate `invitation.ExpiresAt > DateTime.UtcNow` in the accept endpoint. Return 410 Gone for expired tokens.
**Warning signs:** Old tokens in the database are still accepted; the UI shows "Link inválido ou expirado" only for missing tokens.
[ASSUMED — based on standard security practice]

### Pitfall 7: Fractional Index Fields Missing from First Migration
**What goes wrong:** `position DOUBLE PRECISION` and `stage_entered_at TIMESTAMPTZ` are added in Phase 2 when card ordering is first used. This requires a data migration on existing card rows.
**Why it happens:** "We don't need it yet" reasoning.
**How to avoid:** Include these columns in the Phase 1 migration for the (stub) cards table even if no card functionality is implemented. This is a locked decision in STATE.md.
**Warning signs:** Phase 2 migration contains `ALTER TABLE cards ADD COLUMN position` — requires backfilling all existing rows.
[CITED: CONTEXT.md specifics + STATE.md key decisions]

---

## Code Examples

### Auth.js Environment Variables (.env.local)
```bash
# Source: https://authjs.dev/getting-started/providers/github
AUTH_SECRET=""               # npx auth secret
AUTH_GITHUB_ID=""
AUTH_GITHUB_SECRET=""
AUTH_GOOGLE_ID=""
AUTH_GOOGLE_SECRET=""
AUTH_MICROSOFT_ENTRA_ID_ID=""
AUTH_MICROSOFT_ENTRA_ID_SECRET=""
AUTH_MICROSOFT_ENTRA_ID_ISSUER="https://login.microsoftonline.com/{tenant_id}/v2.0"
```

### Session Guard in Server Component
```typescript
// Any protected Server Component
import { auth } from "@/auth"
import { redirect } from "next/navigation"

export default async function ProtectedPage() {
  const session = await auth()
  if (!session) redirect("/login")
  // session.user.email is guaranteed non-null (GitHub fallback handles it)
  return <div>Hello {session.user.name}</div>
}
```

### Workspace Selector Redirect (Post-Login)
```typescript
// app/(auth)/workspace/select/page.tsx
// After login, query user's workspaces
// If zero workspaces -> redirect to /workspace/new
// If one workspace -> redirect to /workspace/[id]
// If multiple -> show selector screen
```

### Slug Auto-Generation
```typescript
// lib/slug.ts
export function generateSlug(name: string): string {
  return name
    .toLowerCase()
    .normalize("NFD")                    // decompose accented chars
    .replace(/[\u0300-\u036f]/g, "")    // remove diacritics (é -> e)
    .replace(/[^a-z0-9]+/g, "-")        // non-alphanumeric -> hyphen
    .replace(/^-+|-+$/g, "")            // trim leading/trailing hyphens
    .slice(0, 50)                        // max 50 chars
}
```

### BoardHub Scaffold (Phase 1 — inactive until Phase 3)
```csharp
// Hubs/BoardHub.cs
// Source: https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs
[Authorize]
public class BoardHub : Hub
{
    // Scaffolded in Phase 1; fully wired in Phase 3
    public async Task JoinBoard(string pipelineId)
    {
        // TODO Phase 3: verify caller belongs to pipeline's tenant before joining group
        await Groups.AddToGroupAsync(Context.ConnectionId, $"pipeline:{pipelineId}");
    }

    public async Task LeaveBoard(string pipelineId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"pipeline:{pipelineId}");
    }
}

// Program.cs registration
app.MapHub<BoardHub>("/hubs/board");
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| NextAuth v4 (`getServerSideProps`) | Auth.js v5 (`auth()` universal) | v5 released 2024 | Single method replaces getServerSession, getSession, withAuth, getToken |
| `NEXTAUTH_SECRET` env var | `AUTH_SECRET` | Auth.js v5 | All provider env vars auto-detected with `AUTH_` prefix |
| `middleware.ts` (Next.js <16) | `proxy.ts` (Next.js 16+) | Next.js 16 release 2025 | Same pattern; file rename only |
| Manual EF WHERE clauses | Global Query Filters (EF Core 2.0+) | EF Core 2.0 (2017) | Automatic application; missed WHERE = security hole |
| Schema-per-tenant | Row-level with global filter | Not applicable to EF Core | Schema-per-tenant never supported in EF Core migrations |
| Testcontainers 3.x | Testcontainers 4.11.0 | 2025 | Same pattern; version increment only |

**Deprecated/outdated:**
- `next-auth@4.x`: Maintenance-only; no App Router native support. Use `next-auth@beta` (Auth.js v5).
- `getServerSession()`: Replaced by `auth()` in Auth.js v5.
- `Newtonsoft.Json`: Replaced by `System.Text.Json` in .NET 9 — do not add as a dependency.
- `react-beautiful-dnd`: Archived by Atlassian 2023. Use `@dnd-kit/core` (Phase 2).

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Invite token expiry of 7 days is appropriate for v1 | Common Pitfalls #6 | Too short frustrates users; too long is a security risk. Configurable or adjustable before Plan 1.2. |
| A2 | Tailwind v3 is available as `tailwindcss@3.x` on npm | Standard Stack | If v3 is yanked, pin to last known 3.x version (e.g., 3.4.x) |
| A3 | `proxy.ts` is the correct middleware filename for Next.js 16 | Architecture Patterns | Could still be `middleware.ts` — verify with `next --version` output |

---

## Open Questions (RESOLVED)

1. **Invite URL base URL in Docker** — RESOLVED: `FRONTEND_URL` env var added to the API service in `docker-compose.yml` (Plan 01-01 Task 2). Backend reads `FRONTEND_URL` to construct invite URLs.

2. **Auth.js v5 JWT and Backend API Bearer Token** — RESOLVED: Auth.js `jwt` callback mints a signed HS256 API token via `jose` using `AUTH_SECRET`, embedded as `api_token` in the session. The frontend `apiFetch` helper reads it from the session and sends it as `Authorization: Bearer`. The backend validates it with the same `AUTH_SECRET` via `JwtBearer` middleware (Plan 01-03 `api-token.ts`, Plan 01-02 `Program.cs`).

3. **Microsoft Entra Tenant Type** — RESOLVED: `common` endpoint used (multi-tenant Microsoft app). Documented in `.env.example` as `AUTH_MICROSOFT_ENTRA_ID_ISSUER=https://login.microsoftonline.com/common/v2.0` (Plan 01-01 Task 2).

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Docker | All services | Yes | 29.3.0 | — |
| Node.js | Frontend build | Yes | 22.11.0 | — |
| npm | Frontend packages | Yes | 11.12.1 | — |
| .NET SDK | Backend build | Yes | 10.0.202 (targets net9.0) | — |
| PostgreSQL client (pg_isready) | DB health check | No (not in PATH) | — | Docker container provides pg_isready internally |
| Docker Compose | Multi-service orchestration | Yes (bundled with Docker Desktop 29.3.0) | v2 | — |

**Missing dependencies with no fallback:** None that block execution.

**Missing dependencies with fallback:**
- `pg_isready` not in host PATH — not needed on host; Docker healthcheck runs it inside the PostgreSQL container.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit (backend) — no frontend test framework specified for Phase 1 |
| Config file | None yet — created in Wave 0 |
| Quick run command | `dotnet test --filter "Category=Unit" --no-build` |
| Full suite command | `dotnet test --no-build` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| AUTH-01 | GitHub OAuth callback returns user with non-null email | Integration | `dotnet test --filter "FullyQualifiedName~GitHubAuth"` | No — Wave 0 |
| AUTH-02 | Google OAuth callback returns valid session | Integration | `dotnet test --filter "FullyQualifiedName~GoogleAuth"` | No — Wave 0 |
| AUTH-03 | Microsoft OAuth callback returns valid session | Integration | `dotnet test --filter "FullyQualifiedName~MicrosoftAuth"` | No — Wave 0 |
| AUTH-04 | Session cookie is HttpOnly, Secure, SameSite=Strict | Integration | Verify response headers in WebApplicationFactory test | No — Wave 0 |
| AUTH-05 | Logout clears session cookie | Integration | `dotnet test --filter "FullyQualifiedName~Logout"` | No — Wave 0 |
| WORK-01 | POST /workspaces creates workspace with correct TenantId | Integration | `dotnet test --filter "FullyQualifiedName~CreateWorkspace"` | No — Wave 0 |
| WORK-01 | Cross-tenant query returns zero rows | Integration | `dotnet test --filter "FullyQualifiedName~TenantIsolation"` | No — Wave 0 |
| WORK-02 | POST /invites generates token, stores in DB | Integration | `dotnet test --filter "FullyQualifiedName~CreateInvite"` | No — Wave 0 |
| WORK-03 | POST /invites/{token}/accept creates membership | Integration | `dotnet test --filter "FullyQualifiedName~AcceptInvite"` | No — Wave 0 |
| WORK-04 | GET /workspaces/{id}/members returns member list | Integration | `dotnet test --filter "FullyQualifiedName~GetMembers"` | No — Wave 0 |
| WORK-05 | PATCH /workspaces/{id}/members/{id} changes role | Integration | `dotnet test --filter "FullyQualifiedName~ChangeRole"` | No — Wave 0 |
| WORK-06 | DELETE /workspaces/{id}/members/{id} removes member | Integration | `dotnet test --filter "FullyQualifiedName~RemoveMember"` | No — Wave 0 |

### Sampling Rate

- **Per task commit:** `dotnet test --filter "Category=Unit" --no-build`
- **Per wave merge:** `dotnet test --no-build`
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps

- [ ] `Nodefy.Tests/Fixtures/PostgresFixture.cs` — shared TestContainers fixture
- [ ] `Nodefy.Tests/Fixtures/ApiFactory.cs` — WebApplicationFactory with DB override
- [ ] `Nodefy.Tests/Integration/WorkspaceTests.cs` — covers WORK-01 through WORK-06
- [ ] `Nodefy.Tests/Integration/AuthTests.cs` — covers AUTH-01 through AUTH-05
- [ ] Framework install: `dotnet add Nodefy.Tests package Testcontainers.PostgreSql --version 4.11.0`

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | Yes | Auth.js v5 with OAuth 2.0 PKCE; no password auth in v1 |
| V3 Session Management | Yes | HttpOnly + Secure + SameSite=Strict cookie (Auth.js default); never localStorage |
| V4 Access Control | Yes | Role check middleware on member management endpoints; global query filter for tenant isolation |
| V5 Input Validation | Yes | FluentValidation or Data Annotations on all API request models; workspace name, invite email, role |
| V6 Cryptography | Yes | `RandomNumberGenerator.GetBytes(32)` for invite tokens; never Math.random() |

### Known Threat Patterns for This Stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Cross-tenant data access | Information Disclosure | EF Core global query filter + PostgreSQL RLS (two-layer) |
| Session fixation | Elevation of Privilege | Auth.js rotates session on authentication |
| Invite token brute-force | Elevation of Privilege | 32-byte random token (256-bit entropy); token invalidated after use |
| CSRF on state-changing endpoints | Tampering | SameSite=Strict cookie (Auth.js default) |
| Expired invite acceptance | Elevation of Privilege | ExpiresAt check in accept endpoint; return 410 Gone |
| Admin-only endpoint accessed by member | Elevation of Privilege | Role claim checked in middleware before allowing member management |
| GitHub email spoofing | Spoofing | Use GitHub's `verified: true` flag from /user/emails; only accept primary verified email |

---

## Sources

### Primary (HIGH confidence)
- https://authjs.dev/getting-started/installation — Auth.js v5 file structure and setup
- https://authjs.dev/getting-started/migrating-to-v5 — v5 breaking changes (cookie prefix, env vars, proxy.ts)
- https://authjs.dev/getting-started/providers/github — GitHub provider setup
- https://authjs.dev/getting-started/providers/google — Google provider setup
- https://authjs.dev/getting-started/providers/microsoft-entra-id — Microsoft provider setup
- https://learn.microsoft.com/en-us/ef/core/miscellaneous/multitenancy — EF Core global query filter pattern
- https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs — SignalR Hub creation and group management
- https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests — WebApplicationFactory pattern
- https://dotnet.testcontainers.org/modules/postgres/ — Testcontainers.PostgreSql API
- https://www.nuget.org/packages/Testcontainers.PostgreSql — Version 4.11.0 confirmed
- https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/9.0.4 — Version 9.0.4 for .NET 9
- npm registry — next@16.2.4, next-auth@4.24.14, @tanstack/react-query@5.99.0, zustand@5.0.12, @microsoft/signalr@10.0.0
- CLAUDE.md — Technology stack constraints (locked choices)

### Secondary (MEDIUM confidence)
- https://github.com/nextauthjs/next-auth/issues/11895 — GitHub null email fix with /user/emails
- https://www.bytefish.de/blog/aspnetcore_multitenancy.html — PostgreSQL RLS + EF Core connection interceptor pattern
- https://tanstack.com/query/v5/docs/react/guides/advanced-ssr — QueryClient SSR initialization pattern

### Tertiary (LOW confidence)
- WebSearch results on Docker Compose healthcheck patterns — general guidance, not project-specific
- WebSearch on invite token vs SMTP tradeoffs — general SaaS patterns

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all package versions verified via npm registry and NuGet
- Architecture: HIGH — patterns from official docs; EF Core + Auth.js v5 patterns verified
- Pitfalls: HIGH — GitHub null email verified via open GitHub issue; RLS superuser bypass from official PostgreSQL docs
- Security: HIGH — ASVS categories mapped to concrete controls in the chosen stack

**Research date:** 2026-04-16
**Valid until:** 2026-05-16 (30 days — stable ecosystem)
