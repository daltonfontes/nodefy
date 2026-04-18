# Phase 1: Foundation - Pattern Map

**Mapped:** 2026-04-16
**Files analyzed:** 28 (new files — greenfield project)
**Analogs found:** 0 / 28 (no existing codebase — all patterns sourced from RESEARCH.md canonical references)

> **Greenfield note:** The project has zero source files. Every pattern in this document is derived from
> RESEARCH.md (which cites official docs and verified library patterns). Phase 1 establishes the
> canonical patterns that all subsequent phases will copy from.

---

## File Classification

| New File | Role | Data Flow | Closest Analog | Match Quality |
|----------|------|-----------|----------------|---------------|
| `docker-compose.yml` | config | — | none | no-analog |
| `docker-compose.override.yml` | config | — | none | no-analog |
| `db/init.sql` | migration | batch | none | no-analog |
| `frontend/auth.ts` | config | request-response | none | no-analog |
| `frontend/proxy.ts` | middleware | request-response | none | no-analog |
| `frontend/src/app/api/auth/[...nextauth]/route.ts` | route | request-response | none | no-analog |
| `frontend/src/app/(auth)/login/page.tsx` | component | request-response | none | no-analog |
| `frontend/src/app/(auth)/workspace/new/page.tsx` | component | request-response | none | no-analog |
| `frontend/src/app/(auth)/workspace/select/page.tsx` | component | request-response | none | no-analog |
| `frontend/src/app/workspace/[id]/settings/members/page.tsx` | component | CRUD | none | no-analog |
| `frontend/src/app/workspace/[id]/settings/members/invite/page.tsx` | component | request-response | none | no-analog |
| `frontend/src/app/invite/[token]/page.tsx` | component | request-response | none | no-analog |
| `frontend/src/lib/api.ts` | utility | request-response | none | no-analog |
| `frontend/src/lib/query-client.tsx` | provider | request-response | none | no-analog |
| `frontend/src/lib/slug.ts` | utility | transform | none | no-analog |
| `frontend/src/store/ui-store.ts` | store | event-driven | none | no-analog |
| `api/Nodefy.Api/Program.cs` | config | — | none | no-analog |
| `api/Nodefy.Api/Data/AppDbContext.cs` | service | CRUD | none | no-analog |
| `api/Nodefy.Api/Data/TenantDbConnectionInterceptor.cs` | middleware | request-response | none | no-analog |
| `api/Nodefy.Api/Middleware/TenantMiddleware.cs` | middleware | request-response | none | no-analog |
| `api/Nodefy.Api/Hubs/BoardHub.cs` | service | event-driven | none | no-analog |
| `api/Nodefy.Api/Endpoints/WorkspaceEndpoints.cs` | controller | CRUD | none | no-analog |
| `api/Nodefy.Api/Endpoints/MemberEndpoints.cs` | controller | CRUD | none | no-analog |
| `api/Nodefy.Api/Endpoints/InviteEndpoints.cs` | controller | request-response | none | no-analog |
| `api/Nodefy.Tests/Fixtures/PostgresFixture.cs` | test | batch | none | no-analog |
| `api/Nodefy.Tests/Fixtures/ApiFactory.cs` | test | request-response | none | no-analog |
| `api/Nodefy.Tests/Integration/WorkspaceTests.cs` | test | CRUD | none | no-analog |
| `api/Nodefy.Tests/Integration/AuthTests.cs` | test | request-response | none | no-analog |

---

## Pattern Assignments

### `frontend/auth.ts` (config, request-response)

**Source:** RESEARCH.md Pattern 1 — Auth.js v5 official docs (https://authjs.dev/getting-started/installation)

**Full pattern:**
```typescript
import NextAuth from "next-auth"
import GitHub from "next-auth/providers/github"
import Google from "next-auth/providers/google"
import MicrosoftEntraID from "next-auth/providers/microsoft-entra-id"

export const { handlers, auth, signIn, signOut } = NextAuth({
  providers: [
    GitHub({
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
              profile.email = (emails.find((e: any) => e.primary && e.verified) ?? emails[0]).email
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
    async jwt({ token, account }) {
      if (account) {
        token.provider = account.provider
        token.providerAccountId = account.providerAccountId
      }
      return token
    },
    async session({ session, token }) {
      session.user.provider = token.provider as string
      return session
    },
  },
})
```

**Critical notes:**
- GitHub `scope: "read:user user:email"` is required — without it, `/user/emails` returns 404
- Only accept emails where `verified: true` from `/user/emails` — prevents spoofing
- `AUTH_SECRET`, `AUTH_GITHUB_ID`, `AUTH_GITHUB_SECRET`, etc. are auto-detected from `.env.local`
- Cookie prefix is `authjs` in v5 (not `next-auth`) — never read cookies directly; use `auth()`

---

### `frontend/proxy.ts` (middleware, request-response)

**Source:** RESEARCH.md Pattern 1 — Auth.js v5 migration docs (https://authjs.dev/getting-started/migrating-to-v5)

**Full pattern:**
```typescript
// proxy.ts — Next.js 16 renames middleware.ts to proxy.ts
export { auth as proxy } from "@/auth"

export const config = {
  // Exclude: API auth routes, static assets, login page
  matcher: ["/((?!api/auth|_next/static|_next/image|favicon.ico|login|invite).*)"],
}
```

**Critical notes:**
- File is `proxy.ts` (Next.js 16), not `middleware.ts` (Next.js <16) — verify with installed next version
- `invite` route excluded from auth check so unauthenticated users can land on the accept page

---

### `frontend/src/app/api/auth/[...nextauth]/route.ts` (route, request-response)

**Source:** RESEARCH.md Pattern 1

```typescript
import { handlers } from "@/auth"
export const { GET, POST } = handlers
```

---

### `frontend/src/app/(auth)/login/page.tsx` (component, request-response)

**Source:** RESEARCH.md D-11, D-12 decisions + Auth.js `signIn()` pattern

**Structure pattern:**
```typescript
'use client'
import { signIn } from "next-auth/react"

// Per D-11: centered card, product logo at top, 3 SSO buttons stacked
// Per D-12: SSO auth failure shown inline (searchParams.error from ?error= query param)
export default function LoginPage({
  searchParams,
}: {
  searchParams: { error?: string }
}) {
  return (
    <div className="flex min-h-screen items-center justify-center">
      <div className="w-full max-w-sm space-y-6 rounded-lg border p-8 shadow-sm">
        {/* Logo + product name */}
        {searchParams.error && (
          <div className="rounded bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {/* Inline error — no redirect per D-12 */}
          </div>
        )}
        <div className="space-y-3">
          <button onClick={() => signIn("github", { callbackUrl: "/workspace/select" })}>
            Continue with GitHub
          </button>
          <button onClick={() => signIn("google", { callbackUrl: "/workspace/select" })}>
            Continue with Google
          </button>
          <button onClick={() => signIn("microsoft-entra-id", { callbackUrl: "/workspace/select" })}>
            Continue with Microsoft
          </button>
        </div>
      </div>
    </div>
  )
}
```

**shadcn components to use:** `Button`, `Card`, `CardContent`, `Alert`

---

### `frontend/src/app/(auth)/workspace/select/page.tsx` (component, request-response)

**Source:** RESEARCH.md D-08, D-09 decisions

**Structure pattern:**
```typescript
// Server Component — reads session + fetches workspaces server-side
import { auth } from "@/auth"
import { redirect } from "next/navigation"

export default async function WorkspaceSelectPage() {
  const session = await auth()
  if (!session) redirect("/login")

  const workspaces = await fetchUserWorkspaces(session) // GET /workspaces

  if (workspaces.length === 0) redirect("/workspace/new") // D-09
  if (workspaces.length === 1) redirect(`/workspace/${workspaces[0].id}`) // fast path

  // D-08: show selector when multiple workspaces
  // Claude's discretion: card grid with workspace name + member count + last-used highlight
  return <WorkspaceSelectorGrid workspaces={workspaces} />
}
```

**shadcn components to use:** `Card`, `CardHeader`, `CardTitle`, `Badge`

---

### `frontend/src/app/(auth)/workspace/new/page.tsx` (component, request-response)

**Source:** RESEARCH.md D-10, D-13 decisions

**Structure pattern:**
```typescript
'use client'
// D-10: form requires only workspace name; slug auto-generated
// D-13: after creation, redirect to empty pipeline board with CTA
import { generateSlug } from "@/lib/slug"
import { useMutation } from "@tanstack/react-query"

export default function NewWorkspacePage() {
  const [name, setName] = useState("")
  const slug = generateSlug(name) // live preview of auto-slug
  const mutation = useMutation({
    mutationFn: (data: { name: string }) => api.post("/workspaces", data),
    onSuccess: (workspace) => router.push(`/workspace/${workspace.id}`),
  })
  // ...
}
```

**shadcn components to use:** `Button`, `Card`, `Input`, `Label`

---

### `frontend/src/app/workspace/[id]/settings/members/page.tsx` (component, CRUD)

**Source:** RESEARCH.md WORK-04, WORK-05, WORK-06

**Structure pattern:**
```typescript
// Server Component for initial data; Client Component for mutations
import { auth } from "@/auth"

// Data fetching: GET /workspaces/{id}/members
// Mutations (client): PATCH role, DELETE member
// shadcn Table component for member list
// Role badge: 'admin' | 'member'
// Dropdown menu per row: "Make admin" / "Make member" / "Remove"
```

**shadcn components to use:** `Table`, `TableHeader`, `TableRow`, `TableCell`, `Badge`, `DropdownMenu`, `AlertDialog`

---

### `frontend/src/app/workspace/[id]/settings/members/invite/page.tsx` (component, request-response)

**Source:** RESEARCH.md Pattern 6 — shareable link invite (Claude's recommendation)

**Structure pattern:**
```typescript
'use client'
// POST /workspaces/{id}/invites -> returns { inviteUrl: string }
// After creation: show inviteUrl in a copy-to-clipboard Input
// shadcn: Input (read-only) + Button "Copy link"
import { useMutation } from "@tanstack/react-query"
import { Select } from "@/components/ui/select" // role: admin | member

export default function InvitePage() {
  const mutation = useMutation({
    mutationFn: (data: { email: string; role: string }) =>
      api.post(`/workspaces/${id}/invites`, data),
    onSuccess: (data) => setInviteUrl(data.inviteUrl), // show copy-to-clipboard
  })
}
```

**shadcn components to use:** `Button`, `Input`, `Label`, `Select`, `Card`

---

### `frontend/src/app/invite/[token]/page.tsx` (component, request-response)

**Source:** RESEARCH.md WORK-03 + Pattern 6

**Structure pattern:**
```typescript
// Server Component: validate token server-side via GET /invites/{token}
// If valid: show workspace name + "Accept Invite" button
// If expired/invalid: show error message (no redirect)
// Accept: POST /invites/{token}/accept -> redirect to workspace
import { auth } from "@/auth"
import { redirect } from "next/navigation"

export default async function InviteAcceptPage({ params }: { params: { token: string } }) {
  // If not authenticated -> redirect to login with callbackUrl = current invite URL
  const session = await auth()
  if (!session) redirect(`/login?callbackUrl=/invite/${params.token}`)
  // Validate token; render accept form or error
}
```

**shadcn components to use:** `Button`, `Card`, `Alert`

---

### `frontend/src/lib/api.ts` (utility, request-response)

**Source:** RESEARCH.md Architecture — "fetch wrapper with Bearer token"

**Full pattern:**
```typescript
// Fetch wrapper that attaches the Auth.js session token as Bearer
// Used by both Server Components (direct fetch) and TanStack Query (client)
import { auth } from "@/auth"

export async function apiFetch(path: string, init?: RequestInit) {
  const session = await auth()
  const baseUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000"
  return fetch(`${baseUrl}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(session ? { Authorization: `Bearer ${session.user.apiToken}` } : {}),
      ...init?.headers,
    },
  })
}

// Client-safe version (no auth() — for use in 'use client' components via TanStack Query)
export const api = {
  get: (path: string) => fetch(`${process.env.NEXT_PUBLIC_API_URL}${path}`),
  post: (path: string, body: unknown) =>
    fetch(`${process.env.NEXT_PUBLIC_API_URL}${path}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    }).then((r) => r.json()),
}
```

---

### `frontend/src/lib/query-client.tsx` (provider, request-response)

**Source:** RESEARCH.md Pattern 5 — TanStack Query SSR (https://tanstack.com/query/v5/docs/react/guides/advanced-ssr)

**Full pattern:**
```typescript
'use client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useState } from 'react'

// CRITICAL: useState(() => new QueryClient()) — NOT module-level singleton
// Module-level singleton causes cross-user cache sharing in SSR (Pitfall 4)
export function Providers({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(() => new QueryClient({
    defaultOptions: {
      queries: { staleTime: 60 * 1000 },
    },
  }))
  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
}
```

---

### `frontend/src/lib/slug.ts` (utility, transform)

**Source:** RESEARCH.md "Don't Hand-Roll" section

**Full pattern:**
```typescript
export function generateSlug(name: string): string {
  return name
    .toLowerCase()
    .normalize("NFD")                    // decompose accented chars
    .replace(/[\u0300-\u036f]/g, "")    // strip diacritics (é -> e, ç -> c)
    .replace(/[^a-z0-9]+/g, "-")        // non-alphanumeric -> hyphen
    .replace(/^-+|-+$/g, "")            // trim leading/trailing hyphens
    .slice(0, 50)                        // max 50 chars (matches DB VARCHAR(50))
}
```

---

### `frontend/src/store/ui-store.ts` (store, event-driven)

**Source:** RESEARCH.md Stack — Zustand 5 (client UI state only; NOT server-fetched data)

**Full pattern:**
```typescript
import { create } from 'zustand'

// RULE: Only local UI state here. Server-fetched data lives in TanStack Query.
interface UIState {
  activeWorkspaceId: string | null
  isInviteDialogOpen: boolean
  setActiveWorkspace: (id: string) => void
  openInviteDialog: () => void
  closeInviteDialog: () => void
}

export const useUIStore = create<UIState>((set) => ({
  activeWorkspaceId: null,
  isInviteDialogOpen: false,
  setActiveWorkspace: (id) => set({ activeWorkspaceId: id }),
  openInviteDialog: () => set({ isInviteDialogOpen: true }),
  closeInviteDialog: () => set({ isInviteDialogOpen: false }),
}))
```

---

### `api/Nodefy.Api/Program.cs` (config)

**Source:** RESEARCH.md Architecture + Pattern 2 (EF Core) + BoardHub scaffold

**Structure pattern:**
```csharp
var builder = WebApplication.CreateBuilder(args);

// EF Core with Npgsql — Scoped lifetime (CRITICAL: never Singleton — Pitfall 3)
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .AddInterceptors(new TenantDbConnectionInterceptor(...)));

// ITenantService — Scoped (resolves per-request TenantId from JWT claim)
builder.Services.AddScoped<ITenantService, TenantService>();

// JWT Bearer auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts => { /* validate Auth.js-issued tokens */ });

// SignalR
builder.Services.AddSignalR();

// CORS for Next.js frontend
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration["FRONTEND_URL"] ?? "http://localhost:3000")
              .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantMiddleware>();

// Minimal API endpoint registration
app.MapWorkspaceEndpoints();
app.MapMemberEndpoints();
app.MapInviteEndpoints();

// SignalR — scaffolded Phase 1, active Phase 3
app.MapHub<BoardHub>("/hubs/board");

app.Run();
```

---

### `api/Nodefy.Api/Data/AppDbContext.cs` (service, CRUD)

**Source:** RESEARCH.md Pattern 2 — EF Core global query filters (https://learn.microsoft.com/en-us/ef/core/miscellaneous/multitenancy)

**Full pattern:**
```csharp
public class AppDbContext : DbContext
{
    private readonly Guid _tenantId;

    // ITenantService is Scoped — resolved per request
    // AppDbContext is Scoped — NEVER register as Singleton (Pitfall 3)
    public AppDbContext(DbContextOptions<AppDbContext> opts, ITenantService tenantService)
        : base(opts)
    {
        _tenantId = tenantService.TenantId;
    }

    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Global query filters — NEVER call .IgnoreQueryFilters() in production code
        modelBuilder.Entity<WorkspaceMember>()
            .HasQueryFilter(m => m.TenantId == _tenantId);

        modelBuilder.Entity<Invitation>()
            .HasQueryFilter(i => i.TenantId == _tenantId);

        // Workspaces: filtered by membership (user sees workspaces they belong to)
        // No direct TenantId column on workspaces; join through WorkspaceMembers
    }
}
```

---

### `api/Nodefy.Api/Data/TenantDbConnectionInterceptor.cs` (middleware, request-response)

**Source:** RESEARCH.md Pattern 2 — connection interceptor for PostgreSQL RLS session variable

**Full pattern:**
```csharp
public class TenantDbConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ITenantService _tenantService;

    // ITenantService injected — resolves per-request tenant
    public TenantDbConnectionInterceptor(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        // Sets PostgreSQL session variable consumed by RLS policies
        // ONLY set if tenant is known (skip for unauthenticated requests like /health)
        if (_tenantService.TenantId != Guid.Empty)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SET app.current_tenant = '{_tenantService.TenantId}'";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
```

---

### `api/Nodefy.Api/Middleware/TenantMiddleware.cs` (middleware, request-response)

**Source:** RESEARCH.md Architecture — "TenantMiddleware: extracts TenantId from JWT claim"

**Full pattern:**
```csharp
public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        // Extract TenantId from JWT claim (set by frontend when making API calls)
        var tenantClaim = context.User.FindFirst("tenant_id");
        if (tenantClaim is not null && Guid.TryParse(tenantClaim.Value, out var tenantId))
        {
            tenantService.SetTenant(tenantId);
        }
        await _next(context);
    }
}

// ITenantService contract
public interface ITenantService
{
    Guid TenantId { get; }
    void SetTenant(Guid tenantId);
}

// Scoped implementation
public class TenantService : ITenantService
{
    public Guid TenantId { get; private set; } = Guid.Empty;
    public void SetTenant(Guid tenantId) => TenantId = tenantId;
}
```

---

### `api/Nodefy.Api/Hubs/BoardHub.cs` (service, event-driven)

**Source:** RESEARCH.md Code Examples — BoardHub scaffold (https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs)

**Full pattern:**
```csharp
// Scaffolded Phase 1 — fully wired Phase 3
// [Authorize] ensures only authenticated clients connect
[Authorize]
public class BoardHub : Hub
{
    public async Task JoinBoard(string pipelineId)
    {
        // TODO Phase 3: verify caller belongs to pipeline's tenant before joining
        await Groups.AddToGroupAsync(Context.ConnectionId, $"pipeline:{pipelineId}");
    }

    public async Task LeaveBoard(string pipelineId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"pipeline:{pipelineId}");
    }
}
```

---

### `api/Nodefy.Api/Endpoints/WorkspaceEndpoints.cs` (controller, CRUD)

**Source:** RESEARCH.md Architecture endpoints list + minimal API pattern (.NET 9)

**Structure pattern:**
```csharp
public static class WorkspaceEndpoints
{
    public static IEndpointRouteBuilder MapWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/workspaces").RequireAuthorization();

        // WORK-01: POST /workspaces — creates workspace + first WorkspaceMember (admin)
        group.MapPost("/", async (CreateWorkspaceRequest req, AppDbContext db, ITenantService tenant) =>
        {
            // slug = auto-generated from req.Name (mirrors frontend lib/slug.ts logic)
            var workspace = new Workspace { Name = req.Name, Slug = GenerateSlug(req.Name), Currency = "BRL" };
            db.Workspaces.Add(workspace);
            // Add creator as admin member
            db.WorkspaceMembers.Add(new WorkspaceMember { TenantId = workspace.Id, UserId = currentUserId, Role = "admin" });
            await db.SaveChangesAsync();
            return Results.Created($"/workspaces/{workspace.Id}", workspace);
        });

        // GET /workspaces — lists workspaces the authenticated user belongs to
        group.MapGet("/", async (AppDbContext db, ITenantService tenant) =>
            Results.Ok(await db.Workspaces.ToListAsync()));

        // PATCH /workspaces/{id}/settings — D-05: admin changes currency
        // Guard: if workspace.CurrencyLocked == true -> return 409 Conflict (D-04)
        group.MapPatch("/{id}/settings", ...);

        return app;
    }
}
```

**Request/Response models (inline or separate DTOs file):**
```csharp
public record CreateWorkspaceRequest(string Name);
public record UpdateWorkspaceSettingsRequest(string Currency);
```

---

### `api/Nodefy.Api/Endpoints/MemberEndpoints.cs` (controller, CRUD)

**Source:** RESEARCH.md WORK-04, WORK-05, WORK-06

**Structure pattern:**
```csharp
public static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/workspaces/{workspaceId}/members").RequireAuthorization();

        // WORK-04: GET members list — admin only
        group.MapGet("/", async (Guid workspaceId, AppDbContext db) =>
            Results.Ok(await db.WorkspaceMembers.Where(m => m.TenantId == workspaceId).ToListAsync()));

        // WORK-05: PATCH role — admin only; cannot demote self if last admin
        group.MapPatch("/{userId}", async (Guid workspaceId, Guid userId, UpdateRoleRequest req, AppDbContext db) =>
        {
            var member = await db.WorkspaceMembers.FirstOrDefaultAsync(m => m.TenantId == workspaceId && m.UserId == userId);
            if (member is null) return Results.NotFound();
            member.Role = req.Role;
            await db.SaveChangesAsync();
            return Results.Ok(member);
        });

        // WORK-06: DELETE member — admin only; cannot remove self
        group.MapDelete("/{userId}", async (Guid workspaceId, Guid userId, AppDbContext db) =>
        {
            var member = await db.WorkspaceMembers.FirstOrDefaultAsync(m => m.TenantId == workspaceId && m.UserId == userId);
            if (member is null) return Results.NotFound();
            db.WorkspaceMembers.Remove(member);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }
}
```

---

### `api/Nodefy.Api/Endpoints/InviteEndpoints.cs` (controller, request-response)

**Source:** RESEARCH.md Pattern 6 — shareable link invite (Claude's recommendation)

**Full pattern:**
```csharp
public static class InviteEndpoints
{
    public static IEndpointRouteBuilder MapInviteEndpoints(this IEndpointRouteBuilder app)
    {
        // WORK-02: POST /workspaces/{id}/invites — admin only
        app.MapPost("/workspaces/{workspaceId}/invites",
            async (Guid workspaceId, CreateInviteRequest req, AppDbContext db, IConfiguration config) =>
        {
            // Cryptographically secure token — NEVER use Math.Random()
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');
            var invite = new Invitation
            {
                TenantId = workspaceId,
                Email = req.Email,
                Role = req.Role,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(7), // A1: 7 days — adjustable
            };
            await db.Invitations.AddAsync(invite);
            await db.SaveChangesAsync();
            var frontendUrl = config["FRONTEND_URL"] ?? "http://localhost:3000";
            return Results.Created($"/invites/{token}", new { InviteUrl = $"{frontendUrl}/invite/{token}" });
        }).RequireAuthorization();

        // WORK-03: POST /invites/{token}/accept
        app.MapPost("/invites/{token}/accept",
            async (string token, AppDbContext db, ITenantService tenant) =>
        {
            // Use .IgnoreQueryFilters() here ONLY — invite tokens are cross-tenant by design
            var invite = await db.Invitations.IgnoreQueryFilters()
                .FirstOrDefaultAsync(i => i.Token == token);

            if (invite is null) return Results.NotFound(new { error = "Convite não encontrado" });
            // Pitfall 6: always validate expiry
            if (invite.ExpiresAt < DateTime.UtcNow) return Results.StatusCode(410); // Gone

            // Create WorkspaceMember, mark invite as used
            db.WorkspaceMembers.Add(new WorkspaceMember { TenantId = invite.TenantId, UserId = currentUserId, Role = invite.Role });
            invite.AcceptedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(new { WorkspaceId = invite.TenantId });
        }).RequireAuthorization();

        return app;
    }
}
```

---

### `api/Nodefy.Tests/Fixtures/PostgresFixture.cs` (test, batch)

**Source:** RESEARCH.md Pattern 4 — TestContainers (https://dotnet.testcontainers.org/modules/postgres/)

**Full pattern:**
```csharp
public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("nodefy_test")
        .WithUsername("nodefy_app")       // matches production role name
        .WithPassword("test_password")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync() => await _container.StartAsync();
    public async Task DisposeAsync() => await _container.DisposeAsync().AsTask();
}
```

---

### `api/Nodefy.Tests/Fixtures/ApiFactory.cs` (test, request-response)

**Source:** RESEARCH.md Pattern 4 — WebApplicationFactory

**Full pattern:**
```csharp
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public ApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove production DbContext registration
            var descriptor = services.Single(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            services.Remove(descriptor);

            // Replace with TestContainers connection
            services.AddDbContext<AppDbContext>(opts =>
                opts.UseNpgsql(_connectionString));
        });
    }
}
```

---

### `api/Nodefy.Tests/Integration/WorkspaceTests.cs` (test, CRUD)

**Source:** RESEARCH.md Pattern 4 + Validation Architecture test map

**Structure pattern:**
```csharp
public class WorkspaceTests : IClassFixture<PostgresFixture>
{
    private readonly HttpClient _client;

    public WorkspaceTests(PostgresFixture fixture)
    {
        _client = new ApiFactory(fixture.ConnectionString).CreateClient();
    }

    [Fact]
    public async Task CreateWorkspace_ReturnsCreated_WithTenantIsolation()
    {
        // POST /workspaces as TenantA
        // Assert 201 + workspace returned
        // Query as TenantB -> assert 0 rows (global filter + RLS)
    }

    // Tests covering: WORK-01 through WORK-06 per Validation Architecture table
}
```

---

### `api/Nodefy.Tests/Integration/AuthTests.cs` (test, request-response)

**Source:** RESEARCH.md Validation Architecture — AUTH-01 through AUTH-05

**Structure pattern:**
```csharp
public class AuthTests : IClassFixture<PostgresFixture>
{
    // Tests covering:
    // AUTH-01: GitHub OAuth returns user with non-null email (uses /user/emails fallback)
    // AUTH-04: Session cookie headers include HttpOnly + Secure + SameSite=Strict
    // AUTH-05: Logout clears session cookie
}
```

---

### `docker-compose.yml` (config)

**Source:** RESEARCH.md Architecture + Environment Availability section

**Structure pattern:**
```yaml
services:
  db:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: nodefy
      POSTGRES_USER: nodefy_app     # NOT superuser — see Pitfall 2
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    healthcheck:
      test: ["CMD", "pg_isready", "-U", "nodefy_app", "-d", "nodefy"]
      interval: 5s
      retries: 5
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./db/init.sql:/docker-entrypoint-initdb.d/init.sql

  api:
    build: ./api
    environment:
      ConnectionStrings__DefaultConnection: "Host=db;Database=nodefy;Username=nodefy_app;Password=${DB_PASSWORD}"
      FRONTEND_URL: ${FRONTEND_URL:-http://localhost:3000}
    depends_on:
      db:
        condition: service_healthy
    ports:
      - "5000:5000"

  frontend:
    build: ./frontend
    environment:
      NEXT_PUBLIC_API_URL: http://localhost:5000
      AUTH_SECRET: ${AUTH_SECRET}
      AUTH_GITHUB_ID: ${AUTH_GITHUB_ID}
      AUTH_GITHUB_SECRET: ${AUTH_GITHUB_SECRET}
      AUTH_GOOGLE_ID: ${AUTH_GOOGLE_ID}
      AUTH_GOOGLE_SECRET: ${AUTH_GOOGLE_SECRET}
      AUTH_MICROSOFT_ENTRA_ID_ID: ${AUTH_MICROSOFT_ENTRA_ID_ID}
      AUTH_MICROSOFT_ENTRA_ID_SECRET: ${AUTH_MICROSOFT_ENTRA_ID_SECRET}
      AUTH_MICROSOFT_ENTRA_ID_ISSUER: ${AUTH_MICROSOFT_ENTRA_ID_ISSUER}
    depends_on:
      - api
    ports:
      - "3000:3000"

volumes:
  postgres_data:
```

---

### `db/init.sql` (migration, batch)

**Source:** RESEARCH.md Pattern 3 — PostgreSQL schema with RLS

**Full pattern:**
```sql
-- Role setup — CRITICAL: NOT SUPERUSER (Pitfall 2: RLS bypassed for superusers)
-- Note: In Docker Compose, POSTGRES_USER already creates the role.
-- Additional grants needed:
GRANT USAGE ON SCHEMA public TO nodefy_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO nodefy_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO nodefy_app;

-- Users table (global — no tenant_id)
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    name VARCHAR(100),
    avatar_url TEXT,
    provider VARCHAR(20) NOT NULL,           -- 'github' | 'google' | 'microsoft'
    provider_account_id VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(provider, provider_account_id)
);

-- Workspaces table (tenant root)
CREATE TABLE workspaces (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    slug VARCHAR(50) UNIQUE NOT NULL,
    currency VARCHAR(3) NOT NULL DEFAULT 'BRL',     -- D-01, D-02, D-03
    currency_locked BOOLEAN NOT NULL DEFAULT false,  -- D-04: locked after first card
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- WorkspaceMembers table (tenant-scoped)
CREATE TABLE workspace_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role VARCHAR(20) NOT NULL DEFAULT 'member',      -- 'admin' | 'member'
    joined_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(tenant_id, user_id)
);

-- Invitations table (tenant-scoped)
CREATE TABLE invitations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    email VARCHAR(255) NOT NULL,
    role VARCHAR(20) NOT NULL DEFAULT 'member',
    token VARCHAR(255) UNIQUE NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    accepted_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Cards stub table — Phase 2 implements fully, but columns MUST be here (Pitfall 7)
CREATE TABLE cards (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    -- Fractional indexing: MUST be in first migration — not retrofittable (CONTEXT.md specifics)
    position DOUBLE PRECISION NOT NULL DEFAULT 0.5,
    stage_entered_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
    -- Remaining columns added in Phase 2 migration
);

-- Enable RLS on tenant-scoped tables
ALTER TABLE workspace_members ENABLE ROW LEVEL SECURITY;
ALTER TABLE invitations ENABLE ROW LEVEL SECURITY;
ALTER TABLE cards ENABLE ROW LEVEL SECURITY;

-- RLS policies — reference session variable set by TenantDbConnectionInterceptor
CREATE POLICY tenant_isolation_policy ON workspace_members
    USING (tenant_id = current_setting('app.current_tenant')::UUID);

CREATE POLICY tenant_isolation_policy ON invitations
    USING (tenant_id = current_setting('app.current_tenant')::UUID);

CREATE POLICY tenant_isolation_policy ON cards
    USING (tenant_id = current_setting('app.current_tenant')::UUID);
```

---

## Shared Patterns

### Authentication Guard (Frontend — Server Components)
**Apply to:** All page.tsx Server Components in `app/workspace/` routes
```typescript
import { auth } from "@/auth"
import { redirect } from "next/navigation"

// At top of every protected Server Component page
const session = await auth()
if (!session) redirect("/login")
// session.user.email is guaranteed non-null (GitHub fallback in auth.ts handles it)
```

### Authentication Guard (Backend — Minimal API)
**Apply to:** All endpoint groups in `Endpoints/*.cs`
```csharp
// On every endpoint group that requires authentication
var group = app.MapGroup("/workspaces").RequireAuthorization();
// Admin-only endpoints add policy check inline:
// .RequireAuthorization(policy => policy.RequireClaim("role", "admin"))
```

### Error Response Pattern (Backend)
**Apply to:** All endpoint handlers in `Endpoints/*.cs`
```csharp
// Use Results.* return type — not throw exceptions in minimal API handlers
if (entity is null) return Results.NotFound(new { error = "Not found" });
if (!authorized) return Results.Forbid();
if (expired) return Results.StatusCode(410); // Gone — for expired invite tokens
// On validation failure:
return Results.ValidationProblem(errors);
```

### TanStack Query Mutation Pattern (Frontend — Client Components)
**Apply to:** All `'use client'` form components that POST/PATCH/DELETE
```typescript
'use client'
import { useMutation, useQueryClient } from '@tanstack/react-query'

const queryClient = useQueryClient()
const mutation = useMutation({
  mutationFn: (data: RequestType) => api.post("/endpoint", data),
  onSuccess: () => {
    // Invalidate relevant queries to refresh server state
    queryClient.invalidateQueries({ queryKey: ["workspaces"] })
  },
  onError: (error) => {
    // Show inline error — no redirect (per D-12 pattern)
  },
})
```

### DbContext Registration (Critical Lifetime Rule)
**Apply to:** `Program.cs`
```csharp
// SCOPED (default for EF Core) — NEVER Singleton
// Singleton DbContext = shared _tenantId across all requests = data breach (Pitfall 3)
builder.Services.AddDbContext<AppDbContext>(opts => ...); // Scoped by default
builder.Services.AddScoped<ITenantService, TenantService>(); // Must also be Scoped
```

### IgnoreQueryFilters Rule
**Apply to:** `InviteEndpoints.cs` (only allowed exception in production code)
```csharp
// The ONLY place .IgnoreQueryFilters() is allowed in production:
// Invite acceptance — tokens are cross-tenant by design
var invite = await db.Invitations.IgnoreQueryFilters()
    .FirstOrDefaultAsync(i => i.Token == token && i.ExpiresAt > DateTime.UtcNow);
// All other queries MUST use the global filter. No exceptions.
```

---

## No Analog Found

All Phase 1 files have no existing analog — the project is greenfield. The table below summarizes
which RESEARCH.md pattern each file should use as its primary reference:

| File | Role | Data Flow | Primary Reference |
|------|------|-----------|-------------------|
| `docker-compose.yml` | config | — | RESEARCH.md Architecture Diagram |
| `db/init.sql` | migration | batch | RESEARCH.md Pattern 3 |
| `frontend/auth.ts` | config | request-response | RESEARCH.md Pattern 1 |
| `frontend/proxy.ts` | middleware | request-response | RESEARCH.md Pattern 1 |
| `frontend/src/app/api/auth/[...nextauth]/route.ts` | route | request-response | RESEARCH.md Pattern 1 |
| `frontend/src/app/(auth)/login/page.tsx` | component | request-response | RESEARCH.md D-11, D-12 |
| `frontend/src/app/(auth)/workspace/new/page.tsx` | component | request-response | RESEARCH.md D-10, D-13 |
| `frontend/src/app/(auth)/workspace/select/page.tsx` | component | request-response | RESEARCH.md D-08, D-09 |
| `frontend/src/app/workspace/[id]/settings/members/page.tsx` | component | CRUD | RESEARCH.md WORK-04–06 |
| `frontend/src/app/workspace/[id]/settings/members/invite/page.tsx` | component | request-response | RESEARCH.md Pattern 6 |
| `frontend/src/app/invite/[token]/page.tsx` | component | request-response | RESEARCH.md WORK-03 |
| `frontend/src/lib/api.ts` | utility | request-response | RESEARCH.md Architecture |
| `frontend/src/lib/query-client.tsx` | provider | request-response | RESEARCH.md Pattern 5 |
| `frontend/src/lib/slug.ts` | utility | transform | RESEARCH.md "Don't Hand-Roll" |
| `frontend/src/store/ui-store.ts` | store | event-driven | RESEARCH.md Stack (Zustand) |
| `api/Nodefy.Api/Program.cs` | config | — | RESEARCH.md Architecture |
| `api/Nodefy.Api/Data/AppDbContext.cs` | service | CRUD | RESEARCH.md Pattern 2 |
| `api/Nodefy.Api/Data/TenantDbConnectionInterceptor.cs` | middleware | request-response | RESEARCH.md Pattern 2 |
| `api/Nodefy.Api/Middleware/TenantMiddleware.cs` | middleware | request-response | RESEARCH.md Architecture |
| `api/Nodefy.Api/Hubs/BoardHub.cs` | service | event-driven | RESEARCH.md Code Examples |
| `api/Nodefy.Api/Endpoints/WorkspaceEndpoints.cs` | controller | CRUD | RESEARCH.md Architecture |
| `api/Nodefy.Api/Endpoints/MemberEndpoints.cs` | controller | CRUD | RESEARCH.md WORK-04–06 |
| `api/Nodefy.Api/Endpoints/InviteEndpoints.cs` | controller | request-response | RESEARCH.md Pattern 6 |
| `api/Nodefy.Tests/Fixtures/PostgresFixture.cs` | test | batch | RESEARCH.md Pattern 4 |
| `api/Nodefy.Tests/Fixtures/ApiFactory.cs` | test | request-response | RESEARCH.md Pattern 4 |
| `api/Nodefy.Tests/Integration/WorkspaceTests.cs` | test | CRUD | RESEARCH.md Pattern 4 |
| `api/Nodefy.Tests/Integration/AuthTests.cs` | test | request-response | RESEARCH.md Pattern 4 |

---

## Metadata

**Analog search scope:** Entire `D:/projetos/nodefy/` directory
**Source files found:** 1 (`CLAUDE.md`) + 4 planning files (no source code)
**Pattern extraction date:** 2026-04-16
**Pattern source:** RESEARCH.md canonical references (official docs, verified library patterns)
**Phase establishes:** These patterns ARE the canonical patterns for all subsequent phases
