# PHC Knowledge Portal

Frontend React + TypeScript + Vite do Portal PHC Knowledge.

## Requisitos
- Node.js 22.12.0 ou superior
- npm 10.9.0 ou superior

## Preparação e execução
```powershell
npm ci
npm run dev
```
Abrir http://127.0.0.1:5173. O proxy encaminha pedidos `/api` para `http://localhost:5000`; configurar `PHC_KNOWLEDGE_HOST_URL` em `.env.local` se necessário. Não guardar credenciais ou segredos nesse ficheiro.

## Verificações
```powershell
npm test
npm run typecheck
npm run build
```

## Funcionalidade presente nesta branch
- Shell do Portal e autenticação por sessão cookie.
- Cliente HTTP com tratamento de erros e antiforgery.
- Página de análise com confirmação/rejeição, estado efectivo e histórico append-only.
- A autorização e a elegibilidade são sempre aplicadas pelo backend; a UI não é uma fronteira de segurança.

A integração/deployment do frontend no Host e a experiência completa de upload continuam fora do âmbito desta branch.
