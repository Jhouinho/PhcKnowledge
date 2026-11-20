# PHC Knowledge — Estado do projeto

> Fotografia do estado versionado. Distinguir implementação, validação observada e trabalho pendente. Esta página não substitui a leitura do código.

- Revisão: 2026-11-20
- Repositório: Jhouinho/PhcKnowledge
- Branch consultada: feature/increment-c-package-import-analysis-confirmation
- HEAD consultado antes desta atualização documental: 9a9df92ff82a98eafa2a5c1ff5134d8226ea7ce5
- Estado local da máquina do utilizador/Cursor: não observado.

## 1. Arquitetura

A direção aprovada está em PROJECT_BASELINE.md e DECISIONS.md: ferramenta interna da equipa; SQL Server central (edição por decidir); SQLite apenas workspace/cache Scanner; Scanner autónomo; ZIP temporário; PHC Standard separado de observações de instalações; RAG futuro. PostgreSQL está excluído.

## 2. Implementado no código — evidência da auditoria

### Plataforma e Portal
- Backend ASP.NET Core e APIs HTTP de ingestão IKP.
- Identidade/autenticação, organização canónica e políticas de autorização.
- Portal backend com sessão por cookie, login/logout, antiforgery e administração de utilizadores.
- Projeto frontend React + TypeScript existe em src/PhcKnowledge.Portal; a existência do projeto não significa que todos os ecrãs/fluxos estejam concluídos.
- Análise do pacote e decisões humanas do fluxo Incremento C têm implementação nas partes verificadas; materialização integral do conhecimento não está concluída.

### Ingestão IKP
- Scanner emite IKP 0.1 com manifesto, capabilities, coverage, Object Index, checksums, artefactos referenciados e factos REL/DTL quando disponíveis.
- Knowledge possui validação/receção, receipts, tentativas, operações de recovery/cleanup e endpoints HTTP.
- A implementação atual mantém arquivo de pacotes e a análise pode reabrir o pacote arquivado. Isto diverge da decisão de ZIP temporário; não alterar sem desenhar e validar materialização, recuperação e eliminação.
- Persistência de receipts e estado de processamento não equivale à materialização integral das observações, definições, relações e artefactos.

### Scanner
- WPF Desktop + CLI, collectors estruturais SQL e PHC, workspace SQLite, análise REL/DTL e exportador IKP.
- Catálogo declara 23 operações: 7 SQL Server Structure, 14 PHC Customizations e 2 PHC Declared Relations.
- A matriz de auditoria encontrou testes de resolução, projeção, parsing, persistência e exportação sintética; não encontrou testes diretos que executem os collectors reais contra SQL Server e validem individualmente as 23 operações.
- Database Schema Snapshot é funcionalidade auxiliar separada do fluxo normal de captura.

### PHC Standard Catalog
- Existe src/PhcKnowledge.StandardCatalog, com parser de batches T-SQL, extração de definições CREATE TRIGGER e testes dedicados.
- docs/sources/triggers phc.sql está presente na branch e foi indicado pelo responsável como contendo triggers standard PHC.
- Não está demonstrada uma pipeline completa de ingestão, curadoria, versionamento, persistência e publicação do catálogo standard.

## 3. Validação e testes — evidência histórica

Resultados CI anteriormente observados no commit cbf3f11ff3d46d5e22e8e46cbd482ced9730fe42:
- PhcKnowledgeScanner.Tests: 268 aprovados.
- PhcKnowledge.Import.Tests: 32 aprovados.
- PhcKnowledge.Ikp.Integration.Tests: 3 aprovados.
- PhcKnowledge.Import.SqlServer.Integration.Tests: 76 aprovados contra SQL Server 2022 / base PhcKnowledgeReceiptTests.
- Validador Object Index: 21 objetos sintéticos.
- Validador ZIP: 5 ficheiros de contrato, 4 artefactos, 2 objetos, 4 operações e 8 checksums.

Estes resultados pertencem ao commit indicado, não são uma execução de testes no HEAD 9a9df92ff82a98eafa2a5c1ff5134d8226ea7ce5. Os 76 testes SQL são integração de receipts/recovery do Knowledge; não provam recolha dos collectors Scanner contra uma instalação PHC.

Captura real anteriormente documentada CSDefault202603: 16.618 objetos, 49 definições, 921 factos REL/DTL e 921 interpretações; 8 dos 21 Object Index kinds presentes. É evidência de uma captura concreta, não prova de cobertura total.

Nesta atualização documental não foram executados testes, builds, SQL nem captura real.

## 4. Lacunas e riscos conhecidos

A divergência entre ZIP temporário e arquivo permanente está analisada em [ikp-temporary-package-lifecycle-gap-2026-11-20.md](docs/architecture/ikp-temporary-package-lifecycle-gap-2026-11-20.md). A decisão de eliminação só pode ser implementada depois de materialização e verificação persistente; o código atual ainda reabre o ZIP arquivado para calcular uma análise não persistida.

1. Alinhar arquivo atual com o ciclo aprovado de ZIP temporário, garantindo recuperação sem eliminar dados antes da materialização íntegra.
2. Materializar observações, definições, artefactos e REL/DTL de forma pesquisável, preservando factos e proveniência.
3. Concluir o modelo e os fluxos de clientes/instalações/ambientes e respetivas permissões.
4. Fechar cobertura de collectors com testes diretos SQL e validação real por operação.
5. Corrigir semântica de coverage quando tabelas obrigatórias faltam ou probes falham; tratar exceções de sourceExistsAsync.
6. Preservar original e derivado separadamente em FORMC; clarificar semântica dos artefactos FORMB.
7. Completar a pipeline PHC Standard Catalog e não classificar automaticamente customizações como standard.
8. Rever documentação histórica contraditória, sem apagar registos históricos.
9. RAG continua fora da implementação atual.

## 5. Próximos passos recomendados

1. Fechar o inventário documental individual e respetivo destino.
2. Rever tecnicamente o ciclo de vida do ZIP e desenhar a transição para transporte temporário.
3. Definir a arquitetura funcional do PHC Standard Catalog a partir do extrator existente.
4. Preparar plano de testes SQL por operação para as 23 operações do Scanner.
5. Atualizar o handoff após cada sessão.

## 6. Regra de leitura

Antes de iniciar uma tarefa, ler PROJECT_BASELINE.md, DECISIONS.md, este ficheiro e docs/governance/current-handoff.md; depois consultar contratos técnicos relevantes e confirmar o código na branch efetiva.
