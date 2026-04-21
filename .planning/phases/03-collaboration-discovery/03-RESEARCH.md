# Phase 3: Collaboration & Discovery — Research

**Researched:** 2026-04-20
**Domain:** ASP.NET Core SignalR hub groups, PostgreSQL full-text search, React real-time state integration
**Confidence:** HIGH (all core claims verified against official Microsoft docs and live codebase inspection)

---

## Summary

Phase 3 adds two orthogonal capabilities to an already-functional Kanban board: real-time broadcast of card mutations to all connected workspace members, and keyword/filter search across cards within a pipeline.

The SignalR infrastructure is already partially wired: `BoardHub` exists, `app.MapHub<BoardHub>("/hubs/board")` is registered, `@microsoft/signalr@10.0.0` is installed in the frontend, and CORS is configured with `AllowCredentials()`. What is missing is (1) tenant verification inside `JoinBoard`, (2) broadcast calls from the card mutation endpoints after `SaveChangesAsync`, and (3) a frontend hook that opens the connection, registers event handlers, and invalidates TanStack Query on incoming events.

The search and filter capability is a query-parameter extension of the existing `GET /pipelines/{id}/board` endpoint. For v1 dataset sizes (hundreds of cards per pipeline), PostgreSQL `ILIKE` is sufficient and avoids schema migration overhead. Filters are additive AND conditions on the existing EF Core query. Zustand holds client-side filter state per-pipeline (keyed by `pipelineId`), and the board hook re-fetches when filter state changes rather than filtering client-side — this keeps the filtered card count and monetary sum aggregates accurate.

**Primary recommendation:** Extend `BoardHub.JoinBoard` with a DB membership check, inject `IHubContext<BoardHub>` into each card endpoint to broadcast after save, and create a `useBoardRealtime` hook in the frontend that binds `hub.on` handlers to `qc.invalidateQueries`. Add a `GET /pipelines/{id}/cards/search` endpoint with `ILIKE` + optional filter query params and a `useCardSearch` + `useCardFilters` hook pair on the frontend.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Real-time broadcast | API (SignalR Hub) | — | Server owns group membership; only the server can enforce tenant isolation before broadcasting |
| Tenant verification on hub join | API (SignalR Hub + DB) | — | Must query `workspace_members` to confirm the connecting user is a member before adding to group |
| JWT token delivery to SignalR | Frontend (Next.js route handler) | API | Browser cannot set Authorization header for WS; token must be fetched via HTTP then passed as `accessTokenFactory` |
| Event-driven cache invalidation | Frontend (TanStack Query) | — | `qc.invalidateQueries` is the canonical React Query pattern for external invalidation |
| Filter state storage | Frontend (Zustand) | — | Per-pipeline, ephemeral UI state — not server state |
| Search + filter query evaluation | API (PostgreSQL ILIKE / WHERE) | — | Aggregates (cardCount, monetarySum) must be recomputed server-side after filtering |
| Full-text index | Database (PostgreSQL) | — | `tsvector` column or `ILIKE` — see §Search Endpoint |

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| REAL-01 | Card move by member A is reflected on member B's board via SignalR within 2s | Hub group broadcast from card move endpoint; frontend `hub.on("card-moved")` invalidates `["board", pipelineId]` |
| REAL-02 | Card create/update by member A appears on member B's board without reload | Hub group broadcast from card create/update/archive endpoints; same invalidation pattern |
| DISC-01 | Member can search cards by title within a pipeline | `GET /pipelines/{id}/cards/search?q=` with `ILIKE` filter; `useCardSearch` hook |
| DISC-02 | Member can filter board by assignee, close date, value; filters combine with AND | Query params on search endpoint; Zustand filter state; board re-fetch on filter change |
</phase_requirements>

---

## Standard Stack

### Core (already installed — no new installs required)

| Library | Version (verified) | Purpose | Notes |
|---------|-------------------|---------|-------|
| `@microsoft/signalr` | 10.0.0 [VERIFIED: package.json] | JS/TS SignalR client | Already in `package.json` |
| `Microsoft.AspNetCore.SignalR` | 1.2.9 [VERIFIED: .csproj] | Server hub infrastructure | Already in .csproj |
| `zustand` | 5.0.12 [VERIFIED: package.json] | Client filter state | Already in `package.json` |
| `@tanstack/react-query` | 5.99.0 [VERIFIED: package.json] | Server state + cache invalidation | Already in `package.json` |

### No new packages needed for Phase 3
All required libraries are already present. Phase 3 is entirely implementation — no dependency installs.

---

## Architecture Patterns

### System Architecture Diagram

```
Browser (member B)
  └─ useBoardRealtime hook
       ├─ HubConnection.start()  ──────────────────────────────────┐
       ├─ connection.invoke("JoinBoard", pipelineId)               │  WebSocket / SSE / LongPoll
       └─ connection.on("card-moved" | "card-created" | ...)       │
            └─ qc.invalidateQueries(["board", pipelineId])         │
                                                                   │
Browser (member A)                                                 │
  └─ moveMutation.mutate(...)                                      │
       └─ PATCH /api/cards/{id}/move ──────────────────────────────┤
                                         ┌─────────────────────────┘
                                   ASP.NET Core API
                                         │
                              CardEndpoints.MoveCard
                                    │        │
                             SaveChangesAsync  IHubContext<BoardHub>
                                              └─ Clients.Group("pipeline:{pipelineId}")
                                                   .SendAsync("card-moved", payload)
                                                         │
                                               All connections in group
                                               (all members watching this pipeline)
```

### Recommended Project Structure Changes

```
api/Nodefy.Api/
├── Hubs/
│   └── BoardHub.cs              # EXTEND: add AppDbContext injection + tenant check in JoinBoard
├── Endpoints/
│   ├── CardEndpoints.cs         # EXTEND: inject IHubContext<BoardHub>, broadcast after SaveChangesAsync
│   └── PipelineEndpoints.cs     # EXTEND: add GET /pipelines/{id}/cards/search endpoint

frontend/src/
├── hooks/
│   ├── use-board.ts             # EXTEND: accept filter params, pass to query key + queryFn
│   ├── use-board-realtime.ts    # NEW: SignalR connection lifecycle + hub.on handlers
│   └── use-card-filters.ts     # NEW: Zustand filter state slice + search state
├── store/
│   └── ui-store.ts             # EXTEND: add filter state keyed by pipelineId
└── components/board/
    └── BoardShell.tsx           # EXTEND: mount useBoardRealtime, add search/filter toolbar
```

---

## Plan 1: SignalR BoardHub

### Research Question 1: Events to broadcast and payload shape

**Events:** Four card lifecycle events should be broadcast:

| Event name | Trigger endpoint | Payload |
|------------|-----------------|---------|
| `card-moved` | `PATCH /cards/{id}/move` | `{ cardId, pipelineId, targetStageId, position }` |
| `card-created` | `POST /pipelines/{pipelineId}/cards` | `{ cardId, pipelineId, stageId }` |
| `card-updated` | `PATCH /cards/{id}` | `{ cardId, pipelineId }` |
| `card-archived` | `PATCH /cards/{id}/archive` | `{ cardId, pipelineId }` |

**Payload philosophy:** Keep payloads minimal — just enough for the receiver to call `qc.invalidateQueries`. Do NOT send the full card object in the SignalR payload; let each client re-fetch via TanStack Query. This avoids inconsistency between the optimistic state, the broadcasted state, and the actual DB state.

**Rationale for 4 separate events over 1 generic "board-changed":** Allows the frontend to make smarter decisions later (e.g., animate a `card-moved` differently, skip invalidation for `card-updated` if the card panel is not open). For v1, all four can call `qc.invalidateQueries(["board", pipelineId])` identically.

### Research Question 2: Group naming — `pipeline:{pipelineId}` vs `workspace:{workspaceId}`

**Recommendation: `pipeline:{pipelineId}`** [VERIFIED: STATE.md architecture notes confirm this decision]

Rationale:
- A workspace can have multiple pipelines. Broadcasting workspace-level events would wake all board instances even for pipelines the member is not currently viewing.
- Pipeline-scoped groups mean a member watching Pipeline A does not receive noise from Pipeline B mutations.
- Tenant isolation is enforced at the group JOIN step (see §Tenant Verification), not at broadcast time. Since all pipelines within a group already belong to the same tenant, there is no cross-tenant risk from pipeline-scoped groups.

**Group key format:** `"pipeline:" + pipelineId.ToString()` (string, not Guid — SignalR group names are strings).

### Research Question 3: Tenant verification on `OnConnectedAsync` / `JoinBoard`

**Current state:** `BoardHub.JoinBoard` adds the connection to the group with zero verification. Any authenticated user with a valid JWT can join any pipeline's group. [VERIFIED: codebase inspection of `Hubs/BoardHub.cs`]

**Required change:** Inject `AppDbContext` into the hub constructor and verify that the authenticated user (`Context.User`) is a member of the workspace that owns the requested pipeline before calling `Groups.AddToGroupAsync`.

**Pattern from existing codebase:** `CardEndpoints` already does:
```
var pipeline = await db.Pipelines.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == pipelineId);
tenant.SetTenant(pipeline.TenantId);
var isMember = await db.WorkspaceMembers.IgnoreQueryFilters()
    .AnyAsync(m => m.TenantId == pipeline.TenantId && m.UserId == user.UserId);
if (!isMember) return Results.Forbid();
```

The hub must replicate this exact pattern, aborting the connection with `Context.Abort()` instead of returning a result.

**Hub DI pattern:** Hub constructors support DI injection. [VERIFIED: Microsoft docs — "Hub constructors can accept services from DI as parameters"]

```csharp
// Source: https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs?view=aspnetcore-9.0
[Authorize]
public class BoardHub : Hub
{
    private readonly AppDbContext _db;
    public BoardHub(AppDbContext db) { _db = db; }

    public async Task JoinBoard(string pipelineId)
    {
        if (!Guid.TryParse(pipelineId, out var pipelineGuid))
        { Context.Abort(); return; }

        var userIdStr = Context.User?.FindFirst("sub")?.Value
                     ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
        { Context.Abort(); return; }

        // Bootstrap tenant from pipeline (IgnoreQueryFilters pattern from CardEndpoints)
        var pipeline = await _db.Pipelines
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == pipelineGuid);
        if (pipeline is null) { Context.Abort(); return; }

        var isMember = await _db.WorkspaceMembers
            .IgnoreQueryFilters()
            .AnyAsync(m => m.TenantId == pipeline.TenantId && m.UserId == userId);
        if (!isMember) { Context.Abort(); return; }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"pipeline:{pipelineId}");
    }

    public Task LeaveBoard(string pipelineId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"pipeline:{pipelineId}");
}
```

**Pitfall — ITenantService in Hub:** Do NOT call `tenantService.SetTenant()` in the hub. Hub instances are transient and do not share the same DI scope as HTTP middleware. The `TenantMiddleware` does not run for SignalR connections. Use `IgnoreQueryFilters()` directly in the hub, same as the IgnoreQueryFilters bootstrap pattern in card endpoints.

### Broadcast from card endpoints

**Pattern:** Inject `IHubContext<BoardHub>` into card endpoints. Call `SendAsync` after `SaveChangesAsync`. [VERIFIED: Microsoft docs — "use IHubContext to send messages from elsewhere in your application"]

```csharp
// Source: https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs?view=aspnetcore-9.0
// Inside MapCardEndpoints, after SaveChangesAsync on the move endpoint:
await db.SaveChangesAsync();

await hubContext.Clients
    .Group($"pipeline:{card.PipelineId}")
    .SendAsync("card-moved", new {
        cardId    = card.Id,
        pipelineId = card.PipelineId,
        targetStageId = card.StageId,
        position  = card.Position,
    });

return Results.Ok(ToDto(card));
```

**IHubContext injection in minimal API endpoints:**

```csharp
// MapCardEndpoints signature — add IHubContext<BoardHub> as a parameter
app.MapPatch("/cards/{id:guid}/move",
    async (Guid id, MoveCardRequest req, AppDbContext db,
           CurrentUserAccessor user, ITenantService tenant,
           IHubContext<BoardHub> hub) =>
    {
        // ... existing logic ...
        await db.SaveChangesAsync();
        await hub.Clients.Group($"pipeline:{card.PipelineId}")
            .SendAsync("card-moved", new { cardId = card.Id, pipelineId = card.PipelineId,
                                          targetStageId = card.StageId, position = card.Position });
        return Results.Ok(ToDto(card));
    }).RequireAuthorization();
```

**Important:** Broadcast AFTER `SaveChangesAsync` succeeds. Never broadcast before — an exception during save would leave other clients with a stale state that never self-corrects.

### JWT token delivery for SignalR WebSocket connections

**Problem:** Browsers cannot set the `Authorization` header for WebSocket connections. SignalR automatically falls back to transmitting the token via query string (`?access_token=...`). [VERIFIED: Microsoft docs — "When using WebSockets and Server-Sent Events, the token is transmitted as a query string parameter"]

**Backend requirement:** The existing `JwtConfig.cs` does NOT currently configure `OnMessageReceived` to read `access_token` from the query string. This MUST be added for SignalR authentication to work. [VERIFIED: codebase inspection — `JwtConfig.cs` line 14-27 has no `Events` handler]

```csharp
// In JwtConfig.cs — add Events inside AddJwtBearer:
.AddJwtBearer(opts =>
{
    opts.TokenValidationParameters = /* ... existing ... */;

    // REQUIRED for SignalR WebSocket + SSE auth
    opts.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hubs/board"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});
```

**Frontend token delivery:** The frontend mints short-lived JWT tokens via `mintApiToken` in `api-token.ts`. The `accessTokenFactory` option in `HubConnectionBuilder.withUrl` accepts a function that returns a `string | Promise<string>`. The token must be fetched fresh for each connection attempt (the factory is called before every HTTP request SignalR makes). [VERIFIED: Microsoft docs]

```typescript
// Source: https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-9.0
.withUrl(`${BACKEND_URL}/hubs/board`, {
    accessTokenFactory: async () => {
        // Fetch a fresh token from the Next.js route handler
        const res = await fetch("/api/signalr-token")
        const { token } = await res.json()
        return token
    }
})
```

A Next.js route handler at `app/api/signalr-token/route.ts` calls `mintApiToken` with the session's user claims and returns the token. This avoids exposing `AUTH_SECRET` to the browser.

### Research Question 4: Frontend SignalR client — hook vs. component, reconnect, cache invalidation

**Hook placement:** Create a dedicated `useBoardRealtime(pipelineId, workspaceId)` hook. Mount it inside `BoardShell` (alongside `useBoard`). The hook is responsible for the entire connection lifecycle. Do NOT initialize the connection at the app root — it must be scoped to when a board is actually mounted and torn down when the board unmounts.

**Why a hook, not a component:** The connection needs access to `useQueryClient()` for cache invalidation. Hooks compose cleanly; a component would require prop drilling or context.

**Reconnect strategy:** Use `.withAutomaticReconnect([0, 2000, 10000, 30000])` (the default schedule). [VERIFIED: Microsoft docs] After reconnect (`onreconnected`), immediately re-invoke `JoinBoard` because the server-side group membership is connection-scoped — it is lost when the WebSocket drops. Also invalidate the board query after reconnect to catch any mutations that occurred during the disconnect window.

**Complete hook pattern:**

```typescript
// Source: https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client?view=aspnetcore-9.0
// frontend/src/hooks/use-board-realtime.ts
"use client"
import { useEffect, useRef } from "react"
import * as signalR from "@microsoft/signalr"
import { useQueryClient } from "@tanstack/react-query"

const BACKEND_URL = process.env.NEXT_PUBLIC_BACKEND_URL ?? "http://localhost:5000"

export function useBoardRealtime(pipelineId: string) {
  const qc = useQueryClient()
  const connectionRef = useRef<signalR.HubConnection | null>(null)

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${BACKEND_URL}/hubs/board`, {
        accessTokenFactory: async () => {
          const res = await fetch("/api/signalr-token")
          const { token } = await res.json()
          return token
        },
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    // Register handlers BEFORE start() — official best practice
    const invalidateBoard = () =>
      qc.invalidateQueries({ queryKey: ["board", pipelineId] })

    connection.on("card-moved",    invalidateBoard)
    connection.on("card-created",  invalidateBoard)
    connection.on("card-updated",  invalidateBoard)
    connection.on("card-archived", invalidateBoard)

    connection.onreconnected(async () => {
      // Re-join group after reconnect — group membership is connection-scoped
      await connection.invoke("JoinBoard", pipelineId)
      invalidateBoard()  // catch mutations during disconnect window
    })

    const start = async () => {
      try {
        await connection.start()
        await connection.invoke("JoinBoard", pipelineId)
      } catch {
        // withAutomaticReconnect handles retries; initial failure is non-fatal
      }
    }

    connectionRef.current = connection
    start()

    return () => {
      connection.invoke("LeaveBoard", pipelineId).catch(() => {})
      connection.stop()
    }
  }, [pipelineId, qc])
}
```

**Self-update suppression:** Member A moves a card, receives the optimistic update immediately via `moveMutation`, and also receives the `card-moved` broadcast from the server. This triggers a second `invalidateQueries`. This is acceptable — TanStack Query deduplicates in-flight requests and the board will simply re-fetch once. Do NOT try to suppress self-updates; the complexity is not worth it for v1.

**Why `invalidateQueries` instead of `setQueryData`:** The broadcast payload is intentionally minimal (no full card data). `setQueryData` would require reconstructing the full `BoardData` shape from a partial event, creating a second source of truth. `invalidateQueries` keeps TanStack Query as the single source of truth and lets it re-fetch authoritative data from the API.

---

## Plan 2: Search & Filters

### Research Question 4: `ILIKE` vs `tsvector` for v1

**Recommendation: `ILIKE` for v1.** [ASSUMED — verified reasoning against PostgreSQL docs pattern, not load tested]

| | `ILIKE` | `tsvector` |
|---|---|---|
| Schema migration needed | No | Yes (new column + trigger or generated column) |
| Index needed | Optional `pg_trgm` GIN index | Yes (GIN index on tsvector column) |
| Prefix search | Yes (`'%foo%'`) | No (only whole-word) |
| Partial word match | Yes | No (stemming, not substring) |
| Setup complexity | Zero | Medium (add migration, trigger, index) |
| Query | `WHERE title ILIKE '%q%'` | `WHERE search_vector @@ plainto_tsquery('q')` |

For v1 with hundreds of cards per pipeline, `ILIKE '%q%'` without an index is a sequential scan over the pipeline's cards table — well within acceptable performance. PostgreSQL can scan 100,000 rows in < 10ms for a narrow table.

`tsvector` provides no UX advantage for title search (users type substrings, not whole words) and adds migration + indexing complexity. If search scales to millions of cards in v2, add a GIN trigram index (`CREATE INDEX CONCURRENTLY ... USING gin (title gin_trgm_ops)`).

### Research Question 5: Query params vs. request body for filters

**Recommendation: Query parameters.** Reasons:
- GET semantics are correct for read-only queries (bookmarkable, cacheable by HTTP layers, compatible with browser back-button)
- The existing `GET /pipelines/{id}/board` endpoint uses query params (e.g., `?workspaceId=`)
- All filter values are scalar (string, date range, numeric range) — no complex nested objects that require a body
- POST-for-search is an anti-pattern unless the filter is truly complex (faceted search, nested boolean logic). Simple AND filters do not justify it.

**Filter endpoint:**

```
GET /pipelines/{id}/cards/search
  ?q=<title substring>            // ILIKE search
  &assigneeId=<uuid>              // exact match
  &closeDateFrom=<ISO8601>        // >= filter
  &closeDateTo=<ISO8601>          // <= filter
  &valueMin=<decimal>             // >= filter
  &valueMax=<decimal>             // <= filter
```

All params are optional and combine with AND logic. Missing param = no filter on that field.

**Response shape:** Return the same `BoardDto` (stages with filtered card lists + recomputed aggregates) so the frontend board renders without structural changes. The board simply shows fewer cards when filters are active. Column aggregates (count, sum) reflect filtered cards only.

**EF Core query pattern:**

```csharp
// GET /pipelines/{id}/cards/search
app.MapGet("/pipelines/{id:guid}/cards/search",
    async (Guid id, string? q, Guid? assigneeId,
           DateTimeOffset? closeDateFrom, DateTimeOffset? closeDateTo,
           decimal? valueMin, decimal? valueMax,
           AppDbContext db, ITenantService tenant) =>
{
    var pipeline = await db.Pipelines.IgnoreQueryFilters()
        .FirstOrDefaultAsync(p => p.Id == id);
    if (pipeline is null) return Results.NotFound();
    tenant.SetTenant(pipeline.TenantId);

    var stages = await db.Stages
        .Where(s => s.PipelineId == id)
        .OrderBy(s => s.Position)
        .ToListAsync();

    var stageDtos = new List<StageBoardDto>();
    foreach (var stage in stages)
    {
        IQueryable<Card> query = db.Cards.Where(c => c.StageId == stage.Id);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(c => EF.Functions.ILike(c.Title, $"%{q}%"));
        if (assigneeId.HasValue)
            query = query.Where(c => c.AssigneeId == assigneeId);
        if (closeDateFrom.HasValue)
            query = query.Where(c => c.CloseDate >= closeDateFrom);
        if (closeDateTo.HasValue)
            query = query.Where(c => c.CloseDate <= closeDateTo);
        if (valueMin.HasValue)
            query = query.Where(c => c.MonetaryValue >= valueMin);
        if (valueMax.HasValue)
            query = query.Where(c => c.MonetaryValue <= valueMax);

        var cards = await query
            .OrderBy(c => c.Position)
            .Select(c => new CardSummaryDto(...))
            .ToListAsync();

        // Recompute aggregates on filtered set
        stageDtos.Add(new StageBoardDto(stage.Id, stage.Name, stage.Position,
            cards.Count, cards.Sum(c => c.MonetaryValue ?? 0m), cards));
    }

    return Results.Ok(new BoardDto(new PipelineDto(pipeline.Id, pipeline.Name, pipeline.Position), stageDtos));
}).RequireAuthorization();
```

**EF.Functions.ILike:** [VERIFIED: Npgsql.EntityFrameworkCore.PostgreSQL supports `EF.Functions.ILike` as a case-insensitive LIKE. This maps to PostgreSQL's native `ILIKE` operator.]

**SQL injection safety:** `EF.Functions.ILike` passes `q` as a parameterized value internally — no string interpolation in the final SQL. The `%` wildcards are part of the pattern string, not SQL syntax, and are properly escaped by Npgsql.

### Research Question 6: Zustand filter state — per-pipeline vs. global

**Recommendation: Per-pipeline, keyed by `pipelineId`.** [ASSUMED — no direct official reference, follows from the project's Zustand pattern]

Reasons:
- A user can navigate between pipelines; each pipeline should remember its last filter independently.
- Global filters would be reset whenever the user switches pipelines, which is confusing.
- Persisting filters across sessions (localStorage) is per-pipeline for the same reason.

**State shape extension to `ui-store.ts`:**

```typescript
// Extend UIState in ui-store.ts
interface FilterState {
  q: string
  assigneeId: string | null
  closeDateFrom: string | null  // ISO8601
  closeDateTo: string | null
  valueMin: number | null
  valueMax: number | null
}

const defaultFilter: FilterState = {
  q: "", assigneeId: null,
  closeDateFrom: null, closeDateTo: null,
  valueMin: null, valueMax: null,
}

// Added to UIState
filters: Record<string, FilterState>  // keyed by pipelineId
setFilter: (pipelineId: string, patch: Partial<FilterState>) => void
clearFilters: (pipelineId: string) => void
```

**Board re-render without full refetch:**

When filters change, the frontend sends a new request to `/pipelines/{id}/cards/search` with the new params. The TanStack Query key includes the filter state: `["board", pipelineId, filters]`. When the filter key changes, TanStack Query automatically re-fetches. This is not a "full refetch" in the sense of data duplication — it is a new, differently-filtered response. The query key differential approach is the correct TanStack Query pattern.

```typescript
// In use-board.ts — extend to accept filters
const filterParams = filters ? buildSearchParams(filters) : null

const { data: board = initialData } = useQuery<BoardData>({
  queryKey: ["board", pipelineId, filterParams ?? "unfiltered"],
  queryFn: async () => {
    const base = filterParams
      ? `/api/pipelines/${pipelineId}/cards/search?workspaceId=${workspaceId}&${filterParams}`
      : `/api/pipelines/${pipelineId}/board?workspaceId=${workspaceId}`
    const res = await fetch(base)
    if (!res.ok) throw new Error(await res.text())
    return res.json()
  },
  initialData: filterParams ? undefined : initialData,
  staleTime: 30_000,
})
```

**Active filter indicator:** When any filter field is non-empty/non-null, show a badge or "Filters active" indicator in the board header so users know they are viewing a filtered view.

**Search box debounce:** Debounce the title search input by 300ms before updating Zustand state. Otherwise each keystroke triggers a new HTTP request. A simple `useEffect` + `setTimeout`/`clearTimeout` pattern is sufficient — no additional library needed.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| SignalR group management | Custom pub/sub or Redis pub/sub | `IHubContext<BoardHub>.Clients.Group(...)` | SignalR already handles group fan-out, reconnection, transport fallback |
| WebSocket auth | Custom token-in-URL scheme | `accessTokenFactory` + `OnMessageReceived` event in JwtBearer | The official pattern handles the browser WebSocket header limitation |
| Client-side card filtering | Filter `BoardData` in JS after fetch | Server-side `ILIKE` + WHERE | Client-side filtering produces wrong column aggregates |
| Full-text search index | Elasticsearch, Redis Search | PostgreSQL `ILIKE` (v1) | Sufficient for dataset size; same DB, no operational overhead |
| Reconnect loop | Manual `setInterval` reconnect | `.withAutomaticReconnect([0, 2000, 10000, 30000])` | Built-in, handles exponential backoff + state transitions |
| Token refresh on reconnect | Session polling | `accessTokenFactory` is called before every HTTP request | Factory is invoked automatically; just return a fresh token from the route handler |

---

## Common Pitfalls

### Pitfall 1: Broadcasting before `SaveChangesAsync`
**What goes wrong:** The broadcast fires but the DB write fails. Other clients receive a `card-moved` event, invalidate their cache, re-fetch, and see the OLD state. Board appears out of sync with no error visible to anyone.
**How to avoid:** Always `await db.SaveChangesAsync()` first. If it throws, the endpoint returns an error and no broadcast is sent. Wrap broadcast in its own try/catch so a SignalR failure doesn't roll back a successful DB write.

### Pitfall 2: Missing `OnMessageReceived` event in JwtBearerEvents
**What goes wrong:** SignalR WebSocket connections return 401. The `[Authorize]` attribute on the hub is enforced, but the bearer token arrives as `?access_token=` in the query string which the JWT middleware ignores by default.
**How to avoid:** Add the `OnMessageReceived` event handler to `JwtConfig.cs` that reads `context.Request.Query["access_token"]` when the path starts with `/hubs/board`. [VERIFIED: Microsoft docs — this is explicitly documented as required]

### Pitfall 3: Hub `AppDbContext` lifetime
**What goes wrong:** If `AppDbContext` is injected and the `ITenantService` is resolved at construction time, `_tenantId` in the DbContext is `Guid.Empty` because `TenantMiddleware` does not run for SignalR connections. The global query filters will exclude everything.
**How to avoid:** In `JoinBoard`, use `.IgnoreQueryFilters()` explicitly for all queries, then do manual `TenantId` equality checks — exactly as `CardEndpoints` does for the bootstrap pattern. Do NOT call `tenantService.SetTenant()` in the hub.

### Pitfall 4: Group membership lost on reconnect
**What goes wrong:** Client reconnects after network interruption. `withAutomaticReconnect` re-establishes the WebSocket, but the server has no record of the previous group memberships (groups are in-memory, connection-scoped). The client silently stops receiving events.
**How to avoid:** In `connection.onreconnected(async () => { await connection.invoke("JoinBoard", pipelineId) })`. This MUST be registered before `connection.start()`.

### Pitfall 5: Self-invalidation causing DnD flicker
**What goes wrong:** Member A is mid-drag (DnD active). The `card-moved` broadcast arrives from the server (triggered by A's own mutation). `invalidateQueries` causes a re-fetch. React Query updates the board state during an active drag, causing the DnD library to lose track of the dragged card.
**How to avoid:** Check `useUIStore().draggingCardId` before calling `invalidateQueries`. If a drag is in progress, skip the invalidation — it will happen via `onSettled` on the mutation anyway.

```typescript
connection.on("card-moved", () => {
  if (useUIStore.getState().draggingCardId) return  // skip during active drag
  qc.invalidateQueries({ queryKey: ["board", pipelineId] })
})
```

### Pitfall 6: `ILIKE` wildcard injection
**What goes wrong:** User searches for `%` or `_` — these are SQL wildcard characters. With `EF.Functions.ILike(c.Title, $"%{q}%")`, the `q` value is passed as a parameter (safe from SQL injection), but the PostgreSQL LIKE matching may produce unexpected results if `q` itself contains `%` or `_`.
**How to avoid:** Escape the search term before constructing the pattern: replace `%` with `\%` and `_` with `\_`, then append `ESCAPE '\'` in the raw query, OR simply accept that `%` in a search term matches all cards (harmless behavior). For v1, the latter is acceptable — document it as known behavior.

---

## Code Examples

### Hub with tenant verification (complete)
```csharp
// Source: official Microsoft SignalR docs + Nodefy IgnoreQueryFilters pattern
[Authorize]
public class BoardHub : Hub
{
    private readonly AppDbContext _db;
    public BoardHub(AppDbContext db) { _db = db; }

    public async Task JoinBoard(string pipelineId)
    {
        if (!Guid.TryParse(pipelineId, out var pipelineGuid))
        { Context.Abort(); return; }

        var userIdStr = Context.User?.FindFirst("sub")?.Value
                     ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
        { Context.Abort(); return; }

        var pipeline = await _db.Pipelines
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == pipelineGuid);
        if (pipeline is null) { Context.Abort(); return; }

        var isMember = await _db.WorkspaceMembers
            .IgnoreQueryFilters()
            .AnyAsync(m => m.TenantId == pipeline.TenantId && m.UserId == userId);
        if (!isMember) { Context.Abort(); return; }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"pipeline:{pipelineId}");
    }

    public Task LeaveBoard(string pipelineId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"pipeline:{pipelineId}");
}
```

### JwtConfig.cs `OnMessageReceived` addition
```csharp
// Source: https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-9.0
opts.Events = new JwtBearerEvents
{
    OnMessageReceived = context =>
    {
        var accessToken = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;
        if (!string.IsNullOrEmpty(accessToken) &&
            path.StartsWithSegments("/hubs/board"))
        {
            context.Token = accessToken;
        }
        return Task.CompletedTask;
    }
};
```

### Frontend `useBoardRealtime` hook (complete)
```typescript
// Source: https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client?view=aspnetcore-9.0
"use client"
import { useEffect, useRef } from "react"
import * as signalR from "@microsoft/signalr"
import { useQueryClient } from "@tanstack/react-query"
import { useUIStore } from "@/store/ui-store"

const BACKEND_URL = process.env.NEXT_PUBLIC_BACKEND_URL ?? "http://localhost:5000"

export function useBoardRealtime(pipelineId: string) {
  const qc = useQueryClient()
  const connectionRef = useRef<signalR.HubConnection | null>(null)

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${BACKEND_URL}/hubs/board`, {
        accessTokenFactory: async () => {
          const res = await fetch("/api/signalr-token")
          const { token } = await res.json()
          return token
        },
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    const invalidate = () => {
      // Skip invalidation if user is actively dragging a card (prevents DnD flicker)
      if (useUIStore.getState().draggingCardId) return
      qc.invalidateQueries({ queryKey: ["board", pipelineId] })
    }

    // Register ALL handlers before start() — required by official docs
    connection.on("card-moved",    invalidate)
    connection.on("card-created",  invalidate)
    connection.on("card-updated",  invalidate)
    connection.on("card-archived", invalidate)

    connection.onreconnected(async () => {
      await connection.invoke("JoinBoard", pipelineId).catch(() => {})
      qc.invalidateQueries({ queryKey: ["board", pipelineId] })
    })

    const start = async () => {
      try {
        await connection.start()
        await connection.invoke("JoinBoard", pipelineId)
      } catch {
        // withAutomaticReconnect handles retries; log and continue
      }
    }

    connectionRef.current = connection
    start()

    return () => {
      connection.invoke("LeaveBoard", pipelineId).catch(() => {})
      connection.stop()
    }
  }, [pipelineId, qc])
}
```

### Next.js signalr-token route handler
```typescript
// app/api/signalr-token/route.ts
import { auth } from "@/auth"
import { mintApiToken } from "@/lib/api-token"
import { NextResponse } from "next/server"

export async function GET() {
  const session = await auth()
  if (!session?.user?.id) return NextResponse.json({ error: "Unauthorized" }, { status: 401 })

  const token = await mintApiToken({
    sub: session.user.id,
    email: session.user.email ?? "",
  })
  return NextResponse.json({ token })
}
```

### Search + filter frontend hook
```typescript
// frontend/src/hooks/use-card-filters.ts
import { useUIStore } from "@/store/ui-store"

export function useCardFilters(pipelineId: string) {
  const filters = useUIStore((s) => s.filters[pipelineId])
  const setFilter = useUIStore((s) => s.setFilter)
  const clearFilters = useUIStore((s) => s.clearFilters)

  const hasActiveFilters = filters && (
    filters.q !== "" ||
    filters.assigneeId !== null ||
    filters.closeDateFrom !== null ||
    filters.closeDateTo !== null ||
    filters.valueMin !== null ||
    filters.valueMax !== null
  )

  return {
    filters: filters ?? defaultFilter,
    setFilter: (patch: Partial<FilterState>) => setFilter(pipelineId, patch),
    clearFilters: () => clearFilters(pipelineId),
    hasActiveFilters: !!hasActiveFilters,
  }
}
```

---

## State of the Art

| Old Approach | Current Approach | Impact for Phase 3 |
|--------------|------------------|--------------------|
| Manual WebSocket reconnect loop | `withAutomaticReconnect()` with configurable backoff | Use built-in; no custom loop needed |
| `Clients.All.SendAsync` for broadcast | `Clients.Group(name).SendAsync` | Scoped to pipeline — tenant isolation achieved at group membership level |
| `SendAsync` with string method names | `Hub<TClient>` strongly typed | Not required for v1; string names are sufficient given the small event set |
| Filtering in JS after full board fetch | Server-side filter query | Required to keep aggregates correct |
| `tsvector` for title search | `ILIKE` for v1 scale | No migration needed; upgrade path to GIN trigram index documented |

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `ILIKE` performance is acceptable for hundreds of cards per pipeline without an index | Search Endpoint | If card counts reach tens of thousands before a trigram index is added, search latency increases. Mitigation: add `CREATE INDEX CONCURRENTLY cards_title_trgm_idx USING gin (title gin_trgm_ops)` in a later migration |
| A2 | Per-pipeline Zustand filter state (keyed by pipelineId) is the right UX | Filter State | If users want to search across pipelines, this design doesn't support it. Out of scope for v1 (DISC-01 says "within a pipeline") |
| A3 | Self-invalidation during drag is suppressed by checking `draggingCardId` in Zustand | Pitfall 5 | If drag state is not in Zustand at the time of the event, the guard won't work. Mitigation: verify `draggingCardId` is set in `handleDragStart` (confirmed — it is in `BoardShell.tsx`) |
| A4 | `accessTokenFactory` fetching a token from `/api/signalr-token` on every reconnect is fast enough | JWT token delivery | Next.js route handler cold start adds ~50ms. For reconnects this is acceptable. |

---

## Open Questions

1. **Token expiry during long-running connections**
   - What we know: JwtConfig sets `setExpirationTime("1h")` for minted tokens. The `accessTokenFactory` is called before every HTTP request SignalR makes (including keep-alive negotiation requests).
   - What's unclear: Does `@microsoft/signalr@10.0.0` call `accessTokenFactory` often enough to keep the token fresh during a multi-hour session?
   - Recommendation: The `accessTokenFactory` is called before every reconnect negotiation. For a stable WebSocket connection, the factory is called at connection start only. The 1-hour expiry should be sufficient for a normal working session. If sessions exceed 1 hour, increase token expiry or implement token refresh in the factory.

2. **Broadcast to the card actor vs. excluding the actor**
   - What we know: `Clients.Group(...)` broadcasts to ALL connections in the group including the sender's own connection if they are also watching that board.
   - What's unclear: Should the user who triggered the mutation receive the broadcast? Their TanStack Query state is already updated via `onSettled`.
   - Recommendation: Broadcast to all including the actor. The double-invalidation (from `onSettled` + `hub.on`) is benign. Use `Clients.GroupExcept(groupName, Context.ConnectionId)` only if performance testing reveals excessive redundant re-fetches — not needed for v1.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| `@microsoft/signalr` | SignalR JS client | Yes [VERIFIED: package.json] | 10.0.0 | — |
| `Microsoft.AspNetCore.SignalR` | Hub infrastructure | Yes [VERIFIED: .csproj] | 1.2.9 | — |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | `EF.Functions.ILike` | Yes [VERIFIED: .csproj] | 9.0.4 | — |
| `zustand` | Filter state | Yes [VERIFIED: package.json] | 5.0.12 | — |
| PostgreSQL `ILIKE` operator | Title search | Yes (standard PostgreSQL) | Any | — |

**No missing dependencies.** All required libraries are already installed.

---

## Validation Architecture

Phase 3 adds real-time and search behaviors. Most real-time behavior is integration-level (requires a live hub + DB). Unit tests are appropriate for the EF Core search query logic.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit + Testcontainers (existing — `TEST-01` from Phase 1) |
| Config file | Existing test project |
| Quick run command | `dotnet test --filter "Category=Unit"` |
| Full suite command | `dotnet test` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | Notes |
|--------|----------|-----------|-------------------|-------|
| REAL-01 | Card move broadcast reaches group members | Integration | Manual (requires two browser sessions) | Hub group test via xUnit + IHubContext mock is possible but complex |
| REAL-02 | Card create/update broadcast | Integration | Manual | Same as REAL-01 |
| DISC-01 | `ILIKE` title search returns matching cards | Unit | `dotnet test --filter "CardSearch"` | Test the EF Core query with Testcontainers PostgreSQL |
| DISC-02 | Assignee/date/value filters combine with AND | Unit | `dotnet test --filter "CardSearch"` | Multiple filter combinations as separate test cases |

### Wave 0 Gaps
- [ ] `Nodefy.Tests/CardSearchTests.cs` — covers DISC-01 and DISC-02 with Testcontainers
- [ ] Manual test script for REAL-01/REAL-02: open two browser tabs, move a card in one, verify update in the other within 2 seconds

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | Yes | JWT bearer on hub via `OnMessageReceived` + `[Authorize]` on hub class |
| V3 Session Management | Yes | Hub group membership verified per-connection; group left on disconnect |
| V4 Access Control | Yes | Tenant membership verified in `JoinBoard` before adding to group |
| V5 Input Validation | Yes | `q` search param is parameterized via `EF.Functions.ILike`; Guid params are parsed with `Guid.TryParse` |
| V6 Cryptography | No | No new crypto — reuses existing HS256 JWT from `mintApiToken` |

### Known Threat Patterns

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Cross-tenant hub group access | Elevation of privilege | `JoinBoard` checks `workspace_members` before `Groups.AddToGroupAsync` |
| ILIKE wildcard abuse (search `%`) | Tampering (data exposure) | Parameterized query; `%` matches all cards but does not expose data beyond user's tenant |
| JWT token in WebSocket query string logged by server | Information disclosure | Document that server logs should not record query strings in production; use HTTPS (TLS encrypts URL) |
| Broadcast to wrong group | Elevation of privilege | Group name derived from `card.PipelineId` from DB (not from user input) in broadcast calls |
| Unauthenticated hub connection | Spoofing | `[Authorize]` on `BoardHub` class enforces JWT before any hub method runs |

---

## Sources

### Primary (HIGH confidence)
- `https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs?view=aspnetcore-9.0` — Hub class API, OnConnectedAsync, Context.User, Groups, IHubContext, DI injection into hubs
- `https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client?view=aspnetcore-9.0` — HubConnectionBuilder, withAutomaticReconnect, hub.on, onreconnected, accessTokenFactory
- `https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-9.0` — Bearer token auth, OnMessageReceived for WebSocket query string token
- Codebase: `api/Nodefy.Api/Hubs/BoardHub.cs` — current hub state
- Codebase: `api/Nodefy.Api/Auth/JwtConfig.cs` — JWT configuration (missing OnMessageReceived confirmed)
- Codebase: `api/Nodefy.Api/Endpoints/CardEndpoints.cs` — IgnoreQueryFilters bootstrap pattern
- Codebase: `frontend/package.json` — confirmed `@microsoft/signalr@10.0.0` installed
- Codebase: `frontend/src/hooks/use-board.ts` — TanStack Query pattern to extend
- Codebase: `frontend/src/store/ui-store.ts` — Zustand store to extend

### Secondary (MEDIUM confidence)
- `.planning/STATE.md` — "SignalR groups: `pipeline:{pipelineId}` with tenant verification on join before broadcast" (Architecture Notes — confirmed design intent)

### Tertiary (LOW confidence / ASSUMED)
- PostgreSQL `ILIKE` performance at v1 scale (hundreds of cards) — ASSUMED based on PostgreSQL sequential scan characteristics; not benchmarked against this specific schema

---

## Metadata

**Confidence breakdown:**
- SignalR hub design: HIGH — verified against official ASP.NET Core 9 docs + live codebase
- JWT auth for WebSocket: HIGH — `OnMessageReceived` pattern is explicitly documented as required
- Frontend hook pattern: HIGH — `withAutomaticReconnect` + `hub.on` + `accessTokenFactory` all from official docs
- Search/filter endpoint: HIGH for `ILIKE` query structure; MEDIUM for performance at scale (ASSUMED)
- Zustand filter state shape: MEDIUM — follows existing store pattern, not prescribed by a framework

**Research date:** 2026-04-20
**Valid until:** 2026-05-20 (30 days — stable Microsoft platform, slow-moving)
