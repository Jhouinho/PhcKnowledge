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

## Auditoria operacional do Scanner — 2026-11-20

### Catálogo e execução declarada

O código atual declara 23 operações em `KnownOperations` e `ScannerCapabilityCatalog`: 7 de estrutura SQL Server, 14 de personalizações PHC e 2 de relações declaradas. A declaração é independente do resultado de uma captura. O resultado efetivo é registado por operação em `coverage.json`; uma capability declarada não prova que a origem exista, que a operação tenha sido executada ou que tenha produzido conteúdo.

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
- A captura real documentada `CSDefault202603` contém 16.618 objetos, mas apenas 8 dos 21 kinds. Não generalizar a validação real aos restantes 13 kinds.
- `IkpPackageExporterTests` valida a estrutura ZIP, os cinco ficheiros de contrato, checksums, referências a artefactos, diagnósticos e REL/DTL com workspace sintético. Não substitui um teste end-to-end do ZIP produzido a partir de uma captura real atual.
- `ScanEngineTests` cobre semântica de coverage (vazio, ausente, falha, parcial, não tentado, skipped e ProbeOnly). Não foi encontrada uma suite de integração SQL específica que execute os três collectors contra uma base PHC real nesta branch.

### Lacunas e riscos encontrados no código

1. **FTU e funções instaladas:** `CollectFtuAsync` recolhe primeiro `dbo.ftu` e depois pesquisa funções `Fx_*`/`User_*` em `sys.objects`. Porém, `RunOptionalAsync` só chama o método quando `dbo.ftu` existe. Se `dbo.ftu` estiver ausente, as funções instaladas independentes não são pesquisadas e toda a operação é marcada `NotPresent`. A origem `sys.objects` não está isolada como operação/capability própria.
2. **Falha na verificação de existência opcional:** `OperationCoverageTracker.RunOptionalAsync` executa `sourceExistsAsync()` fora do bloco `try/catch` de `RunAsync`. Uma exceção na consulta de existência (por exemplo, falha transitória/permissão) escapa ao tratamento por operação; a cobertura dessa operação e das seguintes depende do tratamento externo do ScanEngine. Não há teste específico do tracker para este cenário.
3. **Probe agregado de personalizações:** `PhcCustomizationsCollector.ProbeAsync` considera o collector disponível quando encontra pelo menos uma de `brule`, `eventos` ou `udefs`; depois, as operações não opcionais são executadas individualmente sem probe de existência. A falta de uma dessas tabelas core pode ser registada como erro da operação, e não como `NotPresent`. Confirmar se este comportamento é intencional para todas as versões PHC suportadas.
4. **Cobertura de origem vs. conteúdo:** a coverage mede operações e contagens; a exportação reconcilia definições sem bytes e marca conteúdo parcial. A presença de um objeto no Object Index não garante que a sua definição de código esteja disponível. Consumidores Knowledge devem respeitar `definition.status`, `artifactIds`, issues e coverage, sem inferir completude pelo número de objetos.
5. **Validação real incompleta:** a evidência real documentada cobre 8/21 kinds e 49 definições associadas; o teste do exportador é sintético. Falta validar o ZIP completo gerado de uma captura real atual contra o validador independente, e verificar o conteúdo/contagens por cada uma das 23 operações.

### Conclusão desta etapa

O contrato de 23 operações está implementado e o mapeamento dos 21 kinds do Object Index tem testes sintéticos. A cobertura por operação distingue resultados vazios, ausentes, parciais e falhados. Contudo, não existe evidência de execução real e completa das 23 operações na mesma captura nem de validação end-to-end atual de um ZIP real. As duas primeiras lacunas acima (FTU condicionado por `dbo.ftu` e exceção em `sourceExistsAsync`) merecem correção/decisão antes de considerar a cobertura robusta. Esta auditoria não altera collectors nem o contrato IKP.
