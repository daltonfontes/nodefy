# Research Summary — Nodefy

**Synthesized:** 2026-04-16

---

## Executive Summary

Nodefy é um SaaS CRM multi-tenant com pipelines Kanban visuais para times de vendas de PMEs brasileiras (2–20 usuários). A pesquisa converge em um padrão bem estabelecido: tenancy por row-level isolation, REST com .NET, SignalR para real-time, dnd-kit para drag-and-drop.

**Posicionamento:** o gap que o Trello deixa (sem valor de deal, sem totais de pipeline, sem stage-age) e o gap que o Pipefy cria (complexidade, preço, performance). Nodefy v1 ganha sendo produtivo em menos de 10 minutos com as métricas CRM certas visíveis por padrão.

**Risco central:** correção, não complexidade. Isolamento multi-tenant, OAuth callbacks, configuração de roles no RLS, e rollback de optimistic updates devem estar corretos desde o primeiro commit.

---

## Stack Recomendada

| Camada | Tecnologia | Versão | Rationale |
|--------|-----------|--------|-----------|
| Frontend | Next.js App Router | Latest | `output: "standalone"` obrigatório para Docker |
| Auth (frontend) | next-auth / Auth.js | v5 | App Router nativo; v4 é maintenance-only |
| Drag-and-drop | @dnd-kit/core + sortable | Latest | react-beautiful-dnd arquivado 2023 |
| Server state | TanStack Query | v5 | Optimistic updates + rollback built-in |
| Real-time client | @microsoft/signalr | Latest | Par direto com ASP.NET Core SignalR |
| UI components | shadcn/ui + Tailwind CSS | Tailwind v3 | Sem CSS-in-JS em runtime |
| Local UI state | Zustand | Latest | DnD in-progress, modais, filtros ativos |
| Backend | ASP.NET Core | .NET 9 | Fixo |
| SSO | Google.Apis.Auth.AspNetCore3 + Microsoft.AspNetCore.Authentication.MicrosoftAccount + AspNet.Security.OAuth.GitHub | Latest | Todos HIGH confidence |
| Real-time (backend) | ASP.NET Core SignalR (built-in) | .NET 9 | Groups para isolamento de tenant |
| ORM | EF Core 9 + Npgsql | EF Core 9 | Code-first migrations; global query filters |
| Validação | FluentValidation.AspNetCore | Latest | Mais limpo que DataAnnotations |
| Serialização | System.Text.Json (built-in) | .NET 9 | Newtonsoft.Json é redundante |
| Banco | PostgreSQL | 16-alpine | Row-level security; imagem Docker mínima |
| Infra | Docker Compose | — | db + api + frontend; nginx/Caddy prod TLS |

---

## Table Stakes para v1

| Feature | Notas |
|---------|-------|
| Kanban board com colunas | Render rápido, 50+ cards |
| Drag-and-drop | Optimistic update obrigatório; rollback em falha |
| Painel de detalhe do card | Side panel — mantém contexto do board visível |
| Múltiplos pipelines por workspace | NÃO hardcodar um pipeline por org no data model |
| Estágios customizáveis | Criar / renomear / excluir estágios |
| Busca de cards | Full-text no título mínimo |
| Filtros de cards | Responsável, data, valor — lógica AND |
| Log de atividade por card | Append-only: criação, movimentações, edições, comentários |
| Comentários no card | Texto plano mínimo |
| Isolamento multi-tenant | Row-level security — inegociável |
| Convidar membros com papéis | Admin / membro; convite por email com estado pendente |
| Atualizações near-real-time | SignalR preferido; polling 5s aceitável |

---

## Top Diferenciadores (alto ROI, baixo esforço)

| Diferenciador | Implementação | Por que ganha |
|--------------|--------------|---------------|
| Totais da coluna: contagem + soma de valor | `SUM(monetary_value)` por estágio no cabeçalho | Trello omite; Monday users dependem |
| Indicador de tempo no estágio | `stage_entered_at` em cada card (**DEVE estar no schema inicial**) | Métrica de coaching; Pipefy enterra isso |
| Templates de pipeline | 3–4: Vendas B2B, Onboarding, Suporte, Recrutamento | Elimina blank-slate anxiety |
| Atalho `N` para novo card | Hotkey global na coluna em foco | Alto delight, baixo esforço |
| Estágios colapsáveis | localStorage apenas, sem server state | UX para pipelines largos |

**Deferidos para v2+:** Automações, campos customizados, múltiplas views, integração email, relatórios, notificações push, permissões granulares, IA, app mobile nativo.

---

## Decisões Arquiteturais Chave

| Decisão | Escolha | Rationale |
|---------|---------|-----------|
| Multi-tenancy | Shared schema + RLS + EF Core global query filters | Duas camadas de enforcement; baixa complexidade ops |
| API style | REST | tRPC é TS-only; GraphQL overkill |
| Ordenação de cards | Fractional indexing (DOUBLE PRECISION) | Evita O(N) write fan-out; só card movido é atualizado |
| Armazenamento de auth | Cookie HttpOnly Secure SameSite=Strict | XSS protection; nunca localStorage |
| Claims JWT | `sub` + `tid` + `wid` + `role` | Multi-workspace ready |
| Real-time conflict | Last-write-wins (v1) | Suficiente para board colaborativo |
| SignalR groups | `pipeline:{pipelineId}` + verificação de tenant no join | Isolamento antes do broadcast |
| State frontend | React Query (server) + Zustand (UI local) | Separação clara; sem Redux |

**Ordem de build:** DB Schema → .NET Auth → Tenant/Workspace API → Next.js Auth → Pipeline/Stage CRUD → Card CRUD → Board UI → Card Move API → SignalR → Comments/Activity → Filtros/Busca → Settings

---

## Top 5 Armadilhas Críticas

**1. Isolamento de tenant não enforçado na camada de query (Fase 1)**
`HasQueryFilter(tenantId)` em toda entidade tenant-scoped. Role DB dedicado sem superuser (`nodefy_app`) — RLS é ignorado silenciosamente para superusers. Testes cross-tenant antes de qualquer entidade de negócio.

**2. Fractional indexing + stage_entered_at não no schema inicial (Fase 1)**
`position DOUBLE PRECISION` e `stage_entered_at TIMESTAMPTZ` devem estar na tabela cards desde a migration 1. Retrofit depois que há dados é doloroso.

**3. Optimistic update sem rollback em falha de rede (Fase 2)**
Modelar estado do card explicitamente (`idle | moving | error`). Usar `onError` do React Query para animar o card de volta à posição original com toast.

**4. Callback URLs OAuth não registradas por ambiente (Fase 1)**
Registrar `localhost`, `staging` e `prod` em todos os 3 providers antes de escrever código. GitHub requer chamada separada a `/user/emails` para email verificado — null email silencioso quebra invite flows.

**5. Secrets no git / app como root no Docker (Fase 1)**
`.env` em `.gitignore` no primeiro commit. Compose usa `${VAR}`. Container .NET usa `USER app`.

---

## Implicações para o Roadmap

### Fases Sugeridas

**Fase 1 — Foundation**
DB schema (RLS, fractional positions, stage_entered_at) + Docker + .NET Auth (3 SSO) + Tenant/Workspace/Membership API + Next.js Auth + invite flow.
Entrega: workspace criado e usuário logado.

**Fase 2 — Core Product**
Pipeline + stage CRUD + card CRUD + Board UI + drag-and-drop com optimistic/rollback + painel de detalhe + totais de coluna + indicador de stage-age.
Entrega: workflow Kanban completo usável por time de vendas real.

**Fase 3 — Colaboração**
SignalR BoardHub + broadcast real-time + comentários + log de atividade + filtros + busca.
Entrega: colaboração multi-usuário e descoberta.

**Fase 4 — Polish e Settings**
Templates + atalhos de teclado + estágios colapsáveis + settings de workspace + gestão de membros + responsive/PWA.
Entrega: onboarding acelerado, delight de power-users, auto-serviço de admin.

---

## Confiança Geral: HIGH

| Área | Confiança |
|------|-----------|
| Stack | HIGH |
| Features (table stakes) | HIGH |
| Features (diferenciadores) | MEDIUM |
| Arquitetura | HIGH |
| Armadilhas | HIGH |

---

## Gaps a Endereçar no Planejamento

- Formatação em BRL provavelmente é requisito implícito — confirmar.
- Invite flow: email-based vs. link compartilhável no app — email requer SMTP em v1?
- Rebalanceamento de fractional index: definir threshold de precisão antes da Fase 2.
