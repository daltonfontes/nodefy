---
slug: fix-card-creation-500
status: complete
completed: 2026-04-20
---

## What was done

Fixed POST /api/pipelines/{id}/cards 500 error caused by PostgreSQL RLS blocking the INSERT.

Root cause: `TenantDbConnectionInterceptor` sets `app.current_tenant` only at connection-open time.
When no tenant context is present in the request (no JWT `tenant_id`, no `X-Tenant-Id` header),
the session variable is never set. The endpoint's `tenant.SetTenant(pipeline.TenantId)` call
updates the service but not the already-open connection, so the RLS policy rejects the INSERT.

## Files changed

- `frontend/src/app/api/pipelines/[id]/cards/route.ts` — extract `workspaceId` query param, pass as `tenantId` to `apiFetch`
- `frontend/src/components/board/BoardShell.tsx` — append `?workspaceId=${workspaceId}` to the card creation fetch URL
