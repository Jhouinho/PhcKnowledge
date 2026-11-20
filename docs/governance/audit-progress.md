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


### Ajustes de precedência adicionais

- `PhcKnowledgeScanner/docs/open-decisions.md` já contém nota explícita de que é uma checklist histórica, não um backlog atual; manter as questões não assinaladas sem as reabrir automaticamente.
- `PhcKnowledgeScanner/docs/decisions/0003-independence.md` foi marcado como histórico: o repositório separado foi substituído pelo monorepo, mantendo-se a autonomia de execução e responsabilidade do Scanner.
- `docs/architecture/portal-frontend-architecture-spike-2026-11-19.md` recebeu nota de precedência porque descrevia um estado anterior à criação do frontend React.


## Decisão de classificação após a etapa 6 — 2026-11-20

O responsável confirmou a separação arquitetural entre PHC Standard e recolha específica de instalações:

- `REL`/`DTL` não devem ser capacidades do Scanner. As relações PHC standard são conhecimento interno comum, mantido no PHC Standard Catalog e versionado pela respetiva aplicabilidade PHC; não devem ser repetidas em cada captura.
- As operações `phc.declared-relations.rel` e `phc.declared-relations.dtl` permanecem descritas como comportamento encontrado no código atual, mas passam a ser consideradas legado a retirar/rever. A classificação-alvo do Scanner passa de 23 para 21 operações.
- As sete operações SQL Server são mecanismos de descoberta da estrutura física da instalação. A sua presença no Scanner não implica exportação integral de todos os objetos como personalizações.
- A comparação com o catálogo standard e a exportação de diferenças específicas ainda não estão implementadas. Até lá, não classificar automaticamente objetos desconhecidos como standard ou personalizados.
- Nenhum código, schema IKP, migration ou teste foi alterado nesta decisão documental.

## Etapa 6 — auditoria operação a operação do Scanner/IKP (2026-11-20)

- Confirmado no código o catálogo de 23 operações: 7 SQL Server, 14 personalizações PHC e 2 REL/DTL.
- Cruzadas as operações declaradas com os três collectors, fontes SQL, estados obrigatórios/opcionais, ObjectOperationResolver, Object Index, exportador IKP e testes existentes.
- Mapeamento dos 21 kinds do Object Index tem testes sintéticos; `ObjectOperationResolverTests` cobre as variantes e `ObjectIndexProjectorTests` projeta os 21 kinds. Captura real documentada CSDefault202603: 16.618 objetos, apenas 8/21 kinds presentes; não é validação real de todos os kinds.
- `IkpPackageExporterTests` valida ZIP e contrato com workspace sintético. Existe também harness que validou um ZIP real `CSDefault202603-ikp.zip` na receção/ingestion do Knowledge; a documentação não prova explicitamente que esse ZIP foi produzido pelo `IkpPackageExporter` atual. `ScanEngineTests` cobre semântica de coverage; não foi encontrada suite de integração SQL dos collectors contra PHC real.
- Lacunas encontradas: (1) operação FTU inclui funções `Fx_*`/`User_*` de `sys.objects`, mas só corre se `dbo.ftu` existir; (2) `RunOptionalAsync` chama `sourceExistsAsync` fora do try/catch por operação; (3) Probe de personalizações agrega presença de três tabelas core e não verifica individualmente as operações core; (4) coverage de objeto não garante definição disponível — exporter reconcilia estado de definição separadamente.
- Detalhes e tabela completa das 23 operações publicados em `docs/architecture/ikp-0.1-scanner-compatibility-matrix.md`, secção “Auditoria operacional do Scanner — 2026-11-20”.
- Nenhum código, migration, teste ou captura foi alterado/executado. As lacunas estão registadas como achados; não foram corrigidas nesta etapa.
- Próxima verificação: analisar a cadeia de exportação dos artefactos e a correspondência entre 23 operações, objetos, definições e conteúdo no ZIP; confirmar a proveniência do ZIP real usado no harness e fechar o round-trip captura real → exporter atual → validador independente.

## Etapa 7 — cadeia de artefactos, exportação IKP e validação (2026-11-20)

- Inspecionados `CollectedArtifact`, `IScanWorkspace`, `WorkspaceArtifactMetadata`, `SqliteScanWorkspace`, collectors SQL/PHC, `ObjectIndexProjectionInputReader`, `DefinitionProjectionResolver`, `ObjectIndexProjector`, `IkpPackageExporter`, `IkpPackageInspectionEngine`, validadores C#/Python, testes e workflows.
- Confirmado: os bytes dos artefactos ficam em ficheiros sob `workspace/artifacts`; a SQLite guarda metadata e `objects.definition_artifact_id` liga objetos aos artefactos. O exporter preserva os bytes selecionados e verifica tamanho/SHA-256 antes de os incluir.
- Confirmado: o reader só lê bytes de artefactos referenciados por objetos; o projector resolve estados de definição separadamente da coverage. Database/table/PK/FK e PHC user fields são `NotCollected` para definição. Para user fields, o collector gera um ficheiro descritivo, explicitamente ignorado pelo resolver como definição original.
- Achado: o exporter seleciona artefactos a partir de todos os `DefinitionArtifactId` dos objetos, pelo que inclui também o ficheiro descritivo de user fields e declara-o `source-definition`, apesar de o Object Index emitir `NotCollected` e lista de artefactos vazia. Os validadores não rejeitam artefactos do manifesto que não estejam referenciados no Object Index. Requer decisão/ajuste de contrato.
- Achado secundário: `DefinitionStatus.Empty` está declarado, mas o resolver auditado não emite esse estado. A política para referências com bytes ausentes/hash/tamanho divergentes também merece clarificação: o projector consegue representar `Unavailable`, mas o exporter falha quando tenta incluir a referência original.
- Confirmado: existe round-trip sintético exporter → validador C# em `PhcKnowledge.Ikp.Integration.Tests` e o workflow `dotnet-scanner.yml` também envia um ZIP sintético para o validador Python independente `validate_ikp_zip.py`. A configuração está versionada; não foi executada nesta auditoria.
- Evidência real: P1.B documenta 16.618 objetos, 8/21 kinds e 49 artefactos validados no workspace `CSDefault202603`, mas não prova ZIP completo gerado pelo exporter atual. O harness de 18-11 valida um ZIP real na receção, sem provar a proveniência de produção.
- O exporter atual ainda inclui REL/DTL em `payload/relations/declared-relations.json`; comportamento legado em conflito com D-015, sem alteração nesta etapa.
- Nenhum código, schema IKP, migration, teste ou captura foi alterado/executado nesta etapa.
- Próximo passo: fechar a semântica do artefacto descritivo `phc.user_field` e a proveniência/round-trip da captura real antes de decidir alterações ao exporter. Depois atualizar contrato e testes, e só então implementar.


## Atualização — D-016 e continuidade da auditoria (2026-11-20)

- Decisão aprovada: o Scanner transporta sempre a captura completa; não compara com scans anteriores.
- Durante a materialização, o PHC Knowledge compara o conteúdo com o que já está persistido para a mesma instalação. Conteúdo idêntico não é gravado novamente; conteúdo novo é inserido; conteúdo alterado origina nova versão.
- Cada captura e a sua observação continuam registadas, mesmo quando reutilizam conteúdo idêntico. A ausência numa captura posterior não significa eliminação.
- O artefacto descritivo de `phc.user_field` continua no ZIP; deve ser classificado como evidência/metadados, não como `source-definition`, quando não representa definição original.
- Documento funcional publicado: `docs/architecture/ikp-incremental-materialization-policy-2026-11-20.md`; decisão D-016 publicada em `DECISIONS.md`.
- Sem alterações de código, schemas IKP, migrations ou testes. Sem builds, testes ou captura real executados.
- Próximo passo de auditoria: inspecionar o contrato de manifesto/validação para identificar os papéis de artefacto atualmente permitidos e desenhar a representação correta para evidência descritiva. Depois fechar identidade estável e fingerprints por instalação, antes de autorizar implementação.


## Atualização — auditoria do papel dos artefactos IKP (2026-11-20)

Inspeção read-only de `IkpPackageExporter`, `IkpPackageInspectionEngine`, `IkpInspectedArtifact`, `manifest.schema.json` e `object-index.schema.json` confirmou:

1. O exporter atribui atualmente o role literal `source-definition` a todos os artefactos obtidos a partir de `TechnicalObjectRecord.DefinitionArtifactId`; não determina o role com base no estado `definition.status` do Object Index.
2. O Object Index pode marcar `definition.status = NotCollected` e emitir `artifactIds: []`, enquanto o artefacto descritivo continua no manifesto/payload por estar referenciado pelo registo interno do objeto.
3. O schema do manifesto exige que `role`, quando presente, seja string não vazia; não define enum nem lista fechada de roles.
4. O inspection engine lê e transporta o role para `IkpInspectedArtifact`, sem validação semântica/whitelist do seu valor.
5. O validador exige que os payload paths correspondam aos artefactos do manifesto e que os hashes/tamanhos coincidam. Exige também que os IDs indicados por `definition.artifactIds` existam no manifesto, mas não exige o inverso (todos os artefactos do manifesto serem definições referenciadas pelo Object Index).

**Conclusão limitada:** um role específico para evidência/metadados parece compatível estruturalmente com o schema IKP 0.1 atual, mas ainda não foi verificado o comportamento de todos os consumidores downstream nem existe teste dedicado. O exporter atual teria de deixar de aplicar indiscriminadamente `source-definition` e distinguir os artefactos descritivos. Não foi feita qualquer alteração.

Próxima inspeção: determinar que metadados persistidos no workspace permitem distinguir inequivocamente os artefactos descritivos de `phc.user_field` dos artefactos de definição, e verificar a associação exata objeto↔artefacto. Não escolher o novo role nem alterar o schema/código até concluir essa análise.


## Atualização — origem e semântica do artefacto `phc.user_field` (2026-11-20)

A inspeção do collector, do workspace e dos resolvers confirmou:

- `CollectUserFieldsAsync` consulta `sys.columns`, `sys.tables`, `sys.schemas` e `sys.types` para colunas `u_*`.
- Para cada campo, cria um ficheiro descritivo `.txt` em `phc/user_fields/{schema}_{table}_{column}.txt`, associa-o ao objeto por `DefinitionArtifactId` e mantém os mesmos factos estruturados em `SummaryJson`.
- O helper `SaveDefinitionAsync` é reutilizado e grava metadata genérica (`encoding=utf-8`, `format=prg)); não existe atualmente um campo explícito que identifique a finalidade semântica do artefacto. A extensão `.txt` e o conteúdo não devem ser a única base para classificação.
- `DefinitionProjectionResolver` trata explicitamente `PhcUserField` como `NotCollected`, porque o ficheiro é descrição de metadados e não definição original da coluna.
- `MetadataProjectionResolver` produz uma projeção e fingerprint de metadados para o campo com schema, tabela, coluna, tipo, comprimento, precisão, escala e nulabilidade; o prefixo é metadado descritivo, não entra no fingerprint semântico.
- `SourceIdentityResolver` consegue gerar para `PhcUserField` a identidade `sqlserver.column_id`, com scope baseado no nome da base de dados e no `object_id` da tabela pai. Na materialização, esta identidade terá de ser combinada com a identidade da instalação; não é identidade global entre instalações.
- O role é atualmente hardcoded como `source-definition` pelo exporter para todos os artefactos referenciados. O workspace não contém ainda um purpose/role explícito para este artefacto.

**Conclusão:** existe informação estruturada suficiente para comparar os metadados do campo sem tratar o ficheiro como definição de código. O ficheiro deve continuar a ser transportado por D-016 e associado à observação da captura. O desenho deverá classificar a sua finalidade a partir de evidência explícita do objeto/artefacto (preferencialmente metadata tipada ou associação inequívoca ao `ObjectKind`), nunca apenas pelo nome do caminho. Não se escolhe ainda o mecanismo final nem se altera código.

Próximo passo: verificar se outros collectors reutilizam `DefinitionArtifactId` para conteúdo que não seja definição original e se existe uma classificação por tipo de objeto/artifacto que possa ser generalizada sem heurísticas frágeis.


## Atualização — distinção semântica de FORMB e FORMC (2026-11-20)

A revisão dos restantes artefactos especiais do collector PHC identificou dois casos que não devem ser classificados em bloco como `source-definition`:

### FORMB — ecrãs personalizados
- O collector gera um ficheiro `.txt` com stamp, ecrã, nome, tabela, descrição, defeito, ordem e estado.
- O `SummaryJson` do objeto guarda apenas source, stamp, ecrã, nome, tabela, defeito e estado; não inclui `descricao` nem `ordem`.
- O `MetadataProjectionResolver` também não projeta `descricao` nem `ordem`.
- Portanto, o ficheiro contém informação de configuração/descritiva que não está integralmente representada no Object Index. Deve continuar a ser transportado e não ser tratado como código executável só por estar ligado a `DefinitionArtifactId`.

### FORMC — grelhas/objectos ocultos
- O collector lê `formc.prop`, extrai `Xexpressao` com `FormcPropParser` e produz uma versão formatada por `FoxProFlattenedCodeFormatter`.
- O artefacto atual guarda a versão formatada e um cabeçalho descritivo; o `SummaryJson` guarda apenas stamp, ecrã, nome, tag e tvalobj.
- O valor original completo de `prop` não é persistido no objeto nem num artefacto separado no percurso inspecionado. Assim, o payload atual preserva uma representação derivada, mas não a evidência original integral.
- É necessário desenhar a preservação separada do valor original de `prop` e da expressão extraída/formatada. Não alterar o collector nesta etapa.

**Conclusão:** a classificação de artefactos precisa de distinguir, pelo menos, definição original, representação derivada e evidência/configuração descritiva. A decisão D-016 exige transportar a captura completa; não autoriza eliminar ficheiros que contenham dados que ainda não estejam representados no Object Index. O desenho de roles e a cobertura de dados devem ser fechados por tipo de objeto antes de qualquer alteração.


### Extensão da matriz de artefactos — CSSJS, ESCR e definição projetada (2026-11-20)

- **CSSJS:** o artefacto contém os campos originais `source` e `sourcedefault` (quando presentes), mais cabeçalho; o Object Index não contém esses corpos. É conteúdo-fonte Web e deve continuar transportado.
- **ESCR:** o artefacto preserva os campos originais `expressao`, `cmen` e `cmenhtml` (quando presentes), mais metadados de contexto; o Object Index contém apenas uma parte dos metadados. É evidência de script/conteúdo Web, não apenas metadata.
- **JSU:** o artefacto contém JavaScript original mais cabeçalho; o Object Index não contém o corpo do script.
- **FORMB:** o artefacto é descritivo/configuracional, não contém uma definição de código. Apesar disso, o resolver atual trata qualquer objeto `phc.*` com artefacto como `Available`, exceto os kinds explicitamente em `NotCollectedKinds`; logo, `PhcCustomScreen` é atualmente projetado como definição disponível. Esta semântica precisa de revisão.
- **FORMC:** o artefacto contém uma representação formatada/derivada de `Xexpressao`; o original completo `prop` não é guardado. O resolver atual também o trata como definição disponível. A futura projeção deve distinguir original de derivado.

A matriz já evidencia pelo menos três finalidades diferentes: definição/conteúdo-fonte original, representação derivada e evidência/configuração descritiva. Não assumir que `DefinitionArtifactId` significa sempre definição original. Não alterar o código nesta etapa.


## Auditoria da captura real CSDefault202603 — 2026-11-20

A captura de 16-11 foi localizada na Biblioteca e inspecionada diretamente. O ZIP exterior contém o workspace SQLite e a pasta de artefactos. A cópia de 18-11 é byte a byte igual. Proveniência: Scanner commit `5e3308182e57486732b659b39664bcec8c0d99ed`, CaptureId `49826eeb-0388-48bb-a495-2031f30a58a4`, PackageId `b0b7e61f-8ea4-4cf0-a57d-99d37ab220ea`.

- 16.618 objetos: 8.294 tabelas, 8.268 PK, 36 procedures, 11 hidden grids, 6 funções T-SQL PHC, 1 custom screen, 1 hotkey e 1 database.
- Definições: 49 disponíveis (19 PHC + 30 SQL Server), 6 indisponíveis e 16.563 não recolhidas. Metadata fingerprints: 16.581; definition fingerprints: 49; source identities: 8.307.
- SQL: zero FK, views e triggers; sem colunas ou índices no Object Index/workspace. As tabelas têm apenas metadados básicos; as PK têm as colunas da chave. Seis procedures `usp_*` não têm definição; as 30 definições presentes são `dbo.dt_*` de suporte SQL Server.
- PHC: 19 definições (FORMB 1, FORMC 11, FTU 6, teclas 1). REL/DTL: 295 + 626 = 921; 921 interpretações no workspace e zero relações derivadas na tabela `relations`.
- O pacote IKP de 16-11 não transporta interpretações REL/DTL num ficheiro autónomo, embora o workspace as tenha. O pacote de 10-11 incluía esse ficheiro; registar como diferença pendente de reconciliação.
- A fonte `docs/sources/triggers phc.sql` contém 552 `CREATE TRIGGER` standard; a operação SQL da captura observou zero. Não misturar estas fontes.
- O Database Schema Snapshot separado já existe no código e suporta colunas, índices, FK, views, triggers, procedures, funções, parâmetros, tipos e synonyms; não está incluído no ZIP de 16-11.
- A captura continua a declarar 23 operações (inclui REL/DTL); D-015 define 21 como direção-alvo do Scanner, com REL/DTL no PHC Standard Catalog.
- Relatório detalhado: `docs/architecture/csdefault-standard-catalog-capture-audit-2026-11-20.md`.

**Conclusão:** a captura é íntegra como inventário observado, mas não é um catálogo físico completo. Não classificar automaticamente tabelas como PHC standard só por estarem na CS Default; usar a baseline versionada e a cobertura comprovada. Não foram alterados código, schemas, migrations ou testes.

Próximo passo: localizar/reconciliar o snapshot estrutural completo já fornecido, rever a diferença de exportação das interpretações REL/DTL e definir a incorporação das 552 triggers no catálogo standard. Não voltar a pedir uma captura antes desta reconciliação.
