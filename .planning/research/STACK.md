# Technology Stack — Nodefy

**Project:** Nodefy (SaaS CRM with visual pipelines)
**Researched:** 2026-04-16
**Stack:** Next.js + C# (.NET 9) + PostgreSQL (Docker) + SSO-only auth

---

## Summary

The stack is fixed by the user. This document specifies **which libraries to use within each layer**, how they fit together, and what to avoid.

---

## Layer 1 — Frontend (Next.js)

### Authentication

**Use: `next-auth` v5 (Auth.js)**

- Package: `next-auth@5`
- Native providers: `GitHub`, `Google`, `MicrosoftEntraID`
- App Router compatible
- Auth.js v5 replaced NextAuth.js v4; v4 is maintenance-only

```ts
import NextAuth from "next-auth"
import GitHub from "next-auth/providers/github"
import Google from "next-auth/providers/google"
import MicrosoftEntraID from "next-auth/providers/microsoft-entra-id"

export const { handlers, signIn, signOut, auth } = NextAuth({
  providers: [GitHub, Google, MicrosoftEntraID],
})
```

**Do NOT use:** NextAuth v4, Clerk, Auth0.

**Confidence:** HIGH

---

### Drag-and-Drop

**Use: `@dnd-kit/core` + `@dnd-kit/sortable` + `@dnd-kit/utilities`**

- React-native, no jQuery
- Cross-list drag, keyboard accessibility, touch support
- React 18 / Next.js App Router compatible

```bash
npm install @dnd-kit/core @dnd-kit/sortable @dnd-kit/utilities
```

**Do NOT use:**
- `react-beautiful-dnd` — archived/unmaintained since 2023; no React 18 strict mode support
- `react-dnd` — complex setup, poor mobile/touch
- Native HTML5 DnD API — no touch, cumbersome cross-list logic

**Confidence:** HIGH

---

### Server-State / Data Fetching

**Use: TanStack Query v5 (`@tanstack/react-query@5`)**

- Manages async server state: pipelines, cards, members, comments
- Provides optimistic updates — critical for smooth drag-and-drop
- `QueryClientProvider` in client boundary wrapper for App Router

**Do NOT use:**
- Redux/Zustand for server state — client stores, not server-state solutions
- SWR — fewer features, no mutation tracking, no optimistic rollback

**Confidence:** HIGH

---

### Real-Time Client

**Use: `@microsoft/signalr`**

- Official Microsoft npm package for ASP.NET Core SignalR
- WebSocket with automatic fallback: SSE → Long Polling
- `.withAutomaticReconnect()` for reconnection

```ts
import * as signalR from "@microsoft/signalr"

const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/board")
  .withAutomaticReconnect()
  .build()

connection.on("CardMoved", (payload) => { /* update UI */ })
await connection.start()
```

**Confidence:** HIGH

---

### UI Components

**Use: shadcn/ui + Tailwind CSS v3**

- shadcn/ui copies components into repo (you own the code)
- Tailwind CSS v3 — v4 still stabilizing as of April 2026
- No runtime CSS-in-JS; works with RSC boundaries

**Do NOT use:** MUI, Chakra UI — runtime CSS-in-JS conflicts with RSC/App Router.

**Confidence:** MEDIUM

---

### Client UI State

**Use: Zustand**

- Only for local UI state: drag-in-progress, open modals, active pipeline, active filters
- Do NOT use for server-fetched data (TanStack Query owns that)

**Confidence:** HIGH

---

## Layer 2 — Backend (ASP.NET Core / .NET 9)

### OAuth SSO

**Google:**
- Package: `Google.Apis.Auth.AspNetCore3`
- Method: `.AddGoogleOpenIdConnect(...)`
- Confidence: HIGH (official Microsoft docs, updated 2026-04-09)

**Microsoft (Entra ID / personal accounts):**
- Package: `Microsoft.AspNetCore.Authentication.MicrosoftAccount`
- Method: `.AddMicrosoftAccount(...)`
- Confidence: HIGH (official Microsoft docs, updated 2026-04-07)

**GitHub:**
- Package: `AspNet.Security.OAuth.GitHub` (aspnet-contrib)
- Method: `.AddGitHub(...)`
- Confidence: HIGH (referenced in official ASP.NET Core social login overview)

```csharp
builder.Services.AddAuthentication(options =>
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme)
  .AddCookie()
  .AddGoogleOpenIdConnect(o => {
      o.ClientId = config["Auth:Google:ClientId"];
      o.ClientSecret = config["Auth:Google:ClientSecret"];
  })
  .AddMicrosoftAccount(o => {
      o.ClientId = config["Auth:Microsoft:ClientId"];
      o.ClientSecret = config["Auth:Microsoft:ClientSecret"];
  })
  .AddGitHub(o => {
      o.ClientId = config["Auth:GitHub:ClientId"];
      o.ClientSecret = config["Auth:GitHub:ClientSecret"];
  });
```

**Recommended SSO flow:**
1. User hits `/api/auth/callback/{provider}` after OAuth consent
2. Backend upserts user keyed on provider `sub`
3. Associates with workspace (tenant)
4. Issues short-lived JWT + refresh token stored in `httpOnly` cookie

---

### Real-Time (SignalR Hubs)

**Use: ASP.NET Core SignalR (built-in — no extra NuGet)**

```csharp
builder.Services.AddSignalR();
app.MapHub<BoardHub>("/hubs/board");
```

Use SignalR **Groups** for tenant isolation: each pipeline is group `pipeline:{id}`.

```csharp
public class BoardHub : Hub
{
    public async Task JoinPipeline(string pipelineId)
    {
        // Validate pipelineId against user's JWT claims first
        await Groups.AddToGroupAsync(Context.ConnectionId, $"pipeline:{pipelineId}");
    }
}
```

**Important:** Group membership is in-memory — client must rejoin on reconnection.

**Confidence:** HIGH

---

### ORM and Database Access

**Use: Entity Framework Core 9 + Npgsql**

```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
```

Code-First with migrations.

**Do NOT use:** Dapper alone (no tenant query filtering), NHibernate (outdated).

---

### Multi-Tenancy Strategy

**Decision: Row-Level Tenancy with EF Core Global Query Filters**

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> opts, ITenantService tenant)
    : DbContext(opts)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pipeline>()
            .HasQueryFilter(p => p.WorkspaceId == tenant.CurrentWorkspaceId);
        modelBuilder.Entity<Card>()
            .HasQueryFilter(c => c.WorkspaceId == tenant.CurrentWorkspaceId);
    }
}
```

Register with `ServiceLifetime.Scoped`.

**Critical:**
- Never call `.IgnoreQueryFilters()` in normal code paths
- Never manually add `WHERE TenantId = x` — the global filter handles it

---

### API Serialization

**Use: `System.Text.Json` (built-in in .NET 9)**

```csharp
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
```

**Do NOT use:** Newtonsoft.Json (redundant and slower).

---

### Validation

**Use: `FluentValidation.AspNetCore`** — cleaner than DataAnnotations for complex business rules.

---

## Layer 3 — Database (PostgreSQL via Docker)

**PostgreSQL 16 (`postgres:16-alpine`)**

```yaml
services:
  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: nodefy
      POSTGRES_USER: nodefy
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - pgdata:/var/lib/postgresql/data
```

- Alpine image keeps container size minimal
- Npgsql's built-in connection pool handles pooling for v1
- Run EF Core migrations via Docker entrypoint on startup

---

## Layer 4 — Docker Infrastructure

```
docker-compose.yml
├── db       (postgres:16-alpine)
├── api      (.NET 9 ASP.NET Core — multi-stage Dockerfile)
└── web      (Next.js standalone output — multi-stage Dockerfile)
```

**Next.js must have `output: "standalone"` in `next.config.ts`** for Docker deployment.

---

## Installation Reference

### Frontend

```bash
npm install next-auth@5
npm install @dnd-kit/core @dnd-kit/sortable @dnd-kit/utilities
npm install @tanstack/react-query@5
npm install @microsoft/signalr
npm install zustand
npm install tailwindcss postcss autoprefixer
npx tailwindcss init -p
npx shadcn@latest init
```

### Backend

```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Microsoft.AspNetCore.Authentication.MicrosoftAccount
dotnet add package Google.Apis.Auth.AspNetCore3
dotnet add package AspNet.Security.OAuth.GitHub
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package FluentValidation.AspNetCore
```

---

## What NOT to Use — Summary

| Category | Avoid | Reason |
|----------|-------|--------|
| Auth (frontend) | NextAuth v4 | No App Router native support; maintenance mode |
| Auth (frontend) | Clerk, Auth0 | External cost + lock-in for SSO-only case |
| Drag & Drop | react-beautiful-dnd | Archived by Atlassian 2023; no React 18 strict mode |
| UI Components | MUI, Chakra | Runtime CSS-in-JS breaks RSC boundaries |
| ORM | NHibernate | Outdated; EF Core 9 supersedes |
| Serialization | Newtonsoft.Json | Replaced by System.Text.Json in .NET 9 |
| Real-time | Raw WebSockets | SignalR adds reconnect, groups, fallback at no cost |
| Multi-tenancy | Schema-per-tenant | Unsupported by EF Core migrations |
| Multi-tenancy | Manual per-query filtering | Guaranteed to miss cases; use Global Query Filters |
| Server state | Redux for API data | Not a server-state solution; use TanStack Query |

---

## Sources

- ASP.NET Core social login: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/
- Google login: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins
- Microsoft account login: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/microsoft-logins
- GitHub OAuth provider: https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers
- ASP.NET Core SignalR: https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction
- SignalR JS client: https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client
- EF Core multi-tenancy: https://learn.microsoft.com/en-us/ef/core/miscellaneous/multitenancy
- Auth.js v5: https://authjs.dev
