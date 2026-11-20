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
A auditoria documental/técnica continua em curso. Foi criado `docs/governance/document-inventory.md`, com classificação individual dos 86 ficheiros Markdown/texto presentes após esta publicação. A classificação define destino de manutenção; não equivale a validação integral de todas as afirmações técnicas. Não foi apagada documentação histórica.

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


## Auditoria do contrato de materialização IKP — 2026-11-20

- Branch: `feature/increment-c-package-import-analysis-confirmation`.
- Commit de referência da inspeção: `b22c6fb1e9f954a14d02cfe5b0c903c956d0cff3`.
- Documento publicado: `docs/architecture/ikp-materialization-contract-proposal-2026-11-20.md`.
- A proposta é técnica e **não aprovada**; não altera DECISIONS.md nem converte recomendações em decisões.
- Confirmada separação entre Receipt, Analysis, Decision e a futura Materialization. Não foi encontrada implementação de materialização no percurso inspecionado.
- Questões que bloqueiam o desenho final: destino durável dos bytes dos artefactos; transações/batches; política para pacotes Rejected/Blocked; reanálise depois da eliminação; retenção.
- Não iniciar migrations, materializador ou eliminação do arquivo antes de aprovação explícita.
- Sem alterações de código e sem testes/build nesta etapa.

### Próxima ação
Discutir e decidir o destino dos bytes dos artefactos e a política de disposição para pacotes rejeitados/bloqueados. Depois fechar o modelo lógico e os estados; só então preparar implementação e testes de recovery.


## Atualização — auditoria documental e fluxo manual IKP (2026-11-20)

- Branch: `feature/increment-c-package-import-analysis-confirmation`.
- Commit de referência da auditoria documental nesta sessão: `38e53a8f07d12d5fc15d0ae87f6ac4990faedd08`. As atualizações documentais desta sessão foram publicadas em commits subsequentes; consultar sempre o HEAD remoto da branch para a referência atual.
- A árvore Git foi verificada: 86 ficheiros documentais (`83 .md`, `1 .mdc`, `2 .txt`). O inventário contém 86 entradas; o total do cabeçalho foi corrigido de 85 para 86.
- Fluxo operacional confirmado: Scanner corre no servidor do cliente e gera ZIP; o operador transfere manualmente o ZIP para o ambiente com acesso ao PHC Knowledge e faz upload manual através do Portal. Não existe comunicação direta Scanner→Knowledge.
- D-014 foi registada em `DECISIONS.md`; `PROJECT_BASELINE.md` e especificação do Portal foram alinhados com este fluxo.
- Código confirmado: `POST /api/v1/ikp/packages` existe como API IKP com JWT Bearer/scopes. A UI React atual não disponibiliza upload manual nem gestão de pacotes/capturas. Falta uma operação Portal autenticada por cookie/antiforgery que invoque internamente a ingestão.
- `PROJECT_STATUS.md` distingue agora a API IKP existente da funcionalidade de upload no Portal ainda ausente.
- A especificação do Portal foi corrigida: ZIP temporário até materialização verificada; base central SQL Server sem restringir a Express.
- Estado de armazenamento/materialização: bytes dos artefactos em SQL Server `varbinary(max)` V1 aprovado; pacotes Rejected com retenção inicial de 30 dias de calendário após decisão, configurável, com preservação por hold; artefacto físico isolado por OrganizationId e PackageId, mesmo para ZIPs com hash igual. O desenho 7.3/7.4 permanece proposta, não implementação.
- Continua pendente a confirmação do responsável sobre o tratamento de tentativas `NeedsAttention` sem staging nem arquivo recuperável. Não implementar migrations/materializador/eliminação antes do fecho do contrato.
- Nenhum código, migration ou teste foi alterado/executado nesta etapa; não foram eliminados documentos nem ZIPs.

### Próximo passo
Concluir a reconciliação técnica dos documentos de collectors/IKP e do PHC Standard Catalog; depois fechar o contrato de materialização e o tratamento de `NeedsAttention`. A auditoria documental ainda não está concluída e não deve ser marcada como tal.


- Notas de precedência adicionais publicadas: ADR 0003 do Scanner (repo separado substituído pelo monorepo) e spike de arquitetura do frontend (anterior à existência do React). `open-decisions.md` já estava identificado como checklist histórica, não backlog vigente.


## Atualização — classificação PHC Standard vs Scanner (2026-11-20)

- O responsável confirmou que `REL`/`DTL` não devem ser capacidades do Scanner: as relações PHC standard são conhecimento interno comum, mantido no PHC Standard Catalog e versionado segundo a aplicabilidade PHC.
- O código auditado continua a declarar 23 operações (7 SQL Server, 14 personalizações PHC, 2 REL/DTL); a classificação-alvo do Scanner passa conceptualmente a 21 operações. As duas operações REL/DTL são comportamento legado a retirar/rever, não uma alteração já implementada.
- As sete operações SQL Server são descoberta da estrutura física da instalação. Não significam exportar todos os objetos standard em cada ZIP; a comparação com PHC Standard e a exportação de deltas locais continuam por desenhar.
- Até existir cobertura suficiente do catálogo e regras de comparação, objetos desconhecidos permanecem por classificar; não os rotular automaticamente como standard ou personalizados.
- Sem alterações de código, IKP, migrations ou testes nesta etapa.

## Atualização — auditoria Scanner/IKP operação a operação (2026-11-20)

- Commit anterior a esta atualização: `c68d402fcab2d0a45db89f39828d5e7f5da7e3b6`.
- Catálogo confirmado: 23 operações (7 SQL Server, 14 PHC customizations, 2 REL/DTL). Tabela de fontes, opcionalidade e projeção publicada na matriz de compatibilidade IKP.
- 21 kinds do Object Index têm mapeamento/testes sintéticos. Captura real documentada CSDefault202603: 16.618 objetos e 8/21 kinds; não valida os restantes kinds.
- Testes existentes: ScanEngine cobre semântica de coverage; ObjectOperationResolver cobre mapeamentos; ObjectIndexProjector cobre projeção sintética; IkpPackageExporter cobre ZIP sintético. O harness de receção valida um ZIP real CSDefault202603, mas não está comprovado que esse ZIP tenha sido produzido pelo exporter atual. Não foi encontrada suite de integração SQL dos collectors contra PHC real.
- Achados: FTU+Fx/User fica todo condicionado à existência de dbo.ftu; exceção em `RunOptionalAsync.sourceExistsAsync` não é capturada pelo tratamento por operação; probe de personalizações agrega tabelas core em vez de verificar cada uma; estado de definição/artefactos deve ser lido separadamente da coverage de objetos.
- Não foram feitas alterações de código nem executados testes/builds ou recolhas reais.
- Próximo passo: continuar a auditoria da cadeia de exportação dos artefactos; mapear a fonte standard PHC para REL/DTL e avaliar o que as sete operações SQL devem comparar/exportar. Confirmar a proveniência do ZIP real do harness e fechar o round-trip captura real → exporter atual → validador independente antes de alterar collectors ou o contrato IKP.


## Atualização — auditoria da cadeia de artefactos IKP (2026-11-20)

- Workspace Scanner: bytes dos artefactos em ficheiros `artifacts/{collectorId}/{relativePath}`; metadata e referências em SQLite (`artifacts` e `objects.definition_artifact_id`). O exporter verifica SHA-256/tamanho e preserva bytes.
- Reader lê bytes apenas para artefactos referenciados por objetos; projector determina `definition.status` de forma independente. PHC user fields têm um ficheiro descritivo, mas o resolver marca a definição como `NotCollected` e não inclui esse ID em `definition.artifactIds`.
- Inconsistência a decidir: `IkpPackageExporter` recolhe todos os IDs de `TechnicalObjectRecord.DefinitionArtifactId` e exporta também o artefacto descritivo de user fields como `source-definition`. O manifesto e os validadores aceitam payload sem referência no Object Index. Não corrigido.
- `DefinitionStatus.Empty` existe no enum/schema, mas não é emitido pelo resolver. O projector pode marcar artefactos inválidos como `Unavailable`; o exporter, contudo, falha ao tentar exportar a referência sem bytes válidos. Política a clarificar.
- Há round-trip sintético exporter → validador C# e o workflow `dotnet-scanner.yml` também valida ZIP sintético com o validador Python independente. Configuração verificada; não executada nesta sessão.
- P1.B documenta 16.618 objetos, 8/21 kinds e 49 artefactos verificados na projeção do workspace real `CSDefault202603`. O ZIP real usado no harness de receipt/ingestion não tem proveniência documentada; não afirmar que veio do exporter atual.
- Exporter atual ainda inclui REL/DTL no ZIP, comportamento legado que terá de ser alinhado com D-015.
- Sem alterações de código, schemas, migrations, testes ou capturas; sem builds/testes executados.
- HEAD remoto antes da atualização final do handoff: `8cc7bae1b788bcfb819f28f49a34fd51bc8d9cec`.
- Próximo passo: decidir tratamento do artefacto descritivo de user fields e obter workspace/ZIP real com proveniência demonstrável para completar o round-trip real.


## Atualização — decisão de classificação Scanner / PHC Standard (2026-11-20)
- Após o registo desta atualização, a proposta `docs/architecture/ikp-materialization-contract-proposal-2026-11-20.md` recebeu nota de precedência D-015: o payload REL/DTL legado não deve ser materializado automaticamente como personalização de cada instalação.
- HEAD remoto imediatamente antes desta atualização final do handoff: `d2fab38c0b2a0f7f48dfca93235efb1d68071dfa`.


- HEAD remoto imediatamente antes desta atualização: `8fb7c14a139defa0132f6780a77febab188ba891`.
- Registada a decisão D-015 em `DECISIONS.md`: relações PHC standard em REL/DTL pertencem ao PHC Standard Catalog, como conhecimento comum versionado por aplicabilidade PHC; não são recolhidas integralmente em cada captura.
- `PROJECT_BASELINE.md` clarifica a separação entre PHC Standard, observações e personalizações, e o papel das sete operações SQL Server como mecanismos de descoberta da estrutura física.
- `PROJECT_STATUS.md` distingue as 23 operações atualmente declaradas no código da classificação-alvo de 21 operações Scanner (7 SQL Server + 14 personalizações PHC). REL/DTL são comportamento legado a retirar/rever.
- A matriz de compatibilidade regista a reclassificação, preservando a tabela das 23 operações como evidência do código auditado. O modelo de domínio 0.2 recebeu nota de precedência para a secção REL/DTL histórica.
- `audit-progress.md` regista a decisão e a pendência de desenhar comparação com PHC Standard e exportação de diferenças locais.
- Sem alterações de código, schemas IKP, migrations, testes ou capturas. Não foram executados builds/testes.
- Próximo passo: continuar a auditoria da cadeia de exportação dos artefactos e esclarecer a proveniência do ZIP real do harness; depois desenhar como as operações SQL comparam a estrutura observada com o PHC Standard e como representar apenas diferenças específicas.


## Atualização — captura completa e materialização incremental (2026-11-20)

- O responsável aprovou D-016: o Scanner transporta sempre a captura completa, incluindo artefactos descritivos; não depende de scans anteriores.
- Na integração no Portal/SQL Server, o PHC Knowledge compara o conteúdo com o que já está persistido para a mesma instalação: conteúdo rigorosamente igual não volta a ser gravado; conteúdo novo é inserido e conteúdo alterado origina nova versão.
- Cada captura/observação mantém a sua proveniência, mesmo quando reutiliza conteúdo já persistido. A ausência numa captura posterior não elimina automaticamente informação anterior.
- Foi criado `docs/architecture/ikp-incremental-materialization-policy-2026-11-20.md` com as regras funcionais, limites e testes mínimos.
- O artefacto descritivo de `phc.user_field` continua a ser transportado; não deve ser classificado como `source-definition` se apenas representa metadados. O papel específico no schema IKP ainda requer desenho.
- Nenhum código, schema IKP, migration ou teste foi alterado; não foram executados builds/testes.
- Próxima etapa: continuar a auditoria da exportação para determinar como o manifesto/schema IKP pode representar artefactos descritivos sem os confundir com definições de código; depois fechar a identidade estável e a comparação por instalação antes de desenhar a implementação.


## Atualização — auditoria do role de artefactos IKP (2026-11-20)

- O exporter atual atribui `source-definition` a todos os artefactos referenciados por `TechnicalObjectRecord.DefinitionArtifactId`, sem cruzar essa atribuição com `definition.status` e `definition.artifactIds` projetados no Object Index.
- Para `phc.user_field`, isto permite transportar o ficheiro descritivo como `source-definition`, apesar de a definição estar como `NotCollected` e sem artifactIds no Object Index.
- `manifest.schema.json` aceita `role` como string não vazia, sem enum fechado. O inspection engine preserva o role em `IkpInspectedArtifact`, sem whitelist semântica.
- O validador exige que os artefactos listados em `definition.artifactIds` existam no manifesto, mas não exige que todo o artefacto do manifesto seja referenciado como definição no Object Index.
- Conclusão: um role separado para evidência/metadados parece estruturalmente possível em IKP 0.1, mas os consumidores downstream e a distinção inequívoca da origem do artefacto ainda têm de ser auditados.
- Documento atualizado: `docs/governance/audit-progress.md`. Nenhum código, schema, migration ou teste alterado; sem execução de testes/build.
- Próximo passo: inspecionar `WorkspaceArtifactMetadata`, metadata JSON e associação do collector `phc.user_fields` para determinar como distinguir estes artefactos sem heurísticas frágeis; depois auditar os consumidores downstream do role.


## Atualização — evidência técnica `phc.user_field` (2026-11-20)

- `CollectUserFieldsAsync` recolhe colunas `u_*` a partir de `sys.columns` e cria um ficheiro descritivo `.txt` por campo; a informação também fica estruturada em `SummaryJson`.
- O artefacto é ligado ao objeto através de `DefinitionArtifactId`. O helper atual atribui metadata genérica `encoding=utf-8` e `format=prg`, sem um purpose/role explícito; não classificar apenas pela extensão ou path.
- `DefinitionProjectionResolver` marca intencionalmente estes objetos como `NotCollected`; `MetadataProjectionResolver` produz MetadataFingerprint a partir de schema/tabela/coluna/tipo/comprimento/precisão/escala/nulabilidade.
- `SourceIdentityResolver` gera `sqlserver.column_id` com scope de database + object_id da tabela pai; a materialização deve acrescentar InstallationId ao âmbito de identidade.
- Conclusão: o ficheiro descritivo continua a ser transportado por D-016, mas não é uma definição de código. A classificação futura deve usar finalidade explícita ou associação inequívoca ao ObjectKind, não heurísticas de nome de ficheiro.
- Atualizado `docs/governance/audit-progress.md`. Nenhum código/schema/migration/teste alterado.
- Próximo passo: auditar outros collectors que usam `DefinitionArtifactId` para verificar se há artefactos com semânticas diferentes de source-definition.
