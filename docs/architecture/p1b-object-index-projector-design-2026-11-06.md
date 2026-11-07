# P1.B — Desenho do projetor object-index
**Data:** 2026-11-06  
**Estado:** desenho técnico; sem implementação  
**Âmbito:** transformar um workspace Scanner existente em `object-index.json` IKP 0.1

## 1. Objetivo e limites

Definir uma transformação determinística, de leitura, entre o workspace SQLite + diretório de artefactos e o documento `object-index.json`.

O projetor não recolhe dados da origem, não altera o workspace, não inventa objetos e não decide que um objeto foi removido. Apenas serializa factos observados, com proveniência, identidade e estados de definição sustentados pelos dados existentes.

O primeiro protótipo deve ser executado sobre uma cópia de um workspace real. O SQLite original não deve ser modificado.

## 2. Entradas

O projetor precisa apenas dos elementos já previstos no workspace:

| Entrada | Utilização |
|---|---|
| `scan_meta.capture_id` | `captureId` do índice |
| `scan_meta.coverage_json` | interpretar o alcance e o resultado das operações |
| `objects` | fonte dos objetos, IDs, kinds, nomes, schema, parent, collector, artefacto e SummaryJson |
| `artifacts` | resolver ArtifactId, caminho relativo, content type, SHA-256, tamanho e metadata |
| `issues` | distinguir falhas de recolha/definição de ausência de tentativa |
| ficheiros em `artifacts/` | bytes efetivos para validar e calcular DefinitionFingerprint |

A tabela `declared_relations` fica fora do primeiro `object-index.json`; a sua projeção pertence ao payload de relações, segundo o contrato próprio.

## 3. Pipeline de projeção

A ordem proposta é deliberada: validar primeiro as referências e a proveniência; só depois calcular fingerprints e serializar.

1. **Abrir em modo read-only** o SQLite e validar a integridade estrutural básica.
2. **Ler CaptureId e coverage**, sem inferir sucesso global a partir de um contador.
3. **Carregar objetos e artefactos** para mapas de resolução por ID.
4. **Validar relações internas**: unicidade de SourceObjectId, existência de parents, existência de ArtifactIds e consistência de referências.
5. **Resolver ObjectKind + variante → operationId** através de uma matriz explícita e versionada.
6. **Projetar SourceIdentity** através da matriz P1.B, omitindo-a quando a evidência/scope não forem suficientes.
7. **Projetar metadata** através da política por ObjectKind; nunca serializar SummaryJson arbitrário diretamente.
8. **Resolver definition.status**, validar os bytes do artefacto e calcular DefinitionFingerprint quando permitido.
9. **Projetar parentSourceObjectId** apenas quando o parent existe e a referência é resolvida sem ambiguidade.
10. **Ordenar deterministicamente** os objetos e serializar JSON UTF-8.
11. **Validar o resultado** contra o schema IKP 0.1 e contra invariantes semânticas adicionais.

A saída é um artefacto novo. Nenhuma etapa escreve no SQLite ou altera os ficheiros de origem.

## 4. Mapeamento de operação

O campo `operationId` do objeto significa a operação que observou/inventariou o objeto, não apenas o collector.

Mapeamento atual investigado:

| ObjectKind | Variante | operationId |
|---|---|---|
| `database` | — | `sql.server.structure.database` |
| `table` | — | `sql.server.structure.tables.inventory` |
| `primary_key` | — | `sql.server.structure.primary_keys` |
| `foreign_key` | — | `sql.server.structure.foreign_keys` |
| `view` | — | `sql.server.structure.views` |
| `stored_procedure` | — | `sql.server.structure.procedures` |
| `trigger` | — | `sql.server.structure.triggers` |
| `phc.user_rule` | `brule` | `phc.customizations.brule` |
| `phc.web_user_rule` | `ebrule` | `phc.customizations.ebrule` |
| `phc.hotkey` | `ti` | `phc.customizations.teclas` |
| `phc.default_value` | `udefs` | `phc.customizations.udefs` |
| `phc.web_default_value` | `eudefs` | `phc.customizations.eudefs` |
| `phc.event` | `eventos` | `phc.customizations.eventos` |
| `phc.web_event` | `eeventos` | `phc.customizations.eeventos` |
| `phc.user_field` | `sys.columns` | `phc.customizations.user_fields` |
| `phc.web_script` | `escr` | `phc.customizations.escr` |
| `phc.user_javascript` | `jsu` | `phc.customizations.jsu` |
| `phc.web_cssjs` | `cssjs` | `phc.customizations.cssjs` |
| `phc.hidden_grid` | `formc` | `phc.customizations.formc` |
| `phc.custom_screen` | `formb` | `phc.customizations.formb` |
| `phc.user_tsql_function` | `ftu` ou `sys.objects` | `phc.customizations.ftu` |

A matriz deve ser tratada como código/configuração explícita, não inferida pelo prefixo de `ObjectId` nem apenas pelo `CollectorId`.

Se surgir um kind ou variante desconhecido, o projetor deve falhar de forma explícita ou colocar o objeto em relatório de incompatibilidades. Não deve emitir um `operationId` aproximado.

### Lacuna de proveniência

`TechnicalObjectRecord` não persiste `operationId`. O mapeamento acima reconstrói a operação a partir do emissor e da variante de SummaryJson. Esta reconstrução é válida para os emissores atuais, mas fica vulnerável a mudanças futuras no código.

Não alterar o SQLite nesta etapa. Uma futura alteração do Scanner poderá persistir operationId por objeto se a matriz estática se tornar insuficiente.

## 5. SourceObjectId, parent e SourceIdentity

- `sourceObjectId` recebe exatamente `TechnicalObjectRecord.ObjectId`; não o recalcular.
- `parentSourceObjectId` recebe `ParentObjectId` apenas quando aponta para um objeto resolvido na mesma captura.
- `sourceIdentity` é opcional e segue a matriz P1.B de schemes/scopes.
- A resolução de parent é particularmente importante para `phc.user_field`, cujo `column_id` só tem significado com o objeto SQL pai.
- Não usar `CaptureId`, hash de definição ou hash de metadata como SourceIdentity.
- Não promover REL/DTL para a lista de objetos sem uma decisão de modelo própria.

Parent inexistente ou ambíguo deve gerar diagnóstico de projeção. Não substituir o parent por nome aproximado.

## 6. Metadata e MetadataFingerprint

A fonte bruta é `SummaryJson`, mas a saída é construída por uma projeção explícita por kind.

- Aplicar a matriz `p1b-metadata-fingerprint-policy-2026-11-06.md`.
- O objeto `metadata` deve conter os campos descritivos definidos para o kind, não apenas os campos usados no hash.
- O fingerprint é calculado sobre a projeção semântica aprovada, não necessariamente sobre todo o metadata descritivo.
- Se o kind não tiver projeção semântica definida, pode transportar metadata descritivo sem `metadataFingerprint`, ou transportar objeto vazio se o schema exigir; nunca gerar hash de objeto vazio.
- Canonicalização: RFC 8785 sobre JSON UTF-8; hash SHA-256.
- Não incluir SourceIdentity, SourceObjectId, operationId, parent, artefactos ou datas voláteis na projeção do fingerprint.

## 7. Estado da definição e DefinitionFingerprint

O estado deve ser determinado por evidência do objeto e da operação, não apenas pela presença/ausência de `DefinitionArtifactId`.

| Situação | Estado projetado | Regra |
|---|---|---|
| Artefacto referenciado, ficheiro existente, tamanho e SHA-256 verificados | `Available` | Incluir ArtifactId e SHA-256 dos bytes exatos do artefacto |
| View/procedure/trigger inventariado, sem artefacto, com issue específica de definição indisponível | `Unavailable` | Incluir lista de artefactos vazia; preservar a issue no payload de diagnóstico |
| Objeto sem definição recolhida por desenho (database/table/PK/FK e campo físico descritivo) | `NotCollected` | Não inventar uma definição |
| Artefacto referenciado mas ausente, ilegível ou com digest/tamanho divergente | `Unavailable` | Diagnóstico de integridade; não calcular fingerprint |
| Definição individual comprovadamente consultada e vazia | `Empty` | Não há evidência suficiente deste caso nos emissores atuais; não usar por inferência |

### Artefactos PHC gerados pelo Scanner

Os artefactos PHC atuais são exportações construídas pelo collector: incluem cabeçalhos e metadata, além do conteúdo de código/expressão. O primeiro projetor deve calcular o DefinitionFingerprint dos bytes exatos do artefacto guardado, de acordo com o contrato de fingerprint de artefacto, e não tentar remover cabeçalhos por parsing heurístico.

Consequência conhecida: alterações a metadata repetida no cabeçalho podem alterar o DefinitionFingerprint mesmo quando o código principal não muda. Isto é uma limitação da representação atual do artefacto, não deve ser corrigida com parsing ad hoc no projetor. Uma futura evolução poderá criar artefactos separados para conteúdo original e envelope descritivo, com versionamento e compatibilidade explícitos.

Para `phc.user_field`, o ficheiro textual é uma descrição gerada de metadados da coluna, não a definição original da coluna; projetar `NotCollected` para definição.

Para views, procedures e triggers, quando o artefacto SQL foi guardado, o fingerprint incide sobre os bytes efetivamente guardados. Se a definição não foi acessível, o objeto continua no índice com `Unavailable`.

## 8. Cobertura e ausência

O projetor não emite eventos `Removed` nem interpreta ausência de objeto como remoção.

A coverage deve acompanhar o pacote IKP e ser usada pelo consumidor para avaliar se duas capturas são comparáveis. Os filtros dos emissores e os scopes efetivos permanecem limitações documentadas.

Uma operação concluída com zero objetos não é automaticamente prova de que não existiam objetos: deve ser interpretada com warnings/issues, filtros e scope da consulta.

## 9. Determinismo e validação

O mesmo workspace, com os mesmos bytes, deve produzir os mesmos bytes de `object-index.json`.

Requisitos:
- ordenar objetos por `objectKind` e `sourceObjectId` com comparação ordinal;
- ordenar propriedades via canonicalização aplicável;
- não incluir timestamps de exportação;
- preservar ordem semântica dos arrays;
- validar unicidade de `sourceObjectId`;
- validar referências de parent e artefactos;
- validar que `Available` tem artefacto e DefinitionFingerprint;
- validar que `Empty`, `Unavailable` e `NotCollected` não têm DefinitionFingerprint;
- validar que operationId existe no catálogo de capabilities/coverage;
- validar o JSON Schema IKP 0.1;
- produzir relatório de diagnósticos para inconsistências, sem corrigir silenciosamente os dados.

## 10. Testes mínimos antes de integrar

1. Workspace mínimo com database/table/PK/FK e sem artefactos de definição.
2. Workspace com objeto PHC e artefacto exportado, validando SHA-256 dos bytes exatos.
3. View/procedure/trigger com issue de definição indisponível.
4. Referência de artefacto ausente ou digest divergente.
5. `phc.user_field` com parent resolvido e não resolvido.
6. `phc.user_tsql_function` nas variantes `ftu` e `sys.objects`.
7. SummaryJson inválido ou incompleto.
8. kind/variante sem mapping de operationId.
9. parentSourceObjectId inexistente.
10. Duas execuções sobre o mesmo workspace com saída byte-a-byte idêntica.
11. Validação do resultado contra o fixture e os casos negativos IKP 0.1 existentes.

## 11. O que não fazer nesta fase

- Não adicionar colunas/tabelas ao SQLite.
- Não alterar IDs atuais.
- Não fazer novas consultas SQL/PHC apenas para preencher campos opcionais.
- Não materializar REL/DTL dentro de objects.
- Não implementar comparação multi-snapshot, EntityIdentity ou Removed.
- Não fazer parsing heurístico de cabeçalhos PHC para extrair definições.
- Não classificar artefactos descritivos como definições originais.
- Não apresentar fixture de teste como captura real.

## 12. Estado e próximo passo

O projector end-to-end ainda não está implementado. Já foram implementados componentes puros e determinísticos que não dependem de uma captura real:

- `ObjectOperationResolver`: mapping explícito dos 21 kinds emitidos para a operação observadora, validando collector e variante PHC;
- `SourceIdentityResolver`: projeção dos identificadores nativos e scopes sustentados pelos registos da mesma captura;
- `MetadataProjectionResolver`: metadata descritivo e projeção semântica separada, com normalização conservadora da atividade PHC;
- `CanonicalJsonSerializer` + `MetadataFingerprintCalculator`: canonicalização e SHA-256 da projeção semântica;
- testes unitários para mapeamentos válidos, variantes desconhecidas, ausência de dados, resolução de parent, scopes ambíguos, metadata, atividade desconhecida, canonicalização e estabilidade de fingerprints.

A compilação e os testes desses componentes passaram no GitHub Actions. O estado das definições, a integração read-only do workspace e a validação dos pressupostos com uma captura real continuam pendentes.

Próximo passo sem captura: implementar e testar canonicalização/hash sobre a projeção semântica; depois fechar as regras de estado de definição com entradas tipadas, sem ligar ainda ao workspace SQLite. Quando houver acesso ao PC, recolher uma captura controlada (idealmente primeiro uma base demo sem personalizações e depois uma instalação personalizada), copiar SQLite + artefactos para ambiente de teste e integrar o leitor read-only e o projector end-to-end.
