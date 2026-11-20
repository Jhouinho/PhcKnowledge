# PHC Knowledge — Registo de decisões

> Registo canónico de decisões de produto e arquitetura. Não substitui ADRs técnicos do Scanner nem contratos IKP/P1. Decisões explícitas do responsável prevalecem sobre documentos históricos.

Última revisão: 2026-11-20.

## Decisões aprovadas

| ID | Decisão | Estado |
|---|---|---|
| D-001 | Ferramenta interna da equipa para gerir conhecimento técnico dos clientes acompanhados; não produto comercial multiempresa. | Aprovada |
| D-002 | A base de dados central da plataforma é Microsoft SQL Server. | Aprovada; edição/versão por decidir |
| D-003 | PostgreSQL não será usado como base central nesta arquitetura. | Excluída |
| D-004 | SQLite é apenas workspace/cache local do PhcKnowledgeScanner. | Aprovada |
| D-005 | Scanner é autónomo em execução e responsabilidade, embora versionado no monorepo. Knowledge não o executa em runtime. | Aprovada |
| D-006 | Scanner consulta a base PHC da instalação em modo read-only e emite IKP 0.1. | Aprovada |
| D-007 | ZIP IKP é transporte temporário. Após análise, decisão humana, materialização e confirmação de persistência íntegra, ZIP e temporários devem ser eliminados. | Aprovada; implementação ainda divergente |
| D-008 | PHC Standard curado e observações/personalizações de instalações são camadas distintas. Observações de clientes não são partilhadas automaticamente. | Aprovada |
| D-009 | Factos originais, interpretações derivadas, projeções e anotações humanas mantêm proveniência e semântica separadas. | Aprovada |
| D-010 | Interface web React/TypeScript; backend .NET 10/ASP.NET Core; cliente Windows via WebView2; SignalR conforme os casos de uso. | Direção aprovada |
| D-011 | RAG é objetivo futuro, depois de existir conhecimento estruturado, curado e pesquisável. | Aprovada como objetivo; tecnologia por decidir |
| D-012 | Documentação e interface em Português de Portugal (pt-PT). | Aprovada |
| D-013 | GitHub é a fonte partilhada de verdade entre ChatGPT e Cursor para o estado publicado. | Aprovada |

## Formulações antigas substituídas ou esclarecidas

| Formulação antiga | Estado atual |
|---|---|
| PostgreSQL como base central | Rejeitada; D-002/D-003 estabelecem SQL Server |
| SQL Server Express como restrição obrigatória | Não aprovada como restrição; edição/versão por decidir |
| Plataforma multi-organização/multiempresa como produto | Substituída por ferramenta interna; manter isolamento por cliente/instalação e permissões |
| Conhecimento observado numa instalação partilhado automaticamente | Rejeitado; apenas PHC Standard curado ou conteúdo explicitamente promovido pode ser reutilizado globalmente |
| Arquivo permanente do ZIP IKP | Substituído por transporte temporário e eliminação após persistência íntegra |
| Portal sem frontend React | Informação histórica, ultrapassada pelo início da implementação React |

## Questões em aberto

1. Edição e versão SQL Server central, dimensionamento, licenciamento, backup e recuperação.
2. Estratégia de pesquisa textual no SQL Server e critérios para eventual motor externo.
3. Modelo físico final de clientes/instalações/ambientes e respetivas permissões.
4. Pipeline PHC Standard Catalog: fontes, extração, curadoria, versionamento, persistência e publicação.
5. Critérios exatos de confirmação de materialização e eliminação segura do ZIP/temporários, incluindo falhas e recuperação.
6. Cobertura real dos collectors por operação, incluindo testes diretos contra SQL Server PHC.
7. Sequência e critérios de aceitação para materialização de observações, diff, pesquisa autorizada e RAG.

## Registo de novas decisões

Só marcar como aprovada após validação explícita do responsável. Registar data, contexto, decisão, alternativas rejeitadas, impacto e documentos afetados. Não apagar decisões substituídas: marcar o estado e apontar para a decisão vigente.
