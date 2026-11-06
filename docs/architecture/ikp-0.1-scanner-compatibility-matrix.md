# Matriz de compatibilidade — IKP 0.1 e Scanner atual

Data: 2026-11-06  
Estado: análise de código concluída; nenhuma alteração de código.

## Conclusão

O Scanner já dispõe de recolha SQL, workspace SQLite, inventário de objetos, artefactos com hash, relações PHC, interpretação REL/DTL e exportação do Database Schema Snapshot. A lacuna principal é a ausência de um modelo unificado e exportável que ligue capacidades, cobertura por operação, objetos observados, identidade da origem, artefactos e proveniência.

A evolução deve aproveitar os componentes existentes, sem reescrever os collectors nem transformar o workspace num histórico.

## Matriz

| Componente | Estado atual confirmado | Lacuna IKP | Trabalho necessário |
|---|---|---|---|
| ScanManifest | Instalação/label, datas, modo, versão e resultados | Sem CaptureId e capabilities; identidade da origem confundida com InstallationId | Introduzir CaptureId e separar origem de instalação da plataforma |
| CollectorResult | Estado, artefactos, issues e duração por collector | Granularidade insuficiente | Acrescentar resultados por operação lógica |
| CollectionIssue | Severidade, código, mensagem, objeto e detalhe | Sem referência formal à operação | Ligar issue a operationId e âmbito |
| ScanEngine | Executa collectors e agrega resultados | Não distingue suporte, pedido, tentativa e conclusão por operação | Coordenar capabilities e OperationCoverage |
| ScanMode | Full, Selective, ProbeOnly | Ausência em scan seletivo pode parecer ausência real | Registar requested e não tratar ProbeOnly como recolha concluída |
| PhcCustomizationsCollector | Recolhe várias categorias PHC e persiste objetos/artefactos | Cobertura agregada; filtros de conteúdo; tabelas opcionais sem estado explícito | Separar inventário de definição e emitir cobertura por operação |
| SqlServerStructureCollector | Tabelas, colunas, PK/FK, views, procedures, triggers e outros elementos SQL | Cobertura por suboperações não formalizada; identidade nativa desigual | Declarar operações de schema e SourceIdentity estruturada |
| PhcDeclaredRelationsCollector | Factos REL/DTL e interpretações | Não há exportação IKP completa | Serializar factos e interpretações em payload/relations |
| RelationalKnowledgeProjector | Projeções de relações e FieldMapping em memória | Não exportadas | Export opcional como conhecimento derivado, com versão e evidências |
| TechnicalObjectRecord | ObjectId, Kind, Name, SchemaName, ParentObjectId, CollectorId, DefinitionArtifactId, SummaryJson, CollectedAtUtc | SourceIdentity e fingerprints sem campos dedicados | DTO explícito de identidade e fingerprints; manter SummaryJson como extensão |
| SqliteScanWorkspace | Persiste objetos, artefactos, issues, relações e interpretações | Representa uma captura e limpa dados anteriores | Manter o âmbito; produzir índice IKP a partir dos factos persistidos |
| WorkspaceExportService | Exporta artefactos de personalizações | Não transporta índice, cobertura nem relações completas | Reutilizar artefactos como fonte, não como pacote IKP |
| ZipScanExporter | ZIP de manifesto e artefactos retornados pelos collectors | Não é IKP; personalizações persistidas diretamente no workspace podem ficar fora deste export | Manter como export legado/diagnóstico; criar exportador IKP dedicado |
| DatabaseSchemaSnapshotWriter | Snapshot estruturado, hash estrutural e validação de privacidade | Sem envelope IKP, capabilities, coverage, índice global e checksums por ficheiro | Reutilizar os DTOs de schema dentro de payload/schema |

## Prioridades

### P0 — Captura e cobertura
1. CaptureId gerado no início da captura e PackageId novo por pacote.
2. Catálogo de capabilities por operationId e capabilityVersion.
3. Cobertura por operação: requested, result, sourceAvailability, contentAvailability, scope, observedCount e issueIds.
4. Issues ligadas às operações.
5. Distinguir scan seletivo, probe, falha parcial, estrutura ausente e zero resultados.

### P1 — Objetos e identidade
1. Gerar object-index.json a partir do workspace e do snapshot.
2. Separar SourceObjectId de SourceIdentity.
3. Expor object_id e column_id quando disponíveis, com âmbito.
4. Definir metadados relevantes por objectKind.
5. Calcular MetadataFingerprint e DefinitionFingerprint separadamente.
6. Representar Available, Empty, Unavailable e NotCollected por definição.
7. Excluir timestamps e caminhos temporários dos fingerprints.

### P2 — Exportação
1. Criar exportador IKP dedicado.
2. Integrar schema snapshot em payload/schema.
3. Exportar objetos e artefactos de personalizações.
4. Exportar REL/DTL/FK e interpretações.
5. Exportar projeções derivadas apenas quando existirem.
6. Gerar checksums por ficheiro e digest canónico lógico.
7. Validar referências e coerência antes de fechar o ZIP.

### P3 — Cobertura funcional PHC
Melhorar por etapas, recuperando os mapeamentos PHC já existentes:
- campos: ligar dbo.campos a sys.columns;
- BRULE/EBRULE: recuperar metadados do provider antigo e preservar conteúdo;
- UDEFS/EUDEFS: preservar registos e conteúdo literal/XBase/SQL;
- eventos/eeventos: preservar metadados e distinguir tabela declarada/inferida;
- teclas: preservar metadados TC/TI e registos sem retorno;
- avaliar formb, formc, tufo, PDU/Pdup, USQL/USQLV, IDU, relatórios e funções.

Não recolher dados operacionais dos clientes. O inventário de personalizações não deve excluir definições apenas por não conterem código executável.

## Identidade e proveniência

Os IDs atuais devem ser preservados inicialmente. Stamps PHC, tistamp, schema/nome e hashes alternativos têm estabilidade diferente. O IKP deve transportar SourceObjectId e SourceIdentity explícitos; a plataforma decide EntityIdentity e correspondências históricas.

O workspace continua a representar uma captura. O Scanner não mantém histórico de versões; cada IKP é imutável e o PHC Knowledge mantém histórico.

## Sequência recomendada

1. Modelos de Capture, Capability, OperationCoverage, SourceIdentity e ObjectIndex.
2. Instrumentação das operações e issues.
3. Índice estruturado e fingerprints.
4. Exportador IKP e integridade.
5. Exportação de relações e conhecimento derivado.
6. Fixtures e testes round-trip.
7. Melhorias dos collectors PHC.
8. Integração CLI/Desktop.

## Testes de aceitação

- Zero resultados vs estrutura ausente vs falha.
- Operação não suportada, não solicitada ou não iniciada.
- Inventário completo com definição indisponível.
- Captura parcial sem falsos Removed.
- Mesmo objeto em capturas sucessivas; definição alterada mantendo stamp.
- Renomeação SQL com object_id preservado e drop/recreate com object_id alterado.
- Colisões de nomes/casing e IDs de artefactos.
- Mesmo CaptureId em pacotes diferentes.
- Duas instalações com conteúdo igual sem fusão de histórico.
- Cópia/restauro com linhagem declarada.
- ZIP adulterado, entrada extra, ficheiro em falta e path traversal.
- Recompressão ZIP com digest lógico igual.
- Round-trip de REL/DTL, interpretações e artefactos.

## Limites

Matriz baseada na inspeção do código versionado no repositório PhcKnowledge, não numa recolha executada numa base PHC real. As alterações propostas ainda não foram implementadas.

## Ficheiros analisados

- PhcKnowledgeScanner/src/PhcKnowledgeScanner.Abstractions/Collectors/CollectorResult.cs
- PhcKnowledgeScanner/src/PhcKnowledgeScanner.Abstractions/Collectors/CollectionIssue.cs
- PhcKnowledgeScanner/src/PhcKnowledgeScanner.Abstractions/Manifest/ScanManifest.cs
- PhcKnowledgeScanner/src/PhcKnowledgeScanner.Engine/ScanEngine.cs
- PhcKnowledgeScanner/src/PhcKnowledgeScanner.Collectors/Phc/PhcCustomizationsCollector.cs
- PhcKnowledgeScanner/src/PhcKnowledgeScanner.Collectors/Sql/SqlServerStructureCollector.cs
- PhcKnowledgeScanner/src/PhcKnowledgeScanner.Collectors/Phc/PhcDeclaredRelationsCollector.cs
- PhcKnowledgeScanner/src/PhcKnowledgeScanner.Abstractions/Inventory/TechnicalObjectRecord.cs
- PhcKnowledgeScanner/src/PhcKnowledgeScanner.Workspace/SqliteScanWorkspace.cs
- PhcKnowledgeScanner/src/PhcKnowledgeScanner.Application/Services/WorkspaceExportService.cs
- PhcKnowledgeScanner/src/PhcKnowledgeScanner.Export/ZipScanExporter.cs
- PhcKnowledgeScanner/src/PhcKnowledgeScanner.Export/SchemaSnapshot/DatabaseSchemaSnapshotWriter.cs
