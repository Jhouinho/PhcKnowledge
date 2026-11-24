# Estratégia de testes — fundação histórica e cobertura atual

> **Precedência — 2026-11-24:** o comando `smoke` referido abaixo foi removido da CLI. A validação funcional com instalações PHC reais segue o [protocolo de validação funcional](../../docs/architecture/scanner-functional-validation-protocol-2026-11-24.md) e usa `scan-ikp`. Este ficheiro permanece como fotografia da estratégia de testes automáticos; não é o procedimento das duas recolhas.

## Objetivos nesta fase

Validar a **orquestração** e os **contratos**, não a recolha PHC.

## Pirâmide (fase 1)

| Camada | Âmbito | Estado |
| --- | --- | --- |
| Unitários | `ScanEngine` (seleção, skip, probe-only, duplicados) | Implementado |
| Unitários | Export ZIP legado | Implementado; ver testes do projeto Export |\n| Unitários | Export IKP 0.1 (manifesto, capabilities, coverage, Object Index, checksums e payload) | Implementado; ver `IkpPackageExporterTests` |
| Smoke | CLI `smoke` com collector fictício | Implementado |
| Integração SQL / PHC | Ligação real a instalações | **Adiado** — requer collectors e ambiente |
| Contrato IKP 0.1 | Schemas Draft 2020-12, fixtures, validador Python e testes positivos/negativos | Implementados; ver `tools/ikp-validation` e `docs/architecture/schemas/ikp-0.1` |

## Princípios

1. **Sem BD PHC** nos testes de CI desta fase.
2. Collectors reais serão testados com **fixtures** em `samples/` (dados fictícios / dumps sanitizados).
3. Testes de integração com SQL só com opt-in explícito (env var / trait), nunca por omissão no CI.
4. Qualquer teste que toque ficheiros usa diretórios temporários e limpa após execução.

## Comandos

```bash
dotnet test PhcKnowledgeScanner.sln
dotnet run --project src/PhcKnowledgeScanner.Cli -- smoke
```

## Próximos testes — estado em 2026-11-10

- Validar um ZIP IKP produzido pelo exportador a partir da captura PHC default real com o validador independente.
- Alargar a validação real do Object Index aos kinds ainda não presentes na captura (13/21), sem substituir testes sintéticos.
- Testar compatibilidade/idempotência no futuro importador do PHC Knowledge; estes testes pertencem à solução da plataforma, não ao Scanner.


A existência dos testes unitários e sintéticos do exportador não equivale à validação end-to-end de um ZIP IKP gerado a partir de uma captura real. Essa validação permanece pendente.
