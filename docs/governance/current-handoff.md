# PHC Knowledge — Handoff atual

> Registo operacional para continuidade entre ChatGPT, Cursor e outras ferramentas. Ler depois de PROJECT_BASELINE.md, DECISIONS.md e PROJECT_STATUS.md.

## Estado remoto confirmado
- Data: 2026-11-20
- Repositório: Jhouinho/PhcKnowledge
- Branch: feature/increment-c-package-import-analysis-confirmation
- HEAD no início da atualização documental: 9a9df92ff82a98eafa2a5c1ff5134d8226ea7ce5
- Commit principal que publicou a baseline e as regras de continuidade: 7d60259e49ddab5c1b304035f3790e7e7b53d5c4.
- Este handoff foi atualizado após esse commit. Para o HEAD exato mais recente, consultar a referência remota desta branch; não assumir que o commit principal continua a ser o HEAD.
- Estado local do Cursor: não observado; este handoff descreve apenas o estado GitHub.
- Testes nesta sessão: nenhum; atualização documental sem build ou execução de código.

## Objetivo desta sessão
Consolidar a fonte de contexto partilhada para que ChatGPT e Cursor iniciem tarefas com decisões, arquitetura, estado e handoff atualizados.

## Documentos canónicos publicados nesta atualização
- PROJECT_BASELINE.md — produto, arquitetura e fronteiras.
- DECISIONS.md — decisões aprovadas, substituídas e questões em aberto.
- PROJECT_STATUS.md — implementação, evidência, limitações e próximos passos.
- docs/governance/README.md — índice e ordem de leitura.
- PROJECT_GUIDELINES.md, .cursor/rules/project-context.mdc e docs/governance/working-agreement.md — alinhados com a leitura obrigatória.
- README principal, handoff, progresso de auditoria, fontes e documentos técnicos históricos — alinhados com as fontes canónicas.

## Decisões vigentes
- Ferramenta interna da equipa; não produto comercial multiempresa.
- SQL Server é a base central; PostgreSQL excluído. Edição/versão concreta não fixada.
- SQLite apenas workspace/cache local do Scanner.
- Scanner autónomo e read-only perante instalações PHC.
- ZIP IKP temporário; eliminar apenas após materialização e confirmação íntegra.
- PHC Standard curado separado das observações e personalizações de clientes.
- RAG é objetivo futuro; tecnologia por decidir.

## Auditoria
A auditoria documental/técnica continua em curso. O inventário de 79 documentos foi identificado antes desta atualização. Esta publicação atualiza fontes canónicas e corrige contradições prioritárias; não conclui a classificação individual de todos os documentos e não remove documentação histórica.

## Próximo passo
1. Confirmar o HEAD remoto e manter esta referência de handoff atualizada em cada sessão.
2. Fechar inventário individual dos documentos restantes, sem apagar históricos.
3. Rever o ciclo de ZIP temporário e desenhar materialização/eliminação segura.
4. Definir arquitetura funcional do PHC Standard Catalog.
5. Preparar plano de testes SQL por operação para as 23 operações do Scanner.

## Instrução para a próxima ferramenta
Ler docs/governance/README.md, PROJECT_BASELINE.md, DECISIONS.md, PROJECT_STATUS.md e este ficheiro. Confirmar branch/HEAD/estado local antes de tocar no código. Não assumir que alterações locais do Cursor estão no GitHub.
