# Investigação técnica PHC — stock por grelha (SX/SXX)

**Data inicial:** 26-11-2026  
**Revisão comparativa:** 27-11-2026  
**Estado:** investigação documental/estática; manutenção de SX.stock identificada numa das exportações de triggers, mas divergente entre fontes. Não validada em runtime.  
**Âmbito:** relação entre movimentos de stock, grelhas, inventário físico e tabelas SX/SXX.  
**Repositório:** `Jhouinho/PhcKnowledge`.

## 1. Objectivo e regra de evidência

Esta investigação procura identificar como o PHC mantém o stock por artigo/armazém/cor/tamanho e qual a participação de `SX` e `SXX` nos circuitos de movimentos e inventário.

As conclusões abaixo distinguem:
- **Confirmado no SQL/view analisado:** operação ou referência explícita no material disponível.
- **Confirmado em documentação/registo de assistência:** comportamento descrito para um caso ou instalação.
- **Hipótese:** explicação possível ainda sem prova do mecanismo.
- **Por localizar:** artefacto ou rotina que não consta do material analisado.

Não se afirma que a ausência de uma operação no ficheiro de triggers prove que ela não exista na aplicação PHC ou noutros objectos SQL.

## 2. Modelo de dados identificado

| Tabela | Papel identificado | Limite |
|---|---|---|
| `ST` | Ficha/informação base do artigo e acumulados globais de stock. | Não representa o detalhe por armazém/grelha. |
| `SA` | Saldo/acumulado por artigo e armazém. | Não discrimina cor e tamanho. |
| `SL` | Movimentos de stock; transporta referência, armazém, quantidade e dimensões quando aplicáveis. | O movimento não é o próprio saldo por grelha. |
| `SX` | Saldo por artigo, armazém, cor e tamanho. | Falta schema completo e mecanismo de manutenção. |
| `SXX` | Objecto descrito no inventário de relações como «Stock por Armazém, Cor e Tamanho Global». | Não está demonstrado se é agregado, auxiliar ou materialização de SX. |
| `STIC` | Cabeçalho de inventário físico. | O algoritmo de lançamento não foi localizado. |
| `STIL` | Linhas de contagem, incluindo referência, armazém, quantidade, lote, cor e tamanho. | Não foi demonstrada uma relação 1:1 entre STIL e SL. |

### 2.1 Evidência das views

A view `V_WS_STKARTIGOS` consulta `SA` para apresentar stock por artigo/armazém.

A view `V_WS_STKARTIGOS_GRELHAS` consulta `SX` e expõe:
- referência;
- armazém;
- cor;
- tamanho;
- `SX.stock`.

Isto confirma que, neste interface de consulta, `SX` é fonte de saldo por combinação de grelha, e não apenas um catálogo de cores/tamanhos.

## 3. O que os triggers de movimentos demonstram

Esta revisão compara duas exportações SQL distintas, que não devem ser misturadas:

- **Fonte A — baseline `CSDefault202603`:** `docs/sources/triggers phc.sql`, blob SHA `f9949635b32e265129f2aed3803d12307633c842`. A pesquisa integral anteriormente efectuada não encontrou referências a `SX` ou `SXX` neste ficheiro.
- **Fonte B — exportação independente:** `PhcKnowledgeScanner/triggers phc.sql`, blob SHA `4ab635747718a8bee1e4243a0a9fcbb7d3ae0f39`. É uma exportação de outra origem PHC; a identidade da instalação não é reproduzida nesta documentação pública.

Os resultados de cada fonte são apresentados separadamente. A diferença pode decorrer de versão, configuração, módulos ou personalizações, mas a evidência actual não permite escolher uma explicação.

### 3.1 Origem dos movimentos

Os circuitos observados em `BI`, `FI` e `FN` podem criar ou gerir movimentos em `SL`. Nos movimentos de linhas BI/FI/FN analisados, cor e tamanho são transportados quando aplicáveis.

No caso das transferências BI, a criação dos dois movimentos SL conserva a dimensão da linha de origem, tal como conserva referência, quantidade e outros dados relevantes.

### 3.2 Actualização dos acumulados

**Fonte A — `CSDefault202603`:** nos triggers SL analisados (`tr_sl_insert`, `tr_sl_update` e `tr_sl_delete`) existem operações sobre `ST` e `SA`, mas não foi encontrada escrita em `SX` ou `SXX`. Esta conclusão é exclusiva desta exportação.

**Fonte B — exportação independente:** os três triggers SL contêm escrita em `SX.stock`. `tr_sl_insert` cria combinações em falta e aplica o movimento; `tr_sl_update` desfaz o efeito de `deleted`, garante a combinação para `inserted` e aplica o novo efeito; `tr_sl_delete` desfaz o efeito do movimento eliminado.

Em ambas as fontes, a ausência de escrita em `SXX` nos triggers analisados não demonstra que nenhum outro objecto ou processo mantenha SXX.

## 4. Operações sobre SX que foram encontradas

Na Fonte B foram identificadas operações de manutenção das dimensões de catálogo:

- `tr_sgc_update` / `tr_sgc_delete`: actualizam ou eliminam a cor nas linhas SX.
- `tr_sgt_update` / `tr_sgt_delete`: actualizam ou eliminam o tamanho nas linhas SX.

Estas operações mantêm a coerência das dimensões de catálogo, mas não constituem por si só evidência de cálculo de `SX.stock`.

Na mesma Fonte B, para além da manutenção de catálogo, `tr_sl_insert` e `tr_sl_update` criam combinações SX em falta quando referência, armazém, cor e tamanho estão preenchidos. O `INSERT INTO SX` não indica explicitamente a coluna `stock`; o valor inicial depende, portanto, do schema/default da instalação, que ainda não foi consultado. Não se deve afirmar que começa necessariamente em zero.

## 5. SXX — estado do conhecimento

O inventário de relações identifica `SXX` como «Stock por Armazém, Cor e Tamanho Global» e apresenta uma ligação declarada a `ST` por `SXX.ref = ST.ref`.

O object index consultado identifica o objecto e o campo `SXXSTAMP`, mas não disponibiliza o schema completo, índices, chaves ou definição SQL.

Não foram encontradas no material SQL analisado operações de escrita sobre SXX nem uma sincronização explícita SX→SXX.

**Não concluir ainda** que SXX é uma soma, cópia, totalização ou tabela derivada de SX. O nome e a relação declarada não bastam para determinar a sua função operacional.

## 6. Inventário físico e grelhas

A view `V_WSML_LININVENTARIOFISICO` lê linhas STIL e expõe, entre outros, referência, quantidade contada, armazém, lote, cor e tamanho.

A documentação/registos de assistência consultados descrevem:
- criação de movimentos de entrada/saída no lançamento do inventário para ajustar o stock à contagem;
- necessidade de preencher cor e tamanho nas linhas quando o artigo usa grelhas;
- correcções a rotinas personalizadas de importação/criação de inventários para transportar essas dimensões.

Estes elementos demonstram que cor e tamanho fazem parte dos dados necessários ao processo de inventário de artigos com grelhas. Não demonstram que a rotina de lançamento escreva directamente em SX.

A rotina interna que transforma STIC/STIL em movimentos SL continua por localizar. Também não foi demonstrado o mecanismo pelo qual esses movimentos são reflectidos em SX.

## 7. Procedure usp_stinvclc

Foi localizada e analisada a definição disponível de `usp_stinvclc`.

O corpo observado trabalha com referências, armazéns e lotes, calcula quantidades acumuladas e participa no recálculo de preços/custos ponderados. Usa estruturas temporárias, agrupamentos e SQL dinâmico.

No corpo analisado:
- não foram encontradas referências a `SX` ou `SXX`;
- não foram encontradas referências a `STIC` ou `STIL`;
- não foi encontrada escrita de saldos por cor/tamanho.

Assim, com a evidência disponível, `usp_stinvclc` parece pertencer ao circuito auxiliar de cálculo/valorização, não sendo a rotina demonstrada de manutenção de SX/SXX. O nome da procedure, isoladamente, não prova que seja o procedimento principal de lançamento do inventário.

Também não foi localizado o ponto da aplicação que a invoca nem o fluxo completo de chamadas.

## 8. Campo ST.textteis

Foi levantada a pista de que `ST.textteis` identifica artigos que utilizam cores e tamanhos.

A análise disponível ainda não demonstrou:
- se o campo é consultado pelos triggers ou procedures de stock;
- se controla a criação das linhas SX;
- se determina o tratamento de movimentos SL;
- se é apenas um indicador de classificação/ficha utilizado pela aplicação.

No `tr_bi_insert` analisado, cor e tamanho são transportados para SL sem uma condição explícita baseada em `ST.textteis`. Isto não exclui que a aplicação PHC use o campo noutro ponto do processo.

**Estado:** pista relevante, mas influência operacional por confirmar.

## 9. Hipóteses ainda abertas

1. A aplicação PHC pode actualizar SX directamente ao gravar documentos ou ao executar operações de stock.
2. Pode existir outra procedure, função ou trigger fora do ficheiro disponibilizado.
3. Pode existir um processo separado de criação/reconstrução de combinações de grelha e respectivos saldos.
4. SXX pode ter um papel de agregação ou consulta independente, ainda não demonstrado.

Nenhuma destas hipóteses deve ser tratada como conclusão.

## 10. Lacunas concretas

Para fechar o mecanismo, faltam:

1. Schema completo de SX e SXX: colunas, tipos, PKs, índices e FKs.
2. Definições de todos os triggers, procedures e funções que referenciem SX/SXX.
3. Identificação de tarefas SQL Server Agent ou rotinas externas que escrevam nestas tabelas.
4. Código/fluxo aplicacional PHC que trate grelhas ao gravar movimentos e ao lançar inventários.
5. Ponto de chamada e cadeia de execução de `usp_stinvclc`.
6. Evidência que esclareça o papel de SXX e a eventual relação com SX.
7. Confirmação do uso de `ST.textteis` fora do trigger BI analisado.

## 11. Próxima sequência de investigação

Enquanto não houver acesso a uma instalação, continuar apenas com fontes estáticas:

1. Procurar noutros exports SQL todas as definições que mencionem SX/SXX, incluindo procedures, funções, views e triggers.
2. Procurar definições completas de SX/SXX no Schema Snapshot, caso exista uma captura dessa categoria.
3. Cruzar as rotinas de importação de artigos/grelhas com a criação de combinações SX.
4. Procurar documentação e registos de assistência que descrevam explicitamente recálculo/reconstrução de stock por grelha.
5. Identificar a cadeia de chamada de `usp_stinvclc` sem assumir que é o lançador do inventário.
6. Só quando houver acesso a ambiente controlado, usar uma captura SQL para identificar as escritas reais durante uma operação com artigo de grelha.

Não preparar nem executar testes na instalação de produção. Não executar procedures manualmente nem alterar dados para validar hipóteses.

## 12. Conclusão actual

Está confirmado que `SX` fornece o saldo por artigo/armazém/cor/tamanho na view de grelhas. Os movimentos SL transportam cor e tamanho.

A manutenção de `SX.stock` diverge entre as fontes:
- na Fonte A (`CSDefault202603`), os triggers SL exportados não escrevem em SX;
- na Fonte B, `tr_sl_insert`, `tr_sl_update` e `tr_sl_delete` mantêm `SX.stock` por combinação de artigo/armazém/cor/tamanho.

Por isso, a conclusão correcta não é que o PHC nunca mantém SX através de SL, mas sim que **o mecanismo varia entre as exportações analisadas**. Não está explicado se a diferença decorre de versão, configuração, módulos ou personalização.

A função operacional de `SXX` permanece por demonstrar. `usp_stinvclc` não foi demonstrada como responsável pela manutenção de SX/SXX.

Esta investigação permanece aberta; não foram alterados dados PHC nem executados testes em runtime.

## 13. Comparação dos triggers nas duas fontes — 27-11-2026

### 13.1. Movimentos SL: ST, SA e SX

Na Fonte B, os triggers `tr_sl_insert`, `tr_sl_update` e `tr_sl_delete` mantêm três níveis:

| Nível | Chave funcional observada | Comportamento na Fonte B |
|---|---|---|
| `ST` | Artigo | Stock global e acumulados de entrada/saída; condicionado por `trfa=0`. |
| `SA` | Artigo + armazém | Stock por armazém; não exclui `trfa=1`. |
| `SX` | Artigo + armazém + cor + tamanho | Stock por combinação de grelha; não exclui `trfa=1`. |

A classificação observada mantém `CM < 50` como entrada e `CM >= 50` como saída. O movimento é aplicado com sinal positivo nas entradas e negativo nas saídas; nos triggers de update/delete, o efeito anterior é revertido.

O padrão é compatível com uma transferência que redistribui stock entre armazéns: altera SA e SX nos armazéns envolvidos, mas não altera o total global ST. É uma interpretação coerente com o código da Fonte B, não uma regra universal provada para todas as versões PHC.

Os triggers também usam uma data-limite derivada de STIC (`@@STICLASTDATE`); os movimentos anteriores ao corte são excluídos dos ramos de manutenção analisados.

### 13.2. Criação das combinações SX

Na Fonte B:
- `tr_sl_insert` cria uma linha SX em falta quando `ref`, `armazem`, `cor` e `tam` estão preenchidos.
- `tr_sl_update` aplica o mesmo mecanismo às combinações presentes em `inserted`, depois de reverter o estado anterior.
- `tr_sl_delete` não cria combinações; apenas reverte o saldo na linha SX correspondente.
- O INSERT em SX não lista a coluna `stock`. Sem schema e defaults não é possível determinar o valor inicial da coluna.

A documentação oficial PHC «Stocks e Serviços — Opções com Grelhas» confirma que a criação de cores e tamanhos para um artigo gera uma linha SX por combinação para o armazém 1, e que a rotina «Criar dados de cores e tamanhos» permite gerar combinações para vários armazéns. Isto demonstra uma via funcional de criação de linhas SX independente dos triggers SL.
Fonte: [manual PHC — Opções com Grelhas](https://phc.pt/portal/sug/ptxview.aspx?ptxid=20345476).

### 13.3. BI/BO2: quantidades comprometidas por grelha

Na Fonte B, os triggers BI/BO2 mantêm campos de compromisso em SX, distintos de `SX.stock`:

| Campo SX | Condição de negócio observada |
|---|---|
| `rescli` | `BI.rescli=1`: reserva de cliente. |
| `resfor` | `BI.resfor=1`: reserva de fornecedor. |
| `rescat` | `BI.cativo=1`: quantidade cativada. |
| `qttrec` | `BI.resrec=1`: quantidade em recepção. |

- `tr_bi_insert` acrescenta quantidades pendentes com base em `qtt - qtt2`, desde que o dossier não esteja anulado e a linha não esteja fechada/confirmada nos ramos aplicáveis.
- `tr_bi_update` reverte e reaplica os compromissos quando a linha muda de estado ou de valores; trata, entre outros, a passagem entre não confirmada e confirmada.
- `tr_bi_delete` retira os compromissos da linha eliminada.
- `tr_bo2_update`, quando o dossier passa de não anulado para anulado, retira os compromissos das linhas BI associadas.
- A manutenção usa a combinação de referência, armazém, cor e tamanho. Existem verificações com condição OR e inserts que exigem ambos os campos, pelo que o tratamento de linhas com apenas uma dimensão preenchida não deve ser generalizado.

Estes campos representam compromissos/reservas e não devem ser confundidos com stock físico em `SX.stock`.

### 13.4. Diferenças e riscos estáticos a validar

1. **Criação SX em statements multi-linha (SL):** em `tr_sl_insert` e `tr_sl_update`, a condição exterior usa `IF NOT EXISTS` sobre o conjunto `inserted`. Se existir SX para pelo menos uma das combinações do statement, o INSERT de combinações em falta pode não executar para as restantes. É um risco estático de lógica multi-linha, não um bug reproduzido.
2. **Actualização de cativação em BI:** os ramos de `rescat` em `tr_bi_insert`/`tr_bi_update` usam actualizações directas a partir de `inserted`, ao contrário dos ramos de `rescli`, `resfor` e `qttrec`, que agregam quantidades por combinação. Várias linhas da mesma combinação no mesmo statement justificam validação específica; não está demonstrado que exista erro em execução.
3. **Dimensões parciais:** alguns testes de existência usam `cor<>'' OR tam<>''`, enquanto a criação de SX e alguns updates exigem ambos os campos preenchidos. O comportamento para grelhas incompletas carece de confirmação funcional.
4. **Divergência entre fontes:** a Fonte A não contém escrita SX/SXX nos triggers SL analisados; a Fonte B contém manutenção de SX. Não inferir versão, módulo ou personalização como causa sem metadados comparáveis.
5. **SXX:** não aparece nos triggers SL/BI/BO2 aqui comparados. O seu papel continua sem prova operacional.

### 13.5. Matriz de conclusão

| Pergunta | Resultado | Estado |
|---|---|---|
| SL actualiza SX? | Sim na Fonte B; não nos triggers SL da Fonte A. | Divergência confirmada entre fontes |
| SL actualiza SX em transferências (`trfa=1`)? | Na Fonte B, SA/SX são actualizadas sem filtro `trfa=0`; ST é filtrada. | Confirmado no código; sem validação runtime |
| SL cria SX em falta? | Sim na Fonte B, para cor e tamanho preenchidos. | Confirmado no código |
| BI mantém compromissos em SX? | Sim na Fonte B: rescli, resfor, rescat e qttrec. | Confirmado no código |
| BO2 anulado reverte compromissos? | Sim na Fonte B, através das linhas BI. | Confirmado no código |
| SXX é mantida por estes triggers? | Não foi encontrada escrita em SXX. | Não demonstrado |
| A diferença entre exportações tem causa conhecida? | Não. | Por investigar |

## 14. Próximos passos

1. Obter schema, defaults, PKs e índices de SX/SXX na instalação de referência e, se possível, na origem da Fonte B.
2. Procurar todas as rotinas SQL e processos aplicacionais que escrevam em SX/SXX, sem limitar a pesquisa aos triggers.
3. Confirmar no SQL a configuração dos triggers efectivamente instalados e a versão/build PHC de cada origem, sem expor identificadores de clientes no repositório público.
4. Quando existir uma cópia descartável, testar statements multi-linha com combinações SX já existentes e em falta; testar também várias linhas BI para a mesma combinação, em especial `rescat`.
5. Localizar a rotina que transforma STIC/STIL em movimentos e esclarecer se a criação de linhas SX é feita antes, durante ou depois do lançamento.
6. Manter SXX como questão independente até obter schema e evidência de leitura/escrita.
