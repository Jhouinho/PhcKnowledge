# Testes de integração — triggers SQL Server do Scanner

O teste `SqlServerTriggerIntegrationTests` executa o `SqlServerStructureCollector` contra um SQL Server real. Não substitui a base de dados nem usa mocks.

## Base de dados

Usar uma base de dados SQL Server **dedicada a testes**, nunca uma base de dados PHC de produção. A ligação principal deve ter permissões para criar e remover tabelas e triggers em `dbo`.

Definir a variável:

- `PHC_KNOWLEDGE_SQL_TEST_CONNECTION_STRING`: connection string da ligação privilegiada.

O teste cria objetos com sufixo aleatório e remove-os no fim. Não guarda credenciais no repositório.

## Testes de visibilidade e completude

É necessária uma segunda ligação à mesma instância e base de dados:

- `PHC_KNOWLEDGE_SQL_TEST_RESTRICTED_CONNECTION_STRING`: login de teste usado para validar permissões limitadas.

O teste executa dois cenários com esse login:

1. Sem `VIEW DEFINITION` na base de dados, mas com `SELECT` no schema `dbo`. As triggers das tabelas visíveis podem ser recolhidas, mas o Scanner não declara o inventário global completo.
2. Com `VIEW DEFINITION` na base de dados, mas com `DENY VIEW DEFINITION` numa tabela específica e sem `SELECT` no schema. A trigger dessa tabela fica oculta, apesar da permissão global. O Scanner tem de manter a cobertura parcial.

A permissão `VIEW DEFINITION` ao nível da base de dados, isoladamente, não prova completude: um `DENY` ao nível de um objeto pode ocultar os metadados desse objeto. Nesta versão, a completude só é declarada como verificada quando a ligação é membro de `sysadmin`, que não está sujeita à filtragem normal de visibilidade de metadados.

Exemplo de preparação, executado por um administrador na base de dados dedicada (substituir o nome e definir uma palavra-passe própria fora do repositório):

```sql
CREATE LOGIN [phc_scanner_test_reader]
WITH PASSWORD = '<password-defined-outside-repository>';
GO

USE [PhcKnowledgeScannerTriggerTests];
GO

CREATE USER [phc_scanner_test_reader]
FOR LOGIN [phc_scanner_test_reader];
GO

GRANT SELECT ON SCHEMA::dbo TO [phc_scanner_test_reader];
GO
```

A preparação inicial concede apenas `SELECT` em `dbo`. O próprio teste altera temporariamente as permissões na base de dados dedicada para exercitar o cenário com `VIEW DEFINITION` global e `DENY` num objeto. Não usar estas instruções numa base de dados PHC real.

## GitHub Actions

O workflow `.github/workflows/sql-server-integration.yml` também executa este teste num SQL Server 2022 efémero. Para esse workflow, configurar o secret de repositório `SQLSERVER_SA_PASSWORD` com uma palavra-passe forte para o utilizador `sa`. O login restrito é criado durante o job com uma palavra-passe aleatória e não é persistido no repositório.

## Executar

Na raiz do repositório:

```powershell
dotnet test PhcKnowledgeScanner/tests/PhcKnowledgeScanner.Tests/PhcKnowledgeScanner.Tests.csproj --filter "FullyQualifiedName~SqlServerTriggerIntegrationTests"
```

Se uma das duas variáveis não estiver definida, o teste fica explicitamente **Skipped** pelo xUnit. Um teste ignorado não conta como validação aprovada.

## Cenários exercitados

- Trigger DML numa tabela standard, com INSERT, UPDATE e DELETE.
- Trigger numa tabela `u_*`.
- Estado disabled.
- Trigger INSTEAD OF numa tabela.
- Definição SQL disponível e persistida como artefacto.
- Login com `SELECT` em `dbo`, sem `VIEW DEFINITION` global: as triggers visíveis são recolhidas, mas a cobertura global permanece parcial.
- Login com `VIEW DEFINITION` global e `DENY VIEW DEFINITION` numa tabela: a trigger dessa tabela fica oculta e a cobertura permanece parcial.
- Ligação `sysadmin`: completude do inventário DML de tabelas declarada como verificada.

O teste não tenta simular permissões com mocks: a segunda ligação tem de usar efetivamente o login restrito no SQL Server.
