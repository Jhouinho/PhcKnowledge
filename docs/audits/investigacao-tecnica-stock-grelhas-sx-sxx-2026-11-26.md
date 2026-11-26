# Investigação técnica PHC — stock por grelha (SX/SXX)

**Data:** 26-11-2026  
**Estado:** investigação documental/estática; mecanismo de actualização de SX.stock ainda não identificado.  
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

Foi revisto o ficheiro de triggers PHC disponibilizado para análise, incluindo `tr_sl_insert`, `tr_sl_update` e `tr_sl_delete`.

### 3.1 Origem dos movimentos

Os circuitos observados em `BI`, `FI` e `FN` podem criar ou gerir movimentos em `SL`. Nos movimentos de linhas BI/FI/FN analisados, cor e tamanho são transportados quando aplicáveis.

No caso das transferências BI, a criação dos dois movimentos SL conserva a dimensão da linha de origem, tal como conserva referência, quantidade e outros dados relevantes.

### 3.2 Actualização dos acumulados

Nos corpos dos triggers `tr_sl_insert`, `tr_sl_update` e `tr_sl_delete` analisados:
- existem operações de actualização/reversão sobre `ST` e `SA`;
- não foi encontrada escrita de quantidades em `SX`;
- não foi encontrada escrita em `SXX`.

A pesquisa textual no ficheiro também não encontrou `INSERT INTO SX`, `UPDATE SX`, `INSERT INTO SXX` ou `UPDATE SXX` nos triggers de movimentos.

**Conclusão limitada à fonte:** o ficheiro de triggers analisado não demonstra que os triggers SL mantenham os saldos de grelha. Isto não exclui actualização directa pela aplicação PHC, procedures não incluídas, outros triggers ou outros processos.

## 4. Operações sobre SX que foram encontradas

No ficheiro de triggers foram identificadas operações de manutenção das dimensões de catálogo:

- `tr_sgc_update`: actualiza o código de cor em linhas SX quando a cor correspondente é alterada em SGC.
- `tr_sgc_delete`: elimina de SX as linhas da cor removida.
- `tr_sgt_update`: actualiza o tamanho em linhas SX quando o tamanho correspondente é alterado em SGT.
- `tr_sgt_delete`: elimina de SX as linhas do tamanho removido.

Estas operações mantêm a coerência das dimensões de catálogo. Não constituem evidência de cálculo ou actualização de `SX.stock`.

Não foi encontrada, no SQL de triggers analisado, uma rotina que crie as combinações SX em resposta a movimentos SL.

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

Está confirmado que `SX` fornece o saldo por artigo/armazém/cor/tamanho nas views consultadas e que os movimentos SL transportam cor e tamanho. Também está confirmado que os triggers SL analisados actualizam ST/SA, mas não contêm escrita de saldos em SX/SXX.

O mecanismo que mantém `SX.stock` permanece **não identificado**. A função de `SXX` permanece **parcialmente identificada**. `usp_stinvclc` não foi demonstrada como responsável por qualquer uma dessas actualizações.

Esta investigação permanece aberta e não altera código nem contratos do PHC Knowledge.
