# P1.B — Matriz de projeção do metadata descritivo

| Campo | Valor |
|---|---|
| Data | 2026-11-06 |
| Estado | Proposta estática baseada nos SummaryJson atuais; não implementada |
| Fase | P1.B — object-index |
| Âmbito | Separar metadata descritivo de MetadataFingerprint |
| Restrições | Sem alterações a SQLite, collectors, queries ou schemas |

## 1. Objetivo

Definir o conteúdo do campo `metadata` do object-index sem o confundir com a projeção reduzida que alimenta `metadataFingerprint`.

São duas representações com objetivos diferentes:

- **Metadata descritivo:** preserva factos estruturados úteis que o Scanner já recolhe e que ajudam a compreender o objeto.
- **MetadataFingerprint:** usa apenas os campos semânticos aprovados na política `p1b-metadata-fingerprint-policy-2026-11-06.md`.

O fingerprint não deve ser calculado sobre todo o metadata descritivo. Campos voláteis, IDs internos, classificações derivadas ou dados de contexto podem ser úteis para diagnóstico sem deverem alterar o fingerprint.

## 2. Regras gerais

1. A origem continua a ser o `SummaryJson` do objeto, interpretado por `ObjectKind` e variante.
2. Nunca copiar o JSON bruto para o pacote sem validação e projeção explícita.
3. `Name`, `SchemaName`, `SourceObjectId`, `SourceIdentity`, `ParentSourceObjectId` e `operationId` permanecem propriedades de topo; não os duplicar em metadata sem uma razão semântica explícita.
4. O stamp PHC é projetado em `SourceIdentity` quando a matriz de scope o permite; não o repetir no metadata.
5. O campo `source` do SummaryJson é um discriminador técnico. Quando necessário para distinguir variantes, expô-lo como `sourceVariant`; não expor o nome interno do campo do Scanner.
6. Não reconstruir campos ausentes a partir de cabeçalhos dos artefactos nesta etapa.
7. Não preencher valores em falta com strings vazias, zero ou false.
8. Manter tipos JSON corretos sempre que a origem permita tipagem inequívoca.
9. Propriedades descritivas que não entram no fingerprint devem continuar fora da projeção de hash.
10. Se o SummaryJson estiver inválido ou não corresponder à variante esperada, emitir diagnóstico e não adivinhar metadata.

## 3. Matriz SQL Server

| ObjectKind atual | Metadata descritivo proposto | Excluir / tratar separadamente |
|---|---|---|
| `database` | `versionProperty` | `name` é propriedade de topo. `databaseId` não é promovido a identidade sem scope de instância; não o duplicar em metadata |
| `table` | `typeDesc`, `isMsShipped`, `classification`, `namePrefixU`, `createDate`, `modifyDate` | `objectId` é candidato a SourceIdentity; datas e classificações derivadas ficam fora do fingerprint |
| `primary_key` | `columns` com `name` e `ordinal` | `uniqueIndexId` é ID interno e não integra metadata semântico |
| `foreign_key` | `referencedSchema`, `referencedTable`, `deleteReferentialAction`, `updateReferentialAction`, `columns` com `parent`, `referenced`, `ordinal` | Não adicionar IDs internos que o emissor não recolhe |
| `view` | Objeto vazio `{}` | O emissor não fornece SummaryJson; não inferir metadata a partir do SQL da definição |
| `stored_procedure` | Objeto vazio `{}` | Igual a view |
| `trigger` | `isDisabled` | Não inferir object_id, tipo ou atributos adicionais |

Notas:
- Para `table`, `classification` e `namePrefixU` são classificações do Scanner. Podem ser úteis para diagnóstico e filtragem, mas não são factos nativos SQL.
- `createDate` e `modifyDate` podem ser preservadas como contexto descritivo; não entram no fingerprint.
- A ausência de metadata semântico para view/procedure não justifica hash de `{}`.

## 4. Matriz PHC

A matriz abaixo preserva apenas campos estruturados já presentes em SummaryJson. Campos consultados pelo collector mas não incluídos no SummaryJson não são recuperados nesta fase.

| ObjectKind atual | Variante / origem | Metadata descritivo proposto |
|---|---|---|
| `phc.user_rule` | `brule` | `tabela`, `inactivoRaw` |
| `phc.web_user_rule` | `ebrule` | `tabela`, `ecran`, `inactivoRaw` |
| `phc.hotkey` | `ti` + join `tc` | `ecran`, `teclas`, `inactivoRaw` |
| `phc.default_value` | `udefs` | `tabela`, `campo`, `inactivoRaw` |
| `phc.web_default_value` | `eudefs` | `tabela`, `campo`, `ecran`, `inactivoRaw` |
| `phc.event` | `eventos` | `ecran`, `qevento`, `inactivoRaw` |
| `phc.web_event` | `eeventos` | `ecran`, `qevento`, `inactivoRaw` |
| `phc.user_field` | `sys.columns` | `schema`, `table`, `column`, `type`, `maxLength`, `precision`, `scale`, `isNullable`, `prefix` |
| `phc.web_script` | `escr` | `codigo`, `inactivoRaw` |
| `phc.user_javascript` | `jsu` | `ecran`, `inactivoRaw` |
| `phc.web_cssjs` | `cssjs` | `codigo`, `tipo`, `inactivoRaw` |
| `phc.hidden_grid` | `formc` | `ecran`, `nome`, `tag`, `tvalobj` |
| `phc.custom_screen` | `formb` | `ecran`, `nome`, `tabela`, `defeito`, `inactivoRaw` |
| `phc.user_tsql_function` | `ftu` | `sourceVariant: "ftu"`, `classif`, `inactivoRaw` |
| `phc.user_tsql_function` | `sys.objects` | `sourceVariant: "sys.objects"`, `schema`, `nome`, `inactivoRaw: false` |

### 4.1 Campos não repetidos

- `stamp`: fica em SourceIdentity quando possível.
- `source`: só é transportado como `sourceVariant` quando necessário para distinguir a origem/variante.
- `estado`: não é copiado; é derivado de `inactivo` e pode esconder valores desconhecidos.
- Código, expressões e definições: pertencem aos artefactos e ao DefinitionFingerprint, não ao metadata.
- `columnId`: é candidato a SourceIdentity de `phc.user_field`, não metadata descritivo.
- `objectId`: é candidato a SourceIdentity de `table`, não metadata descritivo.

### 4.2 Atividade: ponto que exige cuidado

O SummaryJson PHC guarda `inactivo` como valor textual ou convertível, enquanto `estado` é calculado pelo helper `PhcActivityState`. O helper trata strings desconhecidas como ativo.

Para não perder evidência, o metadata descritivo deve preservar o valor original como `inactivoRaw` (sem o converter silenciosamente). A projeção de fingerprint pode usar um booleano normalizado apenas quando o valor for reconhecido de forma inequívoca.

Valores nulos ou desconhecidos não devem ser transformados automaticamente em `false` pelo projector. Nestes casos:
- preservar o valor bruto, se existir;
- omitir o campo booleano normalizado;
- omitir MetadataFingerprint quando a atividade for um dos campos necessários e não puder ser interpretada;
- emitir diagnóstico de projeção.

Esta regra é mais conservadora do que chamar diretamente `PhcActivityState.IsInactive` para qualquer valor. Não altera o helper nem os dados persistidos pelo Scanner. A compatibilidade desta decisão deve ser confirmada com captura real.

## 5. Campos do emissor que não podem ser recuperados de SummaryJson

Os seguintes valores são consultados ou escritos nos cabeçalhos dos artefactos, mas não estão atualmente todos presentes no SummaryJson. Não os adicionar ao metadata do índice através de parsing de texto nesta etapa:

- BRULE/EBRULE: mensagem, descrição, grau e expressão;
- teclas: descrição e retorno;
- UDEFS/EUDEFS: descrição, tipo, quando e expressões/valores;
- eventos/eeventos: resumo, reação, descrição e expressões/condições;
- ESCR: resumo, reação, URL e conteúdos;
- JSU: título, ordem e JavaScript;
- CSSJS: título e conteúdos source/sourcedefault;
- FORMC: propriedades completas e código extraído;
- FORMB: nome de ecrã, descrição e ordem;
- FTU: nome e conteúdo xbase da variante dbo.ftu.

Estes dados continuam disponíveis nos artefactos, quando foram emitidos. A sua ausência no metadata não significa perda do conteúdo no pacote.

Uma eventual expansão do metadata deve ser uma decisão de contrato própria, baseada em campos estruturados recolhidos, não em parsing heurístico dos artefactos.

## 6. Relação com MetadataFingerprint

A projeção de hash continua a ser definida exclusivamente em `p1b-metadata-fingerprint-policy-2026-11-06.md`.

| Tipo de campo | Pode estar em metadata descritivo? | Entra no fingerprint? |
|---|---:|---:|
| Atributo semântico aprovado | Sim | Sim |
| ID nativo usado em SourceIdentity | Não, por omissão | Não |
| Nome/schema/parent de topo | Não duplicar | Não |
| Data de criação/modificação | Sim, se útil | Não |
| Classificação derivada do Scanner | Sim, como contexto | Não |
| Campo técnico de seleção (ex.: prefix u_) | Sim, como contexto | Não |
| Valor bruto de atividade | Sim | Só após normalização inequívoca |
| Estado derivado `estado` | Não | Não |
| Código/expressão | Não | Não; pertence a DefinitionFingerprint |
| Variante técnica | Sim, se necessária | Não, salvo decisão semântica explícita |

A canonicalização RFC 8785 e SHA-256 são aplicadas à projeção de fingerprint, não ao objeto metadata descritivo completo.

## 7. Invariantes e testes puros

Antes de integrar com SQLite, criar testes sem dependência de captura para:

1. Cada kind tem um projector de metadata explícito.
2. Kind sem SummaryJson válido não recebe metadata inventado.
3. `source` só é transformado em `sourceVariant` para variantes declaradas.
4. Stamp e IDs nativos não são duplicados em metadata.
5. `estado` nunca é copiado como facto independente.
6. Valores desconhecidos de `inactivo` não são convertidos silenciosamente em ativo.
7. Campos descritivos voláteis podem mudar sem alterar MetadataFingerprint.
8. Campos semânticos aprovados alterados mudam o fingerprint.
9. Ordem de colunas PK/FK é preservada.
10. Os mesmos inputs produzem metadata e fingerprint byte-a-byte determinísticos.

## 8. Decisões pendentes

1. Confirmar com captura real os formatos de `inactivo/inativo` usados nas tabelas PHC cobertas.
2. Confirmar se datas SQL devem ser transportadas no metadata descritivo ou omitidas por política de minimização; em qualquer caso não entram no fingerprint.
3. Rever a utilidade de `classification`, `namePrefixU` e `prefix` no metadata descritivo face à sua natureza derivada/de seleção.
4. Confirmar a convenção final de nomes de propriedades em inglês no IKP, mantendo os nomes PHC originais apenas quando forem valores/campos da origem que devam ser preservados.

## 9. Estado

- Matriz baseada nos SummaryJson dos emissores atuais: proposta documentada.
- Relação com MetadataFingerprint: explicitada.
- Código do projector e hashing: não implementado.
- Captura real: pendente.
- SQLite, collectors, queries e schemas: inalterados.

Próximo passo recomendado: converter as matrizes de mapping, SourceIdentity e metadata em especificações de testes unitários puros. A implementação poderá começar pelo núcleo determinístico do projector, sem depender ainda do acesso ao workspace real.
