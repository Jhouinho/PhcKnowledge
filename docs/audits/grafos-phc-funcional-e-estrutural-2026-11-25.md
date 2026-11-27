# Grafo funcional PHC e relações estruturais observadas

**Criação:** 25-11-2026  
**Revisão:** 27-11-2026  
**Base principal de análise:** `CSDefault202603`, com fontes complementares identificadas por relatório  
**Estado:** grafo de conhecimento em evolução; não é ERD físico nem especificação universal do PHC.

## 1. Para que serve este grafo

O grafo organiza o conhecimento funcional recolhido nos últimos dias e ajuda a escolher a próxima investigação. Mantém duas vistas independentes:

- **Grafo funcional:** processos, origens, efeitos e dependências de configuração.
- **Grafo estrutural:** apenas predicados de ligação concretamente observados em código ou dados.

Uma relação REL/DTL não é automaticamente uma FK SQL. Uma ligação funcional não prova uma relação física. Uma tabela sem registos na captura não é uma tabela sem função.

### Classificação das arestas

| Marca | Significado |
|---|---|
| **Observado** | Predicado, chamada ou efeito directamente identificado numa fonte concreta |
| **Condicional** | O efeito depende de configuração, estado ou condições do trigger |
| **Declarado/documentado** | Relação ou processo descrito em Dic/Rel/Enciclopédia, sem prova de FK SQL |
| **Pendente** | Pergunta de investigação; não deve ser interpretada como relação confirmada |

As setas mostram a direcção de leitura do processo, não cardinalidade. Não se inferem relações 1:1, 1:N ou N:N sem evidência.

## 2. Grafo funcional — processos e efeitos

O diagrama representa os circuitos identificados e assinala explicitamente as áreas ainda abertas.

```mermaid
flowchart TB
  subgraph DOC["Dossiers e documentos"]
    BO["BO — cabeçalho de dossier"]
    BO2["BO2 — dados complementares"]
    BI["BI — linhas de dossier"]
    FI["FI — linhas de faturação"]
    FT["FT — cabeçalho de faturação"]
    FN["FN — documentos de compra"]
    TD["TD — configuração de documentos"]
    TS["TS / TS2 — configuração de dossiers"]
    BO -->|"associação BO / BO2 observada"| BO2
    BO -->|"BI.bostamp"| BI
    BI -.->|"BI.obistamp: referência de origem"| BI
    FT --> FI
  end

  subgraph STOCK["Movimentos e saldos de stock"]
    SL["SL — movimentos físicos de stock"]
    ST["ST — ficha e acumulados globais"]
    SA["SA — saldos por artigo/armazém"]
    CM1["CM1 — códigos de C/C; inclui folansl usado em FN"]
    CM2["CM2 — configuração/sentido do movimento"]
    SV["SV — movimentos de valorização"]
    BI -->|"condicional: dossier / linha / TS-TS2"| SL
    FI -->|"condicional: TD.lancasl e cmsl"| SL
    FN -->|"condicional: CM1.folansl"| SL
    SL -->|"tr_sl_* actualiza/reverte"| ST
    SL -->|"tr_sl_* actualiza/reverte"| SA
    FI -.->|"SV condicionado por TD e regras por linha"| SV
    CM1 -.->|"folansl / configuração de lançamento"| FN
    CM2 -.->|"interpreta entrada e efeitos do movimento"| SL
    TS -.->|"configuração e estado do dossier"| BI
    TD -.->|"lancasl / cmsl / cmsv"| FI
  end

  subgraph GRID["Grelhas e inventário físico"]
    SX["SX — saldo por artigo/armazém/cor/tamanho"]
    SXX["SXX — papel operacional por esclarecer"]
    SGC["SGC / SGT — catálogo de cores e tamanhos"]
    STIC["STIC — cabeçalho do inventário"]
    STIL["STIL — contagens do inventário"]
    VGRID["V_WS_STKARTIGOS_GRELHAS"]
    VSTOCK["V_WS_STKARTIGOS"]
    VGRID -.->|"consulta SX.stock"| SX
    VSTOCK -.->|"consulta SA"| SA
    SGC -.->|"altera/elimina referências em SX"| SX
    STIC -.->|"inventário contém linhas"| STIL
    STIC -.->|"SL.sticstamp"| SL
    STIL -.->|"lançamento em SL por localizar"| SL
    SL -.->|"actualização de SX não demonstrada"| SX
    SX -.->|"relação operacional por esclarecer"| SXX
  end

  subgraph TREASURY["Tesouraria"]
    OD["OD — movimentos previstos"]
    OL["OL — movimentos efectivos"]
    FT2["FT2 / FT3 — dados de pagamento e IVA"]
    RE["RE — recibos"]
    TSRE["TSRE — configuração de recibos"]
    LR["LR — títulos a receber"]
    LM["LM — operações de títulos a receber"]
    LP["LP — títulos a pagar"]
    LO["LO — operações de títulos a pagar"]
    CC["CC — conta-corrente clientes"]
    FC["FC — conta-corrente fornecedores"]
    VY["VY — comissões de vendedores"]
    CHF["CHF — movimentos de cheque"]
    CH["CH — cheques"]
    CHL["CHL — histórico de movimentos de cheque"]
    OC["OC — códigos de tesouraria"]
    BA["BA — movimentos de conta bancária"]
    BL["BL — contas bancárias e saldos"]
    BX["BX — cabeçalho de extrato importado"]
    BR["BR — movimentos importados do extrato"]
    BABR["BABR — associação de reconciliação"]
    ML["ML — movimentos contabilísticos"]
    FE["FE — saldos diários de caixa"]

    FT -->|"condicional: TD.lancaol + TD/FT2/FT3/OC"| OL
    FT2 -.->|"dados de pagamento / local / moeda"| OL
    RE -->|"process=1 e TSRE.movinc=0"| OL
    TSRE -.->|"configuração da série / movimentos incobráveis"| RE
    LR --> LM
    LM -->|"saldo pendente do título"| OD
    LM -->|"CM de cobrança / liquidação"| OL
    LP --> LO
    LO -->|"saldo pendente do título"| OD
    LO -->|"CM de pagamento / amortização / encargos"| OL
    CC -->|"origem CC"| OD
    FC -->|"origem FC"| OD
    VY -->|"origem VY"| OD
    CHF -.->|"eliminação de previsão associada"| OD
    OC -.->|"código de tesouraria"| OL
    FT -.->|"ramo independente: multi=0 e cheque=1"| CH
    CH -->|"aceitação inicial: CHL cm=1"| CHL
    CHL -.->|"cm=3 e condições específicas"| OL

    OL -->|"contado > 0 (número da conta)"| BA
    BA -->|"BA.contado = BL.noconta; actualiza saldos"| BL
    BX -->|"BX.bxstamp = BR.bxstamp"| BR
    BA -->|"BABR.bastamp"| BABR
    BR -->|"BABR.brstamp"| BABR
    BABR -.->|"marca BA.reco e BR.reco"| BA
    BABR -.->|"marca BA.reco e BR.reco"| BR
    BA -.->|"reco / extracto por bastamp"| ML
    OL -->|"local = C"| FE
  end
  subgraph PRE["Pré-cativação e reservas"]
    BIPRECAT["BIPRECAT — pré-cativação"]
    MON["Monitor de Cativação"]
    MON -.->|"consulta/operação documentada"| BIPRECAT
    BIPRECAT -.->|"bostamp identificado; destino por confirmar"| BO
    BIPRECAT -.->|"criação/conversão técnica por localizar"| BI
    MON -.->|"conversão funcional pré-cativo → cativo"| SA
  end

  BO -.->|"assinatura/importação: validações em triggers"| TS
  BO -.->|"assinatura em BO2 e condições de TS2"| BO2
```

### 2.1 Leitura por domínio

#### Dossiers internos — BO, BO2 e BI
- BO é o foco de uma primeira passagem estática; a contagem comunicada em `CSDefault202603` foi zero.
- Foram identificados `tr_bo_insert`, `tr_bo_update` e `tr_bo_delete`.
- INSERT/UPDATE incluem validações de assinatura digital/documentos importados envolvendo BO, BO2, TS e TS2. A expressão integral deve ser reconfirmada no script antes de detalhar a regra.
- `tr_bo_delete` elimina BOTXT por correspondência de `bostamp`.
- Não está demonstrado que BO origine BI, SL, SV ou outros documentos. A análise BO permanece incompleta.

#### Stock, movimentos e reservas
- SL representa movimentos de stock; ST e SA são actualizados/revertidos pelos triggers SL analisados.
- BI pode originar SL, condicionado por configuração TS/TS2 e estado da linha.
- FI pode originar SL segundo TD.lancasl e cmsl; FN pode originar SL segundo CM1.folansl.
- Transferências BI podem produzir dois movimentos SL, um por armazém; `SL.trfa` tem tratamento próprio nos acumulados.
- Reservas/cativações não são saídas físicas. A existência de uma reserva não prova consumo nem movimento SL.
- SV é um circuito de valorização distinto, condicionado por regras próprias; não o confundir com movimento físico SL.
- O ciclo completo de produção — consumo de componentes e entrada de acabado — ainda não está demonstrado. A documentação Manufactor é contexto específico e não deve ser generalizada ao PHC Gestão/CS.

#### SX/SXX e inventário físico
- `V_WS_STKARTIGOS_GRELHAS` consulta SX e apresenta saldo por artigo/armazém/cor/tamanho.
- `V_WS_STKARTIGOS` consulta SA.
- Os triggers SL analisados actualizam ST/SA, mas não demonstram escrita em SX/SXX.
- SGC/SGT alteram ou eliminam referências de cor/tamanho em SX; não demonstram cálculo de `SX.stock`.
- STIC/STIL contêm dados do inventário físico; a rotina que transforma as contagens em movimentos SL e actualiza SX não foi localizada.
- `usp_stinvclc` não foi demonstrada como responsável por SX/SXX.
- SXX é descrita em Rel como stock global por armazém/cor/tamanho, mas o seu papel operacional não está provado.

#### Tesouraria — FT, RE, LM/LR, LO/LP, OD, OL, BA/BL e reconciliação
- FT pode criar OL quando `TD.lancaol=1` e existem correspondências necessárias em TD, FT2, FT3 e OC.
- RE cria/actualiza OL quando fica processado e a série não está configurada para movimentos incobráveis (`TSRE.movinc=0`); pode gerar uma segunda componente quando o segundo local de tesouraria tem valor.
- LM/LR e LO/LP mantêm OD como saldo pendente do título e criam OL directamente nas operações de liquidação elegíveis. Não existe conversão genérica OD → OL.
- A Enciclopédia mapeia os códigos de operação das letras; não os confundir com os códigos CM1 de conta-corrente.
- BA é criado a partir de OL quando `contado>0`. `contado` é o número da conta bancária, correspondente a `BL.noconta`, e não um booleano de reconciliação.
- BA actualiza os saldos totais e reconciliados em BL. BABR liga BA aos movimentos importados BR; os triggers BABR marcam/desmarcam ambos como reconciliados.
- BR contém movimentos importados do extrato BX; os triggers BR chamam `usp_Act_Extratos_Bancarios` para recalcular os saldos do cabeçalho BX.
- BA também propaga `reco` e `extracto` para ML quando existe correspondência por `bastamp`.
- FE é actualizado por OL com `local='C'`. O fecho diário é um controlo funcional/documentado da aplicação, não uma validação encontrada nos triggers OL.
#### Pré-cativação
- BIPRECAT e os campos `biprecatstamp` e `bostamp` foram identificados.
- O processo funcional de pré-cativação está associado ao Monitor de Cativação, mas o ciclo técnico de criação, conversão e ligação a BI/SA ainda não está fechado.
- Não converter as setas pendentes em relações estruturais.

## 3. Grafo estrutural — apenas predicados observados

Este grafo é intencionalmente pequeno. Inclui apenas predicados de ligação identificados no material analisado. Não afirma que sejam constraints FK declaradas no SQL Server.

```mermaid
flowchart LR
  BO["BO.bostamp"]
  BO2["BO2.bo2stamp"]
  BI["BI.bostamp / BI.bistamp / BI.obistamp"]
  SL["SL.bistamp / SL.sticstamp"]
  STIC["STIC.sticstamp"]
  BOTXT["BOTXT.bostamp"]
  OL["OL.olstamp"]
  BA["BA.bastamp / BA.olstamp"]
  BL["BL.noconta"]
  BX["BX.bxstamp"]
  BR["BR.brstamp / BR.bxstamp"]
  BABR["BABR.bastamp / BABR.brstamp"]
  ML["ML.bastamp"]

  BO2 -->|"BO.bostamp = BO2.bo2stamp"| BO
  BI -->|"BI.bostamp = BO.bostamp"| BO
  SL -->|"SL.bistamp = BI.bistamp"| BI
  BI -.->|"obistamp referencia bistamp da linha de origem"| BI
  SL -->|"SL.sticstamp = STIC.sticstamp"| STIC
  BO -->|"tr_bo_delete: BOTXT.bostamp = deleted.bostamp"| BOTXT
  OL -->|"OL.olstamp = BA.olstamp (tr_ol_*)"| BA
  BA -->|"BA.contado = BL.noconta (tr_ba_*)"| BL
  BR -->|"BR.bxstamp = BX.bxstamp (tr_br_* / procedure)"| BX
  BA -->|"BABR.bastamp = BA.bastamp"| BABR
  BR -->|"BABR.brstamp = BR.brstamp"| BABR
  BA -->|"ML.bastamp = BA.bastamp (tr_ba_update)"| ML
```

### 3.1 Registo dos predicados

| # | Predicado observado | Fonte / contexto | Classificação e limite |
|---:|---|---|---|
| 1 | `BO.bostamp = BO2.bo2stamp` | Associação BO/BO2 observada na análise estrutural/código | Predicado observado; não afirmar FK declarada sem catálogo SQL |
| 2 | `BI.bostamp = BO.bostamp` | Relação de linha BI com cabeçalho BO | Predicado observado |
| 3 | `SL.bistamp = BI.bistamp` | Associação do movimento SL à linha BI | Predicado observado; criação de SL é condicional |
| 4 | `BI.obistamp` referencia `BI.bistamp` de uma linha de origem | Auto-relação funcional de BI | Predicado de origem identificado; validar contexto e cardinalidade |
| 5 | `SL.sticstamp = STIC.sticstamp` | Associação do movimento ao cabeçalho de inventário | Predicado observado |
| 6 | `BOTXT.bostamp = deleted.bostamp` | `tr_bo_delete` | Predicado de limpeza no trigger; não prova FK nem ciclo de vida completo de BOTXT |
| 7 | `OL.olstamp = BA.olstamp` | `tr_ol_insert/update/delete` | BA é criado apenas para OL com conta bancária indicada; não é cópia integral de OL |
| 8 | `BA.contado = BL.noconta` | `tr_ba_insert/update/delete` e `tr_bl_update` | `contado` identifica a conta bancária; não é indicador booleano |
| 9 | `BR.bxstamp = BX.bxstamp` | `tr_br_*` e `usp_Act_Extratos_Bancarios` | Movimento importado pertence a um cabeçalho de extrato |
| 10 | `BABR.bastamp = BA.bastamp` | `tr_babr_insert/delete` | Associação de reconciliação |
| 11 | `BABR.brstamp = BR.brstamp` | `tr_babr_insert/delete` | Associação de reconciliação; cardinalidade deve ser confirmada por índices |
| 12 | `ML.bastamp = BA.bastamp` | `tr_ba_update` | Propagação de `reco` e `extracto` para movimentos contabilísticos |

### 3.2 Elementos conhecidos sem aresta estrutural demonstrada

| Elementos | Conhecimento disponível | Não afirmar ainda |
|---|---|---|
| BO / TS / TS2 / BO2 | Validações de assinatura/importação nos triggers BO | Junções físicas ou cardinalidades sem rever Dic/Rel e expressão integral |
| FT / FI / TD / OL | Circuito funcional condicional descrito nos triggers standard | FK entre documentos, linhas, configuração e OL |
| FT / CH / CHL | Ramos funcionais de criação e histórico de cheques | Relações físicas completas ou semântica universal de cm=3 |
| OD / LM / LR e OD / LO / LP | OD mantém o saldo pendente; LM/LO geram OL directamente nas operações elegíveis | Relação genérica OD→OL; não foi demonstrada |
| OD / CC / FC / VY / CHF | Origens e efeitos de triggers identificados | Relações físicas completas ou cardinalidades fora dos predicados observados |
| OL / FE | OL actualiza saldos diários FE quando `local='C'` | FK ou stamp directo; FE agrega por data |
| BI / FI / FN / SL | Origens condicionais de movimentos de stock | Uma ligação universal para todos os tipos de documento |
| SL / ST / SA | Actualização de acumulados nos triggers SL | Chaves físicas completas de artigo/armazém neste grafo |
| SX / SXX / SGC / SGT | Views e operações de manutenção de dimensões em SX | Junção SX/SXX, rotina de saldo e relação directa SL→SX |
| STIC / STIL | Cabeçalho e linhas de inventário | Predicado estrutural STIC–STIL e ligação de cada STIL a SL |
| BIPRECAT | Campos e processo de pré-cativação parcialmente identificados | Destino de `bostamp`, criação/conversão e relações físicas |
| SV / SE | Objectos identificados em stock/valorização/lotes | Relações completas com documentos, SL, STIC ou STIL |

## 4. O que falta para um ERD físico

O grafo estrutural acima não é um ERD físico. Para produzir um ERD por instalação, é necessário recolher e manter separados:
1. schemas e tabelas;
2. colunas, tipos, nulabilidade e ordem;
3. PKs e unique constraints;
4. FKs declaradas e colunas de origem/destino;
5. índices relevantes;
6. views, triggers, procedures e funções que implementem ligações lógicas;
7. predicados usados pela aplicação PHC quando não exista FK.

Cada ligação futura deve ser classificada como:
- FK declarada;
- PK/UK mais predicado observado;
- predicado de código;
- associação funcional documentada;
- hipótese (fora do grafo confirmado).

## 5. Prioridades para continuar a investigação

1. **Tesouraria — configuração e validação:** confirmar valores TSRE (`movinc`, `cmcc`, `cmccn`) e descrições CM1 30/81; depois validar em ambiente descartável os circuitos RE/LM/LO.
2. **Reconciliação bancária:** confirmar estrutura, índices e cardinalidades de BA/BL/BR/BX/BABR; localizar no código aplicacional a criação/remoção das associações BABR.
3. **Fecho FE:** localizar a validação aplicacional que exige password em dias fechados e confirmar a configuração efectiva.
4. **SX/SXX:** localizar quem cria e actualiza `SX.stock`, esclarecer SXX e o circuito do inventário com grelhas.
5. **BO / dossiers internos:** cruzar Dic, Rel, triggers integrais e Enciclopédia; determinar apenas depois os efeitos em BI, stock e tesouraria.
6. **Produção:** fechar o percurso real de consumo/entrada no PHC Gestão/CS, sem extrapolar Manufactor.
7. **VO:** recuperar as evidências originais; não assumir equivalência com BI.

## 6. Fontes de continuidade e proveniência

- [FT/FI/TD e tesouraria — relatório integrado na main](investigacao-funcional-csdefault-ft-fi-td-2026-11-25.md).
- [Recuperação da investigação VO e dossiers internos — main](recuperacao-investigacao-funcional-vo-dossiers-2026-11-25.md).
- [PR #32 — dossiers, stock cativo e grafo](https://github.com/Jhouinho/PhcKnowledge/pull/32).
- [PR #34 — stocks, produção, transferências e SX/SXX](https://github.com/Jhouinho/PhcKnowledge/pull/34).
- [PR #35 — primeira passagem BO](https://github.com/Jhouinho/PhcKnowledge/pull/35).
- [PR #36 — índice de continuidade](https://github.com/Jhouinho/PhcKnowledge/pull/36).
- [PR #38 — OD/OL, letras, reconciliação bancária e FE](https://github.com/Jhouinho/PhcKnowledge/pull/38).

O script `docs/sources/triggers phc.sql` é uma fonte standard legível, não prova automática de que as definições instaladas em cada base sejam idênticas. A análise estática não equivale a execução em runtime. Os ZIP da Enciclopédia e das capturas não devem ser adicionados ao repositório.

**Estado final desta revisão:** grafo funcional reorganizado por domínio e actualizado com BA/BL/BR/BX/BABR; grafo estrutural inclui predicados observados em código; OD→OL genérico removido; relações pendentes explicitamente isoladas; ERD físico completo continua dependente de recolha própria.
