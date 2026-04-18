# Phase 2: Core Product - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-17
**Phase:** 02-core-product
**Areas discussed:** Navegação de Pipelines

---

## Navegação de Pipelines

### Onde ficam listados os pipelines?

| Option | Description | Selected |
|--------|-------------|----------|
| Sidebar lateral fixa | Lista à esquerda, sempre visível. Padrão Pipefy/Trello. Escala bem com muitos pipelines. | ✓ |
| Tabs no topo do board | Cada pipeline é uma aba horizontal. Bom para 2-4 pipelines. Fica congestionado. | |
| Dropdown no header | Apenas pipeline ativo visível, dropdown para trocar. Estilo Linear. Mais limpo mas esconde. | |

**User's choice:** Sidebar lateral fixa  
**Notes:** Layout do mockup selecionado: sidebar esquerda com "PIPELINES" header, pipeline ativo destacado com `>`, `[+ New pipeline]` no rodapé, board à direita com nome do pipeline + botão `[+ Add]`.

---

### Gestão de pipelines na sidebar

| Option | Description | Selected |
|--------|-------------|----------|
| Menu de contexto inline (…) | Hover mostra `…` → Renomear, Excluir. Admin only. Sem sair do board. | ✓ |
| Página de settings separada | Sidebar só navega. Gestão fica em /settings. Mais formal. | |

**User's choice:** Menu de contexto inline  
**Notes:** Admin-only. Sem redirect para página separada.

---

### Sidebar collapsible?

| Option | Description | Selected |
|--------|-------------|----------|
| Sim, collapsible com toggle | Botão colapsa sidebar, board fica em tela cheia. Útil para boards largos. | ✓ |
| Não, sempre visível | Sidebar fixa. Mais simples. | |

**User's choice:** Sim, collapsible com toggle

---

### Gestão de estágios

| Option | Description | Selected |
|--------|-------------|----------|
| Inline no board | Cabeçalho coluna → `…` → Renomear/Excluir. `+ Add stage` ao final. Arrastar header para reordenar. | ✓ |
| Página de settings do pipeline | Board só visualiza. Gestão em página separada com drag-and-drop. | |

**User's choice:** Inline no board  
**Notes:** Reordenamento via arrastar cabeçalho de coluna (fractional indexing).

---

## Claude's Discretion

- **Card visual design:** Claude decide campos visíveis no card compacto na coluna.
- **Card detail UX:** Claude decide onde abre o detalhe (side panel, modal, ou página).
- **Empty states:** Claude decide visual para colunas sem cards e board sem pipelines.
- **DnD visual feedback:** Ghost card, placeholder, rollback UX.
- **Fractional index rebalance threshold:** Claude define o threshold.

## Deferred Ideas

- Real-time SignalR sync → Phase 3
- Card search e filters → Phase 3
