> **Nota de âmbito — 2026-11-20:** esta checklist foi criada na fase inicial do Scanner e não foi reconciliada com todas as decisões/implementações posteriores. Não é o registo atual de decisões do produto. Consultar `../../DECISIONS.md`, ADRs em `docs/decisions/` e o código. Antes de reabrir qualquer ponto, verificar se já foi decidido ou implementado. Manter aqui apenas questões específicas do Scanner ainda válidas; não assumir que todas as caixas por marcar continuam abertas.

# Decisões técnicas pendentes

Distinguir de ADRs em `docs/decisions/` (já tomadas).  
**Nada disto deve ser implementado como “certo” sem validação explícita.**

## Ligação e autenticação SQL

- [ ] Connection string via ficheiro local vs variáveis de ambiente vs prompt
- [ ] Windows Auth vs SQL Auth vs ambos
- [ ] Suporte multi-base / linked servers

## Política de execução

- [ ] Continuar após falha de um collector vs fail-fast
- [ ] Paralelismo entre collectors (risco de carga no SQL)
- [ ] Limites de timeout por collector / por objeto

## Artefactos e volume

- [ ] Escrita direta para disco durante a recolha vs buffer em memória
- [ ] Compressão por ficheiro vs só no ZIP final
- [ ] Política para binários / anexos muito grandes (aviso vs exclusão explícita)
- [ ] Encoding default para texto XBase/SQL (UTF-8 com BOM? Latin1?)

## Descoberta de versão PHC

- [ ] Como identificar versão/produto (Desktop, Web, XL, Manufactor)
- [ ] Matriz mínima de collectors por versão

## Organização de collectors

- [ ] Um assembly por área vs pastas num único `Collectors`
- [ ] Plugin loading dinâmico (MEF/assemblies externos) vs registo estático no CLI

## CLI / UX

- [ ] `System.CommandLine` vs Spectre.Console vs args manuais
- [ ] Progress reporting (consola apenas vs ficheiro de log estruturado)

## Segurança e privacidade

- [ ] Redação de segredos em configs exportadas
- [ ] Classificação de dados sensíveis no manifesto
- [ ] Retenção local de scans

## Relação com conhecimento offline do workspace

- [ ] O Scanner deve ou não consumir o pack `.cursor/knowledge` em runtime?
- [ ] Ou o pack é só referência para desenvolvimento dos collectors?

## Naming / versionamento do pacote ZIP

- [ ] Convenção de nome do ficheiro
- [ ] Versionamento do schema `manifest.json` (`schemaVersion`)
