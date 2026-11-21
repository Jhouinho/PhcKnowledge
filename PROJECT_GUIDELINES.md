# PHC Knowledge — Orientações para desenvolvimento

Documento de entrada para qualquer ferramenta de desenvolvimento. A baseline, decisões e estado canónicos estão separados para evitar duplicação.

## Leitura obrigatória
1. docs/governance/README.md.
2. PROJECT_BASELINE.md.
3. DECISIONS.md.
4. PROJECT_STATUS.md.
5. docs/governance/current-handoff.md.
6. Código, testes e documentos técnicos relevantes na branch/HEAD efetivos.

## Regras essenciais
- Produto interno da equipa; SQL Server é a base central. PostgreSQL não faz parte da arquitetura. A edição/versão SQL Server não está fixada.
- SQLite é apenas workspace/cache local do Scanner.
- Scanner autónomo; sem dependência de runtime do Knowledge.
- ZIP IKP é transporte temporário e só pode ser eliminado depois de materialização e confirmação de persistência íntegra.
- PHC Standard curado é distinto de observações e personalizações de clientes.
- RAG é objetivo futuro, sem tecnologia antecipada.
- Confirmar o código antes de afirmar que algo está implementado.
- Distinguir proposta, decisão aprovada, implementação, validação e pendência.
- Não inventar resultados de testes, cobertura, dados ou decisões.
- Preservar documentação histórica; não apagar sem inventário e autorização.
- Não criar documentação redundante: atualizar primeiro a fonte canónica.
- Não alterar stack, contratos ou âmbito contra decisões aprovadas.
- Documentação e UI em pt-PT.

## Testes e validação
Separar sempre: (1) testes sintéticos/unitários; (2) integração com serviços reais, por exemplo SQL Server; (3) validação com captura real PHC; (4) execução CI, com commit, workflow e resultado. Testes de receipts SQL não provam recolha dos collectors Scanner. Fixtures sintéticas não são capturas reais.

## Fecho de sessão
Atualizar docs/governance/current-handoff.md com branch/HEAD, objetivo, ficheiros alterados, decisões, testes executados e não executados, riscos e próximo passo. Se a publicação não estiver autorizada, indicar o que ficou local. Só afirmar que o GitHub está atualizado depois de verificar o commit remoto.
