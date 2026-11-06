# Estratégia inicial de testes

## Objetivos nesta fase

Validar a **orquestração** e os **contratos**, não a recolha PHC.

## Pirâmide (fase 1)

| Camada | Âmbito | Estado |
| --- | --- | --- |
| Unitários | `ScanEngine` (seleção, skip, probe-only, duplicados) | Implementado |
| Unitários | Export ZIP (estrutura mínima, paths inválidos) | A adicionar com refinamentos |
| Smoke | CLI `smoke` com collector fictício | Implementado |
| Integração SQL / PHC | Ligação real a instalações | **Adiado** — requer collectors e ambiente |
| Contrato de manifesto | Schema JSON estável | Adiado até `schemaVersion` definido |

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

## Próximos testes (após validação da arquitetura)

- Export: manifesto presente; artefactos nos paths esperados; rejeição de `..` em paths
- Engine: agregação de `Partial` / `SucceededWithWarnings`
- Fakes de collector que simulam omissão de objetos e issues
