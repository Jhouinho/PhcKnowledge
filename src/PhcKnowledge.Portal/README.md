# PHC Knowledge Portal

Frontend React + TypeScript + Vite do Portal PHC Knowledge.

## Requisitos

- Node.js 22.12.0 ou superior
- npm 10.9.0 ou superior

## Preparação

A partir desta pasta:

```powershell
npm install
```

O primeiro `npm install` gera o `package-lock.json`. O lockfile deve ser incluído no commit seguinte para que as instalações futuras possam usar `npm ci`.

## Desenvolvimento

Com o Host ASP.NET Core em execução, iniciar o Vite:

```powershell
npm run dev
```

Abrir http://127.0.0.1:5173.

Por omissão, o proxy encaminha pedidos `/api` para `http://localhost:5000`. Se o Host estiver noutra origem, criar um ficheiro `.env.local` com:

```dotenv
PHC_KNOWLEDGE_HOST_URL=https://localhost:7001
```

O valor deve corresponder ao URL local efetivo do Host. Não incluir credenciais ou segredos neste ficheiro.

## Verificações

```powershell
npm run typecheck
npm run build
```

## Âmbito desta fase

O Incremento A estabelece o projeto, a configuração de desenvolvimento, os tokens visuais e o shell responsivo. Não implementa autenticação, administração, cliente HTTP de sessão, serving pelo ASP.NET Core ou integração de produção. Esses trabalhos pertencem aos incrementos seguintes.
