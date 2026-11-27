# Complemento de investigação PHC — schema e manutenção de SX/SXX

**Data:** 27-11-2026  
**Base estrutural:** `CSDefault202603`  
**Estado:** schema recolhido; perfil dos dados de SX/SXX pendente.  
**Fontes de código:** exportação descriptografada `PhcKnowledgeScanner/triggers phc.sql`; não confundir com a exportação baseline em `docs/sources/triggers phc.sql`.

## 1. Schema observado na CSDefault202603

As tabelas `dbo.SX` e `dbo.SXX` existem e têm, respectivamente, 39 e 37 colunas. Ambas incluem `stock`, `rescli`, `resfor`, `rescat` e `qttrec`, com valores por omissão a zero. Os campos de referência e dimensões têm valores por omissão vazios/zero. Não foram observadas colunas identity ou computed.

Diferenças estruturais relevantes:
- `SX` tem `lordem` e `ststamp`; `SXX` não tem esses campos.
- `SX` tem chave primária composta por `ref, armazem, cor, tam`, além de índice único por `sxstamp`.
- `SXX` tem chave primária apenas em `sxxstamp`; o índice por `ref, armazem, cor, tam` não é único.
- Não foram encontradas foreign keys declaradas de ou para SX/SXX.
- Não existem triggers directamente associados a SX/SXX na base consultada.
- As dependências declaradas identificam quatro triggers em SGC/SGT que referenciam SX.
- As definições desses quatro triggers estão encriptadas na base: `is_encrypted=1`, `definition=NULL`. A análise do código foi feita a partir da exportação descriptografada do repositório, não por leitura da definição instalada.

## 2. Triggers SGC/SGT — exportação descriptografada

Foram revistos os corpos de:
- `tr_sgc_delete`: elimina em SX as linhas da mesma referência e cor eliminada.
- `tr_sgc_update`: quando a cor muda, actualiza em SX a dimensão antiga para a nova.
- `tr_sgt_delete`: elimina em SX as linhas da mesma referência e tamanho eliminado.
- `tr_sgt_update`: quando o tamanho muda, actualiza em SX a dimensão antiga para a nova.

Não foram encontrados `tr_sgc_insert` nem `tr_sgt_insert` nesta exportação. Os quatro triggers não recalculam `SX.stock` nem alteram directamente os campos de compromisso.

Riscos estáticos, não reproduzidos:
1. Os DELETE eliminam linhas SX sem verificar stock ou compromissos.
2. Os UPDATE associam `inserted` e `deleted` por referência e dimensão anterior, sem uma chave explícita entre cada linha antiga e nova; operações multi-linha para a mesma referência merecem validação.

## 3. Estado de SXX

A estrutura não sustenta a hipótese simples de que SXX seja uma cópia com a mesma chave funcional de SX: não tem a mesma chave natural garantida e não contém `lordem` nem `ststamp`. Não foram encontradas referências a SXX nos triggers de movimentos SL/BI/BO2 comparados.

Isto não prova ausência de manutenção por aplicação, procedures inacessíveis/encriptadas, SQL dinâmico, tarefas externas ou outros processos.

## 4. Próxima recolha

Foi acrescentado o guião read-only:
`docs/sql/perfil-funcional-sx-sxx-phc-gestao-2026-11-27.sql`

O guião devolve contagens, totais agregados, distribuição por armazém, duplicados pela combinação funcional e comparação agregada SX/SXX. Não devolve referências de artigos.

A recolha é necessária para determinar se SXX contém dados, se tem combinações duplicadas, se coincide com SX ou se representa um conjunto distinto. Até receber esses resultados, a função operacional de SXX permanece **não demonstrada**.

## 5. Limitações

- Schema e dependências são factos da `CSDefault202603`.
- Os corpos de triggers SGC/SGT são factos da exportação descriptografada; a equivalência exacta com os triggers instalados na CSDefault202603 não pode ser verificada porque estão encriptados.
- Riscos multi-linha são hipóteses estáticas, não bugs confirmados.
- Não foram alterados dados nem executados testes em runtime.
