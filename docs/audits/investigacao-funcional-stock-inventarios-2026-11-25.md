# Investigação funcional PHC — Stock e inventários físicos

**Data de consolidação:** 25-11-2026  
**Estado:** consolidação de resultados observados em bases de dados distintas; algumas questões de reconstrução histórica permanecem abertas.  
**Âmbito:** modelo de stock, movimentos, inventários físicos e movimentos de acerto.  
**Repositório:** `Jhouinho/PhcKnowledge`.

## 1. Objectivo e regra de leitura

Este documento regista a investigação do funcionamento do stock PHC e dos inventários físicos, com especial atenção à relação entre:
- saldo actual por artigo/armazém;
- movimentos de stock;
- contagens físicas;
- lançamento do inventário;
- movimentos de acerto gerados pelo lançamento.

Foram analisadas bases de dados distintas. Os seus números e resultados são apresentados separadamente e não devem ser somados nem tratados como se pertencessem à mesma instalação.

Distinguem-se:
- **Estrutura observada:** tabelas/campos e ligações encontrados.
- **Resultado medido:** contagens ou valores obtidos nas bases analisadas.
- **Regra validada no caso:** comportamento confirmado num cenário concreto.
- **Hipótese/lacuna:** mecanismo ainda não demonstrado ou que exige validação noutra configuração.

A investigação é funcional e técnica; não constitui uma especificação universal para todas as versões e configurações PHC.

## 2. Modelo de dados funcional observado

| Tabela | Papel identificado nesta investigação | Limite importante |
|---|---|---|
| `ST` | Ficha e informação base do artigo. | Não representa, isoladamente, o saldo por armazém. |
| `SA` | Informação de stock/acumulados por artigo e armazém, incluindo reservas. | É um estado actual; não substitui o histórico de movimentos. |
| `SL` | Registo dos movimentos de stock. | A descrição do movimento, por si só, não determina o sinal/efeito. |
| `CM2` | Configuração/classificação do código de movimento, incluindo o indicador `entrada`. | Para interpretar o efeito é necessário consultar a configuração efectiva. |
| `STIC` | Cabeçalho do inventário físico. | O estado de lançamento não prova, sozinho, que cada linha tenha originado um movimento. |
| `STIL` | Linhas do inventário físico, com artigo, armazém e quantidade contada. | Não existe uma correspondência obrigatória de uma linha STIL para uma linha SL. |
| `SV` | Movimentos/elementos de valorização de stock identificados na análise. | O seu circuito e relação exacta com cada lançamento de inventário não ficaram integralmente reconstruídos. |
| `SE` | Informação de lotes. | A base blivo2026 analisada não tinha registos SE; não se pode extrapolar daí para instalações com gestão de lotes. |

A relação entre inventário e movimentos de stock foi observada através do cabeçalho: `SL.sticstamp` identifica o inventário associado. Não foi encontrado um campo `stilstamp` em SL que ligue cada movimento directamente a uma linha STIL. A análise por artigo e armazém é, por isso, necessária para comparar contagem e movimentos.

## 3. Interpretação dos movimentos de stock

### 3.1 O sinal do movimento

A investigação confirmou que não se deve deduzir entrada ou saída apenas a partir de `SL.cmdesc` ou de uma descrição textual.

A classificação efectiva observada usa a configuração do código de movimento em `CM2.entrada`:
- `entrada = 1`: quantidade considerada como entrada, com sinal positivo na reconstrução analisada;
- `entrada = 0`: quantidade considerada como saída, com sinal negativo na reconstrução analisada.

Assim, a reconstrução histórica usada no caso Digicanola somou `SL.qtt` com sinal determinado por `CM2.entrada`.

A descrição pode ser enganadora em casos concretos. A cadeia documental e a configuração do código de movimento devem ser seguidas até à origem do movimento.

### 3.2 Origem documental

A análise identificou movimentos de stock relacionados com diferentes documentos:
- dossiers internos, através de `BI.bistamp`;
- documentos de faturação, através de `FI.fistamp`;
- documentos de compra, incluindo a cadeia observada através de `FN.fnstamp`;
- inventários físicos, através de `SL.sticstamp`.

A estrutura efectiva de FI não deve ser presumida a partir de outra tabela ou versão. Num dos casos analisados, FI não permitiu estabelecer a ligação inicialmente presumida; foi a cadeia FN/`fnstamp` que permitiu localizar a origem do movimento.

**Regra prática validada:** para explicar um movimento, identificar primeiro a origem técnica disponível e seguir a cadeia documental; não confiar apenas em `cmdesc`, no nome do documento ou na data.

## 4. Inventários físicos — modelo funcional consolidado

O modelo funcional aceite nesta investigação é:

1. O cabeçalho STIC identifica o inventário físico.
2. As linhas STIL registam as quantidades contadas por artigo/armazém.
3. Ao lançar o inventário, o PHC compara a contagem com a situação teórica considerada para o lançamento.
4. Quando existe diferença a regularizar, o lançamento pode gerar movimentos SL de acerto, associados ao cabeçalho STIC.
5. Os movimentos de acerto alteram o saldo de stock; a contagem física permanece registada nas linhas STIL.

Este modelo não significa que:
- cada linha STIL tenha sempre um movimento SL;
- cada movimento SL corresponda a uma única linha STIL;
- `STIC.lanca = 1` seja prova suficiente de que todas as linhas foram regularizadas;
- os campos `stockatual` e `diferenca` de STIL contenham necessariamente os valores históricos usados no lançamento.

### 4.1 Campos e classificações observados

Na análise foram considerados, entre outros:
- `STIC.lanca`: estado de lançamento;
- `STIC.tipolanca`: tipo de lançamento;
- `STIC.zera` e `STIC.jusante`: opções/indicadores existentes no cabeçalho;
- `STIL.stock`: quantidade contada;
- `STIL.stockatual`, `STIL.diferenca` e `STIL.valstock`: campos presentes nas linhas, cujos valores observados não permitiram reconstruir de forma fiável o cálculo histórico.

Os valores destes campos devem ser interpretados no contexto da versão, do estado do inventário e do processo de lançamento. Não se deve inferir o algoritmo apenas pelo nome.

## 5. Base blivo2026 — inventário de stock e amostras

### 5.1 Dimensão da base analisada

Na base `blivo2026` foram observadas as seguintes contagens:

| Tabela | Registos |
|---|---:|
| ST | 919 |
| SA | 1.146 |
| SL | 84.654 |
| SV | 1.401 |
| STIC | 226 |
| STIL | 1.405 |
| SE | 0 |

Estas contagens descrevem exclusivamente a base analisada.

### 5.2 Inventários lançados e não lançados

Foram identificados:
- **176 inventários lançados** (`lanca = 1`), com 1.099 linhas STIL e 347 movimentos SL associados;
- **50 inventários não lançados** (`lanca = 0`), com 306 linhas STIL e zero movimentos SL associados.

Dos 176 inventários lançados, 126 tinham pelo menos um movimento SL associado e 50 não tinham movimentos associados.

Isto demonstra que o estado lançado não implica necessariamente a criação de movimentos de acerto. Um inventário pode ser lançado sem diferenças a regularizar, ou por outras condições do processo. A razão concreta tem de ser analisada por inventário e por artigo/armazém.

### 5.3 Exemplos analisados

| Inventário | Linhas STIL | Movimentos SL associados | Observação |
|---|---:|---:|---|
| FBA — `MA24011755255,526872557` | 4 | 4 | Movimentos com CM 51, descrição “Saída diversa”, origem IF; quantidade total observada 185. |
| JAbreu — `MA24020645375,346493775` | 9 | 2 | Quantidade total dos movimentos observados: 9. |
| V2 — `MA24020654595,988406364` | 9 | 0 | Inventário lançado sem movimentos SL associados. |

Nos três exemplos:
- `lanca = 1`;
- `tipolanca = 1`;
- `zera = 0`;
- `jusante = 0`.

Estes indicadores comuns não determinam, por si só, se são gerados movimentos nem quantos. A relação STIL→SL não é 1:1.

### 5.4 Campos de diferença nas linhas

Nos exemplos examinados, `STIL.stockatual` e `STIL.diferenca` surgiam a zero, mesmo quando a análise histórica levantava diferenças. Por isso, estes campos não foram aceites como fonte suficiente para reconstruir a comparação efectuada no momento do lançamento.

O saldo actual em ST/SA também não permite validar directamente o saldo histórico à data do inventário.

## 6. Base Digicanola — validação de reconstrução histórica

### 6.1 Dimensão da base analisada

Na base Digicanola foram identificadas, no momento da análise:

| Tabela | Registos |
|---|---:|
| ST | 10.426 |
| SA | 4.686 |
| SL | 159.197 |
| FI | 203.979 |
| SV | 186 |
| STIC | 18 |
| STIL | 29.974 |

Estes números pertencem à Digicanola e não devem ser confundidos com os da base blivo2026.

### 6.2 Inventário de 31-12-2025

Foi analisado o inventário:
- STIC: `ADM26012265140,130124227`;
- data: 31-12-2025.

A comparação foi feita por artigo/armazém, usando:
- a quantidade física registada em STIL;
- o saldo teórico reconstruído a partir dos movimentos SL até ao final do dia anterior ao inventário;
- o sinal de cada movimento determinado por `CM2.entrada`;
- exclusão dos movimentos de acerto do próprio inventário da reconstrução do saldo anterior.

Resultado:
- 442 combinações artigo/armazém analisadas;
- 441 coincidentes;
- 1 divergente;
- 239 combinações sem necessidade de movimento de acerto.

A análise mostra que a reconstrução histórica por movimentos pode ser útil, desde que se respeite a data de corte, a classificação CM2 e a exclusão dos próprios acertos do inventário.

### 6.3 Divergência explicada — artigo 52596

A única divergência identificada foi:
- artigo: `52596`;
- armazém: 1;
- quantidade física contada: 14;
- saldo histórico inicialmente reconstruído: 27;
- diferença esperada face a esse saldo: -13;
- movimento de acerto do inventário: +2;
- desvio aparente: 15 unidades.

A investigação seguiu os movimentos SL e a cadeia documental através de FN/`fnstamp`, identificando 15 unidades associadas à autofacturação:
- documento: `V/Factura SE2024/8654`;
- data: 16-12-2025;
- quantidade: 15;
- `CM2.entrada = 1`.

Apesar da designação documental “V/Factura”, a configuração efectiva do código de movimento classificava aquelas 15 unidades como entrada para efeitos da reconstrução.

Excluindo esse movimento, o saldo teórico passa de 27 para 12. Face à contagem física de 14, o acerto esperado é +2, coincidente com o movimento SL gerado pelo inventário.

**Conclusão do caso:** a divergência foi explicada por um movimento cuja classificação efectiva não era a que se poderia inferir apenas pelo nome do documento. Não foi encontrada evidência de uma falha geral do mecanismo de inventário PHC.

### 6.4 Limites desta validação

A validação de 442 combinações é uma confirmação forte para este inventário e esta base, não uma prova universal para todas as instalações.

A reconstrução depende de:
- histórico SL completo e preservado;
- classificação CM2 correcta e correspondente ao período;
- identificação correcta dos movimentos que pertencem ao próprio inventário;
- ligação documental suficiente para explicar movimentos excepcionais;
- coerência entre unidades, artigos, armazéns e datas.

Não se deve assumir que o saldo actual de SA ou ST permite reconstruir qualquer data passada sem recorrer ao histórico.

## 7. Outra amostra — base dfblock

Foi também identificado um conjunto de trabalho referente à base `dfblock`:
- 20 cabeçalhos STIC;
- 9.778 linhas STIL.

A análise seguinte — cruzar estado `lanca`, `tipolanca`, quantidades e movimentos SL associados — estava preparada, mas não ficou concluída no material recuperado para esta consolidação.

Por isso, não são aqui apresentadas conclusões sobre a taxa de geração de movimentos nem sobre o comportamento dos inventários desta base.

## 8. Conclusões consolidadas

1. ST é a ficha do artigo; SA representa acumulados/saldo por armazém; SL é o histórico de movimentos.
2. CM2.entrada é determinante para interpretar o sinal dos movimentos; a descrição textual não basta.
3. STIC identifica o inventário e STIL conserva as quantidades contadas por artigo/armazém.
4. O lançamento pode gerar movimentos SL de acerto associados ao cabeçalho STIC.
5. A relação entre STIL e SL não é 1:1; um inventário lançado pode não ter movimentos associados.
6. Em blivo2026, 176 inventários estavam lançados; 126 tinham pelo menos um movimento SL e 50 não tinham. Os 50 inventários não lançados não tinham movimentos associados.
7. STIL.stockatual e STIL.diferenca estavam a zero nos exemplos analisados e não foram considerados fonte fiável para reconstituir a comparação histórica.
8. A reconstrução de saldo histórico deve partir de SL, aplicar o sinal configurado em CM2 e respeitar a data de corte, excluindo os acertos do inventário em análise.
9. Na Digicanola, a divergência única no inventário de 31-12-2025 foi explicada por uma autofacturação cuja classificação CM2 indicava entrada.
10. A validação Digicanola não demonstra uma regra universal; demonstra que seguir a cadeia documental e a configuração CM2 explica uma divergência que parecia ser um erro do inventário.

## 9. Questões em aberto

1. **Algoritmo completo de lançamento:** não foi reconstruída toda a lógica interna que decide, por linha, se gera movimento e como determina os valores.
2. **Inventários lançados sem SL:** identificar, em cada caso, se não havia diferença, se houve outra forma de regularização ou se existem condições adicionais.
3. **STIL.stockatual/diferenca/valstock:** apurar quando são preenchidos, se são temporários, recalculados ou usados apenas em determinados tipos de lançamento.
4. **Valorização:** a presença de SV foi identificada, mas o circuito completo entre inventário físico, preço escolhido e valorização/lotes não foi fechado nesta investigação.
5. **Lotes e localizações:** a base blivo2026 não tinha registos SE; é necessário validar o comportamento em instalações com lotes e/ou localizações.
6. **dfblock:** concluir o cruzamento dos 20 STIC e 9.778 STIL com estado de lançamento e movimentos SL.
7. **Histórico incompleto:** definir como detectar movimentos em falta, alterações retroactivas, documentos anulados/reprocessados ou códigos CM2 alterados, que podem comprometer uma reconstrução histórica.

## 10. Regras de governação

- Não alterar dados de stock ou inventários para testar hipóteses.
- Não usar apenas ST/SA para afirmar qual era o stock numa data passada.
- Não deduzir o sinal de SL apenas pela descrição do movimento.
- Não tratar `lanca = 1` como prova de que todas as linhas geraram movimentos.
- Não assumir relação 1:1 entre STIL e SL.
- Não extrapolar resultados de blivo2026, Digicanola ou dfblock para todas as instalações.
- Manter separados stock físico, stock acumulado, reservas, movimentos e valorização.
- Só fechar as lacunas com evidência adicional de código/configuração ou teste controlado.

## 11. Estado

**Consolidado:** modelo ST/SA/SL/STIC/STIL, papel de CM2.entrada, ausência de relação 1:1 STIL→SL, resultados medidos em blivo2026 e validação do inventário Digicanola de 31-12-2025.

**Parcial:** valorização SV, campos históricos de STIL e comportamento dos inventários sem movimentos.

**Por concluir:** algoritmo interno completo de lançamento, amostra dfblock e validação em bases com lotes/localizações.

Este documento consolida o conhecimento já demonstrado. Não declara encerrada a investigação técnica dos inventários.
