-- Nodefy first migration — Phase 1
-- CRITICAL: nodefy_app role is created by POSTGRES_USER docker env var.
-- It must be kept as a standard login role with no elevated privileges.
-- POSTGRES_USER creates a standard (non-elevated) role by default — DO NOT alter that.

-- Required extensions
CREATE EXTENSION IF NOT EXISTS "pgcrypto";  -- gen_random_uuid()

-- ============================================================================
-- Tables
-- ============================================================================

-- Users table (global — no tenant_id)
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    name VARCHAR(100),
    avatar_url TEXT,
    provider VARCHAR(20) NOT NULL,
    provider_account_id VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(provider, provider_account_id)
);

-- Workspaces table (tenant root — id IS the tenant_id)
CREATE TABLE workspaces (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    slug VARCHAR(50) UNIQUE NOT NULL,
    currency VARCHAR(3) NOT NULL DEFAULT 'BRL',
    currency_locked BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT workspaces_currency_check CHECK (currency IN ('BRL', 'USD', 'EUR'))
);

-- WorkspaceMembers table (tenant-scoped)
CREATE TABLE workspace_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role VARCHAR(20) NOT NULL DEFAULT 'member',
    joined_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(tenant_id, user_id),
    CONSTRAINT workspace_members_role_check CHECK (role IN ('admin', 'member'))
);
CREATE INDEX idx_workspace_members_tenant ON workspace_members(tenant_id);
CREATE INDEX idx_workspace_members_user ON workspace_members(user_id);

-- Invitations table (tenant-scoped)
CREATE TABLE invitations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    email VARCHAR(255) NOT NULL,
    role VARCHAR(20) NOT NULL DEFAULT 'member',
    token VARCHAR(255) UNIQUE NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    accepted_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT invitations_role_check CHECK (role IN ('admin', 'member'))
);
CREATE INDEX idx_invitations_tenant ON invitations(tenant_id);
CREATE INDEX idx_invitations_token ON invitations(token);

-- Cards STUB table — Phase 2 implements full schema, but the columns below
-- MUST exist in the first migration (Pitfall 7 / STATE.md key decisions).
-- They are not retrofittable without a data migration on every existing row.
CREATE TABLE cards (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    position DOUBLE PRECISION NOT NULL DEFAULT 0.5,
    stage_entered_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
    -- Phase 2 adds: title, description, monetary_value, assignee_id, close_date,
    --               pipeline_id, stage_id, archived_at, etc.
);
CREATE INDEX idx_cards_tenant ON cards(tenant_id);

-- ============================================================================
-- Row-Level Security (second isolation layer below EF Core global filter)
-- ============================================================================

ALTER TABLE workspace_members ENABLE ROW LEVEL SECURITY;
ALTER TABLE invitations ENABLE ROW LEVEL SECURITY;
ALTER TABLE cards ENABLE ROW LEVEL SECURITY;

-- Policies reference the session variable that TenantDbConnectionInterceptor
-- (Phase 1.2) sets at every connection open: SET app.current_tenant = '<uuid>'.
CREATE POLICY tenant_isolation_policy ON workspace_members
    USING (tenant_id = current_setting('app.current_tenant', true)::UUID);

CREATE POLICY tenant_isolation_policy ON invitations
    USING (tenant_id = current_setting('app.current_tenant', true)::UUID);

CREATE POLICY tenant_isolation_policy ON cards
    USING (tenant_id = current_setting('app.current_tenant', true)::UUID);

-- Note: workspaces table is NOT RLS-protected. Users discover their workspaces
-- via membership joins (handled in Phase 1.2 backend logic).
-- users table is NOT RLS-protected — global lookup table.

-- ============================================================================
-- Role privilege guard (for human review):
-- POSTGRES_USER creates a standard login role by default — keep it that way.
-- WARNING: Do NOT grant nodefy_app any elevated database privileges.
-- Granting elevated privileges would silently disable all RLS tenant isolation.
-- See db/README.md for details.
-- ============================================================================
