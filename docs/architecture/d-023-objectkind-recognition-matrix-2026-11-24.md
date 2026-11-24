# D-023 — Matriz de reconhecimento por ObjectKind (v0.1)

**Data:** 24-11-2026  
**Estado:** primeira matriz documental; evidência estática e resultados agregados de duas capturas reais. **Não aprovada como política automática de matching nem como matriz operacional final.**  
**Referências:** [D-023 — Identidade e correspondência cross-installation](d-023-cross-installation-identity-correspondence-contract.md); [P1.B — SourceIdentity e scopes](p1b-source-identity-scope-matrix-2026-11-06.md); [Matriz Collector/Operation/ObjectKind](collector-operation-objectkind-matrix-2026-11-24.md); [Avaliação CS default / dfblock](../audits/identity-provenance-cross-capture-assessment-2026-11-24.md).

## 1. Finalidade e limites

Esta matriz traduz D-023 em critérios por tipo de objecto. Distingue:

- **identidade local:** continuidade de uma entidade dentro da mesma Installation;
- **correspondência cross-installation:** relação entre identidades locais independentes;
- **correspondência com PHC Standard:** relação com uma entrada do catálogo central, versionado;
- **comparação de definição:** igualdade/diferença de definição, separada da identidade;
- **classificação:** StandardIntact, StandardModified, InstallationDevelopment ou Unknown.

A matriz não autoriza fusão de identidades, classificação automática, alterações ao Scanner/SQLite/IKP ou materialização SQL Server.

### 1.1 Escala de evidência usada

| Nível descritivo | Significado |
|---|---|
| Evidência nativa contextual | Identificador emitido pela origem com scope documentado; só vale dentro desse scope e semântica |
| Candidato estrutural | Nome, parent, schema, colunas ou relações permitem propor correspondência; não provam identidade |
| Evidência comparativa observada | Padrão efectivamente encontrado nas duas capturas CS default/dfblock; não generaliza para outras versões |
| Não disponível | O Scanner actual não recolhe evidência suficiente ou a captura não a demonstrou |
| Por validar | Existe hipótese plausível, mas falta validação de unicidade, estabilidade, cobertura ou semântica |

Não se atribuem scores numéricos. A palavra «forte» não deve ser aplicada a uma família inteira sem prova de unicidade e estabilidade para o scope em causa.

## 2. Evidência real disponível

A comparação CS default / dfblock, realizada com a mesma build do Scanner, reportou:

- 17 182 e 18 954 objectos no Object Index, respectivamente;
- 63 e 881 artefactos;
- 8 294 e 9 601 tabelas físicas;
- 0 e 598 campos físicos `u_*`;
- 12 correspondências cross-capture com `phc.stamp`, nos kinds `phc.custom_screen` (`formb`) e `phc.hidden_grid` (`formc`);
- cerca de 7 933 tabelas comuns por nome/`SourceObjectId`; esta é correspondência observacional, não identidade persistente;
- `sqlserver.object_id` não corresponde entre bases de dados diferentes;
- não foram disponibilizados DefinitionFingerprints semânticos: as definições elegíveis ficaram `Unavailable / NoValidatedPolicy`;
- procedures e triggers ficaram com cobertura Partial; na dfblock, 540 triggers estavam sem definição, sem causa diagnosticada.

Esta evidência é agregada. Não inclui a listagem individual de todos os objectos, stamps, definições ou colisões. Não permite declarar unicidade global nem classificar objectos individuais.

## 3. Matriz SQL Server

| ObjectKind | Evidência actual / scope | Continuidade na mesma Installation | Correspondência entre Installations | Comparação de definição | Classificação possível nesta fase | Estado |
|---|---|---|---|---|---|---|
| `database` | `databaseId` não projectado como SourceIdentity; falta scope inequívoco da instância SQL | Contexto de Installation é atribuído pela plataforma, não por databaseId | Não comparar por databaseId ou nome isolado | Não aplicável como definição de objecto | Unknown até catálogo/contexto e âmbito estarem estabelecidos | Não disponível |
| `table` | `sqlserver.object_id`, scope `database:<database-name>`; nome/schema e estrutura são atributos | Candidato contextual dentro da mesma base; drop/recreate pode mudar object_id | object_id não atravessa bases. Nome/schema/estrutura apenas geram candidato | Sem política semântica validada nesta evidência | Unknown; não classificar como standard ou desenvolvimento pelo nome/exclusividade | Evidência contextual + candidato estrutural |
| `primary_key` | Nome, tabela, colunas e uniqueIndexId; sem SourceIdentity nativa aprovada | Comparação estrutural pode propor candidato; renomeação/recriação ambígua | Nome/tabela/colunas são candidatos, não identidade | Comparação estrutural ainda sem contrato de equivalência validado | Unknown | Não disponível como identidade nativa |
| `foreign_key` | Nome, tabela pai/referenciada, colunas e acções; SELECT actual não recolhe object_id da FK | Relação estrutural pode ser comparada, sem provar continuidade | Não usar nome ou conjunto de colunas como identidade global | Comparação estrutural apenas descritiva | Unknown | Não disponível como identidade nativa |
| `view` | Nome/schema; object_id não está no registo actual; definição pode estar disponível ou indisponível | Nome/schema geram candidato; continuidade não comprovada | Nome/schema/definição podem gerar candidato, não confirmação automática | DefinitionFingerprint semântico indisponível sem política validada | Unknown | Evidência insuficiente |
| `stored_procedure` | Nome/schema; object_id não está no registo actual | Nome/schema geram candidato; não provam continuidade | Nome/schema/conteúdo não fundem identidades | Fingerprint semântico indisponível; cobertura Partial deve ser respeitada | Unknown | Evidência insuficiente |
| `trigger` | Nome/schema/tabela; object_id não está no registo actual; operação cobre apenas triggers DML associadas a tabelas | Relação com tabela pai é contexto, não identidade | Não generalizar para triggers DDL ou triggers DML em views; nome é candidato | Fingerprint semântico indisponível; 540 definições ausentes na dfblock sem causa conhecida | Unknown | Parcial / âmbito restrito |

### 3.1 Regras específicas SQL

1. `sqlserver.object_id` é evidência nativa contextual à base de dados. Nunca é chave cross-installation.
2. Nome/schema iguais podem criar candidatos; não confirmam identidade nem correspondência PHC Standard.
3. Uma tabela que só aparece na dfblock não é automaticamente `InstallationDevelopment`: é necessário catálogo standard com versão/edição e cobertura suficiente.
4. PK/FK são estruturas relacionais. Não confundir igualdade da relação observada com identidade de uma constraint.
5. A ausência de definição de view/procedure/trigger não é ausência do objecto.
6. A operação de triggers não representa todos os triggers SQL Server; respeitar o âmbito declarado no catálogo de capabilities.

## 4. Matriz PHC — objectos com stamp

O contrato P1.B projecta `phc.stamp` com scope por tabela de origem. A comparação real demonstrou 12 correspondências apenas em `formb` e `formc`. Para as restantes famílias, a existência de stamp no modelo de emissão não prova unicidade/estabilidade cross-installation.

| ObjectKind | Fonte / operação | SourceIdentity prevista | Evidência cross-capture observada | Critério permitido nesta fase | Estado |
|---|---|---|---|---|---|
| `phc.user_rule` | `dbo.brule` / `phc.customizations.brule` | `phc.stamp`, scope `phc.table:dbo.brule` | Não reportada | Stamp pode propor candidato; validar unicidade/estabilidade e correspondência ao catálogo antes de confirmar | Por validar |
| `phc.web_user_rule` | `dbo.ebrule` / `phc.customizations.ebrule` | `phc.stamp`, scope `phc.table:dbo.ebrule` | Não reportada | Igual regra; não inferir correspondência pela semelhança com brule | Por validar |
| `phc.hotkey` | `dbo.ti` com apoio de `dbo.tc` / `phc.customizations.teclas` | `phc.stamp` derivado de `tistamp`, scope `phc.table:dbo.ti` | Não reportada | Avaliar semântica de tistamp e associação a tc; não tratar tc como objecto independente | Por validar |
| `phc.default_value` | `dbo.udefs` / `phc.customizations.udefs` | `phc.stamp`, scope `phc.table:dbo.udefs` | Não reportada | Stamp candidato; confirmar semântica e cobertura | Por validar |
| `phc.web_default_value` | `dbo.eudefs` / `phc.customizations.eudefs` | `phc.stamp`, scope `phc.table:dbo.eudefs` | Não reportada | Stamp candidato; não assumir equivalência com udefs | Por validar |
| `phc.event` | `dbo.eventos` / `phc.customizations.eventos` | `phc.stamp`, scope `phc.table:dbo.eventos` | Não reportada | Stamp candidato; validar estabilidade por versão | Por validar |
| `phc.web_event` | `dbo.eeventos` / `phc.customizations.eeventos` | `phc.stamp`, scope `phc.table:dbo.eeventos` | Não reportada | Stamp candidato; não assumir equivalência com eventos | Por validar |
| `phc.web_script` | `dbo.escr` / `phc.customizations.escr` | `phc.stamp`, scope `phc.table:dbo.escr` | Não reportada | Stamp candidato; definição e semântica continuam separadas | Por validar |
| `phc.user_javascript` | `dbo.jsu` / `phc.customizations.jsu` | `phc.stamp`, scope `phc.table:dbo.jsu` | Não reportada | Stamp candidato; validar semântica e âmbito | Por validar |
| `phc.web_cssjs` | `dbo.cssjs` / `phc.customizations.cssjs` | `phc.stamp`, scope `phc.table:dbo.cssjs` | Não reportada | Stamp candidato; não inferir igualdade de comportamento | Por validar |
| `phc.hidden_grid` | `dbo.formc` / `phc.customizations.formc` | `phc.stamp`, scope `phc.table:dbo.formc` | **12 correspondências agregadas em conjunto com formb**; detalhe individual não presente neste relatório | Evidência comparativa observada. Antes de confirmar cada par, verificar unicidade por tabela, duplicados e consistência do registo | Evidência observada; validação individual pendente |
| `phc.custom_screen` | `dbo.formb` / `phc.customizations.formb` | `phc.stamp`, scope `phc.table:dbo.formb` | **12 correspondências agregadas em conjunto com formc**; detalhe individual não presente neste relatório | Evidência comparativa observada. Antes de confirmar cada par, verificar unicidade por tabela, duplicados e consistência do registo | Evidência observada; validação individual pendente |
| `phc.user_tsql_function` (variante `ftu`) | `dbo.ftu` / `phc.customizations.ftu` | `phc.stamp`, scope `phc.table:dbo.ftu` | Não reportada | Stamp candidato; validar variante e unicidade | Por validar |
| `phc.user_tsql_function` (variante `sys.objects`) | Funções `Fx_*` / `User_*` | Sem SourceIdentity; SELECT não recolhe object_id | Não reportada | Nome/schema apenas referência sintética; não emitir identidade nativa | Não disponível |

**Nota sobre as 12 correspondências:** a evidência agregada identifica os kinds `formb` e `formc`, mas não fornece aqui a distribuição exacta por kind, os valores de stamp nem a análise de colisões. Não atribuir 12 correspondências a cada kind nem confirmar pares individuais com base apenas neste resumo.

## 5. Objectos PHC e factos sem identidade de Object Index

### 5.1 ObjectKinds declarados sem operação mapeada

| ObjectKind | Situação | Critério |
|---|---|---|
| `schema` | Constante declarada sem operação/mapeamento no resolver | Não existe evidência de emissão na captura; não criar regra de reconhecimento |
| `column` | Constante declarada sem operação/mapeamento no resolver | Não confundir com `phc.user_field`, que é o tipo projectado para colunas físicas `u_*` |
| `index` | Constante declarada sem operação/mapeamento no resolver | PK e índice não são equivalentes; sem operação, não há reconhecimento no Object Index |
| `phc.campo_definition` | Constante declarada para metadados funcionais de `dbo.campos`, sem operação/mapeamento | Não confundir com coluna física `u_*`; sem evidência projectada, Unknown/não observado |

Estes valores não são declarados como observados nas duas capturas. A presença da constante no código não prova emissão.

### 5.2 `phc.user_field` — colunas físicas u_*

| Elemento | Evidência | Regra |
|---|---|---|
| Identidade nativa | `sqlserver.column_id`, scope `database:<database-name>/object:<parent-table-objectId>` | Contextual à tabela pai e base; não é identidade global |
| Parent | `parentSourceObjectId` da tabela | Só projectar SourceIdentity se o parent for resolvido inequivocamente |
| Captura real | 0 campos físicos `u_*` na CS default; 598 na dfblock | Demonstra diferença observada de inventário, não classificação automática de 598 desenvolvimentos |
| Cross-installation | column_id depende da tabela/base | Não comparar directamente entre instalações; nome da coluna e tabela geram candidatos |
| PHC Standard | Catálogo/cobertura de campos aplicável ainda não cruzados | Unknown até existir referência standard e cobertura suficientes |
| Definição | Metadados estruturais disponíveis conforme captura; sem política semântica cross-installation demonstrada | Não concluir equivalência funcional apenas por tipo/nome |

## 6. REL / DTL — fora do Object Index

REL e DTL são factos declarados transportados como `DeclaredRelationRecord`, não `TechnicalObjectRecord` nem ObjectKinds.

| Fonte | Evidência | Regra de reconhecimento |
|---|---|---|
| `dbo.rel` | Pode conter stamp; sem stamp, identificador derivado de atributos/hash | Não tratar hash derivado como SourceIdentity nativa; exige contrato próprio de identidade para factos declarados |
| `dbo.dtl` | Pode conter stamp; sem stamp, identificador derivado de atributos/hash | Mesma regra; distinguir facto declarado de FK SQL e de relação inferida |

Não atribuir `SameLogicalEntity` ou `SameStandardEntity` automaticamente a REL/DTL. A matriz actual apenas regista que necessitam de uma política própria, fora do Object Index.

## 7. Regras de decisão por tipo de correspondência

| Relação D-023 | Condição mínima nesta matriz | Não significa |
|---|---|---|
| `SameLogicalEntity` | Continuidade sustentada dentro da mesma Installation, com scope e evidência estáveis; tratar recriação/ambiguidade como nova identidade ou candidato pendente | Igualdade de definição |
| `SameStandardEntity` | Correspondência com entrada identificada do catálogo PHC Standard, versão/edição e cobertura aplicáveis, evidência específica do kind | StandardIntact |
| `RenamedFrom/To` | Evidência de continuidade para além da diferença de nome; decisão auditável | Que nome antigo e novo sejam sempre o mesmo objecto |
| `DerivedFrom` | Evidência explícita de derivação ou linhagem | Identidade comum |
| `Replaces` | Evidência funcional/administrativa de substituição | Identidade comum ou equivalência de definição |
| `RelatedTo` | Relação documentada sem equivalência | Qualquer fusão ou classificação automática |

Nome, schema, SourceObjectId, hash de bytes ou fingerprint semântico (quando exista) não fundem identidades. Fingerprint semântico compara definições segundo política validada; não substitui a identidade.

## 8. Classificação standard / instalação

A matriz não classifica nenhum objecto individual como standard intacto, standard alterado ou desenvolvimento da instalação. Para qualquer kind, aplicar as condições D-023:

- **StandardIntact:** correspondência confirmada ao item standard aplicável + comparação compatível segundo política validada.
- **StandardModified:** correspondência confirmada + divergência demonstrada por comparação válida.
- **InstallationDevelopment:** sem correspondência standard, catálogo/edição/versão/cobertura suficientes e evidência positiva de desenvolvimento da instalação.
- **Unknown:** qualquer condição necessária em falta.

Estado actual da classificação nas duas capturas: **Unknown / não avaliado individualmente**, mesmo para objectos que aparentem ter nome PHC ou que sejam exclusivos de uma das capturas. Esta opção é deliberada e evita converter ausência de evidência em conclusão.

## 9. Testes de validação exigidos

Antes de promover uma linha a política operacional, recolher e documentar:

1. **Unicidade:** duplicados do identificador no scope declarado em cada captura.
2. **Estabilidade intra-installation:** mesmo objecto em capturas sucessivas da mesma Installation, incluindo actualização PHC e caso de drop/recreate quando disponível.
3. **Separação cross-installation:** mesmo valor nativo em instalações distintas não causa fusão; as EntityIdentity permanecem independentes.
4. **Renomeação:** nome alterado com evidência suficiente; nome igual com objecto recriado.
5. **Colisão:** stamp repetido dentro da mesma tabela/scope e entre tabelas.
6. **Cobertura:** operação Available, Empty, Partial, Failed, NotPresent e Skipped tratadas sem inferir ausência indevida.
7. **Definição:** Available, Empty, Unavailable e NotCollected; fingerprint semântico só quando houver política validada.
8. **Catálogo standard:** versão/edição e cobertura explícitas; objecto ausente num catálogo incompleto mantém Unknown.
9. **Cardinalidade:** casos 1:1, 1:N e N:1 quando realmente observados, sem os fabricar.
10. **Auditoria:** confirmação, rejeição e revogação conservam evidências, actor/motivo quando humano, política e histórico.

## 10. Pendências e próximo passo

### Confirmado documentalmente
- D-023 separa identidades locais, catálogo standard, correspondências, comparação e classificação.
- O contrato P1.B define os schemes/scopes que o Scanner actual consegue projectar sem novas queries.
- A comparação CS default/dfblock encontrou 12 correspondências stamp em formb/formc e confirmou que SQL object_id não atravessa bases.
- A comparação por nome de tabelas é apenas observacional.
- Não existem fingerprints semânticos disponíveis nas capturas analisadas.

### Ainda por validar
- Distribuição exacta das 12 correspondências entre formb e formc, valores e duplicados de stamp.
- Unicidade e estabilidade dos stamps nas outras famílias PHC.
- Continuidade intra-installation entre capturas sucessivas.
- Relações de renomeação, derivação e substituição.
- Critérios e cobertura do catálogo PHC Standard por versão/edição.
- Semântica de REL/DTL cross-capture.
- Definições e causas das operações Partial, incluindo triggers sem definição.

### Próxima acção
Usar os pacotes originais CS default e dfblock, sem os alterar, para produzir uma listagem de evidências por objecto: `ObjectKind`, `SourceIdentity`, scope, `SourceObjectId`, parent, atributos estruturais, estado de cobertura, artefactos e fingerprint. Essa análise deve responder primeiro às pendências de formb/formc e tabelas SQL, sem fazer classificação standard ainda.

Só após essa análise se decide se a matriz pode ser promovida a baseline operacional e se há necessidade de alterar Scanner, IKP ou o modelo persistido.
