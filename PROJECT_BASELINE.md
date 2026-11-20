# PHC Knowledge — Baseline do projeto

> Documento canónico. Em caso de conflito com documentos históricos, esta baseline e DECISIONS.md representam a direção aprovada. O estado efetivo do código está em PROJECT_STATUS.md.

- Revisão: 2026-11-20
- Linguagem documental e da interface: Português de Portugal (pt-PT)
- Repositório: Jhouinho/PhcKnowledge

## 1. Objetivo

Ferramenta interna da equipa que trabalha com PHC para organizar, relacionar, pesquisar e reutilizar conhecimento técnico adquirido nos clientes e nas respetivas instalações.

Não é um produto SaaS comercial multiempresa, um clone do ERP PHC, um Digital Twin ou apenas um explorador SQL.

## 2. Modelo de conhecimento

Manter separadas:
1. PHC Standard: conhecimento geral, obtido de fontes identificadas, versionado e sujeito a curadoria. A presença numa instalação não prova que algo seja standard.
2. Observações de instalações: factos recolhidos numa instalação concreta, associados ao cliente, instalação, ambiente, base de dados, captura e fonte.
3. Personalizações: objetos e definições específicos de uma instalação; não são partilhados automaticamente com outros clientes.
4. Interpretações e projeções: resultados derivados, versionados e com evidência; nunca substituem silenciosamente os factos originais.
5. Conhecimento reutilizável: conteúdo promovido por curadoria explícita, preservando origem e âmbito.

Observações privadas não se tornam conhecimento global por aparecerem em várias instalações. O acesso deve respeitar o contexto do cliente/instalação.

## 3. Arquitetura aprovada

- Plataforma central, servida por backend e acedida pelo browser.
- Cliente desktop Windows através de WebView2, reutilizando a aplicação web.
- Backend: .NET 10 / ASP.NET Core.
- Frontend: React + TypeScript.
- Base de dados central: Microsoft SQL Server.
- PostgreSQL está excluído desta arquitetura.
- A edição/versão concreta do SQL Server não está fixada; não limitar a plataforma a Express sem decisão explícita e análise de capacidade.
- SQLite é exclusivamente workspace/cache local do PhcKnowledgeScanner; não é a base central.
- SignalR mantém-se como tecnologia prevista para atualizações em tempo real, conforme os casos de uso.
- Começar pelas capacidades de pesquisa integradas no SQL Server; não introduzir motor externo ou base vetorial sem decisão fundamentada.

## 4. Fronteiras Scanner / Knowledge

### PhcKnowledgeScanner
- Componente autónomo, versionado neste monorepo, com solução, testes e execução próprios.
- Executa no servidor/ambiente do cliente e consulta a base PHC em modo read-only.
- Recolhe inventário e personalizações dentro das operações e limites documentados.
- Mantém workspace SQLite local e artefactos locais.
- Emite pacotes IKP 0.1 em ZIP.
- Não envia o ZIP diretamente para o PHC Knowledge, não depende do backend Knowledge em runtime e não mantém a base central.

### PHC Knowledge
- Recebe manualmente, através do Portal, os pacotes IKP transferidos pelo operador a partir do ambiente do cliente.
- Analisa o conteúdo, apresenta evidências e suporta decisões humanas.
- Materializa no SQL Server central a informação necessária ao modelo de conhecimento, com proveniência.
- Não executa o Scanner nem acede diretamente ao workspace SQLite.

## 5. Ciclo de vida do pacote

O percurso operacional aprovado é manual e separado em dois ambientes:
1. O Scanner é executado no servidor/ambiente do cliente e gera o ZIP IKP.
2. O operador transfere manualmente esse ZIP para um ambiente a partir do qual consegue aceder ao PHC Knowledge.
3. O operador autentica-se no Portal e faz o upload manual do ZIP.

Não existe comunicação direta entre o Scanner e o PHC Knowledge, nem o Scanner necessita de credenciais ou conectividade com o servidor central.

O ZIP IKP é um meio temporário de transporte, não o arquivo permanente do conhecimento.

Objetivo aprovado:
1. Receber e validar o pacote.
2. Analisar o conteúdo e o contexto.
3. Obter a decisão humana necessária.
4. Materializar e confirmar a persistência íntegra da informação necessária no SQL Server central.
5. Só depois da confirmação de integridade e conclusão do processamento, eliminar o ZIP e os ficheiros temporários.

Não eliminar o pacote antes da confirmação de persistência. A implementação de arquivo permanente identificada na auditoria diverge deste objetivo e tem de ser reconciliada.

## 6. PHC Standard Catalog

O conhecimento PHC standard deve ter fontes controladas e separadas das capturas dos clientes. A branch contém uma fonte SQL de triggers standard e o projeto PhcKnowledge.StandardCatalog com extração/parsing de triggers.

Isto não demonstra uma pipeline completa de ingestão, classificação, curadoria, versionamento, persistência e publicação. Não inferir que todo o conteúdo da base PHC é standard nem que o Scanner recolhe o dicionário PHC completo.

## 7. Objetivos e exclusões

- RAG é objetivo futuro de consumo do conhecimento estruturado; não antecipar embeddings, vector store ou fornecedor.
- Sem modo offline autónomo da plataforma, peer-to-peer sync ou resolução de conflitos entre bases independentes.
- Não reintroduzir Discovery/Fusion nem Digital Twin.
- Não transformar o Scanner numa dependência de runtime do Knowledge.
- Não confundir cobertura parcial dos collectors com dicionário PHC completo.

## 8. Continuidade entre ferramentas

O GitHub é a fonte partilhada de verdade para o estado publicado. Antes de qualquer tarefa, ChatGPT e Cursor devem ler:
1. PROJECT_BASELINE.md;
2. DECISIONS.md;
3. PROJECT_STATUS.md;
4. docs/governance/current-handoff.md;
5. PROJECT_GUIDELINES.md, regras .cursor/rules/ e documentação técnica relevante.

No fim de cada sessão, atualizar o handoff com branch/HEAD, alterações, decisões, testes, pendências e próximo passo. Se a publicação não estiver autorizada ou não for possível, declarar o que ficou apenas local.
