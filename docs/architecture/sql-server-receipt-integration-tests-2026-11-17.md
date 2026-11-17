# Testes de integração SQL Server — PackageReceipt recovery

Os testes reais da implementação de recuperação usam uma base de dados **dedicada** chamada `PhcKnowledgeReceiptTests`. Não apontar os testes para bases de dados PHC, desenvolvimento partilhado ou produção.

## Preparação

1. Criar uma base de dados vazia chamada `PhcKnowledgeReceiptTests` na instância SQL Server de testes.
2. Definir a variável de ambiente `PHC_KNOWLEDGE_SQL_TEST_CONNECTION_STRING` com uma connection string para essa base de dados.
3. A conta SQL precisa de permissões para criar tabelas, índices e triggers nessa base de dados.
4. Executar:

```powershell
$env:PHC_KNOWLEDGE_SQL_TEST_CONNECTION_STRING = "Server=DESKTOP-GB9607Q\SQLEXPRESS;Database=PhcKnowledgeReceiptTests;User ID=sa;Password=$env:PHC_SCANNER_SQL_PASSWORD;TrustServerCertificate=True"
dotnet test tests/PhcKnowledge.Import.SqlServer.Integration.Tests/PhcKnowledge.Import.SqlServer.Integration.Tests.csproj --configuration Release --logger "console;verbosity=normal"
```

A password é lida de `PHC_SCANNER_SQL_PASSWORD`; não a escrever em ficheiros, argumentos persistentes ou logs.

## Cobertura

- Duas reclamações concorrentes da mesma tentativa: apenas uma recebe lease.
- Expiração e recuperação de lease: token antigo deixa de poder renovar a lease.
- Conclusão bem-sucedida: receipt e tentativa concluída são observáveis em conjunto.
- Falha injectada entre o INSERT do receipt e o UPDATE da tentativa: a transacção deve reverter ambos.

Os testes aplicam as migrations 001 e 002 de forma idempotente na base de dados dedicada. Os registos criados pelos testes são removidos no fim. A base de dados e as tabelas permanecem para execuções seguintes.

Os testes são opt-in: sem a variável de ambiente, ficam ignorados. O pipeline CI continua a compilar o projecto sem precisar de acesso à instância local.
