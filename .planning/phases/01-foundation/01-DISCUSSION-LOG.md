# Phase 1: Foundation - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-16
**Phase:** 01-foundation
**Areas discussed:** Monetary value locale, Multi-workspace per user, Login & first-run UX

---

## Monetary Value Locale

| Option | Description | Selected |
|--------|-------------|----------|
| BRL (pt-BR) hardcoded | Always R$, simple DECIMAL(15,2) schema, no locale column | |
| Configurable per workspace | Each workspace sets currency/locale; requires workspace settings table | ✓ |
| USD (en-US) hardcoded | Always $, simpler for international market | |

**User's choice:** Configurable per workspace

---

### Currencies supported

| Option | Description | Selected |
|--------|-------------|----------|
| BRL + USD only | Two currencies, simpler dropdown | |
| Full ISO 4217 list | Every standard currency | |
| BRL + USD + EUR | Three most common for B2B SaaS in LATAM with international clients | ✓ |

**User's choice:** BRL + USD + EUR

---

### Default currency for new workspaces

| Option | Description | Selected |
|--------|-------------|----------|
| BRL | Sensible default for Brazilian-market product | ✓ |
| No default — require selection at creation | Forces pick during workspace creation | |

**User's choice:** BRL

---

### Currency change policy

| Option | Description | Selected |
|--------|-------------|----------|
| Workspace settings, always changeable | Admin can update anytime; display format changes, stored values stay | |
| Workspace settings, locked after first card | Guard on PATCH endpoint once any card exists | ✓ |

**User's choice:** Locked after first card is created

---

## Multi-Workspace Per User

| Option | Description | Selected |
|--------|-------------|----------|
| Yes — user can belong to multiple workspaces | Realistic SaaS model; workspace selector on login | ✓ |
| No — one workspace per user | Simpler; relax in v2 | |

**User's choice:** Yes, users can belong to multiple workspaces

---

### Multiple-workspace post-login behavior

| Option | Description | Selected |
|--------|-------------|----------|
| Show workspace selector screen | User picks workspace; last used remembered | ✓ |
| Always enter most recently used | Skip selector; switch via header dropdown | |

**User's choice:** Show workspace selector screen

---

### Can users create multiple workspaces?

| Option | Description | Selected |
|--------|-------------|----------|
| Yes — any authenticated user can create workspaces | No restriction | ✓ |
| No — one created workspace per user | Can still be member of others | |

**User's choice:** Yes, no creation restriction

---

### First login (no workspaces)

| Option | Description | Selected |
|--------|-------------|----------|
| Auto-redirect to "Create workspace" page | Smooth onboarding, no dead end | ✓ |
| Empty workspace selector with CTA | Same screen as returning user, empty state | |

**User's choice:** Auto-redirect to "Create workspace" page

---

### Workspace creation fields

| Option | Description | Selected |
|--------|-------------|----------|
| Name only | Slug auto-generated; fastest onboarding | ✓ |
| Name + slug | User controls URL slug | |
| Name + slug + currency | Includes currency at creation time | |

**User's choice:** Name only (slug auto-generated)

---

## Login & First-Run UX

| Option | Description | Selected |
|--------|-------------|----------|
| Centered card with 3 SSO buttons | Product name/logo + stacked GitHub/Google/Microsoft buttons | ✓ |
| Full-page split layout | Left: hero/branding. Right: login card | |
| You decide | Leave to Claude | |

**User's choice:** Centered card with 3 SSO buttons stacked vertically

---

### SSO failure handling

| Option | Description | Selected |
|--------|-------------|----------|
| Show error message on login page | Inline error, user stays on login | ✓ |
| Redirect to dedicated error page | /auth/error page with Try again link | |

**User's choice:** Inline error on login page

---

### Post-workspace-creation landing

| Option | Description | Selected |
|--------|-------------|----------|
| Empty pipeline board with "Create your first pipeline" CTA | Fast path to core product | ✓ |
| Workspace settings/onboarding wizard | Guided checklist before board | |

**User's choice:** Empty pipeline board + CTA

---

### Dark mode

| Option | Description | Selected |
|--------|-------------|----------|
| Light mode only | One color system, simpler | ✓ |
| System preference (auto dark/light) | Respects OS; needs both color palettes | |
| You decide | Leave to Claude | |

**User's choice:** Light mode only in v1

---

## Claude's Discretion

- **Invite flow delivery:** Not discussed — Claude decides between SMTP email and in-app shareable link.
- **Workspace selector UI details:** Layout treatment (list vs. card grid, last-used highlighting) — Claude decides.

## Deferred Ideas

- Full ISO 4217 currency list — v2
- Dark mode — v2 (requires full design pass)
- Workspace onboarding wizard — v2
