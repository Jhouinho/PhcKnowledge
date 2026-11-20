# Inventário documental do PHC Knowledge

- Data da revisão: 2026-11-20
- Repositório: Jhouinho/PhcKnowledge
- Branch: feature/increment-c-package-import-analysis-confirmation
- HEAD de referência: 6fdb4060a8fdb2de8aec9080620b91b4c9fa89f5
- Âmbito: todos os ficheiros versionados com extensão .md, .mdc ou .txt na árvore Git.
- Total inventariado: 85 ficheiros.
- Estado: classificação documental inicial concluída; não equivale a validação integral de cada afirmação técnica nem autoriza apagar ficheiros.

## Regras de manutenção

1. Os documentos canónicos são PROJECT_BASELINE.md, DECISIONS.md, PROJECT_STATUS.md e docs/governance/current-handoff.md; docs/governance/README.md define a ordem de leitura.
2. ADRs, contratos IKP/P1, relatórios de fase, investigações e fontes brutas devem ser preservados como registos históricos, salvo decisão explícita em contrário.
3. Uma fotografia histórica pode conter afirmações corretas para a data em que foi escrita e falsas hoje. Não a reescrever retroativamente: adicionar nota de precedência/estado e encaminhar para o documento canónico atual.
4. Não inferir implementação a partir de uma proposta nem validação a partir da existência de testes. Confirmar código, workflow e commit.
5. A classificação “preservar” não significa que o conteúdo esteja atualizado; significa que o seu valor histórico/contratual não deve ser perdido.
6. Esta matriz identifica o destino de manutenção de cada ficheiro. Os documentos marcados como “validar antes de alterar” exigem leitura específica antes de qualquer edição futura.

## Inventário individual

| Ficheiro | Classe | Tratamento recomendado |
|---|---|---|
| `.cursor/rules/project-context.mdc` | Regra operacional | Canónica; manter sincronizada com baseline, decisões e acordo de continuidade. |
| `DECISIONS.md` | Decisão | Canónico; só alterar com aprovação explícita. |
| `PROJECT_BASELINE.md` | Baseline | Canónico; fonte da arquitetura e fronteiras aprovadas. |
| `PROJECT_GUIDELINES.md` | Orientação | Canónico de entrada para desenvolvimento; evitar duplicar decisões. |
| `PROJECT_STATUS.md` | Estado | Canónico; confirmar sempre contra código/CI atual. |
| `PhcKnowledgeScanner/README.md` | Entrada Scanner | Atualizado nesta fase; manter fronteira autónoma. |
| `PhcKnowledgeScanner/docs/PROJECT-STATUS.md` | Estado histórico Scanner | Fotografia de 10-11-2026; preservar como histórico, não usar para estado atual do Knowledge. |
| `PhcKnowledgeScanner/docs/architecture-proposal.md` | Proposta histórica Scanner | Preservar como estudo inicial; não usar como arquitetura normativa. |
| `PhcKnowledgeScanner/docs/architecture.md` | Arquitetura histórica Scanner | Fotografia inicial; preservar, código/ADRs prevalecem. |
| `PhcKnowledgeScanner/docs/database-schema-snapshot.md` | Especificação Scanner | Preservar; funcionalidade auxiliar separada do fluxo normal. |
| `PhcKnowledgeScanner/docs/decisions/0001-dotnet-version.md` | ADR Scanner | Decisão técnica individual; preservar; verificar se foi substituída antes de alterar. |
| `PhcKnowledgeScanner/docs/decisions/0002-project-structure.md` | ADR Scanner | Decisão técnica individual; preservar; verificar se foi substituída antes de alterar. |
| `PhcKnowledgeScanner/docs/decisions/0003-independence.md` | ADR Scanner | Decisão técnica individual; preservar; verificar se foi substituída antes de alterar. |
| `PhcKnowledgeScanner/docs/decisions/0004-full-table-inventory.md` | ADR Scanner | Decisão técnica individual; preservar; verificar se foi substituída antes de alterar. |
| `PhcKnowledgeScanner/docs/decisions/0005-declared-expression-interpretation-v1.md` | ADR Scanner | Decisão técnica individual; preservar; verificar se foi substituída antes de alterar. |
| `PhcKnowledgeScanner/docs/decisions/0006-declared-expression-interpretation-v1.1.md` | ADR Scanner | Decisão técnica individual; preservar; verificar se foi substituída antes de alterar. |
| `PhcKnowledgeScanner/docs/decisions/0007-full-pk-fk-inventory.md` | ADR Scanner | Decisão técnica individual; preservar; verificar se foi substituída antes de alterar. |
| `PhcKnowledgeScanner/docs/decisions/0008-relational-knowledge-projection-v1.md` | ADR Scanner | Decisão técnica individual; preservar; verificar se foi substituída antes de alterar. |
| `PhcKnowledgeScanner/docs/decisions/0009-field-mapping-extraction-v1.md` | ADR Scanner | Decisão técnica individual; preservar; verificar se foi substituída antes de alterar. |
| `PhcKnowledgeScanner/docs/decisions/0010-scan-knowledge-session.md` | ADR Scanner | Decisão técnica individual; preservar; verificar se foi substituída antes de alterar. |
| `PhcKnowledgeScanner/docs/decisions/0011-desktop-inventory-workspace-session.md` | ADR Scanner | Decisão técnica individual; preservar; verificar se foi substituída antes de alterar. |
| `PhcKnowledgeScanner/docs/decisions/0012-desktop-inventory-relation-exploration.md` | ADR Scanner | Decisão técnica individual; preservar; verificar se foi substituída antes de alterar. |
| `PhcKnowledgeScanner/docs/decisions/0013-desktop-field-mapping-exploration.md` | ADR Scanner | Decisão técnica individual; preservar; verificar se foi substituída antes de alterar. |
| `PhcKnowledgeScanner/docs/decisions/0014-database-schema-snapshot.md` | ADR Scanner | Decisão técnica individual; preservar; verificar se foi substituída antes de alterar. |
| `PhcKnowledgeScanner/docs/handoff-1d.2.1-desktop-inventory.md` | Handoff histórico Scanner | Preservar evidência de execução; não usar como handoff global atual. |
| `PhcKnowledgeScanner/docs/handoff-1d.2.2-desktop-relations.md` | Handoff histórico Scanner | Preservar evidência de execução; não usar como handoff global atual. |
| `PhcKnowledgeScanner/docs/handoff-1d.2.3-field-mappings.md` | Handoff histórico Scanner | Preservar evidência de execução; não usar como handoff global atual. |
| `PhcKnowledgeScanner/docs/open-decisions.md` | Backlog histórico Scanner | Reclassificar por decisão/implementação/aberta; não tratar checklist antiga como backlog vigente. |
| `PhcKnowledgeScanner/docs/research/fatura-manual-frenetikmove-2026-11-04.md` | Investigação instalação | Preservar como caso específico; não generalizar para PHC Standard. |
| `PhcKnowledgeScanner/docs/scope.md` | Âmbito histórico Scanner | Lista orientadora; confrontar com collectors e matriz IKP. |
| `PhcKnowledgeScanner/docs/testing-strategy.md` | Estratégia de testes | Atualizada nesta fase para quatro níveis de evidência. |
| `PhcKnowledgeScanner/samples/README.md` | Índice de samples | Atualizar estrutura real e classificar cada sample como sintético/sanitizado/real. |
| `PhcKnowledgeScanner/samples/database-schema-snapshot/documentation/overview.md` | Exemplo de contrato | Preservar como sample; confirmar se é sintético/sanitizado e não tratar como captura real. |
| `README.md` | Entrada do repositório | Canónico como índice público; encaminha para documentos de governação. |
| `docs/architecture/collector-coverage-investigation-2026-11-06.md` | Investigação histórica | Preservar diagnóstico; coverage posterior implementada, riscos ainda relevantes. |
| `docs/architecture/diagramas-projeto-er-e-classes-2026-11-06.md` | Diagramas históricos Scanner | Preservar como fotografia do workspace SQLite; não é modelo SQL Server central. |
| `docs/architecture/extract-2026-11-conversation.md` | Documento técnico/histórico | Preservar; classificar como contrato, proposta ou fotografia antes de qualquer alteração. |
| `docs/architecture/fixtures/ikp-0.1/minimal-valid/README.md` | Fixture/schema IKP | Contrato técnico; preservar e validar com os validadores. |
| `docs/architecture/fixtures/ikp-0.1/minimal-valid/VALIDATION.md` | Fixture/schema IKP | Contrato técnico; preservar e validar com os validadores. |
| `docs/architecture/ikp-0.1-scanner-compatibility-matrix.md` | Matriz compatibilidade | Contrato técnico; atualizar estado conforme evolução real do Scanner/API. |
| `docs/architecture/ikp-temporary-package-lifecycle-gap-2026-11-20.md` | Auditoria técnica de ciclo de vida | Divergência confirmada entre ZIP temporário aprovado e arquivo permanente implementado; preservar até a materialização ser desenhada e implementada. |
| `docs/architecture/ikp-0.1-specification.md` | Contrato IKP | Preservar como contrato; separar formato de transporte da política de retenção. |
| `docs/architecture/ikp-0.1-validation-performance-checklist-2026-11-11.md` | Documento técnico/histórico | Preservar; classificar como contrato, proposta ou fotografia antes de qualquer alteração. |
| `docs/architecture/increment-c-package-import-analysis-confirmation-design-2026-11-19.md` | Desenho + implementação posterior | Preservar decisões de desenho; atualizar estado de implementação observado na branch. |
| `docs/architecture/knowledge-domain-model-0.2.md` | Modelo conceptual histórico | Preservar conceitos úteis; corrigir conflitos apenas com notas de precedência. |
| `docs/architecture/knowledge-layers.md` | Modelo de camadas | Atualizado; manter semântica standard/observações privadas. |
| `docs/architecture/observation-identity-and-lineage-contract.md` | Contrato/investigação P1 | Preservar como base de identidade/proveniência; verificar implementação antes de atualizar. |
| `docs/architecture/p1-identity-provenance-contract.md` | Contrato/investigação P1 | Preservar como base de identidade/proveniência; verificar implementação antes de atualizar. |
| `docs/architecture/p1-identity-provenance-fingerprints-investigation.md` | Contrato/investigação P1 | Preservar como base de identidade/proveniência; verificar implementação antes de atualizar. |
| `docs/architecture/p1b-ikp-package-export-readiness-investigation-2026-11-07.md` | Contrato/investigação P1 | Preservar como base de identidade/proveniência; verificar implementação antes de atualizar. |
| `docs/architecture/p1b-metadata-fingerprint-policy-2026-11-06.md` | Contrato/investigação P1 | Preservar como base de identidade/proveniência; verificar implementação antes de atualizar. |
| `docs/architecture/p1b-object-index-emitter-investigation-2026-11-06.md` | Contrato/investigação P1 | Preservar como base de identidade/proveniência; verificar implementação antes de atualizar. |
| `docs/architecture/p1b-object-index-metadata-projection-2026-11-06.md` | Contrato/investigação P1 | Preservar como base de identidade/proveniência; verificar implementação antes de atualizar. |
| `docs/architecture/p1b-object-index-projector-design-2026-11-06.md` | Contrato/investigação P1 | Preservar como base de identidade/proveniência; verificar implementação antes de atualizar. |
| `docs/architecture/p1b-projector-workspace-read-contract-2026-11-06.md` | Contrato/investigação P1 | Preservar como base de identidade/proveniência; verificar implementação antes de atualizar. |
| `docs/architecture/p1b-source-identity-scope-matrix-2026-11-06.md` | Contrato/investigação P1 | Preservar como base de identidade/proveniência; verificar implementação antes de atualizar. |
| `docs/architecture/p1b-static-consistency-audit-2026-11-06.md` | Contrato/investigação P1 | Preservar como base de identidade/proveniência; verificar implementação antes de atualizar. |
| `docs/architecture/p2b-http-ingestion-contract-proposal.md` | Proposta implementada | Preservar contrato aprovado; adicionar estado de implementação posterior e não confundir com código atual. |
| `docs/architecture/package-receipt-host-operations-2026-11-17.md` | Relatório operacional | Preservar; descreve workers, não a totalidade do Host/API atual. |
| `docs/architecture/package-receipt-recovery-2026-11-17.md` | Relatório de implementação | Preservar evidência; atualizar referências de branch/API e assinalar conflito do arquivo permanente. |
| `docs/architecture/phase-report-identity-portal-2026-11-18.md` | Relatório de fase | Fotografia histórica; preservar sem reescrever retroativamente. |
| `docs/architecture/phase-report-ikp-package-analysis-a-2026-11-19.md` | Relatório de fase | Fotografia histórica; preservar sem reescrever retroativamente. |
| `docs/architecture/phase-report-ingestion-contract-2026-11-17.md` | Relatório de fase | Fotografia histórica; preservar sem reescrever retroativamente. |
| `docs/architecture/portal-experience-specification-2026-11-19.md` | Especificação Portal | Preservar como especificação; atualizar estado quando implementação divergir. |
| `docs/architecture/portal-frontend-architecture-spike-2026-11-19.md` | Especificação Portal | Preservar como especificação; atualizar estado quando implementação divergir. |
| `docs/architecture/portal-frontend-readiness-review-2026-11-19.md` | Revisão histórica Portal | Preservar fotografia pré-frontend; aviso de estado atual adicionado. |
| `docs/architecture/product-boundaries.md` | Fronteiras produto | Atualizar referência ao estado de implementação; manter visão e separação Scanner/Knowledge. |
| `docs/architecture/rag-readiness-and-knowledge-first-decision-2026-11-06.md` | Decisão estratégica RAG | Preservar princípio; corrigir roadmap e referências tecnológicas ultrapassadas. |
| `docs/architecture/real-ikp-receipt-integration-harness-2026-11-18.md` | Harness real | Preservar como evidência limitada ao percurso testado e captura indicada. |
| `docs/architecture/roadmap-visual-phc-knowledge-2026-11-06.md` | Roadmap histórico | Atualizar fotografia de estado; não assumir que estados de 19-11 refletem branch atual. |
| `docs/architecture/scanner-controlled-change-management-vision-2026-11-07.md` | Documento técnico/histórico | Preservar; classificar como contrato, proposta ou fotografia antes de qualquer alteração. |
| `docs/architecture/schemas/ikp-0.1/README.md` | Fixture/schema IKP | Contrato técnico; preservar e validar com os validadores. |
| `docs/architecture/sql-server-receipt-integration-tests-2026-11-17.md` | Evidência de testes | Preservar; testes SQL de receipts não provam collectors Scanner. |
| `docs/architecture/technology-and-import-contract-assessment.md` | Avaliação histórica | Preservar como avaliação; baseline atual prevalece em stack, uso e ciclo de ZIP. |
| `docs/governance/README.md` | Governação | Canónico; ordem de leitura. |
| `docs/governance/document-inventory.md` | Inventário documental | Matriz individual de 84 ficheiros; atualizar quando a árvore documental mudar. |
| `docs/governance/audit-progress.md` | Auditoria | Estado de auditoria; atualizar por etapa. |
| `docs/governance/current-handoff.md` | Handoff | Canónico operacional; atualizar no fecho de cada sessão. |
| `docs/governance/working-agreement.md` | Acordo | Canónico; preservar protocolo entre ferramentas. |
| `docs/prototypes/portal/README.md` | Protótipo conceptual | Preservar; dados e ações são fictícios, não representam UI funcional. |
| `docs/sources/README.md` | Índice de fontes | Canónico para fontes brutas; manter catálogo e proveniência. |
| `docs/sources/analisar-base-phc-2026-11.txt` | Fonte histórica | Preservar integralmente; não editar como documentação normativa. |
| `src/PhcKnowledge.Portal/README.md` | Documentação componente | Manter alinhada com frontend React real e comandos existentes. |
| `tools/ikp-validation/README.md` | Ferramenta validação | Preservar; confirmar comandos e contratos quando alterados. |
| `tools/ikp-validation/requirements.txt` | Dependências ferramenta | Ficheiro técnico, não documento de arquitetura. |

## Resultado desta revisão

- Criada uma camada canónica de governação e contexto.
- Corrigidas referências prioritárias sobre SQL Server, PostgreSQL, SQLite, ZIP temporário, PHC Standard e existência do frontend React.
- Mantidos os documentos históricos, ADRs, contratos, relatórios e fontes.
- Permanecem por rever tecnicamente o ciclo de materialização/eliminação do ZIP, a pipeline PHC Standard Catalog, a cobertura dos collectors e a estratégia de pesquisa SQL Server.
- Não foi autorizada nem executada qualquer eliminação documental.
