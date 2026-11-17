# Relatório de fase — Fecho dos bloqueadores de contrato e domínio do modelo 0.2

> Este relatório fecha os bloqueadores de contrato/domínio (PackageId/SHA-256, REL/DTL e SourceObjectId). Não significa que os bloqueadores técnicos de ingestão estejam fechados: SQL Server real, reconciliação arquivo/receipt e HTTP/autenticação permanecem pendentes.

| Campo | Valor |
|---|---|
| Data | 2026-11-17 |
| Âmbito | PackageId/SHA-256, REL/DTL e SourceObjectId; sincronização documental |
| Estado | Decisões consolidadas; documentação sincronizada em branch |
| Código / migrations | Não alterados nesta fase |

## 1. Resultado

Foram fechados três bloqueadores:
1. **Package e PackageReceipt:** Package é identidade global; PackageReceipt é receção por Organization. O ZIP é imutável e permanente, arquivado globalmente por SHA-256. A migration atual mantém constraints por Organization e não foi alterada.
2. **REL/DTL:** um ficheiro físico payload/relations/declared-relations.json, com duas coleções lógicas declaredRelations e interpretations. O Knowledge mantém duas entidades distintas. Não é necessário alterar a versão IKP 0.1.
3. **SourceObjectId:** medição real de 16.618 valores; mínimo 12, máximo 54; zero duplicados ordinais, espaços nas extremidades ou valores não-ASCII. Proposta de destino NVARCHAR(256), unicidade por CaptureId, comparação ordinal e validação de vazio/espaços.

## 2. Evidência da captura real

Captura de referência: CSDefault202603 / pacote IKP na Library PHC-Knowledge-Real-Capture.zip.

| Métrica | Resultado |
|---|---:|
| Object Index | 16.618 objetos |
| SourceIdentity | 8.307 |
| MetadataFingerprint | 16.581 |
| Definições associadas | 49 |
| Factos REL/DTL | 921 |
| Interpretações REL/DTL | 921 |
| REL / DTL | 295 / 626 |
| Interpretações órfãs | 0 |
| SourceObjectId máximo | 54 |
| SourceObjectId duplicados ordinais | 0 |
| SourceObjectId com espaços exteriores | 0 |
| SourceObjectId não-ASCII | 0 |

A captura contém 8 dos 21 kinds mapeados. Não generalizar esta evidência aos 13 kinds ausentes.

## 3. Baseline de implementação Import

Confirmado no main analisado:
- IkpPackageValidator;
- IkpPackageIngestionService;
- FileSystemImmutablePackageStore;
- SqlServerPackageReceiptRepository;
- migration 001_package_ingestion_receipts.sql;
- testes unitários e de integração sobre fixture IKP / ZIP sintético → Validator / Ingestion (aceitação do pacote válido, arquivo imutável idempotente, registo de receipt por organização, rejeição de path traversal e payload adulterado).

O fluxo dos testes usa fixture IKP / ZIP sintético → Validator / Ingestion e inclui adulteração de payload. Não demonstra execução SQL Server real, endpoint HTTP/autenticação nem receção end-to-end do ZIP real CSDefault202603 pelo serviço Import.

Ordem atual: arquivo ZIP antes da gravação SQL do receipt. Uma falha SQL posterior pode deixar um arquivo sem receipt. Reconciliação/retry permanece bloqueador técnico.

## 4. Estado por camada

| Área | Estado |
|---|---|
| Scanner / IKP 0.1 | Implementado; captura real examinada |
| Validator / serviço de ingestão | Implementado |
| Arquivo ZIP imutável | Implementado |
| Receipt SQL | Implementado por repository/migration; SQL Server real por validar |
| Capabilities, coverage, issues, artefactos | Transportados no ZIP; não materializados em tabelas próprias |
| REL/DTL | Payload real confirmado; entidades Knowledge futuras |
| Core Organization/Installation/Environment | Modelo de destino; implementação parcial limitada a OrganizationId no receipt |
| Observation / Identity / Context | Modelo de destino; não implementado |
| API HTTP / autenticação | Não demonstradas |
| RAG | Capacidade futura; não antecipar tecnologia |

## 5. Próximos passos

1. Testar repository e migration contra SQL Server real.
2. Definir reconciliação/retry para arquivo sem receipt.
3. Testar idempotência e conflitos globais PackageId/SHA, incluindo multi-organização.
4. Desenhar HTTP/autenticação; OrganizationId deriva do contexto autenticado.
5. Só depois avançar para materialização de artefactos, Observations, REL/DTL e associação de contexto.

## 6. Reflexão da fase

O projeto já ultrapassou o estado de contrato e desenho: existe uma primeira fatia funcional de ingestão no núcleo Import. Contudo, ainda não é uma plataforma utilizável por HTTP nem uma materialização do conhecimento. A documentação anterior tinha ficado desfasada e descrevia Receive / Validate / Archive como trabalho por iniciar; esta sincronização corrige esse estado sem exagerar a maturidade.

Não foram alterados código, schemas, migrations ou coletores. Não foram introduzidas entidades físicas futuras. A próxima fase deve concentrar-se na robustez da fronteira arquivo/SQL e na integração real, não em ampliar prematuramente o modelo.
