# PHC Knowledge

Plataforma de conhecimento técnico sobre PHC, destinada a recolher, organizar, relacionar, pesquisar e reutilizar conhecimento com proveniência, evidências e contexto.

O repositório reúne a plataforma PHC Knowledge e o componente de recolha **PhcKnowledgeScanner**. O Scanner mantém autonomia funcional e executável, mas o seu código é versionado neste mesmo repositório.

## Estrutura e fronteiras

- `PhcKnowledgeScanner/`: aplicação autónoma de recolha read-only, workspace SQLite local e exportação IKP.
- `src/`: plataforma central, incluindo serviços de importação e host HTTP.
- `tests/`: testes da plataforma; os testes do Scanner permanecem nos seus projectos próprios.
- `docs/`: decisões, contratos, relatórios e fontes.

O Knowledge não executa o Scanner em runtime nem acede ao seu workspace SQLite. O pacote IKP é transferido do Scanner para o Portal; o SQL Server central é o armazenamento persistente do conhecimento, histórico e artefactos originais necessários.

## Desenvolvimento

Requisito: .NET SDK 10.0.403 ou posterior compatível com `global.json`.

A partir da raiz:

```bash
dotnet restore PhcKnowledge.sln
dotnet build PhcKnowledge.sln --no-restore
dotnet test PhcKnowledge.sln --no-build
```

O Scanner também mantém a sua solução própria em `PhcKnowledgeScanner/PhcKnowledgeScanner.sln`.

## Estado actual — 23-11-2026

**Referência:** `main`, commit `31deb2badb91a2b0453099290eaddaaa6afd944e`. A implementação D-022 foi integrada pela PR #27, merge commit `ad42f53fc3d4f7b654d61dea56f0f501af0905b8`.

### Direcção aprovada

- Uma organização interna; clientes PHC são contextos técnicos.
- Relação Cliente → Instalação → Capturas.
- Base central Microsoft SQL Server. PostgreSQL está excluído; edição/versão concreta ainda por decidir.
- Scanner autónomo, captura completa dentro do âmbito declarado e transporte IKP manual.
- D-015: manter factos REL/DTL e compará-los no Knowledge com o catálogo PHC Standard versionado.
- D-016: materialização e deduplicação incrementais no Knowledge; ausência só pode significar remoção quando a operação teve sucesso e a cobertura é completa.
- ZIP IKP exclusivamente temporário: só é eliminado depois de materialização integral e verificada.
- SQLite apenas como workspace/cache local do Scanner; não é a base central.

A referência normativa é [Decisões arquitecturais aprovadas](docs/architecture/decisoes-aprovadas-2026-11-21.md).

### Implementado e validado por CI

- Scanner autónomo, collectors existentes e exportador IKP 0.1.
- Receive / Validate / Archive, receipts, recovery e API HTTP IKP.
- Identity e backend de sessão do Portal.
- Análise IKP A+B persistida.
- Trigger Capability 2.0.
- D-022: Object Index v2, envelope de DefinitionFingerprint D-021, análise 0.3 e schemas/fixtures/testes actualizados.

Na execução CI associada ao merge D-022 passaram os workflows .NET Knowledge, .NET Scanner, validação do contrato IKP e integração SQL Server 2022. A suite dedicada do Scanner reportou 291 testes aprovados e 2 ignorados. Isto valida os pipelines executados; não equivale a validação numa instalação PHC real.

### Por construir ou validar

- Confirmação/rejeição da análise no Portal (Incremento C): existe a PR #20 aberta, mas não está integrada na `main`.
- UI React do Portal: não está integrada na `main`.
- Matriz validada de classificação por collector/ObjectKind com captura real.
- Catálogo PHC Standard versionado e comparação efectiva com REL/DTL e restantes evidências.
- Associação de Cliente/Instalação/Captura no fluxo do Portal.
- Materialização persistente de observações, relações, metadados e artefactos no SQL Server.
- Verificação pós-materialização e eliminação segura do ZIP/staging.
- Comparação histórica, pesquisa contextual e evolução RAG.
- Escolha da edição/versão SQL Server e detalhe físico de armazenamento de artefactos.
- Hardening operacional antes de produção.

### Estado da PR #20

A PR [#20 — Increment C](https://github.com/Jhouinho/PhcKnowledge/pull/20) continua aberta e fora da `main`. A branch está divergente da cabeça actual da `main` (97 commits à frente e 89 atrás no compare consultado), pelo que não deve ser integrada sem reconciliação/rebase e nova revisão integral. A sua existência não significa que as funcionalidades estejam disponíveis na `main`.

### Próxima sequência recomendada

1. Reconciliar a PR #20 com a `main` actual e rever os seus contratos, código e testes antes de qualquer merge.
2. Fechar a matriz collector/ObjectKind com a captura real existente, sem alterar collectors ou SQLite antes da validação.
3. Fechar o contrato físico de materialização no SQL Server.
4. Implementar materialização incremental, verificação integral e ciclo de vida temporário do ZIP.
5. Só depois avançar para comparação histórica e pesquisa contextual.

O roadmap e o relatório da revisão global registam os critérios, dependências e limitações. Não declarar validação PHC real nem materialização central concluída sem execução efectiva.
