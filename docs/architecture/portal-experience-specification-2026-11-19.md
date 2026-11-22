> **Revisão vinculativa — 21-11-2026:** esta especificação deve seguir o modelo de uma única organização interna, clientes como contextos técnicos, SQL Server central sem edição/versão escolhida e ZIP temporário eliminado após materialização verificada. Ver [decisões aprovadas](decisoes-aprovadas-2026-11-21.md).

# PHC Knowledge — Especificação da experiência do Portal

| Campo | Valor |
|---|---|
| Data | 2026-11-19 |
| Estado | Proposta consolidada para validação de produto |
| Âmbito | Experiência, navegação, ecrãs, comportamentos e prioridades da UI do Portal |
| Referências visuais | Protótipo Completo, PHC Knowledge Explorer e PHC Knowledge Mockups |
| Implementação | Não iniciada; este documento não representa funcionalidades já disponíveis |

## 1. Objetivo

Definir uma experiência única e coerente para o Portal PHC Knowledge, aproveitando os três protótipos analisados sem os combinar indiscriminadamente.

O Portal deve permitir:

1. **Descobrir** conhecimento técnico PHC através de pesquisa e exploração.
2. **Compreender** objetos, código, relações, contexto e proveniência.
3. **Reutilizar** conhecimento e soluções técnicas documentadas.
4. **Governar** a entrada de capturas e a administração da plataforma.

O produto é uma plataforma de conhecimento técnico reutilizável, contextualizado por instalação e sustentado em evidências verificáveis. Não é um clone do PHC, um explorador SQL genérico, um repositório indiferenciado de ficheiros nem um chatbot com documentação solta.

## 2. Decisão de consolidação dos protótipos

Os três protótipos têm papéis diferentes:

| Referência | Decisão |
|---|---|
| Protótipo Completo | Base para a estrutura global, navegação e áreas funcionais |
| PHC Knowledge Explorer | Referência para a experiência de pesquisa, exploração e detalhe contextual dos objetos |
| PHC Knowledge Mockups | Referência para design system, componentes, estados de interface, timeline, mini-grafo e command palette |

Não criar um quarto conceito visual independente. A implementação deve resultar desta consolidação.

### 2.1 Identidade visual

- Direção visual: **Graphite + Aço + Titânio**, com caráter técnico, sóbrio e profissional.
- Tema escuro como referência visual principal; tema claro disponível como alternativa.
- Tipografia: Inter para interface e JetBrains Mono para código, identificadores e conteúdo técnico.
- Cores semânticas para estados, tipos de evidência e níveis de confiança. A cor nunca deve ser o único meio de comunicar um estado.
- Contraste, legibilidade e densidade de informação têm prioridade sobre efeitos decorativos.
- Componentes reutilizáveis e tokens semânticos; evitar estilos isolados por ecrã.

Os valores concretos de cor, espaçamento, tipografia e elevação devem ser fechados no design system da implementação, usando os protótipos como referência visual e não como fonte automática de CSS.

## 3. Navegação principal

A navegação lateral deve ser estável e curta, com seis áreas:

1. **Início**
2. **Pesquisa e Exploração**
3. **Conhecimento PHC**
4. **Soluções Reutilizáveis**
5. **Clientes e Instalações**
6. **Capturas e Administração**

Pesquisa global, command palette, favoritos, histórico recente e preferências são capacidades transversais; não devem criar secções principais adicionais.

### 3.1 Início

Objetivo: permitir retomar trabalho e chegar rapidamente ao conhecimento.

Conteúdo:
- pesquisa global em destaque;
- objetos e pesquisas recentes;
- favoritos;
- soluções reutilizáveis recentes;
- atividade recente relevante;
- estado operacional/importações quando aplicável e autorizado.

Não transformar o início num dashboard de métricas sem utilidade operacional. Só apresentar indicadores com dados reais, significado claro e ação associada.

### 3.2 Pesquisa e Exploração

Objetivo: encontrar conhecimento por nome, identificador, texto ou fragmento de código.

A pesquisa deve evoluir para abranger:
- tabelas e campos;
- objetos e artefactos PHC;
- eventos, regras, funções, formulários e outros conteúdos recolhidos;
- referências e utilizações dentro do código;
- soluções reutilizáveis e conhecimento documentado.

A pesquisa inicial deve ser determinística e baseada no índice efetivamente disponível. Não apresentar pesquisa semântica ou resultados de IA como existentes antes de serem implementados.

Resultados:
- tipo de resultado;
- nome e contexto;
- excerto relevante, quando aplicável;
- origem/proveniência disponível;
- instalação/ambiente, quando aplicável e permitido;
- indicação de correspondência e navegação para o detalhe.

Filtros devem ser progressivos e depender dos dados disponíveis: tipo, origem, organização/instalação, captura e estado. Não apresentar filtros que não tenham suporte no backend.

### 3.3 Conhecimento PHC

Objetivo: consultar o conhecimento estruturado e a evidência associada.

A área deve permitir explorar o catálogo standard e o conhecimento observado nas instalações, mantendo clara a distinção entre ambos.

O detalhe contextual de um objeto é o ecrã central da experiência. Deve permitir navegar para objetos relacionados, código que o utiliza, evidências, captura e contexto organizacional quando existirem dados materializados.

### 3.4 Soluções Reutilizáveis

Objetivo: transformar experiência de implementação em conhecimento reaproveitável.

Modelo de apresentação de uma solução:
- problema e contexto funcional;
- objetivo e comportamento esperado;
- abordagem/solução técnica;
- artefactos e código associados;
- dependências e objetos PHC relacionados;
- limitações, versões e pré-condições;
- validação, evidências e histórico;
- estado de maturidade/validação.

Esta área deve distinguir claramente conteúdo em rascunho, conteúdo revisto e conteúdo validado. A publicação de conhecimento geral requer curadoria humana; não é automática por semelhança ou frequência de ocorrência.

### 3.5 Clientes e Instalações

Objetivo: contextualizar o conhecimento observado.

A experiência deve refletir a hierarquia conceptual:
- Organização/Cliente;
- Instalação;
- Ambiente;
- localização técnica da base de dados, com histórico quando aplicável;
- capturas associadas.

Os identificadores são estáveis; nomes e localizações podem ser corrigidos com histórico e auditoria. Não confundir organização, instalação, ambiente, base de dados, captura e pacote.

**Limite de implementação:** a gestão completa de Customers, Installations, Environments e associação/correção do contexto ainda não está implementada. A UI deve ser preparada para o modelo de domínio aprovado, mas não simular persistência nem assumir que esses registos já existem.

### 3.6 Capturas e Administração

A área deve reunir duas responsabilidades distinguíveis na navegação interna:

**Capturas e importações**
- lista e detalhe de pacotes recebidos;
- estado de receção e validação;
- análise prévia do conteúdo;
- apresentação de avisos, erros e cobertura;
- confirmação explícita antes da importação/materialização;
- associação ou correção de contexto, quando o fluxo estiver disponível;
- histórico de tentativas e resultados.

**Administração**
- gestão de utilizadores e roles existentes;
- configurações e operações administrativas autorizadas;
- auditoria e estado operacional quando disponíveis.

A experiência não deve confundir receção/validação/arquivo do ZIP com materialização de conhecimento, associação de contexto ou publicação. São etapas distintas.

## 4. Ecrã central — detalhe de objeto

O detalhe de objeto deve oferecer uma visão contextual, sem obrigar o utilizador a navegar por páginas desconexas.

Cabeçalho:
- nome/identificador e tipo;
- descrição/contexto, quando conhecido;
- classificação: PHC Standard, observado ou personalização;
- origem e contexto disponíveis;
- ações: copiar identificador, favorito e navegação contextual.

Separadores propostos:

| Separador | Responsabilidade |
|---|---|
| Resumo | Identificação, descrição, contexto e indicadores suportados por dados reais |
| Definição | Definição original e metadados |
| Campos | Estrutura e metadados dos campos, quando aplicável |
| Código e utilizações | Artefactos e referências que utilizam ou mencionam o objeto |
| Relações | Relações estruturais, factos declarados, interpretações e outras dependências claramente tipificadas |
| Proveniência | Captura, pacote, artefacto de origem e evidências |
| Histórico | Evolução e comparação, apenas quando a capacidade estiver implementada |

Os separadores podem variar por tipo de objeto. Não mostrar separadores vazios ou sem significado para aquele tipo.

### 4.1 Regras de evidência e proveniência

Toda a informação deve preservar a natureza da sua origem:

- **Facto recolhido:** conteúdo original observado numa captura.
- **Interpretação automática:** resultado determinístico de parser/resolução, com versão, estado, diagnósticos e confiança quando aplicável.
- **Projeção consultável:** informação derivada e regenerável; nunca substitui o facto original.
- **Anotação humana:** comentário, validação ou associação introduzida por utilizador.
- **Conhecimento curado:** conteúdo promovido após revisão humana.

Relações declaradas, relações SQL/FK, interpretações de expressões, referências textuais e dependências funcionais não podem ser agregadas num tipo genérico sem distinção.

A interface deve apresentar, quando disponíveis:
- tipo de evidência;
- origem e localização;
- captura/pacote;
- método de deteção ou interpretação;
- estado/confiança/diagnósticos;
- ligação ao conteúdo original.

Não inventar confiança, origem ou significado que os dados não suportem.

## 5. Fluxo de importação

O fluxo visual pretendido é:

1. **Receber pacote** — upload do ZIP IKP e criação da tentativa de receção.
2. **Validar** — verificar estrutura, schemas, checksums e referências cruzadas.
3. **Analisar** — apresentar conteúdo, capabilities, coverage, artefactos, avisos e problemas.
4. **Confirmar** — exigir ação explícita do utilizador antes de qualquer materialização/publicação.
5. **Associar contexto** — selecionar/corrigir organização, instalação e ambiente quando aplicável.
6. **Processar** — materializar os dados suportados, mantendo ligação ao pacote original.
7. **Consultar resultado** — apresentar resumo, itens processados, falhas e proveniência.

O estado atual do backend implementa receção, validação, arquivo imutável, receipts/tentativas e API IKP, além de autenticação e administração de utilizadores. A análise prévia/staging, confirmação explícita, gestão completa de contexto e materialização do domínio ainda não estão concluídas.

A UI deve refletir exatamente o estado real de cada etapa. Um pacote arquivado não deve ser apresentado como conhecimento já importado e pesquisável.

## 6. Componentes e comportamentos transversais

Implementar componentes partilhados para:
- navegação lateral e cabeçalho;
- pesquisa global e resultados;
- breadcrumbs e navegação contextual;
- tabs e filtros;
- tabelas e listas técnicas;
- visualização de código com realce e cópia;
- badges semânticos de tipo/estado;
- cartões de evidência e proveniência;
- mini-grafo de relações;
- timeline de histórico;
- command palette;
- notificações e confirmação de ações;
- formulários e diálogos;
- paginação/virtualização quando necessária.

### 6.1 Estados obrigatórios

Todos os ecrãs e componentes com dados devem tratar explicitamente:
- carregamento;
- vazio (sem dados ainda);
- sem resultados;
- erro recuperável;
- acesso não autorizado;
- dados incompletos ou indisponíveis;
- sucesso/confirmação;
- operação em curso.

Estados vazios devem explicar o que falta e, quando possível, indicar a próxima ação. Não usar dados fictícios para preencher ecrãs vazios em produção.

### 6.2 Acessibilidade e utilização

- navegação por teclado e foco visível;
- contraste adequado nos dois temas;
- controlos com rótulos acessíveis;
- informação não comunicada apenas por cor;
- comportamento responsivo, preservando a legibilidade de tabelas e código;
- feedback claro para ações demoradas e operações irreversíveis.

## 7. Primeira entrega visual e funcional

A primeira entrega funcional deve concentrar-se nos percursos suportados pelo backend, sem antecipar o domínio ainda não implementado.

Ordem de implementação:

1. **Shell do Portal:** estrutura React, integração com o host ASP.NET Core, layout, navegação, tema e componentes base.
2. **Autenticação:** login, sessão por cookie, logout, antiforgery e proteção de páginas.
3. **Administração:** gestão real de utilizadores e roles, ligada aos contratos existentes e protegida pela policy Administrator.
4. **Capturas e importações:** só depois de existir uma API de Portal para listar e consultar recibos/tentativas no contexto da sessão autenticada. A receção iniciada pelo Portal também exige uma operação própria do Portal.
5. **Início:** ecrã simples, sem indicadores, atividade ou registos fictícios. Só apresentar dados reais e úteis quando as respetivas fontes existirem.

Pesquisa global, catálogo de conhecimento e detalhe de objetos não fazem parte do primeiro incremento funcional. Podem permanecer no protótipo como representação da experiência futura, sempre identificados como demonstração.

### 7.1 Contratos de backend confirmados

Os seguintes contratos foram confirmados no código atual do backend e constituem a base de integração inicial.

**Autenticação do Portal**
- `GET /api/portal/auth/antiforgery`
- `GET /api/portal/session`
- `POST /api/portal/auth/login`
- `POST /api/portal/auth/logout`

A sessão do Portal utiliza autenticação por cookie. As operações de escrita protegidas devem enviar o token antiforgery segundo o contrato do host.

**Administração de utilizadores**
- `GET /api/portal/admin/users?q=&isActive=&role=&page=&pageSize=`
- `GET /api/portal/admin/users/{userId}`
- `POST /api/portal/admin/users`
- `PUT /api/portal/admin/users/{userId}/role`
- `POST /api/portal/admin/users/{userId}/activate`
- `POST /api/portal/admin/users/{userId}/deactivate`

Todos estes endpoints exigem a policy `Administrator`. As operações de escrita têm proteção antiforgery.

O DTO de listagem disponibiliza `UserId`, `Email`, `UserName`, `DisplayName`, `IsActive`, `Roles` e `CreatedAtUtc`. A resposta inclui `Items`, `Page`, `PageSize` e `TotalCount`. A criação recebe `Email`, `DisplayName`, `Role` e `Password`.

Não assumir que existem campos ou operações de último acesso, convite, associação de organização ou recuperação de palavra-passe enquanto não fizerem parte dos contratos.

**Superfície pública de integração IKP**
- `POST /api/v1/ikp/packages` — receção de pacote, protegida pela policy IKP de upload.
- `GET /api/v1/ikp/attempts/{attemptId}` — consulta de tentativa, protegida pela policy IKP de leitura.
- `GET /api/v1/ikp/packages/{packageId}` — consulta de pacote, protegida pela policy IKP de leitura.

A superfície IKP usa JWT Bearer e scopes próprios; não usa a sessão por cookie do Portal. Não existe atualmente um endpoint de listagem de pacotes/recibos.

**Regra de integração:** o browser do Portal não deve gerir tokens JWT IKP nem chamar estes endpoints como se a sessão por cookie lhe desse acesso. Para a área Capturas, deverá ser criada uma API própria do Portal, protegida por autenticação/autorização do Portal e antiforgery nas operações de escrita, que invoque os serviços/repositórios internos existentes. A listagem de recibos/tentativas e a consulta de detalhe devem ser adicionadas sem alterar o contrato público IKP nem duplicar a lógica de receção, validação e arquivo.

O upload iniciado pelo Portal também depende de uma operação autenticada do Portal que invoque internamente o fluxo de ingestão. Até existir, a UI não deve apresentar a receção como ação funcional.

### 7.2 Limites da primeira entrega

Ficam fora do primeiro incremento:
- pesquisa global e indexação do conhecimento;
- catálogo, detalhe e relações de objetos PHC;
- consulta de código e proveniência;
- staging, análise prévia, confirmação e materialização;
- gestão completa de clientes, instalações e ambientes;
- soluções reutilizáveis, curadoria e publicação;
- comparação entre capturas;
- pesquisa semântica e RAG.

Estas áreas só devem passar a funcionalidades ativas quando existirem contratos e modelo de domínio que as suportem.

## 8. Evolução posterior

Depois da primeira entrega e da materialização do conhecimento:

- detalhe completo de objetos e campos;
- pesquisa transversal em artefactos e código;
- proveniência e navegação por evidência;
- relações e exploração contextual;
- gestão de clientes, instalações e ambientes;
- soluções reutilizáveis e curadoria;
- histórico e comparação entre capturas;
- pesquisa semântica e RAG fundamentado em evidências.

RAG é um consumidor futuro do conhecimento estruturado. Não antecipar nesta fase escolhas de LLM, embeddings ou base vetorial.

## 9. Fronteiras técnicas e regras de implementação

Esta especificação não altera os contratos nem as decisões técnicas existentes.

- A plataforma é centralizada.
- O PHC Knowledge recebe pacotes IKP; não executa o Scanner em runtime nem acede ao workspace SQLite.
- O ZIP é imutável apenas durante a recepção/processamento e é eliminado após materialização verificada. Não é arquivo permanente.
- A base central é Microsoft SQL Server; edição/versão por decidir. PostgreSQL está excluído; não assumir Express. SQLite permanece limitado ao workspace/cache local do Scanner.
- A UI deve consumir APIs do backend e respeitar autenticação, autorização, organização e isolamento.
- Não criar dados de demonstração como se fossem dados reais.
- Não criar migrations, entidades ou endpoints apenas para fazer coincidir o protótipo com o ecrã.
- Não alterar o contrato IKP nem o modelo de domínio por decisão exclusivamente visual.
- Não implementar PostgreSQL, modo offline, peer sync, Discovery/Fusion ou Digital Twin.
- Não iniciar RAG antes de existir conhecimento estruturado, rastreável e pesquisável.

## 10. Critérios de aceitação da especificação

A direção visual/funcional está pronta para orientar implementação quando:
- a navegação principal e as responsabilidades das áreas estiverem aceites;
- o design system tiver tokens e componentes comuns definidos;
- os ecrãs prioritários estiverem desenhados com estados reais;
- cada ação de UI estiver associada a uma capacidade/API existente ou explicitamente marcada como futura;
- a distinção entre pacote recebido, pacote validado, conhecimento materializado e contexto associado estiver visível;
- a proveniência e os tipos de evidência não forem colapsados;
- a implementação não contrariar os contratos IKP e o modelo de domínio 0.2.

## 11. Referências internas

- [Fronteiras de produto](product-boundaries.md)
- [Camadas de conhecimento e proveniência](knowledge-layers.md)
- [Modelo de domínio 0.2](knowledge-domain-model-0.2.md)
- [Roadmap visual](roadmap-visual-phc-knowledge-2026-11-06.md)
- [Relatório de fase — Backend Identity e Portal](phase-report-identity-portal-2026-11-18.md)
- [Especificação IKP 0.1](ikp-0.1-specification.md)

---

**Nota de estado:** este documento consolida a proposta de experiência do Portal a partir dos protótipos e das decisões documentadas no repositório. Não significa que os ecrãs ou funcionalidades descritos estejam implementados. As capacidades futuras dependem da evolução do backend e do modelo de domínio.
