# Relatório de fase — Backend Identity e Portal

| Campo | Valor |
|---|---|
| Data | 2026-11-18 |
| Âmbito | Identity, autenticação, sessões, autorização, bootstrap e administração de utilizadores |
| PR | [#15 — Identity and Portal backend](https://github.com/Jhouinho/PhcKnowledge/pull/15) |
| Branch | `feature/identity-portal-1.2-1.4.2e` |
| Commit da branch | `ad3b180355b7d98e942c35a00f4962f75a927bc2` |
| Merge commit | `6c619bfc6aa78ba1cd694c48f2d71c71c71aa4b8` |
| Estado | Integrado na `main` |

## 1. Resultado

A PR #15 integrou as fases 1.2 a 1.4.2.E e concluiu o backend Identity/Portal previsto para esta etapa.

Incluído:
- Organização canónica e persistência ASP.NET Identity.
- Utilizadores associados à organização canónica e roles `Administrator` / `Collaborator`.
- Autenticação PortalCookie; JWT Bearer opcional e separado para a API IKP.
- Login, logout, sessão, antiforgery e validação contínua de IsActive/SecurityStamp.
- Políticas de autorização para utilizador autenticado, colaborador e administrador.
- CLI `PhcKnowledge.Install` para scripts SQL, migrations Identity, organização, roles e bootstrap do primeiro administrador.
- API administrativa para listar, consultar, criar, alterar role e ativar/desativar utilizadores.
- Serialização das mutações administrativas e proteção do último administrador ativo da organização canónica.
- Testes de regressão e hardening E1–E5.

## 2. Validação registada

- Host Release e Install Release: builds reportados como aprovados na validação da branch.
- Import.Tests: 199 testes aprovados na validação local reportada.
- Workflow GitHub `.NET — PHC Knowledge` #253: sucesso.
- Workflow GitHub `SQL Server integration tests` #39: sucesso; 64 testes descobertos, 62 aprovados e 2 ignorados por dependerem do ZIP IKP real, não disponibilizado em CI.
- Correção dos 45 testes SQL condicionais: os testes que necessitam de SQL Server usam `SqlServerFact`, permitindo à suite geral correr sem uma base SQL configurada.
- PR #15 integrada na `main`; merge commit `6c619bfc6aa78ba1cd694c48f2d71c71c71aa4b8`.

Os resultados locais acima são os comunicados na execução da branch; os workflows identificados são a evidência CI observada no GitHub.

## 3. Limites — não incluídos nesta fase

- Não existe ainda UI React do Portal.
- Não existe fluxo de análise prévia do ZIP com staging e confirmação explícita da importação.
- Não existe ainda gestão de Customers, Installations e Environments nem associação/correção de contexto da captura.
- O backend Identity/Portal não materializa Observation, artefactos, REL/DTL ou identidade lógica do domínio 0.2.
- Não foi realizada configuração operacional final de Data Protection para produção.

## 4. Hardening identificado

1. **Data Protection:** definir armazenamento persistente e proteção das chaves, especialmente para reinícios, deployment e eventual execução em múltiplas instâncias.
2. **Bloqueio administrativo:** `UserActivationService` usa a existência de uma transação SQL como sinal de proteção. Uma transação aberta por outro chamador não demonstra, por si só, aquisição de `sp_getapplock`. Tornar verificável a posse do mecanismo de serialização e cobrir a chamada direta com teste SQL.

Estes pontos não alteram o resultado de integração da PR, mas devem ser fechados antes de considerar a instalação pronta para produção.

## 5. Reflexão e próximo passo

O projeto passou de um núcleo de ingestão sem fronteira de utilizador para um backend central com API IKP, identidade, autenticação e administração. A fase não entrega ainda uma experiência de Portal utilizável: falta desenhar com o responsável do produto os percursos, a informação e a navegação antes de iniciar React.

Próximo passo proposto:
1. Conversa de produto sobre a experiência do Portal e o fluxo upload → análise → confirmação → associação.
2. Plano curto de hardening de Data Protection e do bloqueio administrativo.
3. Só depois de validar a direção funcional, decompor a implementação do Portal e do fluxo de importação.

Não avançar automaticamente para UI, materialização ou RAG sem validação conjunta.
