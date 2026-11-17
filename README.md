# PHC Knowledge

Plataforma de conhecimento técnico sobre PHC, destinada a recolher, organizar, relacionar, pesquisar e reutilizar conhecimento com proveniência, evidências e contexto.

O repositório reúne a plataforma PHC Knowledge e o componente de recolha **PhcKnowledgeScanner**. O Scanner mantém autonomia funcional e executável, mas o seu código é versionado neste mesmo repositório.

## Estrutura

```text
PhcKnowledge/
├── PhcKnowledge.sln
├── global.json
├── src/                       # plataforma PHC Knowledge (em desenvolvimento)
├── tests/                     # testes da plataforma
├── tools/                     # ferramentas e validação do contrato IKP
├── PhcKnowledgeScanner/       # aplicação de recolha PHC autónoma
│   ├── src/
│   ├── tests/
│   ├── tools/
│   └── docs/
└── docs/                      # arquitetura e contrato de importação
```

## Limites entre componentes

- **PhcKnowledgeScanner:** liga-se às instalações PHC em modo de leitura, recolhe metadados e personalizações, mantém o workspace local e produz artefactos/pacotes.
- **PHC Knowledge:** recebe, valida e importa pacotes IKP; mantém o conhecimento central, histórico, identidade e proveniência.
- A plataforma não executa o Scanner em runtime nem acede ao workspace SQLite do Scanner.
- Ambos evoluem no mesmo repositório, mas mantêm projetos, dependências, testes e responsabilidades separados.

## Desenvolvimento

Requisito: .NET SDK 10.0.403 ou posterior compatível com `global.json`.

A partir da raiz do repositório:

```bash
dotnet restore PhcKnowledge.sln
dotnet build PhcKnowledge.sln --no-restore
dotnet test PhcKnowledge.sln --no-build
```

Executar a aplicação desktop (Windows):

```bash
dotnet run --project PhcKnowledgeScanner/src/PhcKnowledgeScanner.Desktop
```

Executar um teste de ligação pela CLI (definir a password em `PHC_SCANNER_SQL_PASSWORD`, sem a colocar nos argumentos):

```bash
dotnet run --project PhcKnowledgeScanner/src/PhcKnowledgeScanner.Cli -- test-connection --server HOST\INSTANCE --database BD --auth sql --user UTILIZADOR
```

Também se mantém a solução específica do Scanner em `PhcKnowledgeScanner/PhcKnowledgeScanner.sln` para desenvolvimento isolado do componente.

## Estado da plataforma — 2026-11-17

A solução já contém uma primeira fatia funcional do núcleo PHC Knowledge, em src/PhcKnowledge.Import: validação de pacotes IKP, serviço de ingestão, arquivo imutável por SHA-256 e persistência de receipts no SQL Server. Existem testes unitários e de integração sobre fixture IKP / ZIP sintético → Validator / Ingestion (aceitação do pacote válido, arquivo imutável idempotente, registo de receipt por organização, rejeição de path traversal e payload adulterado).

**Implementado no código:** IkpPackageValidator, IkpPackageIngestionService, FileSystemImmutablePackageStore, SqlServerPackageReceiptRepository e migration 001_package_ingestion_receipts.sql. Os testes usam fixture IKP / ZIP sintético → Validator / Ingestion; não demonstram API HTTP/autenticação nem integração SQL Server real.

**Limite importante:** o serviço arquiva o ZIP antes de persistir o receipt. Uma falha SQL posterior pode deixar um ficheiro arquivado sem receipt; reconciliação/retry é evolução necessária. O arquivo é global e content-addressed; os receipts são isolados por organização.

O modelo de destino 0.2 está em [Modelo de domínio 0.2](docs/architecture/knowledge-domain-model-0.2.md). Distingue:
- **Implementado:** receção por serviço, validação, arquivo imutável e receipt SQL.
- **Parcial — apenas no ZIP:** capabilities, coverage, issues, artefactos e REL/DTL ainda não são materializados em tabelas próprias.
- **Futuro:** API HTTP/autenticação, associação a instalação/ambiente, materialização de Observations, resolução de identidade, comparação de capturas e publicação de conhecimento.

A captura real CSDefault202603 confirmou 16.618 SourceObjectIds, máximo de 54 caracteres, sem duplicados ordinais nem espaços nas extremidades. O modelo de destino propõe NVARCHAR(256), comparação ordinal e validação explícita. Não existe ainda migration para Observation.

A próxima etapa é fechar a reconciliação entre arquivo e receipt, testar a persistência contra SQL Server real e desenhar a fronteira HTTP/autenticação. Não criar migrations globais adicionais antes de fechar esses pontos.
