# PHC Knowledge — Handoff atual

> Este ficheiro é um registo operacional de continuidade, não substitui a baseline nem o registo de decisões.

## Estado da referência consultada

- **Data de atualização:** 2026-11-20
- **Repositório:** `Jhouinho/PhcKnowledge`
- **Branch consultada:** `feature/increment-c-package-import-analysis-confirmation`
- **HEAD consultado:** `e2d896ff67f7ef0df903c8a13940da9b826a764d`
- **Commit:** `feat(ikp): add Increment C analysis confirmation API and Portal UI`
- **Estado local de Cursor:** desconhecido; este handoff só descreve o estado remoto consultado.

## Objetivo em curso

Consolidar o contexto partilhado entre ChatGPT e Cursor e, depois, auditar a documentação do PHC Knowledge para remover contradições e identificar material histórico.

## Decisões de trabalho confirmadas nesta conversa

- GitHub é a fonte partilhada para decisões e estado versionado do projeto.
- ChatGPT e Cursor devem consultar o repositório antes de continuar trabalho.
- Cada transição deve registar alterações, decisões, testes, pendências e trabalho local não publicado.
- O ZIP IKP é apenas transporte: após importação/materialização bem-sucedida no Portal, os dados necessários devem ficar persistidos no SQL Server e o ZIP deixa de ser necessário como arquivo permanente.
- A plataforma é para uso interno da equipa do utilizador, para gerir o conhecimento dos clientes acompanhados; não é uma plataforma comercial multiempresa.
- A base de dados central escolhida é SQL Server, não PostgreSQL.
- A implementação funcional de importação/materialização e a política exata de eliminação do ZIP devem ser reconciliadas com o código e os documentos durante a auditoria; não assumir que já estão implementadas.

## Trabalho realizado nesta sessão

- Consultada a branch remota e confirmado o HEAD acima.
- Verificada a árvore de ficheiros; não foram encontrados `AGENTS.md` nem regras Cursor existentes na árvore consultada.
- Identificadas referências documentais contraditórias que justificam auditoria, incluindo referências a PostgreSQL e à retenção permanente do ZIP.
- Criados o acordo de continuidade e a regra Cursor associados a este handoff.

## Validação

- Não foram executados builds nem testes; esta alteração é documental.
- Não foi inspecionado qualquer workspace local do Cursor. Alterações locais não publicadas permanecem desconhecidas.

## Próximo passo seguro

1. Rever este protocolo e confirmar se a estrutura serve o fluxo real de trabalho.
2. Na auditoria documental, identificar a baseline e o registo de decisões canónicos e atualizar as ligações deste acordo.
3. Auditar os documentos e código relativos à ingestão para alinhar a retenção temporária do ZIP com o objetivo aprovado.
4. Não retomar D1/D2 até a auditoria e a baseline estarem consolidadas.
