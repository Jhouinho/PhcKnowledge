# IKP 0.1 — Checklist de validação e desempenho

**Data:** 11-11-2026  
**Estado:** decisão técnica registada; alteração ao schema ainda pendente de validação no ambiente de desenvolvimento.

## Evidência recolhida

Foi executado um teste A/B com o pacote real `CSDefault202603-ikp-validation.zip` e JsonSchema.Net 7.3.1:

- Com `uniqueItems: true` no array principal `objects`, a validação não terminou dentro de 120 segundos.
- Removendo apenas essa regra numa cópia temporária do schema, a validação terminou em aproximadamente 2,64 segundos (2,82 segundos no total) e o pacote foi aceite.

O código de `IkpPackageValidator.ValidateObjectIndex` já verifica a unicidade de `sourceObjectId` através de um `HashSet<string>` com comparação ordinal. Assim, a regra de unicidade dos identificadores continua a ser aplicada no validador da aplicação, sem obrigar o JSON Schema a comparar objetos completos do array.

## Decisão proposta

Remover exclusivamente `uniqueItems: true` do array principal `objects` em `docs/architecture/schemas/ikp-0.1/object-index.schema.json`.

Manter `uniqueItems: true` no array `definition.artifactIds`. Não alterar outras regras do schema nesta intervenção.

## Checklist de execução

- [ ] Confirmar branch e estado do repositório local; criar backup de `object-index.schema.json`.
- [ ] Remover apenas `uniqueItems` do array principal `objects`.
- [ ] Executar `dotnet clean PhcKnowledge.sln`, `dotnet test PhcKnowledge.sln` e `dotnet build PhcKnowledge.sln`.
- [ ] Confirmar que identificadores `sourceObjectId` duplicados continuam a ser rejeitados pelo validador e que `artifactIds` duplicados continuam a ser rejeitados pelo schema.
- [ ] Validar novamente `CSDefault202603-ikp-validation.zip`, registar o tempo e confirmar a aceitação do pacote.

## Critério de conclusão

A etapa só fica concluída quando os testes e o build passam, os dois casos de duplicação são rejeitados e o pacote IKP real é validado com sucesso.

**Limite desta decisão:** este registo não autoriza alterações ao exportador, aos coletores, ao modelo SQLite ou à lógica de identidade. Também não autoriza a importação do pacote. A ingestão/importação só deve ser analisada depois de fechar esta validação.
