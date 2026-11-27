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
| [#38 — OD/OL, reconciliação bancária e FE](https://github.com/Jhouinho/PhcKnowledge/pull/38) | Tesouraria / letras / reconciliação / caixa | Investigação OD/OL, LM/LO/RE, BA/BL/BR/BX/BABR e FE | Aberto, Draft, não merged |

Os PRs são trabalho documental separado. Não declarar os respectivos documentos como integrados na main até ocorrer merge. A actualização do grafo no PR #32 incorpora novas relações bancárias observadas, mas não integra automaticamente os relatórios dos PRs #34, #35 e #38.

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

### 3.2 Tesouraria — OD / OL / BA / BL / BR / BX / BABR / FE

**Fonte:** relatório de investigação no [PR #38](https://github.com/Jhouinho/PhcKnowledge/pull/38), cruzado com a Enciclopédia PHC.

O modelo foi corrigido: **OD não é convertido genericamente em OL**. OD mantém previsões; documentos/operações de origem geram OL directamente quando ocorre uma liquidação efectiva. Em LM/LO, OD e OL são resultados paralelos da operação sobre o título.

| Tabela | Conhecimento actual | Lacuna principal |
|---|---|---|
| OD | Movimentos previstos, manuais ou automáticos; origens CC, FC, LR/LM, LP/LO, VY e CHF identificadas no código | Valores/configuração reais e validação transaccional |
| OL | Movimentos efectivos gerados directamente por FT, RE, LM, LO e outros circuitos | Inventário completo de origens e validação runtime |
| BA | Movimentos bancários derivados de OL quando existe conta bancária (`contado>0` | Índices/cardinalidades completas e validação do circuito |
| BL | Contas bancárias e saldos totais/reconciliados actualizados por BA | Confirmar estrutura/índices e regras adicionais |
| BX | Cabeçalho do extrato bancário importado | Confirmar estrutura/índices e sequência de actualização |
| BR | Movimentos importados que pertencem a BX | Confirmar estrutura/índices e associação a BABR |
| BABR | Associação entre BA e BR; triggers marcam/desmarcam ambos como reconciliados | Cardinalidade física e fluxo aplicacional de criação/remoção |
| FE | Saldos diários da tesouraria em Caixa; fecha dias e regista utilizador | Localizar/validar a autorização aplicacional em dias fechados |

**OD e OL**
- A Enciclopédia descreve OD como movimentos previsionais manuais ou automáticos provenientes de documentos.
- A opção **Dar como realizada a previsão** só transforma movimentos directos e movimentos constantes; não os que vêm de outros documentos.
- LM/LR e LO/LP mantêm OD pelo saldo pendente do título e criam OL directamente nas operações elegíveis.
- RE cria/actualiza OL quando fica processado e a série não está configurada para movimentos incobráveis (`TSRE.movinc=0`). Pode gerar uma segunda componente para o segundo local de tesouraria.
- Os códigos LM/LO são mapeados pelo artigo PHC **Códigos das Letras**; não confundir esses códigos de operação com CM1. CM1 é usado para os movimentos de C/C gerados em alguns ramos (códigos 30 e 81).

**BA / BL / BR / BX / BABR**
- `OL.contado` corresponde a `BL.noconta`; não é um indicador booleano de reconciliação.
- `tr_ol_*` cria/actualiza/elimina BA para OL com conta bancária indicada.
- `tr_ba_*` mantém em BL saldos totais e reconciliados (`reco=1`.
- `tr_babr_insert/delete` liga BA a BR e marca/desmarca ambos como reconciliados.
- BR contém movimentos importados; BX é o cabeçalho do extrato. `tr_br_*` chama `usp_Act_Extratos_Bancarios` para recalcular os saldos do BX.
- `tr_ba_update` propaga `reco` e `extracto` para ML pelo `bastamp`.
- BA não é réplica integral de OL e não se deve confundir a sua manutenção de saldos com a reconciliação.

**FE**
- `tr_ol_*` actualiza os saldos FE quando `OL.local='C'`, incluindo os dias posteriores.
- A Enciclopédia confirma que fechar a caixa marca o saldo diário como fechado e regista o utilizador; dias anteriores ainda abertos também são fechados.
- Os parâmetros **Só se pode movimentar a caixa em dias já fechados com password** e **Password para movimentar a caixa em dias já fechados** controlam o bloqueio/autorização.
- Não foi encontrado trigger `tr_fe_*` nem validação de `FE.fechado` nos triggers OL; a regra de autorização é aplicacional/configuracional.

Limites:
- As tabelas transaccionais consultadas em `CSDefault202603` estavam sem registos; não houve validação em execução.
- A análise de triggers é do script standard e não prova que cada instalação tenha definições idênticas.
- O extracto REL consultado não contém relações declaradas para BA, BL, BR, BX ou BABR; as ligações descritas são predicados e efeitos observados em código.

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

O grafo funcional e estrutural foi actualizado no [PR #32](https://github.com/Jhouinho/PhcKnowledge/pull/32).

A vista funcional inclui agora:
- OD e OL como resultados paralelos das operações sobre títulos;
- RE → OL condicionado por processamento e configuração TSRE;
- OL → BA quando existe conta bancária;
- BA → BL para manutenção de saldos;
- BA ↔ BABR ↔ BR para reconciliação com movimentos importados de BX;
- BA → ML para propagação de estado de reconciliação;
- OL → FE quando o local é Caixa.

A vista estrutural inclui os predicados observados em código:
- BO/BO2, BO/BI, BI/SL, BI.obistamp, SL/STIC e BOTXT por bostamp;
- OL/BA, BA/BL, BR/BX, BABR/BA, BABR/BR e BA/ML.

O grafo não é ERD físico e não afirma FKs SQL. O facto de uma ligação estar no grafo estrutural significa que o predicado foi observado em código, não que exista uma constraint FK.

## 5. Prioridades para a próxima investigação

### Prioridade 1 — configuração e validação de tesouraria
1. Confirmar valores TSRE (`ndoc`, `movinc`, `cmcc`, `cmccn`) e descrições CM1 para os códigos 30 e 81.
2. Rever estrutura/índices de BA, BL, BR, BX e BABR.
3. Localizar no código aplicacional a criação/remoção de BABR e a autorização de fecho FE.
4. Auditar operações multi-linha nos triggers OL/BA e validar em ambiente descartável.

Consulta específica pendente (não é uma query de estrutura; serve apenas para confirmar configuração e descrições efectivas):

```sql
SELECT ndoc, movinc, cmcc, cmccn
FROM dbo.tsre
ORDER BY ndoc;

SELECT cm, cmdesc
FROM dbo.cm1
WHERE cm IN (30, 81)
ORDER BY cm;
```

### Prioridade 2 — SX / SXX e inventário com grelhas
1. Procurar todas as rotinas SQL que escrevem em SX/SXX, não apenas triggers SL.
2. Rever Dic/Rel e definições completas de SX/SXX.
3. Localizar a rotina de lançamento STIC/STIL.
4. Encontrar a cadeia de chamada de `usp_stinvclc`, sem a assumir como lançador.
5. Esclarecer o papel de `ST.textteis`.
6. Separar evidência SQL, aplicação PHC e documentação/registos de assistência.

### Prioridade 3 — BO e dossiers internos
1. Revalidar integralmente `tr_bo_insert/update/delete`.
2. Cruzar assinatura/importação com Enciclopédia.
3. Identificar efeitos em BI, stock, tesouraria e dossiers relacionados apenas se houver evidência.

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

A investigação de OD/OL e da reconciliação bancária já tem uma primeira síntese no PR #38. A próxima acção concreta é confirmar os valores de configuração TSRE e as descrições CM1 30/81 com a consulta específica acima. Depois disso, avançar para índices/cardinalidades BA/BL/BR/BX/BABR e validação da aplicação.

Em paralelo, PR #34 mantém SX/SXX e produção em aberto, PR #35 mantém BO como primeira passagem e PR #32 contém o grafo actualizado. Todos permanecem fora da main até revisão e integração explícita.

**Este índice é uma fotografia de continuidade em 27-11-2026. Não significa que os circuitos PHC estejam todos fechados nem que os PRs tenham sido integrados.**
