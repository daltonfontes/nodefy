# Phase 2: Core Product - Pattern Map

**Mapped:** 2026-04-17
**Files analyzed:** 34 new/modified files
**Analogs found:** 34 / 34

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `api/Nodefy.Api/Data/Entities/Pipeline.cs` | model | CRUD | `api/Nodefy.Api/Data/Entities/Workspace.cs` | exact |
| `api/Nodefy.Api/Data/Entities/Stage.cs` | model | CRUD | `api/Nodefy.Api/Data/Entities/Workspace.cs` | exact |
| `api/Nodefy.Api/Data/Entities/ActivityLog.cs` | model | event-driven | `api/Nodefy.Api/Data/Entities/Workspace.cs` | role-match |
| `api/Nodefy.Api/Data/Entities/Card.cs` (extend) | model | CRUD | `api/Nodefy.Api/Data/Entities/Card.cs` | exact |
| `api/Nodefy.Api/Data/AppDbContext.cs` (extend) | config | CRUD | `api/Nodefy.Api/Data/AppDbContext.cs` | exact |
| `api/Nodefy.Api/Endpoints/PipelineEndpoints.cs` | controller | CRUD | `api/Nodefy.Api/Endpoints/WorkspaceEndpoints.cs` | exact |
| `api/Nodefy.Api/Endpoints/StageEndpoints.cs` | controller | CRUD | `api/Nodefy.Api/Endpoints/MemberEndpoints.cs` | exact |
| `api/Nodefy.Api/Endpoints/CardEndpoints.cs` | controller | CRUD | `api/Nodefy.Api/Endpoints/WorkspaceEndpoints.cs` | role-match |
| `api/Nodefy.Api/Endpoints/ActivityLogEndpoints.cs` | controller | request-response | `api/Nodefy.Api/Endpoints/MemberEndpoints.cs` | role-match |
| `api/Nodefy.Api/Program.cs` (extend) | config | — | `api/Nodefy.Api/Program.cs` | exact |
| `api/Nodefy.Tests/Integration/PipelineTests.cs` | test | request-response | `api/Nodefy.Tests/Integration/WorkspaceTests.cs` | exact |
| `api/Nodefy.Tests/Integration/StageTests.cs` | test | request-response | `api/Nodefy.Tests/Integration/MemberTests.cs` | exact |
| `api/Nodefy.Tests/Integration/BoardTests.cs` | test | request-response | `api/Nodefy.Tests/Integration/WorkspaceTests.cs` | role-match |
| `api/Nodefy.Tests/Integration/CardTests.cs` | test | request-response | `api/Nodefy.Tests/Integration/MemberTests.cs` | exact |
| `api/Nodefy.Tests/Integration/ActivityLogTests.cs` | test | request-response | `api/Nodefy.Tests/Integration/WorkspaceTests.cs` | role-match |
| `api/Nodefy.Tests/Unit/FractionalIndexTests.cs` | test | transform | `api/Nodefy.Tests/Unit/SlugTests.cs` | role-match |
| `frontend/src/app/workspace/[id]/pipeline/[pipelineId]/page.tsx` | component | request-response | `frontend/src/app/workspace/[id]/layout.tsx` | role-match |
| `frontend/src/components/board/BoardShell.tsx` | component | event-driven | `frontend/src/components/Providers.tsx` | role-match |
| `frontend/src/components/board/KanbanColumn.tsx` | component | event-driven | `frontend/src/app/workspace/[id]/settings/members/MemberTable.tsx` | role-match |
| `frontend/src/components/board/KanbanCard.tsx` | component | event-driven | `frontend/src/app/workspace/[id]/settings/members/MemberTable.tsx` | role-match |
| `frontend/src/components/board/CardDragOverlay.tsx` | component | event-driven | `frontend/src/app/workspace/[id]/settings/members/MemberTable.tsx` | partial |
| `frontend/src/components/board/CardDetailPanel.tsx` | component | CRUD | `frontend/src/app/workspace/[id]/settings/members/MemberTable.tsx` | role-match |
| `frontend/src/components/sidebar/PipelineSidebar.tsx` | component | request-response | `frontend/src/components/WorkspaceTopNav.tsx` | role-match |
| `frontend/src/components/sidebar/PipelineListItem.tsx` | component | event-driven | `frontend/src/app/workspace/[id]/settings/members/MemberTable.tsx` | role-match |
| `frontend/src/hooks/use-board.ts` | hook | CRUD | `frontend/src/app/workspace/[id]/settings/members/MemberTable.tsx` | role-match |
| `frontend/src/hooks/use-pipelines.ts` | hook | CRUD | `frontend/src/app/workspace/[id]/settings/members/MemberTable.tsx` | role-match |
| `frontend/src/hooks/use-stages.ts` | hook | CRUD | `frontend/src/app/workspace/[id]/settings/members/MemberTable.tsx` | role-match |
| `frontend/src/store/ui-store.ts` (extend) | store | event-driven | `frontend/src/store/ui-store.ts` | exact |
| `frontend/src/types/api.ts` (extend) | utility | — | `frontend/src/types/api.ts` | exact |
| `frontend/src/app/api/workspaces/[id]/pipelines/route.ts` | utility | request-response | `frontend/src/app/api/workspaces/[id]/members/route.ts` | exact |
| `frontend/src/app/api/pipelines/[id]/route.ts` | utility | request-response | `frontend/src/app/api/workspaces/[id]/members/[userId]/route.ts` | exact |
| `frontend/src/app/api/pipelines/[id]/board/route.ts` | utility | request-response | `frontend/src/app/api/workspaces/[id]/members/route.ts` | role-match |
| `frontend/src/app/api/cards/[id]/move/route.ts` | utility | request-response | `frontend/src/app/api/workspaces/[id]/members/route.ts` | role-match |
| `frontend/src/app/api/cards/[id]/route.ts` | utility | request-response | `frontend/src/app/api/workspaces/[id]/members/[userId]/route.ts` | exact |

---

## Pattern Assignments

### `api/Nodefy.Api/Data/Entities/Pipeline.cs` (model, CRUD)

**Analog:** `api/Nodefy.Api/Data/Entities/Workspace.cs`

**Entity pattern** (lines 1-11):
```csharp
namespace Nodefy.Api.Data.Entities;

public class Workspace
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Currency { get; set; } = "BRL";
    public bool CurrencyLocked { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; }
}
```

**Apply as:** Flat C# POCO, no attributes, no base class, no navigation properties. Pipeline entity shape:
```csharp
public class Pipeline
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = "";
    public double Position { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```
Snake_case column mapping is done exclusively in `AppDbContext.OnModelCreating`, never in the entity class.

---

### `api/Nodefy.Api/Data/Entities/Stage.cs` (model, CRUD)

**Analog:** `api/Nodefy.Api/Data/Entities/Workspace.cs`

Same POCO shape as Pipeline. Add `Guid PipelineId` FK:
```csharp
public class Stage
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PipelineId { get; set; }
    public string Name { get; set; } = "";
    public double Position { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

---

### `api/Nodefy.Api/Data/Entities/ActivityLog.cs` (model, event-driven)

**Analog:** `api/Nodefy.Api/Data/Entities/Workspace.cs`

`Payload` is stored as JSONB but mapped as `string` in EF Core (use `HasColumnType("jsonb")` in AppDbContext):
```csharp
public class ActivityLog
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CardId { get; set; }
    public Guid ActorId { get; set; }
    public string Action { get; set; } = "";
    public string Payload { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
}
```

---

### `api/Nodefy.Api/Data/Entities/Card.cs` (extend existing, CRUD)

**Analog:** `api/Nodefy.Api/Data/Entities/Card.cs` (itself, lines 1-10)

**Current state to extend from:**
```csharp
namespace Nodefy.Api.Data.Entities;

public class Card
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public double Position { get; set; } = 0.5;
    public DateTimeOffset StageEnteredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

Add these properties after `CreatedAt`, following same POCO convention:
```csharp
public string Title { get; set; } = "";
public string? Description { get; set; }
public decimal? MonetaryValue { get; set; }
public Guid PipelineId { get; set; }
public Guid StageId { get; set; }
public Guid? AssigneeId { get; set; }
public DateTimeOffset? CloseDate { get; set; }
public DateTimeOffset? ArchivedAt { get; set; }
```

---

### `api/Nodefy.Api/Data/AppDbContext.cs` (extend, config)

**Analog:** `api/Nodefy.Api/Data/AppDbContext.cs` (itself)

**DbSet registration pattern** (lines 17-21) — add three new sets after `Cards`:
```csharp
public DbSet<Card> Cards => Set<Card>();
// Phase 2 additions:
public DbSet<Pipeline> Pipelines => Set<Pipeline>();
public DbSet<Stage> Stages => Set<Stage>();
public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
```

**Snake_case column mapping pattern** (lines 39-47 for Workspace):
```csharp
modelBuilder.Entity<Workspace>(b =>
{
    b.ToTable("workspaces");
    b.Property(e => e.Id).HasColumnName("id");
    b.Property(e => e.Name).HasColumnName("name");
    b.Property(e => e.CreatedAt).HasColumnName("created_at");
});
```
Apply same `b.ToTable("pipelines")` + `b.Property(...).HasColumnName(...)` block for Pipeline, Stage, ActivityLog.

**Global query filter pattern** (lines 58-59 for WorkspaceMember):
```csharp
b.HasQueryFilter(m => m.TenantId == _tenantId);
```
Apply to Pipeline, Stage, ActivityLog. For Card (line 84), **upgrade** to dual-predicate:
```csharp
// Change from:
b.HasQueryFilter(c => c.TenantId == _tenantId);
// Change to:
b.HasQueryFilter(c => c.TenantId == _tenantId && c.ArchivedAt == null);
```

**Cascade delete pattern** — add inside Stage and ActivityLog entity mappings:
```csharp
// Stage → Pipeline cascade
b.HasOne<Pipeline>().WithMany().HasForeignKey(s => s.PipelineId)
    .OnDelete(DeleteBehavior.Cascade);
// ActivityLog → Card cascade
b.HasOne<Card>().WithMany().HasForeignKey(a => a.CardId)
    .OnDelete(DeleteBehavior.Cascade);
```

**Monetary value column type** — for Card.MonetaryValue in Card entity mapping:
```csharp
b.Property(e => e.MonetaryValue).HasColumnName("monetary_value").HasColumnType("numeric(15,2)");
```

**JSONB column type** — for ActivityLog.Payload in ActivityLog entity mapping:
```csharp
b.Property(e => e.Payload).HasColumnName("payload").HasColumnType("jsonb");
```

---

### `api/Nodefy.Api/Endpoints/PipelineEndpoints.cs` (controller, CRUD)

**Analog:** `api/Nodefy.Api/Endpoints/WorkspaceEndpoints.cs`

**Imports + namespace** (lines 1-8):
```csharp
using Microsoft.EntityFrameworkCore;
using Nodefy.Api.Auth;
using Nodefy.Api.Data;
using Nodefy.Api.Data.Entities;
using Nodefy.Api.Tenancy;

namespace Nodefy.Api.Endpoints;
```

**Static class + MapGroup + RequireAuthorization** (lines 17-19):
```csharp
public static IEndpointRouteBuilder MapWorkspaceEndpoints(this IEndpointRouteBuilder app)
{
    var group = app.MapGroup("/workspaces").RequireAuthorization();
```
Apply as: `app.MapGroup("/workspaces/{workspaceId:guid}/pipelines").RequireAuthorization()`

**Admin guard pattern** (lines 79-82):
```csharp
tenant.SetTenant(id);
if (!await WorkspaceEndpoints.IsAdmin(db, id, user.UserId)) return Results.Forbid();
```

**Validation pattern** (lines 24-25):
```csharp
if (string.IsNullOrWhiteSpace(req.Name) || req.Name.Length < 2)
    return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Nome é obrigatório (mínimo 2 caracteres)"] });
```

**POST + 201 Created pattern** (lines 56-57):
```csharp
return Results.Created($"/workspaces/{ws.Id}",
    new WorkspaceDto(ws.Id, ws.Name, ws.Slug, ws.Currency, ws.CurrencyLocked, "admin"));
```

**PATCH + FirstOrDefaultAsync + 404 guard** (lines 83-88):
```csharp
var ws = await db.Workspaces.FirstOrDefaultAsync(w => w.Id == id);
if (ws is null) return Results.NotFound();
ws.Currency = req.Currency;
await db.SaveChangesAsync();
return Results.Ok(new WorkspaceDto(...));
```

**Inline record DTOs** (lines 12-13):
```csharp
public record CreateWorkspaceRequest(string Name);
public record WorkspaceDto(Guid Id, string Name, string Slug, string Currency, bool CurrencyLocked, string? Role);
```

---

### `api/Nodefy.Api/Endpoints/StageEndpoints.cs` (controller, CRUD)

**Analog:** `api/Nodefy.Api/Endpoints/MemberEndpoints.cs`

**Route group nesting pattern** (lines 16-17):
```csharp
var group = app.MapGroup("/workspaces/{id:guid}/members").RequireAuthorization();
```
Apply as: `app.MapGroup("/pipelines/{pipelineId:guid}/stages").RequireAuthorization()`

**PATCH + MapPatch pattern** (lines 31-48):
```csharp
group.MapPatch("/{userId:guid}",
    async (Guid id, Guid userId, UpdateRoleRequest req, AppDbContext db, CurrentUserAccessor caller, ITenantService tenant) =>
{
    tenant.SetTenant(id);
    // validation...
    var member = await db.WorkspaceMembers.FirstOrDefaultAsync(m => m.UserId == userId);
    if (member is null) return Results.NotFound();
    member.Role = req.Role;
    await db.SaveChangesAsync();
    return Results.Ok(new { member.UserId, member.Role });
});
```

**Stage reorder endpoint** — `PATCH /{id:guid}/position` — unique to Phase 2. No existing analog for fractional indexing. After computing new position, call `FractionalIndex.NeedsRebalance` and return all sibling positions if rebalance occurred.

**DELETE pattern** (lines 51-60):
```csharp
group.MapDelete("/{userId:guid}",
    async (Guid id, Guid userId, AppDbContext db, CurrentUserAccessor caller, ITenantService tenant) =>
{
    // ...
    db.WorkspaceMembers.Remove(member);
    await db.SaveChangesAsync();
    return Results.NoContent();
});
```

---

### `api/Nodefy.Api/Endpoints/CardEndpoints.cs` (controller, CRUD)

**Analog:** `api/Nodefy.Api/Endpoints/WorkspaceEndpoints.cs` + `InviteEndpoints.cs`

Same static class + MapGroup pattern. Route: `app.MapGroup("/pipelines/{pipelineId:guid}/cards").RequireAuthorization()` for create; individual card routes at `/cards/{id:guid}`.

**Card move endpoint — critical rules (no codebase analog, from RESEARCH.md):**
1. Update `card.StageId` AND `card.StageEnteredAt = DateTimeOffset.UtcNow` in the same assignment block
2. Call `ActivityLogEndpoints.LogActivity(db, ...)` BEFORE `SaveChangesAsync()`
3. One `await db.SaveChangesAsync()` call total — log entry and card update saved atomically

**Member-level access check** — cards are not admin-only. Use:
```csharp
var isMember = await db.WorkspaceMembers.AnyAsync(m => m.UserId == user.UserId);
if (!isMember) return Results.Forbid();
```

**Board load endpoint** — `GET /pipelines/{id:guid}/board` — returns aggregated DTO. Use `OrderBy(s => s.Position)` for stages and `OrderBy(c => c.Position)` for cards within each stage. Compute `cardCount` and `monetarySum` via `Count()` and `Sum()` in EF Core projection (translates to SQL).

---

### `api/Nodefy.Api/Endpoints/ActivityLogEndpoints.cs` (controller, request-response)

**Analog:** `api/Nodefy.Api/Endpoints/MemberEndpoints.cs`

Single read endpoint + static `LogActivity` helper. No write endpoint exposed via HTTP.

**LogActivity helper pattern** (internal static method, not a service):
```csharp
internal static void LogActivity(AppDbContext db, Guid tenantId, Guid cardId,
    Guid actorId, string action, object payload)
{
    db.ActivityLogs.Add(new ActivityLog
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        CardId = cardId,
        ActorId = actorId,
        Action = action,
        Payload = System.Text.Json.JsonSerializer.Serialize(payload),
        CreatedAt = DateTimeOffset.UtcNow,
    });
    // Caller MUST call db.SaveChangesAsync() — never call it here
}
```

---

### `api/Nodefy.Api/Program.cs` (extend, config)

**Analog:** `api/Nodefy.Api/Program.cs` (lines 53-57)

**Endpoint registration pattern:**
```csharp
app.MapSsoSyncEndpoints();
app.MapWorkspaceEndpoints();
app.MapMemberEndpoints();
app.MapInviteEndpoints();
app.MapHub<BoardHub>("/hubs/board");
```

Add before `app.MapHub`:
```csharp
app.MapPipelineEndpoints();
app.MapStageEndpoints();
app.MapCardEndpoints();
app.MapActivityLogEndpoints();
```

---

### Integration Tests: `PipelineTests.cs`, `StageTests.cs`, `BoardTests.cs`, `CardTests.cs`, `ActivityLogTests.cs`

**Analog:** `api/Nodefy.Tests/Integration/WorkspaceTests.cs` and `MemberTests.cs`

**Test class boilerplate** (WorkspaceTests.cs lines 11-14):
```csharp
public class WorkspaceTests : IClassFixture<PostgresFixture>
{
    private readonly ApiFactory _factory;
    public WorkspaceTests(PostgresFixture fixture) => _factory = new ApiFactory(fixture.ConnectionString);
```

**CreateClient helper** (lines 16-23):
```csharp
private HttpClient CreateClient(Guid userId, string email, Guid? tenantId = null)
{
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
    client.DefaultRequestHeaders.Add("X-Test-Email", email);
    if (tenantId.HasValue)
        client.DefaultRequestHeaders.Add("X-Test-Tenant-Id", tenantId.Value.ToString());
    return client;
}
```

**SeedUser helper** (lines 25-34):
```csharp
private async Task SeedUser(HttpClient client, Guid userId, string email)
{
    await client.PostAsJsonAsync("/sso/sync", new
    {
        provider = "github",
        providerAccountId = userId.ToString(),
        email,
        name = "Test User",
        avatarUrl = (string?)null
    });
}
```

**Test fact pattern** (lines 37-51):
```csharp
[Fact]
public async Task CreateWorkspace_ReturnsCreated_WithCallerAsAdminMember()
{
    var userId = Guid.NewGuid();
    var client = CreateClient(userId, "user@test.com");
    await SeedUser(client, userId, "user@test.com");
    var resp = await client.PostAsJsonAsync("/workspaces", new { name = "Acme" });
    resp.StatusCode.Should().Be(HttpStatusCode.Created);
    var body = await resp.Content.ReadFromJsonAsync<WorkspaceDto>();
    body.Should().NotBeNull();
}
```

**Private DTO records** (line 136):
```csharp
private record WorkspaceDto(Guid Id, string Name, string Slug, string Currency, bool CurrencyLocked, string? Role);
```
Each test file declares its own private DTO records — never reference production DTOs.

**MemberTests CreateWorkspace helper** (lines 37-42) — copy this pattern for seed helpers in all new test files:
```csharp
private async Task<WorkspaceDto> CreateWorkspace(HttpClient client, string name)
{
    var resp = await client.PostAsJsonAsync("/workspaces", new { name });
    resp.StatusCode.Should().Be(HttpStatusCode.Created);
    return (await resp.Content.ReadFromJsonAsync<WorkspaceDto>())!;
}
```

**Direct DB manipulation in tests** (WorkspaceTests.cs lines 97-100) — for setting up test preconditions:
```csharp
using var scope = _factory.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<Nodefy.Api.Data.AppDbContext>();
var workspace = await db.Workspaces.FindAsync(ws!.Id);
workspace!.CurrencyLocked = true;
await db.SaveChangesAsync();
```

---

### `api/Nodefy.Tests/Unit/FractionalIndexTests.cs` (unit test, transform)

**Analog:** `api/Nodefy.Tests/Unit/SlugTests.cs`

**Unit test class pattern** (lines 1-6):
```csharp
using FluentAssertions;
using Nodefy.Api.Lib;
using Xunit;

namespace Nodefy.Tests.Unit;

public class SlugTests
{
```

**Theory with inline data** (lines 9-14):
```csharp
[Theory]
[InlineData("Vendas — Acme Corp", "vendas-acme-corp")]
[InlineData("Açaí & Café", "acai-cafe")]
public void GenerateSlug_HandlesAccents_LowercasesAndStripsDiacritics(string input, string expected)
    => Slug.Generate(input).Should().Be(expected);
```

Apply for FractionalIndex: `[InlineData(1_000_000.0, 2_000_000.0, 1_500_000.0)]` style tests for `Between()`, and `[Fact]` tests for `NeedsRebalance()` boundary conditions (`< 1e-9`, `> 1e15`).

---

### `frontend/src/app/workspace/[id]/pipeline/[pipelineId]/page.tsx` (RSC, request-response)

**Analog:** `frontend/src/app/workspace/[id]/layout.tsx`

**RSC with params + auth guard pattern** (lines 1-20):
```tsx
import { auth } from "@/auth"
import { redirect } from "next/navigation"
import { apiFetch } from "@/lib/api"
import type { Workspace } from "@/types/api"
import { WorkspaceTopNav } from "@/components/WorkspaceTopNav"

export default async function WorkspaceLayout({ children, params }: { children: React.ReactNode; params: Promise<{ id: string }> }) {
  const { id } = await params
  const session = await auth()
  if (!session) redirect(`/login?callbackUrl=/workspace/${id}`)
  const workspaces = await apiFetch<Workspace[]>("/workspaces")
  // ...
}
```

**Apply as:** Board page is an RSC that fetches initial pipeline/board data via `apiFetch` server-side, then renders `<BoardShell>` as a Client Component boundary. Layout stays `bg-background` (not `bg-secondary` — board has its own background treatment per UI-SPEC).

**apiFetch server-side usage pattern:**
```tsx
const boardData = await apiFetch<BoardData>(`/pipelines/${pipelineId}/board`, { tenantId: workspaceId })
```

---

### `frontend/src/components/board/BoardShell.tsx` (Client Component, event-driven)

**Analog:** `frontend/src/components/Providers.tsx`

**Client boundary wrapper pattern** (lines 1-16):
```tsx
"use client"
import { QueryClient, QueryClientProvider } from "@tanstack/react-query"
import { SessionProvider } from "next-auth/react"
import { useState } from "react"

export function Providers({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(
    () => new QueryClient({ defaultOptions: { queries: { staleTime: 60_000 } } })
  )
  return (
    <SessionProvider>
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    </SessionProvider>
  )
}
```

**Apply as:** `BoardShell` must be `"use client"` because dnd-kit requires client context. It wraps `DndContext` from `@dnd-kit/core`, renders `PipelineSidebar` and `KanbanBoard` side by side. It receives `initialData` prop from the RSC parent (hydration). Does NOT need its own `QueryClientProvider` — that is already in the root `Providers`.

---

### `frontend/src/components/board/KanbanColumn.tsx` (Client Component, event-driven)

**Analog:** `frontend/src/app/workspace/[id]/settings/members/MemberTable.tsx`

**Client component with hooks pattern** (lines 1-7):
```tsx
"use client"
import { useState } from "react"
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { Button } from "@/components/ui/button"
import { MoreHorizontal } from "lucide-react"
```

**DropdownMenu pattern** (lines 96-107):
```tsx
<DropdownMenu>
  <DropdownMenuTrigger asChild>
    <Button variant="ghost" size="icon" className="h-8 w-8">
      <MoreHorizontal className="h-4 w-4" />
    </Button>
  </DropdownMenuTrigger>
  <DropdownMenuContent align="end">
    <DropdownMenuItem onClick={() => ...}>Renomear</DropdownMenuItem>
    <DropdownMenuSeparator />
    <DropdownMenuItem className="text-destructive focus:text-destructive" onClick={() => ...}>
      Excluir estágio
    </DropdownMenuItem>
  </DropdownMenuContent>
</DropdownMenu>
```

**Dialog confirm pattern** (lines 118-131):
```tsx
<Dialog open={!!confirmRemove} onOpenChange={(o) => !o && setConfirmRemove(null)}>
  <DialogContent>
    <DialogHeader>
      <DialogTitle>Remover membro</DialogTitle>
      <DialogDescription>...</DialogDescription>
    </DialogHeader>
    <DialogFooter>
      <Button variant="outline" onClick={() => setConfirmRemove(null)}>Cancelar</Button>
      <Button variant="destructive" onClick={() => { ... }}>Remover</Button>
    </DialogFooter>
  </DialogContent>
</Dialog>
```
For stage delete, use `AlertDialog` (not `Dialog`) and context-specific cancel label per UI-SPEC copywriting contract.

---

### `frontend/src/components/board/KanbanCard.tsx` (Client Component, event-driven)

**Analog:** `frontend/src/app/workspace/[id]/settings/members/MemberTable.tsx`

**Avatar + Badge pattern** (lines 76-89):
```tsx
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"

// In render:
<Avatar className="h-8 w-8">
  {m.avatarUrl && <AvatarImage src={m.avatarUrl} />}
  <AvatarFallback>{(m.name ?? m.email)[0]?.toUpperCase()}</AvatarFallback>
</Avatar>
<Badge variant={m.role === "admin" ? "default" : "secondary"}>
  {m.role === "admin" ? "Admin" : "Membro"}
</Badge>
```

**Apply as:** Card uses 24px avatar (`className="h-6 w-6"`). Stage-age badge uses inline className (not `variant` prop) to achieve color semantics from UI-SPEC: `bg-slate-100 text-slate-500` / `bg-amber-100 text-amber-700` / `bg-red-100 text-red-600`.

`useSortable` hook is unique to Phase 2 — no codebase analog. Copy from RESEARCH.md Pattern (dnd-kit docs):
```tsx
import { useSortable } from "@dnd-kit/sortable"
import { CSS } from "@dnd-kit/utilities"

const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
  id: card.id,
  data: { card, stageId },
})
const style = { transform: CSS.Transform.toString(transform), transition, opacity: isDragging ? 0.4 : 1 }
```

---

### `frontend/src/components/board/CardDetailPanel.tsx` (Client Component, CRUD)

**Analog:** `frontend/src/app/workspace/[id]/settings/members/MemberTable.tsx`

**Optimistic mutation pattern** (lines 22-41):
```tsx
const qc = useQueryClient()
const roleMutation = useMutation({
  mutationFn: async ({ userId, role }: { userId: string; role: "admin" | "member" }) => {
    const res = await fetch(`/api/workspaces/${workspaceId}/members/${userId}`, {
      method: "PATCH", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ role }),
    })
    if (!res.ok) throw new Error(await res.text())
    return res.json()
  },
  onMutate: async ({ userId, role }) => {
    await qc.cancelQueries({ queryKey: ["members", workspaceId] })
    const previous = qc.getQueryData<Member[]>(["members", workspaceId])
    qc.setQueryData<Member[]>(["members", workspaceId], (old) => old?.map((m) => m.userId === userId ? { ...m, role } : m) ?? [])
    return { previous }
  },
  onError: (_err, _vars, ctx) => {
    if (ctx?.previous) qc.setQueryData(["members", workspaceId], ctx.previous)
    alert("Não foi possível alterar o papel. Tente novamente.")
  },
  onSettled: () => qc.invalidateQueries({ queryKey: ["members", workspaceId] }),
})
```

**shadcn Sheet usage** — no codebase analog. Copy from RESEARCH.md Pattern 7:
```tsx
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet"
const searchParams = useSearchParams()
const openCardId = searchParams.get("card")
```

URL-driven sheet state: `open={!!openCardId}`. Close by removing `?card=` param via `router.replace`.

---

### `frontend/src/components/sidebar/PipelineSidebar.tsx` (Client Component, request-response)

**Analog:** `frontend/src/components/WorkspaceTopNav.tsx`

**Layout component pattern** (lines 1-20):
```tsx
import Link from "next/link"
import { LogoutButton } from "./LogoutButton"
import { Logo } from "./Logo"

export function WorkspaceTopNav({ workspaceId, workspaceName }: { workspaceId: string; workspaceName: string }) {
  return (
    <header className="border-b bg-background">
      <div className="mx-auto flex max-w-6xl items-center justify-between p-4">
```

**Apply as:** PipelineSidebar is `"use client"` (needs Zustand for collapse state). Reads `sidebarCollapsed` from `useUIStore`. Width: `w-60` (240px) or `w-12` (48px) with `transition-all duration-200`. Uses `localStorage` persistence via Zustand persist middleware.

---

### `frontend/src/components/sidebar/PipelineListItem.tsx` (Client Component, event-driven)

**Analog:** `frontend/src/app/workspace/[id]/settings/members/MemberTable.tsx`

Copies the `DropdownMenu` + `MoreHorizontal` button pattern. Admin-only gate driven by `workspace.role === "admin"` from props. Inline rename: `useState<boolean>` for `isRenaming`, replaces pipeline name text with `<Input>` when true. Save on `blur`/`Enter`, cancel on `Escape`.

---

### `frontend/src/hooks/use-board.ts` (hook, CRUD)

**Analog:** `frontend/src/app/workspace/[id]/settings/members/MemberTable.tsx`

**useQuery + initialData pattern** (lines 15-19):
```tsx
const { data: members = initialMembers } = useQuery<Member[]>({
  queryKey: ["members", workspaceId],
  queryFn: async () => (await fetch(`/api/workspaces/${workspaceId}/members`)).json(),
  initialData: initialMembers,
})
```

Apply as board hook:
```tsx
export function useBoard(pipelineId: string, initialData: BoardData) {
  const { data: board = initialData } = useQuery<BoardData>({
    queryKey: ["board", pipelineId],
    queryFn: async () => (await fetch(`/api/pipelines/${pipelineId}/board`)).json(),
    initialData,
  })
  // ...
}
```

**Full optimistic mutation with cancelQueries + snapshot + rollback** (lines 22-41 in MemberTable.tsx) — this is the exact pattern for `moveMutation`. Key: `cancelQueries` MUST be the first line in `onMutate`.

**Client-side fetch** — note `MemberTable.tsx` calls `/api/workspaces/${workspaceId}/members` (the Next.js Route Handler proxy), NOT the backend directly. All client-side mutations in Phase 2 follow this same proxy pattern.

---

### `frontend/src/hooks/use-pipelines.ts` and `use-stages.ts` (hooks, CRUD)

**Analog:** `frontend/src/app/workspace/[id]/settings/members/MemberTable.tsx`

Same `useMutation` pattern. Pipeline/stage mutations use `/api/workspaces/[id]/pipelines/...` and `/api/pipelines/[id]/stages/...` Route Handler proxies. Optimistic update: `qc.setQueryData(["pipelines", workspaceId], (old) => ...)` with rollback in `onError`.

---

### `frontend/src/store/ui-store.ts` (extend, store)

**Analog:** `frontend/src/store/ui-store.ts` (itself, lines 1-11)

**Current store pattern:**
```ts
import { create } from "zustand"

interface UIState {
  activeWorkspaceId: string | null
  setActiveWorkspace: (id: string | null) => void
}

export const useUIStore = create<UIState>((set) => ({
  activeWorkspaceId: null,
  setActiveWorkspace: (id) => set({ activeWorkspaceId: id }),
}))
```

Add to `UIState` interface:
```ts
activePipelineId: string | null
setActivePipelineId: (id: string | null) => void

sidebarCollapsed: boolean
setSidebarCollapsed: (collapsed: boolean) => void

draggingCardId: string | null
setDraggingCardId: (id: string | null) => void
```

Add to `create<UIState>((set) => ({...}))` body:
```ts
activePipelineId: null,
setActivePipelineId: (id) => set({ activePipelineId: id }),

sidebarCollapsed: false,
setSidebarCollapsed: (collapsed) => set({ sidebarCollapsed: collapsed }),

draggingCardId: null,
setDraggingCardId: (id) => set({ draggingCardId: id }),
```

`sidebarCollapsed` MUST be persisted to `localStorage` key `nodefy_sidebar_collapsed` via Zustand `persist` middleware. Wrap `create` with `persist`:
```ts
import { persist } from "zustand/middleware"
export const useUIStore = create<UIState>()(
  persist(
    (set) => ({ ... }),
    { name: "nodefy_sidebar_collapsed", partialize: (s) => ({ sidebarCollapsed: s.sidebarCollapsed }) }
  )
)
```

---

### `frontend/src/types/api.ts` (extend, utility)

**Analog:** `frontend/src/types/api.ts` (itself, lines 1-29)

**Existing interface pattern:**
```ts
export interface Workspace {
  id: string
  name: string
  slug: string
  currency: "BRL" | "USD" | "EUR"
  currencyLocked: boolean
  role: "admin" | "member"
}
```

Add after existing interfaces:
```ts
export interface Pipeline {
  id: string
  name: string
  position: number
  createdAt: string
}

export interface Stage {
  id: string
  pipelineId: string
  name: string
  position: number
  cardCount: number
  monetarySum: number
}

export interface Card {
  id: string
  stageId: string
  pipelineId: string
  title: string
  description: string | null
  monetaryValue: number | null
  assigneeId: string | null
  closeDate: string | null
  position: number
  stageEnteredAt: string
  createdAt: string
}

export interface BoardData {
  pipeline: Pipeline
  stages: (Stage & { cards: Card[] })[]
}

export interface ActivityLog {
  id: string
  action: "created" | "moved" | "edited" | "archived"
  payload: Record<string, unknown>
  actorName: string
  createdAt: string
}
```

---

### Next.js Route Handler Proxies (utility, request-response)

**Analog:** `frontend/src/app/api/workspaces/[id]/members/route.ts` (lines 1-12)

**GET proxy pattern:**
```ts
import { NextResponse } from "next/server"
import { apiFetch } from "@/lib/api"

export async function GET(_req: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params
  try {
    const data = await apiFetch(`/workspaces/${id}/members`, { tenantId: id })
    return NextResponse.json(data)
  } catch (e: any) {
    return NextResponse.json({ error: e.message }, { status: 500 })
  }
}
```

**POST proxy pattern** (from `api/workspaces/proxy/route.ts` lines 1-12):
```ts
export async function POST(req: Request) {
  const body = await req.json()
  try {
    const created = await apiFetch<{ id: string }>("/workspaces", { method: "POST", body: JSON.stringify(body) })
    return NextResponse.json(created, { status: 201 })
  } catch (e: any) {
    return NextResponse.json({ error: e.message }, { status: 500 })
  }
}
```

Apply same pattern for all Phase 2 Route Handler proxies:
- `frontend/src/app/api/workspaces/[id]/pipelines/route.ts` — GET (list) + POST (create)
- `frontend/src/app/api/pipelines/[id]/route.ts` — PATCH (rename) + DELETE
- `frontend/src/app/api/pipelines/[id]/board/route.ts` — GET
- `frontend/src/app/api/cards/[id]/route.ts` — PATCH (edit fields) + archive
- `frontend/src/app/api/cards/[id]/move/route.ts` — PATCH

**Note:** `apiFetch` uses `auth()` from Auth.js v5 — server-only. It CANNOT be called from Client Components. Client Components call the Route Handler proxy with plain `fetch`. This is the established pattern in the codebase.

---

## Shared Patterns

### Admin Role Check (Backend)

**Source:** `api/Nodefy.Api/Endpoints/WorkspaceEndpoints.cs` lines 102-107
**Apply to:** PipelineEndpoints (all write ops), StageEndpoints (all write ops)

```csharp
internal static async Task<bool> IsAdmin(AppDbContext db, Guid workspaceId, Guid? userId)
{
    if (userId is null) return false;
    return await db.WorkspaceMembers.IgnoreQueryFilters()
        .AnyAsync(m => m.TenantId == workspaceId && m.UserId == userId && m.Role == "admin");
}
```

Call pattern at the top of every admin-only endpoint handler:
```csharp
tenant.SetTenant(workspaceId);
if (!await WorkspaceEndpoints.IsAdmin(db, workspaceId, user.UserId)) return Results.Forbid();
```

### Tenant Context Injection (Backend)

**Source:** `api/Nodefy.Api/Endpoints/MemberEndpoints.cs` lines 21-22
**Apply to:** All new endpoints that are tenant-scoped

```csharp
tenant.SetTenant(id);  // always first line in tenant-scoped handler
```

`ITenantService` is injected as a parameter. `TenantMiddleware` runs before endpoints and may pre-populate it from JWT claim, route `id`, or `X-Tenant-Id` header. Endpoints call `SetTenant` explicitly when the route carries an ID.

### Optimistic Update with Rollback (Frontend)

**Source:** `frontend/src/app/workspace/[id]/settings/members/MemberTable.tsx` lines 22-41
**Apply to:** All `useMutation` calls in `use-board.ts`, `use-pipelines.ts`, `use-stages.ts`, `CardDetailPanel.tsx`

Three mandatory hooks in order:
1. `onMutate`: `cancelQueries` FIRST, then snapshot, then `setQueryData` optimistically, then `return { previous }`
2. `onError`: `setQueryData(previous)` to restore snapshot
3. `onSettled`: `invalidateQueries` to re-sync with server

### Client-Side API Calls via Route Handler Proxy

**Source:** `frontend/src/app/workspace/[id]/settings/members/MemberTable.tsx` lines 23-28
**Apply to:** All Client Component mutations in board, sidebar, card panel

```tsx
const res = await fetch(`/api/workspaces/${workspaceId}/members/${userId}`, {
  method: "PATCH",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ role }),
})
if (!res.ok) throw new Error(await res.text())
return res.json()
```

Client code always calls `/api/...` (Next.js Route Handler). Never imports `apiFetch` in Client Components — `apiFetch` uses `auth()` which is server-only.

### EF Core Global Query Filter with Tenant

**Source:** `api/Nodefy.Api/Data/AppDbContext.cs` line 59 (WorkspaceMember), line 73 (Invitation), line 84 (Card)
**Apply to:** Pipeline, Stage, ActivityLog entities in AppDbContext

```csharp
b.HasQueryFilter(e => e.TenantId == _tenantId);
```

Never call `.IgnoreQueryFilters()` except in the two documented locations: `InviteEndpoints.AcceptInvite` (cross-tenant token lookup) and `WorkspaceEndpoints.GetWorkspaces` (cross-tenant membership scan).

### IgnoreQueryFilters Exceptions

**Source:** `api/Nodefy.Api/Endpoints/WorkspaceEndpoints.cs` lines 66-70 and `InviteEndpoints.cs` lines 51-53
**Apply to:** Card archive check (future), NOT to board/stage queries

```csharp
// ALLOWED: GetWorkspaces cross-tenant scan — filter is by user_id instead
var rows = await db.WorkspaceMembers.IgnoreQueryFilters()
    .Where(m => m.UserId == user.UserId)
    ...

// FORBIDDEN in Phase 2: any card/stage/pipeline query
```

### shadcn DropdownMenu Context Menu

**Source:** `frontend/src/app/workspace/[id]/settings/members/MemberTable.tsx` lines 96-111
**Apply to:** PipelineListItem `…` menu, KanbanColumn `…` menu

```tsx
<DropdownMenu>
  <DropdownMenuTrigger asChild>
    <Button variant="ghost" size="icon" className="h-8 w-8">
      <MoreHorizontal className="h-4 w-4" />
    </Button>
  </DropdownMenuTrigger>
  <DropdownMenuContent align="end">
    <DropdownMenuItem onClick={...}>Renomear</DropdownMenuItem>
    <DropdownMenuSeparator />
    <DropdownMenuItem className="text-destructive focus:text-destructive" onClick={...}>
      Excluir
    </DropdownMenuItem>
  </DropdownMenuContent>
</DropdownMenu>
```

---

## No Analog Found

Files where no close codebase match exists — use RESEARCH.md patterns instead:

| File | Role | Data Flow | Reason |
|---|---|---|---|
| `api/Nodefy.Api/Lib/FractionalIndex.cs` | utility | transform | No position-ordering utilities in codebase. Use RESEARCH.md Pattern 5 exactly. |
| dnd-kit DndContext orchestration in BoardShell | component | event-driven | No drag-and-drop components exist in codebase. Use RESEARCH.md Patterns 1, 2 (dnd-kit official docs). |
| shadcn Sheet usage in CardDetailPanel | component | CRUD | Sheet not yet in codebase (Phase 2 install). Use RESEARCH.md Pattern 7 (shadcn Sheet docs). |
| shadcn Popover for inline name inputs | component | event-driven | Popover not yet in codebase (Phase 2 install). Use shadcn Popover docs + UI-SPEC interaction contract. |

---

## Metadata

**Analog search scope:** `api/Nodefy.Api/Endpoints/`, `api/Nodefy.Api/Data/`, `api/Nodefy.Tests/`, `frontend/src/components/`, `frontend/src/app/`, `frontend/src/store/`, `frontend/src/lib/`, `frontend/src/types/`
**Files scanned:** 42
**Pattern extraction date:** 2026-04-17
