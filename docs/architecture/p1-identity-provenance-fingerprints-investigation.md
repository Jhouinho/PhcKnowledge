# P1 — Investigação: identidade, proveniência e fingerprints

| Campo | Valor |
|---|---|
| Data | 2026-11-06 |
| Estado | **Investigação histórica concluída (2026-11-06); decisões aplicadas posteriormente no contrato P1 e no Scanner** |
| Âmbito | `PhcKnowledgeScanner/` no monorepo PhcKnowledge |
| Relação | Complementa `observation-identity-and-lineage-contract.md`, `ikp-0.1-specification.md`, schemas `object-index` / `fingerprint`, matriz IKP |

## 1. Modelo atual e responsabilidades efetivas

### 1.1 Contratos centrais

| Tipo | Papel real hoje |
|---|---|
| `TechnicalObjectRecord` | Facto de inventário na captura: `ObjectId`, `Kind`, `Name`, `SchemaName`, `ParentObjectId`, `CollectorId`, `DefinitionArtifactId`, `SummaryJson`, `CollectedAtUtc`. **Uma linha = uma observação na captura atual do workspace**, não EntityIdentity nem ObservationId formal. |
| `ObjectKind` | Taxonomia de strings (`table`, `view`, `phc.user_rule`, …). Inclui `column`, `index`, `schema`, `phc.campo_definition` que **quase não são materializados** no inventário SQLite. |
| `CollectedArtifact` | DTO em memória (path, tipo, bytes). Persistência real em `SqliteScanWorkspace.SaveArtifactAsync`. |
| `DeclaredRelationRecord` | Facto Camada A (REL/DTL), tabela própria; `RelationId` + `SourceRecordId` (stamp) ou hash de conteúdo. |
| `DerivedRelationRecord` | Camada derivada (FK analyzer, etc.); `RelationId` tipo `rel:fk:…`. |
| `OperationCoverage` (P0) | Cobertura por operação lógica da captura; **não** identifica objetos. |
| `DatabaseSchemaSnapshot` | Export estrutural paralelo (mais rico que o inventário leve): tabelas+colunas, FK, índices, rotinas, `HasDefinitionAvailable`, manifesto com hashes. |
| `CaptureId` (P0) | UUID da captura no manifesto/workspace meta; **não** entra em `TechnicalObjectRecord.ObjectId`. |

### 1.2 O que o `ObjectId` atual realmente é

Na prática, `TechnicalObjectRecord.ObjectId` é um **SourceObjectId sintético do Scanner** (chave estável *dentro da captura* e tipicamente *entre capturas da mesma instalação se o nome/stamp não mudar*). Mistura:

- identidade por **nome qualificado** (SQL: `table:dbo.ft`, `view:dbo.v1`);
- identidade por **stamp PHC** (`phc.brule:{stamp}`, `phc.tecla:{tistamp}`);
- identidade por **conteúdo** quando falta stamp (`phc.rel:hash:{sha256(tori|tdes|tliga)}`).

**Não** materializa formalmente `SourceIdentity` (scheme/value/scope), `TechnicalObjectId`/`EntityIdentity` da plataforma, nem `ObservationId`. O schema IKP `object-index` já separa `sourceObjectId` + `sourceIdentity` opcional + fingerprints + `definition.status` + `operationId` — o Scanner ainda não emite esse índice.

### 1.3 Workspace

- `InitializeAsync` **apaga** objects/artifacts/issues/relations e reinsere `scan_meta` → **uma captura por ficheiro SQLite**, não histórico.
- `UpsertObjectAsync` / `ON CONFLICT(object_id)` → a chave é só `object_id`; a segunda escrita na mesma captura sobrescreve a primeira.
- `capture_id` / coverage JSON existem em `scan_meta` (P0); não há FK de objetos → captura além do meta.
- Artefactos: `artifact_id = "{collectorId}/{relativePath}"` + coluna `sha256` do conteúdo; path no disco sob `artifacts/`.

### 1.4 Dois canais de “estrutura SQL”

1. **Inventário workspace** (`SqlServerStructureCollector`): BD, tabelas (sem colunas), PK/FK, views/procedures/triggers (triggers só `u_%`), artefactos de definição quando disponíveis.
2. **Schema snapshot** (`DatabaseSchemaSnapshotExtractor` + Export): colunas, índices, FKs, funções, synonyms, classificação, `OriginFingerprint`, `SnapshotHash`, comparação por nome.

São modelos paralelos; a identidade do inventário e as chaves do snapshot (`Schema.Name`) coincidem semanticamente para tabelas, mas **não partilham um ObjectId canónico único** nem fingerprints por objeto.

---

## 2. Matriz de identidade por família de objetos

Legenda de estabilidade: **A** alta entre capturas da mesma BD se o registo/stamp permanece; **M** média (quebra com rename); **B** baixa (recreate / falta stamp); **C** contextual (só válido nesta BD/instalação).

| Família | ObjectId / chave atual (exemplo) | SourceObjectId nativo na origem? | Estabilidade | Rename muda ID? | Drop/recreate | Colisões / notas |
|---|---|---|---|---|---|---|
| Base de dados | `db:{DB_NAME()}` | Nome lógico; `database_id` só em `SummaryJson` | M/C | Sim (rename DB) | Novo nome → novo ID | Sem server no ObjectId; duas instâncias com mesmo nome DB colidem se fundidas fora de contexto |
| Schema | `schema:{name}` só como **ParentObjectId** de views/procs | `schema_id` não guardado | M | Sim | Sim | Objeto `Kind=schema` quase nunca persistido |
| Tabela | `table:{schema}.{name}` | `sys.tables.object_id` em `SummaryJson.objectId` | M + evidência C | **Sim** | object_id SQL muda; ObjectId Scanner **igual** se mesmo nome | object_id SQL ≠ ObjectId; casing: comparação ignore-case em vários sítios |
| Coluna (inventário) | *Não materializada* como `TechnicalObjectRecord` | Snapshot tem nome; query tem `column_id` | — | — | — | Lacuna face a IKP/object-index; só snapshot + campos `u_*` |
| Coluna `u_*` | `phc.user_field:{schema}.{table}.{col}` | `column_id` em summary | M + C | Sim | column_id muda; ID Scanner por nome | Prefixo `u_` filtrado; não cobre colunas standard |
| PK | `pk:{schema}.{table}.{pkName}` | Nome constraint + parent | M | Rename PK ou tabela → novo ID | Sim | Colunas da chave em summary; sem object_id SQL da constraint |
| FK | `fk:{schema}.{table}.{fkName}` | Idem | M | Idem | Sim | Relação derivada: `rel:fk:{fkObjectId}` |
| Índice | Snapshot: `{schema}.{table}.{indexName}` | Não no inventário workspace | M | Sim | Sim | Collector de estrutura **não** recolhe índices (comentário explícito) |
| View | `view:{schema}.{name}`; parent `schema:{s}` | object_id SQL **não** no summary | M | Sim | Sim | Definição → artefacto; ausência → issue + `DefinitionArtifactId=null` |
| Procedure | `procedure:{schema}.{name}` | Idem | M | Sim | Sim | Idem views |
| Trigger | `trigger:{schema}.{table}.{name}` | Idem; parent `table:…` | M | Rename tabela/trigger → novo ID | Sim | **Âmbito filtrado** a tabelas `u_%` no inventário (snapshot pode ser mais amplo) |
| Função T-SQL (snapshot) | `{schema}.{name}` no snapshot | — | M | Sim | Sim | Inventário: FTU PHC separado |
| BRULE / EBRULE | `phc.brule:{stamp}` / `phc.ebrule:{stamp}` | Stamp PHC (`brulestamp`) | **A** (se stamp estável) | Não (nome/tabela em meta) | Stamp novo → entidade nova | Stamp vazio/ausente: risco de skip ou ID frágil (verificar fila) |
| Eventos / eeventos | `phc.event:{stamp}` / `phc.web_event:{stamp}` | Stamp | A | Não | Stamp novo | |
| UDEFS / EUDEFS | `phc.udefs:{stamp}` / `phc.eudefs:{stamp}` | Stamp | A | Não | Stamp novo | |
| Teclas | `phc.tecla:{tistamp}` | `tistamp` | A | Não | Stamp novo | **Filtra** linhas sem `retorno` → não observados ≠ inexistentes |
| ESCR / JSU / CSSJS / FORMC / FORMB | `phc.web_script:…`, `phc.user_javascript:…`, etc. + stamp | Stamp | A | Não | Stamp novo | |
| FTU | `phc.user_tsql_function:{stamp}` e `…:installed:{sch}.{nome}` | Stamp **ou** schema.nome | A / M | Installed: sim | Mistura duas estratégias no mesmo Kind | Possível duplicar conceptualmente stamp vs installed |
| `dbo.campos` | Kind declarado; **sem collector** | — | — | — | — | Lacuna explícita (Desktop exclui da UI) |
| REL | `phc.rel:{relstamp}` ou `phc.rel:hash:{sha256}` | `relstamp` preferido | A / B | Hash muda se expressão/tabelas mudam | Stamp reutilizado raro; hash pode colidir semanticamente com outra linha “igual” | Hash **é** fingerprint usado como ID — confusão identidade/conteúdo |
| DTL | `phc.dtl:{dtlstamp}` ou hash | `dtlstamp` | A / B | Idem | Idem | Idem REL |
| Artefacto | `{collectorId}/{relativePath}` | Path sintético | M | Path inclui nome/stamp sanitizado | Mesmo path → upsert SHA | SHA256 = hash de conteúdo (definição), não identidade do objeto |
| Snapshot “origem” | `OriginFingerprint` 16 hex | Hash de `SERVER\|DB` | C | Rename server/DB → muda | N/A | **Não** é InstallationId; entra no `SnapshotHash` |

### Mapeamento conceptual (sem materializar ainda)

| Conceito IKP / contrato | Cobertura atual |
|---|---|
| SourceObjectId | ≈ `TechnicalObjectRecord.ObjectId` / `DeclaredRelationRecord.RelationId` / chaves de snapshot |
| SourceIdentity | Parcial e informal: stamps em ObjectId; `object_id`/`column_id` só em JSON; sem `scheme`/`scope` |
| TechnicalObjectId / EntityIdentity | **Só na plataforma** — Scanner não atribui |
| ObservationId | **Implícito**: (CaptureId + SourceObjectId) se CaptureId conhecido; **não** há coluna/DTO |
| Definition status | Implícito: artefacto presente / issue `definition_missing` / filtro teclas; snapshot tem `HasDefinitionAvailable` |
| operationId por objeto | **Ausente** no inventário (existe no schema IKP) |

---

## 3. Matriz de proveniência e evidência

| Pergunta | Resposta no modelo atual |
|---|---|
| De onde veio? | `CollectorId`; para SQL, ligação implícita à conexão da captura; `OriginFingerprint` só no snapshot |
| Em que captura? | `CaptureId` no manifesto/`scan_meta`; objetos **não** referenciam CaptureId (redundante enquanto workspace = 1 captura) |
| Quando observado? | `CollectedAtUtc` no objeto; manifesto `StartedAtUtc`/`FinishedAtUtc` |
| Evidência de identidade? | Stamp no ID; `SummaryJson` (objectId SQL, classification, …); REL raw metadata; **sem** bloco SourceIdentity |
| Recolhido vs inferido? | Inventário/declared = factos; `DerivedRelationRecord` + interpretações AST = derivados (`KnowledgeLayer`); projector RelationalKnowledge = projeção |
| Definição disponível? | `DefinitionArtifactId` null vs set; issues `*.definition_missing`; snapshot `HasDefinitionAvailable` + `IncludesSqlDefinitions` |
| Pai / âmbito? | `ParentObjectId` (table/schema); SchemaName; teclas usam SchemaName=ecran |
| Coverage da categoria? | `OperationCoverage` (P0) — necessário para `Removed`/`NotObserved` na plataforma |

**Lacunas de proveniência:** sem ligação objeto→`operationId`; sem distinguir formalmente “metadados só” vs “definição recolhida” no inventário (exceto artefacto null); Analysis pós-scan não marca `CollectorId` de origem nos derivados da mesma forma que factos.

---

## 4. Inventário de hashes existentes e semântica

| Hash | Onde | Entrada | Inclui origem? | Voláteis? | Determinístico? | Papel conceptual |
|---|---|---|---|---|---|---|
| Artefacto `sha256` | `SqliteScanWorkspace.SaveArtifactAsync` | Bytes gravados | Não | Não (só conteúdo) | Sim (bytes exactos) | **DefinitionHash** do ficheiro/artefacto |
| `StableHash` REL/DTL | `PhcDeclaredRelationsCollector` | `tori\|tdes\|tliga` ou from/to/expr UTF-8 | Não | Não | Sim | Usado como **substituto de ID** quando falta stamp — **não** deve ser tratado como EntityIdentity |
| `OriginFingerprint` | Snapshot extractor | `phc-schema-origin\|SERVER\|DB` → SHA256 truncado 16 hex | **Sim** (server+db) | Rename server/db | Sim para o mesmo par | Fingerprint de **âmbito de origem**, não de estrutura |
| `SnapshotHash` | `SchemaSnapshotHasher.Compute` | JSON canónico: schemaVersion, IncludesSqlDefinitions, **OriginFingerprint**, counts, classification, Tables (com create/modify dates), Relationships, Indexes, Views/Triggers/Routines (defs só se IncludesSqlDefinitions), UserTypes, Synonyms; **exclui** SnapshotHash e CollectedAtUtc do manifesto | **Sim** (via OriginFingerprint) | **Sim** (create/modify dates nas tabelas) | Sim para o mesmo payload | Mistura **SnapshotHash** + traços de origem + datas → **não** é StructuralFingerprint puro nem DefinitionHash por objeto |
| (ausente) MetadataFingerprint por objeto | — | — | — | — | — | Previsto no IKP; não calculado |
| (ausente) DefinitionFingerprint por objeto no índice | — | Poderia reutilizar sha256 do artefacto | — | — | — | Schema IKP exige quando `definition.status=Available` |

### Opção `IncludesSqlDefinitions`

- Flag entra no payload do `SnapshotHash` → dois exports da mesma BD com/sem definições têm hashes **diferentes** (significado correcto: o snapshot não é o mesmo pacote de conteúdo).
- Com `false`, defs são null no hash mas `HasDefinitionAvailable` permanece → distingue “não incluído no pacote” vs “indisponível na origem” **no snapshot**; no inventário workspace a ausência de artefacto não distingue Empty vs Unavailable vs NotCollected.

### Regra confirmada

Hash de conteúdo ≠ identidade. O caso REL/DTL `hash:` é a principal violação prática desta regra no código atual.

---

## 5. Limitações da comparação entre capturas

### O que já existe

`DatabaseSchemaSnapshotComparer`: diff por chaves `schema.name` (tabelas, colunas, FK, índices, view/proc/func/trigger). Produz added/removed/property changes. **Não** usa object_id SQL, stamps, coverage nem fingerprints por objeto. **Não** propõe renomeações.

### O que o workspace não permite

- Guardar duas capturas no mesmo SQLite.
- Histórico de um ObjectId ao longo do tempo.
- Comparar inventário PHC (brule, teclas, …) entre capturas sem export externo / plataforma.

### Distinções conceptuais vs suporte atual

| Estado desejado | Suporte atual |
|---|---|
| Novo | Comparer: chave só à direita; plataforma precisará de coverage da categoria na captura esquerda |
| Removido | Comparer: chave só à esquerda — **sem** gate de coverage (risco de falso Removed se operação falhou/selecionada) |
| Alterado | Colunas: props tipadas; defs SQL: **não** comparadas pelo Comparer (só presença de objeto) |
| Sem alteração | Implícito (sem entrada no diff) |
| Identidade incerta | **Não** modelado |
| Possível renomeação | **Não** inferida (correcto face à regra); também sem candidatos |
| Definição indisponível | Snapshot: flag; inventário: issue; comparer **não** distingue “igual mas sem def” |

**Regra:** Scanner/IKP não emitem `Removed`. Remoção é conclusão da plataforma com coverage adequada — alinhado com P0 e contrato de observação.

---

## 6. Proposta de modelo conceptual P1 (sem implementar)

Manter a separação Scanner → IKP → plataforma já acordada.

### 6.1 Por observação de objeto (futuro object-index)

Reutilizar campos existentes sempre que possível:

| Campo conceptual | Origem proposta (reutilização) |
|---|---|
| `sourceObjectId` | Continuar `TechnicalObjectRecord.ObjectId` / RelationId (documentar por família) |
| `sourceIdentity` | Extrair evidências já recolhidas: stamp → `{scheme:phc-stamp,value,scope:table}`; SQL → `{scheme:sql-server-object-id,value,scope:database}` **sem** promover a EntityIdentity |
| `objectKind` | `Kind` |
| `parentSourceObjectId` | `ParentObjectId` |
| `metadata` | `SummaryJson` + Name/SchemaName normalizados |
| `definition.status` | Derivado: artefacto? → Available; issue missing → Unavailable; nunca recolhido pela operação → NotCollected; conteúdo vazio legítimo → Empty |
| `definition.artifactIds` | `DefinitionArtifactId` |
| `definitionFingerprint` | Reutilizar `artifacts.sha256` (normalizado lowercase 64 hex) quando Available |
| `metadataFingerprint` | **Novo cálculo** canónico por Kind (excluir CollectedAtUtc, paths, object_id volátil opcional/configurável) |
| `operationId` | Mapear Kind/collector → operação P0 conhecida |
| ObservationId | **Não** precisa de coluna no Scanner: `CaptureId` + `sourceObjectId` (+ kind se necessário) |

### 6.2 Fingerprints

- **DefinitionFingerprint** = hash do artefacto original (já existe).
- **MetadataFingerprint** = hash de metadados normalizados **sem** origem de instalação e sem timestamps de recolha; decidir explicitamente se `create_date`/`modify_date` SQL entram (hoje entram no SnapshotHash — recomendação P1: **excluir** de MetadataFingerprint de objeto; manter como metadados observados).
- **SnapshotHash** = manter para o pacote schema snapshot; documentar que inclui OriginFingerprint e datas → não usar como StructuralFingerprint cross-installation sem normalizar.
- **StructuralFingerprint** (opcional, fase posterior): variante do snapshot **sem** OriginFingerprint nem datas, para comparar “mesma estrutura” entre cópias.

### 6.3 Workspace

- Continuar **uma captura por workspace** no Scanner.
- Histórico e ObservationId materializado → **PHC Knowledge** após import IKP.
- Não migrar SQLite para multi-captura nesta fase.

### 6.4 Comparação

- Continuar comparer baseado em SourceObjectId/nome como **candidato factual** de added/removed/changed.
- Plataforma aplica coverage + capabilities antes de `Removed`/`Added` fortes.
- Renomeação: apenas candidatos com evidência (stamp estável + nome mudou; ou object_id SQL no mesmo âmbito + nome mudou) — **nunca** só por DefinitionFingerprint igual.

---

## 7. Alterações mínimas necessárias (prioridade) — só após aprovação

### P1.A — Contratos / documentação de identidade (Scanner, sem quebrar APIs)

1. Documentar por família o significado de `ObjectId` e evidências em `SummaryJson` (matriz §2).
2. Definir função pura (spec) `definition.status` a partir de artefacto + issues + coverage da operação.
3. Definir mapeamento Kind → `operationId` P0.
4. Especificar payload canónico de `metadataFingerprint` por Kind (lista de campos incluídos/excluídos).

### P1.B — Materialização no export IKP (ainda não implementar agora)

1. Gerar `object-index.json` a partir do workspace + artefactos (sem novas colunas obrigatórias se summary/artefactos chegarem).
2. Anexar `sourceIdentity` opcional a partir de summary/stamp.
3. Expor `definitionFingerprint` = sha256 do artefacto.
4. Ligar cada entrada a `operationId` e `captureId`.

### P1.C — Lacunas de recolha (só se o índice as exigir)

1. Colunas SQL standard no inventário **ou** importar só do snapshot para o índice (preferir reutilizar snapshot para não duplicar massa).
2. Índices: já no snapshot.
3. `dbo.campos` / `PhcCampoDefinition`: continua fora; não recolher “por rotina” até haver operação/capability explícita.
4. Teclas sem retorno: decidir se entram no índice com `definition.Empty`/`NotCollected` (hoje omitidas).

### P1.D — Hashes

1. Não alterar `SchemaSnapshotHasher` sem decisão sobre OriginFingerprint/datas (breaking para hashes existentes).
2. Separar spec de StructuralFingerprint (opcional) vs SnapshotHash atual.
3. Parar de tratar `phc.rel:hash:` / `phc.dtl:hash:` como identidade forte na plataforma (marcar `sourceIdentity` fraca / confidence).

### P1.E — Comparação

1. Gate de coverage no comparador da **plataforma** (não no Scanner).
2. Estender comparer de snapshot para defs opcionalmente via DefinitionFingerprint — fase posterior.
3. Candidatos a rename — só plataforma, com evidências.

**Não nesta fase:** novas tabelas SQLite de histórico; InstallationId interno no Scanner; novos estados IKP; exportador IKP completo.

---

## 8. Riscos e decisões em aberto

1. **`ObjectId` por nome vs object_id SQL:** manter nome como SourceObjectId sintético e object_id só em SourceIdentity? (Recomendação: sim.)
2. **Incluir create/modify dates no MetadataFingerprint?** (Recomendação: não.)
3. **`SnapshotHash` com OriginFingerprint:** aceitar como hash de pacote ligado à origem, ou versionar hasher para variante estrutural? (Breaking.)
4. **REL/DTL sem stamp:** manter hash-as-id no Scanner ou gerar ID opaco + fingerprint separado? (Recomendação futura: separar.)
5. **FTU stamp vs installed:** uma ou duas famílias de SourceObjectId?
6. **Triggers só `u_%` no inventário vs snapshot completo:** coverage/operation deve declarar o âmbito; senão falso Removed de triggers standard.
7. **Teclas sem retorno omitidas:** NotCollected vs Empty vs fora do índice?
8. **Unificar inventário e snapshot** num único object-index ou dois artefactos IKP (`payload/schema` + índice)? (IKP já prevê schema + object-index.)
9. **Normalização de casing** SQL (`dbo.FT` vs `dbo.ft`) nos IDs — política OrdinalIgnoreCase vs preservar casing da origem no SourceObjectId.
10. **ObservationId explícito no IKP:** necessário no pacote ou derivado na importação? (Schema object-index atual não exige ObservationId — pode ficar na plataforma.)

---

## Exemplos concretos (código atual)

```text
Tabela:     ObjectId = "table:dbo.ft"
            SummaryJson.objectId = 123456789  (sys.tables.object_id)
            ParentObjectId = null

PK:         ObjectId = "pk:dbo.ft.PK_ft"
            ParentObjectId = "table:dbo.ft"

View:       ObjectId = "view:dbo.vCl"
            DefinitionArtifactId = "sql.server.structure/views/dbo.vCl.sql"
            artefacts.sha256 = <definição T-SQL>

BRULE:      ObjectId = "phc.brule:ABC123STAMP…"
            DefinitionArtifactId = "phc.customizations/phc/brule/….prg"

Tecla:      ObjectId = "phc.tecla:{tistamp}"
            (linhas sem retorno não criam registo)

REL:        RelationId = "phc.rel:{relstamp}"
         ou "phc.rel:hash:{sha256(tori|tdes|tliga)}"

Artefacto:  artifact_id = "phc.customizations/phc/brule/ABC….prg"
            sha256 = hash dos bytes gravados

Captura:    CaptureId = UUID no ScanManifest / scan_meta
            Workspace.InitializeAsync limpa objects → sem histórico local
```

---

## Conclusão

O Scanner já tem **chaves de observação por captura**, **artefactos com SHA-256**, **stamps PHC**, **object_id SQL pontual em JSON**, **coverage P0** e um **comparer de snapshot por nome**. Na data da investigação faltava a camada explícita SourceIdentity / fingerprints por objeto / object-index / operationId por objeto. Estas capacidades foram implementadas posteriormente no projector/exportador IKP. O workspace continua a não ser repositório histórico.

A evolução mínima é **mapear e enriquecer na serialização IKP** o que já está no workspace e no snapshot, calculando MetadataFingerprint e DefinitionFingerprint sem redesign do SQLite — e deixar EntityIdentity, ObservationId persistente, Removed e rename candidates para a plataforma.

**Atualização 2026-11-10:** a evolução descrita foi aplicada no contrato P1 e no exportador IKP. A validação real cobriu 8/21 kinds da captura default; não demonstra cobertura universal nem identidade estável entre capturas. Consultar `p1-identity-provenance-contract.md` e `ikp-0.1-scanner-compatibility-matrix.md` para o estado atual.
