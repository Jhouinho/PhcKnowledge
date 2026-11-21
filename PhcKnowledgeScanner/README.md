# PhcKnowledgeScanner


> **Contrato actual com o PHC Knowledge — 21-11-2026:** o Scanner mantém autonomia e não consulta o histórico central. Produz capturas completas dentro do âmbito declarado, com cobertura explícita, identidade e proveniência. Mantém a recolha REL/DTL. O ZIP é apenas transporte temporário; após materialização verificada, os dados e artefactos originais ficam no SQL Server central e o ZIP é eliminado. Ver [decisões aprovadas](../docs/architecture/decisoes-aprovadas-2026-11-21.md).

Componente autónomo de recolha técnica de instalações PHC, incluído no monorepo **PHC Knowledge**.

O Scanner continua a ser uma aplicação independente do ponto de vista funcional e de execução: pode ser instalado e executado sem iniciar a plataforma PHC Knowledge. A sua localização e versionamento, contudo, são neste repositório, em `PhcKnowledgeScanner/`.

> **Estado atual:** Desktop WPF + CLI, collector estrutural SQL, personalizações PHC, relações declaradas (`dbo.rel` / `dbo.dtl`) e Database Schema Snapshot (metadados estruturais versionáveis, com proteção de privacidade).

## Responsabilidades

- Ligar a uma base de dados PHC em modo read-only.
- Recolher inventário técnico, definições de personalizações e relações.
- Preservar conteúdos originais, metadados, proveniência e limitações da recolha.
- Manter workspace local SQLite e artefactos locais.
- Produzir exportações que evoluirão para o contrato IKP.

O Scanner **não** mantém o conhecimento central nem executa a importação na plataforma. O PHC Knowledge recebe e valida os pacotes produzidos pelo Scanner. O Scanner não depende do backend, da base de dados central SQL Server nem do runtime da plataforma.

## Stack

| Item | Escolha |
| --- | --- |
| Runtime | .NET 10 |
| UI desktop | WPF nativa Windows |
| Host headless | CLI |
| Workspace | SQLite + ficheiros de artefactos |
| SQL | Microsoft.Data.SqlClient, apenas leitura |

## Projetos

```text
src/
  PhcKnowledgeScanner.Abstractions/
  PhcKnowledgeScanner.Application/
  PhcKnowledgeScanner.Engine/
  PhcKnowledgeScanner.Collectors/
  PhcKnowledgeScanner.Workspace/
  PhcKnowledgeScanner.Analysis/
  PhcKnowledgeScanner.Export/
  PhcKnowledgeScanner.Desktop/
  PhcKnowledgeScanner.Cli/
tests/
  PhcKnowledgeScanner.Engine.Tests/
  PhcKnowledgeScanner.Tests/
tools/
  ValidateRelationalKnowledge/
```

## Comandos

A partir da raiz do monorepo, consultar o [README principal](../README.md). Para trabalhar isoladamente a partir desta pasta:

```bash
dotnet build PhcKnowledgeScanner.sln
dotnet test PhcKnowledgeScanner.sln
dotnet run --project src/PhcKnowledgeScanner.Desktop
```

## Princípios

- SQL apenas leitura.
- Factos originais, relações derivadas e inferências mantêm-se separados.
- Sem truncagem silenciosa; limitações e issues devem ser registadas.
- Passwords não são guardadas em ficheiros, logs, manifestos ou ZIPs.
- Schema Snapshot partilhável contém metadados; definições SQL completas são opcionais e sujeitas a validação de privacidade.

## Documentação

- [Arquitetura](docs/architecture.md)
- [Schema Snapshot](docs/database-schema-snapshot.md)
- [Decisões](docs/decisions/)
