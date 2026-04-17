using Nodefy.Api.Tenancy;

namespace Nodefy.Api.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    public TenantMiddleware(RequestDelegate next) { _next = next; }

    public async Task InvokeAsync(HttpContext ctx, ITenantService tenantService)
    {
        // Source 1: explicit JWT claim
        var claim = ctx.User?.FindFirst("tenant_id")?.Value;
        // Source 2: route values (e.g., /workspaces/{id}/...)
        if (string.IsNullOrEmpty(claim) && ctx.Request.RouteValues.TryGetValue("workspaceId", out var wsId))
            claim = wsId?.ToString();
        // Source 3: header (set by frontend when active workspace is selected)
        if (string.IsNullOrEmpty(claim) && ctx.Request.Headers.TryGetValue("X-Tenant-Id", out var hdr))
            claim = hdr.ToString();
        // Source 4: route param `id` for /workspaces/{id}/...
        if (string.IsNullOrEmpty(claim) && ctx.Request.RouteValues.TryGetValue("id", out var routeId)
            && ctx.Request.Path.StartsWithSegments("/workspaces"))
            claim = routeId?.ToString();

        if (Guid.TryParse(claim, out var tenantId))
            tenantService.SetTenant(tenantId);
        await _next(ctx);
    }
}
