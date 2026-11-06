# Validação da fixture IKP 0.1

Data: 2026-11-06  
Fixture: `minimal-valid`  
Resultado: **PASS — validação estrutural, integridade e referências cruzadas**

## Verificações executadas

| Verificação | Resultado |
|---|---|
| Cinco ficheiros de contrato presentes | PASS |
| JSON de manifesto, capabilities, coverage, object-index, relações, diagnósticos e checksums analisável | PASS |
| CaptureId consistente entre manifesto, coverage, índice e relações | PASS |
| 4 operações de cobertura correspondem a capabilities declaradas | PASS |
| 4 objetos observados têm operationId declarado | PASS |
| 4 artefactos do manifesto existem no payload | PASS |
| artifactIds do índice resolvem para o manifesto | PASS |
| SHA-256 e tamanho dos 8 ficheiros cobertos por checksums | PASS |
| SHA-256 e tamanho dos 4 artefactos conferem com o manifesto | PASS |
| issueId referenciado pela cobertura existe nos diagnósticos | PASS |
| Relação e interpretação apontam para objeto existente | PASS |
| Digest lógico recalculado corresponde ao valor declarado | PASS |
| Definição BRULE indisponível representada sem apagar o objeto inventariado | PASS |

## Valores calculados

- Ficheiros cobertos por `checksums.json`: 8
- Artefactos payload: 4
- Objetos no índice: 4
- Operações de cobertura: 4
- Relações declaradas: 1
- Issues: 1
- Digest lógico SHA-256: `0af0c97178fb1bae568c555f67d2752471b57231ccd152fa82c0a62355ed3f32`

## Cenário de cobertura demonstrado

A fixture inclui duas BRULE observadas:
- `phc.brule:7F3A`: definição disponível e artefacto preservado.
- `phc.brule:7F3B`: objeto inventariado, mas definição indisponível; a issue correspondente é ligada à operação e ao objeto.

Assim, a ausência de definição não é confundida com ausência do objeto nem com remoção.

## Limites desta validação

- Foi validada a árvore de ficheiros que constitui o conteúdo do pacote, não um ZIP binário final.
- Foi executada validação de integridade e de referências cruzadas; ainda não existe nesta etapa um validador JSON Schema executável para todos os documentos.
- O checksum do ZIP completo será calculado pela plataforma no momento de receção, conforme o contrato.
- Os dados são demonstrativos e não provêm de uma instalação PHC real.
