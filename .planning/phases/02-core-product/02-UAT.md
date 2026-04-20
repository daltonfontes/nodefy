---
status: partial
phase: 02-core-product
source: [02-01-SUMMARY.md, 02-02-SUMMARY.md, 02-03-SUMMARY.md, 02-04-SUMMARY.md]
started: 2026-04-20T00:00:00Z
updated: 2026-04-20T12:00:00Z
---

## Current Test
<!-- OVERWRITE each test - shows where we are -->

number: 3
name: Criar Stage (Coluna)
expected: |
  Dentro do pipeline criado, clicar para adicionar uma coluna.
  Digitar um nome (ex: "Em Progresso") e confirmar.
  A coluna aparece no board com header e contador zerado.
awaiting: user response

## Tests

### 1. Cold Start Smoke Test
expected: |
  Pare o servidor Next.js e o backend .NET. Reinicie os dois do zero.
  O backend deve subir sem erros e responder em GET /health com 200.
  O frontend deve carregar /workspace/select sem erros.
result: pass

### 2. Criar Pipeline
expected: |
  Na sidebar do workspace, clicar em "+ Novo pipeline".
  Digitar um nome e confirmar. O pipeline aparece na lista lateral
  e fica selecionado/ativo.
result: pass
verified_by: 02-04-PLAN.md human verification (Cenário A)
note: "Gap fechado pela plan 02-04 — workspace home agora renderiza formulário 'Criar primeiro pipeline' para admins sem pipelines. Admin criou pipeline e foi redirecionado para o board."

### 3. Criar Stage (Coluna)
expected: |
  Dentro do pipeline criado, clicar para adicionar uma coluna.
  Digitar um nome (ex: "Em Progresso") e confirmar.
  A coluna aparece no board com header e contador zerado.
result: pending
note: "Desbloqueado pelo 02-04-PLAN.md — pipeline agora pode ser criado a partir da workspace home."

### 4. Criar Card
expected: |
  Clicar para adicionar um card em uma coluna.
  Preencher título (e opcionalmente valor monetário) e salvar.
  O card aparece na coluna com o título correto.
result: pending
note: "Desbloqueado pelo 02-04-PLAN.md — depende de stage criado (teste 3)."

### 5. Arrastar Card Entre Colunas
expected: |
  Pegar um card e arrastá-lo para outra coluna.
  O card se move imediatamente (otimista). Ao soltar, fica na nova coluna.
  Se houver falha de rede, o card volta para a posição original.
result: pending
note: "Desbloqueado pelo 02-04-PLAN.md — depende de cards criados (teste 4)."

### 6. Painel de Detalhe do Card
expected: |
  Clicar em um card abre um Sheet lateral com título, descrição e log de atividade.
  A URL muda para ?card={id}. Fechar o Sheet volta a URL para o estado anterior.
result: pending
note: "Desbloqueado pelo 02-04-PLAN.md — depende de cards criados (teste 4)."

### 7. Arquivar Card
expected: |
  No detalhe do card, acionar "Arquivar". O card some do board.
  Não aparece mais em nenhuma coluna do pipeline.
result: pending
note: "Desbloqueado pelo 02-04-PLAN.md — depende de cards criados (teste 4)."

### 8. Sidebar Recolhível
expected: |
  Clicar no botão de recolher/expandir a sidebar. A sidebar colapsa/expande.
  Recarregar a página — o estado (colapsado ou expandido) é preservado.
result: pending
note: "Desbloqueado pelo 02-04-PLAN.md — pipeline agora acessível a partir da workspace home."

### 9. Agregados de Coluna
expected: |
  O header de cada coluna mostra o número de cards e a soma dos valores monetários.
  Ao mover ou criar um card, os números atualizam corretamente.
result: pending
note: "Desbloqueado pelo 02-04-PLAN.md — depende de pipeline e cards (testes 3–4)."

### 10. Badge de Tempo de Stage
expected: |
  Cards criados há algum tempo mostram um badge indicando quanto tempo estão no stage.
  (Pode ser necessário ajustar manualmente uma data no BD para testar estágios de warning/critical.)
result: pending
note: "Desbloqueado pelo 02-04-PLAN.md — depende de cards criados (teste 4)."

## Summary

total: 10
passed: 2
issues: 0
pending: 8
skipped: 0
blocked: 0

## Gaps

- truth: "Página inicial do workspace (/workspace/[id]) deve ter entrada funcional para criar o primeiro pipeline"
  status: closed
  closed_by: 02-04-PLAN.md
  closed_date: 2026-04-20
  resolution: "RSC workspace home reescrita com FirstPipelineForm (admin) e mensagem informativa (member). Stub removido. Human verification aprovada — Cenário A completo."
  test: 2
