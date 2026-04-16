<!-- GSD:project-start source:PROJECT.md -->
## Project

**Nodefy**

Nodefy é um CRM SaaS multi-tenant com pipelines visuais no estilo Pipefy. Cada empresa cria seu próprio workspace com pipelines customizáveis, cards de negócios e gestão de times. O sistema é voltado para equipes de vendas e operações que precisam visualizar e mover deals entre estágios de forma colaborativa.

**Core Value:** Qualquer membro de um workspace consegue ver e mover cards entre estágios do pipeline em tempo real, sem atrito.

### Constraints

- **Stack Frontend**: Next.js — escolha do usuário
- **Stack Backend**: C# (.NET) — escolha do usuário
- **Banco de dados**: PostgreSQL via Docker — escolha do usuário
- **Autenticação**: SSO apenas (GitHub, Google, Microsoft) — sem auth própria no v1
- **Deployment**: Docker-first — containerização desde o início
- **Multi-tenancy**: Isolamento por tenant no banco (schema ou row-level)
<!-- GSD:project-end -->

<!-- GSD:stack-start source:research/STACK.md -->
## Technology Stack

## Summary
## Layer 1 — Frontend (Next.js)
### Authentication
- Package: `next-auth@5`
- Native providers: `GitHub`, `Google`, `MicrosoftEntraID`
- App Router compatible
- Auth.js v5 replaced NextAuth.js v4; v4 is maintenance-only
### Drag-and-Drop
- React-native, no jQuery
- Cross-list drag, keyboard accessibility, touch support
- React 18 / Next.js App Router compatible
- `react-beautiful-dnd` — archived/unmaintained since 2023; no React 18 strict mode support
- `react-dnd` — complex setup, poor mobile/touch
- Native HTML5 DnD API — no touch, cumbersome cross-list logic
### Server-State / Data Fetching
- Manages async server state: pipelines, cards, members, comments
- Provides optimistic updates — critical for smooth drag-and-drop
- `QueryClientProvider` in client boundary wrapper for App Router
- Redux/Zustand for server state — client stores, not server-state solutions
- SWR — fewer features, no mutation tracking, no optimistic rollback
### Real-Time Client
- Official Microsoft npm package for ASP.NET Core SignalR
- WebSocket with automatic fallback: SSE → Long Polling
- `.withAutomaticReconnect()` for reconnection
### UI Components
- shadcn/ui copies components into repo (you own the code)
- Tailwind CSS v3 — v4 still stabilizing as of April 2026
- No runtime CSS-in-JS; works with RSC boundaries
### Client UI State
- Only for local UI state: drag-in-progress, open modals, active pipeline, active filters
- Do NOT use for server-fetched data (TanStack Query owns that)
## Layer 2 — Backend (ASP.NET Core / .NET 9)
### OAuth SSO
- Package: `Google.Apis.Auth.AspNetCore3`
- Method: `.AddGoogleOpenIdConnect(...)`
- Confidence: HIGH (official Microsoft docs, updated 2026-04-09)
- Package: `Microsoft.AspNetCore.Authentication.MicrosoftAccount`
- Method: `.AddMicrosoftAccount(...)`
- Confidence: HIGH (official Microsoft docs, updated 2026-04-07)
- Package: `AspNet.Security.OAuth.GitHub` (aspnet-contrib)
- Method: `.AddGitHub(...)`
- Confidence: HIGH (referenced in official ASP.NET Core social login overview)
### Real-Time (SignalR Hubs)
### ORM and Database Access
### Multi-Tenancy Strategy
- Never call `.IgnoreQueryFilters()` in normal code paths
- Never manually add `WHERE TenantId = x` — the global filter handles it
### API Serialization
### Validation
## Layer 3 — Database (PostgreSQL via Docker)
- Alpine image keeps container size minimal
- Npgsql's built-in connection pool handles pooling for v1
- Run EF Core migrations via Docker entrypoint on startup
## Layer 4 — Docker Infrastructure
## Installation Reference
### Frontend
### Backend
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
## Sources
- ASP.NET Core social login: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/
- Google login: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins
- Microsoft account login: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/microsoft-logins
- GitHub OAuth provider: https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers
- ASP.NET Core SignalR: https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction
- SignalR JS client: https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client
- EF Core multi-tenancy: https://learn.microsoft.com/en-us/ef/core/miscellaneous/multitenancy
- Auth.js v5: https://authjs.dev
<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->
## Conventions

Conventions not yet established. Will populate as patterns emerge during development.
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->
## Architecture

Architecture not yet mapped. Follow existing patterns found in the codebase.
<!-- GSD:architecture-end -->

<!-- GSD:skills-start source:skills/ -->
## Project Skills

No project skills found. Add skills to any of: `.claude/skills/`, `.agents/skills/`, `.cursor/skills/`, or `.github/skills/` with a `SKILL.md` index file.
<!-- GSD:skills-end -->

<!-- GSD:workflow-start source:GSD defaults -->
## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:
- `/gsd-quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd-debug` for investigation and bug fixing
- `/gsd-execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->



<!-- GSD:profile-start -->
## Developer Profile

> Profile not yet configured. Run `/gsd-profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->
