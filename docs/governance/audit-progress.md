# PHC Knowledge — Progresso da auditoria documental

**Estado:** em curso; não é uma baseline aprovada nem uma auditoria concluída.  
**Última atualização:** 2026-11-20  
**Repositório:** `Jhouinho/PhcKnowledge`  
**Branch de trabalho:** `feature/increment-c-package-import-analysis-confirmation`  
**Commit de referência antes da atualização documental:** `9a9df92ff82a98eafa2a5c1ff5134d8226ea7ce5`

## Objetivo e limites

Verificar a coerência entre decisões de produto/arquitetura, documentação, código e testes; identificar documentos vigentes, desatualizados, históricos, redundantes ou que devam ser preservados como contratos/fixtures.

Durante esta auditoria:
- não alterar implementação;
- não apagar nem reescrever documentação técnica;
- não fazer merge nem mudar de branch;
- não declarar implementado ou validado sem evidência;
- apresentar o inventário e a proposta de tratamento antes de qualquer limpeza documental.

## Contexto de produto confirmado pelo utilizador

- A plataforma é uma ferramenta de uso interno da equipa, para gerir e reutilizar conhecimento técnico dos clientes acompanhados; não é um produto comercial multiempresa.
- O Scanner corre no ambiente do cliente e produz um ZIP que é transportado para o ambiente interno.
- O ZIP é meio de transporte. O objetivo aprovado é materializar e persistir no SQL Server a informação necessária e eliminar o ZIP/temporários após confirmação de importação íntegra; a implementação atual ainda tem de ser reconciliada com esse objetivo.
- A base de dados central escolhida é SQL Server.
- O Scanner está no monorepo, mas mantém responsabilidade e execução autónomas; não deve ser introduzida dependência de runtime entre Scanner e Knowledge.
- RAG é um objetivo futuro; não selecionar tecnologia nem antecipar implementação nesta fase.
- Preservar proveniência por cliente, instalação, ambiente/base de dados, captura e artefacto, sem colapsar estes conceitos.
- O utilizador pretende pesquisa e reutilização dos artefactos/código recolhidos, com contexto de origem, para compreender customizações existentes e planear implementações.
- As regras de classificação Desktop/Web acordadas incluem Fox/Visual FoxPro e SQL puro como Desktop; C# como Web; HTML/JS/CSS isolados como Web; HTML/JS/CSS encapsulados em Fox como Desktop. A classificação deve ser baseada em evidência e não inferida para a instalação inteira a partir de um único artefacto. A implementação desta classificação ainda deve ser verificada.

## Constatações técnicas já registadas durante a auditoria

1. A branch consultada anteriormente era `feature/increment-c-package-import-analysis-confirmation`, com HEAD `170631ea8e17b584ac70cd6b379312570c0c948a`. O conteúdo do handoff anterior referia `e2d896ff67f7ef0df903c8a13940da9b826a764d`; essa referência não coincide com o HEAD confirmado pela API da branch e deve ser tratada como desatualizada.
2. A solução contém áreas separadas para Host, Import e Portal, migrations e testes. A existência dos testes não significa que tenham sido executados nesta auditoria.
3. Foram identificadas migrations para receipts, tentativas, organizações, análises de importação e decisões de análise.
4. O fluxo de análise consegue reabrir o pacote através de `OpenArchivedPackageReadAsync`; existe arquivo permanente no fluxo atual. O cleanup observado trata staging antigo não protegido por tentativas pendentes. Isto diverge do objetivo de ZIP transitório e requer análise antes de qualquer alteração.
5. A análise do pacote é persistida em JSON e existem decisões de confirmação/rejeição. Até ao ponto inspecionado, não foi identificada uma camada equivalente que materialize o conteúdo integral dos artefactos numa base de conhecimento pesquisável.
6. O exportador IKP inclui artefactos de definição referenciados pelos objetos da captura, valida metadata, bytes, tamanho, SHA-256 e ContentType. Não se deve assumir que todos os ficheiros do workspace são exportados.
7. A pesquisa `TableSearch` observada é centrada em tabelas e critérios como nome/schema/ObjectId; não equivale a pesquisa global sobre o conteúdo dos artefactos de código.
8. Não foi ainda encontrada evidência suficiente de um classificador Desktop/Web que implemente as regras acordadas.
9. Os documentos P1/P1B contêm contratos de identidade, proveniência e fingerprints relevantes. Em particular, SourceObjectId é referência dentro da captura, não identidade permanente; DefinitionFingerprint e MetadataFingerprint são distintos; ausência de um artefacto numa captura não prova remoção.
10. A matriz de compatibilidade Scanner/IKP documenta validação com captura real para uma parte dos tipos mapeados, não para todos.
11. Foram identificados documentos do Scanner que afirmam que a integração/importação no Knowledge ainda não existe. Devem ser avaliados como registos históricos face ao estado mais recente, sem os apagar nesta fase.
12. Existem ADRs do Scanner numerados 0001–0014. A orientação preliminar é preservá-los como histórico de decisões do Scanner e reconciliar decisões superadas com evidência atual.

Estas constatações são um registo intermédio. A auditoria ainda não concluiu a inspeção de todos os collectors, mapeamentos IKP, testes e documentos.

## Classificação preliminar — não aprovada como inventário final

- Preservar: especificação IKP, schemas, fixtures/validações, contratos P1/P1B e ADRs, salvo prova de redundância ou substituição explícita.
- Atualizar após auditoria: handoff, estado do projeto, limites do produto, avaliação tecnológica/importação, decisões em aberto e especificação da experiência do Portal.
- Marcar como histórico quando descrevem propostas ou fases já ultrapassadas: arquitetura/propostas antigas do Scanner e handoffs de fases concluídas.
- Rever profundamente antes de tratar como modelo implementado: `knowledge-domain-model-0.2.md`.
- Não remover documentos antes do inventário final e validação do utilizador.

## Trabalho ainda por concluir

1. Inspecionar collectors, artefactos produzidos e mapeamentos para IKP; distinguir recolha, indexação, referência no pacote e exportação efetiva.
2. Cruzar os tipos acordados — brule/ebrule, eventos/eeventos, udefs/eudefs, ti/tc, campos físicos u_*, escr, jsu, cssjs, formc, formb, ftu, Fx_*/User_*, rel/dtl — com código e testes, sem presumir cobertura.
3. Inspecionar testes de receipts, recovery, análise, decisões, endpoints HTTP e integração SQL Server.
4. Registar separadamente testes existentes, testes executados e resultados documentados. O teste SQL Server depende de configuração de ambiente; não declarar execução sem confirmação.
5. Ler e reconciliar os relatórios de fase, documentos de ingestão/recovery, documentação do Portal, roadmap, investigação de cobertura, RAG, P1/P1B, protótipos, README e regras de projeto.
6. Produzir inventário completo dos 79 ficheiros documentais identificados, com classificação, justificação e destino/referência substituta.
7. Apresentar a proposta ao utilizador e aguardar aprovação antes de alterar ou remover documentos.

## Próximo passo

Retomar a inspeção técnica dos collectors e do mapeamento IKP. Não retomar D1/D2 nem iniciar a consolidação da baseline até a auditoria estar concluída e o inventário ser validado.


## Atualização de governação — 2026-11-20

O responsável autorizou a atualização documental nesta branch. Foram criadas fontes canónicas PROJECT_BASELINE.md, DECISIONS.md, PROJECT_STATUS.md e docs/governance/README.md; PROJECT_GUIDELINES, README, regras Cursor e handoff foram alinhados.

Correções consolidadas:
- SQL Server é a base central; PostgreSQL excluído; edição/versão SQL Server não fixada.
- SQLite limitado ao workspace/cache local do Scanner.
- Ferramenta interna da equipa, não produto comercial multiempresa.
- ZIP IKP é transporte temporário; arquivo permanente no código é divergência por resolver.
- PHC Standard curado separado de observações/personalizações dos clientes.
- React Portal já existe; documentos que o descrevem como inexistente são históricos.
- StandardCatalog contém extração/parsing de triggers; pipeline completa de catálogo ainda não demonstrada.

A auditoria permanece em curso. Esta publicação não executou testes nem alterou código. O inventário individual e qualquer decisão de eliminação/fusão documental continuam pendentes.


## Etapa 2 — inventário individual e reconciliação de estado (2026-11-20)

- HEAD de entrada: `6fdb4060a8fdb2de8aec9080620b91b4c9fa89f5`.
- Inventário inicial: 83 ficheiros Markdown/texto; após criar o próprio inventário, total: 84.
- Criado `docs/governance/document-inventory.md`, com classe e tratamento recomendado para cada ficheiro.
- Atualizados avisos de precedência/estado em Scanner PROJECT-STATUS, open-decisions, product-boundaries, P2.B, receipt recovery/host, Incremento C, RAG e roadmap.
- Corrigido `PhcKnowledgeScanner/samples/README.md` para refletir a estrutura realmente versionada.
- Confirmado no código da branch que existem endpoints/serviços/Portal/testes de decisão do Incremento C; a existência dos ficheiros prova implementação parcial, não conclusão funcional total.
- Sem alterações de código, sem builds/testes e sem eliminação de documentos.
- Commit desta etapa: `070e456b06f7054c0764a3f4cac6532150381743`.


## Etapa 3 — ciclo de vida do ZIP IKP (2026-11-20)

- Inspeção read-only de `IkpPackageIngestionService`, `PackageReceiptRecoveryService`, `PackageStagingCleanupService`, `FileSystemImmutablePackageStore`, interfaces de storage, `IkpPackageAnalysisService` e `MaterializationReadinessEvaluator`.
- Confirmado: staging é removido após conclusão do receipt; ZIP é arquivado permanentemente por SHA-256; análise inicial depende de reabrir o ZIP; não existe operação de delete no store; cleanup só elimina staging órfão; readiness não materializa entidades.
- Criado `docs/architecture/ikp-temporary-package-lifecycle-gap-2026-11-20.md` com o fluxo atual, divergência face à decisão, riscos e critérios mínimos.
- Nenhum código alterado; nenhum teste/build executado.


## Atualização do inventário após auditoria do ZIP — 2026-11-20

- Criado `docs/architecture/ikp-temporary-package-lifecycle-gap-2026-11-20.md`.
- Inventário documental atualizado de 84 para 85 ficheiros Markdown/texto.
- O relatório regista a divergência: arquivo permanente implementado versus ZIP temporário aprovado; análise depende atualmente de reabrir o ZIP; não existe delete no store nem materialização integral.


## Etapa 4 — contrato de materialização IKP (2026-11-20)

- Confirmado HEAD de entrada da inspeção: `b22c6fb1e9f954a14d02cfe5b0c903c956d0cff3`, branch `feature/increment-c-package-import-analysis-confirmation`.
- Inspecionados os scripts SQL 001, 002, 004 e 005; modelos Receipt/Attempt/Analysis/Decision; regras e serviço de decisão; readiness evaluator; repositórios SQL Server; endpoints Portal; serviço de análise e package store.
- Evidência: Receipt, Analysis e Decision são persistidos separadamente; a decisão é append-only e ligada ao SemanticFingerprintSnapshot; não foi identificada materialização do conhecimento no percurso inspecionado.
- Criado `docs/architecture/ikp-materialization-contract-proposal-2026-11-20.md`, explicitamente proposta não aprovada. Abrange identidade/proveniência, itens e artefactos, estados, idempotência, recovery, verificação e eliminação segura.
- Questões em aberto: destino durável dos bytes, granularidade de transação, política para Rejected/Blocked, reanálise após eliminação e retenção.
- Sem alterações de código, migrations ou testes; nenhum ZIP foi eliminado.


## Etapa 5 — reconciliação documental e fluxo operacional (2026-11-20)

- Confirmada a branch `feature/increment-c-package-import-analysis-confirmation`; HEAD de entrada desta etapa: `38e53a8f07d12d5fc15d0ae87f6ac4990faedd08`.
- Verificada a árvore Git completa: 86 ficheiros documentais (`83 .md`, `1 .mdc`, `2 .txt`). O inventário individual já continha 86 entradas, mas o total no cabeçalho dizia 85; corrigido para 86. As referências anteriores a 79/83/84/85 pertencem a fotografias intermédias e não representam a árvore atual.
- Reconciliados os documentos canónicos com o fluxo operacional confirmado pelo responsável: Scanner executado no servidor do cliente → ZIP criado localmente → transferência manual pelo operador → upload manual no Portal PHC Knowledge. Não existe envio direto Scanner→Knowledge.
- Confirmado no código que existe `POST /api/v1/ikp/packages`, autenticado por JWT Bearer e scopes IKP. O frontend React atual não tem upload de pacotes nem gestão funcional de capturas; falta a operação/API do Portal autenticada pela sessão do Portal que invoque internamente a ingestão. A especificação do Portal já explicita esta lacuna.
- Corrigida a especificação do Portal para não afirmar que o ZIP deve permanecer arquivado permanentemente nem limitar a base central a SQL Server Express. A baseline continua a estabelecer SQL Server, edição/versão por decidir, e ZIP temporário.
- Atualizados `PROJECT_BASELINE.md`, `DECISIONS.md` (D-014), `PROJECT_STATUS.md`, `docs/architecture/portal-experience-specification-2026-11-19.md` e `docs/governance/document-inventory.md`.
- A inspeção de títulos, notas de precedência e referências a tecnologia/estado foi feita sobre os documentos versionados; a classificação individual existe. Isto não equivale ainda a uma validação semântica exaustiva de cada afirmação técnica em todos os 86 ficheiros.
- Permanecem por concluir: reconciliação técnica dos collectors/mapeamentos IKP e testes por operação; PHC Standard Catalog; estratégia de pesquisa; validação completa da especificação de Portal; fecho do contrato de materialização e política operacional para tentativas `NeedsAttention` sem bytes recuperáveis.
- Nenhum código, migration, teste ou artefacto de captura foi alterado/executado nesta etapa. Não foram eliminados documentos nem ZIPs.
