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

### 2.1 Conhecimento já integrado na main

| Domínio | Documento | Estado e limite |
|---|---|---|
| FT / FI / TD / OL / CH / CHL | [Investigação funcional FT/FI/TD](../audits/investigacao-funcional-csdefault-ft-fi-td-2026-11-25.md) | Leitura estática e configuração; sem validação transaccional na CSDefault202603. |
| BO / BO2 / BI / SL / ST / SA | [Investigação funcional BO, BI e stock](../audits/investigacao-funcional-bo-bi-sl-2026-11-25.md) | Triggers, reservas, `obistamp`, `qtt2` e anulação lógica parcialmente reconstruídos; validação funcional pendente. |
| Dossiers / stock cativo / pré-cativo / inventários | [Investigação de dossiers e stock cativo](../audits/investigacao-funcional-dossiers-internos-stock-cativo-precativo-2026-11-25.md), [stock e inventários](../audits/investigacao-funcional-stock-inventarios-2026-11-25.md), [síntese de rastreabilidade](../audits/sintese-rastreabilidade-dossiers-stock-inventarios-2026-11-25.md) | Documentação funcional e evidência de bases distintas; lacunas de BIPRECAT, inventário e valorização mantidas. |
| Grafo PHC | [Grafo funcional e relações estruturais](../audits/grafos-phc-funcional-e-estrutural-2026-11-25.md) | Duas vistas: funcional e predicados observados; não é ERD físico nem afirma FKs. |
| Stock / transferências / SX-SXX | [Handoff de stock](phc-stock-production-investigation-handoff-2026-11-25.md), [continuidade PHC Gestão](continuity-stock-phc-gestao-2026-11-27.md), [investigação SX/SXX](../audits/investigacao-tecnica-stock-grelhas-sx-sxx-2026-11-26.md), [síntese complementar de stock](../audits/investigacao-funcional-phc-stock-transferencias-producao-2026-11-27.md) | Inclui guião SQL read-only para pendências de transferências; manutenção de SX.stock e papel de SXX por identificar. |
| OD / OL / BA / BL / BR / BX / BABR / FE | [Investigação de tesouraria](../research/od-ol-tesouraria-ba-fe.md) | Estrutura e configuração validadas; riscos multi-linha estáticos, não bugs confirmados; reconciliação sem validação runtime na CSDefault202603. |
| Devolução de títulos | [Ciclo de devolução CH/CHL/OY/OX/OL/CC](../audits/investigacao-ciclo-devolucao-titulos-2026-11-27.md) | Dois cenários empíricos; informação identificável anonimizada; compensação em CC e origem de `CHL.contado` permanecem por confirmar. |
| VO | [Recuperação da investigação VO e dossiers](../audits/recuperacao-investigacao-funcional-vo-dossiers-2026-11-25.md) | Evidência recuperável insuficiente para fechar VO; manter como lacuna explícita. |

### 2.2 Estado dos PRs de consolidação

| PR | Área | Estado actual |
|---|---|---|
| [#31 — BO, BI e movimentos de stock](https://github.com/Jhouinho/PhcKnowledge/pull/31) | BO / BO2 / BI / SL / ST / SA | Integrado na main. |
| [#32 — dossiers, stock cativo e grafo](https://github.com/Jhouinho/PhcKnowledge/pull/32) | Dossiers / inventários / pré-cativação / grafo | Integrado na main. |
| [#38 — OD/OL, reconciliação bancária e FE](https://github.com/Jhouinho/PhcKnowledge/pull/38) | Tesouraria / bancos / caixa | Integrado na main. |
| [#39 — ciclo de devolução de títulos](https://github.com/Jhouinho/PhcKnowledge/pull/39) | CH / CHL / OY / OX / OL / CC | Integrado na main após anonimização. |
| [#40 — stock e transferências](https://github.com/Jhouinho/PhcKnowledge/pull/40) | BI / FI / FN / SL / SX / SXX / inventários / produção | Integrado na main; preserva o conteúdo complementar do #37 e o guião SQL pendente do #34. |
| [#34 — stock (branch original)](https://github.com/Jhouinho/PhcKnowledge/pull/34) | Stock / transferências | Fechado como substituído pelo #40; conteúdo preservado. |
| [#37 — stock complementar](https://github.com/Jhouinho/PhcKnowledge/pull/37) | Stock / produção / Manufactor | Fechado como substituído pelo #40; relatório preservado. |
| [#35 — primeira passagem BO](https://github.com/Jhouinho/PhcKnowledge/pull/35) | BO / assinatura / BOTXT | Fechado como documentação preliminar redundante; factos centrais preservados nos relatórios #31 e #32. |
| [#36 — índice anterior](https://github.com/Jhouinho/PhcKnowledge/pull/36) | Continuidade | A actualizar/substituir por este PR, para reflectir os merges realizados. |

### 2.3 Auditoria de sobreposição e resultado da consolidação

- **BO (#31, #32, #35):** o relatório aprofundado #31 e a consolidação funcional/grafo #32 foram integrados. O #35 foi encerrado como primeira passagem redundante; não foi descartada evidência exclusiva.
- **Stock (#34, #37, #40):** o conteúdo do #34 e a síntese complementar do #37 foram reaplicados sobre a main actual e integrados pelo #40. O guião SQL read-only de transferências está agora versionado em `docs/sql/pendentes-transferencias-stock-phc-gestao-2026-11-27.sql`.
- **Tesouraria (#38):** investigação integrada; as conclusões continuam limitadas pela ausência de BA/BR/BABR transaccionais na CSDefault202603 e pela falta de reprodução dos riscos multi-linha.
- **Devolução (#39):** investigação integrada depois de anonimizar nomes, base de testes, cheques, stamps e valores financeiros exactos. Mantém a proveniência de uma cópia de testes, não da baseline standard.
- **Grafo (#32):** integrado com vistas funcional e estrutural; não deve ser interpretado como ERD físico nem como declaração de FKs.

**Resultado:** o património documental dos últimos dias está integrado na main, com excepção deste índice actualizado. As branches/PRs substituídos foram encerrados sem eliminar o conteúdo técnico que continham. Não criar novas branches por cada investigação; actualizar os documentos de continuidade existentes e usar PRs apenas para ciclos de integração coerentes.

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

**Fonte canónica:** [relatório de investigação de tesouraria](../research/od-ol-tesouraria-ba-fe.md), cruzado com a Enciclopédia PHC.

O modelo foi corrigido: **OD não é convertido genericamente em OL**. OD mantém previsões; documentos/operações de origem geram OL directamente quando ocorre uma liquidação efectiva. Em LM/LO, OD e OL são resultados paralelos da operação sobre o título.

| Tabela | Conhecimento actual | Lacuna principal |
|---|---|---|
| OD | Movimentos previstos, manuais ou automáticos; ligações/circuitos identificados com CC, FC, LR/LM, LP/LO, VY e CHF (criação e eliminação devem ser distinguidas por trigger) | Valores/configuração reais e validação transaccional |
| OL | Movimentos efectivos gerados directamente por FT, RE, LM, LO e outros circuitos | Inventário completo de origens e validação runtime |
| BA | Movimentos bancários derivados de OL quando existe conta bancária (`contado>0`); PK `bastamp`, índice não único em `olstamp` | Validar fluxo aplicacional e comportamento com dados reais |
| BL | Contas bancárias e saldos totais/reconciliados actualizados por BA; PK `noconta`, UNIQUE `blstamp` | Regras adicionais e validação transaccional |
| BX | Cabeçalho do extrato bancário importado; PK composta `(contado, extracto)`, UNIQUE `bxstamp` | Sequência de actualização e validação transaccional |
| BR | Movimentos importados que pertencem a BX; PK `brstamp`; sem índice específico em `bxstamp`/ `bastamp` no resultado | Associação funcional via BABR e validação transaccional |
| BABR | Associação entre BA e BR; PK `babrstamp`, índices não únicos em `bastamp` e `brstamp`; sem FK | Fluxo aplicacional de criação/remoção e cardinalidade usada em runtime |
| FE | Saldos diários da tesouraria em Caixa; fecha dias e regista utilizador | Localizar/validar a autorização aplicacional em dias fechados |

**RD / PD / PO / FO**
- RD (Recibos de Adiantamento) cria/actualiza OL directamente; em `CSDefault202603`, `TSRD.ndoc=1/regrd=0` (normal/entrada) e `TSRD.ndoc=2/regrd=1` (devolução/saída). Pode criar CH quando pago por cheque.
- PD (Adiantamentos a Fornecedores) cria/actualiza FC e OL directamente; se houver cheque, associa CHF ao documento.
- PO (Pagamentos a Fornecedores) cria/actualiza FC e OL quando processado; pode criar um segundo movimento OL para o segundo local.
- FO (Compras a Fornecedores) cria/actualiza FC. O trigger FO de eliminação limpa OL por fostamp, mas não foi encontrado INSERT/UPDATE de OL em FO; não o classificar como origem de criação.

**OD e OL**
- A Enciclopédia descreve OD como movimentos previsionais manuais ou automáticos provenientes de documentos.
- A opção **Dar como realizada a previsão** só transforma movimentos directos e movimentos constantes; não os que vêm de outros documentos.
- LM/LR e LO/LP mantêm OD pelo saldo pendente do título e criam OL directamente nas operações elegíveis.
- RE cria/actualiza OL quando fica processado e a série não está configurada para movimentos incobráveis (`TSRE.movinc=0`). Pode gerar uma segunda componente para o segundo local de tesouraria. Em `CSDefault202603`, TSRE tem `ndoc=1` e `ndoc=2`, ambos com `movinc=0` e `cmcc=0`; `cmccn` é NULL/vazio.
- Os códigos LM/LO são mapeados pelo artigo PHC **Códigos das Letras**; não confundir esses códigos de operação com CM1. CM1 é usado para os movimentos de C/C gerados em alguns ramos (códigos 30 e 81). Na configuração consultada de `CSDefault202603`, ambos têm `cmdesc='Anula. Letra'`; CM1=30 surge no ramo de CC associado a determinadas operações LM/LO e CM1=81 no ramo LO `cm=22`, que cria FC.

**BA / BL / BR / BX / BABR**
- `BA.contado` é usado pelos triggers BA para localizar `BL.noconta`; não é um indicador booleano de reconciliação. `tr_ol_*` transporta `OL.contado` para BA. `tr_bl_update` actualiza `ollocal` nas tabelas dependentes quando mudam banco/conta; não preenche `OL.contado`.
- `tr_ol_*` cria/actualiza/elimina BA para OL com conta bancária indicada.
- `tr_ba_*` mantém em BL saldos totais e reconciliados (`reco=1`).
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

**Fontes canónicas:** [investigação aprofundada BO/BI](../audits/investigacao-funcional-bo-bi-sl-2026-11-25.md) e [consolidação de dossiers/stock cativo](../audits/investigacao-funcional-dossiers-internos-stock-cativo-precativo-2026-11-25.md).

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

**Fontes canónicas:** [handoff funcional de stock](phc-stock-production-investigation-handoff-2026-11-25.md) e [ponto de continuidade PHC Gestão](continuity-stock-phc-gestao-2026-11-27.md).

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

**Fonte canónica:** [investigação técnica SX/SXX](../audits/investigacao-tecnica-stock-grelhas-sx-sxx-2026-11-26.md).

Confirmado:
- `V_WS_STKARTIGOS_GRELHAS` consulta SX e apresenta saldo por artigo/armazém/cor/tamanho.
- `V_WS_STKARTIGOS` consulta SA.
- A manutenção de `SX.stock` diverge entre duas exportações SQL: não aparece nos triggers SL da Fonte A (`docs/sources/triggers phc.sql`, baseline `CSDefault202603`); aparece em `tr_sl_insert/update/delete` da Fonte B (`PhcKnowledgeScanner/triggers phc.sql`). A diferença está documentada no relatório comparativo.
- Na Fonte B, `trfa=0` condiciona ST, mas não as actualizações de SA/SX; o padrão é compatível com transferência entre armazéns sem alteração do total global.
- Na Fonte B, BI/BO2 mantêm `SX.rescli`, `SX.resfor`, `SX.rescat` e `SX.qttrec`, distintos de `SX.stock`.
- A documentação PHC confirma criação de SX por combinação para o armazém 1 e uma rotina para criar combinações em vários armazéns.
- Há riscos estáticos multi-linha a validar na criação SX por SL e no ramo `rescat`; não são bugs reproduzidos.
- SGC/SGT mantêm dimensões de cor/tamanho. STIC/STIL e a rotina de lançamento continuam por investigar.
- `usp_stinvclc` não foi demonstrada como responsável por SX/SXX.
- SXX não aparece nos triggers comparados; o papel operacional permanece por provar.

**Pergunta central:** que objectos/processos completam a manutenção de SX/SXX em cada origem, e o que explica a divergência entre exportações?

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

### 3.8 Devolução de títulos de clientes — CH / CHL / OY / OX / OL / CC

**Fonte canónica:** [auditoria do ciclo de devolução de títulos](../audits/investigacao-ciclo-devolucao-titulos-2026-11-27.md), executada em 27-11-2026 sobre uma cópia de testes anonimizada.

- Foram observados dois cenários: título criado directamente e título criado a partir de recibo.
- O relatório regista os movimentos observados em CH/CHL, OY/OX, OL e CC.
- A inversão de tesouraria foi correlacionada com `tr_chl_insert`; a limpeza dos movimentos originais foi correlacionada com as condições de `tr_re_update` no script standard versionado.
- A eventual compensação em CC permanece por confirmar.
- Não foi demonstrada equivalência integral entre os triggers instalados e o script standard.
- Os resultados pertencem à cópia de testes uma cópia de testes do cliente; não constituem baseline universal PHC.

**Estado:** relatório integrado na `main`; manter separadas observações, correlação com código e questões em aberto.

### 3.9 VO — recuperação pendente

O relatório de recuperação na main confirma que a análise de VO não ficou persistida com evidência suficiente. Não assumir que VO é sinónimo de BI nem que a análise de BI substitui a análise de VO.

Para recuperar:
1. obter as evidências originais da conversa/consultas;
2. identificar tabela, campos, REL/DTL e stamps;
3. rever triggers e operações;
4. cruzar Enciclopédia e dados da instalação;
5. produzir matriz de ciclo de vida apenas depois de confirmar as fontes.

## 4. Grafo e modelo de relações

O grafo funcional e estrutural está integrado em [Grafo funcional PHC e relações estruturais observadas](../audits/grafos-phc-funcional-e-estrutural-2026-11-25.md).

A vista funcional inclui agora:
- RD/PD/PO como origens directas de OL; FO é apenas limpeza de OL no trigger de eliminação;
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
1. Estrutura física de BA, BL, BR, BX e BABR confirmada no catálogo SQL de `CSDefault202603`: PK, índices e ausência de FKs estão registados em [Investigação de tesouraria](../research/od-ol-tesouraria-ba-fe.md).
2. Investigar o risco lógico de `tr_babr_delete`: repõe `reco=0` sem verificar outras associações; confirmar com dados e fluxo aplicacional se a remoção pode ser parcial.
3. Localizar o fluxo aplicacional de criação/remoção de BABR e a autorização de fecho FE.
4. Auditar operações multi-linha nos triggers OL/BA e validar em ambiente descartável.

Consulta de configuração executada em `CSDefault202603`; os resultados estão incorporados no [relatório de tesouraria](../research/od-ol-tesouraria-ba-fe.md): TSRE ndoc 1/2 têm `movinc=0`, `cmcc=0`, `cmccn=NULL`; TSRD ndoc 1 tem `regrd=0` e ndoc 2 tem `regrd=1`; CM1 30 e 81 têm ambos a descrição `Anula. Letra`.

### Prioridade 2 — outras origens directas de OL
1. Aprofundar PR, VY/VZ, OX, OLBB, OW/OWLI, GPC e FACA/FACC/FACD.
2. Distinguir criação, actualização e limpeza em cada trigger.
3. Cruzar cada circuito com a Enciclopédia antes de classificar a função.


**Guião de recolha preparado:** [`inspeccao-schema-sx-sxx-phc-gestao-2026-11-27.sql`](../sql/inspeccao-schema-sx-sxx-phc-gestao-2026-11-27.sql). É read-only e ainda não foi executado. Devolve schema/defaults, índices, constraints, FKs, triggers, dependências e referências em módulos SQL.
### Prioridade 3 — SX / SXX e inventário com grelhas
1. Obter schema, defaults, PKs e índices de SX/SXX nas origens comparáveis.
2. Identificar versão/build e configuração dos triggers de cada origem, sem publicar identificadores de clientes.
3. Procurar procedures, funções, views, tarefas SQL Agent e processos aplicacionais que escrevam em SX/SXX.
4. Validar em cópia descartável os riscos multi-linha identificados em `tr_sl_insert/update` e no ramo `rescat` de BI.
5. Localizar a rotina de lançamento STIC/STIL e a cadeia de chamada de `usp_stinvclc`.
6. Esclarecer o papel de SXX e a influência operacional de `ST.textteis`.

### Prioridade 4 — BO e dossiers internos
1. Revalidar integralmente `tr_bo_insert/update/delete`.
2. Cruzar assinatura/importação com Enciclopédia.
3. Identificar efeitos em BI, stock, tesouraria e dossiers relacionados apenas se houver evidência.

### Prioridade 5 — produção e movimentos
1. Fechar a semântica de OFC/OCI/OMI/OCP/OPT com fontes PHC Gestão/CS.
2. Distinguir reservas, consumo físico, produção e valorização.
3. Identificar o circuito completo de componentes e produto acabado.
4. Não extrapolar documentação Manufactor.

### Prioridade 6 — VO
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

A investigação de OD/OL e da reconciliação bancária está integrada em [Investigação de tesouraria](../research/od-ol-tesouraria-ba-fe.md). A validação read-only de TSRE, TSRD e CM1 em `CSDefault202603` está concluída e registada no relatório. A validação do catálogo SQL de BA/BL/BR/BX/BABR também está concluída. A leitura dos triggers BABR revelou um risco lógico no DELETE: o flag `reco` é reposto a zero sem verificar se subsistem outras associações. A consulta de contagens devolveu BA=0, BR=0 e BABR=0 em `CSDefault202603`, pelo que a reconciliação não é validável em runtime nesta base. A auditoria estática identificou também riscos multi-linha: actualizações de BL sem agregação por conta nos triggers BA e actualização de FE com somas globais do statement, potencialmente incorrectas quando há várias datas em inserted/deleted.

O conhecimento de stock e as pendências SQL estão integrados em [Ponto de continuidade — investigação de stocks PHC Gestão](continuity-stock-phc-gestao-2026-11-27.md). O guião de leitura está em `docs/sql/pendentes-transferencias-stock-phc-gestao-2026-11-27.sql`; executar apenas quando houver acesso à base indicada e guardar os quatro conjuntos de resultados. Não repetir consultas já respondidas.

**Próxima prioridade no circuito de stock:** obter schema e defaults de SX/SXX, identificar a origem/versão das duas exportações de triggers e preparar validações multi-linha numa cópia descartável. O guião SQL de transferências TS/TS2/CM2 continua preparado e por executar quando houver acesso à CSDefault202603. VO continua como lacuna documental independente.

**Este índice é uma fotografia de continuidade em 27-11-2026, actualizada após os merges #31, #32, #38, #39 e #40. A integração documental não significa que os circuitos PHC estejam todos fechados ou validados em runtime.**
