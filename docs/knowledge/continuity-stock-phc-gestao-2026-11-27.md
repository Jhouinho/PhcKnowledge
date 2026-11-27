# Ponto de continuidade — investigação de stocks PHC Gestão

**Data:** 27-11-2026  
**Repositório:** `Jhouinho/PhcKnowledge`  
**Branch de documentação de origem:** `docs/stock-production-handoff-main-2026-11-25`  
**Branch desta preparação:** `docs/pending-stock-sql-checks-2026-11-27`  
**Documento-base:** `docs/knowledge/phc-stock-production-investigation-handoff-2026-11-25.md`  
**Investigação complementar:** `docs/audits/investigacao-tecnica-stock-grelhas-sx-sxx-2026-11-26.md`

## Âmbito activo

A continuação desta investigação deve concentrar-se no **PHC Gestão** e no funcionamento dos movimentos e saldos de stock. O PHC Manufactor deixa de ser uma dependência ou prioridade da investigação. A informação Manufactor já recolhida mantém-se apenas como contexto histórico/complementar; não deve orientar as conclusões sobre o PHC Gestão.

## Conhecimento já consolidado

- `BI`, `FI` e `FN` são três origens identificadas de movimentos `SL`, sob condições/configurações distintas.
- `SL` representa movimentos; `ST` contém acumulados globais por artigo; `SA` contém acumulados por artigo/armazém.
- No circuito de transferência observado em `tr_bi_insert`, uma linha BI pode originar dois movimentos SL: principal em `BI.armazem` com `TS.cmstocks`, e complementar em `BI.ar2mazem` com `TS.cm2stocks`; o segundo usa `SL.slstamp = 'T' + BI.bistamp`.
- O tratamento de `SL.trfa` nos triggers de stock distingue os acumulados globais dos saldos por armazém. A interpretação funcional é que a transferência redistribui stock entre armazéns sem alterar o total global, mas os sinais/códigos CM2 ainda têm de ser confirmados.
- Reservas/cativações e quantidades especiais não devem ser confundidas com movimentos físicos.
- A investigação de `SX/SXX` concluiu que o mecanismo de actualização de `SX.stock` continua por localizar; não assumir que os triggers SL o actualizam.

## Pendências SQL — executar quando houver acesso à CSDefault202603

Foi criado um guião SQL de leitura, para não depender de reconstruir as consultas no momento:

[`docs/sql/pendentes-transferencias-stock-phc-gestao-2026-11-27.sql`](../sql/pendentes-transferencias-stock-phc-gestao-2026-11-27.sql)

O guião não altera dados nem objectos. Executa quatro blocos:

1. Inventaria as colunas reais de `TS`, `TS2` e `CM2`, sem presumir campos ainda não confirmados.
2. Devolve a configuração dos registos `TS` com `ndos = 5`.
3. Devolve `TS` e o registo associado de `TS2`, usando a ligação já observada `TS.tsstamp = TS2.ts2stamp`.
4. Devolve os registos de `CM2`, para localizar os códigos 98 e 48 e os respectivos parâmetros de valorização.

**Perguntas a fechar com os resultados:**

- Qual é o valor efectivo de `TS.trfa` no dossier 5?
- Qual é o valor efectivo de `TS.producao` no dossier 5?
- Qual é o valor efectivo de `TS2.trocaequi` associado?
- Que campos de `CM2` identificam os códigos 98 e 48?
- Quais são os valores de `mudapcpond` e `mudapcult` para ambos os códigos, caso esses campos existam?
- Com esses valores, que movimento usa cada código CM2 e como se reflecte em `SL`, `SA` e `ST`?

**Procedimento quando houver acesso:** executar o ficheiro completo na base `CSDefault202603`, guardar os quatro conjuntos de resultados (ou exportá-los para CSV) e partilhá-los aqui. A análise continuará a partir desses resultados; não será necessário repetir consultas já respondidas. Se o bloco 3 falhar por alguma coluna de ligação inexistente, usar o resultado do bloco 1 para ajustar a ligação antes de prosseguir.

## Próxima sequência de trabalho

1. Confirmar a versão exacta do ficheiro de triggers SQL no GitHub e reconfirmar a evidência citada no documento-base.
2. Fechar as pendências SQL acima, começando pelo esquema real de `CM2`, `TS` e `TS2`.
3. Fechar o sentido e os sinais de `TS.cmstocks` e `TS.cm2stocks`.
4. Seguir uma transferência concreta: `TS → BI → SL` (dois movimentos) e reconciliar os efeitos em `SA` e `ST`.
5. Analisar update, delete e anulação do dossier, confirmando reversões dos dois movimentos.
6. Só depois retomar lacunas de `SAL`, `SX/SXX` e outros circuitos de stock.

## Regras de rigor

- Distinguir evidência directa no SQL, documentação funcional, inferência e validação em runtime.
- Não inferir o sinal de um movimento pelo nome do campo ou código; confirmar a configuração CM2.
- Não inventar colunas: confirmar primeiro o esquema real.
- Não assumir que ausência de uma operação num ficheiro de triggers significa ausência de funcionalidade no PHC.
- Não executar alterações em produção; testes de alteração/anulação exigem ambiente controlado.
- Actualizar a documentação canónica e o grafo de relações quando uma conclusão for demonstrada.

## Estado do repositório e ressalva

Esta preparação está numa branch própria e ainda não foi integrada na `main`. Antes de declarar integração, verificar o estado efectivo da Pull Request e da branch.

## Como retomar num chat novo

Começar por ler este documento e o documento-base/auditoria referidos no cabeçalho. Não repetir a investigação desde o início. Se ainda não houver acesso ao SQL Server, continuar a análise estática dos triggers. Quando houver acesso, executar o guião SQL indicado em “Pendências SQL” e fechar apenas as lacunas com os resultados reais.
