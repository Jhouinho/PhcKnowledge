# Ponto de continuidade — investigação de stocks PHC Gestão

**Data:** 27-11-2026  
**Repositório:** `Jhouinho/PhcKnowledge`  
**Branch de documentação:** `docs/stock-production-handoff-main-2026-11-25`  
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

## Próxima sequência de trabalho

1. Confirmar a versão exacta do ficheiro de triggers SQL no GitHub e reconfirmar a evidência citada no documento-base.
2. Inspeccionar o esquema e a configuração de `CM2`, `TS` e `TS2`, sem presumir nomes de colunas; usar `sys.columns` quando houver acesso à base de dados.
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

O documento-base e a auditoria SX/SXX foram localizados na branch indicada acima. Este apontamento é um guia de retoma, não significa que a documentação esteja integrada na `main`. Antes de declarar integração, verificar o estado efectivo da Pull Request e da branch.

## Como retomar num chat novo

Começar por ler este documento e os dois documentos referidos no cabeçalho. Não repetir a investigação desde o início. A primeira tarefa é auditar a fonte SQL actual e fechar as transferências no PHC Gestão, começando pela configuração CM2.
