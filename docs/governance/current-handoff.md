# PHC Knowledge — Handoff atual

> Registo operacional de continuidade. Não substitui a baseline nem o registo canónico de decisões.

## Estado remoto confirmado

- **Data:** 2026-11-20
- **Repositório:** `Jhouinho/PhcKnowledge`
- **Branch:** `feature/increment-c-package-import-analysis-confirmation`
- **HEAD anterior à atualização deste handoff:** `170631ea8e17b584ac70cd6b379312570c0c948a`
- **Estado local do Cursor:** desconhecido; este handoff descreve apenas o estado remoto consultado.
- **Auditoria documental:** em curso; não concluída.
- **Baseline canónica consolidada:** ainda não identificada/aprovada.

> O HEAD indicado no handoff anterior (`e2d896ff67f7ef0df903c8a13940da9b826a764d`) não coincide com o HEAD devolvido pela referência remota da branch (`170631ea8e17b584ac70cd6b379312570c0c948a`). Esta atualização corrige o registo a partir da referência remota verificada.

## Objetivo imediato

Completar a auditoria documental e técnica do PHC Knowledge, comparando documentação, decisões aprovadas, implementação e testes. O inventário final será apresentado ao utilizador antes de qualquer limpeza ou atualização estrutural de documentação.

## Decisões de produto/arquitetura confirmadas

- Plataforma de uso interno da equipa, para gerir conhecimento dos clientes acompanhados; não é produto comercial multiempresa.
- SQL Server é a base de dados central escolhida.
- O Scanner corre no ambiente do cliente e produz ZIP como meio de transporte.
- O objetivo aprovado é materializar no SQL Server a informação necessária após importação íntegra e eliminar ZIP/temporários; a implementação atual ainda tem de ser reconciliada com esse objetivo.
- Scanner integrado no monorepo, mas autónomo em responsabilidade e execução; sem dependência de runtime do Knowledge.
- RAG é objetivo futuro, sem escolha tecnológica ou implementação antecipada.
- Preservar proveniência por cliente/instalação/ambiente/base de dados/captura/artefacto, sem assumir equivalências.
- As regras Desktop/Web acordadas e o estado da sua implementação estão registados em `docs/governance/audit-progress.md`.

## Estado da auditoria

Consultar `docs/governance/audit-progress.md` para constatações intermédias, classificação preliminar e tarefas em falta.

Pontos já sinalizados:
- O fluxo de análise reabre pacotes arquivados; o cleanup observado não demonstra ainda a política aprovada de ZIP transitório.
- A análise e as decisões persistem, mas não foi identificada até ao ponto inspecionado a materialização integral dos conteúdos dos artefactos numa base de conhecimento pesquisável.
- A pesquisa TableSearch observada não equivale a pesquisa global de código.
- A cobertura de collectors, exportação IKP e classificação Desktop/Web ainda exige verificação.
- Documentos antigos que afirmam que a integração não existe devem ser avaliados como históricos, não apagados automaticamente.
- Contratos P1/P1B, schemas, fixtures e ADRs devem ser preservados enquanto não houver prova e validação de substituição.

## Restrições de trabalho durante a auditoria

- Não alterar código ou comportamento.
- Não apagar nem reescrever documentos técnicos.
- Não fazer merge, rebase ou mudança de branch.
- Não declarar testes executados sem evidência.
- Não assumir que alterações locais do Cursor estão publicadas.
- Não consolidar baseline canónica antes de terminar a auditoria e obter validação do utilizador.

## Próximo passo seguro

Continuar a inspeção dos collectors e do mapeamento dos artefactos para IKP; depois cruzar testes e documentos restantes. Produzir inventário final dos documentos com classificação e proposta de tratamento. Pedir aprovação antes de qualquer alteração documental adicional.
