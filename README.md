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

A implementação D-022 ainda precisa de validação com recolhas reais. A captura anterior CSDefault202603 não deve ser apresentada como validação da implementação D-022.

O procedimento para essas recolhas está em [Protocolo de validação funcional do Scanner](docs/architecture/scanner-functional-validation-protocol-2026-11-24.md). A primeira base é a CS default; a segunda é uma base PHC com desenvolvimento, escolhida pelo operador. As duas usam a mesma versão do Scanner e artefactos separados. O protocolo está preparado e as recolhas ainda não foram executadas.

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

1. Fechar a matriz collector/operation/ObjectKind com evidência de código e das duas recolhas reais.
2. Validar D-022 e a matriz com a CS default e, em seguida, com uma base PHC com desenvolvimento, segundo o protocolo de validação funcional. Não alterar collectors antes dessas recolhas.
3. Fechar o contrato físico de materialização no SQL Server.
4. Implementar materialização incremental, verificação integral e eliminação segura do ZIP.
5. Só depois avançar para comparação histórica, pesquisa contextual e RAG.

Não alterar collectors, workspace SQLite ou semântica de recolha antes de a matriz estar validada e a alteração correspondente ser aprovada. Não declarar materialização central concluída sem execução e verificação efectivas.

O [roadmap visual](docs/architecture/roadmap-visual-phc-knowledge-2026-11-06.md) regista a sequência e os critérios. As [orientações do projecto](PROJECT_GUIDELINES.md) definem a governação e a documentação canónica.


## Estado vigente após comparação das capturas — 2026-11-24

A validação funcional foi executada nas duas bases previstas pelo protocolo, com a mesma build do Scanner: CS default e uma instalação PHC com desenvolvimento (dfblock). A integridade dos ZIP, hashes, checksums, IKP 0.1, Object Index v2 e alinhamento dos workspaces por CaptureId foram confirmados.

Resultados agregados:
- CS default: 17 182 objectos no Object Index e 63 artefactos.
- dfblock: 18 954 objectos e 881 artefactos; mais 10 kinds PHC e 598 campos físicos `u_*`.
- Foram encontradas 12 correspondências com `phc.stamp`, em `formb`/`formc`.
- `sqlserver.object_id` não atravessa bases de dados. Cerca de 7 933 tabelas foram correspondidas por nome/`SourceObjectId`, como evidência observacional e não identidade persistente.
- Os envelopes D-022 estão correctos, mas não existe DefinitionFingerprint semântico disponível (`NoValidatedPolicy`); diff semântico de definições não demonstrado.
- Procedures e triggers ficaram Partial nas duas capturas. A causa dos 540 triggers `definition_missing` da dfblock não foi diagnosticada.

O relatório agregado, sem payload proprietário, está em [Avaliação de identidade e proveniência entre capturas](docs/audits/identity-provenance-cross-capture-assessment-2026-11-24.md). Os pacotes e workspaces permanecem locais; não foram adicionados ao repositório.

### Próxima fase de domínio

Antes da materialização incremental, fechar os contratos operacionais de:
1. identidade e correspondência entre capturas e instalações;
2. proveniência e classificação PHC Standard / desenvolvimento / alteração ao standard / origem desconhecida;
3. comparação exacta e semântica de definições.

A identidade lógica continua scoped à Installation. Correspondências entre instalações não fundem automaticamente entidades ou históricos. A ausência na CS default, isoladamente, não prova que um objecto seja personalizado. O catálogo PHC Standard versionado de D-015 continua a ser uma dependência.

A análise não autoriza alterações a código, collectors, schemas executáveis, SQLite, migrations ou materialização. A avaliação detalhada e as limitações estão no relatório ligado acima.


## Investigação funcional PHC — CS Default

A investigação exploratória das tabelas FT, FI e TD, cruzando a captura CSDefault202603, o script de triggers e a Enciclopédia PHC, está consolidada em [Investigação funcional PHC — CS Default: FT, FI e TD](docs/audits/investigacao-funcional-csdefault-ft-fi-td-2026-11-25.md). O relatório regista fontes, proveniência, relações REL/DTL, triggers, conclusões provisórias, lacunas e próximos passos. Os ZIP da captura e da Enciclopédia não são versionados; o material oficial PHC não é copiado para o repositório.

A auditoria de persistência da investigação de 25-11-2026 confirmou o relatório FT/FI/TD e identificou que a análise de VO e o circuito completo dos dossiers internos ainda não estão documentalmente comprovados. Ver [Recuperação da investigação funcional — FT, VO e dossiers internos](docs/audits/recuperacao-investigacao-funcional-vo-dossiers-2026-11-25.md), que regista explicitamente a lacuna e os passos de recuperação sem inferir conclusões não suportadas.


### Investigação complementar — stock, transferências e produção

A síntese funcional complementar de BI/FI/FN, transferências, produção, Manufactor e lacunas de SAL/SX está em [Investigação funcional PHC — stock, transferências e produção](docs/audits/investigacao-funcional-phc-stock-transferencias-producao-2026-11-27.md). É preservada como contexto histórico e complementar; as conclusões sobre PHC Gestão devem seguir a evidência específica do produto e não ser extrapoladas a partir de Manufactor.
