# Estratégia de testes do PhcKnowledgeScanner

> Estratégia específica do Scanner. A estratégia global do PHC Knowledge distingue quatro níveis de evidência; este documento detalha a parte Scanner.

## Níveis de validação

| Nível | Objetivo | O que prova | O que não prova |
|---|---|---|---|
| Sintético/unitário | Regras, resolvers, projeções, parsers, exportador e contratos com dados controlados | Comportamento determinístico nos cenários cobertos | Recolha real em SQL Server |
| Integração SQL | Executar collectors contra SQL Server de teste com schema/dados conhecidos | Queries, mapeamentos e comportamento no SQL testado | Compatibilidade universal com todas as instalações PHC |
| Captura real PHC | Executar Scanner numa instalação autorizada e analisar artefactos/coverage | Comportamento observado naquela instalação/captura | Cobertura de operações ou kinds ausentes e outras instalações |
| CI | Executar workflows publicados num commit identificado | Resultado automatizado daquele workflow/commit | Testes não incluídos no workflow ou validação real dependente de ambiente externo |

## Princípios
1. Não usar dados comerciais reais em testes automatizados.
2. Testes SQL são opt-in e usam base de teste dedicada; não ligar a instalações de clientes sem autorização.
3. Fixtures e samples devem ser identificados como sintéticos, sanitizados ou reais.
4. Cada teste declara o nível de evidência que fornece.
5. Testes ao resolver/projector/exporter não provam que o collector recolhe corretamente.
6. Mapear as 23 operações do catálogo para teste sintético, integração SQL, captura real e CI.
7. Registar versão SQL Server, schema/fixture, operação, resultado e limitações.

## Estado conhecido na auditoria de 2026-11-20
- Foram encontrados testes sintéticos de resolução, projeção, parsing, persistência e exportação.
- Não foram encontrados testes versionados que executem diretamente os collectors reais contra SQL Server e validem individualmente as 23 operações.
- A captura real CSDefault202603 fornece evidência para objetos presentes; apenas 8 dos 21 Object Index kinds foram observados.
- A integração SQL Server de receipts do Knowledge não é integração SQL dos collectors Scanner.

## Comandos
A partir de PhcKnowledgeScanner/:

```bash
dotnet test PhcKnowledgeScanner.sln
dotnet run --project src/PhcKnowledgeScanner.Cli -- smoke
```

Estes comandos não foram executados nesta atualização documental.
