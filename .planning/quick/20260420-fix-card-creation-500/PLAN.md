---
slug: fix-card-creation-500
created: 2026-04-20
status: in-progress
---

# Fix card creation 500

## Problem

POST /api/pipelines/{id}/cards returns 500. Root cause: TenantDbConnectionInterceptor only sets
`app.current_tenant` when the DB connection is opened, at which point ITenantService.TenantId is
Guid.Empty (no X-Tenant-Id header or JWT tenant_id claim sent). The endpoint calls
`tenant.SetTenant(pipeline.TenantId)` after the connection is already open, so the PostgreSQL RLS
policy blocks the INSERT into cards/activity_logs with a policy violation error.

## Fix

Mirror the board route pattern: pass workspaceId as a query parameter from BoardShell.tsx, extract
it in the Next.js cards route handler, and forward it as `tenantId` to `apiFetch` so the JWT
includes `tenant_id` and `TenantMiddleware` sets the tenant before the DB connection opens.

## Tasks

1. `frontend/src/app/api/pipelines/[id]/cards/route.ts` — extract `workspaceId` query param, pass as `tenantId` to `apiFetch`
2. `frontend/src/components/board/BoardShell.tsx` — append `?workspaceId=${workspaceId}` to the fetch URL in `handleCreateCard`
