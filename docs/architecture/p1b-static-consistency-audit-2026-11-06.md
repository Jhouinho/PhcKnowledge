# P1.B — Auditoria estática de consistência do object-index

| Campo | Valor |
|---|---|
| Data | 2026-11-06 |
| Estado | Auditoria estática concluída; sem alterações funcionais |
| Branch analisada | `main` |
| Âmbito | Contratos P1.B, IKP 0.1, fixture mínima, catálogo de capabilities, collectors e workspace SQLite |
| Restrições | Sem captura real; sem alterações ao SQLite, collectors ou queries |

## 1. Conclusão executiva

É possível avançar com a preparação do projector e dos seus testes sem uma captura real. A inspeção estática confirmou a arquitetura de leitura proposta e o mapeamento dos 21 tipos emitidos. Contudo, encontrou divergências entre a fixture IKP 0.1 e o Scanner atual, além de limitações de coverage e de representação de definições que devem ser tratadas antes de usar a fixture como referência semântica.

**Não foi alterado código, schema SQLite, collectors, queries nem fixtures.** Este documento regista os resultados para orientar a próxima etapa.

## 2. Resultados confirmados

### 2.1 Proveniência objeto → operação

O mapeamento estático de 21 `ObjectKind` para operações é reconstruível a partir das chamadas de `OperationCoverageTracker` e dos pontos de criação de `TechnicalObjectRecord`.

- SQL: 7 tipos, observados por 7 operações em `sql.server.structure`.
- PHC: 14 tipos, observados por 13 operações em `phc.customizations`.
- `phc.user_tsql_function` é uma operação com duas variantes (`source=ftu` e `source=sys.objects`).
- `phc.hotkey` provém de uma query que junta `dbo.ti` e `dbo.tc`, mas a identidade stamp é `tistamp` de `dbo.ti`.
- `phc.user_field` é emitido a partir de `sys.columns` para colunas `u_*`; não representa a tabela funcional `dbo.campos`.

A operação é inequívoca para os emissores atuais, mas não está persistida no `TechnicalObjectRecord`. O projector deve manter um mapping explícito, versionado e fechado; kind/variante desconhecido deve originar diagnóstico/falha controlada, nunca uma operação aproximada.

### 2.2 Operações e coverage

O catálogo atual declara:
- 7 operações SQL;
- 14 operações de personalizações PHC;
- 2 operações REL/DTL no collector separado de relações declaradas.

As 2 operações REL/DTL não pertencem aos 21 tipos de objeto. Os respetivos dados vivem em `declared_relations`, não em `objects`.

O `OperationCoverageTracker` mede a diferença de `observedCount` em torno da operação. Os collectors incrementam esse contador quando emitem objetos. Em vários métodos PHC, as linhas são filtradas antes da emissão quando não têm expressão/conteúdo. Consequentemente, `observedCount` representa objetos materializados, não necessariamente o total de linhas da tabela de origem.

A semântica genérica `CoverageSemantics.Succeeded` define `contentAvailability=Empty` quando `observedCount=0`. Num emissor que filtra linhas, esse Empty significa zero objetos emitidos pelo âmbito implementado; não prova que a tabela de origem não tenha registos ou que não existam personalizações sem conteúdo. A interpretação deve conservar os filtros e scope efetivos.

### 2.3 Definições individuais

As views, procedures e triggers continuam no inventário quando a definição SQL é nula: o collector cria uma issue Warning com `object_name` e emite o objeto sem `DefinitionArtifactId`. É possível projetar `Unavailable` quando a issue específica corresponde inequivocamente ao objeto.

Nos emissores PHC, várias linhas sem expressão/conteúdo são ignoradas antes de criar `TechnicalObjectRecord`. Assim, não há observação individual que permita atribuir `Empty` a essas linhas. Não se deve criar objetos Empty a partir delas nem deduzi-los a partir de coverage Empty.

Os artefactos PHC são exports textuais com cabeçalhos e metadata gerados pelo collector. O fingerprint dos bytes atuais é reprodutível, mas pode mudar se mudar o cabeçalho, mesmo que o fragmento de código se mantenha. A política já documentada de hashear os bytes exatos do artefacto é coerente com o estado atual; não remover cabeçalhos por heurística.

### 2.4 Estados de atividade PHC

`PhcActivityState.IsInactive` normaliza valores booleanos, numéricos e strings reconhecidas. Strings desconhecidas e valores nulos são tratados como `false` (ativo). Isto é comportamento atual do Scanner, não prova de que todos os dados de origem estejam normalizados.

O projector não deve inventar um segundo parser nem reescrever retroativamente os valores. Deve preservar o valor bruto disponível em metadata descritivo quando relevante, e evitar produzir um fingerprint que transforme silenciosamente um valor desconhecido em estado ativo. A validação real da variedade de valores exige captura.

## 3. Divergências da fixture mínima IKP 0.1

A fixture `docs/architecture/fixtures/ikp-0.1/minimal-valid` passa a validação estrutural e de integridade documentada. Isso não demonstra compatibilidade semântica com os emissores atuais.

| Aspeto | Fixture | Scanner atual | Consequência |
|---|---|---|---|
| Kind de tabela | `sql.table` | `table` | Vocabulário divergente |
| Kind BRULE | `phc.brule` | `phc.user_rule` | Vocabulário divergente |
| Operação de tabelas | `sql.schema.tables.inventory` | `sql.server.structure.tables.inventory` | A fixture não usa o operationId real |
| Operação BRULE | `phc.customizations.brule.inventory` | `phc.customizations.brule` | A fixture inventa separação inventory/definition que o collector atual não declara |
| Relações declaradas | Um objeto em `object-index.json` | Registos em `declared_relations`, fora de `objects` | Divergência de fronteira do modelo |
| Operação REL/DTL | `phc.relations.declared.inventory` | `phc.declared-relations.rel` e `phc.declared-relations.dtl` | Identificadores e granularidade diferentes |
| SourceIdentity de tabela | Scope `PHC_DEMO` | Candidato P1.B é base de dados com scheme `sqlserver.object_id` | A fixture não demonstra a política P1.B de scope |
| SchemaName BRULE | `dbo` | O registo PHC usa `SchemaName` para tabela/ecrã/contexto (ex.: tabela), não para schema SQL | Semântica de campo potencialmente diferente |

A fixture é explicitamente demonstrativa e não provém de captura real. A validação 18/18 prova as regras implementadas pelo validador e a coerência interna do exemplo, não a correspondência com o catálogo e os emissores atuais.

**Decisão recomendada:** manter a fixture intacta nesta auditoria; criar uma tarefa separada para alinhar a fixture e/ou clarificar o vocabulário normativo do IKP. Não alterar o schema nem o contrato por inferência durante P1.B.

## 4. Problemas de resolução de issues

As issues individuais de definição SQL usam identificadores:
- view: `schema.name`;
- procedure: `schema.name`;
- trigger: `schema.table.trigger`.

O `TechnicalObjectRecord` tem campos separados `Name`, `SchemaName` e `ParentObjectId`. Portanto, uma resolução genérica por igualdade entre `issue.object_name` e `object.Name` não é válida. É necessária uma regra explícita por kind e validação de unicidade.

As issues de exceção da operação são diferentes das issues de definição individual. A issue genérica pode não ter `object_name`, pelo que deve afetar o estado de coverage da operação, não ser atribuída indiscriminadamente a todos os objetos.

## 5. Scope e limites de inventário que têm de acompanhar a interpretação

| Operação | Limite confirmado |
|---|---|
| `sql.server.structure.triggers` | Apenas triggers associados a tabelas `u_*` |
| `phc.customizations.user_fields` | Apenas colunas físicas `u_*` em `sys.columns` |
| `phc.customizations.teclas` | Join `ti` + `tc`; emite apenas linhas com `ti.retorno` não vazio |
| `phc.customizations.brule` / `ebrule` | Emite apenas linhas com `expressao` não vazia |
| `phc.customizations.udefs` | Emite apenas linhas com pelo menos uma parte de definição útil |
| `phc.customizations.eudefs` | Emite apenas linhas com `expvb`, `transact` ou `valorc` não vazios |
| `phc.customizations.eventos` / `eeventos` | Emite apenas linhas com expressão/condição não vazia |
| `phc.customizations.jsu` | Emite apenas linhas com JavaScript não vazio |
| `phc.customizations.cssjs` | Emite apenas linhas com `source` ou `sourcedefault` não vazios |
| `phc.customizations.ftu` | Omite linha de `dbo.ftu` quando nome e xbase estão vazios; variante `sys.objects` omite definição vazia e nomes já vistos em FTU |

O mapping de operationId continua válido, mas o índice deve ser entendido como inventário dos objetos emitidos pelo perfil atual, não como catálogo completo de todas as linhas das tabelas PHC.

## 6. Plano de trabalho seguro sem captura real

### Pode avançar já

1. Consolidar um catálogo de mapping `ObjectKind + source variant → operationId`, extraído do código atual.
2. Definir as projeções de metadata descritivo separadas das projeções usadas no fingerprint.
3. Definir regras de resolução de issues por tipo SQL.
4. Criar testes puros para canonicalização, hash, mapping, parsing de SummaryJson, parent e ordenação.
5. Criar fixtures unitárias pequenas, claramente sintéticas, para testar regras isoladas; não as apresentar como workspace/captura PHC.

### Fica dependente de captura real

1. Confirmar os valores e a unicidade dos stamps PHC no âmbito proposto.
2. Confirmar valores reais de `inactivo/inativo` e casos desconhecidos.
3. Confirmar integridade e layout de artefactos, incluindo variantes PHC.
4. Validar parent resolution de `phc.user_field` com tabelas reais.
5. Validar cobertura, contagens e issues numa captura completa.
6. Produzir um `object-index.json` real.

### Continua expressamente fora de âmbito

- Alterar schema/tabelas SQLite.
- Persistir `operationId` nos objetos.
- Alterar collectors, queries ou filtros.
- Recolher campos PHC standard adicionais.
- Materializar REL/DTL no object-index.
- Inferir remoções ou identidade lógica de plataforma.

## 7. Sequência recomendada

1. Tratar a fixture IKP como exemplo de contrato, não como golden fixture do Scanner atual.
2. Fechar a matriz de mapping e de metadata descritivo.
3. Implementar e testar funções puras do projector, sem dependência de SQLite real.
4. Quando existir acesso ao PC, executar validação read-only sobre o workspace real (SQLite + pasta completa de artefactos).
5. Só depois decidir se há alguma lacuna que justifique alteração ao modelo de armazenamento ou aos collectors.

## 8. Ficheiros analisados

- `docs/architecture/p1b-metadata-fingerprint-policy-2026-11-06.md`
- `docs/architecture/p1b-source-identity-scope-matrix-2026-11-06.md`
- `docs/architecture/p1b-object-index-projector-design-2026-11-06.md`
- `docs/architecture/p1b-object-index-emitter-investigation-2026-11-06.md`
- `docs/architecture/ikp-0.1-specification.md`
- `docs/architecture/ikp-0.1-scanner-compatibility-matrix.md`
- Schemas IKP 0.1 e fixture `minimal-valid`
- `KnownOperations`, `ScannerCapabilityCatalog`, `CoverageSemantics`, `OperationCoverageTracker`
- `TechnicalObjectRecord`, `SqliteScanWorkspace`, `SqlKeyInventory`
- `SqlServerStructureCollector`, `PhcCustomizationsCollector`, `PhcActivityState`

