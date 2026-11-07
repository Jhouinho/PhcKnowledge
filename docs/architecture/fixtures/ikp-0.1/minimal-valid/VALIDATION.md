# Validação da fixture IKP 0.1

Data: 2026-11-06  
Fixture: `minimal-valid`  
Resultado: **PASS — schemas, integridade e referências cruzadas**

## Verificações executadas

| Verificação | Resultado |
|---|---|
| Cinco documentos principais validados pelos respetivos JSON Schemas Draft 2020-12 | PASS |
| Os cinco schemas passam a validação do meta-schema Draft 2020-12 | PASS |
| CaptureId consistente entre manifesto, coverage, índice e relações | PASS |
| 4 operações de cobertura correspondem a 4 capabilities declaradas | PASS |
| 3 objetos técnicos observados ligados a operações válidas | PASS |
| 4 artefactos declarados existem e correspondem aos ficheiros payload | PASS |
| artifactIds do índice resolvem para o manifesto | PASS |
| SHA-256 e byteLength dos 8 ficheiros cobertos por checksums | PASS |
| SHA-256 e byteLength dos 4 artefactos conferem com o manifesto | PASS |
| issueId referenciado pela cobertura existe nos diagnósticos | PASS |
| Interpretação aponta para relação declarada no payload de relações, sem exigir objeto no índice | PASS |
| Digest lógico recalculado corresponde ao valor declarado | PASS |
| Definição BRULE indisponível preserva o objeto inventariado | PASS |
| Casos positivos, negativos e de fronteira executados sobre cópias temporárias | **22/22 PASS** |

A validação dos schemas foi feita com o validador Python `jsonschema` (Draft 2020-12). O validador do pacote e as mutações de teste foram executados sobre uma representação local byte-a-byte coincidente com os ficheiros de referência: os oito tamanhos e SHA-256 declarados foram confirmados.

## Valores calculados

- Ficheiros cobertos por `checksums.json`: 8
- Artefactos payload: 4
- Objetos no índice: 3
- Operações de cobertura: 4
- Relações declaradas no payload separado: 1
- Issues: 1
- Digest lógico SHA-256: `671f88cacc755ef2f4ad35447c4df9792e9e5c1878cff8c50f0631e930cb7629`

## Cenário de cobertura demonstrado

A fixture inclui duas BRULE observadas:
- `phc.brule:7F3A`: definição disponível e artefacto preservado.
- `phc.brule:7F3B`: objeto inventariado, mas definição indisponível; a issue correspondente é ligada à operação e ao objeto.

A cobertura da operação de definição é `SucceededWithWarnings / Partial`. A ausência de uma definição não é confundida com ausência do objeto nem com remoção.

## Casos negativos e de fronteira

Os 22 cenários cobrem:
- pacote válido e cobertura parcial válida;
- ficheiro obrigatório ausente;
- payload e checksum adulterados;
- enum inválido;
- operação desconhecida ou não suportada;
- operação não solicitada com resultado incompatível;
- referências de artefactos e issues inválidas;
- IDs de objetos duplicados;\n- separação entre objetos técnicos e relações REL/DTL declaradas;\n- validação isolada do object-index contra o schema oficial, incluindo UUID e estados inválidos;
- CaptureId divergente;
- ficheiros extra na raiz ou no payload;
- artefacto omitido do manifesto;
- capability sem entrada de coverage;
- path traversal.

Os testes alteram cópias temporárias e recalculam checksums quando o objetivo é testar uma regra semântica. Assim, não são bloqueados prematuramente pela verificação de integridade.

## Limites

- A validação foi feita sobre o conteúdo extraído/árvore de ficheiros; ainda não foi produzido nem testado um ZIP binário final.
- O checksum do ZIP completo será calculado pela plataforma no momento de receção.
- O validador ainda não aplica limites anti-zip-bomb, análise de malware, deteção de segredos/dados pessoais, política de partilha, assinatura digital ou compatibilidade semântica entre versões de capabilities.
- O workflow GitHub Actions foi criado para repetir a validação em alterações futuras; não é aqui declarado um resultado de execução CI.
- Os dados são demonstrativos e não provêm de uma instalação PHC real.
