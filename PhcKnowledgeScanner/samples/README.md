# Samples e fixtures do PhcKnowledgeScanner

Este diretório contém exemplos e fixtures. Não colocar dumps reais de clientes, connection strings, credenciais ou dados comerciais.

## Estrutura atualmente versionada

```text
samples/
  manifests/
    example-manifest.json
  database-schema-snapshot/
    manifest.json
    documentation/overview.md
    tables/
    indexes/
    relationships/
    views/
    procedures/
    triggers/
    functions/
    synonyms/
    types/
```

O exemplo `database-schema-snapshot` é um conjunto de artefactos de demonstração do contrato Schema Snapshot. Não o tratar como captura real de uma instalação nem como prova da cobertura atual dos collectors. Confirmar a sanitização e a origem de cada novo sample antes de o publicar.

A estrutura acima substitui a estrutura antiga `fixtures/` descrita em versões anteriores deste README.
