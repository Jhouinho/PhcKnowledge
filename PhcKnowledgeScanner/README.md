# PhcKnowledgeScanner

Componente autónomo de recolha técnica de instalações PHC, incluído no monorepo PHC Knowledge. Mantém execução e responsabilidades independentes e pode ser instalado/executado sem iniciar a plataforma central.

> Estado: Desktop WPF + CLI, collectors estruturais SQL e PHC, relações declaradas REL/DTL, workspace SQLite e Database Schema Snapshot.

## Responsabilidades
- Ligar à base de dados PHC em modo read-only.
- Recolher inventário técnico, definições de personalizações e relações dentro da cobertura implementada.
- Preservar conteúdos originais, metadados, proveniência e limitações.
- Manter workspace local SQLite e artefactos locais.
- Produzir pacotes IKP 0.1.

O Scanner não mantém o conhecimento central nem executa a importação na plataforma. O PHC Knowledge recebe e processa os pacotes. O Scanner não depende do backend, da base de dados central SQL Server nem do runtime da plataforma.

## Fronteira de armazenamento
A base central do PHC Knowledge é SQL Server. O SQLite deste componente é exclusivamente workspace/cache local; não é base central nem mecanismo de sincronização.

## Stack
| Item | Escolha |
|---|---|
| Runtime | .NET 10 |
| UI desktop | WPF nativa Windows |
| Host headless | CLI |
| Workspace | SQLite + ficheiros de artefactos |
| SQL | Microsoft.Data.SqlClient, apenas leitura |

## Princípios
- SQL apenas leitura.
- Factos originais, relações derivadas e inferências mantêm-se separados.
- Sem truncagem silenciosa; limitações e issues devem ser registadas.
- Passwords não são guardadas em ficheiros, logs, manifestos ou ZIPs.
- Schema Snapshot partilhável contém metadados; definições SQL completas são opcionais e sujeitas a validação de privacidade.

## Documentação
- Arquitetura: docs/architecture.md
- Schema Snapshot: docs/database-schema-snapshot.md
- Decisões: docs/decisions/
- Estratégia de testes: docs/testing-strategy.md
