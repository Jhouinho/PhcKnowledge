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

Os schemas validam forma, tipos, campos obrigatórios e enums. Não substituem validações semânticas entre ficheiros, hashes dos bytes, consistência de CaptureId, unicidade de identificadores ou validação de paths. Essas regras estão no validador Python em `tools/ikp-validation`.

Os schemas são intencionalmente extensíveis em objetos de metadados e envelopes, mas mantêm estritos os campos que determinam identidade e integridade. A versão do contrato continua a ser `0.1`; alterações incompatíveis exigem nova versão de contrato ou capability, não uma alteração silenciosa deste schema.
