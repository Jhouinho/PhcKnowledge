# Testes de integração SQL Server — PackageReceipt recovery

Os testes reais da implementação de recuperação usam uma base de dados **dedicada** chamada `PhcKnowledgeReceiptTests`. Nunca apontar os testes para bases de dados PHC, desenvolvimento partilhado ou produção.

## Execução automática no GitHub Actions

O workflow `.github/workflows/sql-server-integration.yml` inicia um contentor SQL Server 2022 isolado em cada execução da PR ou da branch principal.

O próprio projecto de testes:
1. Aguarda que o SQL Server esteja disponível.
2. Cria a base de dados `PhcKnowledgeReceiptTests`, se ainda não existir.
3. Aplica as migrations 001 e 002.
4. Executa os testes de integração reais.
5. Remove os registos criados pelos testes.

O contentor é descartado no final do job. Não são necessárias credenciais, secrets ou uma instância SQL Server na máquina do utilizador. A password usada no workflow é exclusiva do contentor efémero de CI e não deve ser reutilizada noutro ambiente.

## Execução local opcional

Para executar os mesmos testes numa instância SQL Server local, criar previamente uma base de dados vazia chamada `PhcKnowledgeReceiptTests` e definir a variável de ambiente `PHC_KNOWLEDGE_SQL_TEST_CONNECTION_STRING`.

Exemplo PowerShell:

```powershell
$env:PHC_KNOWLEDGE_SQL_TEST_CONNECTION_STRING = "Server=DESKTOP-GB9607Q\\SQLEXPRESS;Database=PhcKnowledgeReceiptTests;User ID=sa;Password=$env:PHC_SCANNER_SQL_PASSWORD;TrustServerCertificate=True"
dotnet test tests/PhcKnowledge.Import.SqlServer.Integration.Tests/PhcKnowledge.Import.SqlServer.Integration.Tests.csproj --configuration Release --logger "console;verbosity=normal"
```

A conta SQL precisa de permissões para criar a base de dados (caso ainda não exista), tabelas, índices e triggers. A password local é lida de `PHC_SCANNER_SQL_PASSWORD`; não a escrever em ficheiros, argumentos persistentes ou logs.

## Cobertura

- Duas reclamações concorrentes da mesma tentativa: apenas uma recebe lease.
- Expiração e recuperação de lease: token antigo deixa de poder renovar a lease.
- Conclusão bem-sucedida: receipt e tentativa concluída são observáveis em conjunto.
- Falha injectada entre o INSERT do receipt e o UPDATE da tentativa: a transacção deve reverter ambos.

Os registos criados pelos testes são removidos no fim. A base de dados e as tabelas permanecem durante a vida do ambiente de testes. No GitHub Actions, o contentor completo é efémero.

Os testes são opt-in: sem `PHC_KNOWLEDGE_SQL_TEST_CONNECTION_STRING`, ficam ignorados no workflow .NET geral. O workflow SQL Server dedicado define a variável e executa-os efectivamente contra SQL Server 2022.
