# Feature Landscape: Nodefy

**Domain:** Multi-tenant SaaS CRM with visual Kanban pipelines
**Researched:** 2026-04-16
**Confidence note:** WebSearch and WebFetch were unavailable. All findings derive from training
knowledge (cutoff August 2025) of Pipefy, Trello, Monday.com, HubSpot, and the broader
pipeline-CRM market. Confidence levels reflect source quality per domain.

---

## Table Stakes

Features users expect when they arrive at a pipeline CRM. Absence causes immediate abandonment.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Kanban board with columns | Core interaction contract. Every competitor has it. | Low | Columns = pipeline stages. Must render fast even with 50+ cards. |
| Drag-and-drop card movement | The "feel" of a Kanban product. Users test this in the first 60 seconds. | Medium | Requires smooth animation and optimistic UI update before server confirms. |
| Card detail view (title, description, assignee, due date, value) | Every CRM card has a contextual form. | Low | Side panel preferred to keep pipeline context visible. |
| Multiple pipelines per workspace | Sales teams always have more than one process (SDR, AE, Onboarding). | Low | Critical to not hard-code "one pipeline per org" in the data model. |
| Customizable stages per pipeline | Every company's process is different. | Low | Add/rename/delete stages is table stakes. Reordering via drag is secondary. |
| Card search | Users lose cards within days. Search is the escape hatch. | Low | Full-text on title minimum. |
| Card filters (assignee, date, value) | Managers live in filtered views. Without filters the board becomes unusable past ~30 cards. | Low | Filter bar above board. Multi-filter AND logic. |
| Activity log on each card | "Who moved this? When was it edited?" — asked on day 1. | Medium | Append-only log: creation, stage moves, field edits, comments. |
| Card comments | Async communication on the work item itself. | Low | Plain-text comments minimum. |
| Multi-tenant workspace isolation | Users assume their data is invisible to others. | High | Row-level security or schema-per-tenant in PostgreSQL. Non-negotiable for B2B SaaS. |
| Invite team members with roles | No useful CRM works solo. Admin vs. member minimum. | Low | Email-based invite with pending state. |
| Near-real-time board updates | If a teammate moves a card, my board should reflect it. | Medium | SignalR or short-interval polling (5s). Polling acceptable for v1. |

**Confidence:** HIGH

---

## Differentiators

Features that set Nodefy apart. Not expected on arrival, but highly valued once discovered.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Column header: card count + total deal value | "12 deals / R$480k in Negotiation" — instant situational awareness. | Low | `SUM(value)` per stage in the column header. Trivial query, high perceived value. |
| Stage-age indicator on each card | "This deal has been in Proposal for 23 days." Age-in-stage is a coaching metric. | Low | Store `entered_at` on the card↔stage relation. Render as "23d in stage". **Must be in initial data model.** |
| Pipeline templates | "Start from a Sales template" — reduces blank-slate anxiety, cuts time-to-first-value. | Low-Medium | Ship 3–4 curated templates: B2B Sales, Onboarding, Support, Recruiting. |
| Collapsible pipeline stages | Large pipelines (10+ stages) overflow horizontally. | Low | Pure UI toggle stored in localStorage. No server state needed. |
| Card label / tag system | A card can belong to multiple dimensions: "hot lead", "blocked", "enterprise". | Medium | Tags normalized in DB. Tag filter added to filter bar. |
| Keyboard shortcut: new card in focused column | `N` to open card creation in the currently-hovered column. | Low | Global hotkey layer. High delight-to-effort ratio. |

**Confidence:** MEDIUM

---

## Anti-Features

Features to explicitly NOT build in v1.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| Automations / workflow triggers | Adds weeks of backend complexity. Complex rule editor, hard to debug. | Manual board movement for v1. Ship in v2 after observing real workflows. |
| Custom fields on cards | Each field type (dropdown, date, number, checkbox) is its own mini-feature. | Fixed schema: title, description, value, assignee, due date, tags. |
| Multiple views (List, Gantt, Calendar) | Every view is a separate rendering engine. Confuses core Kanban audience. | Ship Kanban only. |
| Public-facing intake forms | Separate product surface: public URL, spam protection, field mapping, GDPR consent. | Deep-link "create card" URL for internal use only. |
| Email integration | The largest source of accidental scope creep in CRM SaaS. | Launch without email. |
| Native mobile app | Separate codebase, separate release cycle, App Store review delays. | Responsive web + PWA. |
| Reports and dashboards | Users request dashboards before they have enough data to need one. | Surface key numbers inline (column totals, card age). Dashboard is v2. |
| SLA / deadline alerts + push notifications | Requires background job scheduler + notification delivery + user preferences. | Visual indicators (red border) for overdue due dates. Push/email alerts are v2. |
| Fine-grained permission tiers | ACL beyond Admin/Member is a security product in itself. | Two roles only: admin and member. |
| AI-assisted features | LLM integration adds latency, cost, and privacy concerns in multi-tenant context. | Ship clean data model first. |

**Confidence:** HIGH

---

## Competitor Analysis

### Pipefy

**Does well:** Pipe abstraction, phase fields, automations, card connections, SLA tracking, GraphQL API.

**Does poorly:** Steep learning curve, opaque automation debugging, performance with 100+ cards,
pricing increased sharply in 2023 pushing SMBs away, poor mobile experience, dated visual design.

**Pipefy's current ICP:** Operations teams, enterprise compliance. NOT primarily sales teams. **This is the gap Nodefy can own.**

### Trello

Core: lists, cards, checklists, labels, due dates. No CRM features (no deal value, no pipeline totals, no activity log).
**Lesson:** Simplicity is only a moat early. Must have a clear upgrade path to depth.

### HubSpot CRM (Free tier)

Sets the expectation floor. Deal value and close-date are first-class fields — users exposed to HubSpot expect both in v1.
**Lesson:** Compete on pipeline UX simplicity, not CRM graph depth.

### Monday.com

Table-first, not Kanban-first. Column totals (SUM of values per group) are heavily used — confirms column-header deal total as high-ROI differentiator.
**Lesson:** "Visual pipeline for sales teams" is a valid counter-position to Monday's breadth.

---

## Feature Dependencies

```
Workspace creation
  └── Pipeline creation
        └── Stage configuration
              └── Card creation
                    ├── Card detail view
                    ├── Card comments
                    ├── Activity log (hooks into all mutations)
                    ├── Card filters
                    └── Stage-age tracking (requires entered_at on card↔stage — must be in initial schema)

User management
  └── Workspace invite
        └── Role assignment (admin / member)
              └── Card assignment

Multi-tenancy isolation — cross-cutting constraint, not a feature. Must be correct from day one.

Column totals (differentiator) — requires card value field in schema.
```

---

## Competitive Positioning

Nodefy's wedge: **zero-configuration pipeline CRM that feels fast and focused, with the sales
metrics (deal value, stage age, pipeline totals) that Trello omits and Pipefy buries behind complexity.**

Target: SMB sales teams, 2–20 users, productive in under 10 minutes.

---

## Open Questions

- Has Pipefy addressed performance and mobile UX issues since 2023?
- What is Pipefy's current pricing for small teams?
- Do target users (Brazilian SMB teams, given Portuguese PROJECT.md) have different expectations? BRL currency formatting may be an implicit requirement.
