# Diagramas do projeto — modelo ER e diagrama de classes

**Data:** 2026-11-06  
**Âmbito:** estado atual do código em `PhcKnowledgeScanner/`  
**Tipo:** documentação de arquitetura derivada da implementação existente

Este documento contém dois diagramas complementares:

1. **Modelo ER** — estrutura persistida no `workspace.db` SQLite.
2. **Diagrama de classes** — principais contratos e componentes de execução, desde os collectors até à aplicação e ao workspace.

> **Nota de leitura:** o modelo ER representa as relações lógicas entre registos. O schema SQLite atual não declara constraints `FOREIGN KEY` para estas ligações. As cardinalidades abaixo descrevem a semântica usada pelo código, não constraints garantidas pelo motor SQLite.

---

## 1. Diagrama entidade-relacionamento (ER)

O workspace representa uma captura/execução e agrega inventário técnico, artefactos, issues, relações derivadas e relações declaradas PHC. As interpretações das relações declaradas são persistidas separadamente dos factos originais.

```mermaid
erDiagram
    SCAN_META {
        TEXT scan_id PK
        TEXT installation_label
        TEXT status
        TEXT created_at_utc
        TEXT capture_id
        TEXT capabilities_json
        TEXT coverage_json
    }

    OBJECTS {
        TEXT object_id PK
        TEXT kind
        TEXT name
        TEXT schema_name
        TEXT parent_object_id FK
        TEXT collector_id
        TEXT definition_artifact_id FK
        TEXT summary_json
        TEXT collected_at_utc
    }

    ARTIFACTS {
        TEXT artifact_id PK
        TEXT collector_id
        TEXT relative_path
        TEXT content_type
        TEXT sha256
        INTEGER byte_length
        TEXT metadata_json
    }

    ISSUES {
        INTEGER id PK
        TEXT collector_id
        TEXT severity
        TEXT code
        TEXT message
        TEXT object_name
        TEXT detail
        TEXT created_at_utc
        TEXT issue_id
        TEXT operation_id
    }

    RELATIONS {
        TEXT relation_id PK
        TEXT relation_type
        TEXT from_object_id FK
        TEXT to_object_id FK
        TEXT evidence
        TEXT layer
        REAL confidence
        TEXT explanation
        TEXT validation_state
    }

    DECLARED_RELATIONS {
        TEXT relation_id PK
        TEXT source_type
        TEXT source_record_id
        TEXT from_table
        TEXT from_table_name
        TEXT to_table
        TEXT to_table_name
        TEXT original_expression
        TEXT created_by
        TEXT created_at
        TEXT modified_by
        TEXT modified_at
        INTEGER is_marked
        TEXT raw_metadata_json
        TEXT collected_at_utc
        TEXT from_object_id FK
        TEXT to_object_id FK
        INTEGER from_table_resolved
        INTEGER to_table_resolved
        TEXT correspondence_kind
        TEXT correspondence_group_id
    }

    DECLARED_RELATION_INTERPRETATIONS {
        TEXT relation_id PK,FK
        TEXT parser_version
        TEXT parse_status
        TEXT confidence
        TEXT ast_json
        TEXT diagnostics_json
        INTEGER field_ref_count
        INTEGER table_refs_resolved
        INTEGER table_refs_ambiguous
        INTEGER table_refs_unresolved
        TEXT interpreted_at_utc
    }

    SCAN_META ||--o{ OBJECTS : "workspace contém"
    OBJECTS o|--o{ OBJECTS : "parent_object_id"
    ARTIFACTS o|--o{ OBJECTS : "definition_artifact_id"
    OBJECTS ||--o{ RELATIONS : "from_object_id"
    OBJECTS ||--o{ RELATIONS : "to_object_id"
    OBJECTS o|--o{ DECLARED_RELATIONS : "from_object_id"
    OBJECTS o|--o{ DECLARED_RELATIONS : "to_object_id"
    DECLARED_RELATIONS ||--o| DECLARED_RELATION_INTERPRETATIONS : "relation_id"
```

### 1.1 Leitura das entidades

| Entidade | Responsabilidade |
|---|---|
| `scan_meta` | Metadados da execução/workspace. Guarda `scan_id`, `capture_id`, estado, catálogo de capabilities e coverage serializados em JSON. |
| `objects` | Inventário de objetos SQL e personalizações PHC. O `object_id` é a chave do registo dentro do workspace. |
| `artifacts` | Manifesto dos conteúdos recolhidos em ficheiros no diretório `artifacts/`; guarda hash SHA-256, tamanho e metadados. |
| `issues` | Avisos, erros e diagnósticos produzidos pelos collectors. `operation_id` e `issue_id` permitem relacionar o diagnóstico com a operação lógica. |
| `relations` | Relações derivadas entre objetos, com evidência, camada, confiança e estado de validação. |
| `declared_relations` | Factos de relações declaradas PHC provenientes de REL/DTL. Mantêm a expressão original e o estado de resolução dos extremos. |
| `declared_relation_interpretations` | Resultado da análise de uma expressão declarada: AST, estado, confiança, diagnósticos e métricas de referências. |

### 1.2 Relações e cuidados de interpretação

- Um objeto pode apontar para outro objeto como pai através de `parent_object_id` (relação recursiva).
- Um objeto pode referenciar o artefacto que contém a sua definição através de `definition_artifact_id`.
- Uma relação derivada liga dois objetos por `from_object_id` e `to_object_id`. O tipo e a evidência explicam a natureza dessa ligação.
- Uma relação declarada pode ter os extremos resolvidos para objetos do inventário; os IDs são opcionais porque a resolução pode ser ambígua ou inexistente.
- Uma interpretação referencia a relação declarada por `relation_id), mantendo o facto original independente do resultado do parser.
- `capabilities_json` e `coverage_json` são documentos JSON embutidos em `scan_meta`, não tabelas relacionais autónomas.
- `collector_id` identifica a origem operacional, mas não existe uma tabela `collectors` no workspace.
- `issues.collector_id` e `issues.operation_id` são referências informativas; não são foreign keys para tabelas de catálogo.

---

## 2. Diagrama de classes

O diagrama seguinte mostra o caminho principal de execução e consulta. Foram incluídos contratos e classes que estruturam o fluxo; não representa todos os collectors concretos, DTOs de interface ou classes auxiliares.

```mermaid
classDiagram
    direction LR

    class ICollector {
        <<interface>>
        +Id string
        +DisplayName string
        +DeclaredCapabilities IReadOnlyList
        +ProbeAsync(context) Task~CollectorCapability~
        +CollectAsync(context) Task~CollectorResult~
    }

    class ScanContext {
        +CaptureId string
        +InstallationId string
        +OutputRoot string
        +Mode ScanMode
        +SelectedCollectorIds IReadOnlySet
        +Properties IReadOnlyDictionary
        +SqlConnectionString string
        +Workspace IScanWorkspace
        +ExecutionProgress CollectorExecutionProgress
    }

    class ScanEngine {
        -_collectors IReadOnlyList~ICollector~
        +Collectors IReadOnlyList~ICollector~
        +BuildCapabilityCatalog() IReadOnlyList~DeclaredCapability~
        +RunAsync(context) Task~ScanManifest~
    }

    class CollectorResult {
        +CollectorId string
        +Status CollectorStatus
        +Artifacts IReadOnlyList~CollectedArtifact~
        +Issues IReadOnlyList~CollectionIssue~
        +Duration TimeSpan
        +OperationCoverages IReadOnlyList~OperationCoverage~
    }

    class ScanManifest {
        +CaptureId string
        +InstallationId string
        +StartedAtUtc DateTimeOffset
        +FinishedAtUtc DateTimeOffset
        +Mode ScanMode
        +ScannerVersion string
        +CapabilityCatalog IReadOnlyList~DeclaredCapability~
        +OperationCoverages IReadOnlyList~OperationCoverage~
        +Results IReadOnlyList~CollectorResult~
    }

    class IScanWorkspace {
        <<interface>>
        +RootPath string
        +ScanId string
        +CaptureId string
        +InitializeAsync()
        +SaveCaptureCoverageAsync()
        +UpsertObjectsAsync(records)
        +SaveArtifactAsync()
        +AddIssueAsync()
        +GetObjectsAsync()
        +GetObjectAsync(objectId)
        +ReadArtifactTextAsync(artifactId)
        +GetRelationsAsync()
        +GetDeclaredRelationsAsync()
        +GetDeclaredRelationInterpretationsAsync()
        +GetSummaryAsync()
    }

    class SqliteScanWorkspace {
        -_connection SqliteConnection
        +CreateAsync(rootPath)
        +OpenExistingAsync(rootPath, readOnly)
        +InitializeAsync()
        +SaveCaptureCoverageAsync()
        +UpsertObjectsAsync(records)
        +SaveArtifactAsync()
        +AddIssueAsync()
        +GetObjectsAsync()
        +GetObjectAsync(objectId)
        +ReadArtifactTextAsync(artifactId)
        +GetRelationsAsync()
        +GetDeclaredRelationsAsync()
        +GetSummaryAsync()
    }

    class TechnicalObjectRecord {
        +ObjectId string
        +Kind string
        +Name string
        +SchemaName string
        +ParentObjectId string
        +CollectorId string
        +DefinitionArtifactId string
        +SummaryJson string
        +CollectedAtUtc DateTimeOffset
    }

    class CollectedArtifact {
        +RelativePath string
        +ContentType string
        +Content ReadOnlyMemory~byte~
        +Metadata IReadOnlyDictionary
    }

    class CollectionIssue {
        +Severity CollectionIssueSeverity
        +Code string
        +Message string
        +ObjectName string
        +Detail string
        +IssueId string
        +OperationId string
    }

    class DeclaredCapability {
        +CategoryId string
        +OperationId string
        +CapabilityVersion string
        +CollectorId string
        +Supported bool
        +Description string
    }

    class OperationCoverage {
        +OperationId string
        +CapabilityVersion string
        +Requested bool
        +Result OperationExecutionResult
        +SourceAvailability SourceAvailability
        +ContentAvailability ContentAvailability
        +ObservedCount int
        +IssueIds IReadOnlyList
        +Scope IReadOnlyDictionary
    }

    class DerivedRelationRecord {
        +RelationId string
        +RelationType string
        +FromObjectId string
        +ToObjectId string
        +Evidence string
        +Layer KnowledgeLayer
        +Confidence double
        +Explanation string
        +ValidationState string
    }

    class DeclaredRelationRecord {
        +RelationId string
        +SourceType string
        +SourceRecordId string
        +FromTable string
        +ToTable string
        +OriginalExpression string
        +FromObjectId string
        +ToObjectId string
        +FromTableResolution TableReferenceResolution
        +ToTableResolution TableReferenceResolution
        +CorrespondenceKind DeclaredRelationCorrespondenceKind
    }

    class DeclaredRelationInterpretationRecord {
        +RelationId string
        +ParserVersion string
        +ParseStatus ExpressionParseStatus
        +Confidence InterpretationConfidence
        +Ast ExprAstNode
        +DiagnosticsJson string
        +FieldRefCount int
        +TableRefsResolved int
        +TableRefsAmbiguous int
        +TableRefsUnresolved int
    }

    class IScanAppService {
        <<interface>>
        +CurrentWorkspace IScanWorkspace
        +RunStructureScanAsync() Task~ScanManifest~
    }

    class InventoryAppService {
        -_scanAppService IScanAppService
        +GetSummaryAsync()
        +SearchObjectsAsync(kind, search)
        +GetObjectAsync(objectId)
        +GetArtifactTextAsync(artifactId)
        +GetRelationsAsync()
        +GetIssuesAsync()
    }

    class IInventoryAppService {
        <<interface>>
        +GetSummaryAsync()
        +SearchObjectsAsync(kind, search)
        +GetObjectAsync(objectId)
        +GetArtifactTextAsync(artifactId)
        +GetRelationsAsync()
        +GetIssuesAsync()
    }

    class IWorkspaceExportService {
        <<interface>>
        +ExportCurrentAsync(zipPath)
    }

    class WorkspaceExportService {
        -_scanAppService IScanAppService
        +ExportCurrentAsync(zipPath)
    }

    class IRelationalKnowledgeProjector {
        <<interface>>
        +ProjectAsync(workspace) Task~RelationalKnowledgeSnapshot~
        +Project(foreignKeys, declaredRelations, interpretations) RelationalKnowledgeSnapshot
    }

    class RelationalKnowledgeProjector {
        +ProjectAsync(workspace)
        +Project(foreignKeys, declaredRelations, interpretations)
    }

    class RelationalKnowledgeSnapshot {
        +Evidences IReadOnlyList
        +KnowledgeRelations IReadOnlyList
        +FieldMappings IReadOnlyList
        +GetEvidences()
        +GetKnowledgeRelations()
        +GetProvenance(knowledgeRelationId)
        +GetFieldMappings()
    }

    ICollector ..> ScanContext : receives
    ICollector ..> CollectorResult : produces
    ScanEngine o-- ICollector : orchestrates
    ScanEngine ..> ScanContext : executes with
    ScanEngine ..> ScanManifest : returns
    ScanManifest o-- CollectorResult : contains
    ScanManifest o-- DeclaredCapability : catalogs
    ScanManifest o-- OperationCoverage : reports
    CollectorResult o-- CollectedArtifact : contains
    CollectorResult o-- CollectionIssue : contains
    CollectorResult o-- OperationCoverage : reports
    ScanEngine ..> IScanWorkspace : persists results
    IScanWorkspace <|.. SqliteScanWorkspace : implementation
    IScanWorkspace ..> TechnicalObjectRecord : stores
    IScanWorkspace ..> DerivedRelationRecord : stores
    IScanWorkspace ..> DeclaredRelationRecord : stores
    IScanWorkspace ..> DeclaredRelationInterpretationRecord : stores
    InventoryAppService ..|> IInventoryAppService : implements
    InventoryAppService --> IScanAppService : uses
    IScanAppService --> IScanWorkspace : exposes active workspace
    WorkspaceExportService ..|> IWorkspaceExportService : implements
    WorkspaceExportService --> IScanAppService : uses
    IRelationalKnowledgeProjector <|.. RelationalKnowledgeProjector : implementation
    RelationalKnowledgeProjector ..> IScanWorkspace : reads
    RelationalKnowledgeProjector ..> RelationalKnowledgeSnapshot : creates
```

### 2.1 Responsabilidades por camada

| Camada | Componentes centrais | Responsabilidade |
|---|---|---|
| Abstractions | `ICollector`, `ScanContext`, `CollectorResult`, `IScanWorkspace`, records | Contratos partilhados e modelos de dados. |
| Collectors | Implementações de `ICollector` para SQL Server e PHC | Recolha read-only, emissão de objetos, artefactos, issues e coverage. |
| Engine | `ScanEngine` | Seleção, probe, execução dos collectors, agregação de resultados e manifesto. |
| Workspace | `SqliteScanWorkspace` | Persistência SQLite e ficheiros de artefactos; abertura de workspaces existentes, incluindo modo read-only. |
| Analysis | `RelationalKnowledgeProjector` e serviços de interpretação | Projeção de evidência relacional e interpretação de relações declaradas, sem confundir factos com inferências. |
| Application | serviços de scan, inventário, sessão e exportação | Orquestração de casos de uso consumidos pela interface. |
| Desktop / CLI | aplicações de entrada | Interfaces de utilização; não detalhadas neste diagrama de classes. |

### 2.2 Fluxo principal de execução

```mermaid
flowchart TD
    A["Desktop / CLI"] --> B["Serviço de aplicação"]
    B --> C["ScanEngine"]
    C --> D["Seleciona collectors"]
    D --> E["ProbeAsync"]
    E --> F["CollectAsync"]
    F --> G["CollectorResult"]
    G --> H["Persistência no IScanWorkspace"]
    H --> I["SqliteScanWorkspace"]
    I --> J[("workspace.db")]
    I --> K[("artifacts/")]
    G --> L["ScanManifest"]
    J --> M["Inventory / Analysis"]
    K --> M
    M --> N["Exploração / exportação / conhecimento relacional"]
```

---

## 3. O que estes diagramas não representam

- O **object-index IKP 0.1** ainda em desenho/validação. Não se deve interpretar `TechnicalObjectRecord` como se já contivesse `SourceIdentity`, `MetadataFingerprint`, `DefinitionStatus` ou `operationId` ao nível do objeto.
- A identidade lógica multi-captura e a linhagem da plataforma. O workspace atual representa uma captura e não mantém histórico de entidades entre instalações/capturas.
- As tabelas internas da base de dados PHC de origem. O ER é do workspace SQLite do Scanner, não do ERP PHC.
- Todas as classes concretas, componentes visuais, configurações de DI, DTOs ou detalhes internos dos collectors.
- Constraints relacionais impostas pela base de dados: as ligações assinaladas são referências lógicas inferidas dos campos e dos métodos de leitura/escrita atuais.

## 4. Fontes de implementação consultadas

- `PhcKnowledgeScanner/src/PhcKnowledgeScanner.Workspace/SqliteScanWorkspace.cs` — schema SQLite e persistência.
- `PhcKnowledgeScanner/src/PhcKnowledgeScanner.Abstractions/Workspace/IScanWorkspace.cs` — contrato do workspace.
- `PhcKnowledgeScanner/src/PhcKnowledgeScanner.Abstractions/Collectors/ICollector.cs`, `ScanContext.cs`, `CollectorResult.cs` — contrato de recolha.
- `PhcKnowledgeScanner/src/PhcKnowledgeScanner/Engine/ScanEngine.cs` — orquestração (o caminho exato no repositório é `PhcKnowledgeScanner/src/PhcKnowledgeScanner.Engine/ScanEngine.cs`).
- Records em `PhcKnowledgeScanner/src/PhcKnowledgeScanner.Abstractions/Inventory/`, `Coverage/` e `Manifest/`.
- Serviços em `PhcKnowledgeScanner/src/PhcKnowledgeScanner.Application/Services/`.
- `PhcKnowledgeScanner/src/PhcKnowledgeScanner.Analysis/Knowledge/RelationalKnowledgeProjector.cs` e respetivo contrato.

**Validação:** diagramas elaborados por inspeção estática do código atual. Não foram alterados código, schema SQLite, collectors ou dados de captura. Não foi executado build/teste nesta tarefa.


---

## 5. Extensão arquitetural — pipeline IKP e importação (2026-11-08)

A secção original descreve o modelo persistido do **workspace SQLite do Scanner** e as classes do Scanner. Não deve ser interpretada como modelo de dados da plataforma central. Esta extensão acrescenta a fronteira de transporte/importação já implementada e distingue-a explicitamente do staging ainda não construído.

### 5.1 Modelo lógico do pacote e validação

O pacote IKP é um ZIP imutável de transporte. Os cinco documentos de raiz e os artefactos de payload são conteúdo do pacote, não tabelas SQLite nem entidades já persistidas na plataforma.

```mermaid
flowchart LR
    subgraph SC["Scanner / workspace local"]
      W[("workspace.db + artifacts/")]
      X["IkpPackageExporter"]
      W --> X
    end
    X --> Z["ZIP IKP 0.1<br/>manifest + capabilities + coverage<br/>object-index + checksums + payload"]
    subgraph PL["Plataforma — projeto Import"]
      V["IkpPackageValidator"]
      R["ValidationResult"]
      D["PhcKnowledge.IkpDiagnostic<br/>CLI local"]
      V --> R
      D --> V
    end
    Z --> V
    R -. "futura etapa; ainda não implementada" .-> S["Staging persistente"]
    S -. "futura etapa" .-> DB[("PostgreSQL")]
```

### 5.2 ER conceptual da futura plataforma — não implementado

Este diagrama é uma vista de domínio para orientar a próxima fase. **Não representa tabelas existentes nem migrações implementadas.** O PR #2 apenas valida o pacote em memória; não cria registos persistentes.

```mermaid
erDiagram
    ORGANIZATION ||--o{ INSTALLATION : owns
    INSTALLATION ||--o{ CAPTURE : records
    CAPTURE ||--o{ PACKAGE : transported_as
    PACKAGE ||--|| VALIDATION_RUN : checked_by
    PACKAGE ||--o{ PACKAGE_ARTIFACT : contains
    CAPTURE ||--o{ OBJECT_OBSERVATION : observes
    OBJECT_IDENTITY ||--o{ OBJECT_OBSERVATION : groups
    OBJECT_OBSERVATION ||--o{ OBSERVATION_EVIDENCE : supported_by
    ORGANIZATION {
      uuid organization_id PK
    }
    INSTALLATION {
      uuid installation_id PK
      uuid organization_id FK
      string display_name
    }
    CAPTURE {
      uuid capture_id PK
      uuid installation_id FK
      datetime captured_at
    }
    PACKAGE {
      uuid package_id PK
      uuid capture_id FK
      string zip_sha256
      string logical_digest
    }
    VALIDATION_RUN {
      uuid validation_id PK
      uuid package_id FK
      string status
      json diagnostics
    }
    PACKAGE_ARTIFACT {
      uuid artifact_id PK
      uuid package_id FK
      string relative_path
      string sha256
      bigint byte_length
    }
    OBJECT_IDENTITY {
      uuid entity_identity_id PK
      uuid installation_id FK
      string identity_scope
    }
    OBJECT_OBSERVATION {
      uuid observation_id PK
      uuid capture_id FK
      uuid entity_identity_id FK
      string source_object_id
      string definition_fingerprint
      string metadata_fingerprint
    }
    OBSERVATION_EVIDENCE {
      uuid evidence_id PK
      uuid observation_id FK
      string evidence_type
      string source_reference
    }
```

As entidades acima são **proposta conceptual**, não decisão de schema aprovada. A sequência real de implementação continua: validar pacote real → definir staging → persistir captura/pacote → associação a instalação e identidade → observações e histórico. A associação a uma instalação não deve ser inferida apenas do nome da base de dados ou de um fingerprint de origem.

### 5.3 Diagrama de classes — importação atualmente implementada

```mermaid
classDiagram
    direction LR
    class IkpPackageValidator {
      +ValidateAsync(stream) ValidationResult
    }
    class ValidationResult {
      +IsValid bool
      +PackageId string
      +CaptureId string
      +Errors IReadOnlyList
      +Diagnostics IReadOnlyList
    }
    class IkpDiagnosticProgram {
      +Main(args) Task~int~
      +WriteJsonReport(path) Task
    }
    class ZipPackageReader {
      +ReadEntries(stream)
      +ValidateEntryPaths()
    }
    class JsonSchemaValidator {
      +Validate(document, schema)
    }
    class IntegrityValidator {
      +ValidateChecksums()
      +ValidateLogicalDigest()
    }
    class CrossReferenceValidator {
      +ValidateManifestAndFiles()
      +ValidateCapabilitiesAndCoverage()
      +ValidateObjectIndex()
      +ValidateRelations()
    }
    IkpDiagnosticProgram --> IkpPackageValidator : invokes
    IkpPackageValidator --> ZipPackageReader : reads in memory
    IkpPackageValidator --> JsonSchemaValidator : validates schemas
    IkpPackageValidator --> IntegrityValidator : verifies integrity
    IkpPackageValidator --> CrossReferenceValidator : verifies references
    IkpPackageValidator --> ValidationResult : returns
```

O diagrama representa responsabilidades lógicas do validador; não pretende enumerar todos os tipos internos nem afirmar que cada responsabilidade corresponde necessariamente a uma classe pública autónoma. A ferramenta de diagnóstico é um consumidor local do mesmo validador, não uma via de importação alternativa.

### 5.4 Limites atuais e invariantes

- O projeto `PhcKnowledge.Import` não referencia assemblies do Scanner.
- A validação ocorre em memória; não extrai o ZIP para o filesystem.
- Não existe ainda staging persistente, associação de instalação, ingestão PostgreSQL, identidade lógica multi-captura nem publicação de conhecimento.
- `PackageId` identifica o pacote físico; `CaptureId` identifica a captura. Não substituem `InstallationId`, que pertence à plataforma.
- `SourceObjectId` é uma referência ao objeto dentro da captura; não é identidade permanente.
- Factos REL/DTL e interpretações permanecem separados.
- A validação bem-sucedida prova conformidade estrutural e integridade do pacote; não prova que todos os collectors recolheram todos os dados possíveis da instalação.

