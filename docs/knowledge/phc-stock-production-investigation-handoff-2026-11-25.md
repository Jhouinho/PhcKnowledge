# PHC Gestão — Investigação funcional de stocks, dossiers internos e produção

> **Documento de continuidade**  
> Data: 25-11-2026  
> Repositório: `Jhouinho/PhcKnowledge`  
> Branch: `docs/stock-production-investigation-handoff-2026-11-25`  
> Estado: investigação documental/estática; ainda não validada numa instalação PHC em execução.

## 1. Objectivo e âmbito

Este documento consolida o conhecimento produzido nesta sequência de investigação para permitir retomar o trabalho noutro chat sem depender do histórico da conversa.

O objectivo é reconstruir o funcionamento standard do PHC Gestão, partindo das tabelas e dos triggers SQL, para compreender:
- como os documentos originam movimentos de stock;
- como os movimentos actualizam stock global e stock por armazém;
- como funcionam transferências entre armazéns;
- como se relacionam dossiers internos, linhas executadas e documentos de origem;
- onde começa e acaba o circuito de produção;
- que efeitos são físicos (movimentos), quais são reservas/cativações e quais são quantidades auxiliares.

**Regra de rigor:** separar sempre (1) evidência directa no SQL, (2) documentação funcional, (3) inferência e (4) questões por validar na base de dados. Não transformar uma hipótese numa regra standard.

## 2. Fontes e limitações

### 2.1. Fonte SQL

Foi analisado o ficheiro SQL de triggers PHC disponibilizado na conversa e associado ao repositório/GitHub. No contexto de leitura da conversa, a fonte estava identificada como `turn146file0`. Os números de linha citados durante a análise são referências dessa cópia/contexto, não substituem a identificação do ficheiro e a validação contra a versão actual do repositório.

Triggers/áreas observados:
- `tr_bi_insert`, `tr_bi_update` e lógica de eliminação de linhas BI;
- `tr_bo2_update` (anulação/actualização do cabeçalho de dossier);
- `tr_sl_insert` (efeitos de movimentos nos acumulados);
- `tr_fi_insert`, `tr_fi_update`, `tr_fi_delete`;
- `tr_fn_insert`;
- referências a tabelas auxiliares `OFC`, `OCI`, `OMI`, `OCP`, `OPT`.

### 2.2. Fonte funcional

Foi consultada documentação técnica local do PHC Manufactor relativa a ordens de fabrico, estados, componentes, confirmação, reservas e integração com PHC Gestão. Esta documentação é relativa ao **Manufactor** e não deve ser automaticamente generalizada a todas as instalações de PHC Gestão nem confundida com o comportamento do PHC CS isolado.

### 2.3. Limitações actuais

- Não houve acesso à base de dados durante a parte final da investigação.
- Não foram executadas queries de validação nesta sessão.
- Não foram testados movimentos em ambiente PHC.
- Não foi demonstrada uma integração Manufactor → PHC Gestão universal.
- Não foi provada a semântica completa de todas as tabelas auxiliares referidas.
- As referências de linha do ficheiro SQL devem ser reconfirmadas depois de abrir a versão actual do ficheiro no GitHub.

## 3. Modelo conceptual de stock que está a emergir

É essencial não tratar todos os valores de stock como equivalentes.

| Conceito | Papel conhecido | Interpretação |
|---|---|---|
| `SL` | Movimentos de stock | Registo dos movimentos que explicam alterações de stock |
| `ST` | Saldos/acumulados globais por artigo | Valores agregados do artigo, não equivalentes ao detalhe por armazém |
| `SA` | Saldos por armazém | Permite observar a distribuição do stock pelos armazéns |
| Reservas/cativações | Quantidades comprometidas | Não são, por si, saída física de stock |
| Quantidades auxiliares/especiais | Quantidades relacionadas com processos | Não devem ser confundidas com stock físico disponível |

A interpretação exacta de todos os campos de ST/SA e a reconciliação dos acumulados ainda devem ser confirmadas com o trigger completo e queries reais.

## 4. Circuitos de origem de movimentos SL identificados

A investigação identificou três famílias de origem que merecem ser mantidas distintas.

### 4.1. Dossiers internos — BI

O trigger `tr_bi_insert` cria movimentos SL quando se verificam condições como:
- tipo de dossier configurado com `TS.stocks = 1`;
- referência preenchida;
- linha fora de conferência (`emconf = 0`);
- dossier não anulado.

O movimento transporta dados da linha BI, incluindo referência, armazém, quantidade e, quando aplicável, lote e dimensões de grelha (cor/tamanho).

A escolha do código de movimento considera a configuração do tipo de dossier:
- circuito normal: `TS.cmstocks`;
- condições específicas de produção: `TS.cm2stocks`;
- troca de equivalência: também pode seleccionar `TS.cm2stocks`, segundo as condições observadas.

A condição observada no código envolve `TS.producao` com `BI.producao`, ou `TS2.trocaequi` com `BI.trocaequi`. A descrição do movimento também pode mudar para `TS.cm2desc`.

**Conclusão segura:** BI pode originar SL e a natureza/configuração do dossier influencia o código do movimento.  
**Não concluído:** esta regra não prova, por si só, a explosão automática de uma composição em componentes, nem prova como é feita a entrada do produto acabado.

### 4.2. Actualização e conferência de BI

A lógica de actualização trata transições de estado. Em particular, a passagem de uma linha de conferência para não conferência pode desencadear a criação do movimento, pelo que a criação do SL não tem necessariamente de ocorrer no instante da inserção inicial da linha.

A análise deve continuar a distinguir:
- inserção;
- alteração de quantidade/referência/armazém/lote/grelha;
- entrada e saída de conferência;
- eliminação da linha;
- anulação do cabeçalho do dossier.

### 4.3. Relação entre linhas — `BI.obistamp`

Foi observado tratamento de `BI.obistamp` que actualiza a linha BI de origem:
- incremento de `BI.qtt2` pela quantidade executada;
- actualização de dados do documento que executou a operação;
- em determinadas condições, actualização de `BI.partes2`.

Isto demonstra um mecanismo de execução/associação entre linhas de dossiers. **Não demonstra que seja exclusivo da produção** nem que represente necessariamente a relação produto acabado/componentes.

### 4.4. Documentos de facturação — FI

O trigger `tr_fi_insert` foi observado a verificar `TD.lancasl = 1`. Entre os campos usados no movimento estão:
- `TD.cmsl`: código de movimento;
- `TD.cmsln`: descrição;
- `FI.armazem`;
- `FI.lote`;
- `FI.cor` e `FI.tam`.

Foi também observada inversão de sinal da quantidade quando `FT.tipodoc = 3`. Isto mostra que a natureza do documento influencia o sinal, enquanto a configuração do tipo de documento determina o lançamento em SL.

A análise também identificou lógica em `tr_fi_update` e `tr_fi_delete` para actualizar/criar ou eliminar movimentos associados à linha FI. Confirmar todos os campos que forçam reconstrução do movimento ao reabrir o SQL.

### 4.5. Documentos de fornecedor — FN

O trigger `tr_fn_insert` usa `CM1.folansl` para determinar o lançamento em SL. Foi ainda observada lógica separada de valorização em SV através de configurações como `folansv` e `folansvgr`.

**Conclusão:** movimento físico (SL) e valorização (SV) são circuitos relacionados, mas não necessariamente coincidentes.

## 5. Transferências entre armazéns

### 5.1. Evidência observada no BI

Na lógica de `tr_bi_insert`, quando o tipo de dossier tem `TS.stocks = 1` e `TS.trfa = 1`, o código observado prevê dois registos SL para a mesma linha BI:

| Registo | Armazém | Código | Identificação |
|---|---|---|---|
| Principal | `BI.armazem` | `TS.cmstocks` | `SL.slstamp = BI.bistamp` |
| Complementar | `BI.ar2mazem` | `TS.cm2stocks` | `SL.slstamp = 'T' + BI.bistamp` |

Os dois movimentos transportam a mesma referência/quantidade e dimensões relevantes.

### 5.2. Efeito nos acumulados

Na análise de `tr_sl_insert` foi observado tratamento especial de `SL.trfa`:
- o movimento normal actualiza os acumulados globais e por armazém;
- o ramo de transferência actualiza os saldos por armazém sem actualizar os acumulados globais da mesma forma.

Isto explica conceptualmente como uma transferência pode alterar a distribuição entre armazéns sem alterar o stock global do artigo.

### 5.3. O que falta validar

Não assumir ainda o sentido de cada movimento apenas pelo nome dos campos. É necessário:
1. confirmar na tabela de configuração de movimentos (CM2) o sinal/efeito de `TS.cmstocks` e `TS.cm2stocks`;
2. confirmar que os códigos são opostos (saída na origem e entrada no destino);
3. validar `BI.armazem` e `BI.ar2mazem` com uma transferência real;
4. verificar o comportamento em update, delete e anulação;
5. confirmar se todos os tipos de transferência usam este mecanismo ou se existem variantes.

## 6. Produção — evidência e fronteiras

### 6.1. O que o SQL demonstra

O SQL demonstra que condições de produção influenciam a selecção do código de movimento usado no SL: quando `TS.producao = 1` e `BI.producao = 1`, pode ser usado `TS.cm2stocks` em vez de `TS.cmstocks`, com descrição alternativa `TS.cm2desc`.

O mesmo campo `cm2stocks` pode surgir em condições de troca de equivalência. Portanto, **não é correcto interpretar `cm2stocks` como um código exclusivamente de produção**.

### 6.2. O que a documentação Manufactor descreve

A documentação funcional local do PHC Manufactor descreve um processo de ordem de fabrico com:
- artigo a fabricar;
- quantidade a produzir;
- armazém/lote de destino;
- grelha/estrutura de componentes;
- confirmação e validação de necessidades;
- reservas/cativações e, consoante a configuração, encomendas a fornecedores;
- planeamento/execução;
- registo de entrada de produção, potencialmente parcial.

A documentação também descreve dossiers internos de suporte no PHC Gestão, incluindo mecanismos associados a:
- reservas/cativação de componentes;
- quantidades especiais/quantidade a fabricar do artigo final.

A confirmação da ordem e os dossiers de suporte podem ter efeitos sobre quantidades comprometidas/especiais sem corresponderem a movimentos físicos de stock.

**Cuidado:** esta descrição é do Manufactor. Não prova que qualquer instalação PHC CS use exactamente os mesmos dossiers, campos ou sequência, nem que o SQL de triggers analisado contenha toda a integração.

### 6.3. Distinções que devem permanecer explícitas

| Conceito | O que significa | Não confundir com |
|---|---|---|
| Quantidade planeada/a fabricar | Intenção ou necessidade de produção | Stock físico |
| Reserva/cativação de componente | Quantidade comprometida | Consumo físico |
| Consumo de componente | Saída física do componente, quando registada | Reserva |
| Entrada de produção | Entrada física do artigo produzido | Quantidade especial planeada |
| Valorização/custo | Efeito económico do movimento | Quantidade física |

### 6.4. O que ainda não está demonstrado

- que a composição seja automaticamente explodida em várias linhas BI;
- que cada componente gere sempre um movimento SL de saída;
- como é registada a entrada do produto acabado;
- se a entrada é feita em BI, noutro tipo de documento ou por integração externa;
- como são tratadas produções parciais;
- se e como a produção gera SV/valorização;
- que tabelas auxiliares representam componentes, operações ou apontamentos.

## 7. Tabelas auxiliares — OFC, OCI, OMI, OCP, OPT

O SQL contém referências a `OFC`, `OCI`, `OMI`, `OCP` e `OPT`, incluindo operações de limpeza/eliminação relacionadas com linhas BI e outras tabelas auxiliares.

Foi observada lógica em que:
- eliminar registos de `OCI` pode eliminar registos relacionados em `OCP`;
- eliminar `OCP` pode actualizar quantidades atribuídas em documentos de fornecedor e linhas BI2;
- eliminar/anular dossiers pode remover registos auxiliares associados.

Isto sugere relações entre documentos e atribuições, mas não permite atribuir com segurança semântica funcional definitiva a cada tabela.

**Não classificar ainda:**
- OFC como cabeçalho de ordem de fabrico;
- OCI como componente;
- OMI como operação;
- OCP como consumo;
- OPT como apontamento.

Estas interpretações precisam de ser confirmadas por definição das tabelas, chaves, triggers, stored procedures, código aplicacional e dados reais.

## 8. Reservas e quantidades auxiliares

Na análise dos triggers BI surgiram mecanismos distintos de actualização:
- `rescat` / `cativo` associados à actualização de `ST.qttcat`;
- `resusr` associado à actualização de `ST.usrqtt`.

Estes mecanismos devem ser tratados como acumulados de quantidades comprometidas/especiais, e não como prova de saída física. A saída física deve ser procurada no movimento SL.

A próxima análise deve confirmar:
- sinal e fórmula de cada campo;
- se existe reversão em update/delete/anulação;
- relação entre reservas em ST e detalhe por armazém;
- se as quantidades são reflectidas em SA ou apenas em ST;
- diferença entre cativo, reservado, pendente e quantidade de utilizador.

## 9. Caso específico de autofacturação — não generalizar

Foi analisado um caso concreto de uma linha FN relacionada com autofacturação:
- `fnstamp` e `fostamp` identificados;
- referência/descrição de depósito de resíduos;
- documento de origem identificado como V/Factura;
- campo `facturada = 0`;
- `fechabo = 0`.

Este caso foi explicitamente identificado pelo utilizador como **um caso específico de autofacturação**. Não deve ser transformado numa regra geral sobre documentos FN, autofacturação ou circuito de stock sem amostra adicional e confirmação dos triggers/configuração envolvidos.

## 10. Dados de esquema já fornecidos pelo utilizador

Foram partilhados inventários de colunas:
- tabela de linhas de facturação (a listagem começou por `fistamp`, com campos como `ftstamp`, `ref`, `qtt`, `armazem`, `lote`, `cor`, `tam`, `facturada`);
- tabela `FN`, com `fnstamp`, `ref`, `design`, `docnome`, `adoc`, `data`, `qtt`, `armazem`, `fostamp`, `fnfref`, `facturada`, `fechabo`.

O utilizador corrigiu uma consulta inicial que assumia colunas `no`, `nome`, `fdata`, `anulado`: essas colunas não existem na tabela consultada. **Regra para futuras queries:** confirmar primeiro o esquema real de cada tabela com `sys.columns`; não transportar nomes de colunas de uma tabela/cabeçalho para outra.

## 11. Conclusões consolidadas

1. SL é o registo central dos movimentos físicos analisados; não confundir movimentos com acumulados.
2. BI, FI e FN têm circuitos distintos que podem originar SL.
3. A configuração do tipo de documento é determinante: `TS.stocks`, `TS.cmstocks`, `TS.cm2stocks`, `TS.trfa`, `TD.lancasl`, `CM1.folansl`.
4. Transferências BI podem criar dois movimentos SL, um por armazém, com identificação distinta.
5. `SL.trfa` recebe tratamento específico nos acumulados, permitindo preservar o total global enquanto se alteram os saldos por armazém.
6. Produção influencia a selecção do código CM, mas isso não prova a mecânica integral de consumo e entrada do produto acabado.
7. `BI.obistamp` demonstra associação/execução entre linhas, mas não é prova de relação específica de produção.
8. Reservas/cativações e quantidades especiais não são movimentos físicos.
9. A documentação Manufactor fornece contexto funcional, mas não deve ser generalizada ao PHC Gestão sem validação.
10. As tabelas OFC/OCI/OMI/OCP/OPT continuam por classificar com segurança.

## 12. Plano de continuação — ordem recomendada

### Fase A — confirmar a fonte e o inventário SQL
1. Abrir no GitHub o ficheiro exacto de triggers SQL e registar caminho, branch e commit.
2. Reconfirmar os triggers e linhas citados neste documento.
3. Identificar todos os triggers/procedures que mencionam `BI`, `SL`, `ST`, `SA`, `OFC`, `OCI`, `OMI`, `OCP`, `OPT`.
4. Extrair definições das tabelas auxiliares e respectivas chaves/índices.

### Fase B — fechar transferências
1. Inspeccionar a definição e dados de CM2.
2. Confirmar códigos e sinais configurados em `TS.cmstocks` e `TS.cm2stocks`.
3. Seguir um caso real de transferência em BI e os dois SL.
4. Reconciliar ST e SA antes/depois.
5. Testar alteração, eliminação e anulação.

### Fase C — fechar produção sem misturar produtos
1. Confirmar se a instalação em estudo tem PHC Manufactor ou apenas PHC CS.
2. Obter documentação exacta da versão/módulo em uso.
3. Identificar os dossiers internos de suporte configurados e respectivos TS.
4. Inspeccionar `BI.producao`, `BI.composto`, `BI.obistamp`, `BI.qtt2`, `BI.partes2` e os campos de origem/execução.
5. Identificar o documento de consumo dos componentes.
6. Identificar o documento de entrada do produto acabado.
7. Seguir os movimentos SL e, separadamente, SV.
8. Verificar produção parcial, estorno, reabertura e anulação.

### Fase D — reconciliar stock e reservas
1. Fechar fórmulas de ST e SA a partir de SL.
2. Distinguir stock total, por armazém, reservado/cativo e quantidade especial.
3. Investigar SAL/SX apenas depois de fechar os movimentos base e os acumulados.

### Fase E — documentar conhecimento
Manter uma matriz por circuito com colunas:
- origem/tabela;
- trigger/procedure;
- condição;
- configuração envolvida;
- destino;
- efeito físico;
- efeito em acumulados;
- reversão;
- nível de evidência;
- teste necessário.

## 13. Queries de validação a executar quando houver acesso à base de dados

Começar por descobrir esquema real, sem assumir colunas:

```sql
SELECT
    c.column_id,
    c.name AS Coluna,
    TYPE_NAME(c.user_type_id) AS Tipo
FROM sys.columns AS c
WHERE c.object_id = OBJECT_ID('BI')
ORDER BY c.column_id;
```

Repetir para `BO`, `TS`, `TS2`, `SL`, `ST`, `SA`, `CM2`, `OFC`, `OCI`, `OMI`, `OCP`, `OPT`.

Depois de conhecer o esquema, preparar queries para:
- uma transferência conhecida;
- uma produção conhecida;
- uma linha BI com `obistamp`;
- uma linha com reserva/cativo;
- movimentos SL associados a FI e FN;
- reconciliação de saldos ST/SA com movimentos SL.

Não executar queries de escrita ou triggers manualmente em produção. Os testes de alteração/anulação devem ocorrer em ambiente controlado e com cópia de segurança.

## 14. Orientação para o próximo chat

Começar por este documento e pela branch indicada. Não recomeçar a investigação desde o princípio.

**Primeira tarefa recomendada:** auditar a versão actual do ficheiro SQL de triggers no GitHub e confirmar que a evidência descrita nas secções 4–8 corresponde à versão exacta do repositório. Em seguida, fechar o circuito das transferências (CM2 + TS + BI + SL + ST/SA). Só depois retomar a produção.

Ao continuar:
- não pedir ao utilizador para repetir as decisões/conclusões já registadas;
- não inventar nomes de colunas: consultar `sys.columns`;
- não tratar Manufactor como sinónimo de PHC Gestão;
- não inferir semântica das tabelas auxiliares apenas pelo nome;
- separar conclusões demonstradas de hipóteses;
- actualizar este documento e a documentação canónica quando houver novas conclusões validadas.

## 15. Estado final desta sessão

- Conhecimento consolidado neste documento.
- Nenhum código de produto alterado.
- Nenhuma query executada na base de dados nesta sessão.
- Nenhum comportamento validado em runtime.
- Próximo passo: revisão da fonte SQL e confirmação de transferências.
