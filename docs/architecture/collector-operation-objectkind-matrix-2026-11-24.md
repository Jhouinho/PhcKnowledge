# Matriz estática Collector / Operation / ObjectKind — PHC Knowledge

**Data:** 2026-11-24  
**Repositório:** `Jhouinho/PhcKnowledge`  
**Branch:** `main`  
**Baseline analisada:** `dfb44b5ce4370ccf5b43856c87328f1915c58a99`  
**Estado:** levantamento estático do código; **não é ainda uma matriz validada por captura PHC nova**.

## 1. Objetivo e regra de leitura

Relacionar o catálogo de capabilities, as operações executadas pelos collectors, os `ObjectKind` emitidos e a resolução de `operationId` usada pelo Object Index v2.

Esta matriz distingue:
- operação declarada no catálogo;
- fonte/âmbito observado pelo collector;
- tipo de objeto que pode ser projectado no Object Index;
- factos transportados fora do Object Index;
- lacunas que exigem confirmação com uma captura real.

Fontes principais:
- `PhcKnowledgeScanner.Abstractions/Coverage/ScannerCapabilityCatalog.cs`
- `PhcKnowledgeScanner.Abstractions/Coverage/KnownOperations.cs`
- `PhcKnowledgeScanner.Abstractions/Inventory/ObjectKind.cs`
- `PhcKnowledgeScanner.Export/ObjectIndex/ObjectOperationResolver.cs`
- `PhcKnowledgeScanner.Export/ObjectIndex/ObjectIndexProjector.cs`
- `PhcKnowledgeScanner.Collectors/Phc/PhcCustomizationsCollector.cs`
- `PhcKnowledgeScanner.Collectors/Phc/PhcDeclaredRelationsCollector.cs`
- `PhcKnowledgeScanner.Collectors/Sql/SqlServerStructureCollector.cs`

## 2. SQL Server — estrutura

| Collector | OperationId | ObjectKind resolvido | Fonte / âmbito observado | Declaração |
|---|---|---|---|---|
| `sql.server.structure` | `sql.server.structure.database` | `database` | Metadados da base de dados | Obrigatória |
| `sql.server.structure` | `sql.server.structure.tables.inventory` | `table` | Inventário de tabelas físicas | Obrigatória |
| `sql.server.structure` | `sql.server.structure.primary_keys` | `primary_key` | Chaves primárias | Obrigatória |
| `sql.server.structure` | `sql.server.structure.foreign_keys` | `foreign_key` | Chaves estrangeiras | Obrigatória |
| `sql.server.structure` | `sql.server.structure.views` | `view` | Views e tentativa de recolha da definição | Obrigatória |
| `sql.server.structure` | `sql.server.structure.procedures` | `stored_procedure` | Stored procedures e tentativa de recolha da definição | Obrigatória |
| `sql.server.structure` | `sql.server.structure.triggers` | `trigger` | Triggers DML associadas a tabelas; exclui triggers DML em views e triggers DDL | Obrigatória; capability v2.0 |

**Limite importante:** a operação de triggers declara explicitamente um âmbito restrito. Não interpretar o resultado como inventário global de todos os triggers SQL Server.

## 3. PHC — personalizações

Todas as linhas pertencem ao collector `phc.customizations`. As variantes PHC são verificadas pelo `ObjectOperationResolver` através da propriedade `source` em `SummaryJson`; não se deve inferir a operação apenas pelo nome ou pelo tipo.

| OperationId | ObjectKind | source esperado | Origem principal | Declaração |
|---|---|---|---|---|
| `phc.customizations.brule` | `phc.user_rule` | `brule` | `dbo.brule` | Obrigatória |
| `phc.customizations.ebrule` | `phc.web_user_rule` | `ebrule` | `dbo.ebrule` | Opcional |
| `phc.customizations.teclas` | `phc.hotkey` | `ti` | `dbo.ti`, com apoio de `dbo.tc` | Obrigatória |
| `phc.customizations.udefs` | `phc.default_value` | `udefs` | `dbo.udefs` | Obrigatória |
| `phc.customizations.eudefs` | `phc.web_default_value` | `eudefs` | `dbo.eudefs` | Opcional |
| `phc.customizations.eventos` | `phc.event` | `eventos` | `dbo.eventos` | Obrigatória |
| `phc.customizations.eeventos` | `phc.web_event` | `eeventos` | `dbo.eeventos` | Opcional |
| `phc.customizations.user_fields` | `phc.user_field` | `sys.columns` | Catálogos SQL Server; colunas físicas `u_*` | Obrigatória |
| `phc.customizations.escr` | `phc.web_script` | `escr` | `dbo.escr` | Opcional |
| `phc.customizations.jsu` | `phc.user_javascript` | `jsu` | `dbo.jsu` | Opcional |
| `phc.customizations.cssjs` | `phc.web_cssjs` | `cssjs` | `dbo.cssjs` | Opcional |
| `phc.customizations.formc` | `phc.hidden_grid` | `formc` | `dbo.formc`, focado em grelhas/objectos ocultos com `Xexpressao` | Opcional |
| `phc.customizations.formb` | `phc.custom_screen` | `formb` | `dbo.formb` | Opcional |
| `phc.customizations.ftu` | `phc.user_tsql_function` | `ftu` ou `sys.objects` | `dbo.ftu` e funções instaladas `Fx_*` / `User_*` | Opcional |

**Limites importantes:**
- `teclas` é uma operação conjunta; `tc` é fonte auxiliar e não tem `ObjectKind` independente nesta projeção.
- `user_fields` observa colunas físicas `u_*`; não representa por si só os metadados funcionais de `dbo.campos`.
- `formc` não equivale a inventariar toda a tabela `formc`; o collector está focado no subconjunto documentado.
- `ftu` combina a tabela PHC e a descoberta de funções no catálogo SQL; validar os discriminadores reais em captura.
- Operações opcionais têm de distinguir estrutura ausente/não aplicável, zero resultados, falha e recolha não tentada. A palavra “opcional” não significa “completa”.

## 4. Relações declaradas PHC — fora do Object Index

| Collector | OperationId | Fonte | Modelo de destino | ObjectKind |
|---|---|---|---|---|
| `phc.declared-relations` | `phc.declared-relations.rel` | `dbo.rel` | Factos `DeclaredRelationRecord` | Não aplicável |
| `phc.declared-relations` | `phc.declared-relations.dtl` | `dbo.dtl` | Factos `DeclaredRelationRecord` | Não aplicável |

REL/DTL são factos declarados preservados num modelo próprio; não são convertidos em `TechnicalObjectRecord` nem mapeados por `ObjectOperationResolver` para `ObjectKind`. A sua cobertura continua a ser transportada pela operação correspondente. Não os confundir com Foreign Keys SQL nem com relações derivadas.

## 5. ObjectKinds declarados sem mapeamento de operação

O catálogo `ObjectKind` contém os seguintes valores para os quais o `ObjectOperationResolver` actualmente não define uma operação:

| ObjectKind | Situação estática |
|---|---|
| `schema` | Constante declarada; sem operação/mapeamento no resolver |
| `column` | Constante declarada; sem operação/mapeamento no resolver |
| `index` | Constante declarada; sem operação/mapeamento no resolver |
| `phc.campo_definition` | Constante declarada para metadados de `dbo.campos`; sem operação/mapeamento no resolver |

Isto não prova que estes tipos sejam emitidos numa captura actual. Significa que, se forem fornecidos como `TechnicalObjectRecord` ao Object Index, a resolução falha explicitamente em vez de inventar um `operationId`. A decisão sobre os tornar capacidades emitidas exige evidência e aprovação; não os adicionar implicitamente nesta matriz.

## 6. Regras de coerência impostas pelo Object Index v2

Para cada objeto projectado:
1. `ObjectKind` e `CollectorId` têm de corresponder a um mapeamento explícito.
2. Para objetos PHC, `SummaryJson.source` tem de existir e corresponder exactamente à variante esperada.
3. O `operationId` resolvido tem de existir exactamente uma vez no catálogo de capabilities e estar declarado como suportado.
4. Tem de existir exactamente uma entrada de coverage para a operação.
5. Versão da capability, collector de proveniência, estado da execução e contagem observada não podem contradizer os objetos projectados.
6. Uma operação não resolvida ou cobertura inconsistente impede a projeção; não é permitido atribuir uma operação aproximada.

O catálogo estático declara 23 operações: 7 de estrutura SQL, 14 de personalizações PHC e 2 de relações declaradas. A presença no catálogo não prova que a operação tenha sido executada ou que tenha produzido objetos numa captura concreta.

## 7. O que falta para fechar a matriz

Esta versão é uma leitura estática do código. Não fecha a classificação operacional nem autoriza alterações aos collectors.

Com uma captura nova produzida pelo Scanner actualizado numa instalação PHC de teste, será necessário:
- cruzar capabilities declaradas com coverage efectivamente emitida;
- confirmar `requested`, resultado, contagens e razões de ausência por operação;
- confirmar os `ObjectKind`, `CollectorId`, `SummaryJson.source` e `operationId` dos objetos reais;
- validar separadamente artefactos/definições disponíveis, vazios, indisponíveis e não recolhidos;
- confirmar REL e DTL como factos separados e não como ObjectKinds;
- identificar diferenças de versão PHC, permissões e estruturas opcionais;
- documentar qualquer divergência com evidência, antes de propor alterações.

**Não alterar collectors, queries, workspace SQLite, schema de captura ou semântica de recolha antes desta validação e de decisão explícita.**

## 8. Conclusão

O contrato actual tem um caminho explícito para mapear 21 ObjectKinds para operações de observação, enquanto 4 constantes de ObjectKind não têm mapeamento no resolver. REL/DTL seguem um percurso separado. Esta é uma conclusão de código, não uma afirmação sobre o conteúdo de uma instalação PHC específica.

Próximo passo: validar esta matriz estática com uma captura nova. Só depois fechar a matriz como baseline operacional e passar ao contrato físico de materialização SQL Server.
