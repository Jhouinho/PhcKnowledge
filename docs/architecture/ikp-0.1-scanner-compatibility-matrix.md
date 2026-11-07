# Matriz de compatibilidade — IKP 0.1 e Scanner atual

Data: 2026-11-07  
Estado: P0 concluído; P1.B Object Index e fingerprints implementados e validados sinteticamente; exportador IKP ZIP ainda não existe.

Investigação de prontidão para exportação: [P1.B — IKP package export readiness](p1b-ikp-package-export-readiness-investigation-2026-11-07.md).

## Conclusão

O Scanner gera `CaptureId` por captura, declara capabilities e regista coverage por operação. O Object Index é agora projetado a partir do workspace, com SourceIdentity, fingerprints e estados individuais de definição. O build, 275 testes e validação do JSON produzido pelo C# contra o schema IKP 0.1 passaram no run `42419610023`. **Isto ainda não significa que o Scanner emita pacotes IKP**: falta o exportador ZIP e a integração dos cinco documentos obrigatórios.

## Matriz

| Componente | Estado atual confirmado | Lacuna IKP | Trabalho necessário |
|---|---|---|---|
| ScanManifest | `CaptureId`, catálogo, `OperationCoverages`, resultados, timestamps, modo e versão | Não é persistido integralmente no workspace | Exportador recebe manifesto concluído e valida-o contra o workspace; decidir persistência futura para export offline |
| CollectorResult | Estado, artefactos, issues, duração e `OperationCoverages` | — (P0) | Manter alinhamento com schema coverage |
| CollectionIssue | Severidade, código, mensagem, objeto, detalhe, `IssueId`, `OperationId` | — (P0) | Evitar credenciais nos diagnósticos (já) |
| ScanEngine | Gera/propaga CaptureId; agrega capabilities e coverage; Selective/ProbeOnly semânticos | — (P0) | Coordenar com exportador futuro |
| ScanMode | Full, Selective, ProbeOnly com coverage correta | — (P0) | — |
| PhcCustomizationsCollector | Coverage por operação (brule, ebrule, teclas, udefs, …) | Sem export IKP de artefactos | P2 export |
| SqlServerStructureCollector | Coverage e objetos SQL com SourceIdentity/fingerprints projetados | Captura real ainda não validada | Validar com captura demo e depois instalação personalizada |
| PhcDeclaredRelationsCollector | Coverage REL/DTL com NotPresent vs falha de leitura | Sem payload IKP relations | P2 |
| RelationalKnowledgeProjector | Projeções em memória | Não exportadas | P2 opcional |
| TechnicalObjectRecord | ObjectId/Kind/… no workspace; projeção IKP separa SourceObjectId, SourceIdentity e fingerprints | Não persiste identidade lógica da plataforma (fora do âmbito Scanner) | Manter o modelo de observação sem inventar EntityIdentity |
| SqliteScanWorkspace | Uma captura; `capture_id`, capabilities/coverage JSON, objetos, artefactos e issues; reader read-only | Não persiste timestamps reais, modo, ScannerVersion nem origin descriptor | Exportação inicial recebe ScanManifest em memória; não inferir estes valores a partir de `created_at_utc` ou label |
| WorkspaceExportService / ZipScanExporter | Export legado | Não é IKP | Exportador IKP dedicado |
| DatabaseSchemaSnapshotWriter | Snapshot estruturado | Sem envelope IKP | Reutilizar em payload/schema |

## Prioridades

### P0 — Captura e cobertura — **feito**
1. CaptureId UUID no início da captura (≠ InstallationId, scanId, PackageId).
2. Catálogo `DeclaredCapability` / `ScannerCapabilityCatalog` independente da execução; conflitos de `operationId` incompatíveis são rejeitados; duplicados idênticos colapsam na primeira ocorrência.
3. `OperationCoverage` com result / sourceAvailability / contentAvailability / observedCount / issueIds; `SucceededWithWarnings` só com issues `Warning` (não por meros IssueIds/`Info`).
4. Issues com `IssueId` e `OperationId`; falha não tratada do collector fica sem `OperationId`. Coverages já publicadas em `CollectorExecutionProgress` são preservadas; só as operações sem resultado conclusivo ficam `NotAttempted`.
5. Selective, ProbeOnly, empty, NotPresent, Failed, NotAttempted e preservação pós-exceção cobertos por testes.
6. Limite IKP: não há estado separado de “persistência falhou”; se a execução da operação já ficou registada e a finalização/persistência agregada falha, o `result` da operação mantém-se e a falha fica na issue `collector.unhandled`.

### P1 — Objetos e identidade — **implementado; validação sintética aprovada**
1. Object Index read-only a partir do workspace.
2. Separação SourceObjectId / SourceIdentity.
3. Fingerprints de metadata e definição.
4. Estados Available/Empty/Unavailable/NotCollected com evidência individual.
5. Mapping e schema IKP testados para os 21 kinds emitidos.

### P2 — Exportação — **por implementar**
1. Exportador IKP dedicado com os cinco documentos obrigatórios.
2. Checksums de todos os ficheiros e logicalContentDigest.
3. Payload inicial dos artefactos já persistidos no workspace.
4. Integrar schema snapshot, personalizações, REL/DTL e knowledge derivado apenas após mapear a sua proveniência.

### P3 — Cobertura funcional PHC
Melhorar recolha PHC (campos, BRULE, UDEFS, eventos, teclas, etc.) sem dados operacionais de clientes.

## Identidade e proveniência

`CaptureId` identifica a captura. `InstallationId` no manifesto permanece um rótulo de origem (`Servidor/Base`) e **não** é o identificador interno definitivo da plataforma. `PackageId` só existirá quando houver pacote IKP.

O workspace continua a representar uma captura. O Scanner não mantém histórico nem infere `Removed`.

## Sequência recomendada (restante)

1. **Concluído:** Object Index e fingerprints, validados com fixtures sintéticas.
2. **Próximo:** exportador IKP mínimo a partir de ScanManifest + workspace, sem alterar schema SQLite.
3. Validar ZIP extraído com `tools/ikp-validation/validate_ikp_fixture.py` e testes round-trip.
4. Integrar schema snapshot, personalizações, REL/DTL e conhecimento derivado.
5. Validar integração com captura PHC demo standard e depois personalizada.

## Testes de aceitação P0

Cobertos em `PhcKnowledgeScanner.Engine.Tests` (fakes): CaptureId válido/distinto/propagado; catálogo independente; unsupported vs not requested; Empty; NotPresent vs Failed; preservação com falha irmã; NotAttempted; Selective; ProbeOnly; issues↔operation; regressões Full/Selective/unavailable/ProbeOnly/exceção.

## Limites

Ainda não há emissão IKP ZIP. A projeção do Object Index não altera o schema SQLite. Workspaces antigos continuam a ser lidos em read-only; exportar um workspace sem ScanManifest completo exige uma decisão explícita de persistência, não inferências.
