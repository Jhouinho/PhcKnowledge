# PHC Knowledge

Ferramenta interna da equipa PHC para organizar, relacionar, pesquisar e reutilizar conhecimento técnico com evidências e proveniência.

## Contexto canónico
Antes de trabalhar, consultar:
- [Baseline do produto e arquitetura](PROJECT_BASELINE.md)
- [Decisões aprovadas e questões em aberto](DECISIONS.md)
- [Estado real do projeto e evidências](PROJECT_STATUS.md)
- [Handoff atual](docs/governance/current-handoff.md)
- [Acordo de continuidade e índice](docs/governance/README.md)

O GitHub é a fonte partilhada de verdade para o estado publicado entre ChatGPT e Cursor. Não assumir que alterações locais estão publicadas.

## Componentes
- src/: plataforma PHC Knowledge, backend ASP.NET Core e frontend Portal.
- tests/: testes da plataforma.
- tools/: ferramentas de validação IKP.
- PhcKnowledgeScanner/: Scanner autónomo WPF + CLI, com solução, testes e workspace próprios.
- docs/: contratos, arquitetura, investigações, fontes e governação.

## Fronteiras
- Scanner: executa no ambiente do cliente, consulta SQL Server PHC em modo read-only, mantém workspace SQLite local e emite pacotes IKP.
- Knowledge: recebe e analisa pacotes, suporta decisões humanas e materializa conhecimento na base SQL Server central.
- A plataforma não executa o Scanner em runtime nem acede ao workspace SQLite do Scanner.
- O ZIP IKP é transporte temporário; o objetivo é eliminá-lo e aos temporários após materialização e confirmação íntegra.
- PHC Standard curado mantém-se separado das observações/personalizações de instalações.

## Stack aprovada
.NET 10 / ASP.NET Core, React + TypeScript, SQL Server central e cliente Windows via WebView2. A edição/versão concreta do SQL Server está por decidir. SQLite é apenas workspace/cache do Scanner. PostgreSQL não é utilizado.

## Desenvolvimento
Requisito: .NET SDK 10.0.403 ou posterior compatível com global.json.

A partir da raiz:
```bash
dotnet restore PhcKnowledge.sln
dotnet build PhcKnowledge.sln --no-restore
dotnet test PhcKnowledge.sln --no-build
```

Scanner Desktop:
```bash
dotnet run --project PhcKnowledgeScanner/src/PhcKnowledgeScanner.Desktop
```

Scanner CLI (definir password em PHC_SCANNER_SQL_PASSWORD, sem a colocar nos argumentos):
```bash
dotnet run --project PhcKnowledgeScanner/src/PhcKnowledgeScanner.Cli -- test-connection --server HOST\\INSTANCE --database BD --auth sql --user UTILIZADOR
```

Também existe a solução isolada PhcKnowledgeScanner/PhcKnowledgeScanner.sln. Consultar PROJECT_STATUS.md para o estado atual; relatórios de fase datados são fotografias históricas.
