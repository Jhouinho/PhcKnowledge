# JSON Schemas — IKP 0.1

Schemas publicados em JSON Schema Draft 2020-12.

| Ficheiro | Documento |
|---|---|
| `manifest.schema.json` | Manifesto e registo de artefactos |
| `capabilities.schema.json` | Capacidades/operações do Scanner |
| `coverage.schema.json` | Resultado e disponibilidade por operação |
| `object-index.schema.json` | Objetos observados, identidade e definição |
| `checksums.schema.json` | Integridade por ficheiro e digest lógico |
| `fingerprint.schema.json` | Definição reutilizável de fingerprint |

Os schemas validam forma, tipos, campos obrigatórios e enums, incluindo a unicidade local de `definition.artifactIds`. Não substituem validações semânticas entre ficheiros, hashes dos bytes, consistência de CaptureId ou validação de paths, cobertas pelo validador de referência em `tools/ikp-validation`. A unicidade de `sourceObjectId` é também verificada pelo `IkpPackageValidator` C# através de um `HashSet<string>` com comparação ordinal.

**Nota de desempenho — 2026-11-11:** um teste A/B com um pacote real mostrou que `uniqueItems: true` no array raiz `objects` fazia a validação exceder 120 segundos; removendo apenas essa regra numa cópia temporária, a validação terminou em 2,82 segundos. Está decidida a remoção dessa regra no schema oficial, mantendo a verificação de `sourceObjectId` no validador C# e `uniqueItems` em `definition.artifactIds`. A alteração ao schema ainda não foi aplicada; até ser aplicada, o schema versionado continua a conter a regra antiga.

Os schemas são intencionalmente extensíveis em objetos de metadados e envelopes, mas mantêm estritos os campos que determinam identidade e integridade. A versão do contrato continua a ser `0.1`; alterações incompatíveis exigem nova versão de contrato ou capability, não uma alteração silenciosa deste schema.
