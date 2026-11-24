# PHC Knowledge

Plataforma de conhecimento técnico sobre PHC, destinada a recolher, organizar, relacionar, pesquisar e reutilizar conhecimento com proveniência, evidências e contexto.

O repositório reúne a plataforma PHC Knowledge e o componente autónomo **PhcKnowledgeScanner**. O Scanner mantém solução, execução, workspace e testes próprios, mas o repositório canónico de desenvolvimento é este monorepo.

## Estrutura e fronteiras

- `PhcKnowledgeScanner/`: aplicação autónoma de recolha read-only, workspace SQLite local e exportação IKP.
- `src/`: plataforma central, incluindo importação, Host HTTP e Portal React.
- `tests/`: testes da plataforma; os testes do Scanner permanecem nos seus projectos próprios.
- `docs/`: decisões, contratos, roadmap, relatórios e fontes.

O Knowledge não executa o Scanner em runtime nem acede ao seu workspace SQLite. O Scanner produz o pacote IKP, transferido para o Portal. A base central é Microsoft SQL Server; PostgreSQL está excluído. A edição/versão concreta do SQL Server continua por decidir.

## Desenvolvimento

Requisito: .NET SDK 10.0.403 ou posterior compatível com `global.json`.

A partir da raiz:

```bash
dotnet restore PhcKnowledge.sln
dotnet build PhcKnowledge.sln --no-restore
dotnet test PhcKnowledge.sln --no-build
```

O Scanner mantém também a sua solução em `PhcKnowledgeScanner/PhcKnowledgeScanner.sln`.

## Baseline actual — 2026-11-24

**Branch:** `main`  
**Commit:** `dfb44b5ce4370ccf5b43856c87328f1915c58a99`  
**Integração mais recente:** PR [#28 — Incremento C](https://github.com/Jhouinho/PhcKnowledge/pull/28), merged em 2026-11-24.

### Direcção aprovada

- Uma organização interna; clientes PHC são contextos técnicos.
- Relação Cliente → Instalação → Capturas.
- Base central Microsoft SQL Server; edição/versão por decidir.
- Scanner autónomo, captura completa dentro do âmbito declarado e transporte IKP manual.
- D-015: manter factos REL/DTL e compará-los no Knowledge com o catálogo PHC Standard versionado.
- D-016: materialização e deduplicação incrementais; ausência só significa remoção quando a operação teve sucesso e a cobertura é completa.
- ZIP IKP exclusivamente temporário: só é eliminado depois de materialização integral e verificada.
- SQLite apenas como workspace/cache local do Scanner.

A referência normativa é [Decisões arquitecturais aprovadas](docs/architecture/decisoes-aprovadas-2026-11-21.md).

### Implementado na main

- Scanner autónomo, collectors existentes e exportador IKP 0.1.
- Receive / Validate / Archive, receipts, recovery e API HTTP IKP.
- Identity e backend de sessão do Portal.
- Análise IKP A+B persistida.
- Trigger Capability 2.0.
- D-022: Object Index v2, DefinitionFingerprint D-021 e PackageImportAnalysis 0.3.
- Incremento C: decisão explícita de confirmar/rejeitar PackageImportAnalysis, persistência append-only das decisões, endpoints do Host e UI React do Portal.

### Validação conhecida

Os quatro checks associados à cabeça da PR #28 terminaram com sucesso: `build-and-test`, `dotnet-tests`, `portal` e integração `SQL Server 2022 — receipt recovery and Scanner triggers`. Isto comprova os pipelines executados na PR; não equivale a deployment nem a validação funcional numa instalação PHC real.

A implementação D-022 ainda precisa de validação com uma captura nova numa instalação PHC. A captura anterior CSDefault202603 não deve ser apresentada como validação da implementação D-022.

A matriz estática de referência, ainda pendente de validação com captura real, está em [Matriz Collector / Operation / ObjectKind](docs/architecture/collector-operation-objectkind-matrix-2026-11-24.md).

### Por implementar ou validar

- Matriz validada collector/operation/ObjectKind/cobertura, incluindo limites e evidência por tipo de objeto.
- Associação completa de Cliente/Instalação/Captura no fluxo do Portal.
- Catálogo PHC Standard versionado e comparação efectiva com REL/DTL e restantes evidências.
- Materialização persistente de Capture, Observation, relações, metadados e artefactos no SQL Server.
- Verificação pós-materialização e eliminação segura do ZIP/staging.
- Escolha da edição/versão SQL Server e detalhe físico do armazenamento de artefactos.
- Hardening operacional antes de produção.
- Comparação histórica, pesquisa contextual e evolução RAG, dependentes de conhecimento estruturado e rastreável.

### PRs históricas

- PR [#28](https://github.com/Jhouinho/PhcKnowledge/pull/28): integrada; é a reconciliação válida do Incremento C sobre D-022.
- PR [#20](https://github.com/Jhouinho/PhcKnowledge/pull/20): preservada como histórico; não é a candidata de integração nem representa trabalho pendente de merge.

### Próxima sequência recomendada

1. Fechar a matriz collector/operation/ObjectKind com evidência de código e captura real.
2. Validar D-022 e a matriz com uma captura nova numa instalação PHC.
3. Fechar o contrato físico de materialização no SQL Server.
4. Implementar materialização incremental, verificação integral e eliminação segura do ZIP.
5. Só depois avançar para comparação histórica, pesquisa contextual e RAG.

Não alterar collectors, workspace SQLite ou semântica de recolha antes de a matriz estar validada e a alteração correspondente ser aprovada. Não declarar materialização central concluída sem execução e verificação efectivas.

O [roadmap visual](docs/architecture/roadmap-visual-phc-knowledge-2026-11-06.md) regista a sequência e os critérios. As [orientações do projecto](PROJECT_GUIDELINES.md) definem a governação e a documentação canónica.
