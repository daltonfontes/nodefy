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

-- Pipelines table (tenant-scoped)
CREATE TABLE pipelines (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    position DOUBLE PRECISION NOT NULL DEFAULT 0.0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_pipelines_tenant ON pipelines(tenant_id);

-- Stages table (tenant-scoped, belongs to a pipeline)
CREATE TABLE stages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    pipeline_id UUID NOT NULL REFERENCES pipelines(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    position DOUBLE PRECISION NOT NULL DEFAULT 0.0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_stages_tenant ON stages(tenant_id);
CREATE INDEX idx_stages_pipeline ON stages(pipeline_id);

-- Cards table (full Phase 2 schema)
CREATE TABLE cards (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    pipeline_id UUID NOT NULL REFERENCES pipelines(id) ON DELETE CASCADE,
    stage_id UUID NOT NULL REFERENCES stages(id) ON DELETE RESTRICT,
    title VARCHAR(200) NOT NULL DEFAULT '',
    description TEXT,
    monetary_value NUMERIC(15,2),
    assignee_id UUID REFERENCES users(id) ON DELETE SET NULL,
    position DOUBLE PRECISION NOT NULL DEFAULT 0.5,
    stage_entered_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    close_date TIMESTAMPTZ,
    archived_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_cards_tenant ON cards(tenant_id);
CREATE INDEX idx_cards_stage ON cards(stage_id);
CREATE INDEX idx_cards_pipeline ON cards(pipeline_id);

-- Activity logs table (tenant-scoped, linked to a card)
CREATE TABLE activity_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    card_id UUID NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
    actor_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    action VARCHAR(100) NOT NULL,
    payload JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_activity_logs_card ON activity_logs(card_id);

-- ============================================================================
-- Row-Level Security (second isolation layer below EF Core global filter)
-- ============================================================================

ALTER TABLE workspace_members ENABLE ROW LEVEL SECURITY;
ALTER TABLE workspace_members FORCE ROW LEVEL SECURITY;
ALTER TABLE invitations ENABLE ROW LEVEL SECURITY;
ALTER TABLE invitations FORCE ROW LEVEL SECURITY;
ALTER TABLE pipelines ENABLE ROW LEVEL SECURITY;
ALTER TABLE pipelines FORCE ROW LEVEL SECURITY;
ALTER TABLE stages ENABLE ROW LEVEL SECURITY;
ALTER TABLE stages FORCE ROW LEVEL SECURITY;
ALTER TABLE cards ENABLE ROW LEVEL SECURITY;
ALTER TABLE cards FORCE ROW LEVEL SECURITY;
ALTER TABLE activity_logs ENABLE ROW LEVEL SECURITY;
ALTER TABLE activity_logs FORCE ROW LEVEL SECURITY;

-- Policies reference the session variable that TenantDbConnectionInterceptor
-- (Phase 1.2) sets at every connection open: SET app.current_tenant = '<uuid>'.
CREATE POLICY tenant_isolation_policy ON workspace_members
    USING (tenant_id = current_setting('app.current_tenant', true)::UUID);

CREATE POLICY tenant_isolation_policy ON invitations
    USING (tenant_id = current_setting('app.current_tenant', true)::UUID);

CREATE POLICY tenant_isolation_policy ON pipelines
    USING (tenant_id = current_setting('app.current_tenant', true)::UUID);

CREATE POLICY tenant_isolation_policy ON stages
    USING (tenant_id = current_setting('app.current_tenant', true)::UUID);

CREATE POLICY tenant_isolation_policy ON cards
    USING (tenant_id = current_setting('app.current_tenant', true)::UUID);

CREATE POLICY tenant_isolation_policy ON activity_logs
    USING (tenant_id = current_setting('app.current_tenant', true)::UUID);

-- Note: workspaces table is NOT RLS-protected. Users discover their workspaces
-- via membership joins (handled in Phase 1.2 backend logic).
-- users table is NOT RLS-protected — global lookup table.

-- ============================================================================
-- Application role (non-superuser) — RLS is enforced at this privilege level.
-- nodefy_app (POSTGRES_USER) is a superuser and bypasses RLS.
-- nodefy_api is used in integration tests to verify Postgres-level isolation.
-- ============================================================================
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'nodefy_api') THEN
        CREATE ROLE nodefy_api NOLOGIN;
    END IF;
END $$;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO nodefy_api;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO nodefy_api;
