# PHC Knowledge — roadmap de execução

| Campo | Valor |
|---|---|
| Data | 2026-11-18 |
| Baseline | `2026-11-18-phc-knowledge-current-state.md` |
| Estado | Roadmap proposto; sequência sujeita à validação das decisões bloqueantes |

## Princípios de execução

1. Fechar uma fase com critérios verificáveis antes de abrir trabalho dependente.
2. Não desenvolver UI, IA ou funcionalidades avançadas para compensar um modelo de domínio ainda instável.
3. Usar capturas reais para validar pressupostos do Scanner/IKP; não substituir evidência real por fixtures.
4. Preservar proveniência, imutabilidade e isolamento organizacional desde o primeiro schema.
5. Documentar no final de cada fase apenas o resultado, decisões, limitações e próximo passo.

## Visão geral

| Fase | Objetivo | Estado na baseline |
|---|---|---|
| R0 | Auditoria e baseline | Documentos preparados para revisão |
| R1 | Fechar Scanner e IKP | Object Index/exportação validados com captura real; falta validação do percurso central Import/Host |
| R2 | Aprovar modelo de domínio 0.2 | Modelo conceptual recomendado para aprovação; detalhes físicos/operacionais pendentes |
| R3 | Materializar conhecimento | Por implementar |
| R4 | API, segurança e plataforma central | Por implementar |
| R5 | UI e workflows de conhecimento | Por implementar |
| R6 | Histórico, identidade e pesquisa | Por implementar |
| R7 | Catálogo standard e RAG | Futuro |

## R0 — Auditoria e baseline

**Objetivo:** criar uma referência única e fiável do estado do produto.

Trabalho:
- inventariar projetos e responsabilidades;
- separar implementação, testes, documentação e propostas;
- rever branches e PRs;
- registar divergências e decisões por confirmar;
- identificar riscos e dependências.

Critérios de conclusão:
- relatório de estado atual aprovado;
- matriz de decisões/lacunas aprovada;
- roadmap priorizado aprovado;
- nenhuma branch eliminada sem triagem.

Entregáveis:
- `2026-11-18-phc-knowledge-current-state.md`;
- `2026-11-18-phc-knowledge-decisions-and-gaps.md`;
- este roadmap.

## R1 — Fechar Scanner e IKP 0.1

**Objetivo:** estabilizar o contrato de captura e a cadeia de exportação/validação.

Ordem de trabalho:
1. rever as branches de otimização do Object Index e escolher uma implementação;
2. rever identidades nativas SQL e decidir integração;
3. confirmar schema, validador C#, exportador e fixtures em conjunto;
4. executar build e testes do Scanner e Import;
5. reutilizar a captura real `CSDefault202603` já produzida em 2026-11-08;
6. usar o pacote IKP V4 existente para validar Import/Host, arquivo, receipt, repetição idempotente e recuperação;
7. registar limitações por tipo de objeto e operation coverage já observadas.

Critérios de conclusão:
- build e testes relevantes verdes;
- pacote real aceite pelo validador (já verificado em 2026-11-08);
- checksums e contagens reconciliados (já verificado no pacote V4);
- identidade e proveniência verificadas (Object Index P1.B);
- falhas/estados parciais documentados;
- nenhuma alteração ao modelo SQLite/coletores feita apenas para fazer passar o teste sem decisão explícita.

Dependências: acesso ao pacote IKP V4 já produzido e ambiente Import/Host configurado. Não é necessária nova captura para repetir a validação do Object Index/exportação.

## R2 — Aprovar modelo de domínio 0.2

**Objetivo:** aprovar o modelo conceptual 0.2 e fechar os detalhes necessários antes de criar persistência de domínio.

Entidades/conceitos a consolidar:
- Organization;
- User e Membership/Permission;
- Installation;
- Environment;
- DatabaseLocation/Source;
- Package e PackageReceipt;
- Capture;
- Observation;
- Artifact;
- DeclaredRelation e respetiva interpretação;
- EntityIdentity e regras de resolução;
- eventos de auditoria e histórico.

Questões obrigatórias:
- uma instalação pode ter vários ambientes e bases;
- uma captura é um evento imutável de observação;
- pacote e captura não são a mesma entidade;
- SourceObjectId só identifica o objeto dentro do âmbito da captura;
- identidade lógica pode ser desconhecida, ambígua ou revista;
- ausência numa captura não prova eliminação se a cobertura for incompleta;
- Empty, Unavailable e NotCollected mantêm semânticas distintas;
- relações declaradas pela origem não se confundem com relações inferidas;
- isolamento por organização deve ser imposto na persistência e nos serviços.

Critérios de conclusão:
- diagramas ER e de estados aprovados;
- dicionário de entidades/campos;
- constraints e índices definidos;
- decisões de identidade e proveniência documentadas;
- escolha da base relacional central confirmada: Microsoft SQL Server;
- detalhes físicos/operacionais fechados (DatabaseLocation, eventos/projeções, permissões/auditoria, reavaliações);
- modelo físico pronto para migrations.

Dependências: R1 e decisão D-01 do registo de decisões.

## R3 — Materialização do conhecimento

**Objetivo:** transformar pacotes validados em dados de domínio consultáveis, mantendo o pacote original como evidência imutável.

Componentes:
- aplicação/orquestração de materialização;
- persistência de Capture e Observation;
- ligações a artefactos;
- persistência de relações declaradas e interpretações;
- associação de instalação/ambiente;
- idempotência e reprocessamento;
- estados de materialização e auditoria.

Sequência:
1. definir contrato de materialização;
2. implementar schema e migrations;
3. materializar metadados e índice de objetos;
4. ligar definições a artefactos;
5. persistir relações declaradas sem as confundir com relações inferidas;
6. associar contexto ou manter a captura pendente quando ambígua;
7. garantir transações, idempotência e reprocessamento;
8. validar com pacote real.

Critérios de conclusão:
- pacote aceite pode ser materializado sem perda de evidência;
- reprocessamento não duplica dados;
- cada observação tem proveniência rastreável;
- captura parcial não gera falsas conclusões de ausência;
- isolamento organizacional demonstrado por testes.

## R4 — API, segurança e plataforma central

**Objetivo:** disponibilizar os casos de uso de domínio através de uma API segura.

Âmbito:
- ASP.NET Core API;
- autenticação;
- organizações, memberships e permissões;
- isolamento multi-organização;
- gestão de instalações/ambientes;
- endpoints de receção e consulta de estado;
- operações de pesquisa e detalhe;
- auditoria;
- SignalR apenas onde atualizações em tempo real acrescentem valor.

Critérios de conclusão:
- fluxos autenticados e autorizados;
- testes de isolamento entre organizações;
- API documentada;
- operação on-prem repetível;
- configuração e segredos fora do código;
- monitorização e logs operacionais úteis.

## R5 — Interface de produto

**Objetivo:** permitir a utilização diária sem depender de ferramentas de desenvolvimento.

Experiência inicial:
- visão geral da organização;
- instalações e ambientes;
- histórico de capturas;
- estado de receção/materialização;
- catálogo de objetos por captura;
- detalhe de objeto com definição, artefactos e proveniência;
- pesquisa textual;
- comparação inicial de capturas;
- gestão de utilizadores/permissões.

Tecnologia de referência documentada: React + TypeScript; desktop Windows através de WebView2 sobre a mesma experiência web, caso se mantenha a decisão.

Critérios de conclusão:
- fluxos principais completos;
- estados vazios, parciais e de erro claros;
- navegação até à evidência original;
- permissões refletidas na UI e impostas no backend;
- usabilidade validada com tarefas reais de consultoria PHC.

## R6 — Identidade, histórico e pesquisa avançada

**Objetivo:** relacionar observações ao longo do tempo sem alterar a evidência.

Capacidades:
- matching de identidade por tipo de objeto;
- identidade forte, provável, ambígua e desconhecida;
- revisão humana de associações;
- presença/alteração/desaparecimento/reaparecimento;
- comparação de definições e metadados;
- pesquisa full-text sobre nomes, código e metadados;
- referências cruzadas e dependências;
- filtros por origem, instalação, ambiente e captura.

Critérios de conclusão:
- regras de matching testadas por tipo;
- ambiguidade preservada em vez de resolvida arbitrariamente;
- comparação consciente da cobertura de cada captura;
- resultados de pesquisa sempre navegáveis até à proveniência.

## R7 — Conhecimento standard, recuperação semântica e RAG

**Objetivo:** reutilizar conhecimento PHC e responder a perguntas técnicas com evidência.

Primeiro, catálogo standard:
- tabelas e campos standard;
- relações e comportamentos conhecidos;
- triggers, eventos e regras standard;
- versionamento por versão PHC;
- separação de extensões/customizações por instalação.

Depois, recuperação semântica:
- fragmentação de código e documentação;
- embeddings e índice vetorial;
- pesquisa híbrida lexical + semântica;
- filtros de segurança e contexto;
- re-ranking quando justificado.

Por fim, RAG:
- perguntas sobre código, objetos, dependências e alterações;
- respostas com citações para objetos, artefactos e capturas;
- indicação explícita de incerteza e cobertura insuficiente;
- feedback e validação humana;
- avaliação com conjunto de perguntas técnicas reais.

Critérios de conclusão:
- nenhuma resposta sem referências verificáveis;
- permissões aplicadas antes da recuperação;
- métricas de precisão/recall e avaliação humana;
- separação clara entre factos capturados e inferências do modelo.

## Próxima sequência recomendada

A sequência imediata deve ser:

1. rever e aprovar os documentos R0;
2. atualizar e consolidar a decisão Microsoft SQL Server;
3. aprovar o modelo conceptual 0.2, mantendo explícitos os pontos reservados ao desenho físico;
4. rever as branches de identidade SQL e otimização Object Index, preservando alterações úteis;
5. executar Import/Host e arquivo com o pacote real já existente;
6. fechar o desenho físico SQL Server e iniciar R3, a materialização, com esse pacote.

**Não iniciar já:** chatbot, embeddings, RAG, Change Studio, UI extensa ou novas funcionalidades de recolha sem uma lacuna demonstrada.

## Definição de pronto por fase

Uma fase só fica concluída quando:
- o resultado esperado existe no repositório;
- testes relevantes foram executados e os resultados registados;
- decisões pendentes foram resolvidas ou explicitamente adiadas;
- limitações conhecidas estão documentadas;
- a fase seguinte tem dependências claras;
- existe uma reflexão curta: o que ficou feito, o que aprendemos e o que falta.
