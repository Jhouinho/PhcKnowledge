# Revisão global da main — PHC Knowledge

**Data:** 2026-11-23  
**Repositório:** `Jhouinho/PhcKnowledge`  
**Branch:** `main`  
**HEAD consultado:** `31deb2badb91a2b0453099290eaddaaa6afd944e`  
**Merge D-022:** `ad42f53fc3d4f7b654d61dea56f0f501af0905b8` (PR #27)

## Objectivo e âmbito

Rever a coerência entre o estado da main, as decisões aprovadas, a implementação D-022, os workflows CI e o roadmap. A revisão foi documental e remota; não executou código nem acedeu a uma instalação PHC real ou ao SQL Server do utilizador.

## Resultado

A main contém a implementação D-022. O IKP global mantém-se em 0.1; o Object Index está em schema v2 e a análise de importação em 0.3. O Scanner separa integridade do artefacto e fingerprint semântico; sem política validada, o fingerprint fica explicitamente indisponível. O Importer lê a nova representação e readiness/coverage não ficam bloqueadas apenas pela indisponibilidade do fingerprint.

### CI confirmado no merge D-022

No commit `ad42f53fc3d4f7b654d61dea56f0f501af0905b8`, os quatro workflows terminaram com sucesso:

- .NET tests / Knowledge;
- .NET — PHC Knowledge Scanner;
- IKP 0.1 contract validation;
- SQL Server integration tests (SQL Server 2022 no pipeline).

A suite dedicada do Scanner reportou 291 testes aprovados e 2 ignorados. Estes resultados não são validação numa instalação PHC real.

## Estado consolidado

### Implementado na main

- Scanner autónomo e exportador IKP 0.1.
- Recepção, validação, receipts, recovery e API HTTP IKP.
- Identity e backend de autenticação/sessão do Portal.
- Análise IKP A+B persistida.
- Trigger Capability 2.0.
- D-022: Object Index v2, DefinitionFingerprint D-021, PackageImportAnalysis 0.3, schemas, fixtures e testes actualizados.

### Parcial, fora da main ou por validar

- Incremento C (confirmar/rejeitar análise) e UI React: PR #20 aberta, não integrada.
- A branch da PR #20 está divergente: 97 commits à frente e 89 atrás da main no compare consultado. Inclui alterações extensas em documentação, solução, backend, frontend, catálogo standard e testes. Não deve ser integrada sem reconciliação/rebase e revisão integral.
- D-022 ainda não foi exercitada com uma captura nova do Scanner numa instalação PHC real.
- Matriz completa de classificação collector/ObjectKind ainda por fechar com captura real.
- Associação de Cliente/Instalação/Captura no fluxo do Portal por integrar.
- Materialização persistente de observações, REL/DTL, metadados e artefactos originais no SQL Server por construir.
- Verificação pós-materialização, recuperação e eliminação segura do ZIP/staging por implementar.
- Catálogo PHC Standard versionado/comparação efectiva, comparação histórica e pesquisa contextual por concluir.
- Edição/versão SQL Server e detalhe físico do armazenamento de artefactos por decidir.
- Hardening operacional pré-produção por concluir.

## Coerência documental

Foram identificadas referências desactualizadas em:
- `README.md`: HEAD antigo, estado pré-D-022 e próximo passo já ultrapassado.
- `PROJECT_GUIDELINES.md`: estado 18-11 apresentado sem baseline mais recente.
- Roadmap: Incremento C descrito como aguardando autorização, sem referência à PR aberta; sem estado D-022 integrado.
- `PhcKnowledgeScanner/docs/PROJECT-STATUS.md`: afirmações antigas de que o Knowledge não tinha API/ingestão e descreve o produto central como fora do monorepo.

Os relatórios e decisões históricos não foram reescritos. Os documentos de orientação foram actualizados com uma secção/estado vigente que prevalece sobre fotografias antigas.

## Próxima sequência recomendada

1. Reconciliar a PR #20 com a main actual (ou reaplicar o trabalho numa branch nova baseada na main); rever integralmente e executar CI antes de qualquer merge.
2. Validar D-022 com uma captura nova numa instalação PHC de teste e pacote IKP produzido pelo Scanner actualizado.
3. Fechar a matriz collector/ObjectKind com evidência real, sem alterar collectors/SQLite antecipadamente.
4. Fechar o contrato físico de materialização no SQL Server central.
5. Implementar materialização incremental, verificação integral e ciclo de vida temporário do ZIP.
6. Só depois avançar para diff histórico e pesquisa contextual/RAG.

## Alterações desta revisão

Actualizados na main:
- `README.md`;
- `PROJECT_GUIDELINES.md`;
- `docs/architecture/roadmap-visual-phc-knowledge-2026-11-06.md`;
- `PhcKnowledgeScanner/docs/PROJECT-STATUS.md`;
- este relatório.

Não foram alterados código, schemas executáveis, fixtures, collectors, workspace SQLite, migrations, pipelines ou materialização SQL. Não foi executado build/teste local. Não foi feita validação PHC real.
