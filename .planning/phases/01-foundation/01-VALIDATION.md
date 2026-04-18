---
phase: 1
slug: foundation
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-16
---

# Phase 1 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET 9) + TestContainers (backend); Jest/vitest (frontend) |
| **Config file** | `backend/Nodefy.Tests/Nodefy.Tests.csproj` / `frontend/package.json` |
| **Quick run command** | `dotnet test --filter Category=Unit` |
| **Full suite command** | `dotnet test && cd frontend && npm test -- --run` |
| **Estimated runtime** | ~60 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter Category=Unit`
- **After every plan wave:** Run `dotnet test && cd frontend && npm test -- --run`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 60 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 1-01-01 | 01 | 1 | WORK-01 | — | Tenant isolation: cross-tenant queries return 0 rows | integration | `dotnet test --filter "TenantIsolation"` | ❌ W0 | ⬜ pending |
| 1-02-01 | 02 | 1 | AUTH-01 | — | SSO callback creates user record with non-null email | unit | `dotnet test --filter "SsoCallback"` | ❌ W0 | ⬜ pending |
| 1-02-02 | 02 | 1 | AUTH-02 | — | GitHub null email fallback hits /user/emails endpoint | unit | `dotnet test --filter "GitHubEmailFallback"` | ❌ W0 | ⬜ pending |
| 1-02-03 | 02 | 2 | WORK-02 | — | Workspace create scopes all reads to tenantId | integration | `dotnet test --filter "WorkspaceCreate"` | ❌ W0 | ⬜ pending |
| 1-02-04 | 02 | 2 | WORK-03 | — | Invite token is 32-byte random, expires 7 days | unit | `dotnet test --filter "InviteToken"` | ❌ W0 | ⬜ pending |
| 1-02-05 | 02 | 3 | WORK-04 | — | Invitee accept sets correct role | integration | `dotnet test --filter "InviteAccept"` | ❌ W0 | ⬜ pending |
| 1-02-06 | 02 | 3 | WORK-05 | — | Admin promote/demote changes role in DB | integration | `dotnet test --filter "MemberRole"` | ❌ W0 | ⬜ pending |
| 1-02-07 | 02 | 3 | WORK-06 | — | Admin remove deletes membership row | integration | `dotnet test --filter "RemoveMember"` | ❌ W0 | ⬜ pending |
| 1-03-01 | 03 | 1 | AUTH-03 | — | SSO login page renders all 3 provider buttons | manual | — | — | ⬜ pending |
| 1-03-02 | 03 | 2 | AUTH-04 | — | Session persists across browser refresh (HttpOnly cookie) | manual | — | — | ⬜ pending |
| 1-03-03 | 03 | 2 | AUTH-05 | — | Logout invalidates session, redirects to login | manual | — | — | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `backend/Nodefy.Tests/Foundation/TenantIsolationTests.cs` — stubs for WORK-01
- [ ] `backend/Nodefy.Tests/Foundation/SsoCallbackTests.cs` — stubs for AUTH-01, AUTH-02
- [ ] `backend/Nodefy.Tests/Foundation/WorkspaceTests.cs` — stubs for WORK-02 through WORK-06
- [ ] `backend/Nodefy.Tests/Fixtures/DatabaseFixture.cs` — TestContainers PostgreSQL shared fixture
- [ ] `dotnet add backend/Nodefy.Tests package Testcontainers.PostgreSql` — if not already installed

*Wave 0 creates stub test files before any implementation starts.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| SSO login page renders GitHub/Google/Microsoft buttons | AUTH-03 | Browser OAuth redirect flow, no headless support | Navigate to /login, confirm 3 provider buttons visible |
| Session survives browser refresh | AUTH-04 | Cookie persistence is browser-level | Log in, refresh, confirm user still shown as authenticated |
| Logout invalidates session | AUTH-05 | Server-side session cookie deletion | Log in, click logout, confirm redirect to /login and session cookie cleared |
| Invite link copy-to-clipboard | WORK-03 | UI interaction | Admin creates invite, confirm URL in clipboard input, copy works |
| Invitee accept flow end-to-end | WORK-04 | Multi-tab OAuth flow | Open invite link in incognito, complete SSO, confirm workspace access |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
