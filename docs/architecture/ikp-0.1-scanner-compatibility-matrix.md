> Fotografia histórica de 2026-11-17. API HTTP, Identity, Portal backend e frontend React evoluíram depois desta revisão. Consultar ../../PROJECT_STATUS.md para o estado atual. Esta matriz continua útil para compatibilidade Scanner/IKP, mas não é o estado global da plataforma.

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
