# Governação e contexto canónico do PHC Knowledge

Índice e ordem de leitura obrigatória para ChatGPT, Cursor e restantes ferramentas.

## Antes de trabalhar

1. [Baseline do produto e arquitetura](../../PROJECT_BASELINE.md) — objetivo e fronteiras aprovadas.
2. [Registo de decisões](../../DECISIONS.md) — decisões aprovadas, substituídas e questões em aberto.
3. [Estado do projeto](../../PROJECT_STATUS.md) — implementação, evidência e lacunas.
4. [Handoff atual](current-handoff.md) — branch/HEAD, sessão mais recente, alterações, testes e próximo passo.
5. [Acordo de continuidade](working-agreement.md).
6. [Orientações do projeto](../../PROJECT_GUIDELINES.md) e regras em .cursor/rules/.
7. Documentação técnica diretamente relacionada com a tarefa.

## Precedência

- Decisões aprovadas: DECISIONS.md.
- Objetivo e arquitetura vigente: PROJECT_BASELINE.md.
- Estado efetivo: PROJECT_STATUS.md mais código/testes no commit consultado.
- Continuidade operacional: current-handoff.md.
- Relatórios de fase, investigações e ADRs antigos preservam o contexto histórico; não substituem silenciosamente os documentos canónicos.

Se houver conflito entre baseline, decisão e código, registar a divergência. Não alterar uma decisão aprovada por inferência.
