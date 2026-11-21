> **Nota de estado — 2026-11-20:** este spike foi escrito antes da criação do frontend. O projeto React + TypeScript/Vite já existe em `src/PhcKnowledge.Portal`; consultar `PROJECT_STATUS.md` e o código da branch para o estado atual. Preservar as decisões de integração e organização que continuem compatíveis, mas não reutilizar as afirmações de inexistência do frontend como estado atual.

# PHC Knowledge — EXA-004: arquitetura do frontend do Portal

| Campo | Valor |
|---|---|
| Data | 2026-11-19 |
| Estado | Decisão técnica proposta para implementação |
| Base analisada | `main`, estrutura e contratos consultados em 2026-11-19 |
| Âmbito | Integração React + TypeScript no host ASP.NET Core existente |

## 1. Decisão

O Portal será uma aplicação **React + TypeScript**, desenvolvida com **Vite**, integrada no projeto existente `PhcKnowledge.Host`.

O `PhcKnowledge.Host` continua a ser o único backend e o único ponto de entrada da aplicação em produção. O frontend não terá API própria, servidor Node em produção, nem ligação direta à base de dados.

A aplicação React será criada em:

```text
src/
├── PhcKnowledge.Host/
│   └── wwwroot/                 # artefactos estáticos gerados pelo build
└── PhcKnowledge.Portal/
    ├── package.json
    ├── package-lock.json
    ├── index.html
    ├── vite.config.ts
    └── src/
        ├── app/
        ├── components/
        ├── features/
        ├── lib/
        ├── styles/
        └── main.tsx
```

O projeto Node não será adicionado à solução .NET como projeto compilável. Mantém-se separado do ciclo de restore/build .NET, embora versionado no mesmo repositório.

## 2. Evidência da estrutura atual

A revisão da branch `main` confirmou:

- `PhcKnowledge.sln` contém o projeto web `src/PhcKnowledge.Host`, direcionado a `.NET 10`.
- `src/PhcKnowledge.Host/PhcKnowledge.Host.csproj` usa `Microsoft.NET.Sdk.Web`; não contém integração de frontend nem dependências Node.
- `Program.cs` regista serviços, autenticação, middleware de autenticação/autorização/antiforgery e mapeia as APIs IKP e Portal. Ainda não configura serving de ficheiros estáticos nem fallback de SPA.
- Não existe atualmente `package.json`, configuração Vite, projeto React ou frontend compilado.
- O `.gitignore` já exclui `node_modules/`, `dist/` e outputs de build.

A solução tem, portanto, um host adequado para servir o frontend, mas essa capacidade ainda terá de ser acrescentada explicitamente.

## 3. Desenvolvimento e produção

### Desenvolvimento local

- Vite serve a aplicação React e disponibiliza hot reload.
- O proxy de desenvolvimento encaminha os pedidos `/api/**` para a instância local de `PhcKnowledge.Host`.
- O frontend usa sempre caminhos relativos, por exemplo `/api/portal/session`; não guarda URLs de backend específicas por ambiente.
- O browser comunica com o Vite durante o desenvolvimento. O proxy encaminha os pedidos API para o Host, evitando configurar CORS para este cenário.

A origem e porta do Host devem ser configuradas localmente no Vite, sem credenciais ou segredos no repositório.

### Produção

- `npm run build` gera os ficheiros estáticos diretamente em `src/PhcKnowledge.Host/wwwroot`.
- O Host serve esses ficheiros com o middleware estático do ASP.NET Core.
- As rotas do frontend recebem o `index.html` através de fallback de SPA.
- O fallback deve excluir explicitamente caminhos de API e health checks: um endpoint API inexistente deve continuar a devolver 404 e nunca receber HTML da aplicação.
- A publicação final contém um único processo/host ASP.NET Core e uma única origem para UI e API.

O build Node deve ser executado explicitamente no pipeline de frontend/publicação antes de publicar o Host. Não se deve tornar o `dotnet build` dependente de Node/npm, nem executar instalação de pacotes implicitamente em todos os builds e testes .NET.

## 4. Organização da aplicação

A estrutura inicial deve ser funcional e evolutiva, sem antecipar áreas de produto ainda sem backend:

- `app/`: composição da aplicação, router, providers e layout autenticado.
- `components/`: componentes transversais e reutilizáveis.
- `features/auth/`: login, sessão e logout.
- `features/admin-users/`: administração de utilizadores.
- `features/home/`: início simples.
- `lib/api/`: cliente HTTP, erros, antiforgery e contratos partilhados.
- `styles/`: tokens e estilos globais.

A primeira entrega não implementa páginas de pesquisa, catálogo de conhecimento, soluções, clientes/instalações ou capturas funcionais. Essas áreas não devem ser preenchidas com dados fictícios.

### Bibliotecas e apresentação

- React + TypeScript + Vite para a base da aplicação.
- React Router para navegação e rotas protegidas.
- TanStack Query para estado remoto, cache, loading, erros e invalidação após operações administrativas.
- CSS com custom properties/tokens semânticos e CSS Modules para estilos locais; evitar introduzir um framework visual pesado antes de validar o design system.
- Lucide React para ícones, mantendo ícones decorativos fora de controlos sem rótulo acessível.

As versões concretas ficam fixadas pelo `package-lock.json`. A direção visual mantém Graphite + Aço + Titânio, tema escuro de referência e tema claro alternativo.

## 5. Integração HTTP e segurança

### 5.1 Cliente HTTP

Criar um único cliente baseado em `fetch` que:

- use URLs relativas à origem atual;
- envie cookies de sessão (`credentials: "same-origin"`);
- trate respostas JSON, respostas sem conteúdo e erros HTTP;
- exponha erros tipados para a UI;
- acrescente `X-CSRF-TOKEN` apenas nas operações que exigem antiforgery;
- não registe passwords, cookies, tokens ou conteúdo sensível.

A UI não decide autorização com base apenas no estado visual. O backend continua a ser a autoridade para autenticação e autorização.

### 5.2 Ciclo de sessão

Contratos existentes:

- `GET /api/portal/auth/antiforgery`
- `GET /api/portal/session`
- `POST /api/portal/auth/login`
- `POST /api/portal/auth/logout`

Fluxo inicial:

1. Pedir o token antiforgery.
2. Consultar a sessão.
3. Se existir sessão autenticada, construir o contexto do utilizador e respetivas roles; caso contrário, apresentar login.
4. Enviar login com o cabeçalho `X-CSRF-TOKEN`.
5. Depois de login bem-sucedido, voltar a pedir o token antiforgery, agora associado ao principal autenticado.
6. Em logout, enviar o token atual e limpar o estado local após resposta bem-sucedida.
7. Em respostas 401, invalidar o estado de sessão e encaminhar para login. Em 403, apresentar falta de autorização sem entrar num ciclo de redirecionamento.

O endpoint de sessão devolve `Authenticated`, `UserId`, `Email`, `DisplayName` e `Roles`.

### 5.3 Administração

A área de administração usa exclusivamente os contratos existentes:

- `GET /api/portal/admin/users?q=&isActive=&role=&page=&pageSize=`
- `GET /api/portal/admin/users/{userId}`
- `POST /api/portal/admin/users`
- `PUT /api/portal/admin/users/{userId}/role`
- `POST /api/portal/admin/users/{userId}/activate`
- `POST /api/portal/admin/users/{userId}/deactivate`

A API exige a policy `Administrator`; as operações de escrita exigem antiforgery. A UI deve refletir loading, vazio, erro, 401 e 403. Os campos apresentados limitam-se aos DTOs atuais: `UserId`, `Email`, `UserName`, `DisplayName`, `IsActive`, `Roles` e `CreatedAtUtc`.

### 5.4 Separação Portal / IKP

A autenticação Portal usa o esquema `PortalCookie`; a superfície IKP usa JWT Bearer e scopes próprios.

O frontend não deve:
- obter, guardar ou renovar JWT IKP;
- chamar a API IKP como se o cookie do Portal a autorizasse;
- contornar a policy de administrador através de controlos de UI.

A integração futura de Capturas no Portal depende de endpoints próprios de Portal, protegidos por cookie e que invoquem internamente os serviços existentes. Esta necessidade não altera o contrato público IKP.

## 6. Alterações necessárias no Host

A integração de produção exigirá:

1. servir os ficheiros de `wwwroot` com o middleware estático;
2. mapear fallback para as rotas da SPA, sem capturar caminhos `/api` ou `/health`;
3. manter os endpoints API e health com os seus comportamentos atuais;
4. garantir que o build de frontend ocorre antes do publish de produção;
5. manter os ficheiros gerados fora do controlo de versões.

A ordem concreta do pipeline será implementada e testada no incremento de integração, não alterando já o comportamento do Host neste documento.

## 7. Plano de implementação

### Incremento A — base executável

- Criar o projeto React + TypeScript + Vite.
- Fixar versões e incluir lockfile.
- Criar tokens de tema e shell responsivo.
- Configurar proxy Vite para o Host local.
- Confirmar build e arranque isolado do frontend.

### Incremento B — integração com o Host

- Configurar output para `Host/wwwroot`.
- Adicionar serving estático e fallback de SPA com exclusão de API/health.
- Validar rotas frontend e 404 API.
- Confirmar que o Host continua a arrancar sem frontend compilado em ambiente de testes, quando aplicável.

### Incremento C — autenticação

- Implementar cliente HTTP e antiforgery.
- Implementar sessão, login, logout e rotas protegidas.
- Testar cookie, token antiforgery, 401 e 403.

### Incremento D — administração

- Implementar listagem, filtros, paginação, detalhe e operações suportadas.
- Ligar à API existente e respeitar role e antiforgery.
- Tratar todos os estados de interface.

### Incremento E — validação e publicação

- Adicionar build do frontend ao pipeline de CI/publicação.
- Executar build .NET e testes existentes.
- Executar build frontend e testes de integração da UI.
- Validar o percurso completo no browser com Host e SQL Server de teste.

## 8. Critérios de saída

- O frontend React é servido pelo Host em produção, sem servidor Node separado.
- O desenvolvimento usa Vite e proxy, sem CORS aberto no Host.
- O fallback da SPA nunca transforma erros API/health em HTML.
- Sessão, login e logout funcionam com cookie e antiforgery existentes.
- Administração usa contratos reais e autorização do backend.
- JWT IKP permanece fora do browser.
- Os builds .NET não dependem implicitamente de Node/npm.
- Build frontend, build .NET e testes relevantes passam.

## 9. Limites desta decisão

Este documento fecha a arquitetura de integração proposta. Não cria ainda o projeto React, não altera `Program.cs`, não modifica endpoints nem introduz novas capacidades de domínio. A implementação deve seguir os incrementos acima e validar cada integração antes de avançar.
