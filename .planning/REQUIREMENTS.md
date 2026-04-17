# Requirements: Nodefy

**Defined:** 2026-04-16
**Core Value:** Qualquer membro de um workspace consegue ver e mover cards entre estágios do pipeline em tempo real, sem atrito.

## v1 Requirements

### Autenticação & Identidade

- [x] **AUTH-01**: Usuário pode fazer login via GitHub SSO
- [x] **AUTH-02**: Usuário pode fazer login via Google SSO
- [x] **AUTH-03**: Usuário pode fazer login via Microsoft SSO
- [x] **AUTH-04**: Sessão do usuário persiste entre refreshes do navegador (cookie HttpOnly)
- [x] **AUTH-05**: Usuário pode fazer logout de qualquer página

### Workspace & Membros

- [x] **WORK-01**: Usuário autenticado pode criar um workspace (tenant)
- [x] **WORK-02**: Admin pode convidar membros por email com papel admin ou membro
- [x] **WORK-03**: Convidado pode aceitar o convite e acessar o workspace
- [x] **WORK-04**: Admin pode ver a lista de membros do workspace
- [x] **WORK-05**: Admin pode alterar o papel de um membro (admin/membro)
- [x] **WORK-06**: Admin pode remover um membro do workspace

### Pipeline & Estágios

- [ ] **PIPE-01**: Admin pode criar pipelines dentro do workspace
- [ ] **PIPE-02**: Admin pode renomear e excluir pipelines
- [ ] **PIPE-03**: Admin pode adicionar, renomear e excluir estágios de um pipeline
- [ ] **PIPE-04**: Admin pode reordenar estágios de um pipeline
- [ ] **PIPE-05**: Cabeçalho de cada coluna exibe contagem de cards e soma do valor monetário (diferenciador)

### Cards

- [ ] **CARD-01**: Usuário pode criar card com título, descrição, valor monetário, responsável e data de fechamento
- [ ] **CARD-02**: Usuário pode editar campos do card
- [ ] **CARD-03**: Usuário pode arquivar (excluir soft) card
- [ ] **CARD-04**: Usuário pode mover card entre estágios via drag-and-drop com update otimista
- [ ] **CARD-05**: Card exibe indicador de tempo no estágio atual (ex: "23 dias neste estágio")
- [ ] **CARD-06**: Card exibe log de atividade (criação, movimentações, edições) em ordem cronológica

### Colaboração em Tempo Real

- [ ] **REAL-01**: Movimentação de card por um membro é refletida no board de outros membros via SignalR em tempo real
- [ ] **REAL-02**: Criação e atualização de card por um membro é visível para outros membros sem reload

### Descoberta

- [ ] **DISC-01**: Usuário pode buscar cards por título dentro de um pipeline
- [ ] **DISC-02**: Usuário pode filtrar cards por responsável, data de fechamento e valor

### Qualidade & Testes

- [x] **TEST-01**: Backend (.NET) desenvolvido com TDD — testes escritos antes da implementação (xUnit + TestContainers)
- [ ] **TEST-02**: Fluxos críticos cobertos por testes E2E (Playwright): login SSO, criar pipeline, criar card, mover card

## v2 Requirements

### Colaboração

- **COLAB-01**: Usuário pode comentar em cards
- **COLAB-02**: Usuário pode mencionar membros em comentários (`@nome`)

### Automações

- **AUTO-01**: Admin pode criar regra "quando card entra em estágio X, mover para Y após N dias"
- **AUTO-02**: Admin pode criar regra "quando card entra em estágio X, atribuir a membro Y"

### Templates

- **TMPL-01**: Workspace pode criar pipeline a partir de template (Vendas B2B, Onboarding, Suporte, Recrutamento)

### Notificações

- **NOTF-01**: Usuário recebe notificação quando card é atribuído a ele
- **NOTF-02**: Usuário recebe notificação quando card de sua responsabilidade está há N dias sem movimento
- **NOTF-03**: Usuário pode configurar preferências de notificação

### Produtividade

- **PROD-01**: Atalho de teclado `N` cria novo card na coluna em foco
- **PROD-02**: Usuário pode colapsar estágios para visualização compacta

## Out of Scope

| Feature | Motivo |
|---------|--------|
| Login com email + senha própria | SSO-only no v1 — reduz superfície de segurança |
| Campos customizados nos cards | Alta complexidade; schema fixo é suficiente para v1 |
| Múltiplas views (Lista, Gantt, Calendário) | Cada view é um produto separado; Kanban only no v1 |
| Formulários de intake externos (público) | Superfície de produto separada; v2 |
| Integração com email (envio/recebimento) | Maior fonte de scope creep em CRM SaaS |
| App mobile nativo | Web responsiva + PWA é suficiente para v1 |
| Relatórios e dashboards avançados | Dados insuficientes para dashboard útil no v1 |
| Alertas de SLA + notificações push | Requer 3 sistemas separados (jobs, delivery, prefs) |
| Permissões granulares além admin/membro | ACL completa é um produto de segurança em si |
| IA assistida | Latência + custo + privacidade multi-tenant; não validado para v1 |
| Conexões entre cards/pipelines | Abstração do Pipefy; complexidade alta para v1 |

## Traceability

| Requisito | Fase | Nome da Fase | Status |
|-----------|------|--------------|--------|
| AUTH-01 | Phase 1 | Foundation | Pending |
| AUTH-02 | Phase 1 | Foundation | Pending |
| AUTH-03 | Phase 1 | Foundation | Pending |
| AUTH-04 | Phase 1 | Foundation | Pending |
| AUTH-05 | Phase 1 | Foundation | Pending |
| WORK-01 | Phase 1 | Foundation | Pending |
| WORK-02 | Phase 1 | Foundation | Pending |
| WORK-03 | Phase 1 | Foundation | Pending |
| WORK-04 | Phase 1 | Foundation | Pending |
| WORK-05 | Phase 1 | Foundation | Pending |
| WORK-06 | Phase 1 | Foundation | Pending |
| TEST-01 | Phase 1 | Foundation | Pending |
| PIPE-01 | Phase 2 | Core Product | Pending |
| PIPE-02 | Phase 2 | Core Product | Pending |
| PIPE-03 | Phase 2 | Core Product | Pending |
| PIPE-04 | Phase 2 | Core Product | Pending |
| PIPE-05 | Phase 2 | Core Product | Pending |
| CARD-01 | Phase 2 | Core Product | Pending |
| CARD-02 | Phase 2 | Core Product | Pending |
| CARD-03 | Phase 2 | Core Product | Pending |
| CARD-04 | Phase 2 | Core Product | Pending |
| CARD-05 | Phase 2 | Core Product | Pending |
| CARD-06 | Phase 2 | Core Product | Pending |
| REAL-01 | Phase 3 | Collaboration & Discovery | Pending |
| REAL-02 | Phase 3 | Collaboration & Discovery | Pending |
| DISC-01 | Phase 3 | Collaboration & Discovery | Pending |
| DISC-02 | Phase 3 | Collaboration & Discovery | Pending |
| TEST-02 | Phase 4 | Quality & Hardening | Pending |

**Coverage:**
- v1 requirements: 28 total
- Mapped to phases: 28
- Unmapped: 0 ✓

---
*Requirements defined: 2026-04-16*
*Last updated: 2026-04-16 after roadmap creation*
