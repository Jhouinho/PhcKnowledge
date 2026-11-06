# Database Schema Snapshot

Snapshot estrutural de metadados SQL Server (sem registos de negócio).

| Campo | Valor |
| --- | --- |
| SchemaVersion | 1.0.0 |
| ScannerVersion | test |
| CollectedAtUtc | 2026-11-04T12:00:00.0000000+00:00 |
| OriginFingerprint | a1b2c3d4e5f60718 |
| SnapshotHash | 80a1fd99b288981938c31e2fb3edd8d52bd81e8becd78eb561031319d3926ae3 |
| CompletionStatus | Succeeded |
| IncludesSqlDefinitions | False |
| SafeForPublicShare | True |
| Tables | 2 |
| Columns | 6 |
| ForeignKeys | 1 |
| Indexes | 2 |

## Classificação

Conservative: ms_shipped → sql_system; name prefix u_ → candidate_custom; remaining → unknown. Never asserts PHC standard.

- sql_system: 0
- candidate_custom: 2
- unknown: 4

## Privacidade

Validação de privacidade: OK (modo seguro).

## Limitações

- Fixture de teste — sem dados de cliente.
- Extração limitada a metadados de catálogo (sys.*). Sem registos de negócio.
