# PhcKnowledgeScanner

Ferramenta **independente** de recolha técnica de instalações PHC (CS Desktop / Web / XL / Manufactor).

> **Fase atual:** Desktop WPF + collector estrutural SQL + personalizações PHC + **relações declaradas** (`dbo.rel` / `dbo.dtl`, Camada A) + export ZIP só com pastas `brule`/`ebrule`/`udefs`/`eudefs`/`eventos`/`eeventos`/`teclas`/`campos`/`formc`/`formb`/`ftu` + **Database Schema Snapshot** (metadados estruturais versionáveis, modo seguro sem SQL completo).  
> Contrato alinhado com `PhcCustomizationCodeExport`.  
> **Pendente:** parser de expressões `tliga`/`expliga`, `lt`/`ltli`, mapa de dependências visual, abertura de workspaces offline.

## Objetivo

Recolher informação técnica de instalações distintas (versões, configurações e personalizações), **preservando conteúdos originais**, com inventário do que foi / não foi possível recolher e porquê. Exportação em **ZIP estruturado**.

Este projeto **não** depende do `ConsoleApp` nem do Scanner V1.

## Stack

| Item | Escolha |
| --- | --- |
| Runtime | **.NET 10 (LTS)** |
| UI desktop | **WPF** (`PhcKnowledgeScanner.Desktop`) — nativa Windows, sem WebView/HTTP |
| Host headless | `PhcKnowledgeScanner.Cli` |
| Persistência | SQLite (metadados) + ficheiros (artefactos) + SHA-256 |
| SQL | `Microsoft.Data.SqlClient` com `ApplicationIntent=ReadOnly` |

## Projetos

```text
src/
  PhcKnowledgeScanner.Abstractions/
  PhcKnowledgeScanner.Application/
  PhcKnowledgeScanner.Engine/
  PhcKnowledgeScanner.Collectors/     # sql.server.structure + phc.customizations + phc.declared-relations
  PhcKnowledgeScanner.Workspace/
  PhcKnowledgeScanner.Analysis/
  PhcKnowledgeScanner.Export/
  PhcKnowledgeScanner.Desktop/        # WPF
  PhcKnowledgeScanner.Cli/
tests/
  PhcKnowledgeScanner.Engine.Tests/
  PhcKnowledgeScanner.Tests/
```

## Comandos

```bash
dotnet build PhcKnowledgeScanner.sln
dotnet test PhcKnowledgeScanner.sln

# Desktop
dotnet run --project src/PhcKnowledgeScanner.Desktop

# CLI — teste de ligação (password via env, nunca em logs)
set PHC_SCANNER_SQL_PASSWORD=***
dotnet run --project src/PhcKnowledgeScanner.Cli -- test-connection --server HOST\INSTANCE --database BD --auth sql --user sa
dotnet run --project src/PhcKnowledgeScanner.Cli -- scan --server HOST\INSTANCE --database BD --auth sql --user sa

# Schema snapshot (metadados estruturais; sem registos; sem SQL completo por omissão)
dotnet run --project src/PhcKnowledgeScanner.Cli -- schema-snapshot --server HOST\INSTANCE --database BD --output .\database-schema
dotnet run --project src/PhcKnowledgeScanner.Cli -- compare-schema-snapshot --left .\schema-a --right .\schema-b
```

## Princípios

- SQL **apenas leitura**
- Factos ≠ relações derivadas ≠ inferências
- Sem truncagem silenciosa; issues registadas por objeto/categoria
- Sem passwords em disco, logs, manifesto ou ZIP
- Schema snapshot partilhável: só metadados; sem código SQL de módulos por omissão

## Documentação

- [docs/architecture.md](docs/architecture.md)
- [docs/database-schema-snapshot.md](docs/database-schema-snapshot.md)
- [docs/architecture-proposal.md](docs/architecture-proposal.md) (histórico; UI escolhida: WPF, não WebView2)
- [docs/decisions/](docs/decisions/)
