# Testes de integração — triggers SQL Server do Scanner

O teste `SqlServerTriggerIntegrationTests` executa o `SqlServerStructureCollector` contra um SQL Server real. Não substitui a base de dados nem usa mocks.

## Base de dados

Usar uma base de dados SQL Server **dedicada a testes**, nunca uma base de dados PHC de produção. A ligação principal deve ter permissões para criar e remover tabelas e triggers em `dbo`.

Definir a variável:

- `PHC_KNOWLEDGE_SQL_TEST_CONNECTION_STRING`: connection string da ligação privilegiada.

O teste cria objetos com sufixo aleatório e remove-os no fim. Não guarda credenciais no repositório.

## Teste de visibilidade limitada

Para validar a diferença entre inventário visível e inventário comprovadamente completo, é necessária uma segunda ligação à mesma instância e base de dados:

- `PHC_KNOWLEDGE_SQL_TEST_RESTRICTED_CONNECTION_STRING`: login de teste sem `VIEW DEFINITION` ao nível da base de dados e com `SELECT` no schema `dbo`.

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

Não conceder `db_owner`, `db_datareader` ou `VIEW DEFINITION` ao login restrito. A permissão `SELECT` no schema permite testar a visibilidade dos objetos de tabela sem conceder visibilidade global das definições.

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
- Login restrito: cobertura parcial, aviso de visibilidade não comprovada e definição indisponível quando o SQL Server a oculta.

O teste não tenta simular permissões com mocks: a segunda ligação tem de usar efetivamente o login restrito no SQL Server.
