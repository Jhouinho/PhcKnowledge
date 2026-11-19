# PHC Knowledge — Revisão de prontidão do Portal para o frontend

| Campo | Valor |
|---|---|
| Data | 2026-11-19 |
| Repositório | `Jhouinho/PhcKnowledge` |
| Base analisada | `main`, estado consultado em 2026-11-19 |
| Objetivo | Consolidar a revisão da especificação, protótipo e contratos HTTP antes do EXA-004 |
| Estado | Revisão documental concluída; alterações de backend não executadas |

## 1. Conclusão

A direção de produto e a estrutura de navegação estão suficientemente definidas para iniciar o EXA-004 — Frontend Architecture Spike. A implementação React deve, contudo, começar pelas capacidades já suportadas pelo backend e não pela totalidade dos ecrãs do protótipo.

A primeira entrega funcional deve abranger:
1. Shell do Portal, design system, navegação e temas.
2. Autenticação por cookie, sessão e proteção de páginas.
3. Administração de utilizadores, ligada aos endpoints existentes.
4. Área de Capturas, depois de existir uma API própria do Portal para consulta/listagem dos recibos e tentativas.
5. Início simples, sem indicadores ou atividade fictícios.

Pesquisa, catálogo/detalhe de conhecimento, soluções reutilizáveis e gestão completa de clientes/instalações permanecem fora do primeiro incremento funcional.

## 2. Capacidades confirmadas no backend

### 2.1 Autenticação do Portal

Contratos encontrados em `PortalApiEndpoints`:
- `GET /api/portal/auth/antiforgery`
- `GET /api/portal/session`
- `POST /api/portal/auth/login`
- `POST /api/portal/auth/logout`

A sessão é baseada no esquema de cookie do Portal. As operações de escrita protegidas usam antiforgery.

### 2.2 Administração de utilizadores

Contratos encontrados em `PortalAdminUserEndpoints`:
- `GET /api/portal/admin/users?q=&isActive=&role=&page=&pageSize=`
- `GET /api/portal/admin/users/{userId}`
- `POST /api/portal/admin/users`
- `PUT /api/portal/admin/users/{userId}/role`
- `POST /api/portal/admin/users/{userId}/activate`
- `POST /api/portal/admin/users/{userId}/deactivate`

Todos os endpoints de administração exigem a policy `Administrator`. As operações de escrita têm metadata de antiforgery.

O DTO de listagem disponibiliza:
- `UserId`
- `Email`
- `UserName`
- `DisplayName`
- `IsActive`
- `Roles`
- `CreatedAtUtc`

A resposta de listagem contém ainda `Items`, `Page`, `PageSize` e `TotalCount`.

A criação recebe `Email`, `DisplayName`, `Role` e `Password`. Não foi encontrado, nos contratos analisados, um fluxo de convite, recuperação de palavra-passe, último acesso ou associação de organização exposta no DTO. A UI não deve pressupor essas capacidades.

### 2.3 Receção IKP

Contratos encontrados em `IkpApiEndpoints`:
- `POST /api/v1/ikp/packages` — receção de ZIP, policy `ikp.upload`.
- `GET /api/v1/ikp/attempts/{attemptId}` — consulta de tentativa, policy `ikp.read`.
- `GET /api/v1/ikp/packages/{packageId}` — consulta de pacote, policy `ikp.read`.

A superfície IKP utiliza JWT Bearer e scopes próprios. Não é a sessão por cookie do Portal. Não foi encontrado endpoint de listagem de pacotes/recibos.

**Decisão de integração:** o browser do Portal não deve receber nem gerir tokens JWT IKP, nem chamar diretamente a superfície IKP como se fosse autenticada pela sessão do Portal. Para apresentar Capturas no Portal, deve ser criada uma API de Portal, protegida por cookie, autorização e antiforgery nas operações de escrita, que consulte os serviços/repositórios internos existentes. A listagem deve ser acrescentada sem alterar o contrato público IKP nem duplicar a lógica de receção/validação/arquivo.

O eventual upload iniciado pela UI do Portal também necessita de uma operação própria autenticada pelo Portal que invoque internamente o fluxo de ingestão. Até existir, o protótipo não deve apresentar um botão de upload funcional.

## 3. Diferenças encontradas no protótipo

| Área | Situação observada | Consolidação |
|---|---|---|
| Navegação | O ecrã Administração existia, mas não tinha entrada visível na navegação. | Adicionar Administração como subárea de Capturas e Administração. |
| Utilizadores | A tabela apresentava organização, último acesso e estado de convite. | Usar apenas DisplayName, Email, UserName, Roles, IsActive e CreatedAtUtc. |
| Filtros de utilizadores | O protótipo não refletia os filtros suportados pela API. | Alinhar pesquisa, role e estado com q, role e isActive. |
| Capturas | A lista parecia funcional, embora não exista endpoint de listagem no Portal. | Manter aviso de demonstração e assinalar a dependência de uma API de Portal. |
| Upload | O botão simulava uma receção que não era executada. | Não apresentar a simulação como operação; upload no Portal fica dependente de endpoint próprio. |
| Pesquisa | Resultados e filtros são fictícios; não foi identificada API de pesquisa global. | Manter como conceito visual, sem integração nesta primeira entrega. |
| Detalhe de objeto | O conteúdo e as relações são ilustrativos; o separador Campos é específico de certos tipos. | Manter como conceito; tabs condicionais por tipo quando o domínio estiver disponível. |
| Início | Indicadores e atividade são fictícios. | Não transportar esses valores para a aplicação real. Início inicial simples, com dados reais quando existirem. |
| Soluções / Clientes | Áreas conceptuais sem backend de domínio completo. | Não implementar persistência nem simular operações reais. |

## 4. Escopo proposto para o EXA-004

### Incluído no spike

- Estrutura React + TypeScript e integração com o host ASP.NET Core, de acordo com a organização real da solução.
- Shell responsivo: sidebar, header, breadcrumbs, navegação e tema.
- Tokens visuais Graphite + Aço + Titânio; tema escuro de referência e tema claro alternativo.
- Componentes base: botões, inputs, selects, tabelas, badges, alertas, estados vazios, loading e erro.
- Cliente HTTP centralizado, tratamento de erros e convenção para antiforgery.
- Estado de sessão e proteção das rotas.
- Página de login e percurso de sessão.
- Administração de utilizadores: listagem, pesquisa/filtros/paginação, detalhe, criação, alteração de role e ativação/desativação, respeitando as policies.
- Página inicial mínima, sem dados inventados.

### Dependente de trabalho backend antes de ficar funcional

- Listagem de pacotes/recibos e tentativas para o Portal.
- Consulta de detalhe de captura através da sessão do Portal.
- Eventual receção/upload iniciado no Portal.
- Contratos de leitura necessários à apresentação de estado e histórico.

### Fora do primeiro incremento

- Pesquisa global e indexação do conhecimento.
- Catálogo e detalhe de objetos PHC.
- Referências de código, relações e proveniência consultáveis.
- Materialização, staging, confirmação de importação e comparação entre capturas.
- Gestão completa de clientes, instalações e ambientes.
- Soluções reutilizáveis, curadoria e publicação.
- Pesquisa semântica e RAG.

## 5. Regras de implementação

1. Não criar endpoints, DTOs ou entidades apenas para fazer coincidir a UI com o protótipo.
2. Não alterar o contrato IKP para acomodar a sessão do Portal.
3. Não expor JWT IKP ao browser nem misturar os esquemas de autenticação.
4. Respeitar autorização `Administrator` na gestão de utilizadores e antiforgery nas operações de escrita.
5. Mostrar apenas campos que os contratos fornecem; não inventar organização, último acesso, convites ou permissões adicionais.
6. Não apresentar um pacote recebido/arquivado como conhecimento já materializado e pesquisável.
7. Manter dados demonstrativos exclusivamente no protótipo, sempre identificados como fictícios.
8. Não iniciar a implementação de pesquisa, domínio de conhecimento ou RAG neste spike.

## 6. Critérios de saída do EXA-004

- A aplicação React arranca integrada com o host previsto, sem criar um segundo backend.
- O tema e a navegação mantêm a direção visual aprovada.
- Login, sessão e logout funcionam com o esquema de cookie existente.
- Rotas e ações administrativas respeitam autenticação, role e antiforgery.
- A administração usa os DTOs e filtros reais, incluindo estados loading, vazio, erro e sem autorização.
- A integração IKP permanece isolada da sessão do browser.
- As dependências backend para Capturas estão documentadas e não são simuladas.
- Build e testes relevantes passam; não são introduzidas regressões no backend.

## 7. Ficheiros de referência

- `docs/architecture/portal-experience-specification-2026-11-19.md`
- `docs/prototypes/portal/index.html`
- `src/PhcKnowledge.Host/Http/PortalApiEndpoints.cs`
- `src/PhcKnowledge.Host/Http/PortalAuthDtos.cs`
- `src/PhcKnowledge.Host/Http/PortalAdminUserEndpoints.cs`
- `src/PhcKnowledge.Host/Http/PortalAdminUserDtos.cs`
- `src/PhcKnowledge.Host/Http/IkpApiEndpoints.cs`

---

Esta revisão é documental. Não foram alterados contratos, modelo de domínio, endpoints ou comportamento do backend.
