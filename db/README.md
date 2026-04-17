# db/init.sql — Nodefy First Migration

This file is the first and only database migration for Phase 1 of Nodefy. It is mounted read-only into the Postgres container at `/docker-entrypoint-initdb.d/init.sql` and executed automatically on first boot when the data volume is empty. The migration creates all core tables (`users`, `workspaces`, `workspace_members`, `invitations`, and the stub `cards` table), enables Row-Level Security (RLS) on all tenant-scoped tables, and installs the `tenant_isolation_policy` that enforces cross-tenant data isolation via the `app.current_tenant` session variable.

## IMPORTANT: nodefy_app Role Must Stay Non-Superuser

The `nodefy_app` Postgres role is created by Docker via the `POSTGRES_USER` environment variable, which creates a non-superuser role by default. **Never alter this role to add `SUPERUSER` or `BYPASSRLS`.** PostgreSQL Row-Level Security policies are silently bypassed for superusers, which would completely defeat the tenant isolation layer. If you need maintenance operations that require elevated privileges, connect as the system `postgres` superuser separately — never elevate `nodefy_app`. The `db/init.sql` file contains explicit comment guards as a reminder.

## Fractional Indexing Columns

The `cards` table includes `position DOUBLE PRECISION` and `stage_entered_at TIMESTAMPTZ` in this first migration intentionally. These columns cannot be added later without a data migration on every existing card row. Phase 2 will extend the `cards` table with additional columns (title, description, pipeline_id, stage_id, etc.) via a subsequent migration, but the ordering infrastructure must exist from day one.
