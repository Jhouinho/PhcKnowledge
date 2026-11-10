# P1.B — Investigação dos emissores e preparação do object-index

| Campo | Valor |
|---|---|
| Data | 2026-11-06 |
| Estado | Investigação do código concluída; materialização dependente de workspace real |
| Fase | P1.B — emissores, identidade e metadata |
| Branch analisada | `main` |
| Âmbito | `PhcKnowledgeScanner`, catálogo P0, workspace SQLite e schema IKP 0.1 |
| Alterações funcionais | Nenhuma |

## 1. Conclusão executiva

O Scanner já declara operações lógicas estáveis e executa os emissores dentro dessas operações. É possível construir um mapeamento determinístico para os `ObjectKind` atualmente emitidos.

A limitação principal é que `TechnicalObjectRecord` não transporta `operationId`: guarda `CollectorId`, mas a operação fica apenas na coverage/issues. A ligação objeto → operação pode ser reconstruída hoje através dos pontos de emissão e do catálogo, mas não está persistida individualmente no registo.

O workspace SQLite guarda objetos, artefactos, capabilities, coverage e issues. Não guarda uma cópia autónoma do `object-index.json`. O índice terá de ser projetado a partir destes dados.

## 2. Mapeamento confirmado — ObjectKind → operação observadora

Mapeamento confirmado pela chamada do tracker que envolve o método emissor e pelos pontos onde são criados os `TechnicalObjectRecord`.

| objectKind | observedByOperationId | Collector | Origem efetiva |
|---|---|---|---|
| `database` | `sql.server.structure.database` | `sql.server.structure` | Metadados da base de dados |
| `table` | `sql.server.structure.tables.inventory` | `sql.server.structure` | `sys.tables` / `sys.schemas` |
| `primary_key` | `sql.server.structure.primary_keys` | `sql.server.structure` | Catálogo de constraints/índices e colunas |
| `foreign_key` | `sql.server.structure.foreign_keys` | `sql.server.structure` | Catálogo de foreign keys e colunas |
| `view` | `sql.server.structure.views` | `sql.server.structure` | `sys.views` / `sys.sql_modules` |
| `stored_procedure` | `sql.server.structure.procedures` | `sql.server.structure` | `sys.procedures` / `sys.sql_modules` |
| `trigger` | `sql.server.structure.triggers` | `sql.server.structure` | Triggers em tabelas `u_*` |
| `phc.user_rule` | `phc.customizations.brule` | `phc.customizations` | `dbo.brule` |
| `phc.web_user_rule` | `phc.customizations.ebrule` | `phc.customizations` | `dbo.ebrule` |
| `phc.hotkey` | `phc.customizations.teclas` | `phc.customizations` | `dbo.ti` + `dbo.tc` |
| `phc.default_value` | `phc.customizations.udefs` | `phc.customizations` | `dbo.udefs` |
| `phc.web_default_value` | `phc.customizations.eudefs` | `phc.customizations` | `dbo.eudefs` |
| `phc.event` | `phc.customizations.eventos` | `phc.customizations` | `dbo.eventos` |
| `phc.web_event` | `phc.customizations.eeventos` | `phc.customizations` | `dbo.eeventos` |
| `phc.user_field` | `phc.customizations.user_fields` | `phc.customizations` | Colunas físicas `u_*` em `sys.columns` |
| `phc.web_script` | `phc.customizations.escr` | `phc.customizations` | `dbo.escr` |
| `phc.user_javascript` | `phc.customizations.jsu` | `phc.customizations` | `dbo.jsu` |
| `phc.web_cssjs` | `phc.customizations.cssjs` | `phc.customizations` | `dbo.cssjs` |
| `phc.hidden_grid` | `phc.customizations.formc` | `phc.customizations` | `dbo.formc` |
| `phc.custom_screen` | `phc.customizations.formb` | `phc.customizations` | `dbo.formb` |
| `phc.user_tsql_function` | `phc.customizations.ftu` | `phc.customizations` | `dbo.ftu` e funções instaladas `Fx_*` / `User_*` |

São 21 tipos atualmente emitidos: 7 SQL e 14 PHC.

### Tipos declarados sem emissor encontrado

Os seguintes valores existem em `ObjectKind.cs`, mas não encontrei criação de `TechnicalObjectRecord` para eles nos collectors atuais:

- `schema`
- `column`
- `index`
- `phc.campo_definition`

Não devem ser materializados artificialmente no primeiro índice. Em particular, `phc.user_field` representa colunas físicas `u_*`; não representa as definições funcionais da tabela PHC `dbo.campos`.

## 3. Identificadores de origem já disponíveis

Os valores abaixo já são recolhidos e podem sustentar `SourceIdentity`, sem nova consulta à origem. A atribuição de scheme/scope é uma proposta técnica baseada na semântica observada; deve ser validada antes de serializar.

| Família | Evidência já recolhida | SourceIdentity candidato | Scope candidato |
|---|---|---|---|
| Personalizações PHC com stamp | `stamp` em `SummaryJson` | `phc.stamp` | Tabela PHC de origem (`dbo.brule`, `dbo.eventos`, etc.) |
| Tabela SQL | `objectId` em `SummaryJson` | `sqlserver.object_id` | Base de dados de origem |
| Coluna física `u_*` | `columnId` em `SummaryJson` | `sqlserver.column_id` | Base de dados + tabela/schema-pai |
| Função instalada `Fx_*` / `User_*` | Nome e schema; sem `object_id` | Nenhum confirmado | — |
| Database | `databaseId` em `SummaryJson` | Não atribuir ainda | Exigiria scope de instância SQL, não recolhido |
| PK/FK | Nome; PK também tem `uniqueIndexId` | Nenhum confirmado | `uniqueIndexId` não é tratado como identidade da constraint |
| View/procedure/trigger | Nome/schema; sem `object_id` | Nenhum recolhido | — |

Os stamps PHC são transportados dentro de `SummaryJson`, não como campo tipado. O mesmo acontece com os identificadores SQL. A extração do índice pode fazê-lo sem alterar o SQLite.

### Caso misto: `phc.user_tsql_function`

O mesmo `ObjectKind` é produzido por duas fontes dentro da operação `phc.customizations.ftu`:

1. Registos `dbo.ftu`, com `stamp`.
2. Funções instaladas em `sys.objects` (`Fx_*` / `User_*`), sem `object_id` no SELECT atual.

A operação observadora é inequívoca, mas a estratégia de SourceIdentity e a projeção de metadata têm de distinguir as duas variantes pelo campo `source` de `SummaryJson`.

## 4. REL/DTL não são atualmente objetos do inventário técnico

`dbo.rel` e `dbo.dtl` são persistidas como `DeclaredRelationRecord`, numa tabela SQLite própria (`declared_relations`), não como `TechnicalObjectRecord`.

- A operação observadora é, respetivamente, `phc.declared-relations.rel` e `phc.declared-relations.dtl`.
- O stamp é guardado em `SourceRecordId`.
- Sem stamp, `RelationId` é derivado de hash dos atributos da relação.
- A resolução dos extremos é preservada separadamente.

Não converter estas relações em objetos artificiais só para preencher `object-index.json`. Devem ser tratadas no payload de relações do IKP (etapa P2), mantendo a distinção entre relação declarada PHC, FK SQL e relação derivada.

## 5. Metadata já disponível e limites para fingerprints

O workspace guarda `SummaryJson` por objeto. Os campos variam por tipo e não existe ainda uma política canónica por `ObjectKind`.

### SQL

| objectKind | Metadata atual relevante | Exclusões/observações |
|---|---|---|
| `database` | `name`, `databaseId`, `versionProperty` | `databaseId` depende da instância; não usar automaticamente no fingerprint |
| `table` | `typeDesc`, `isMsShipped`, `classification`, `namePrefixU`, `createDate`, `modifyDate`, `objectId` | Datas são voláteis; `objectId` é candidato a SourceIdentity; prefix/classification são derivados |
| `primary_key` | `uniqueIndexId`, colunas e ordinais | Fingerprint deve refletir nomes/ordem das colunas; não assumir `uniqueIndexId` como identidade |
| `foreign_key` | schema/tabela referenciada, ações delete/update, colunas e ordinais | Projeção deve preservar ordem e extremos |
| `view` / `stored_procedure` | Sem SummaryJson | Sem metadataFingerprint até existir metadata semântica |
| `trigger` | `isDisabled` | A consulta limita-se a triggers em tabelas `u_*`; âmbito deve ser explícito |

### PHC

Os resumos já contêm, conforme o tipo, campos como `stamp`, `tabela`, `ecran`, `campo`, `teclas`, `qevento`, `codigo`, `tipo`, `nome`, `tag`, `tvalobj`, `defeito`, `inactivo`, `estado` e dados técnicos de coluna (`type`, `maxLength`, `precision`, `scale`, `isNullable`, `columnId`).

A projeção deverá:
- excluir `source` quando apenas identifica a variante fixa do tipo;
- excluir o stamp quando já estiver em `SourceIdentity`;
- excluir datas de recolha e campos derivados/duplicados;
- preservar valores PHC e nomes sem lower-case universal;
- tratar `phc.user_tsql_function` por variante `ftu` / `sys.objects`.

Não calcular ainda MetadataFingerprint para tipos sem uma projeção definida. A existência de SummaryJson não torna todos os seus campos semanticamente relevantes.

## 6. Definição e artefactos

O workspace separa `DefinitionArtifactId` do resumo, e a tabela `artifacts` já guarda SHA-256 e tamanho dos bytes. Os artefactos SQL de views/procedures/triggers contêm a definição obtida de `sys.sql_modules`, codificada em UTF-8.

Estados que o código permite sustentar:

| Situação observada | Estado candidato |
|---|---|
| View/procedure/trigger inventariado e definição SQL guardada | `Available` |
| View/procedure/trigger inventariado, mas `OBJECT_DEFINITION` indisponível e issue emitida | `Unavailable` |
| Database, table, PK/FK sem artefacto de definição recolhido | `NotCollected` |
| Campo físico `u_*` com artefacto textual gerado a partir de metadata | `NotCollected` para definição; o artefacto é evidência descritiva, não definição original |
| Personalização PHC com artefacto de exportação gerado | Em princípio `Available`, sujeito à decisão sobre o conteúdo exato a fingerprintar |

### Limite importante dos artefactos PHC

O collector constrói texto de exportação com cabeçalhos/metadados e conteúdo PHC, e guarda esse texto como artefacto UTF-8. O artefacto não é necessariamente igual ao campo original isolado na base de dados.

Antes de fixar DefinitionFingerprint, é necessário decidir se o fingerprint representa:
- os bytes exatos do artefacto exportado atualmente; ou
- apenas o conteúdo original da definição, extraído sem cabeçalhos gerados.

A primeira opção é possível sem recolha adicional, mas alterações a metadados incluídos no cabeçalho também alteram o hash da definição. A segunda exige uma representação/artefacto distinto ou extração inequívoca.

Não foi encontrada evidência suficiente para atribuir `Empty` a definições individuais: vários métodos filtram registos sem conteúdo antes de criar o objeto. Um resultado vazio da operação não prova que existiam objetos com definição vazia.

## 7. Limitações de cobertura que afetam a interpretação do índice

1. A operação `phc.customizations.user_fields` recolhe colunas físicas `u_*` de `sys.columns`; não recolhe as definições funcionais de `dbo.campos`.
2. A operação `phc.customizations.ftu` também consulta funções instaladas em `sys.objects`, mas o scope declarado por `RunOptionalAsync` é `dbo.ftu`. A cobertura não descreve integralmente a segunda fonte.
3. A operação `sql.server.structure.triggers` filtra triggers de tabelas `u_*`; não é inventário global de triggers.
4. Vários métodos PHC filtram linhas sem expressão/conteúdo antes de emitir objetos. O `observedCount` representa objetos emitidos pelo collector, não necessariamente todas as linhas existentes na tabela.
5. A maioria das operações SQL usa tracker sem scope explícito. A consulta real pode ser conhecida pelo código, mas não fica toda refletida no registo de coverage.
6. `TechnicalObjectRecord` não contém `operationId`; a relação objeto/operação é reconstruída por mapeamento estático.
7. O índice não deve usar a ausência de objetos sem verificar a coverage e os filtros efetivos da operação.

## 8. Consequências para o primeiro object-index

Com a informação do código, é possível definir o mapeamento e preparar um projetor sem alteração ao SQLite. Não é ainda possível produzir um índice real de uma captura sem ler um workspace efetivo.

A materialização exige:
- ficheiro SQLite do workspace da captura;
- diretório `artifacts` correspondente, para validar/reler os bytes referenciados;
- confirmação de que ambos pertencem à mesma captura.

O repositório contém código, schemas e samples de Schema Snapshot, mas não contém um workspace SQLite de captura PHC nem o respetivo diretório de artefactos. Não se deve fabricar um índice de exemplo e apresentá-lo como observação real.

## 9. Próximos passos

1. Obter um workspace real (SQLite + `artifacts`) e validar que CaptureId, coverage, objetos e artefactos são coerentes.
2. Produzir uma primeira matriz de normalização de metadata apenas para os tipos presentes nesse workspace.
3. Resolver a política de fingerprint dos artefactos PHC e a representação descritiva de `phc.user_field`.
4. Materializar um `object-index.json` de referência fora do SQLite e validar contra o schema IKP 0.1.
5. Só depois decidir se existe alguma lacuna irrecuperável que justifique alterar o modelo SQLite ou os collectors.

Nenhuma alteração funcional, de schema SQLite ou de contrato IKP foi efetuada nesta investigação.


### Estado da implementação do mapping — 2026-11-07

O mapping deixou de existir apenas em documentação: foi acrescentado `ObjectOperationResolver` em `PhcKnowledgeScanner.Export.ObjectIndex`, com cobertura unitária para os 7 kinds SQL, os 14 kinds PHC (incluindo as duas variantes de função T-SQL) e falhas de mapping/collector/JSON. O resolver não usa fallback por nome nem por prefixo.

Foi também acrescentado `SourceIdentityResolver`, que opera sobre o inventário completo da captura para resolver o scope SQL da base de dados e da tabela-pai. Não emite identidade para funções `sys.objects`, PK/FK, módulos SQL ou database quando a evidência atual não chega.

Os testes do mapping e da projeção de identidade passaram no GitHub Actions. O projector completo, a projeção de metadata/definition e a leitura tipada de artefactos continuam por implementar. Nenhuma alteração ao schema SQLite ou aos collectors foi feita.


### Estado complementar — projeção de definições (2026-11-07)

Foi implementado `DefinitionProjectionResolver` no projeto Export. A resolução é individual por objeto e segue estas regras:

- database, table, PK, FK e `phc.user_field`: `NotCollected`; o artefacto descritivo de user_field não é confundido com definição;
- objetos PHC com artefacto de definição e SQL view/procedure/trigger: `Available` apenas quando a referência, os bytes, o tamanho e o SHA-256 coincidem;
- SQL view/procedure/trigger sem artefacto: `Unavailable`, podendo associar uma issue específica apenas quando coincidem código, operationId e objectName;
- issues genéricas de operação não são propagadas como falhas de cada objeto;
- `Empty` não é inferido a partir de ausência de artefacto ou coverage.

O fingerprint de definição é SHA-256 dos bytes exatos do artefacto, sem normalizar cabeçalhos ou conteúdo. Testes desta lógica fazem parte dos 228 testes aprovados no GitHub Actions.

---

## 10. Verificação complementar — relações parent de objetos SQL (2026-11-09)

### Âmbito

Foi feita uma inspeção do código atual na branch `main`, incluindo os emissores SQL, a materialização de PK/FK, a classificação/identificação de tabelas, o projector e os testes existentes. Esta verificação não executou uma nova captura SQL nem alterou o workspace SQLite.

### Evidência no código

- `CollectViewsAsync` cria cada `TechnicalObjectRecord` com `SchemaName = schema` e `ParentObjectId = $"schema:{schema}"`.
- `CollectProceduresAsync` faz exatamente o mesmo para `StoredProcedure`.
- Não existe atualmente um emissor de objetos `ObjectKind.Schema` nem uma operação/capability de inventário de schemas. Embora `ObjectKind.Schema` esteja declarado, não é materializado no inventário atual.
- `SqlKeyInventory.CreatePrimaryKey` e `CreateForeignKey` atribuem como parent `TableInventoryClassification.BuildObjectId(schema, table)`, cujo formato é `table:{schema}.{table}`. Esse formato coincide com o ID emitido por `CollectAllTablesAsync`.
- `CollectTriggersAsync` atribui como parent a tabela no mesmo formato `table:{schema}.{table}`; a tabela é emitida pelo inventário de tabelas.
- Os campos físicos PHC `u_*` também usam o ID da tabela recolhida como parent.
- O projector só serializa `ParentSourceObjectId` quando encontra o ID indicado no conjunto de objetos da captura. Caso contrário, emite `object_index_parent_unresolved` e omite a referência do JSON. Este comportamento é intencional e já tem teste próprio.

### Evidência na captura real analisada

Na captura identificada por `CaptureId = a1f2fc33-7b0a-4c8f-b049-073a33ef7978`, o relatório de validação registou 36 diagnósticos `object_index_parent_unresolved`, associados a stored procedures cujo parent indicado é um schema não materializado. A captura analisada não continha Views; por isso, o caso das Views é uma conclusão do código, não uma ocorrência observada nessa captura.

Os logs de duas execuções do projector foram iguais byte a byte e o hash do `object-index.json` coincidiu com o valor do relatório. Isto sustenta a determinística e integridade dos bytes projetados nessa validação, mas não significa que os diagnósticos semânticos estejam resolvidos.

### Decisão

Não introduzir objetos Schema nesta fase. A presença de `SchemaName` preserva a informação do schema sem criar uma entidade artificial. Um parent só deve ser preenchido quando o objeto-pai correspondente também é materializado no inventário.

Assim:
- Views: manter `SchemaName`; definir `ParentObjectId = null`.
- Stored procedures: manter `SchemaName`; definir `ParentObjectId = null`.
- Não alterar relações de triggers, PK, FK ou campos `u_*`, porque apontam para tabelas materializadas.
- Não alterar o projector nem o contrato IKP 0.1.
- Não criar operação/capability de schemas nem modificar SQLite.

### Implementação e validação — 2026-11-09

A correção foi implementada em `SqlServerStructureCollector.cs`:

- `CollectViewsAsync`: mantém `SchemaName` e emite `ParentObjectId = null`.
- `CollectProceduresAsync`: mantém `SchemaName` e emite `ParentObjectId = null`.
- Não foram alterados triggers, PK, FK, campos PHC, projector, contrato IKP ou SQLite.

Foi acrescentado o teste parametrizado `Project_Does_Not_Create_Unmaterialized_Schema_Parent_For_Sql_Modules`, cobrindo View e StoredProcedure. Confirma projeção bem-sucedida, preservação de `schemaName`, ausência de `parentSourceObjectId` e ausência do diagnóstico `object_index_parent_unresolved`. Mantêm-se os testes gerais de parent resolvido e não resolvido.

### Estado da validação

O workflow GitHub Actions **141** (run `42679586394`) terminou com sucesso. O job `build-and-test` concluiu com sucesso as etapas Restore, Build, Test e validação do índice gerado contra o JSON Schema IKP 0.1.

A correção está, portanto, validada por build, testes automatizados e contrato. Continua pendente a validação de regressão com uma nova projeção do workspace SQLite e dos artefactos da captura real. Essa validação não deve ser simulada nem substituída por dados inventados.
