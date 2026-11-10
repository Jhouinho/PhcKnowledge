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

## Estado da plataforma

A arquitetura, decisões de produto e contrato de importação estão documentados em:

- [Orientações do projeto](PROJECT_GUIDELINES.md)
- [Arquitetura](docs/architecture/)
- [Especificação IKP 0.1](docs/architecture/ikp-0.1-specification.md)
- [Schemas e validação IKP 0.1](docs/architecture/schemas/ikp-0.1/README.md)
- [Validador e testes de contrato](tools/ikp-validation/README.md)

**Estado em 2026-11-10:** o Scanner é executável e já exporta pacotes IKP 0.1 através da CLI. A projeção do Object Index foi validada com dados de uma captura PHC real; falta confirmar um ciclo end-to-end do ZIP produzido pelo exportador com essa captura. A plataforma central ainda não tem projetos próprios em `src/` nem testes em `tests/`. A próxima entrega é Receive / Validate / Archive: receber o ZIP, validar estrutura, segurança e integridade, arquivar o original de forma imutável e registar a importação no SQL Server Express. Não inclui ainda associação automática à instalação, materialização de conhecimento ou publicação.
