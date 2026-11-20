> Fotografia histórica de 2026-11-17. API HTTP, Identity, Portal backend e frontend React evoluíram depois desta revisão. Consultar ../../PROJECT_STATUS.md para o estado atual. Esta matriz continua útil para compatibilidade Scanner/IKP, mas não é o estado global da plataforma.

> **Precedência — auditoria de 2026-11-20:** os parágrafos abaixo sobre identidade global de Package/ZIP, arquivo content-addressed partilhado entre organizações e próximos passos de API são uma fotografia de 2026-11-17. Não representam o contrato atual. Para transporte, armazenamento temporário, isolamento físico por organização/pacote, retenção e materialização, prevalece `ikp-materialization-contract-proposal-2026-11-20.md` e `PROJECT_BASELINE.md`. O fluxo de transporte é Scanner no servidor do cliente → ZIP → transferência manual → upload manual no Portal; não existe envio direto Scanner→Knowledge.


# Matriz de compatibilidade — IKP 0.1, Scanner e PHC Knowledge

Data de revisão: 2026-11-17

## Conclusão

O Scanner emite IKP 0.1. A captura real CSDefault202603 foi examinada: 16.618 objetos, 921 relações declaradas e 921 interpretações REL/DTL. O Object Index inclui 8 dos 21 kinds mapeados; os restantes continuam sem validação real nesta captura.

O núcleo PhcKnowledge.Import já implementa a primeira fatia Receive / Validate / Archive como serviço, com validator, arquivo imutável por SHA-256 e persistência SQL de receipts. Os testes usam fixture IKP / ZIP sintético → Validator / Ingestion (aceitação do pacote válido, arquivo imutável idempotente, registo de receipt por organização, rejeição de path traversal e payload adulterado); não são prova de execução contra SQL Server real nem de API HTTP/autenticação.

## Matriz

| Componente | Estado confirmado | Limite / trabalho restante |
|---|---|---|
| Scanner / CaptureId | CaptureId, capabilities, coverage e Object Index implementados | Validar kinds ausentes e estabilidade entre capturas |
| Object Index | 16.618 objetos na captura real; 8/21 kinds presentes | Não generalizar para os 21 kinds |
| SourceObjectId | Máximo 54; mínimo 12; zero duplicados ordinais, espaços nas extremidades ou não-ASCII na captura | Modelo destino NVARCHAR(256); ainda sem tabela Observation/migration |
| Artefactos | 49 definições associadas e artefactos reais verificados; pacote real inclui manifesto e checksums | Materialização SQL dos artefactos continua futura |
| REL/DTL | 921 factos + 921 interpretações; 295 REL / 626 DTL; zero órfãos | Spec alinhada nesta revisão; entidades SQL futuras |
| IKP ZIP | Exportador IkpPackageExporter; cinco ficheiros de contrato; arquivo ZIP com integridade | Segurança operacional adicional, assinatura/antimalware e política de partilha fora da fatia atual |
| Validator C# | Estrutura, schemas, checksums e referências cruzadas; duplicados sourceObjectId com comparação ordinal | Cobertura de validações semânticas não é idêntica à do exporter |
| Ingestion service | IkpPackageIngestionService coordena validação, arquivo e receipt | Não é endpoint HTTP |
| Package store | FileSystemImmutablePackageStore; content-addressed global por SHA-256; não sobrescreve | Reconciliação de ficheiros sem receipt após falha SQL |
| Receipt SQL | SqlServerPackageReceiptRepository + migration 001_package_ingestion_receipts.sql | Testar em SQL Server real; constraints atuais são por Organization |
| Testes | Fixture IKP / ZIP sintético → Validator / Ingestion (aceitação, arquivo idempotente, receipt, path traversal, payload adulterado) | Não prova SQL Server real, API, autenticação ou ZIP real CSDefault202603 |
| API / autenticação | Não demonstradas | Criar fronteira HTTP; OrganizationId deve vir do contexto autenticado |
| Materialização Knowledge | Ainda não implementada | PackageArtifact/Observation/REL/DTL e processamento posterior |

## Identidade de pacote e recepção

- PackageId identifica globalmente o pacote IKP.
- ZipSha256 identifica os bytes exatos do ZIP e é global no arquivo.
- Num pacote válido, o mesmo SHA-256 implica o mesmo manifesto e o mesmo PackageId.
- Mesmo PackageId com SHA diferente é conflito global.
- Mesmo PackageId e SHA na mesma Organization é receção idempotente.
- Mesmo PackageId e SHA noutra Organization é uma nova receção organizacional ligada ao mesmo Package global e ao mesmo arquivo físico.
- Mesmo CaptureId com PackageIds diferentes é permitido: uma captura pode ser exportada em mais do que um pacote.
- O modelo de destino separa Package global de PackageReceipt por Organization. A migration atual ainda não implementa esta separação física.

## REL/DTL — contrato efetivamente emitido

Ficheiro único: payload/relations/declared-relations.json.

Envelope: schemaVersion, captureId, declaredRelations e interpretations.

Factos e interpretações são coleções lógicas separadas. O Knowledge deverá persistir DeclaredRelation e DeclaredRelationInterpretation como entidades distintas. A interpretação é derivada/versionada; não substitui o facto original nem a expressão PHC.

## Próxima etapa

1. Testar a persistência e as constraints atuais contra SQL Server real.
2. Definir reconciliação/retry para ZIP arquivado sem receipt SQL (arquivo ocorre antes do receipt).
3. Testar explicitamente idempotência e conflitos de PackageId/SHA, incluindo receções multi-organização.
4. Desenhar endpoint HTTP e autenticação; obter OrganizationId do contexto autenticado.
5. Só depois avançar para materialização de artefactos, Observations, REL/DTL e associação de contexto.

Não gerar migrations globais novas nesta etapa. Ver [Modelo de domínio 0.2](knowledge-domain-model-0.2.md).

## Reclassificação arquitetural aprovada — 2026-11-20

A auditoria identificou que a contagem de 23 operações descreve o código existente, mas mistura recolha de personalizações com conhecimento PHC standard que deve ser mantido centralmente.

- **REL/DTL:** as duas operações `phc.declared-relations.rel` e `phc.declared-relations.dtl` são comportamento atualmente implementado e documentado abaixo, mas deixam de integrar o catálogo-alvo de capacidades do Scanner. As relações PHC standard pertencem ao PHC Standard Catalog do Knowledge, como referência comum versionada segundo a aplicabilidade da versão/edição PHC. Não devem ser repetidas em cada ZIP.
- **Diferenças locais:** se uma instalação divergir das relações de referência, o Knowledge poderá registar a diferença como observação específica. O mecanismo para detetar, representar e exportar apenas esses deltas ainda não está definido nem implementado. Não inferir que a simples presença em `REL`/`DTL` numa captura é uma personalização.
- **Estrutura SQL Server:** as sete operações são mecanismos de descoberta da estrutura física da base de dados (identificação, tabelas, PK, FK, views, procedures e triggers). Continuam relevantes para conhecer a instalação, mas não significam que todos os objetos observados devam ser exportados como customizações. O objetivo é comparar com o PHC Standard Catalog e preservar evidência de diferenças específicas.
- **Classificação conservadora:** até existir cobertura e regras de comparação suficientes no catálogo standard, objetos não reconhecidos ficam como observações por classificar; não são automaticamente promovidos a standard nem rotulados como personalizados.
- **Contagem:** o código auditado declara atualmente 23 operações. A classificação-alvo passa conceptualmente a 21 operações Scanner (7 SQL Server + 14 personalizações PHC), sem REL/DTL. Esta alteração é documental/arquitetural; o código, o schema IKP e os testes ainda não foram alterados.

### Catálogo e execução declarada

Na implementação auditada, `KnownOperations` e `ScannerCapabilityCatalog` declaram 23 operações: 7 de estrutura SQL Server, 14 de personalizações PHC e 2 de relações declaradas. Após a reclassificação arquitetural acima, as duas últimas são comportamento legado a retirar do catálogo-alvo; a direção é 21 operações Scanner. A declaração é independente do resultado de uma captura. O resultado efetivo é registado por operação em `coverage.json`; uma capability declarada não prova que a origem exista, que a operação tenha sido executada ou que tenha produzido conteúdo.

| OperationId | Origem efetivamente consultada | Execução no collector | Projeção IKP |
|---|---|---|---|
| `sql.server.structure.database` | `DB_NAME()`, `DB_ID()`, `DATABASEPROPERTYEX` | Obrigatória quando o collector SQL é executado | Object Index: `database` |
| `sql.server.structure.tables.inventory` | `sys.tables`, `sys.schemas` | Obrigatória | Object Index: `table` |
| `sql.server.structure.primary_keys` | Catálogo SQL de índices/chaves primárias | Obrigatória | Object Index: `primaryKey` |
| `sql.server.structure.foreign_keys` | Catálogo SQL de foreign keys e colunas | Obrigatória | Object Index: `foreignKey` |
| `sql.server.structure.views` | `sys.views`, `sys.sql_modules` | Obrigatória | Object Index: `view` |
| `sql.server.structure.procedures` | `sys.procedures`, `sys.sql_modules` | Obrigatória | Object Index: `storedProcedure` |
| `sql.server.structure.triggers` | `sys.triggers`, tabelas/schema e `sys.sql_modules` | Obrigatória | Object Index: `trigger` |
| `phc.customizations.brule` | `dbo.brule` | Obrigatória | Object Index: `phcUserRule`; definição associada quando disponível |
| `phc.customizations.ebrule` | `dbo.ebrule` | Opcional; ausência registada como `NotPresent` | Object Index: `phcWebUserRule` |
| `phc.customizations.teclas` | `dbo.ti` | Obrigatória | Object Index: `phcHotkey` |
| `phc.customizations.udefs` | `dbo.udefs` | Obrigatória | Object Index: `phcDefaultValue` |
| `phc.customizations.eudefs` | `dbo.eudefs` | Opcional; ausência registada como `NotPresent` | Object Index: `phcWebDefaultValue` |
| `phc.customizations.eventos` | `dbo.eventos` | Obrigatória | Object Index: `phcEvent` |
| `phc.customizations.eeventos` | `dbo.eeventos` | Opcional; ausência registada como `NotPresent` | Object Index: `phcWebEvent` |
| `phc.customizations.user_fields` | `sys.columns`, `sys.tables`, `sys.schemas`, `sys.types`; filtra nomes `u_` | Obrigatória; consulta colunas físicas `u_*` | Object Index: `phcUserField` |
| `phc.customizations.escr` | `dbo.escr` | Opcional | Object Index: `phcWebScript` |
| `phc.customizations.jsu` | `dbo.jsu` | Opcional | Object Index: `phcUserJavascript` |
| `phc.customizations.cssjs` | `dbo.cssjs` | Opcional | Object Index: `phcWebCssJs` |
| `phc.customizations.formc` | `dbo.formc` | Opcional | Object Index: `phcHiddenGrid` |
| `phc.customizations.formb` | `dbo.formb` | Opcional | Object Index: `phcCustomScreen` |
| `phc.customizations.ftu` | `dbo.ftu` e `sys.objects` (`FN`, `IF`, `TF` com nomes `Fx_*`/`User_*`) | Opcional atualmente condicionado à existência de `dbo.ftu` | Object Index: `phcUserTsqlFunction` para `source=ftu` e `source=sys.objects` |
| `phc.declared-relations.rel` | `dbo.rel` | Opcional; verificação individual da tabela | `payload/relations/declared-relations.json`, como facto declarado |
| `phc.declared-relations.dtl` | `dbo.dtl` | Opcional; verificação individual da tabela | Mesmo payload REL/DTL, distinguido por `sourceType` |

### Mapeamento e evidência de testes

- `ObjectOperationResolver` mapeia explicitamente os 7 kinds SQL e 14 kinds PHC para as operações acima. Para objetos PHC exige `SummaryJson.source` e valida a variante; não tenta adivinhar a operação.
- `ObjectOperationResolverTests` cobre os 7 mapeamentos SQL e as variantes PHC, incluindo funções vindas de `ftu` e `sys.objects`.
- `ObjectIndexProjectorTests` constrói uma captura sintética com os 21 kinds do Object Index e verifica a projeção. Isto prova o mapeamento/projeção com fixtures, não a presença de todos os kinds numa instalação real.
- A captura real documentada `CSDefault202603` contém 16.618 objetos, mas apenas 8 dos 21 kinds; o pacote real tem 52 artefactos, dos quais 49 correspondem a definições disponíveis. Não generalizar a validação real aos restantes 13 kinds.
- `IkpPackageExporterTests` valida a estrutura ZIP, os cinco ficheiros de contrato, checksums, referências a artefactos, diagnósticos e REL/DTL com workspace sintético. O harness de receção real (`real-ikp-receipt-integration-harness-2026-11-18.md`) valida um ZIP IKP real `CSDefault202603-ikp.zip` com o validador/ingestion do Knowledge, mas a documentação não comprova explicitamente que esse ZIP foi produzido pelo `IkpPackageExporter` atual. Falta fechar esse round-trip: captura real → exporter atual → validador independente.
- `ScanEngineTests` cobre semântica de coverage (vazio, ausente, falha, parcial, não tentado, skipped e ProbeOnly). Não foi encontrada uma suite de integração SQL específica que execute os três collectors contra uma base PHC real nesta branch.

### Lacunas e riscos encontrados no código

1. **FTU e funções instaladas:** `CollectFtuAsync` recolhe primeiro `dbo.ftu` e depois pesquisa funções `Fx_*`/`User_*` em `sys.objects`. Porém, `RunOptionalAsync` só chama o método quando `dbo.ftu` existe. Se `dbo.ftu` estiver ausente, as funções instaladas independentes não são pesquisadas e toda a operação é marcada `NotPresent`. A origem `sys.objects` não está isolada como operação/capability própria.
2. **Falha na verificação de existência opcional:** `OperationCoverageTracker.RunOptionalAsync` executa `sourceExistsAsync()` fora do bloco `try/catch` de `RunAsync`. Uma exceção na consulta de existência (por exemplo, falha transitória/permissão) escapa ao tratamento por operação; a cobertura dessa operação e das seguintes depende do tratamento externo do ScanEngine. Não há teste específico do tracker para este cenário.
3. **Probe agregado de personalizações:** `PhcCustomizationsCollector.ProbeAsync` considera o collector disponível quando encontra pelo menos uma de `brule`, `eventos` ou `udefs`; depois, as operações não opcionais são executadas individualmente sem probe de existência. A falta de uma dessas tabelas core pode ser registada como erro da operação, e não como `NotPresent`. Confirmar se este comportamento é intencional para todas as versões PHC suportadas.
4. **Cobertura de origem vs. conteúdo:** a coverage mede operações e contagens; a exportação reconcilia definições sem bytes e marca conteúdo parcial. A presença de um objeto no Object Index não garante que a sua definição de código esteja disponível. Consumidores Knowledge devem respeitar `definition.status`, `artifactIds`, issues e coverage, sem inferir completude pelo número de objetos.
5. **Validação real incompleta:** a evidência real documentada cobre 8/21 kinds, 49 definições disponíveis e 52 artefactos no ZIP. Existe validação de receção de um ZIP real no harness, mas não está provado documentalmente que foi produzido pelo `IkpPackageExporter` atual. Falta validar o round-trip captura real → exporter atual → validador independente, e verificar o conteúdo/contagens por cada uma das 23 operações.

## Auditoria da cadeia de artefactos e validação — 2026-11-20

### Workspace SQLite e artefactos

- `SqliteScanWorkspace.SaveArtifactAsync` grava os bytes em ficheiros separados sob `artifacts/{collectorId}/{relativePath}`; a tabela SQLite `artifacts` guarda `artifact_id`, collector, caminho, content type, SHA-256, tamanho e metadata JSON. Os bytes não ficam dentro de `workspace.db`.
- O `ArtifactId` é determinístico por collector e caminho relativo (`{collectorId}/{relativePath}`), não por hash. O método escreve primeiro o ficheiro e depois faz upsert dos metadados SQLite; não existe transação atómica comum entre filesystem e SQLite.
- Os collectors SQL e PHC usam `SaveArtifactAsync` através dos seus helpers `SaveDefinitionAsync` e guardam o ID devolvido em `TechnicalObjectRecord.DefinitionArtifactId`. A associação do objeto é persistida separadamente em `objects.definition_artifact_id`.
- `ObjectIndexProjectionInputReader` lê a integridade SQLite, metadata da captura, objetos, issues e inventário de artefactos; lê bytes apenas para IDs referenciados por objetos. Ficheiro em falta produz diagnóstico e snapshot sem bytes. A integridade SQLite, isoladamente, não verifica a integridade dos ficheiros.
- `DefinitionProjectionResolver` valida unicidade da referência, presença dos bytes, tamanho e SHA-256. Database, table, PK, FK e PHC user fields são `NotCollected` quanto a definição. No caso de `phc.user_field`, o próprio código explica que o ficheiro associado é uma descrição de metadados gerada, não a definição original da coluna. As restantes definições PHC e as definições de views/procedures/triggers podem ser `Available` quando a evidência passa as verificações; ausência ou problemas são representados como `Unavailable`.
- O enum `DefinitionStatus.Empty` existe, mas o resolver auditado não produz esse estado. Não está demonstrado um caso de exportação que o use.

### Montagem do IKP

- `IkpPackageExporter` valida CaptureId, coverage e integridade SQLite, projeta o Object Index, valida tamanho/hash dos artefactos selecionados e constrói o ZIP sem normalizar o conteúdo original dos ficheiros.
- O conjunto de artefactos a incluir é atualmente calculado a partir de **todos os `DefinitionArtifactId` dos objetos**, e não dos `definition.artifactIds` emitidos pelo Object Index.
- **Inconsistência identificada:** o collector de `user_fields` cria um ficheiro descritivo e guarda o respetivo ID no objeto; o resolver classifica explicitamente esse artefacto como não sendo uma definição e emite `NotCollected` com `artifactIds: []`. Apesar disso, o exporter volta a recolher o ID diretamente do objeto e inclui o ficheiro no ZIP como `role: source-definition`. O manifesto lista-o como artefacto e os validadores aceitam-no, porque verificam que os IDs referenciados pelo Object Index existem, mas não exigem que todos os artefactos do manifesto sejam referenciados pelo Object Index. Falta decidir se este ficheiro deve ser excluído do IKP ou representado como artefacto descritivo com papel próprio.
- Artefactos sem referência a partir de qualquer objeto não são incluídos; o teste `ExportAsync_Writes_Contract_Files_And_Only_Referenced_Artifacts` confirma que um artefacto obsoleto fica fora do ZIP.
- O exporter gera `payload/diagnostics/issues.json` e, na implementação atual, `payload/relations/declared-relations.json` para REL/DTL. Esta última inclusão continua a ser comportamento legado e está em conflito com a decisão D-015; não foi alterada nesta auditoria.
- O manifesto enumera os ficheiros payload com tamanho e SHA-256. `checksums.json` cobre os restantes ficheiros de contrato e payload e inclui `logicalContentDigest`. O ZIP é escrito para ficheiro temporário e movido para o destino no fim.

### Validação independente e evidência

- Existe teste de integração sintético `P1_ExportAsync_Produces_Zip_Accepted_By_IkpPackageValidator`: cria workspace SQLite, exporta com `IkpPackageExporter` e valida com `IkpPackageValidator` do projeto Knowledge. Também existem cenários de metadata histórica parcial e adulteração de payload.
- O workflow `.github/workflows/dotnet-scanner.yml` executa `dotnet test PhcKnowledge.sln`, recolhe um ZIP sintético emitido pelos testes e executa `tools/ikp-validation/validate_ikp_zip.py`, um validador Python separado. Este valida estrutura ZIP, paths, duplicados, links simbólicos, CRC, schemas, checksums, digest lógico e referências cruzadas. A configuração do workflow está confirmada; não foi executada nesta auditoria.
- A validação sintética exporter → validador C# e exporter → validador Python está, portanto, automatizada no workflow. Não equivale a validar uma captura PHC real.
- A investigação P1.B documenta projeção do workspace real `CSDefault202603`: 16.618 objetos, 8/21 kinds e 49 artefactos com tamanho/hash confirmados. Documenta também que isso **não** provou um ZIP completo produzido pelo exporter atual.
- O harness de receção de 18-11 valida um ZIP real `CSDefault202603-ikp.zip` no percurso de ingestão/receipt, mas não identifica a proveniência de produção do ZIP. Não é possível afirmar que foi produzido pelo `IkpPackageExporter` atual.

### Pendências concretas

1. Fechar a semântica dos artefactos descritivos de `phc.user_field`: exclusão do payload ou novo tipo/role explícito, sem os apresentar como definição de código.
2. Decidir se o estado `Empty` deve ter semântica e regra de emissão ou se deve ser removido de futuras versões do contrato.
3. Clarificar a política de falha quando o resolver consegue projetar `Unavailable` por bytes/hash/tamanho em falta, mas o exporter falha ao tentar incluir o `DefinitionArtifactId` original. O comportamento atual é fail-closed para artefactos referenciados com bytes inválidos.
4. Obter o workspace real original ou um ZIP demonstravelmente produzido pelo exporter atual para fechar o round-trip real e executar o validador Python sobre esse pacote.

### Conclusão desta etapa

O código auditado declara 23 operações, mas a classificação-alvo aprovada passa a 21 após retirar REL/DTL do catálogo de capacidades do Scanner. O mapeamento dos 21 kinds do Object Index tem testes sintéticos; essa contagem de kinds é independente da contagem de operações. A cobertura por operação distingue resultados vazios, ausentes, parciais e falhados. Contudo, não existe evidência de execução real e completa das 23 operações na mesma captura nem de validação end-to-end atual de um ZIP real. As duas primeiras lacunas acima (FTU condicionado por `dbo.ftu` e exceção em `sourceExistsAsync`) merecem correção/decisão antes de considerar a cobertura robusta. Esta auditoria não altera collectors nem o contrato IKP.
