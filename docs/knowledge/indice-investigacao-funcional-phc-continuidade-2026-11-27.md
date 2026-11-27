# PHC Gestão — índice de investigação funcional e continuidade

**Data de consolidação:** 27-11-2026  
**Base de referência:** `CSDefault202603`, salvo indicação em contrário  
**Finalidade:** organizar as investigações funcionais realizadas nos últimos dias e permitir a sua continuação sem depender de um único histórico de conversa.  
**Estado:** índice de continuidade; não substitui os relatórios técnicos de cada circuito.

## 1. Regras de leitura e evidência

Este documento é um mapa de navegação e estado. Não transforma notas incompletas em conclusões técnicas.

Manter sempre separadas:

1. **Estrutura/documentação:** Dic, Rel, Enciclopédia e documentação oficial.
2. **Código:** comportamento expresso em triggers, procedures, funções, views e código aplicacional.
3. **Dados da instalação:** resultados concretos de consultas read-only à CSDefault202603.
4. **Interpretação funcional:** explicação que resulta do cruzamento das fontes.
5. **Por confirmar:** hipóteses, lacunas e resultados ainda não recuperados.

As relações PHC declaradas (REL/DTL) não são foreign keys SQL. A ausência de uma escrita num ficheiro de triggers não prova que a aplicação PHC ou outro processo não a execute. A análise estática não equivale a validação em runtime.

**Metodologia acordada:** consultar primeiro Dic, Rel, triggers, Enciclopédia e fontes públicas verificáveis. Pedir consultas SQL apenas para responder a dúvidas concretas sobre dados, configuração ou comportamento físico da instalação; não pedir novamente estrutura que já possa ser obtida dos recursos existentes.

## 2. Estado do GitHub em 27-11-2026

A `main` consultada está no commit:

`3e4c2b980531abb63006c0427d5d19950bbac470` — `docs(readme): link VO and internal dossier recovery audit`.

### 2.1 Conteúdo já integrado na main

| Área | Documento | Estado |
|---|---|---|
| Tesouraria de faturação e cheques | [Investigação funcional CSDefault — FT, FI e TD](../audits/investigacao-funcional-csdefault-ft-fi-td-2026-11-25.md) | Integrado; análise estática e configuração consultada |
| Lacunas de VO e dossiers internos | [Recuperação da investigação funcional — VO e dossiers internos](../audits/recuperacao-investigacao-funcional-vo-dossiers-2026-11-25.md) | Integrado; regista explicitamente o que ainda não estava demonstrado |

O relatório FT/FI/TD fecha uma passagem de leitura estática/configuracional do circuito FT → OL e dos ramos de cheques CH/CHL. Não declara validação transaccional: as tabelas transaccionais relevantes estavam sem registos na consulta reportada e os triggers instalados não puderam ser comparados com o texto do script standard.

### 2.2 Trabalho documentado em pull requests abertos

| PR | Conteúdo | Estado observado |
|---|---|---|
| [#34 — Stocks, transferências, produção e SX/SXX](https://github.com/Jhouinho/PhcKnowledge/pull/34) | Handoff funcional de BI/FI/FN, SL, ST/SA, transferências, produção, reservas e investigação de grelhas SX/SXX | Aberto, não merged |
| [#35 — Investigação inicial de BO](https://github.com/Jhouinho/PhcKnowledge/pull/35) | Primeira passagem de BO, validações de assinatura/importação e limpeza de BOTXT | Aberto, não merged |

O PR #34 contém:
- `docs/knowledge/phc-stock-production-investigation-handoff-2026-11-25.md`;
- `docs/audits/investigacao-tecnica-stock-grelhas-sx-sxx-2026-11-26.md`.

O PR #35 contém:
- `docs/audits/investigacao-funcional-csdefault-bo-2026-11-26.md`;
- actualização do README para indexar o relatório BO.

O PR #33 foi fechado sem merge. O PR #34 é a versão posterior da consolidação de stocks e acrescenta a investigação SX/SXX; não tratar o PR #33 como conteúdo integrado na main.

**Regra de estado:** os documentos dos PRs #34 e #35 são conteúdo disponível para revisão, mas não fazem parte da main enquanto os PRs não forem integrados. Não declarar esses trabalhos publicados na main.

## 3. Mapa dos circuitos investigados

### 3.1 Tesouraria — FT/FI/TD, OL e cheques

**Fonte canónica:** relatório FT/FI/TD integrado na main.

Elementos registados:
- `TD.lancaol` condiciona o ramo de criação de OL a partir de FT.
- O ramo de criação de OL depende de correspondências em TD, FT2, FT3 e OC.
- A criação de CH a partir de FT é um ramo distinto, com condições próprias.
- A aceitação inicial do cheque cria histórico CHL (`cm=1`) e não cria, nesse ramo, um segundo OL.
- Outros movimentos CHL (`cm=3`) podem originar OL sob condições específicas; a semântica funcional geral de `cm=3` permanece por confirmar.
- Na CSDefault202603, a configuração consultada para Fatura-Recibo e Fatura Simplificada aponta para `olcodigo=R10001`, local `Caixa`, moeda EURO e contado 0. O registo OC identifica R10001 como Recebimentos de Clientes.
- A análise do script não demonstrou reconciliação automática dos OL existentes quando se altera a configuração TD.

**Limites:** leitura de código standard e configuração comunicada pelo utilizador; não houve validação transaccional com documentos reais nem comparação integral com os triggers instalados.

### 3.2 Tesouraria — OD, BA e FE

**Estado:** investigação recuperada parcialmente do histórico de conversa; ainda não existe relatório correspondente na main nem nos PRs recentes aqui identificados.

A informação disponível nesta consolidação é:

| Tabela | Leitura actual | Limite |
|---|---|---|
| `OD` | Movimentos previstos de tesouraria | O mecanismo concreto de passagem de OD para OL não está identificado |
| `OL` | Movimentos efectivos de tesouraria | A origem pode variar conforme o circuito; não assumir que todos os OL vêm de OD |
| `BA` | Estrutura auxiliar associada a movimentos OL e a operações bancárias/reconciliação | Não é uma réplica integral de OL |
| `FE` | Estrutura de saldos diários de caixa | O efeito operacional do fecho e as regras de bloqueio continuam por esclarecer |

#### BA — observações recuperadas

Foram identificados na estrutura os campos `extracto`, `reco`, `marcada`, `brstamp`, `olcodigo` e `olstamp`, e índices para a chave própria, ligação a OL, extracto, contado e data.

A leitura do trigger `tr_ol_*` disponível na conversa indica sincronização de BA com OL sob a condição `contado > 0`. O trigger analisado não preenche nem sincroniza todas as colunas BA, incluindo `olcodigo`, `extracto`, `reco`, `brstamp` e `marcada`.

**Não concluir:** que os triggers OL implementam todo o processo de reconciliação bancária, ou que BA seja uma cópia integral de OL.

#### FE — observações recuperadas

A estrutura analisada indica:
- `data` como chave primária;
- índice único em `festamp`;
- campos `saldof` e `esaldof` para saldos;
- campos `fechado` e `quemfechou` relacionados com o fecho;
- registo organizado por dia, não por movimento.

A leitura do trigger `tr_ol_*` indica actualização dos saldos FE a partir de movimentos OL com `local = 'C'`.

**Por confirmar:** se e onde o PHC impede alterações a movimentos de caixa de dias fechados, e se o controlo é feito noutros triggers, procedures ou código aplicacional.

#### OD — origens e passagem para OL por resolver

Os apontamentos recuperados não são consistentes quanto ao inventário das origens de OD:
- uma nota refere `CC`, `FC`, `LM`, `LO` e `VY`;
- outra referência histórica menciona `CC`, `FC`, `LR`, `LP`, `VY` e `CHF`.

Não consolidar nenhuma das listas como inventário final até rever os triggers e as fontes originais.

O ponto de investigação em aberto é identificar o mecanismo concreto que transforma uma previsão em OD num movimento efectivo em OL. Foram propostas consultas para:
1. listar triggers directamente associados a OD;
2. procurar triggers com dependências simultâneas de OD e OL.

**Não há resultados dessas consultas registados no contexto recuperado.** A ausência de dependências em `sys.sql_expression_dependencies`, se ocorrer, não exclui código aplicacional, SQL dinâmico, procedures ou triggers sem dependências detectáveis.

**Próximo passo:** rever Dic/Rel, triggers e Enciclopédia; confirmar a lista de origens; só depois pedir/executar consultas read-only dirigidas às lacunas concretas.

### 3.3 Stocks — movimentos, saldos e reservas

**Fonte de continuidade:** documento de handoff no PR #34.

Modelo de trabalho:
- `SL`: movimentos de stock;
- `ST`: informação base do artigo e acumulados globais;
- `SA`: saldos por artigo/armazém;
- reservas/cativações e quantidades auxiliares: compromissos ou estados quantitativos que não devem ser confundidos com movimentos físicos.

Circuitos de origem identificados no SQL analisado:
- BI pode originar SL, condicionado pela configuração do dossier em TS/TS2 e pelo estado da linha;
- FI pode originar SL segundo `TD.lancasl` e códigos configurados;
- FN pode originar SL segundo `CM1.folansl`;
- a valorização SV é um circuito distinto, embora relacionado com SL.

Transferências BI:
- a lógica analisada pode criar dois movimentos SL, um para o armazém de origem e outro para o de destino;
- `SL.trfa` recebe tratamento específico nos acumulados;
- os códigos e sentidos devem ser lidos em conjunto com CM2 e a configuração TS/TS2, não apenas pelo nome dos campos.

Produção:
- condições de produção influenciam a selecção do código de movimento;
- a documentação Manufactor consultada dá contexto funcional, mas não pode ser generalizada automaticamente ao PHC Gestão/CS;
- não está demonstrado o circuito completo de consumo de componentes e entrada de produto acabado;
- as tabelas auxiliares OFC, OCI, OMI, OCP e OPT não devem ser classificadas apenas pelo nome.

Reservas/cativações não são saídas físicas: procurar o movimento SL para demonstrar consumo/entrada física.

### 3.4 Stocks por grelha — SX/SXX e inventário

**Fonte:** relatório técnico SX/SXX no PR #34.

Elementos identificados:
- a view `V_WS_STKARTIGOS_GRELHAS` consulta SX para apresentar saldo por artigo/armazém/cor/tamanho;
- a view `V_WS_STKARTIGOS` consulta SA para stock por artigo/armazém;
- os triggers SL analisados actualizam ST/SA, mas não demonstram escrita de saldos em SX/SXX;
- os triggers SGC/SGT encontrados alteram/eliminam referências de cor/tamanho em SX, sem demonstrar cálculo de `SX.stock`;
- STIC/STIL contêm dados do inventário físico, mas a rotina que transforma o inventário em movimentos e actualiza SX não foi localizada;
- `usp_stinvclc` não foi demonstrada como responsável por SX/SXX;
- SXX continua parcialmente identificada; não assumir que é agregação ou cópia de SX;
- `ST.textteis` é uma pista cuja influência operacional permanece por demonstrar.

**Questão principal em aberto:** quem cria e mantém efectivamente as combinações e os saldos de SX, e qual o papel operacional de SXX?

### 3.5 Dossiers internos — BO

**Fonte:** primeira passagem no PR #35.

Elementos registados:
- a contagem comunicada de `dbo.BO` na CSDefault202603 foi zero;
- foram identificados no script `tr_bo_insert`, `tr_bo_update` e `tr_bo_delete`;
- INSERT/UPDATE incluem validações relacionadas com assinatura digital/documentos importados, envolvendo BO, BO2, TS e TS2;
- DELETE limpa registos BOTXT associados por `bostamp`.

**Limites:** o circuito BO não está fechado. A expressão completa da validação de assinatura/importação deve ser reconfirmada no script. Faltam o cruzamento integral com Dic/Rel e Enciclopédia, a leitura completa dos triggers e a identificação dos efeitos sobre BI, stock, tesouraria e dossiers relacionados. A contagem zero limita a validação empírica nesta captura, mas não prova inutilização da tabela.

## 4. Lacunas transversais e regras para evitar conclusões erradas

1. **Script vs instalação:** o script standard legível não foi comparado integralmente com os triggers instalados, alguns dos quais estão encriptados.
2. **Ausência de dados:** contagens zero em tabelas transaccionais impedem validar percursos reais; não provam ausência de funcionalidade.
3. **Relações PHC:** REL/DTL são relações declaradas, não prova automática de FK SQL nem de existência de registos relacionados.
4. **OD:** as listas de origens recuperadas divergem; não consolidar sem nova verificação.
5. **BA:** sincronização parcial pelo trigger OL não prova o ciclo completo de reconciliação.
6. **FE:** a existência de campos de fecho não demonstra, por si só, a regra de bloqueio de movimentos em dias fechados.
7. **SX/SXX:** não atribuir a manutenção de SX aos triggers SL nem classificar SXX como totalização sem prova.
8. **Produção:** não generalizar documentação Manufactor para PHC CS.
9. **BO:** a primeira passagem não prova transformação de BO em BI nem efeitos em stock/tesouraria.
10. **Autofacturação:** o caso observado é específico; não o generalizar a todos os documentos FN.

## 5. Sequência de continuação recomendada

### Prioridade 1 — recuperar e fechar a investigação OD/BA/FE

1. Recuperar a conversa integral/exportada e preservar os resultados SQL originais.
2. Rever Dic/Rel, Enciclopédia e triggers relativos a OD, OL, BA e FE.
3. Confirmar o inventário de origens de OD e corrigir a divergência dos apontamentos.
4. Identificar procedures, funções, views, triggers e código aplicacional que possam criar OL a partir de OD.
5. Confirmar o papel de BA na reconciliação e o comportamento de FE em dias fechados.
6. Só pedir consultas read-only específicas para as questões que não sejam resolvidas documentalmente.

### Prioridade 2 — rever e integrar a investigação de stocks

1. Rever o PR #34 e confirmar as fontes/referências do handoff.
2. Fechar transferências com TS/TS2, CM2, BI e SL.
3. Continuar a procura da rotina que mantém SX.stock e esclarecer SXX.
4. Separar consumo físico, reservas/cativações e valorização.
5. Aprofundar produção apenas com fontes específicas da versão/módulo PHC em análise.

### Prioridade 3 — aprofundar BO

1. Consultar Dic e Rel antes de novas queries de estrutura.
2. Revalidar integralmente os triggers BO no ficheiro actual do repositório.
3. Cruzar as validações de assinatura/importação com a Enciclopédia.
4. Identificar, com evidência, os efeitos sobre linhas, stock, tesouraria e dossiers relacionados.
5. Manter a investigação aberta até haver evidência suficiente.

## 6. Governação documental

- Este índice aponta para os relatórios específicos; não os substitui nem duplica o seu detalhe técnico.
- Novas conclusões devem indicar fonte, versão/commit, proveniência e nível de evidência.
- Uma hipótese só passa a conclusão quando houver evidência suficiente.
- Não alterar código PHC, dados da instalação ou contratos do produto no âmbito desta investigação documental.
- Não integrar PRs automaticamente: cada PR deve ser revisto e integrado segundo o fluxo normal do repositório.
- Quando a exportação da conversa estiver disponível, actualizar este índice com a cronologia, resultados SQL exactos e referências originais em falta.

## 7. Ponto exacto de retoma

A próxima investigação funcional a retomar é **OD → OL**, incluindo a relação de OL com BA e FE. O mecanismo de concretização de OD em OL ainda não está identificado e as consultas propostas no histórico não têm resultados recuperados.

Em paralelo, PR #34 contém a consolidação de stocks e PR #35 a primeira passagem de BO. Ambos permanecem fora da main até revisão e merge explícitos.

Este documento é uma fotografia de continuidade em 27-11-2026, não uma declaração de que todos os circuitos PHC estão fechados.
