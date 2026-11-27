# PHC Gestão — índice de conhecimento funcional e plano de continuidade

**Data de consolidação:** 27-11-2026  
**Base principal:** `CSDefault202603`, salvo indicação em contrário  
**Finalidade:** ponto único de entrada para o conhecimento funcional PHC documentado nos últimos dias e preparação das próximas investigações.  
**Estado:** índice de continuidade; os relatórios específicos continuam a ser as fontes de detalhe.

## 1. Princípios de leitura e método

Este índice organiza o conhecimento; não converte notas incompletas em conclusões.

Em cada investigação, separar sempre:

1. **Estrutura/documentação:** Dic, Rel (REL/DTL), Enciclopédia e documentação oficial.
2. **Código:** comportamento expresso em triggers, procedures, funções, views e código aplicacional.
3. **Dados da instalação:** resultados concretos de consultas read-only, com base e contexto identificados.
4. **Interpretação funcional:** explicação resultante do cruzamento das fontes.
5. **Por confirmar:** hipóteses, lacunas e resultados ainda não recuperados.

### Regras metodológicas obrigatórias

- Consultar primeiro Dic, Rel, triggers, Enciclopédia e fontes públicas oficiais verificáveis.
- O utilizador já disponibilizou Dic e Rel; não voltar a pedir queries genéricas para redescobrir campos ou relações declaradas.
- Pedir consultas SQL apenas para responder a dúvidas concretas que não possam ser resolvidas documentalmente — por exemplo, confirmar dados, configuração ou comportamento físico de uma instalação.
- REL/DTL não são foreign keys SQL. A existência de uma relação declarada não prova que existam registos relacionados.
- A ausência de uma escrita num ficheiro de triggers não prova que a aplicação PHC ou outro objecto/processo não a execute.
- Código standard analisado não prova que a instalação tenha exactamente a mesma versão de trigger.
- Contagens zero limitam a validação empírica; não provam que a tabela ou funcionalidade seja inútil.
- Não generalizar comportamentos de uma base, versão ou módulo para todo o PHC.
- Não alterar dados PHC nem executar testes destrutivos. A investigação documental é read-only.
- Não versionar ZIPs de capturas ou da Enciclopédia, nem conteúdo proprietário de clientes.

## 2. Estado do conhecimento no GitHub

A última `main` consultada está no commit:

`3e4c2b980531abb63006c0427d5d19950bbac470` — `docs(readme): link VO and internal dossier recovery audit`.

### 2.1 Já integrado na main

| Domínio | Documento | Conclusão/limite |
|---|---|---|
| FT / FI / TD / OL / CH / CHL | [Investigação funcional FT/FI/TD](../audits/investigacao-funcional-csdefault-ft-fi-td-2026-11-25.md) | Leitura estática dos triggers standard e configuração consultada. Não houve validação transaccional com documentos reais. |
| VO / dossiers internos | [Auditoria de recuperação VO e dossiers](../audits/recuperacao-investigacao-funcional-vo-dossiers-2026-11-25.md) | Confirma a lacuna documental: não existe evidência recuperável suficiente para fechar VO nem o circuito integral dos dossiers internos. |

### 2.2 Documentação em PRs — ainda não integrada

| PR | Área | Conteúdo | Estado observado em 27-11-2026 |
|---|---|---|---|
| [#32 — dossiers, stock cativo e grafo](https://github.com/Jhouinho/PhcKnowledge/pull/32) | Dossiers / BI / BO / inventários / pré-cativação / grafo | Relatórios funcionais e grafo revisto funcional + estrutural | Aberto, não merged |
| [#34 — stocks, transferências e produção](https://github.com/Jhouinho/PhcKnowledge/pull/34) | BI / FI / FN / SL / ST / SA / SX / SXX | Handoff funcional e investigação técnica SX/SXX | Aberto, não merged |
| [#35 — investigação inicial BO](https://github.com/Jhouinho/PhcKnowledge/pull/35) | BO / BO2 / TS / TS2 / BOTXT | Primeira passagem de triggers e validações | Aberto, não merged |
| [#36 — índice de continuidade](https://github.com/Jhouinho/PhcKnowledge/pull/36) | Governação do conhecimento | Este índice e referência no README | Aberto, não merged |

Os PRs são trabalho documental separado. Não declarar os respectivos documentos como integrados na main até ocorrer merge. A actualização do grafo neste PR #32 não integra automaticamente os conteúdos dos PRs #34 e #35.

## 3. Mapa consolidado por domínio

### 3.1 Tesouraria de faturação — FT / FI / TD / OL

**Fonte canónica:** relatório FT/FI/TD integrado na main.

Conclusões documentadas:
- FT é o cabeçalho de faturação; FI contém as linhas; TD configura o comportamento por tipo de documento.
- O ramo FT → OL depende de `TD.lancaol=1` e das correspondências necessárias em TD, FT2, FT3 e OC.
- A criação de CH a partir de FT é um ramo independente, com condições próprias.
- Na configuração consultada de `CSDefault202603`, Fatura-Recibo (`ndoc=3`) e Fatura Simplificada (`ndoc=8`) usam `olcodigo=R10001`, local Caixa, moeda EURO e contado 0. OC classifica R10001 como Recebimentos de Clientes.
- A aceitação inicial de CH gera CHL (`cm=1`) e não cria, nesse ramo, um segundo OL.
- Alguns ramos CHL (`cm=3`) podem criar OL sob condições específicas; não generalizar a semântica de cm=3.
- O código standard analisado não demonstra reconciliação automática dos OL existentes quando se altera a configuração TD.
- Foi observada uma diferença no cálculo de OL.entr/eentr entre INSERT inicial e UPDATE/recuperação; não foi classificada como defeito.

Limites:
- FT, FT2, FT3, OL e CH não tinham registos na consulta reportada.
- Os triggers instalados estavam encriptados e não foram comparados integralmente com o script standard.
- Conclusões fechadas apenas para leitura estática/configuracional, não para validação transaccional.

### 3.2 Tesouraria — OD / OL / BA / FE

**Estado:** conhecimento recuperado parcialmente do histórico; falta relatório técnico completo e confirmação das fontes originais.

| Tabela | Conhecimento actual | Lacuna principal |
|---|---|---|
| OD | Movimentos previstos | Mecanismo concreto OD → OL não identificado |
| OL | Movimentos efectivos de tesouraria | Não assumir que todos os OL vêm de OD |
| BA | Estrutura auxiliar associada a movimentos OL e reconciliação | Sincronização observada é parcial; ciclo de reconciliação por fechar |
| FE | Saldos diários de caixa | Regra operacional de bloqueio em dias fechados por esclarecer |

BA:
- Foram identificados campos como `extracto`, `reco`, `marcada`, `brstamp`, `olcodigo` e `olstamp`.
- O trigger OL analisado sincroniza BA em condições como `contado > 0`, mas não actualiza todos os campos.
- BA não é réplica integral de OL.

FE:
- Estrutura diária com `data`, `festamp`, `saldof`, `esaldof`, `fechado` e `quemfechou`.
- O trigger OL analisado actualiza FE para movimentos com `local='C'`.
- Falta localizar onde o PHC impede ou permite alterações em dias fechados.

OD:
- As notas recuperadas divergem quanto às origens: uma lista menciona CC, FC, LM, LO e VY; outra menciona CC, FC, LR, LP, VY e CHF.
- Não consolidar qualquer lista até rever Dic/Rel, triggers e Enciclopédia.
- A ausência de dependências em `sys.sql_expression_dependencies` não excluiria código aplicacional, SQL dinâmico ou rotinas não detectadas.

**Próxima pergunta central:** qual é o mecanismo concreto que transforma uma previsão OD num movimento OL?

### 3.3 Dossiers internos — BO / BO2 / BI / BOTXT

**Fonte:** primeira passagem no PR #35; grafo e investigação de dossiers no PR #32.

Conclusões iniciais:
- A contagem comunicada de `dbo.BO` em `CSDefault202603` foi zero.
- Foram identificados `tr_bo_insert`, `tr_bo_update` e `tr_bo_delete`.
- INSERT/UPDATE incluem validações relacionadas com assinatura digital/documentos importados, envolvendo BO, BO2, TS e TS2.
- Campos observados na leitura inicial: `TS2.assinaimp`, `TS2.docsimport`, `TS2.assinadoc`, `BO2.assinatura` e `BO.obrano`.
- `tr_bo_delete` limpa BOTXT por `bostamp`.

Limites:
- A expressão integral das validações e as mensagens devem ser reconfirmadas no script.
- O circuito funcional de BO não está fechado.
- Não está demonstrado que BO origine BI, SL, SV, OL ou outros documentos.
- A contagem zero limita a observação de dados, mas não prova inutilização da tabela.

Próxima abordagem: Dic → Rel → leitura integral dos três triggers → Enciclopédia → apenas depois consultas específicas, se necessárias.

### 3.4 Stock — BI / FI / FN / SL / ST / SA

**Fonte:** handoff funcional no PR #34.

Modelo de trabalho:
- SL: movimentos de stock.
- ST: ficha e acumulados globais do artigo.
- SA: saldos por artigo/armazém.
- Reservas/cativações: compromissos quantitativos, não movimentos físicos.

Origens identificadas no SQL analisado:
- BI pode originar SL, condicionado por TS/TS2 e estado da linha.
- FI pode originar SL segundo TD.lancasl e códigos configurados.
- FN pode originar SL segundo CM1.folansl.
- SV é um circuito de valorização distinto, embora relacionado com stock.

Transferências:
- A lógica BI analisada pode criar dois movimentos SL, um para o armazém de origem e outro para o destino.
- `SL.trfa` tem tratamento próprio nos acumulados.
- Interpretar os códigos em conjunto com CM2 e TS/TS2.

Produção:
- Condições de produção influenciam o código de movimento.
- A documentação Manufactor consultada é contexto específico e não deve ser generalizada ao PHC Gestão/CS.
- O circuito completo de consumo de componentes e entrada de acabado não está demonstrado.
- OFC, OCI, OMI, OCP e OPT não devem ser classificados apenas pelo nome.

### 3.5 SX / SXX — stock por grelha

**Fonte:** relatório técnico SX/SXX no PR #34.

Confirmado:
- `V_WS_STKARTIGOS_GRELHAS` consulta SX e apresenta saldo por artigo/armazém/cor/tamanho.
- `V_WS_STKARTIGOS` consulta SA.
- Triggers SL analisados actualizam ST/SA, mas não demonstram escrita em SX/SXX.
- SGC/SGT alteram ou eliminam referências de cor/tamanho em SX, sem demonstrar cálculo de `SX.stock`.
- STIC/STIL contêm dados de inventário físico; rotina de lançamento para SL e actualização de SX não localizada.
- `usp_stinvclc` não foi demonstrada como responsável por SX/SXX.
- SXX é descrita em Rel como stock global por armazém/cor/tamanho, mas o papel operacional permanece por provar.
- `ST.textteis` é uma pista; influência operacional por demonstrar.

**Pergunta central:** quem cria as combinações e mantém efectivamente `SX.stock`, e qual é o papel operacional de SXX?

### 3.6 Inventário físico — STIC / STIL

- STIC identifica o cabeçalho do inventário; STIL contém contagens e dimensões como referência, armazém, lote, cor e tamanho.
- Foi observada a ligação de SL ao cabeçalho STIC através de `SL.sticstamp = STIC.sticstamp`.
- Não está demonstrada uma relação directa entre cada linha STIL e um SL.
- Falta localizar a rotina que transforma as contagens em movimentos e mantém os saldos de grelha.

### 3.7 Pré-cativação — BIPRECAT

- BIPRECAT e os campos `biprecatstamp` e `bostamp` foram identificados.
- O Monitor de Cativação constitui o processo funcional de referência.
- A criação, associação e conversão técnica para BI/SA continuam por localizar.
- Não confundir pré-cativo, cativo/reserva e saída física.

### 3.8 VO — recuperação pendente

O relatório de recuperação na main confirma que a análise de VO não ficou persistida com evidência suficiente. Não assumir que VO é sinónimo de BI nem que a análise de BI substitui a análise de VO.

Para recuperar:
1. obter as evidências originais da conversa/consultas;
2. identificar tabela, campos, REL/DTL e stamps;
3. rever triggers e operações;
4. cruzar Enciclopédia e dados da instalação;
5. produzir matriz de ciclo de vida apenas depois de confirmar as fontes.

## 4. Grafo e modelo de relações

O grafo revisto está em [Grafo funcional PHC e relações estruturais observadas](../audits/grafos-phc-funcional-e-estrutural-2026-11-25.md), actualizado no PR #32.

O documento mantém duas vistas:
- **Funcional:** BO/BO2/BI, stock, reservas, inventário, grelhas e tesouraria.
- **Estrutural:** apenas os predicados observados `BO.bostamp = BO2.bo2stamp`, `BI.bostamp = BO.bostamp`, `SL.bistamp = BI.bistamp`, `BI.obistamp` como referência à linha de origem, `SL.sticstamp = STIC.sticstamp` e a limpeza BOTXT por `bostamp`.

O grafo não é ERD físico e não afirma FKs SQL. Relações funcionais e mecanismos pendentes estão identificados separadamente.

## 5. Prioridades para a próxima investigação

### Prioridade 1 — OD → OL / BA / FE
1. Rever Dic e Rel de OD, OL, BA e FE.
2. Localizar triggers, procedures, funções, views e referências na Enciclopédia.
3. Confirmar as origens de OD e resolver a divergência das listas.
4. Identificar a operação que cria/converte OD em OL.
5. Fechar a sincronização BA e o comportamento de FE em dias fechados.
6. Só depois pedir consultas read-only específicas para lacunas concretas.

### Prioridade 2 — SX / SXX e inventário com grelhas
1. Procurar todas as rotinas SQL que escrevem em SX/SXX, não apenas triggers SL.
2. Rever Dic/Rel e definições completas de SX/SXX.
3. Localizar a rotina de lançamento STIC/STIL.
4. Encontrar a cadeia de chamada de `usp_stinvclc`, sem a assumir como lançador.
5. Esclarecer o papel de `ST.textteis`.
6. Separar evidência SQL, aplicação PHC e documentação/registos de assistência.

### Prioridade 3 — BO e dossiers internos
1. Consultar Dic e Rel antes de novas queries.
2. Revalidar integralmente `tr_bo_insert/update/delete`.
3. Cruzar assinatura/importação com Enciclopédia.
4. Identificar efeitos em BI, stock, tesouraria e dossiers relacionados apenas se houver evidência.
5. Manter a investigação aberta enquanto o circuito não estiver demonstrado.

### Prioridade 4 — produção e movimentos
1. Fechar a semântica de OFC/OCI/OMI/OCP/OPT com fontes PHC Gestão/CS.
2. Distinguir reservas, consumo físico, produção e valorização.
3. Identificar o circuito completo de componentes e produto acabado.
4. Não extrapolar documentação Manufactor.

### Prioridade 5 — VO
Recuperar evidências originais antes de reconstruir ou documentar conclusões.

## 6. Regras para actualizar o conhecimento

Cada novo relatório deve conter:
- pergunta de investigação;
- base/versão e âmbito;
- fontes consultadas e proveniência;
- factos observados em Dic/Rel;
- código e condições exactas;
- dados da instalação, quando existam;
- interpretação funcional;
- hipóteses explicitamente separadas;
- questões não resolvidas;
- próximos passos concretos.

O grafo só recebe uma aresta estrutural quando existir predicado observado. O grafo funcional pode conter relações documentadas/condicionais, mas deve marcá-las como tal.

Não reescrever relatórios antigos como se tivessem sido produzidos com evidência posterior. Registar a evolução e a data da revisão.

## 7. Ponto exacto de retoma

A próxima investigação recomendada é **OD → OL**, incluindo BA e FE. É a lacuna mais transversal no circuito de tesouraria e está explicitamente assinalada no grafo.

Em paralelo, PR #34 mantém a investigação SX/SXX aberta e PR #35 mantém BO como primeira passagem. O PR #32 contém o grafo actualizado. Todos permanecem fora da main até revisão e integração explícita.

**Este índice é uma fotografia de continuidade em 27-11-2026. Não significa que os circuitos PHC estejam todos fechados nem que os PRs tenham sido integrados.**
