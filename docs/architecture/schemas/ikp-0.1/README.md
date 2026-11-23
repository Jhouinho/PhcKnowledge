# JSON Schemas — IKP 0.1

Schemas publicados em JSON Schema Draft 2020-12.

| Ficheiro | Documento |
|---|---|
| `manifest.schema.json` | Manifesto e registo de artefactos |
| `capabilities.schema.json` | Capacidades/operações do Scanner |
| `coverage.schema.json` | Resultado e disponibilidade por operação |
| `object-index.schema.json` | Objetos observados, identidade e definição |
| `checksums.schema.json` | Integridade por ficheiro e digest lógico |
| `fingerprint.schema.json` | MetadataFingerprint |
| `definition-fingerprint.schema.json` | Envelope D-021 do DefinitionFingerprint |

Os schemas validam forma, tipos, campos obrigatórios e enums, incluindo a unicidade local de `definition.artifactIds`. Não substituem validações semânticas entre ficheiros, hashes dos bytes, consistência de CaptureId ou validação de paths, cobertas pelo validador de referência em `tools/ikp-validation`. A unicidade de `sourceObjectId` é também verificada pelo `IkpPackageValidator` C# através de um `HashSet<string>` com comparação ordinal.

**Nota de desempenho — 2026-11-18:** um teste A/B com um pacote real mostrou que `uniqueItems: true` no array raiz `objects` fazia a validação exceder 120 segundos; removendo apenas essa regra, a validação terminou em cerca de 2,8 segundos. A regra foi removida do schema oficial. Mantêm-se a verificação de `sourceObjectId` no validador C# (`HashSet` ordinal) e `uniqueItems` em `definition.artifactIds`. Com a captura real CSDefault202603 (~16.618 objetos), o harness de receção/recovery passou a concluir em segundos.

Os schemas são intencionalmente extensíveis em objetos de metadados e envelopes, mas mantêm estritos os campos que determinam identidade e integridade. A versão global do contrato continua a ser `0.1`. Alterações incompatíveis a um ficheiro interno exigem evolução explícita da versão do respectivo schema (por exemplo, `object-index.schemaVersion`), ou nova versão de contrato/capability quando afectem o contrato global; nunca uma alteração silenciosa. A compatibilidade de uma versão anterior só existe quando estiver explicitamente declarada na matriz aplicável. Não se exige leitura ou conversão dos pacotes IKP de teste de iterações antigas do Scanner que foram descartados. A regra específica de leitura do coverage schema v1 não cria uma obrigação geral de suportar esses pacotes.


## Baseline de compatibilidade — Object Index v2 (D-022, 23-11-2026)

O IKP global mantém a versão 0.1; o Object Index passa de schemaVersion=1 para schemaVersion=2 para representar o envelope D-021 de definitionFingerprint. Esta evolução é incompatível com o formato anterior: o importador actualizado aceita v2 e rejeita v1. Não existe obrigação de ler ou converter pacotes IKP de teste antigos que foram descartados.

O fingerprint.schema.json existente fica associado ao MetadataFingerprint, cujo contrato não muda. Na implementação será introduzido um schema próprio definition-fingerprint.schema.json para os estados D-021. O Object Index v2 exige fingerprint explícito (Available ou Unavailable) quando a definição está Available, e omite a propriedade quando a definição está Empty, Unavailable ou NotCollected.

A análise de importação evolui para schema 0.3, com contagens separadas de fingerprints disponíveis e indisponíveis. Estas alterações são aqui documentadas como baseline aprovado; os ficheiros JSON Schema, fixtures, modelos e validadores executáveis só serão alterados em conjunto na fase de implementação.


Object Index `schemaVersion=2` é a versão suportada pela implementação D-022. O fingerprint genérico é exclusivo de `MetadataFingerprint`; `DefinitionFingerprint` tem schema próprio com estados `Available` e `Unavailable`, incluindo política/versionamento ou `reasonCode`. Para definições `Available`, o envelope é obrigatório; para `Empty`, `Unavailable` e `NotCollected`, é omitido. A primeira implementação emite `Unavailable/NoValidatedPolicy` para definições disponíveis, pois não existem políticas semânticas de produção aprovadas. Object Index v1 é rejeitado; não há conversão de pacotes de teste antigos descartados.
