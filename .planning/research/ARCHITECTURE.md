# Architecture Patterns — Nodefy

**Stack:** Next.js (frontend) + C# .NET (backend API) + PostgreSQL (Docker) + SSO auth
**Researched:** 2026-04-16
**Overall confidence:** HIGH

---

## 1. Multi-Tenancy Strategy

### Recommendation: Row-Level Security (RLS) on a shared schema

| Strategy | Ops Complexity | Migration Cost | Recommended For |
|----------|----------------|----------------|-----------------|
| Separate databases | Very high | Per-db migrations | Enterprise, regulated |
| Schema-per-tenant | Medium-high | `search_path` per request | Mid-market SaaS |
| **Shared schema + RLS** | **Low** | **Standard EF Core** | **B2B SaaS v1** |

**Why RLS wins for Nodefy v1:**
- One migration path — no schema switching per request
- PgBouncer / Npgsql pooling works without reconfiguration
- RLS as defense-in-depth — database enforces isolation even if app middleware has a bug
- Escape hatch to schema-per-tenant later without changing the domain model

### RLS Implementation Pattern

```sql
-- Every tenant-scoped table has: tenant_id UUID NOT NULL
-- .NET middleware sets session variable from JWT claim
SET app.current_tenant = '<uuid>';

-- Policy per table
CREATE POLICY tenant_isolation ON cards
    USING (tenant_id = current_setting('app.current_tenant', true)::uuid);
```

EF Core adds second application-level layer via `HasQueryFilter()`:

```csharp
modelBuilder.Entity<Card>()
    .HasQueryFilter(c => c.TenantId == _tenantService.CurrentTenantId);
```

**Never call `.IgnoreQueryFilters()` in normal code paths.**

---

## 2. API Architecture: REST

**Why not tRPC:** Requires TypeScript backend. Backend is C# .NET — eliminates it entirely.
**Why not GraphQL:** N+1 protection overhead disproportionate to Nodefy's bounded query surface.
**Why REST:** Native to .NET, OpenAPI/Swagger first-class, React Query + NSwag-generated TS types = type safety without tRPC.

### REST URL Structure

```
/api/v1/auth/*
/api/v1/workspaces
/api/v1/workspaces/{wid}/members
/api/v1/workspaces/{wid}/pipelines
/api/v1/workspaces/{wid}/pipelines/{pid}/stages
/api/v1/workspaces/{wid}/pipelines/{pid}/cards
/api/v1/cards/{cid}
/api/v1/cards/{cid}/move
/api/v1/cards/{cid}/comments
/api/v1/cards/{cid}/activity
```

---

## 3. System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                          Browser                                │
│                                                                 │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐        │
│   │  Auth Layer  │  │ Pipeline UI  │  │  Card Detail │        │
│   │  (NextAuth)  │  │(DnD + Board) │  │  (comments,  │        │
│   └──────┬───────┘  └──────┬───────┘  └──────┬───────┘        │
│          │                 │                  │                 │
│   ┌──────▼─────────────────▼──────────────────▼──────────────┐ │
│   │  API Client Layer (React Query + generated TS types)     │ │
│   └──────────────────────────┬────────────────────────────────┘ │
└──────────────────────────────┼──────────────────────────────────┘
                               │ HTTPS (REST + SignalR WebSocket upgrade)
┌──────────────────────────────▼──────────────────────────────────┐
│                   .NET Backend (ASP.NET Core)                   │
│                                                                 │
│  ┌───────────────┐  ┌───────────────┐  ┌────────────────────┐  │
│  │  Auth Module  │  │  API Layer    │  │  SignalR BoardHub  │  │
│  │  (OAuth2 +    │  │  (Controllers)│  │                    │  │
│  │   JWT issue)  │  │               │  │                    │  │
│  └───────┬───────┘  └───────┬───────┘  └────────┬───────────┘  │
│          └──────────────────┼───────────────────┘              │
│  ┌────────────────────────── ▼──────────────────────────────┐   │
│  │           Application Services                            │   │
│  │  TenantService | PipelineService | CardService | AuthSvc  │   │
│  └───────────────────────────┬───────────────────────────────┘   │
│  ┌────────────────────────── ▼──────────────────────────────┐   │
│  │         EF Core DbContext (tenant-scoped) + RLS           │   │
│  └───────────────────────────┬───────────────────────────────┘   │
└──────────────────────────────┼──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│                    PostgreSQL (Docker)                          │
│   users, user_identities, tenants, workspaces, memberships     │
│   pipelines, stages, cards, comments, activity_log             │
│   RLS policies on all tenant-scoped tables                     │
└─────────────────────────────────────────────────────────────────┘
```

---

## 4. Data Model

### Core Table Definitions

```sql
CREATE TABLE users (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email       TEXT NOT NULL UNIQUE,
    name        TEXT NOT NULL,
    avatar_url  TEXT,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE user_identities (
    id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id      UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    provider     TEXT NOT NULL,  -- 'github' | 'google' | 'microsoft'
    provider_id  TEXT NOT NULL,
    UNIQUE (provider, provider_id)
);

CREATE TABLE tenants (
    id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    slug       TEXT NOT NULL UNIQUE,
    name       TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE workspaces (
    id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id  UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    name       TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE memberships (
    id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id UUID NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    user_id      UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role         TEXT NOT NULL CHECK (role IN ('admin', 'member')),
    invited_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    accepted_at  TIMESTAMPTZ,
    UNIQUE (workspace_id, user_id)
);

CREATE TABLE pipelines (
    id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id UUID NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    tenant_id    UUID NOT NULL,
    name         TEXT NOT NULL,
    description  TEXT,
    created_by   UUID NOT NULL REFERENCES users(id),
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    archived_at  TIMESTAMPTZ
);

CREATE TABLE stages (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    pipeline_id UUID NOT NULL REFERENCES pipelines(id) ON DELETE CASCADE,
    tenant_id   UUID NOT NULL,
    name        TEXT NOT NULL,
    position    INTEGER NOT NULL,
    color       TEXT,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE cards (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    stage_id        UUID NOT NULL REFERENCES stages(id),
    pipeline_id     UUID NOT NULL REFERENCES pipelines(id),
    tenant_id       UUID NOT NULL,
    title           TEXT NOT NULL,
    description     TEXT,
    monetary_value  NUMERIC(18, 2),
    currency        TEXT DEFAULT 'BRL',
    assignee_id     UUID REFERENCES users(id) ON DELETE SET NULL,
    due_date        DATE,
    position        DOUBLE PRECISION NOT NULL,  -- fractional index
    stage_entered_at TIMESTAMPTZ NOT NULL DEFAULT now(),  -- for stage-age tracking
    created_by      UUID NOT NULL REFERENCES users(id),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    archived_at     TIMESTAMPTZ
);

CREATE INDEX idx_cards_stage_position ON cards(stage_id, position);
CREATE INDEX idx_cards_pipeline       ON cards(pipeline_id);
CREATE INDEX idx_cards_assignee       ON cards(assignee_id);
CREATE INDEX idx_cards_due_date       ON cards(due_date) WHERE due_date IS NOT NULL;

CREATE TABLE comments (
    id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    card_id    UUID NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
    tenant_id  UUID NOT NULL,
    author_id  UUID NOT NULL REFERENCES users(id),
    body       TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    edited_at  TIMESTAMPTZ
);

CREATE TABLE activity_log (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    card_id     UUID NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
    tenant_id   UUID NOT NULL,
    actor_id    UUID NOT NULL REFERENCES users(id),
    event_type  TEXT NOT NULL,
    -- 'card.created' | 'card.moved' | 'card.edited' | 'card.assigned' | 'card.archived' | 'comment.added'
    payload     JSONB NOT NULL DEFAULT '{}',
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_activity_card ON activity_log(card_id, occurred_at DESC);

-- Enable RLS
ALTER TABLE pipelines    ENABLE ROW LEVEL SECURITY;
ALTER TABLE stages       ENABLE ROW LEVEL SECURITY;
ALTER TABLE cards        ENABLE ROW LEVEL SECURITY;
ALTER TABLE comments     ENABLE ROW LEVEL SECURITY;
ALTER TABLE activity_log ENABLE ROW LEVEL SECURITY;
ALTER TABLE memberships  ENABLE ROW LEVEL SECURITY;
```

### Card Position / Ordering — Fractional Indexing

Store `position` as `DOUBLE PRECISION`. Moving a card between two others computes `(pos_before + pos_after) / 2.0` — only the moved card's row is updated. Avoids O(N) write fan-out of sequential integers.

When float precision degrades (gap < 1e-10), a background rebalancing job resets positions to evenly-spaced values. Rare in practice.

**`stage_entered_at` must be stored in the initial schema** — required for stage-age tracking. Cannot be cleanly backfilled.

---

## 5. Real-Time Architecture

### BoardHub Design

```
Hub route: /hubs/board

Groups:
  "pipeline:{pipelineId}" — all users viewing a pipeline board
  "card:{cardId}"          — users with card detail open

Events server → client:
  card:moved    { cardId, fromStageId, toStageId, position, movedBy }
  card:created  { card, stageId }
  card:updated  { cardId, changedFields }
  card:archived { cardId }
  comment:added { cardId, comment }
  stage:updated { stageId, changedFields }
```

### Drag-and-Drop + Real-Time Flow

```
User drags card
  │
  ├─► Optimistic update: move card in React state immediately
  │
  ├─► POST /api/v1/cards/{cid}/move
  │     ├─► DB: update card.stage_id, card.position, card.stage_entered_at
  │     ├─► DB: insert activity_log entry
  │     └─► SignalR broadcast to "pipeline:{pipelineId}"
  │               → card:moved event
  │
  └─► Other connected users:
        Receive card:moved via SignalR
        Skip if movedBy == self (already done via optimistic update)
        Otherwise update board state
```

**Conflict resolution (v1):** Last-write-wins. Add optimistic locking in v2 if conflicts are reported.

---

## 6. Authentication Flow

```
Browser       Next.js         .NET API        SSO Provider
   │              │                │                │
   ├─GET /login──►│                │                │
   │              ├──redirect──────┼────────────────►│
   │◄─redirect w/code─────────────┤                │
   ├─GET /callback►│                │                │
   │              ├─POST /auth/sso►│                │
   │              │  {provider,code}├─token exchange►│
   │              │                │◄─access_token──┤
   │              │                ├─fetch profile──►│
   │              │                │◄─profile───────┤
   │              │                ├─upsert user/identity
   │              │                ├─issue JWT (RS256)
   │              │◄─{jwt, user}───┤
   │              ├─set HttpOnly cookie
   │◄─session─────┤
```

**JWT claims:** `sub` (userId), `tid` (tenantId), `wid` (workspaceId), `role`, `exp`/`iat`.
**Storage:** HttpOnly Secure SameSite=Strict cookie — never localStorage.

---

## 7. Frontend Architecture (Next.js App Router)

```
app/
├── (auth)/
│   ├── login/page.tsx
│   └── auth/callback/
│
├── (app)/
│   ├── layout.tsx                -- Auth guard + workspace context
│   ├── dashboard/page.tsx        -- Pipeline list
│   ├── pipelines/
│   │   └── [pipelineId]/
│   │       └── page.tsx          -- Board view
│   └── settings/
│       ├── workspace/page.tsx
│       └── members/page.tsx
│
└── api/
    └── auth/[...nextauth]/route.ts
```

**State management:**
- Remote data: React Query (TanStack Query v5)
- Board DnD optimistic state: Zustand
- No Redux

---

## 8. Build Order

```
[1] Database Schema + Migrations (EF Core)
         │
         ▼
[2] .NET Auth Module (SSO exchange + JWT issuance)
         │
         ▼
[3] Tenant + Workspace + Membership API
         │
         ▼
[4] Next.js Auth (NextAuth + SSO + cookie)
         │
         ▼
[5] Pipeline + Stage CRUD API
         │
         ▼
[6] Card CRUD API (no move yet)
         │
    ┌────┴─────────────────────┐
    ▼                          ▼
[7] Card Move API          [8] Board UI (static)
    │                          │
    └──────────────┬───────────┘
                   ▼
[9] SignalR BoardHub + Board real-time integration
                   │
                   ▼
[10] Comments + Activity Log API + UI
                   │
                   ▼
[11] Filters + Search
                   │
                   ▼
[12] Settings UI (workspace, invite, roles)
```

**Critical path:** DB Schema → Auth → Workspace API → Auth UI → Pipeline/Stage → Card CRUD → Board UI → Card Move → SignalR

---

## 9. Docker Infrastructure

```yaml
services:
  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: nodefy
      POSTGRES_USER: nodefy
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    ports: ["5432:5432"]
    volumes: [pgdata:/var/lib/postgresql/data]

  api:
    build: ./backend
    environment:
      ConnectionStrings__Default: "Host=db;Database=nodefy;Username=nodefy;Password=${DB_PASSWORD}"
    ports: ["5000:8080"]
    depends_on: [db]

  frontend:
    build: ./frontend
    environment:
      NEXTAUTH_URL: "http://localhost:3000"
      NEXT_PUBLIC_API_URL: "http://localhost:5000"
    ports: ["3000:3000"]
    depends_on: [api]

volumes:
  pgdata:
```

Production: add nginx/Caddy for TLS termination and WebSocket upgrade routing (`/hubs/*` → .NET).

---

## 10. Key Decisions

| Decision | Choice | Rationale | Confidence |
|----------|--------|-----------|------------|
| Multi-tenancy | Shared schema + RLS | Low ops complexity, defense-in-depth | HIGH |
| API style | REST | tRPC is TS-only; GraphQL overkill | HIGH |
| Real-time | SignalR | First-class .NET support, auto-fallback, group broadcasts | HIGH |
| Card ordering | Fractional indexing (float) | Avoids O(N) write fan-out on move | HIGH |
| Frontend state | React Query + Zustand | Industry standard concern separation | HIGH |
| DnD library | @dnd-kit/core | react-beautiful-dnd unmaintained | HIGH |
| Auth storage | HttpOnly cookie | XSS protection; CSRF via SameSite=Strict | HIGH |
| Conflict resolution | Last-write-wins (v1) | Sufficient for collaborative board | MEDIUM |

---

## Open Questions

- **Invite flow:** Email-based invite requires email service. In-app link is simpler for v1 — which is acceptable?
- **JWT encoding:** Encode both `tid` (tenant) and `wid` (workspace) in JWT for multi-workspace future-proofing?
- **Fractional index rebalancing:** When and how does the background rebalancing job trigger?
- **SignalR scale-out:** For multiple .NET instances, Redis backplane (`Microsoft.AspNetCore.SignalR.StackExchangeRedis`) needed. V1 single-instance Docker is fine.
