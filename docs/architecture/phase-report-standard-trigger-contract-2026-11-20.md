# Relatório de Fase 0 — contrato de evidências de triggers standard

| Campo | Valor |
|---|---|
| Data | 2026-11-20 |
| Estado | Fase 0 encerrada; três decisões aprovadas; Fase 1 autorizada |
| Referência PHC | CS Default 202603 |
| Documento consolidado | [Integração do catálogo PHC Standard com o IKP](standard-catalog-integration-specification.md) |

## Resultado

A Fase 0 consolidou a evidência necessária para definir a integração das triggers standard PHC:

- A fonte `docs/sources/triggers phc.sql` corresponde à CS Default 202603 e contém 552 triggers associadas a 258 tabelas.
- O ficheiro é uma fonte temporária de importação. Só deve ser removido do repositório depois de as definições e a proveniência estarem persistidas no SQL Server central, reconciliadas e consultáveis no Portal sem dependência do ficheiro.
- O collector IKP atual recolhe apenas triggers de tabelas `u_*`; o Database Schema Snapshot já dispõe de uma consulta estrutural mais abrangente, incluindo eventos e estado `INSTEAD OF`.
- O contrato IKP 0.1 suporta versões de capability por operação. A recomendação é manter `sql.server.structure.triggers`, preservar `1.0` para a semântica histórica e usar `2.0` para o inventário alargado.
- A chave de matching proposta é kind + schema/nome da trigger + schema/nome da tabela pai. Eventos e estados são metadados de comparação; `object_id` é apenas evidência local da captura.
- A indisponibilidade da definição não deve ser confundida com encriptação sem evidência. O hash original mantém-se exato; eventual fingerprint de comparação será separado e versionado.

## Decisões registadas e aprovadas

Em 2026-11-20, o utilizador aprovou expressamente:

1. **Capability:** manter `sql.server.structure.triggers`, preservar `1.0` para o âmbito histórico `u_*` e evoluir para `2.0` para triggers DML em tabelas sem filtro `u_*`, mantendo IKP 0.1.
2. **Identidade:** kind + schema/nome da trigger + schema/nome da tabela pai. Eventos e estado são metadados de comparação; `object_id` é auxiliar e local à captura.
3. **Comparação SQL:** manter SHA-256 exato dos bytes originais e criar fingerprint de comparação separado, versionado, baseado em normalização lexical T-SQL testada.

O algoritmo lexical concreto ainda tem de ser especificado, implementado e testado; a aprovação incide sobre a abordagem e os seus princípios de preservação.

## Reflexão e próximo passo

A auditoria permitiu fechar a compatibilidade estrutural do IKP sem alterar o contrato global 0.1. A principal cautela é preservar a diferença entre inventário antigo limitado a `u_*` e inventário alargado, para que capturas antigas nunca sejam interpretadas como prova de ausência de triggers standard.

A Fase 0 está encerrada e a Fase 1 (Scanner) está autorizada. O primeiro trabalho da Fase 1 é detalhar o contrato 2.0, os estados/campos exportados, a cobertura e os testes de compatibilidade; depois implementar e validar com uma captura real. A remoção de `docs/sources/triggers phc.sql` continua bloqueada até a importação integral estar persistida e reconciliada no SQL Server do Portal. A atualização documental não alterou código, schemas, SQLite, migrations ou testes.
