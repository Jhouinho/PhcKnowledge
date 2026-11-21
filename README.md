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

## Estado da plataforma — 2026-11-21

A referência de código é a `main`, commit `b8a188175eb6792210e2a242bc237c6747b19f68`. A Trigger Capability 2.0 foi integrada na PR #21 e os workflows associados terminaram com sucesso.

### Decisões actuais

As decisões aprovadas de produto, identidade, captura, transporte e armazenamento estão consolidadas em [Decisões arquitecturais aprovadas](docs/architecture/decisoes-aprovadas-2026-11-21.md). Em síntese:

- Uma única organização (a empresa utilizadora); clientes são contextos técnicos.
- Cada base de dados PHC registada tem uma instalação e um `InstallationId`; cada instalação tem várias capturas.
- Base central Microsoft SQL Server; edição/versão por decidir. PostgreSQL está excluído e não se assume Express.
- O Scanner é autónomo; o pacote é transferido manualmente.
- D-015: manter a recolha REL/DTL e comparar a evidência da instalação com o catálogo PHC Standard versionado.
- D-016: captura completa dentro do âmbito declarado; materialização e deduplicação incrementais no Knowledge.
- ZIP é apenas transporte temporário; depois da materialização verificada, é eliminado.
- Dados estruturados, histórico e artefactos originais necessários ficam permanentemente no SQL Server.

### Estado técnico conhecido

**Existente:**
- Scanner autónomo e exportador IKP 0.1.
- Recepção, validação, receipts e recuperação de pacotes.
- Análise IKP A+B integrada na `main`.
- Trigger Capability 2.0 integrada na PR #21.

**Por construir ou validar:**
- Matriz semântica completa dos collectors e ObjectKinds.
- Materialização persistente de observações, relações e artefactos no modelo de conhecimento.
- Comparação histórica com regras seguras de cobertura.
- Catálogo PHC Standard versionado e comparação efectiva.
- Ciclo final upload → análise → decisão → materialização → verificação → eliminação do ZIP.
- Pesquisa contextual sobre o conhecimento materializado.
- Edição/versão concreta do SQL Server.

O arquivo imutável actual de ZIPs é transitório e não representa o destino aprovado. Não o remover antes de existir materialização, verificação e recuperação testadas.

### Próximo passo

Construir a matriz de classificação por collector/ObjectKind, usando primeiro a captura real já disponível. Não alterar collectors, SQLite ou migrations antes de validar as lacunas concretas. O roadmap deve acompanhar a referência de decisões aprovada.
