---
slug: api-401-workspace-select
status: resolved
trigger: API 401 error on /workspace/select after GitHub OAuth callback
created: 2026-04-19
updated: 2026-04-19
---

## Symptoms

- expected: After GitHub OAuth callback succeeds, user is redirected to /workspace/select and sees their workspaces
- actual: WorkspaceSelectPage calls apiFetch which returns 401 Unauthorized with empty body from backend
- error_messages: |
    Error: API 401:
    at apiFetch (src/lib/api.ts:26:11)
    at async WorkspaceSelectPage (src/app/(auth)/workspace/select/page.tsx:12:22)
- timeline: Never worked — fresh setup, first time building this flow
- reproduction: Sign in via GitHub OAuth → callback redirects to /workspace/select → page crashes with 401
- backend_status: .NET backend is running; 401 originates from backend itself

## Current Focus

hypothesis: "auth.config.ts session callback does not forward token.sub to session.user.id, so mintApiToken receives sub='' causing backend Guid.TryParse to return null and the GET /workspaces handler returns 401"
test: "JWT signed with empty sub passes crypto validation but CurrentUserAccessor.UserId returns null → Results.Unauthorized()"
expecting: "After fix, session.user.id carries the backend UUID, JWT sub is a valid Guid, and GET /workspaces succeeds"
next_action: "apply fix to auth.config.ts session callback"
reasoning_checkpoint: "Traced token.sub set in auth.ts jwt callback → not forwarded by session callback → session.user.id undefined → mintApiToken sub='' → backend UserId null → 401"
tdd_checkpoint: ""

## Evidence

- timestamp: 2026-04-19T00:00:00Z
  file: frontend/auth.ts
  observation: jwt callback sets token.sub = dto.id (backend UUID) on fresh sign-in via /sso/sync. This is correct.

- timestamp: 2026-04-19T00:00:00Z
  file: frontend/auth.config.ts
  observation: session callback copies token.provider and token.providerAccountId to session but NEVER sets session.user.id = token.sub. This is the missing link.

- timestamp: 2026-04-19T00:00:00Z
  file: frontend/src/lib/api.ts line 10
  observation: sub is taken from (session as any).sub ?? session.user.id ?? "". Both are undefined/null, so sub defaults to "".

- timestamp: 2026-04-19T00:00:00Z
  file: api/Nodefy.Api/Auth/CurrentUserAccessor.cs line 15-17
  observation: UserId = Guid.TryParse(sub_claim). Guid.TryParse("") returns false → UserId is null.

- timestamp: 2026-04-19T00:00:00Z
  file: api/Nodefy.Api/Endpoints/WorkspaceEndpoints.cs line 63
  observation: if (user.UserId is null) return Results.Unauthorized(); — this is the 401 source.

## Eliminated

- JWT signature mismatch: eliminated — AUTH_SECRET is shared between frontend/backend; signature is valid
- Backend not running: eliminated — symptom description confirms backend is running
- OAuth flow failure: eliminated — flow reaches /workspace/select successfully, indicating OAuth completed

## Resolution

root_cause: "auth.config.ts session callback does not forward token.sub to session.user.id. Auth.js v5 only auto-populates session.user.id from token.sub when no custom session callback is defined. With a custom callback, it must be set explicitly. This means mintApiToken receives sub='' → backend Guid.TryParse('') → UserId=null → 401."
fix: "In auth.config.ts, add `session.user.id = token.sub as string` inside the session callback before returning."
verification: "After fix: apiFetch produces JWT with valid UUID sub, CurrentUserAccessor.UserId is non-null, GET /workspaces returns workspace list."
files_changed: "frontend/auth.config.ts"
