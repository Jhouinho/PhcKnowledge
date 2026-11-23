# Matriz de compatibilidade — IKP 0.1, Scanner e PHC Knowledge


> **Actualização de decisões — 21-11-2026:** D-015 e D-016 foram aprovadas. O Scanner mantém recolha REL/DTL; cada captura é completa dentro do âmbito declarado; o Knowledge compara e materializa incrementalmente. O ZIP é temporário e os dados/artefactos permanentes ficam no SQL Server. Ver [Decisões arquitecturais aprovadas](decisoes-aprovadas-2026-11-21.md). A matriz actual continua a ser fotografia de compatibilidade, não prova de cobertura integral.

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
3. Testar explicitamente idempotência e conflitos de PackageId/SHA, incluindo recepções concorrentes/idempotentes no contexto da organização única.
4. Desenhar endpoint HTTP e autenticação; o OrganizationId é o contexto global único da plataforma, enquanto o cliente e a instalação são escolhidos/confirmados no fluxo de importação.
5. Só depois avançar para materialização de artefactos, Observations, REL/DTL e associação de contexto.

Não gerar migrations globais novas nesta etapa. Ver [Modelo de domínio 0.2](knowledge-domain-model-0.2.md).


## Política aprovada de captura e materialização

- O Scanner não consulta o histórico central para seleccionar o que recolhe.
- A captura cobre integralmente as operações declaradas e reporta sucesso, falha, cobertura e avisos.
- REL/DTL continuam a ser recolhidos como factos declarados da instalação.
- O Knowledge compara com o catálogo standard e com o histórico da mesma instalação.
- Só uma operação bem-sucedida com cobertura completa pode sustentar uma conclusão de remoção.
- O ZIP é eliminado apenas depois da materialização SQL e verificação de integridade, referências, contagens e proveniência.


## Âmbito de organização — clarificação de 21-11-2026

A plataforma tem uma única organização interna. Testes de idempotência devem cobrir PackageId/SHA repetidos, concorrência, instalações distintas e capturas sucessivas, mas não devem introduzir tenancy ou isolamento multi-organização. OrganizationId existente no receipt é contexto global da plataforma.


## Decisão aprovada — D-017 (22-11-2026): cobertura e ausência segura

O contrato de cobertura foi aprovado para evolução, sem alteração da versão principal IKP 0.1:

- coverage schema v1 mantém leitura retrocompatível e sem garantias inferidas;
- coverage schema v2 acrescenta `sourceRowsRead`, `objectsEmitted`, `rowsFiltered`, `rowsFailed` e `sourceCompleteness` (`Complete`, `Partial`, `Unknown`);
- `observedCount=0`, `Empty` ou `Succeeded` em pacotes antigos não demonstram, isoladamente, origem vazia ou consulta completa;
- só a cobertura comprovadamente completa, com fonte visível, âmbito/filtros conhecidos e comparação compatível, pode sustentar `Removed`;
- contagens de linhas e objetos são métricas distintas e não têm de fechar aritmeticamente;
- os registos filtrados não podem ser convertidos automaticamente em objetos removidos.

**Estado de implementação:** decisão e especificação documental aprovadas; Scanner, exporter, validator e testes ainda não foram alterados nesta decisão. Os testes de aceitação definidos em D-017 continuam pendentes.


## Decisão aprovada — D-018 (23-11-2026): classificação e proveniência de artefactos

O contrato distingue três dimensões independentes: origem do conteúdo, natureza semântica e proveniência. As origens classificadas são `source-content`, `derived-projection`, `generated-description` e `generated-diagnostic`. A natureza semântica identifica código, configuração, estrutura SQL, metadados, descrição, relação ou diagnóstico.

A referência `definition.artifactIds` fica reservada a definições técnicas reais. Artefactos descritivos, estruturais e diagnósticos não são definições e devem ter associações próprias. Metadados disponíveis não implicam definição original disponível. Conteúdo original não pode ser substituído por conteúdo derivado; DefinitionFingerprint usa exclusivamente o conteúdo original relevante, enquanto projeções podem ter hashes próprios.

**Estado:** decisão e semântica documental aprovadas. Não foram autorizadas nem executadas alterações aos collectors, exporter, workspace SQLite, validator, schemas executáveis ou materialização SQL. A matriz por collector/ObjectKind e os testes de aceitação continuam necessários antes da implementação.
