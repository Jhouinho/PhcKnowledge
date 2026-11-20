# Relatório de Fase 0 — contrato de evidências de triggers standard

| Campo | Valor |
|---|---|
| Data | 2026-11-20 |
| Estado | Auditoria concluída; decisões técnicas propostas aguardam aprovação |
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

## Decisões ainda necessárias

1. Aprovar a evolução da capability existente para versão 2.0.
2. Aprovar a chave de matching proposta.
3. Aprovar a política de comparação SQL, depois de definido e testado o algoritmo concreto.

## Reflexão e próximo passo

A auditoria permitiu fechar a compatibilidade estrutural do IKP sem alterar o contrato global 0.1. A principal cautela é preservar a diferença entre inventário antigo limitado a `u_*` e inventário alargado, para que capturas antigas nunca sejam interpretadas como prova de ausência de triggers standard.

O próximo passo é obter aprovação das três decisões acima. Só depois poderá começar a Fase 1 (Scanner). Esta fase não alterou código, schemas, SQLite, migrations ou testes.
