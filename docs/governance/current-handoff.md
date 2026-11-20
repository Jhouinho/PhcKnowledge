# PHC Knowledge — Handoff atual

> Registo operacional para continuidade entre ChatGPT, Cursor e outras ferramentas. Ler depois de PROJECT_BASELINE.md, DECISIONS.md e PROJECT_STATUS.md.

## Estado remoto confirmado
- Data: 2026-11-20
- Repositório: Jhouinho/PhcKnowledge
- Branch: feature/increment-c-package-import-analysis-confirmation
- HEAD no início da primeira atualização documental: 9a9df92ff82a98eafa2a5c1ff5134d8226ea7ce5
- Commit da baseline canónica: 7d60259e49ddab5c1b304035f3790e7e7b53d5c4.
- Commit do inventário e reconciliação documental: 070e456b06f7054c0764a3f4cac6532150381743.
- Commit principal que publicou a baseline e as regras de continuidade: 7d60259e49ddab5c1b304035f3790e7e7b53d5c4.
- O handoff foi atualizado em commits subsequentes; consultar sempre a referência remota desta branch para obter o HEAD exato, pois a própria atualização do handoff cria um novo commit.
- Estado local do Cursor: não observado; este handoff descreve apenas o estado GitHub.
- Testes nesta sessão: nenhum; atualização documental sem build ou execução de código.

## Objetivo desta sessão
Consolidar a fonte de contexto partilhada e fechar a primeira classificação individual do inventário documental para que ChatGPT e Cursor iniciem tarefas com decisões, arquitetura, estado e handoff atualizados.

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
A auditoria documental/técnica continua em curso. Foi criado `docs/governance/document-inventory.md`, com classificação individual dos 85 ficheiros Markdown/texto presentes após esta publicação. A classificação define destino de manutenção; não equivale a validação integral de todas as afirmações técnicas. Não foi apagada documentação histórica.

## Próximo passo
1. Confirmar o HEAD remoto e manter esta referência de handoff atualizada em cada sessão.
2. Fechar inventário individual dos documentos restantes, sem apagar históricos.
3. Rever o ciclo de ZIP temporário e desenhar materialização/eliminação segura.
4. Definir arquitetura funcional do PHC Standard Catalog.
5. Preparar plano de testes SQL por operação para as 23 operações do Scanner.

## Instrução para a próxima ferramenta
Ler docs/governance/README.md, PROJECT_BASELINE.md, DECISIONS.md, PROJECT_STATUS.md e este ficheiro. Confirmar branch/HEAD/estado local antes de tocar no código. Não assumir que alterações locais do Cursor estão no GitHub.


## Segunda etapa da auditoria — 2026-11-20

- Inventário de 84 ficheiros concluído e versionado.
- Dez documentos com contradições prioritárias receberam notas de precedência/estado ou correção do índice de samples.
- `PhcKnowledgeScanner/docs/open-decisions.md` foi identificado como checklist histórica; não assumir que os itens não assinalados continuam abertos.
- O desenho do Incremento C foi reconciliado com a implementação visível na branch, sem declarar concluída toda a experiência.
- Nenhum código foi alterado e nenhum teste foi executado nesta etapa.


## Auditoria técnica do ciclo de ZIP — 2026-11-20

A inspeção read-only confirmou que o ZIP é arquivado permanentemente e que `IkpPackageAnalysisService` reabre esse arquivo para calcular a primeira análise. O store não expõe eliminação; o cleanup existente atua apenas sobre staging. `MaterializationReadinessEvaluator` avalia prontidão e não materializa conhecimento. Foi criado `docs/architecture/ikp-temporary-package-lifecycle-gap-2026-11-20.md` com evidências, divergência, riscos e critérios para desenho futuro. Não houve alteração de código nem execução de testes.
