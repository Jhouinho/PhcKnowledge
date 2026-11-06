# IKP 0.1 — fixture mínima de referência

Esta pasta contém um exemplo demonstrativo de pacote IKP 0.1. Não contém dados de cliente nem pretende representar uma captura PHC real.

## Conteúdo

- Os cinco ficheiros de contrato obrigatórios.
- Um artefacto de schema SQL.
- Uma definição BRULE fictícia.
- Uma relação PHC e interpretação.
- Um diagnóstico ligado à operação de recuperação de definições.
- Checksums SHA-256 reais dos ficheiros incluídos e digest lógico calculado.

## Como validar manualmente

1. Verificar que cada caminho em `checksums.json.files` existe e que o SHA-256 dos bytes UTF-8 corresponde.
2. Verificar que `manifest.json.artifacts[*]` aponta para o mesmo caminho, tamanho e SHA-256 do artefacto.
3. Verificar referências `artifactIds` do índice contra o manifesto.
4. Verificar `operationId` do índice e `issueIds` da cobertura contra capabilities e diagnósticos.
5. Recalcular o digest lógico pela concatenação ordenada de `path + LF + byteLength decimal + LF + sha256 minúsculo + LF`, em UTF-8.

O digest lógico exclui `checksums.json). O checksum do ZIP completo é calculado pela plataforma sobre os bytes recebidos, não dentro do próprio ZIP.

Os fingerprints de metadados são SHA-256 do JSON canónico dos metadados; os fingerprints de definição correspondem ao SHA-256 dos bytes originais do artefacto. Todos os hashes de integridade e o digest lógico foram calculados.
