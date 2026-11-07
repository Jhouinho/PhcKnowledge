# P1.B — Política de MetadataFingerprint por ObjectKind
**Data:** 2026-11-06  
**Estado:** política documentada; projeção semântica implementada; canonicalização/hash ainda não implementados  
**Âmbito:** primeiro `object-index.json` IKP 0.1

## 1. Objetivo

Definir, para cada `ObjectKind` atualmente emitido pelo Scanner, uma projeção semântica explícita dos metadados que pode alimentar `MetadataFingerprint`.

O fingerprint deve responder à pergunta:

> Mudaram as características relevantes deste objeto entre duas observações?

Não deve responder se mudou a identidade, o código/definição, a data de recolha, a posição interna atribuída pelo SQL Server ou a versão das regras de classificação do Scanner.

Esta política complementa o contrato `p1-identity-provenance-contract.md`. Não altera o schema IKP 0.1 nem o SQLite.

## 2. Regras gerais

1. A origem é o `SummaryJson` produzido pelo emissor, mas só entram na projeção os campos explicitamente aprovados nesta matriz.
2. `sourceObjectId`, `objectKind`, `sourceIdentity`, `operationId`, nome, schema e parent são propriedades do objeto no índice; não são copiados automaticamente para `metadata`.
3. Identificadores nativos usados em `SourceIdentity` não são repetidos no metadata fingerprint.
4. Excluir datas de criação/modificação, datas de recolha, IDs internos de catálogo, contadores, caminhos de artefactos e campos derivados de outros campos.
5. Não aplicar lower-case universal. Preservar nomes SQL e valores PHC exatamente, salvo trim de padding que o próprio emissor já efetua.
6. Arrays que representam colunas/ordens devem manter a ordem semântica indicada pelo ordinal. Não ordenar alfabeticamente.
7. Booleanos e números devem ser serializados como tipos JSON, não como strings, quando a projeção os consegue tipar sem ambiguidade.
8. Canonicalizar o objeto projetado com JSON Canonicalization Scheme (RFC 8785) e calcular SHA-256 dos bytes UTF-8.
9. Se a projeção não estiver suficientemente definida ou não tiver campos semânticos, omitir `metadataFingerprint`. Não calcular hash de `{}` nem de metadata arbitrário.
10. A política deve ter uma versão própria no código/documentação da implementação. A versão não entra no objeto metadata nem no hash; deve ser rastreável na versão do projetor/exportador.

## 3. Matriz de projeção

### 3.1 SQL Server

| ObjectKind | Campos incluídos na projeção | Excluir / notas | Decisão |
|---|---|---|---|
| `database` | Nenhum, nesta versão | `databaseId` é local à instância; `versionProperty` é propriedade do motor/BD e não descreve uma alteração semântica do objeto; o nome já é propriedade de topo | Omitir fingerprint |
| `table` | `typeDesc`, `isMsShipped` | Excluir `objectId` (ID interno), `createDate`, `modifyDate`, `classification` e `namePrefixU` (classificações/derivações do Scanner) | Incluir |
| `primary_key` | `columns` como lista ordenada de `{name, ordinal}` | Excluir `uniqueIndexId` (ID interno do índice). Nome/schema/tabela são propriedades do objeto/parent | Incluir |
| `foreign_key` | `referencedSchema`, `referencedTable`, `deleteReferentialAction`, `updateReferentialAction`, `columns` ordenadas de `{parent, referenced, ordinal}` | Não incluir IDs internos. Nome/schema/tabela pai são propriedades do objeto/parent | Incluir |
| `view` | Nenhum disponível | O emissor não preenche `SummaryJson`. A definição SQL, quando disponível, pertence ao `DefinitionFingerprint` | Omitir até existir metadata real |
| `stored_procedure` | Nenhum disponível | Igual a view | Omitir até existir metadata real |
| `trigger` | `isDisabled` | Não existe atualmente `object_id` no resumo; não inferir outros atributos | Incluir |

Notas:
- `table.objectId` e `database.databaseId` podem ser evidência para SourceIdentity, com scope apropriado, mas não pertencem ao fingerprint semântico.
- Em PK/FK, a ordem das colunas é parte da semântica. Uma alteração de ordinal ou de coluna deve alterar o fingerprint.
- Uma alteração apenas no ID interno de catálogo não deve alterar o fingerprint.

### 3.2 Personalizações PHC

Para os tipos abaixo, o campo `source` identifica a variante fixa do emissor. É útil para interpretar o registo, mas não entra no fingerprint: o `objectKind` e a identidade de origem já distinguem o objeto. O stamp também fica fora da projeção quando representado em `SourceIdentity`.

| ObjectKind | Campos incluídos na projeção | Excluir / notas | Decisão |
|---|---|---|---|
| `phc.user_rule` | `tabela`, `inactivo` normalizado para boolean | Excluir `stamp`, `source`, `estado` (derivado de inactivo). A expressão está no artefacto/definição | Incluir |
| `phc.web_user_rule` | `tabela`, `ecran`, `inactivo` boolean | Excluir `stamp`, `source`, `estado`. Mensagem/descrição/grau não estão no SummaryJson atual; não os recuperar do cabeçalho do artefacto nesta fase | Incluir |
| `phc.hotkey` | `ecran`, `teclas`, `inactivo` boolean | Excluir `stamp`, `source`, `estado` | Incluir |
| `phc.default_value` | `tabela`, `campo`, `inactivo` boolean | Excluir `stamp`, `source`, `estado`. `tipo`, `quando` e partes de expressão não estão no resumo atual | Incluir |
| `phc.web_default_value` | `tabela`, `campo`, `ecran`, `inactivo` boolean | Excluir `stamp`, `source`, `estado`. `tipo`, `quando` não estão no resumo atual | Incluir |
| `phc.event` | `ecran`, `qevento`, `inactivo` boolean | Excluir `stamp`, `source`, `estado`. Expressões/condições pertencem à definição | Incluir |
| `phc.web_event` | `ecran`, `qevento`, `inactivo` boolean | Excluir `stamp`, `source`, `estado`. Expressões/condições pertencem à definição | Incluir |
| `phc.user_field` | `schema`, `table`, `column`, `type`, `maxLength`, `precision`, `scale`, `isNullable` | Excluir `columnId` (ID interno), `source`, `prefix` (critério de seleção, não propriedade semântica) | Incluir |
| `phc.web_script` | `codigo`, `inactivo` boolean | Excluir `stamp`, `source`, `estado`. `resumo`, `reacao`, `rurl` não estão no resumo atual | Incluir |
| `phc.user_javascript` | `ecran`, `inativo` boolean | Excluir `stamp`, `source`, `estado`. `titulo` e `ordem` não estão no resumo atual | Incluir |
| `phc.web_cssjs` | `codigo`, `tipo`, `inativo` boolean | Excluir `stamp`, `source`, `estado`. Título e conteúdo não estão no resumo atual | Incluir |
| `phc.hidden_grid` | `ecran`, `nome`, `tag`, `tvalobj` | Excluir `stamp`, `source`. O código extraído de Xexpressao pertence à definição | Incluir |
| `phc.custom_screen` | `ecran`, `nome`, `tabela`, `defeito`, `inactivo` boolean | Excluir `stamp`, `source`, `estado`. `ecrannm`, descrição e ordem não estão no resumo atual | Incluir |
| `phc.user_tsql_function` | Variante `ftu`: `classif`, `inactivo` boolean. Variante `sys.objects`: apenas `schema` | Excluir `stamp`, `source`, `estado`; na variante `sys.objects`, excluir também `inactivo` e `nome` (o nome já está em `TechnicalObjectRecord.Name`) | Incluir com projeção discriminada |

### 3.3 Campos PHC com normalização de atividade

O emissor grava `inactivo` como texto (por exemplo, `"0"`) e também grava `estado`, derivado do mesmo valor. A projeção deve converter o valor para boolean usando a mesma regra semântica de `PhcActivityState.IsInactive`:

- `true`: inativo;
- `false`: ativo.

Não incluir simultaneamente `inactivo` e `estado`: isso duplicaria a mesma informação e criaria risco de divergência. A normalização deve ser centralizada e testada contra a função já existente; não deve ser reimplementada com conversões permissivas ad hoc.

Para `phc.user_tsql_function`, o discriminador `source` seleciona a projeção. Se estiver ausente ou tiver valor desconhecido, omitir o fingerprint e emitir diagnóstico; não adivinhar a variante.

**Exceção importante — variante `sys.objects`:** o collector atual escreve `inactivo = false` e `estado = "Activo"` como constantes para funções encontradas em `sys.objects`. A consulta não recolhe um estado de atividade PHC para estas funções. Estes valores são defaults sintéticos, não factos observados; não devem ser projetados para metadata nem usados no `MetadataFingerprint`. O nome já está em `TechnicalObjectRecord.Name`, pelo que também não deve ser duplicado no metadata. Esta regra não altera a variante `ftu`, onde `inactivo` é lido da tabela PHC.


## 4. Limitações reconhecidas

1. O resumo atual de vários emissores PHC é deliberadamente mais pequeno do que os campos consultados e o cabeçalho do artefacto. A matriz usa apenas dados estruturados presentes em `SummaryJson`; não promove texto de cabeçalho a metadata sem uma decisão específica.
2. `phc.user_field` representa colunas físicas `u_*` de `sys.columns`, não a tabela funcional PHC `dbo.campos`. O fingerprint descreve a coluna física observada, não uma definição funcional de campo PHC.
3. `view` e `stored_procedure` não têm metadata estruturada atualmente. O nome/schema não justificam um hash de metadata vazio.
4. `database` também não tem nesta fase uma projeção útil. O identificador interno e a propriedade de versão não devem produzir diferenças semânticas artificiais.
5. A matriz não resolve lacunas de coverage, nem transforma ausência de registo em prova de remoção.
6. O fingerprint de metadata não substitui nem duplica o fingerprint de definição.

## 5. Contrato de serialização e casos de teste necessários

Antes da implementação, o projetor deverá garantir:

- propriedades emitidas numa representação JSON determinística, canonicalizada por RFC 8785;
- ausência de propriedades não recolhidas, em vez de valores inventados ou preenchidos por omissão;
- distinção entre `null`, string vazia, zero e `false` quando a origem os distingue;
- arrays de colunas ordenados pelo ordinal;
- nomes e valores preservados sem lower-case universal;
- IDs internos, datas voláteis, campos derivados e marcadores de fonte excluídos;
- `phc.user_tsql_function` testado para as duas variantes;
- ausência de `metadataFingerprint` nos tipos marcados como omitir;
- alteração do fingerprint quando muda qualquer campo aprovado;
- estabilidade do fingerprint quando só muda um campo excluído.

Casos de regressão prioritários:
1. tabela com mesmo nome e metadados, mas `objectId` e datas diferentes → fingerprint igual;
2. PK com mesmas colunas e ordinais, mas `uniqueIndexId` diferente → igual;
3. FK com alteração de ação referencial ou coluna → diferente;
4. coluna `u_*` com `columnId` diferente, mas mesma definição física → igual;
5. personalização PHC com stamp diferente mas mesma projeção semântica → igual, desde que comparada como metadata e não como identidade;
6. personalização com estado ativo/inativo alterado → diferente;
7. função instalada sem discriminador de variante → fingerprint omitido;
8. view/procedure sem resumo → fingerprint omitido.

## 6. Decisões ainda não fechadas

- Confirmar se o primeiro object-index deve incluir todos os 21 kinds ou um subconjunto com cobertura mais madura.
- Confirmar, com uma captura real, que os valores de `inactivo` são consistentes com `PhcActivityState.IsInactive` em todas as tabelas.
- Rever se `typeDesc` e `isMsShipped` são necessários no fingerprint de tabela ou se devem permanecer apenas como metadata descritiva. A proposta atual inclui ambos porque descrevem a natureza física do objeto, mas esta escolha deve ser validada com snapshots reais.
- Decidir numa etapa própria se os resumos dos emissores PHC devem ser enriquecidos com metadata estruturada já consultada, sem depender de parsing de cabeçalhos de artefactos.

## 7. Estado da investigação

- Leitura dos emissores SQL, PHC e `SqlKeyInventory`: concluída para a matriz inicial.
- Política de campos semânticos por kind: documentada.
- `MetadataProjectionResolver`: implementado e testado; produz metadata descritivo e a projeção semântica separada.
- Normalização de atividade: reconhece apenas valores explícitos; valores desconhecidos preservam o bruto e impedem fingerprint.
- Canonicalização RFC 8785 e SHA-256: ainda não implementados.
- Alterações ao SQLite, schema IKP ou collectors: nenhuma.
- Validação com workspace real: pendente de acesso a uma captura (SQLite + artefactos).

Próximo passo recomendado: implementar canonicalização e cálculo SHA-256 sobre `FingerprintProjection`, com testes de estabilidade/invariância, antes de integrar os resultados no projector end-to-end.
