> **Nota de precedência — 2026-11-20:** este ADR descreve a decisão de 2026-10-30 de separar o Scanner V1 do `ConsoleApp` e manter repositório próprio. A direção atual mantém o `PhcKnowledgeScanner` no monorepo `Jhouinho/PhcKnowledge`, mas preserva a sua solução, execução, responsabilidade e ciclo de desenvolvimento autónomos (D-005 em `../../DECISIONS.md`). A exigência de repositório Git/GitHub separado foi substituída; o ADR é preservado como registo histórico, não como instrução atual.

# ADR 0003 — Independência do ConsoleApp / Scanner V1

- **Estado:** Aceite
- **Data:** 2026-10-30

## Contexto

Existe um Scanner V1 dentro de `ConsoleApp`. O pedido explícito é um projeto e repositório **totalmente independentes**.

## Decisão

- Pasta irmã: `PhcKnowledgeScanner/` (não dentro de `ConsoleApp/`)
- Solução, Git e GitHub próprios
- **Proibidas** project references, partilha de código por link e alterações ao `ConsoleApp` neste âmbito
- Consulta futura ao V1 apenas como referência documental/técnica, com justificação se algum padrão for reutilizado conscientemente

## Consequências

- Possível duplicação deliberada de ideias (não de binários)
- Evolução e versionamento desacoplados
