# Matriz de compatibilidade — IKP 0.1 e Scanner atual

Data: 2026-11-06  
Estado: P0 (CaptureId, capabilities, coverage) implementado no Scanner; exportador IKP ainda não existe.

## Conclusão

O Scanner gera `CaptureId` por captura, declara um catálogo de capabilities por `operationId` e regista `OperationCoverage` por operação lógica nos três collectors atuais. O manifesto interno e o workspace SQLite preservam estes dados. **Isto não significa que o Scanner emita pacotes IKP** — faltam object-index, fingerprints, linhagem e o exportador IKP.

## Matriz

| Componente | Estado atual confirmado | Lacuna IKP | Trabalho necessário |
|---|---|---|---|
| ScanManifest | `CaptureId`, catálogo, `OperationCoverages`, resultados | Sem envelope IKP exportável; `InstallationId` continua label de origem | PackageId no exportador; identidade de instalação na plataforma |
| CollectorResult | Estado, artefactos, issues, duração e `OperationCoverages` | — (P0) | Manter alinhamento com schema coverage |
| CollectionIssue | Severidade, código, mensagem, objeto, detalhe, `IssueId`, `OperationId` | — (P0) | Evitar credenciais nos diagnósticos (já) |
| ScanEngine | Gera/propaga CaptureId; agrega capabilities e coverage; Selective/ProbeOnly semânticos | — (P0) | Coordenar com exportador futuro |
| ScanMode | Full, Selective, ProbeOnly com coverage correta | — (P0) | — |
| PhcCustomizationsCollector | Coverage por operação (brule, ebrule, teclas, udefs, …) | Sem export IKP de artefactos | P2 export |
| SqlServerStructureCollector | Coverage (database, tables, PK/FK, views, procedures, triggers) | Sem SourceIdentity/fingerprints no índice | P1 |
| PhcDeclaredRelationsCollector | Coverage REL/DTL com NotPresent vs falha de leitura | Sem payload IKP relations | P2 |
| RelationalKnowledgeProjector | Projeções em memória | Não exportadas | P2 opcional |
| TechnicalObjectRecord | ObjectId/Kind/… no workspace | SourceIdentity e fingerprints | P1 |
| SqliteScanWorkspace | Uma captura; `capture_id`, capabilities/coverage JSON; issues com operation_id | Não é histórico de versões | Produzir índice IKP a partir dos factos |
| WorkspaceExportService / ZipScanExporter | Export legado | Não é IKP | Exportador IKP dedicado |
| DatabaseSchemaSnapshotWriter | Snapshot estruturado | Sem envelope IKP | Reutilizar em payload/schema |

## Prioridades

### P0 — Captura e cobertura — **feito**
1. CaptureId UUID no início da captura (≠ InstallationId, scanId, PackageId).
2. Catálogo `DeclaredCapability` / `ScannerCapabilityCatalog` independente da execução; conflitos de `operationId` incompatíveis são rejeitados; duplicados idênticos colapsam na primeira ocorrência.
3. `OperationCoverage` com result / sourceAvailability / contentAvailability / observedCount / issueIds; `SucceededWithWarnings` só com issues `Warning` (não por meros IssueIds/`Info`).
4. Issues com `IssueId` e `OperationId`; falha não tratada do collector fica sem `OperationId` e deixa operações como `NotAttempted`.
5. Selective, ProbeOnly, empty, NotPresent, Failed, NotAttempted cobertos por testes.

### P1 — Objetos e identidade
1. Gerar object-index.json a partir do workspace e do snapshot.
2. Separar SourceObjectId de SourceIdentity.
3. Fingerprints de metadados e definição.
4. Representar Available/Empty/Unavailable/NotCollected por definição.

### P2 — Exportação
1. Exportador IKP dedicado.
2. Integrar schema snapshot, personalizações, REL/DTL e checksums.

### P3 — Cobertura funcional PHC
Melhorar recolha PHC (campos, BRULE, UDEFS, eventos, teclas, etc.) sem dados operacionais de clientes.

## Identidade e proveniência

`CaptureId` identifica a captura. `InstallationId` no manifesto permanece um rótulo de origem (`Servidor/Base`) e **não** é o identificador interno definitivo da plataforma. `PackageId` só existirá quando houver pacote IKP.

O workspace continua a representar uma captura. O Scanner não mantém histórico nem infere `Removed`.

## Sequência recomendada (restante)

1. Índice estruturado e fingerprints.
2. Exportador IKP e integridade.
3. Exportação de relações e conhecimento derivado.
4. Fixtures e testes round-trip.
5. Melhorias dos collectors PHC.

## Testes de aceitação P0

Cobertos em `PhcKnowledgeScanner.Engine.Tests` (fakes): CaptureId válido/distinto/propagado; catálogo independente; unsupported vs not requested; Empty; NotPresent vs Failed; preservação com falha irmã; NotAttempted; Selective; ProbeOnly; issues↔operation; regressões Full/Selective/unavailable/ProbeOnly/exceção.

## Limites

Não há emissão IKP ZIP nesta etapa. Workspaces de validação antigos abrem em read-only sem exigir colunas novas.
