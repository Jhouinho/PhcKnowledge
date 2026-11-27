# Investigação funcional PHC — Dossiers internos, stock cativo e pré-cativo

**Data de consolidação:** 25-11-2026  
**Estado:** consolidação documental da evidência disponível; investigação técnica ainda aberta nas lacunas identificadas.  
**Âmbito:** dossiers internos (BO/BO2/BI), cópia entre dossiers, reservas, stock cativo e pré-cativo.  
**Base de referência:** CSDefault202603, quando indicado.  
**Repositório:** `Jhouinho/PhcKnowledge`.

## 1. Objectivo e método

Este documento regista os resultados já obtidos na análise funcional de dossiers internos e do circuito de stock cativo/pré-cativo.

Foram cruzadas três classes de evidência:

1. **Enciclopédia PHC:** documentação funcional sobre configuração, cópia de dossiers, stock cativo, pré-cativo e monitor de cativação.
2. **Script de triggers PHC:** código SQL legível fornecido para análise, em particular os triggers de BO, BO2 e BI.
3. **Captura/configuração e registos de assistência:** elementos da CSDefault202603 e casos concretos de suporte, usados apenas dentro do seu âmbito.

Ao longo do documento distinguem-se:
- **Documentado pela Enciclopédia:** comportamento descrito oficialmente pela documentação consultada.
- **Observado no script:** condição ou operação directamente encontrada no código fornecido.
- **Observado na configuração/captura:** valor ou estrutura efectivamente apresentada para a base analisada.
- **Evidência de assistência:** comportamento ou solução registados num caso concreto, não universalizável.
- **Interpretação:** leitura conjunta das fontes, limitada à evidência.
- **Por validar:** mecanismo ou comportamento que exige código adicional, consulta read-only ou teste controlado.

Não se assume que o comportamento observado seja universal a todas as versões, módulos, configurações ou instalações PHC.

## 2. Proveniência e limites das fontes

### 2.1 Enciclopédia PHC

Foram consultados, entre outros, os artigos:
- **Configuração de Dossiers Internos** — `configuraçãodedossiers.htm`.
- **Constituição da Tabela de Dossiers** — `constituiçãodatabeladedossiers.htm`.
- **Dossiers Internos** — `dossiersinternos.htm`.
- **Funcionamento do Stock Cativo e Pré-cativo** — `funcionamentodostockcativoepré-cativo.htm`.
- **Monitor de Cativação de Stock** — `monitordecativaçãodestock.htm`.
- **Coluna Informativa do Stock Cativo** — `colunainformativadostockcativo.htm`.
- **Registo de Dossiers** — `registodedossiers.htm`.
- **Opções na Cópia de Dossiers** — `opçõesnacópiadedossiers.htm`.
- **Estorno de Dossier Interno** — `estornodedossierinterno.htm`.

Foram ainda identificados artigos específicos para PHC CS Web/Gestão Web. As particularidades dessas variantes não são generalizadas para o comportamento desktop.

A Enciclopédia é material oficial PHC. Este relatório conserva apenas títulos, nomes de ficheiro e sínteses próprias; não reproduz artigos integrais, imagens ou assets.

### 2.2 Script de triggers

Foi analisado o ficheiro de triggers PHC fornecido para esta investigação:
- `docs/sources/triggers phc.sql` (referência de origem no repositório).
- Cópia textual analisada: 1 215 789 bytes, 21 713 linhas.

Triggers relevantes e linhas da cópia textual:
| Tabela | Trigger | Evento | Linha |
|---|---|---|---:|
| BO | `tr_bo_insert` | INSERT | 389 |
| BO | `tr_bo_delete` | DELETE | 1973 |
| BO | `tr_bo_update` | UPDATE | 2448 |
| BI | `tr_bi_insert` | INSERT | 6535 |
| BI | `tr_bi_delete` | DELETE | 11588 |
| BI | `tr_bi_update` | UPDATE | 11592 |
| BO2 | `tr_bo2_update` | UPDATE | 11765 |

As linhas são referências à cópia textual analisada e podem não corresponder a linhas de outras versões do script.

### 2.3 CSDefault202603 e assistência

Na configuração consultada:
- BO, BI e BI2 estavam vazias, sem documentos transaccionais.
- TS e TS2 continham nove registos cada, associados aos dossiers internos configurados.
- Os dados da CSDefault permitem conhecer configuração, não validar um circuito transaccional real.

Os registos de assistência são evidência contextual de casos concretos. Não substituem a Enciclopédia nem o código standard, e não devem ser convertidos em regras gerais.

## 3. Estrutura funcional observada: BO, BO2, BI e SL

A leitura do script e das relações disponíveis sustenta a seguinte estrutura:

- **BO:** cabeçalho do dossier interno.
- **BO2:** informação complementar do cabeçalho; o código usa `BO.bostamp = BO2.bo2stamp`.
- **BI:** linhas do dossier; cada linha é identificada por `BI.bistamp` e liga ao cabeçalho através de `BI.bostamp`.
- **SL:** movimentos de stock; o script associa movimentos à linha através de `SL.bistamp = BI.bistamp`.
- **BI.obistamp:** campo usado pelo código para associar uma linha de destino à linha de origem de outro dossier.

A utilização de `BI.obistamp` no código é evidência de uma ligação funcional entre linhas de origem e destino. Não se deve classificar essa ligação como relação PHC REL/DTL sem consultar e validar a declaração correspondente.

## 4. Comportamentos dos triggers observados

### 4.1 BO — inserção e alteração: validação de assinatura digital

Os triggers `tr_bo_insert` e `tr_bo_update` contêm uma validação condicional de assinatura digital.

O bloqueio depende da conjugação de condições, incluindo:
- existência do registo BO2 associado ao BO;
- assinatura ainda vazia em BO2;
- configuração do tipo de dossier em TS/TS2;
- `TS2.assinaimp = 0`;
- `TS2.docsimport = 0`;
- `TS2.assinadoc = 1`;
- número do documento `BO.obrano > 0).

**Conclusão limitada:** o código não demonstra que todos os dossiers internos exigem assinatura digital. A validação só é aplicada quando as condições do ramo são satisfeitas.

### 4.2 BO — eliminação

O trigger `tr_bo_delete` elimina o registo BOTXT associado ao BO.

Este facto não demonstra que a eliminação total do dossier se limite a BOTXT, nem permite concluir que BO e BI sejam eliminados exclusivamente por este trigger. A operação de eliminação pode envolver outros mecanismos da aplicação ou outros triggers.

### 4.3 BO2 — anulação lógica

O trigger `tr_bo2_update` trata a transição de `BO2.anulado = 0` para `BO2.anulado = 1`.

No ramo observado, o código:
- reverte quantidades acumuladas de reservas de cliente, stock cativo, reservas de fornecedor, reservas de utilizador e quantidade em recepção, em ST/SA;
- elimina movimentos SL associados;
- limpa estruturas auxiliares, incluindo OFC, OCI, OMI, OCP, OPT e BIPRECAT;
- actualiza a linha de origem através de `obistamp`, quando aplicável.

**Conclusão:** a anulação observada é uma alteração de estado com reversão de efeitos derivados. Não é uma prova de eliminação física de BO ou BI.

### 4.4 BI — inserção e movimentos de stock

O trigger `tr_bi_insert` pode criar movimentos SL quando se verificam, entre outras, as condições:
- `TS.stocks = 1`;
- referência preenchida;
- `BI.emconf = 0`;
- `BO2.anulado = 0).

Quando `TS.trfa = 1`, o código pode criar um segundo movimento, com stamp derivado de `T + BI.bistamp`, no armazém `BI.ar2mazem`, usando os códigos CM2 configurados.

A criação e o sentido dos movimentos dependem da configuração TS/TS2 e dos códigos de movimento. Não se deve concluir que toda a linha BI cria sempre SL.

### 4.5 BI — reservas e quantidades

O trigger actualiza acumulados em ST/SA com base, em vários ramos, na diferença:

`BI.qtt - BI.qtt2`

As flags da linha determinam os acumulados tratados:
| Flag BI | Acumulado observado |
|---|---|
| `rescli` | `ST.qttcli` / `SA.rescli` |
| `cativo` | `ST.qttcat` / `SA.rescat` |
| `resfor` | `ST.qttfor` / `SA.resfor` |
| `resusr` | `ST.usrqtt` |
| `resrec` | `ST.qttrec` / `SA.qttrec` |

Há ramos em que só é adicionada quantidade quando `qtt - qtt2 > 0`, bem como criação de registos SA em falta sob condições específicas.

A tabela representa os campos e efeitos observados no script; não substitui a análise integral das condições de cada ramo.

### 4.6 BI — ligação à linha de origem

Quando `BI.obistamp` está preenchido, o trigger de inserção actualiza a linha de origem:
- guarda a referência ao documento/linha de destino;
- incrementa `qtt2` pela quantidade do destino;
- trata também partes2 quando aplicável.

Este comportamento é coerente com a utilização da segunda quantidade da linha de origem nas operações de passagem/cópia entre dossiers. A correspondência funcional exacta com cada opção de cópia deve, contudo, ser interpretada em conjunto com a documentação da Enciclopédia.

### 4.7 BI — alteração e eliminação

O trigger `tr_bi_update` trata transições de `emconf` entre 1 e 0, revertendo ou criando movimentos SL e reservas. Também contempla alterações de quantidade, quantidade2, armazém, flags de reserva, estado de fecho e outros campos. Em termos gerais, reverte os efeitos anteriores e aplica os efeitos correspondentes aos novos valores.

O trigger `tr_bi_delete`:
- reverte reservas em ST/SA;
- elimina SL associado;
- reverte quantidade2/partes2 na origem e limpa a referência ao documento de destino, quando aplicável;
- elimina estruturas auxiliares sob condições específicas.

A flag `BI.fechada` condiciona alguns efeitos de reservas. O ciclo completo de fecho do dossier não ficou reconstruído nesta passagem.

## 5. Cópia de dossiers internos — opções documentadas

A Enciclopédia descreve a cópia de linhas entre dossiers internos e também a passagem para documentos de faturação e compras. A operação pode ser iniciada pelo botão **Copiar as linhas de outro dossier** ou pela configuração **Pergunta se quer copiar de outro dossier**.

As opções mais relevantes para a investigação são:

| Opção | Comportamento descrito |
|---|---|
| Nº de unidades a copiar | Multiplica as quantidades de origem; aplicável à cópia entre dossiers, não à passagem para factura/compra. |
| Só passa linhas com quantidade maior que zero | Exclui linhas cuja quantidade não seja positiva. |
| Não passa a referência dos artigos | Não preenche a referência no destino; a documentação apresenta como exemplo evitar duplicar uma saída de stock quando o destino também movimenta stock. |
| Verifica se existe stock em armazém antes de passar a linha | Alerta para falta de stock; é relevante em cenários em que o controlo geral de stock está desligado e o stock cativo é considerado. |
| Fecha o dossier após ser copiado | Depois de gravar o destino, pode surgir uma janela para seleccionar os dossiers de origem a fechar. Não significa fecho automático sem confirmação/seleção. |
| Actualiza preços e descontos | Obtém preços/descontos da ficha de artigos ou de dossiers de preços, conforme a opção escolhida. |
| Actualiza a segunda quantidade do dossier de origem | A quantidade colocada no destino é reflectida em Qtt2 da origem; exige que a origem esteja configurada para Qtt2 e está documentada para cópia entre dossiers internos. |
| Passa o saldo de quantidades do dossier de origem | Passa Qtt1-Qtt2 para o destino; a origem tem de utilizar Qtt2. |
| Passa a quantidade para a quantidade 2 | Passa Qtt1 da origem para Qtt2 do destino; só entre dossiers internos. |
| Passa a data do dossier do documento de origem | Usa a data do dossier de origem na cópia entre dossiers, quando a opção e o contexto de cópia o permitem. |
| Passa os componentes do documento original | Controla a manutenção dos componentes do documento original ou o recálculo pela ficha do artigo. |
| Copia armazém de destino para armazém do documento de destino | Em transferências entre armazéns, usa o armazém de destino da origem no documento de destino. |
| Limpa quantidade 2 na cópia de dossier | Não copia Qtt2/Partes fornecidas para o destino, excepto quando as opções de saldo ou passagem de Qtt1 para Qtt2 prevalecem. |
| Copia dados do cabeçalho | Pode substituir dados do cliente, mas exige seleccionar pelo menos um campo de cabeçalho; copiar apenas linhas não actualiza o cabeçalho. |
| Inclui nome e número do documento de origem | Preserva informação de rastreio documental. |

A Enciclopédia esclarece ainda que, se as opções de saldo e passagem de Qtt1 para Qtt2 não estiverem activas, Qtt2 da origem passa directamente para o destino. O comportamento do botão junto às linhas é descrito como equivalente neste aspecto.

Algumas opções de cópia têm particularidades na configuração de destino (por exemplo, câmbio, descontos e decimais). As variantes Web e Gestão Web têm documentação própria e não devem ser tratadas como equivalentes em todos os detalhes ao desktop.

## 6. Stock cativo — funcionamento documentado

### 6.1 Dependências de configuração

Na configuração de Dossiers Internos, página Integração:
- **Permite Cativar Stock** surge quando está activa a opção **Movimenta as reservas de clientes nos stocks**.
- **Permite Pré-Cativar Stock** surge quando está activa a opção **Movimenta as reservas de fornecedores nos stocks**.

A opção **Permite Cativar Stock** disponibiliza o campo lógico `Cativo` nas linhas. Quando activo, indica que a quantidade dessa linha está cativada para aquele dossier.

A finalidade é reservar stock para um cliente, impedindo que outra encomenda disponibilize as mesmas unidades.

### 6.2 Disponibilidade

A Enciclopédia apresenta os seguintes cálculos:

**Documentos independentes do stock cativo**

`Stock actual + Quantidade em Recepção - Quantidade Cativada`

**Disponibilização do stock cativo do próprio documento**

`Stock actual + Quantidade em Recepção - Quantidade Cativada + Quantidade Cativada do documento`

Não confundir estas fórmulas com fórmulas de painéis logísticos personalizados encontrados em registos de assistência. Uma fórmula customizada que subtraia também reservas de cliente não é, por si só, a regra geral PHC.

### 6.3 Exemplo funcional da Enciclopédia

O exemplo documentado parte de:
- stock actual: 20;
- reservado a clientes: 0;
- quantidade cativada: 0;
- stock previsto: 20.

Ao criar um dossier de reserva para um cliente com 5 unidades cativadas:
- stock actual: 20;
- reservado a clientes: 5;
- quantidade cativada: 5;
- stock previsto: 15.

As unidades cativadas só podem ser utilizadas nos percursos associados à reserva, nomeadamente:
1. facturação do próprio dossier;
2. cópia para outro dossier interno de cliente, quando está activa a opção **Actualiza a segunda quantidade do dossier de origem**.

Se forem facturadas 5 unidades através do dossier de reserva, o exemplo passa a stock actual 15, reservado 0, cativado 0 e previsto 15.

Se forem facturadas 6, o exemplo apresenta stock actual 14, reservado 0, cativado 0 e previsto 14.

Se forem facturadas 4, o exemplo apresenta stock actual 16, reservado 1, cativado 1 e previsto 15. A unidade restante continua reservada e cativada.

A Enciclopédia indica que a quantidade cativada tem prioridade sobre o restante stock disponível quando se factura o dossier de reserva ou se copia para documento de faturação/dossier interno nos percursos descritos.

Um documento de faturação isolado, não associado à reserva, não pode consumir a quantidade cativada. No exemplo, uma saída isolada de 5 deixa stock actual 15, reservado 5, cativado 5 e previsto 10.

A quantidade cativada pode ser consultada por referência, lote, armazém e grelha cor/tamanho.

## 7. Pré-cativação — funcionamento documentado

### 7.1 Conceito

A pré-cativação permite associar a uma encomenda de cliente uma quantidade ainda não existente em stock, mas já encomendada a um fornecedor.

Exemplo da Enciclopédia:
- encomenda de cliente A: 10 unidades;
- encomenda de cliente B: 5 unidades;
- encomenda ao fornecedor: 20 unidades;
- 10 unidades pré-cativadas para A;
- 5 unidades pré-cativadas para B;
- 5 unidades ficam livres para stock ou outras pré-cativações.

### 7.2 Configuração e operações

Num dossier de cliente configurado para permitir cativar stock, as opções de linha incluem **Pré-Cativar Encomendas a Fornecedores**.

Num dossier de fornecedor configurado para permitir pré-cativar stock, existem opções para **Pré-Cativar para Encomendas de Clientes**.

A Enciclopédia descreve análises que permitem consultar:
- na encomenda de fornecedor, as encomendas de cliente pré-cativadas por ela;
- na encomenda de cliente, as encomendas sem encomenda de fornecedor associada/pré-cativada.

### 7.3 Conversão de pré-cativo em cativo

O parâmetro **A cativação de stocks, resultado de pré-cativação é automática** controla o processo após a entrada da mercadoria:

- **Activo:** o stock que entrou é automaticamente cativado para as linhas de cliente que pré-cativaram a encomenda ao fornecedor.
- **Inactivo:** o utilizador pode marcar/cativar manualmente, no Monitor de Cativação, as linhas cuja pré-cativação já tem stock disponível.

A recepção pode ocorrer por Compra, por outro dossier interno de entrada ou por Guia de Recepção (dossier interno que movimenta quantidade em recepção). A documentação diz que, após a recepção, são verificadas as encomendas de cliente que pré-cativaram a encomenda ao fornecedor e são cativadas as linhas que podem ser satisfeitas.

A funcionalidade não suporta Dossiers Internos em conferência. A Enciclopédia acrescenta que, ao pré-cativar stock, este fica sempre pré-cativado, estejam os dossiers em conferência ou não.

## 8. Monitor de Cativação de Stock — comportamento documentado

O Monitor apresenta linhas de dossiers internos configurados para permitir cativar stock, com artigos (não serviços), ainda não marcadas para cativação, não totalmente satisfeitas e com stock disponível no armazém.

O utilizador pode filtrar, ordenar, pesquisar, consultar linhas do dossier e alterar a quantidade **Fornecer**.

A documentação descreve as seguintes regras:
- Linhas ligadas a pré-cativação, para as quais o stock já está disponível, são automaticamente marcadas para cativar quando o parâmetro de cativação automática está desligado e existe stock disponível.
- Só é possível marcar quando **Fornecer** é menor ou igual a **Disponível**; caso contrário, é apresentado um aviso.
- Ao marcar/desmarcar uma linha, a disponibilidade é actualizada nas restantes linhas com a mesma referência/cor/tamanho/lote/armazém.
- Ao gravar, só é cativado stock efectivamente existente em armazém.
- Se **Fornecer** for reduzido, o PHC cria novas linhas no documento para a parte que fica por cativar.
- Se a linha estiver ligada a uma pré-cativação, a nova linha mantém a associação à mesma pré-cativação.
- O Monitor apresenta um relatório de resultado por documento e permite regressar à lista actualizada.

A ordenação inclui a coluna **Pré-Cativado já disponível**.

## 9. Cruzamento com o código SQL disponível

### 9.1 Reservas e cativo em BI

O script confirma que os efeitos de reserva/cativo são materializados em acumulados de ST/SA através das flags de BI e da diferença `qtt - qtt2`.

Isto é coerente com a documentação funcional de stock cativo, mas não explica, por si só, todo o processo aplicacional de cativação ou pré-cativação.

### 9.2 BIPRECAT

O script contém referências a BIPRECAT nos triggers de limpeza, incluindo:
- `tr_bi_delete`;
- `tr_bi_update`, em condições específicas relacionadas com transições de conferência;
- `tr_bo2_update`, na anulação lógica do dossier.

O inventário estrutural disponível identificou os nomes `biprecatstamp` e `bostamp` em BIPRECAT. Esta informação não constitui um inventário completo de colunas.

No script analisado não foi encontrado `INSERT INTO BIPRECAT` nem `UPDATE BIPRECAT`. As referências localizadas são operações de limpeza.

**Conclusão:** o script fornecido mostra que BIPRECAT participa no circuito e que as associações são limpas em determinados percursos. Não revela o mecanismo que cria ou actualiza essas associações.

### 9.3 BI2.precatstamp e EBIPRECAT

O inventário disponível também menciona `BI2.precatstamp` e a tabela `EBIPRECAT` (com os campos identificados `ebiprecatstamp`, `ebostamp` e `bostamp`).

Não existe evidência suficiente nesta passagem para associar esses elementos ao mecanismo de stock cativo/pré-cativo em análise. A sua função permanece por apurar; não devem ser incorporados no modelo funcional como se a ligação estivesse demonstrada.

### 9.4 Quantidade em recepção

O código usa `BI.resrec` e os acumulados `ST.qttrec` / `SA.qttrec`. Estes campos representam quantidade em recepção prevista no contexto observado. Não são, isoladamente, prova de entrada física nem da conversão de pré-cativo em cativo.

### 9.5 Ausência do mecanismo de conversão no script

Os triggers BI analisados tratam movimentos, reservas e reversões. No script fornecido não foi localizado o código que cria as associações BIPRECAT nem o procedimento que converte pré-cativo em cativo após a recepção.

Não se deve concluir que esse mecanismo não existe no PHC. Pode estar na lógica da aplicação, em procedures/funções/triggers não incluídos no ficheiro, ou noutro componente.

## 10. Evidência de assistência — âmbito limitado

### 10.1 Dependência de configuração

Um caso de assistência relativo à Mundo Escolar regista que, para configurar um dossier com stock cativo, foi necessário activar primeiro **Movimenta as reservas de clientes nos stocks**.

Este caso é coerente com a dependência descrita pela Enciclopédia. A regra geral é sustentada pela documentação; o caso de assistência é evidência adicional de uma configuração concreta.

### 10.2 Cópia e desaparecimento de reservas

Outro caso de assistência descreve que a cópia de uma Encomenda de Cliente para um dossier “Falta de Artigos” fazia desaparecer reservas. A solução registada variava com o percurso:
- para faturação, marcar **Fecha o dossier após ser copiado**;
- para cópia entre dossiers, desmarcar **Fecha o dossier após ser copiado** e **Actualiza a segunda quantidade do dossier de origem**.

Este registo demonstra um problema e uma solução num contexto concreto. Não permite concluir que estas opções tenham sempre o mesmo efeito em todos os circuitos, configurações ou versões.

### 10.3 Fórmulas logísticas personalizadas

Foi encontrado um painel logístico personalizado que calcula disponibilidade através de campos de stock/reservas. Essa fórmula é evidência de uma implementação específica e não deve ser apresentada como cálculo standard PHC.

## 11. Síntese consolidada

1. BO representa o cabeçalho, BO2 a informação complementar e BI as linhas do dossier interno; SL é associado às linhas através de `bistamp`.
2. A anulação lógica de BO2 reverte efeitos derivados, incluindo movimentos SL e acumulados de reserva, sem demonstrar eliminação física de BO/BI.
3. BI materializa movimentos de stock e reservas de acordo com a configuração do dossier, o estado de conferência/anulação e as flags da linha.
4. A segunda quantidade (`qtt2`) participa no cálculo das quantidades pendentes/reservadas e na ligação de linhas de destino à origem.
5. A Enciclopédia documenta opções de cópia que podem actualizar Qtt2 da origem, passar saldos e preservar associações entre dossiers.
6. O stock cativo é uma reserva de stock existente associada a um dossier/cliente; o stock pré-cativo associa encomendas de cliente a quantidades ainda encomendadas a fornecedor.
7. A conversão de pré-cativo em cativo é descrita funcionalmente pela Enciclopédia e pode ser automática ou manual via Monitor, conforme o parâmetro.
8. O script SQL fornecido não contém o mecanismo de criação/actualização das associações BIPRECAT nem a conversão pré-cativo → cativo.
9. As referências BIPRECAT encontradas no script são operações de limpeza; não bastam para reconstruir o ciclo completo.
10. A CSDefault202603 não tinha documentos BO/BI, pelo que não houve validação transaccional real nesta base.

## 12. Lacunas e trabalho seguinte

A investigação deve prosseguir sem alterar o Scanner, o Portal ou a base de dados. Quando houver acesso ao ambiente SQL Server, preparar uma recolha estritamente read-only para:

1. Obter o esquema físico completo de BIPRECAT, incluindo todas as colunas, tipos, chaves e índices.
2. Inventariar definições de procedures, funções, views e triggers relevantes, pesquisando referências a BIPRECAT, `precat`, `cativo`, `rescli`, `resfor` e ao Monitor.
3. Obter o texto integral dos triggers instalados e comparar com o script fornecido, identificando versão/build quando possível.
4. Inspeccionar definições de ecrãs/formulários apenas como evidência de personalização; `formb`/`formc` não devem ser confundidas com o código standard do Monitor.
5. Identificar, se possível, o mecanismo aplicacional que trata a recepção e a conversão de pré-cativo em cativo. O código do Monitor pode residir no executável PHC e não ser extraível por SQL.
6. Só depois de identificar o mecanismo, desenhar um cenário de teste controlado numa base de testes, cobrindo pré-cativação parcial, recepção parcial/total, cativação automática e manual, cópia para outro dossier, faturação, anulação e eliminação.

### 12.1 Capacidade de recolha do Scanner

A documentação/auditoria do Scanner já identificou uma capability de SQL Programmable para objectos como views, procedures, triggers e functions, mas a declaração da capability não prova que todos os tipos sejam efectivamente recolhidos, nem que as definições estejam disponíveis. A cobertura real tem de ser confirmada por tipo de objecto e pelo resultado da captura.

A capability PHC Screens/UI recolhe estruturas como `formb` e `formc`; estas podem ajudar a identificar personalizações, mas não são substituto do código standard do Monitor de Cativação.

Não alterar agora o Scanner para este efeito. Primeiro é necessário confirmar a cobertura actual e identificar a fonte real do mecanismo em falta.

## 13. Regras de governação

- Este documento regista investigação funcional; não é uma especificação universal do PHC.
- Não executar alterações na CSDefault202603.
- Não modificar triggers, tabelas, procedimentos, Scanner ou Portal com base apenas nestes achados.
- Não inferir comportamento da aplicação a partir de nomes de campos isolados.
- Não classificar uma relação como REL/DTL sem evidência da declaração.
- Não transformar casos de assistência ou código customizado em regras standard.
- Preservar a distinção entre documentação oficial, código observado, configuração concreta, assistência e hipótese.
- Actualizar este documento quando forem obtidas novas evidências materiais; evitar criar relatórios paralelos para a mesma investigação.

## 14. Estado da investigação

**Consolidado nesta passagem:** estrutura funcional BO/BO2/BI/SL, principais efeitos dos triggers disponíveis, opções documentadas de cópia de dossiers, funcionamento funcional do stock cativo/pré-cativo e limites do código SQL analisado.

**Em aberto:** esquema completo e mecanismo de escrita de BIPRECAT; conversão técnica de pré-cativo em cativo; código do Monitor/recepção; comparação com triggers instalados e validação transaccional numa base de testes.

A investigação não está fechada tecnicamente. O presente documento fecha apenas a consolidação do conhecimento que já tem suporte nas fontes consultadas.
