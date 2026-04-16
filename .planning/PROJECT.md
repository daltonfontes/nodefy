# Nodefy

## What This Is

Nodefy é um CRM SaaS multi-tenant com pipelines visuais no estilo Pipefy. Cada empresa cria seu próprio workspace com pipelines customizáveis, cards de negócios e gestão de times. O sistema é voltado para equipes de vendas e operações que precisam visualizar e mover deals entre estágios de forma colaborativa.

## Core Value

Qualquer membro de um workspace consegue ver e mover cards entre estágios do pipeline em tempo real, sem atrito.

## Requirements

### Validated

(None yet — ship to validate)

### Active

**Autenticação & Identidade**
- [ ] Usuário pode fazer login via SSO (GitHub, Google, Microsoft)
- [ ] Usuário pode criar e gerenciar seu workspace (tenant)
- [ ] Usuário pode convidar membros para o workspace com papel admin ou membro

**Pipeline Visual**
- [ ] Admin pode criar, editar e excluir pipelines dentro do workspace
- [ ] Admin pode configurar estágios (colunas) do pipeline
- [ ] Usuário pode criar cards com título, descrição, valor monetário, responsável e data de fechamento
- [ ] Usuário pode mover cards entre estágios via drag-and-drop
- [ ] Usuário pode editar e excluir seus próprios cards

**Colaboração**
- [ ] Usuário pode comentar em cards
- [ ] Sistema registra histórico de atividade de cada card (movimentações, edições)

**Produtividade**
- [ ] Usuário pode filtrar cards por responsável, data de fechamento e valor
- [ ] Usuário pode buscar cards por título dentro de um pipeline

### Out of Scope

- Automações/gatilhos — complexidade alta, v2
- Formulários externos de captura de leads — v2
- Integrações com ferramentas externas (Slack, email marketing) — v2
- App mobile nativo — v2
- Relatórios e dashboards avançados — v2

## Context

O projeto é inspirado no Pipefy mas com foco em simplicidade e na experiência visual de pipeline. SaaS multi-tenant onde cada empresa tem isolamento completo de dados. SSO via OAuth (GitHub, Google, Microsoft) é requisito desde o início — sem senha própria no v1.

## Constraints

- **Stack Frontend**: Next.js — escolha do usuário
- **Stack Backend**: C# (.NET) — escolha do usuário
- **Banco de dados**: PostgreSQL via Docker — escolha do usuário
- **Autenticação**: SSO apenas (GitHub, Google, Microsoft) — sem auth própria no v1
- **Deployment**: Docker-first — containerização desde o início
- **Multi-tenancy**: Isolamento por tenant no banco (schema ou row-level)

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| SSO-only auth (sem senha própria) | Reduz superfície de segurança, acelera v1 | — Pending |
| Multi-tenant SaaS desde v1 | Objetivo do produto é ser plataforma, não ferramenta interna | — Pending |
| Pipelines configuráveis por tenant | Cada empresa tem fluxo diferente — não faz sentido engessado | — Pending |

## Evolution

Este documento evolui a cada transição de fase e marco de milestone.

**Após cada transição de fase** (via `/gsd-transition`):
1. Requisitos invalidados? → Mover para Out of Scope com motivo
2. Requisitos validados? → Mover para Validated com referência da fase
3. Novos requisitos surgiram? → Adicionar em Active
4. Decisões a registrar? → Adicionar em Key Decisions
5. "What This Is" ainda preciso? → Atualizar se divergiu

**Após cada milestone** (via `/gsd-complete-milestone`):
1. Revisão completa de todas as seções
2. Verificar Core Value — ainda é a prioridade certa?
3. Auditar Out of Scope — os motivos ainda são válidos?
4. Atualizar Context com estado atual

---
*Last updated: 2026-04-16 after initialization*
