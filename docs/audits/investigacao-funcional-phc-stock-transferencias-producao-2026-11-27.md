# Investigação funcional PHC — circuitos de stock, transferências e produção

**Data de consolidação:** 27-11-2026  
**Estado:** investigação exploratória em curso; conclusões limitadas ao script de triggers e à documentação PHC analisados. Não constitui especificação universal do PHC.  
**Âmbito:** movimentos SL, saldos ST/SA, transferências entre armazéns, circuitos BI/FN/FI e produção PHC Manufactor.  
**Repositório:** `Jhouinho/PhcKnowledge`; relatório preparado na branch `docs/audit-stock-circuits-2026-11-27`.

## 1. Objectivo

Persistir a investigação funcional realizada sobre a forma como diferentes circuitos PHC originam movimentos de stock e como esses movimentos afectam os saldos. O objectivo é construir uma matriz rastreável:

`documento/origem → configuração → movimento SL → efeito em ST/SA`

A investigação procura também separar:
- movimentos físicos registados em SL;
- saldos agregados mantidos em ST e SA;
- informação de lote e grelha transportada nos movimentos;
- reservas e quantidades especiais;
- disponibilidade efectiva;
- circuitos de produção do PHC Gestão e do PHC Manufactor.

Não foram alterados dados, triggers, código, schemas, collectors ou contratos da plataforma.

## 2. Fontes e limites de proveniência

### 2.1 Script de triggers PHC

Fonte versionada no repositório: `docs/sources/triggers phc.sql`.

O script contém os triggers standard legíveis analisados nesta investigação. As referências de linha abaixo correspondem ao ficheiro de origem e devem ser usadas para reabrir os blocos exactos:

| Trigger / circuito | Referência no script | Assunto |
|---|---:|---|
| `tr_sl_insert` | linha 10251 (bloco de actualização de stock, aproximadamente 10251–10255) | Efeitos de SL em ST/SA e tratamento de `trfa` |
| `tr_sl_delete` | linha 5473 | Reversão dos efeitos do movimento |
| `tr_sl_update` | linha 12242 | Reversão e reaplicação após alteração |
| `tr_bi_insert` | linhas 6540–6615 | Criação de SL a partir de dossiers internos e transferências |
| `tr_bi_update` | linhas 11610–11674 | Transições de conferência e sincronização do movimento |
| `tr_fn_insert` | linha 8295 | Criação de SL a partir de documentos de fornecedor |
| `tr_fi_insert` | linha 11757 | Criação de SL a partir de linhas de facturação |
| `tr_fi_update` | linha 8228 | Actualização/sincronização de SL |
| `tr_fi_delete` | linha 11761 | Eliminação de movimentos associados à linha FI |
| `tr_bi_delete` / anulação do dossier | linha 11588 e linha 11765 | Reversão/limpeza de movimentos e registos auxiliares |

As referências de linha são pontos de entrada para a revisão do código; a interpretação deve considerar o bloco condicional completo, não apenas a linha indicada.

### 2.2 Documentação PHC Manufactor

Foram analisados artigos/documentos do Manual Técnico relacionados com:
- **Consumos PHC CS Manufactor**;
- **Consumos no Touch-Screen**;
- **Suporte Documental para Produção**.

O material descreve o processo funcional do Manufactor e a integração documental com PHC Gestão. O ZIP do Manual Técnico existente em `docs/kb-manual-tecnico.zip` é material de origem; este relatório regista apenas sínteses próprias. Não reproduz artigos, imagens ou assets proprietários.

A documentação do Manufactor não prova, por si só, a implementação exacta de uma integração em todas as instalações ou versões. A ponte técnica entre movimentos do Manufactor e a tabela SL do PHC Gestão continua por demonstrar.

### 2.3 Âmbito da evidência

Este relatório combina:
1. comportamento explícito no script de triggers;
2. documentação funcional PHC;
3. interpretações resultantes do cruzamento dessas fontes.

Não foi feita nesta etapa uma validação transaccional numa instalação PHC com movimentos reais. Não se deve apresentar as conclusões como observação empírica de execução, nem como comportamento universal de todas as versões, módulos e configurações.

## 3. Modelo base: movimentos, saldos e disponibilidade

| Conceito | Estrutura / circuito | O que representa nesta investigação |
|---|---|---|
| Movimento | `SL` | Registo do acontecimento de stock, com artigo, quantidade, armazém e atributos aplicáveis |
| Stock global | `ST` | Agregado de stock por artigo |
| Stock por armazém | `SA` | Agregado por artigo e armazém |
| Stock por lote | `SAL` | Estrutura de saldo detalhado cuja manutenção ainda não foi identificada |
| Stock por grelha | `SX` | Estrutura relacionada com cor/tamanho; manutenção das quantidades ainda não identificada |
| Reservas/cativos | `ST.qttcat` e circuitos associados | Quantidades reservadas/cativadas; não equivalem a movimento físico |
| Quantidade especial de utilizador | `ST.usrqtt` | Quantidade especial associada a processos como quantidade a fabricar; não equivale a entrada física |

A distinção fundamental é:
- **Movimento:** o que aconteceu;
- **Saldo:** o valor agregado que o PHC mantém;
- **Disponibilidade:** o que pode ser utilizado num contexto, considerando reservas, cativos, armazéns e outras regras.

Não inferir disponibilidade apenas a partir de ST ou SA.

## 4. Como SL actualiza ST e SA

### 4.1 Inserção

A leitura de `tr_sl_insert` demonstra que o trigger:
1. verifica a data do movimento face ao último inventário lançado com jusante;
2. garante a existência de registo SA para a combinação de artigo e armazém;
3. actualiza o stock global em ST;
4. actualiza o stock por armazém em SA;
5. actualiza acumulados de entradas/saídas e, conforme o ramo/configuração, dados de valorização e último movimento.

No ramo observado, a classificação do código de movimento é:
- `CM < 50`: entrada, acrescenta quantidade;
- `CM >= 50`: saída, retira quantidade.

Esta é a regra implementada no trigger analisado. Não deve ser generalizada a outros circuitos sem validar a configuração e os códigos de movimento utilizados.

### 4.2 Alteração e eliminação

Os triggers de UPDATE e DELETE implementam lógica compensatória:
- eliminar um movimento reverte os efeitos anteriormente aplicados a ST/SA;
- alterar quantidade retira o efeito anterior e aplica o novo;
- alterar artigo ou armazém reverte o efeito na combinação anterior e aplica-o na nova;
- movimentos protegidos por inventário lançado com jusante podem ser excluídos da recalculação quando são anteriores à data de referência.

A lógica de reversão é importante: não se deve interpretar um UPDATE como simples substituição dos campos da linha SL sem considerar os efeitos compensatórios sobre os saldos.

### 4.3 Particularidade de `trfa`

O indicador `trfa` altera o tratamento dos acumulados:
- movimentos normais actualizam ST e SA;
- movimentos de transferência (`trfa=1)) não actualizam o agregado global ST através do ramo analisado;
- os movimentos de transferência actualizam os saldos SA;
- os acumulados globais de entradas/saídas também não são actualizados através desse ramo.

Interpretação suportada: a transferência muda a distribuição do stock por armazém sem alterar a quantidade global do artigo. O modelo de dois lados da transferência está descrito na secção seguinte.

## 5. Matriz de circuitos que alimentam SL

| Origem | Condição/configuração observada | Efeito |
|---|---|---|
| `BI` — dossier interno | `TS.stocks = 1`, referência preenchida, linha fora de conferência e dossier não anulado | Cria movimento SL |
| `FN` — documento de fornecedor | Configuração `CM1.folansl = 1` e condições da linha/documento | Pode criar movimento SL |
| `FI` — linha de facturação | `TD.lancasl = 1` e referência preenchida | Cria movimento SL |
| PHC Manufactor | Documentação descreve entradas de produto acabado e saídas de componentes no seu circuito de produção | Movimentos físicos documentados no Manufactor; chegada a SL do PHC Gestão ainda por provar |

### 5.1 BI — dossiers internos

O `tr_bi_insert` cria movimentos SL quando se verificam as condições de stock, referência, conferência e estado do dossier. O movimento transporta informação da linha, incluindo referência, armazém, quantidade, lote, cor e tamanho.

O `tr_bi_update` também trata transições de estado. Uma linha inicialmente em conferência (`emconf=1)) pode gerar movimento quando passa para fora de conferência (`emconf=0)); por isso, o movimento não tem necessariamente de ser criado no INSERT inicial.

O trigger também trata alterações e eliminação/anulação, revertendo reservas e movimentos associados conforme as condições.

### 5.2 FN — documentos de fornecedor

O `tr_fn_insert` utiliza `CM1.folansl` para determinar se o documento de fornecedor gera movimento SL. O mesmo circuito pode gerar movimentos de valorização em SV através de configurações distintas (`folansv` e `folansvgr`).

Conclusão: movimento físico em SL e movimento de valorização em SV são circuitos relacionados, mas não necessariamente coincidentes.

### 5.3 FI — facturação

O `tr_fi_insert` verifica `TD.lancasl=1` e usa, entre outros:
- `TD.cmsl`: código de movimento;
- `TD.cmsln`: descrição;
- `FI.armazem`: armazém;
- `FI.lote`: lote;
- `FI.cor` e `FI.tam`: dimensões da grelha.

O trigger aplica inversão de sinal à quantidade quando `FT.tipodoc=3`. A natureza do documento influencia o sinal, enquanto a configuração do tipo de documento determina se há lançamento em SL.

O percurso de alteração pode actualizar o movimento existente ou criar outro quando determinados campos mudam. A eliminação da linha FI elimina os movimentos SL associados. Não se deve resumir o circuito como um INSERT isolado: UPDATE, DELETE e anulação do cabeçalho também fazem parte do ciclo de vida.

## 6. Transferências entre armazéns

### 6.1 Estrutura criada a partir de BI

O `tr_bi_insert` revela que, quando o tipo de dossier tem `TS.stocks=1` e `TS.trfa=1`, o trigger pode criar dois registos SL para a mesma linha BI:

| Movimento | Armazém | Código | Identificação |
|---|---|---|---|
| Principal | `BI.armazem` | `TS.cmstocks` | `SL.slstamp = BI.bistamp` |
| Complementar | `BI.ar2mazem` | `TS.cm2stocks` | `SL.slstamp = 'T' + BI.bistamp` |

Ambos transportam a mesma referência, quantidade, lote e dimensões da grelha. A criação dos dois registos é evidência do script, não uma afirmação de que toda a transferência em todas as instalações segue necessariamente este mecanismo.

### 6.2 Sentido dos movimentos

O sentido efectivo de entrada/saída depende da configuração dos códigos CM utilizados. A estrutura sugere os dois lados da transferência, mas é necessário confirmar:
- os valores concretos de `TS.cmstocks` e `TS.cm2stocks`;
- os registos correspondentes em CM2 e a respectiva classificação de entrada/saída;
- se os códigos têm efeitos opostos na configuração analisada;
- a simetria do comportamento em UPDATE, DELETE e anulação;
- se ambos os movimentos são sempre gerados em todos os cenários de transferência.

**Estado:** estrutura de dois movimentos demonstrada no código; sentido funcional e validação de um exemplo real ainda pendentes.

## 7. Produção: PHC Gestão e PHC Manufactor não devem ser confundidos

### 7.1 Campos de produção no circuito BI

O trigger BI selecciona `TS.cm2stocks` em vez de `TS.cmstocks` quando:
- `TS.producao=1` e a linha BI tem `producao=1`; ou
- se verifica a condição de troca de equivalência com `TS2.trocaequi=1) e `BI.trocaequi=1`.

A descrição também muda para `TS.cm2desc`.

Isto demonstra a selecção de um código alternativo para o movimento da linha. **Não demonstra, por si só, explosão da composição, consumo automático de componentes ou criação automática de uma entrada de produto acabado.**

### 7.2 Relação entre linhas BI

Quando `BI.obistamp` está preenchido, o trigger actualiza a linha de origem:
- incrementa `BI.qtt2` pela quantidade executada;
- actualiza dados do documento que executou a operação;
- em determinadas configurações, actualiza também `BI.partes2`.

É um mecanismo de execução de linhas relacionadas. Não está demonstrado que seja exclusivo da produção nem que represente necessariamente a relação produto acabado/componentes de uma ordem de fabrico.

### 7.3 Manufactor: ordem de fabrico e consumos

A documentação PHC Manufactor descreve uma ordem de fabrico com:
- artigo a fabricar;
- quantidade a produzir;
- armazém e lote de destino;
- estrutura/grelha de componentes;
- operações e execução.

A documentação distingue vários momentos:
1. criação da ordem;
2. confirmação e validação de necessidades;
3. planeamento e execução;
4. entrada de produção, que pode ser parcial.

A confirmação pode ter consequências sobre necessidades e reservas; uma ordem apenas lançada não deve ser tratada como equivalente a uma ordem confirmada.

Os documentos de consumos descrevem dois modos:
- **Automático:** consumos calculados a partir das quantidades declaradas como produzidas, da estrutura de componentes da operação e dos parâmetros gerais;
- **Manual:** o utilizador escolhe a ordem, operação e componentes a consumir.

Em ambos os casos, o registo dos consumos gera movimentos de saída dos componentes.

No Touch-Screen, a entrada do produto acabado pode ser registada imediatamente e os consumos podem ser processados mais tarde, por exemplo no fecho do turno ou na saída do terminal. Existe uma configuração para processar os consumos imediatamente (online).

Consequência: entrada do produto acabado e consumo das matérias-primas não têm necessariamente de ocorrer no mesmo instante.

### 7.4 Integração documental Manufactor → PHC Gestão

A documentação de suporte documental descreve dois dossiers internos no PHC Gestão:
- **Reservas de componentes:** dossier configurado para reserva/cativação, reflectindo as necessidades dos componentes;
- **Quantidade a fabricar:** dossier que movimenta quantidade de utilizador, reflectindo a quantidade especial do artigo a fabricar.

No circuito BI foram encontrados efeitos sobre `ST.qttcat` (reservas/cativos) e `ST.usrqtt` (quantidade de utilizador). Estes efeitos não devem ser confundidos com movimentos físicos em SL.

Assim, devem manter-se separados três fluxos:
1. reserva/cativação de componentes;
2. quantidade especial a fabricar;
3. movimentos físicos de entrada do produto acabado e saída dos componentes.

A documentação do Manufactor descreve estes fluxos funcionais, mas **não prova como os movimentos físicos são transferidos para a tabela SL do PHC Gestão**.

### 7.5 Tabelas auxiliares e lacunas

O script BI contém referências a `OFC`, `OCI`, `OMI`, `OCP` e `OPT), associadas a linhas BI e a operações de limpeza/actualização. Foram observadas relações de associação através de stamps e efeitos de triggers auxiliares, incluindo actualizações de quantidades atribuídas em documentos de fornecedor e linhas BI2.

A evidência actual não permite classificar com segurança estas tabelas como componentes, operações, consumos ou entradas de fabrico. Não lhes atribuir semântica apenas com base nos nomes.

## 8. SAL e SX — lacuna de mecanismo

Os triggers SL analisados demonstram actualização de ST e SA. Transportam também informação de lote, cor e tamanho nos movimentos, mas isso não demonstra que mantenham os saldos detalhados.

| Estrutura | Evidência actual | Estado |
|---|---|---|
| ST | Actualização através dos movimentos SL | Demonstrado no script |
| SA | Actualização por artigo/armazém através de SL | Demonstrado no script |
| SAL | Não foi encontrada actualização de saldo nos triggers SL analisados | Mecanismo por identificar |
| SX | Triggers SGC/SGT mantêm referências/configuração de cores e tamanhos; não foi demonstrada actualização de quantidades por SL | Mecanismo de saldo por identificar |

A conclusão correcta é que a lógica responsável por SAL e SX não está identificada no conjunto de triggers analisado. **Não concluir que o PHC não actualiza SAL ou SX.**

Foi também encontrado código SQL específico de cálculo de inventário, com cursor e tratamento de referência, armazém, lote e movimentos de transferência. Isto demonstra que existe lógica de inventário que percorre movimentos com essas dimensões, mas não prova, por si só, o mecanismo de manutenção permanente de SAL/SX.

## 9. Estado consolidado da evidência

| Questão | Estado | Qualificação |
|---|---|---|
| SL actualiza ST e SA | Demonstrado no script | Leitura estática |
| BI pode originar SL | Demonstrado no script | Condicionado por TS, referência, conferência e estado |
| FN pode originar SL | Demonstrado no script | Condicionado por CM1.folansl |
| FI pode originar SL | Demonstrado no script | Condicionado por TD.lancasl e referência |
| Alteração/eliminação revertem e reaplicam efeitos | Demonstrado no script | Considerar inventário e condições dos triggers |
| Transferência BI pode criar dois SL | Demonstrado no script | Armazéns BI.armazem e BI.ar2mazem |
| Transferência altera SA sem alterar ST global no ramo trfa | Demonstrado no script | Sentido dos códigos carece de validação |
| Manufactor regista entrada de produto acabado | Documentado | Manual PHC Manufactor |
| Manufactor gera saídas de consumo de componentes | Documentado | Modos automático e manual |
| Entrada e consumos podem ocorrer em momentos diferentes | Documentado | Touch-Screen |
| Reservas e quantidade a fabricar são reflectidas em dossiers CL | Documentado | Não são movimentos físicos |
| BI.producao prova explosão da composição | Não demonstrado | Não inferir |
| Manufactor escreve directamente em SL do PHC Gestão | Por provar | Ponte técnica por identificar |
| Tabelas OFC/OCI/OMI/OCP/OPT representam componentes/consumos | Por provar | Semântica não fechada |
| SAL é actualizado por SL | Não identificado | Não equivale a ausência de funcionalidade |
| SX mantém quantidades por SL | Não identificado | SGC/SGT encontrados apenas para configuração/referências |

## 10. Próximos passos de investigação

Prioridade 1 — **fechar transferências**:
1. ler integralmente os ramos de `tr_bi_insert`, `tr_bi_update`, `tr_bi_delete` e `tr_bo2_update`;
2. identificar a configuração CM2 de `TS.cmstocks` e `TS.cm2stocks` para os tipos de dossier relevantes;
3. confirmar o sinal/entrada/saída dos dois movimentos;
4. construir a sequência INSERT → UPDATE → DELETE/anulação;
5. quando houver instalação e dados de teste, validar uma transferência controlada e os dois saldos SA.

Prioridade 2 — **fechar a ponte de produção**:
1. identificar os mecanismos/tabelas de integração do Manufactor com PHC Gestão;
2. procurar identificadores que liguem ordem, operação, consumo, entrada de produção e dossier interno;
3. separar entrada parcial, entrada final e consumos processados posteriormente;
4. identificar a origem exacta dos movimentos físicos no PHC Gestão;
5. só depois ligar estes movimentos a SL, ST e SA.

Prioridade 3 — **identificar SAL/SX**:
1. procurar todos os triggers, procedures e funções que escrevem em SAL e SX;
2. procurar chamadas e rotinas da aplicação que façam manutenção dessas tabelas;
3. distinguir manutenção de configuração de grelha de manutenção de saldos;
4. cruzar com código de inventário e testes controlados.

Prioridade 4 — **validar origem e cobertura**:
1. identificar versão/build e proveniência do script de triggers;
2. comparar, numa instalação de teste, os triggers instalados com o script;
3. executar apenas testes controlados e read-only na análise inicial;
4. registar dados e resultados sem incluir informação proprietária de clientes no repositório público.

## 11. Regras de governação e continuidade

- Este relatório é uma auditoria/investigação funcional, não um contrato canónico do produto PHC Knowledge.
- Não alterar a base de dados PHC, triggers, código da aplicação, Scanner, Portal, schemas ou migrations como consequência automática destes achados.
- Não transformar hipóteses em factos; cada conclusão deve indicar a fonte e o âmbito.
- Não confundir PHC Gestão com PHC Manufactor.
- Não confundir reservas, quantidades especiais e movimentos físicos.
- Não concluir ausência de funcionalidade por não encontrar lógica num único script.
- Não copiar conteúdo integral da Enciclopédia ou dos manuais PHC para o repositório.
- Em novas sessões, continuar a partir deste relatório e actualizar o mesmo documento quando houver resultados materiais, evitando relatórios dispersos.
- As fontes brutas permanecem separadas das sínteses; pacotes de captura e dados de clientes não devem ser adicionados ao repositório público.

## 12. Síntese

A investigação já reconstruiu uma parte relevante do circuito de stock: BI, FN e FI podem alimentar SL sob configurações específicas; SL actualiza os agregados ST e SA; transferências BI podem gerar dois movimentos associados à mesma linha, um por armazém, com tratamento especial de `trfa).

A produção exige uma separação adicional. O PHC Manufactor documenta entradas de produto acabado, consumos de componentes, reservas e quantidades a fabricar, mas ainda não está demonstrada a ponte técnica que leva os movimentos físicos do Manufactor à SL do PHC Gestão. Os campos `BI.producao` e `TS.cm2stocks` não são prova dessa integração nem da explosão automática de componentes.

A manutenção de SAL e SX continua por identificar. A ausência de evidência nos triggers analisados não prova ausência de funcionalidade.

**Conclusão:** conhecimento preservado como investigação exploratória com proveniência, níveis de confiança e lacunas explícitas. O próximo passo é fechar transferências, identificar a ponte de produção e localizar o mecanismo de manutenção dos saldos detalhados.
