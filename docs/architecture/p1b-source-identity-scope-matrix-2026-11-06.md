# P1.B — Matriz SourceIdentity e scopes dos emissores
**Data:** 2026-11-06  
**Estado:** matriz implementada no núcleo puro do Export; integração no projector e validação com captura real pendentes  
**Âmbito:** primeiro `object-index.json` IKP 0.1

## 1. Objetivo

Mapear, por família de objetos emitidos, a evidência nativa já presente no `SummaryJson` e definir o respetivo `scheme`, `value` e `scope`.

Esta matriz não cria identificadores. Apenas projeta para o contrato IKP a evidência que o Scanner já recolhe. Quando a evidência é insuficiente, `sourceIdentity` deve ser omitido.

O `SourceObjectId` atual mantém-se como referência do objeto dentro da captura. Não deve ser substituído nem reinterpretado como identidade nativa.

## 2. Regras de decisão

1. `SourceIdentity` só é emitida quando existe um identificador nativo cuja semântica e scope são conhecidos.
2. O valor é serializado como string, preservando o valor original sem perda de precisão.
3. O scope identifica o contexto mínimo em que o valor é único; não deve conter hostname, credenciais ou outros dados sensíveis.
4. Não usar `CaptureId` como scope: mudaria em cada captura e impediria reconhecer a mesma origem entre capturas.
5. Não usar hashes de conteúdo, nomes qualificados, nomes de constraints ou IDs sintéticos como se fossem identificadores nativos.
6. Não inferir um identificador que o SELECT atual não recolhe, mesmo que o catálogo SQL tenha essa coluna disponível.
7. Se a evidência existe mas o scope não pode ser resolvido com os dados da captura, omitir `sourceIdentity`; não inventar um scope aproximado.
8. A qualidade/estabilidade é documentada por família. Não acrescentar `confidence` numérico.

## 3. Matriz por ObjectKind

### 3.1 SQL Server

| ObjectKind | Evidência atual | Scheme | Value | Scope proposto | Decisão |
|---|---|---|---|---|---|
| `database` | `databaseId` de `DB_ID()` | — | — | — | Omitir. O ID depende da instância SQL e a captura não recolhe uma identidade estável da instância que permita construir scope inequívoco |
| `table` | `objectId` de `sys.tables.object_id` | `sqlserver.object_id` | Inteiro serializado em decimal | `database:<database-name>` | Emitir quando o nome da base de dados da captura estiver disponível. É local à base de dados e pode mudar após drop/recreate |
| `primary_key` | Nome e `uniqueIndexId` | — | — | — | Omitir. `uniqueIndexId` identifica o índice único associado, não é assumido como identidade nativa estável da constraint |
| `foreign_key` | Nome da FK, tabela pai/referenciada, colunas e ações | — | — | — | Omitir. A consulta atual não recolhe `sys.foreign_keys.object_id` |
| `view` | Nome/schema; sem `object_id` no registo atual | — | — | — | Omitir |
| `stored_procedure` | Nome/schema; sem `object_id` no registo atual | — | — | — | Omitir |
| `trigger` | Nome/schema/tabela; sem `object_id` no registo atual | — | — | — | Omitir |
| `phc.user_field` | `columnId` de `sys.columns.column_id`; parent aponta para tabela SQL | `sqlserver.column_id` | Inteiro serializado em decimal | `database:<database-name>/object:<parent-table-objectId>` | Emitir apenas se o parent for resolvido para a tabela e o seu `objectId` estiver presente |

Notas:
- `database:<database-name>` é um scope local à instalação/captura lógica gerida pela plataforma. O nome da base de dados, isoladamente, não identifica globalmente uma instância SQL. A plataforma deve manter o contexto de instalação separado do `SourceIdentity`.
- Para `phc.user_field`, `column_id` é único apenas dentro do objeto pai. O projetor deve resolver o `parentSourceObjectId` para o registo `table` e obter o respetivo `objectId`. Não usar apenas `schema.table` como substituto do object_id.
- Se o parent não existir, estiver ambíguo ou não tiver `objectId`, omitir `sourceIdentity` dessa coluna.
- Os IDs internos podem ser úteis para correlacionar observações da mesma instalação, mas não provam continuidade após drop/recreate.

### 3.2 Personalizações PHC com stamp

Para estas famílias, o valor é o stamp PHC já presente em `SummaryJson`. O scheme comum é `phc.stamp`; o scope diferencia as tabelas PHC de origem.

| ObjectKind | Campo atual | Scheme | Value | Scope |
|---|---|---|---|---|
| `phc.user_rule` | `stamp` | `phc.stamp` | stamp | `phc.table:dbo.brule` |
| `phc.web_user_rule` | `stamp` | `phc.stamp` | stamp | `phc.table:dbo.ebrule` |
| `phc.hotkey` | `stamp` (originado de `tistamp`) | `phc.stamp` | tistamp | `phc.table:dbo.ti` |
| `phc.default_value` | `stamp` | `phc.stamp` | stamp | `phc.table:dbo.udefs` |
| `phc.web_default_value` | `stamp` | `phc.stamp` | stamp | `phc.table:dbo.eudefs` |
| `phc.event` | `stamp` | `phc.stamp` | stamp | `phc.table:dbo.eventos` |
| `phc.web_event` | `stamp` | `phc.stamp` | stamp | `phc.table:dbo.eeventos` |
| `phc.web_script` | `stamp` | `phc.stamp` | stamp | `phc.table:dbo.escr` |
| `phc.user_javascript` | `stamp` | `phc.stamp` | stamp | `phc.table:dbo.jsu` |
| `phc.web_cssjs` | `stamp` | `phc.stamp` | stamp | `phc.table:dbo.cssjs` |
| `phc.hidden_grid` | `stamp` | `phc.stamp` | stamp | `phc.table:dbo.formc` |
| `phc.custom_screen` | `stamp` | `phc.stamp` | stamp | `phc.table:dbo.formb` |
| `phc.user_tsql_function` (variante `ftu`) | `stamp` | `phc.stamp` | stamp | `phc.table:dbo.ftu` |

Regras para os stamps:
- O valor deve ser transportado sem alteração semântica. O emissor já aplica `RTRIM` nas consultas; o projetor não deve inventar normalizações adicionais.
- O scope é a tabela de origem, não o `ObjectKind` genérico. Isso evita colisões entre stamps iguais em tabelas PHC diferentes.
- O scope proposto pressupõe a origem `dbo` confirmada nas consultas atuais.
- Se o stamp estiver vazio/nulo, omitir `sourceIdentity`; não usar o nome do objeto como fallback.
- A matriz assume que o stamp identifica registos dentro da respetiva tabela PHC. A unicidade/estabilidade entre versões PHC deve ser confirmada com uma captura real ou documentação PHC antes de classificar a evidência como identidade forte transversal a versões.

### 3.3 Variante de função T-SQL instalada

`phc.user_tsql_function` também pode representar uma função encontrada em `sys.objects` (`Fx_*` / `User_*`), não encontrada em `dbo.ftu`.

| Variante | Evidência atual | Scheme | Decisão |
|---|---|---|---|
| `ftu` | stamp de `dbo.ftu` | `phc.stamp` | Usar a linha `phc.user_tsql_function` da matriz PHC |
| `sys.objects` | nome e schema; o SELECT atual não recolhe `o.object_id` | — | Omitir |

Não construir uma identidade a partir de `schema + nome` e rotulá-la como nativa. O nome qualificado pode continuar em `SourceObjectId`/propriedades de topo, como referência sintética.

## 4. Famílias sem SourceIdentity nesta versão

A ausência de `sourceIdentity` não é erro nem significa ausência de identidade na origem. Significa apenas que os dados atualmente recolhidos não permitem projetar uma evidência nativa com scope suficiente.

| Família | Motivo |
|---|---|
| Database | `databaseId` exige scope de instância SQL, não recolhido |
| PK | `uniqueIndexId` não é assumido como identidade estável da constraint |
| FK | O SELECT não recolhe `object_id` da FK |
| View / procedure / trigger | Os SELECTs atuais não transportam `object_id` para o registo |
| Funções instaladas `Fx_*` / `User_*` | O SELECT atual não recolhe `o.object_id` |
| REL/DTL com stamp | Não são `TechnicalObjectRecord` no modelo atual; permanecem em `declared_relations` e exigem projeção própria |
| REL/DTL sem stamp | O identificador atual é derivado de atributos/hash; não é identidade nativa |

## 5. Consequências para o projetor

O projetor pode extrair as identidades atuais sem alterar SQLite:

1. Interpretar `SummaryJson` de cada registo.
2. Para stamps PHC, selecionar o campo `stamp` e aplicar o scope fixo mapeado pelo emissor.
3. Para tabelas SQL, ler `objectId` e associá-lo ao nome da base de dados da captura.
4. Para colunas físicas, resolver o parent table e reutilizar o `objectId` nativo da tabela no scope.
5. Para `phc.user_tsql_function`, selecionar a variante por `source` e só emitir identidade na variante `ftu`.
6. Para todas as outras famílias, omitir a propriedade.
7. Preservar o `SourceObjectId` atual sem migração ou reescrita.

A extração de `operationId` continua a ser uma preocupação separada: o registo atual não o persiste por objeto, pelo que o projetor precisa do mapeamento explícito objectKind/variante → operationId já investigado em P1.B.

## 6. Pontos a validar antes de implementação

1. Confirmar com uma captura real a unicidade prática dos stamps PHC dentro de cada tabela e a sua estabilidade entre capturas/atualizações PHC.
2. Confirmar que o nome da base de dados pode ser resolvido inequivocamente no contexto da captura sem incluir hostname ou credenciais.
3. Confirmar que os `parentSourceObjectId` de `phc.user_field` resolvem sempre para o `SourceObjectId` das tabelas SQL correspondentes.
4. Confirmar que não existem múltiplas bases de dados/ambientes no mesmo workspace com nomes ambíguos.
5. Não ampliar SELECTs para recolher IDs adicionais nesta fase. Se uma necessidade real surgir, registar a lacuna e avaliar o custo/benefício separadamente.

## 7. Estado

- Matriz baseada nos emissores atuais: documentada e implementada no núcleo puro do Export.
- `SourceIdentityResolver` projeta stamps PHC, `sqlserver.object_id` e `sqlserver.column_id` apenas quando o scope pode ser resolvido dentro da mesma captura.
- Funções `sys.objects`, PK/FK, módulos SQL e database continuam sem SourceIdentity, por falta de identidade nativa com scope suficiente.
- Testes unitários do resolver: aprovados no GitHub Actions.
- Alterações ao schema SQLite e aos collectors: nenhuma; novas consultas à base PHC: nenhuma.
- Captura real `CSDefault202603`: projeção confirmada para os kinds presentes; 8/21 kinds mapeados foram observados.
- Unicidade e estabilidade de identidade entre capturas não ficam demonstradas por uma única captura.
- Próximo passo: testar estabilidade entre capturas e validar kinds não observados, sem recolha adicional até existir necessidade demonstrada.


## Aditamento — validação entre duas instalações, 2026-11-24

A comparação real CS default / dfblock encontrou 12 correspondências cross-capture com `phc.stamp`, nos kinds `formb` e `formc`. O resultado é compatível com a utilização do stamp como evidência nativa dentro do scope PHC documentado, mas não demonstra cobertura nem unicidade universal para os restantes kinds.

O `sqlserver.object_id` não corresponde entre bases de dados distintas, confirmando que o scope por base de dados é obrigatório. As correspondências de tabelas por nome/`SourceObjectId` (cerca de 7 933) permanecem observacionais; não devem ser promovidas a identidade lógica nem provocar fusão automática.

Não foram recolhidos novos identificadores, alterados scopes ou modificados collectors. A política de matching e a distinção entre continuidade dentro da Installation e correspondência entre Installations continuam a exigir contrato próprio. Ver [avaliação de identidade e proveniência entre capturas](../audits/identity-provenance-cross-capture-assessment-2026-11-24.md).


## Aditamento — D-023 e matriz de reconhecimento por ObjectKind (2026-11-24)

Esta matriz continua a definir apenas os schemes/scopes de SourceIdentity que o Scanner actual consegue projectar. Não define, por si só, identidade lógica, correspondência cross-installation ou classificação PHC Standard. Essas dimensões são regidas por [D-023](d-023-cross-installation-identity-correspondence-contract.md) e pela [matriz de reconhecimento por ObjectKind v0.1](d-023-objectkind-recognition-matrix-2026-11-24.md).

A análise por objecto CS default/dfblock confirmou 1 par `phc.stamp` em `phc.custom_screen` (`formb`) e 11 em `phc.hidden_grid` (`formc`). Os 12 pares são 1:1, sem stamps repetidos dentro destes kinds/scopes nas duas capturas, e apresentam `SourceObjectId`, `MetadataFingerprint` e metadata projectada iguais. Isto demonstra correspondência observacional apenas neste conjunto; não prova estabilidade intra-installation, generalização, identidade global ou correspondência com PHC Standard. Para as restantes famílias, a presença de stamp no contrato de emissão não equivale a unicidade/estabilidade cross-installation demonstrada. Não ampliar scopes nem recolher novos identificadores sem validação e aprovação.
