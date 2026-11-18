# PHC Knowledge — avaliação do estado atual

| Campo | Valor |
|---|---|
| Data | 2026-11-18 |
| Branch de referência | `main` |
| Repositório | `Jhouinho/PhcKnowledge` |
| Natureza | Auditoria documental e estática do GitHub |
| Estado | Preliminar; sujeito a validação por build, testes e execução real |

## 1. Objetivo e âmbito

Este relatório estabelece uma baseline do que existe no PHC Knowledge, distinguindo implementação observável, desenho documentado, trabalho em branches e trabalho ainda não demonstrado.

Foram considerados a estrutura da solução .NET, projetos incluídos, ficheiros de contrato IKP, código selecionado do Host e Import, testes de integração, documentação arquitetural, estado da `main`, PRs e diferenças de branches consultadas em 2026-11-18.

**Limitação importante:** não foi executado build, teste local, teste de deployment, nem uma importação ponta a ponta nesta auditoria. A existência de um projeto, classe ou teste no repositório não prova por si só que o fluxo esteja operacional num ambiente real.

## 2. Resumo executivo

O projeto ultrapassou a fase de definição conceptual do Scanner e do formato de transporte. O repositório contém:

- Scanner PHC integrado na solução, organizado em projetos especializados;
- workspace SQLite e componentes de captura/análise;
- exportador de pacotes IKP 0.1;
- schemas e validação do contrato;
- núcleo inicial de receção/importação;
- arquivo imutável de pacotes;
- persistência SQL Server de receipts/tentativas;
- Host .NET para executar recuperação, limpeza e monitorização;
- testes unitários e projetos de integração.

O principal vazio do produto está depois da receção do pacote: a transformação de uma captura validada em conhecimento de domínio persistido, contextualizado, pesquisável e reutilizável. A API, autenticação/autorização, experiência web, materialização de observações, identidade lógica, histórico/comparação e pesquisa do produto ainda não estão demonstrados como implementados.

**Diagnóstico:** a fundação de aquisição e transporte está substancialmente avançada; o núcleo de gestão e exploração do conhecimento continua por construir.

## 3. Inventário por capacidade

### 3.1 Scanner e aquisição

| Capacidade | Evidência observada | Avaliação |
|---|---|---|
| Scanner integrado no repositório | Projetos `PhcKnowledgeScanner.*` incluídos em `PhcKnowledge.sln` | Implementado no código |
| Separação de responsabilidades | Abstractions, Engine, Application, Collectors, Workspace, Analysis, Export, CLI e Desktop | Estrutura implementada |
| Workspace local | Projeto Workspace e uso em testes de exportação | Implementado; validar operação real |
| Captura de metadados e artefactos PHC | Coletores e contratos no Scanner | Implementado em parte; cobertura deve ser confirmada por tipo |
| Análise de relações declaradas | Teste de exportação verifica REL e DTL | Evidência de suporte no contrato e testes |
| Identidade nativa de objetos SQL | Trabalho identificado em branch própria, não confirmado na `main` durante esta revisão | Pendente de integração/decisão |
| Scanner Desktop e experiência final | Projeto Desktop existe; branches de evolução identificadas | Maturidade funcional por validar |

A captura real e as decisões de proveniência P1.B fazem parte do histórico do projeto. A validação do índice de objetos com uma captura real continua a ser um critério de aceitação relevante; não deve ser substituída por dados sintéticos.

### 3.2 Contrato e exportação IKP

| Capacidade | Evidência observada | Avaliação |
|---|---|---|
| Contrato IKP 0.1 | Especificação, schemas e fixtures | Definido |
| Exportação nativa | `IkpPackageExporter` e projeto Export | Implementado |
| Object Index | Schema com CaptureId, SourceObjectId, objectKind, metadata, definição, fingerprints e operationId | Definido e suportado pelo código |
| Estados de definição | Available, Empty, Unavailable, NotCollected no schema | Definidos |
| Integridade | SHA-256 e checksums no fluxo de exportação/validação | Implementado no contrato |
| Coverage/capabilities/operations | Ficheiros contratuais e teste de integração | Implementado no contrato |
| Compatibilidade histórica | Teste para metadados parciais/desconhecidos | Coberta por teste |
| Desempenho do JSON Schema | Branch de otimização remove validação dispendiosa de unicidade do array | Revisão e integração pendentes |

O teste `P1_ExportAsync_Produces_Zip_Accepted_By_IkpPackageValidator` demonstra um percurso sintético de exportação e validação. Não substitui a aceitação com uma captura real.

### 3.3 Receção, validação e arquivo

A `main` inclui `PhcKnowledge.Import` e `PhcKnowledge.Host`. O Host configura repositório SQL Server para tentativas de receção, armazenamento imutável em filesystem e serviços alojados para preflight, recuperação, limpeza e monitorização.

| Capacidade | Evidência observada | Avaliação |
|---|---|---|
| Validador independente do Scanner | Projeto Import e teste de integração | Implementado |
| Receção e staging | Arquitetura e componentes de ingestão | Primeira fatia implementada |
| Arquivo imutável | `FileSystemImmutablePackageStore` | Implementado |
| Receipts e tentativas | Repositório SQL Server e scripts de schema | Implementado |
| Recuperação de operações interrompidas | Serviço/worker de recuperação registados no Host | Implementado; operação real por validar |
| Limpeza de staging | Worker e serviço de limpeza | Implementado |
| Monitorização de operações pendentes | Monitor registado no Host e configuração de alertas | Implementado |
| API HTTP para receber pacotes | Não identificada na solução revista | Não demonstrada |
| Materialização em entidades de domínio | Não identificada na solução revista | Não implementada/demonstrada |

A PR #9, já integrada na `main` no snapshot consultado, introduziu a recuperação de receção. Documentação anterior que descreva a ausência dessa recuperação deve ser corrigida, mantendo explícito que falta validação operacional em ambiente real.

### 3.4 Plataforma de conhecimento

| Capacidade | Estado nesta auditoria |
|---|---|
| Organização e isolamento multi-organização | Requisito/desenho; implementação end-to-end não demonstrada |
| Utilizadores, autenticação e autorização | Não demonstrados |
| Instalações e ambientes | Modelo conceptual; persistência e fluxos não demonstrados |
| Associação de uma captura a uma instalação | Regra desenhada; fluxo operacional por implementar |
| Observation e evidência imutável | Conceito arquitetural; materialização não demonstrada |
| Identidade lógica entre capturas | Desenho em evolução; resolução implementada não demonstrada |
| Histórico e comparação | Não demonstrados |
| Catálogo de conhecimento PHC standard | Objetivo arquitetural; produto/catálogo implementado não demonstrado |
| Pesquisa textual/contextual | Não demonstrada na plataforma |
| Pesquisa semântica/RAG | Objetivo futuro; não implementado |
| Interface React/TypeScript | Não identificada na solução/revisão da árvore de projetos |
| Cliente desktop Knowledge via WebView2 | Não demonstrado |

## 4. Arquitetura e fronteiras

A direção documentada estabelece uma plataforma centralizada, com o Scanner como produtor independente de pacotes e o Knowledge como consumidor/importador. O Knowledge não deve depender do runtime do Scanner nem reintroduzir os módulos Discovery/Fusion do antigo ConsoleApp.

A documentação tecnológica consultada em `technology-and-import-contract-assessment.md` indica .NET 10, ASP.NET Core, React/TypeScript, SQL Server Express central, SQLite auxiliar e SignalR. Existe, porém, uma divergência com decisões anteriores do projeto que apontavam PostgreSQL como base central.

**Decisão necessária:** confirmar formalmente o motor relacional central antes de criar o modelo físico e as migrations. Não desenvolver em paralelo para dois motores nem assumir que a mudança para SQL Server está aprovada sem validação explícita.

A decisão de armazenamento dos pacotes também deve permanecer separada da escolha da base relacional: filesystem para desenvolvimento/on-prem e eventual object storage através de abstração são preocupações distintas.

## 5. Branches e trabalho não integrado

A comparação consultada em 2026-11-18 identificou branches antigas e trabalho parcialmente sobreposto. A idade de uma branch, isoladamente, não é critério suficiente para a eliminar.

| Branch/grupo | Leitura preliminar | Ação recomendada |
|---|---|---|
| `feat/package-receipt-recovery` | Trabalho integrado pela PR #9 | Considerar concluída; não reaplicar |
| `docs/domain-model-0.2-ingestion-sync` | Sem trabalho exclusivo relevante face à `main` no compare consultado | Arquivar/eliminar apenas após confirmação do owner |
| `cursor/optimize-object-index-schema-29b2` | Sobreposição com a otimização do schema | Comparar com a branch de otimização e escolher uma fonte |
| `feature/ikp-object-index-schema-perf` | Alteração de desempenho ainda não integrada na `main` no momento da revisão | Rever, testar e integrar se válida |
| `feature/ikp-import-validation-staging-20261108` | Trabalho ancestral/sobreposto com Import atual | Extrair apenas diferenças exclusivas úteis |
| `feature/ikp-0.1-package-exporter-20261108` | Exportador já presente na `main` | Confirmar ausência de alterações únicas antes de arquivar |
| `feature/sql-native-object-identities-20261108` | Identidades nativas SQL não encontradas no modelo da `main` revisto | Rever com prioridade; não descartar |
| `architecture/domain-model-0.2-review` | Decisões, estados e diagramas adicionais | Rever conteúdo, aprovar ou rejeitar explicitamente |
| Branches Scanner Desktop | Evolução do cliente Scanner | Confirmar quais as alterações integradas e quais continuam necessárias |

Esta tabela é uma triagem, não uma ordem de eliminação. Antes de apagar qualquer branch, comparar commits exclusivos, verificar PRs associadas e preservar alterações úteis numa branch ativa ou commit identificado.

## 6. Problemas, riscos e decisões pendentes

| ID | Tema | Risco/impacto | Prioridade |
|---|---|---|---|
| A-01 | Motor central SQL Server vs PostgreSQL | Bloqueia modelo físico, migrations e integração | Crítica |
| A-02 | Modelo de domínio 0.2 não consolidado na implementação | Risco de retrabalho e identidades inconsistentes | Crítica |
| A-03 | Receção ainda não ligada à materialização de conhecimento | Pacotes arquivados sem conhecimento pesquisável | Crítica |
| A-04 | Captura real ponta a ponta ainda é critério por fechar | Contrato pode funcionar em fixtures mas falhar em dados reais | Crítica |
| A-05 | Identidade nativa SQL em branch | Perda de identidade estável/precisa entre capturas | Alta |
| A-06 | API, autenticação, autorização e isolamento | Plataforma não utilizável por organizações | Alta |
| A-07 | Ausência de UI de produto demonstrada | Sem experiência operacional para utilizadores | Alta |
| A-08 | Pesquisa e catálogo standard por implementar | Valor de reutilização ainda não acessível | Alta |
| A-09 | Schema Object Index com otimização fora da main | Risco de custo de validação em pacotes grandes | Alta |
| A-10 | Documentação desatualizada após PR #9 | Orienta decisões com estado incorreto | Média |

## 7. Critérios de qualidade a preservar

1. **Artefacto primeiro:** decisões e funcionalidades devem partir de evidência capturada e contratos explícitos.
2. **Proveniência:** toda a informação consultável deve indicar captura, pacote, origem e estado de recolha.
3. **Separar evidência de interpretação:** observações originais não são reescritas por inferências posteriores.
4. **Identidade não é histórico:** identidade lógica, presença numa captura e alterações ao longo do tempo são conceitos separados.
5. **Sem perda de informação:** estados Empty, Unavailable e NotCollected não podem ser tratados como equivalentes.
6. **Idempotência:** reprocessar um pacote não deve duplicar observações ou alterar evidência arquivada.
7. **Isolamento organizacional:** nenhuma pesquisa ou associação pode atravessar organizações sem uma regra explícita de partilha.
8. **RAG com citações:** respostas futuras devem apontar para objetos, artefactos e capturas que suportam a resposta.
9. **Sem antecipar IA:** primeiro pesquisa determinística, proveniência e modelo de domínio; depois recuperação semântica.
10. **Scanner independente:** a plataforma importa o contrato IKP; não executa os coletores do Scanner.

## 8. Conclusão

O PHC Knowledge tem uma base de captura, contrato e ingestão inicial sólida para continuar. A prioridade deixou de ser acrescentar mais tipos de recolha ou iniciar um chatbot. É necessário fechar as decisões de domínio e tecnologia, provar a cadeia com uma captura real e construir a materialização que transforma pacotes em observações e conhecimento consultável.

Este relatório é uma baseline técnica, não uma certificação de produção. As conclusões devem ser atualizadas depois de build/testes e validação de uma importação real.
