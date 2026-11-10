# P1.B — Contrato de leitura do workspace para o object-index

| Campo | Valor |
|---|---|
| Data | 2026-11-06 |
| Estado | API read-only, leitor tipado e integração no projector implementados; testes CI aprovados |
| Âmbito | Superfície de leitura SQLite + artefactos necessária ao projector |
| Restrição | Não alterar schema SQLite, dados existentes ou collectors |

## 1. Conclusão

O Scanner já expõe agora a API read-only mínima no projeto Workspace. A implementação lê os dados tipados e os bytes originais dos artefactos sem alterar o schema SQLite nem os collectors. O Export pode trabalhar sobre DTOs e bytes, sem conhecer SQL interno do workspace.

## 2. O que já existe

| Necessidade | API/comportamento atual | Estado |
|---|---|---|
| Abrir workspace existente | `SqliteScanWorkspace.OpenExistingAsync(rootPath, readOnly: true)` | Disponível |
| Impedir escrita SQLite | Abre `SqliteOpenMode.ReadOnly`, sem `EnsureSchema`, com `PRAGMA query_only=ON` | Disponível |
| CaptureId/capabilities/coverage | `GetCaptureMetadataAsync` | Disponível; deteta colunas ausentes e distingue JSON indisponível de arrays explicitamente vazios |
| Objetos | `GetObjectsAsync`, `GetObjectAsync` | Disponível |
| Issues | `GetIssuesAsync` | Disponível |
| Metadados de artefactos | `GetArtifactsAsync`, `GetArtifactMetadataAsync` | Disponível |
| Bytes de artefacto | `ReadArtifactBytesAsync` | Disponível; lê bytes exatos sem conversão de encoding |
| Relações derivadas | `GetRelationsAsync` | Disponível, fora do primeiro object-index |
| Relações declaradas | `GetDeclaredRelationsAsync` | Disponível, fora do primeiro object-index |

## 3. Lacunas concretas

### 3.1 Capture metadata

O workspace grava `capabilities_json` e `coverage_json` em `scan_meta`. `GetCaptureMetadataAsync` lê CaptureId, status, capabilities e coverage, detetando a presença de cada coluna antes da consulta. A resposta distingue coluna ausente, valor nulo/inválido e array vazio explícito através de flags e diagnostics. `HasCompleteCoverageMetadata` só é verdadeiro quando CaptureId e ambos os conjuntos de metadata estão disponíveis.

### 3.2 Artefactos

A tabela `artifacts` contém:
- `artifact_id`;
- `collector_id`;
- `relative_path`;
- `content_type`;
- `sha256`;
- `byte_length`;
- `metadata_json`.

A API expõe agora `GetArtifactsAsync` e `GetArtifactMetadataAsync`, incluindo ID, collector, caminho relativo, content type, SHA-256, byte length e metadata JSON. `ReadArtifactBytesAsync` devolve os bytes exatos, sem conversão de encoding. Se devolver `null`, `GetArtifactMetadataAsync` permite distinguir ausência da linha de artefacto de ficheiro ausente.

`ReadArtifactTextAsync` mantém-se para consumidores textuais e passou a reutilizar a mesma validação de caminho. A validação de tamanho e SHA-256 é feita pelo projector de definições sobre os bytes devolvidos.

### 3.3 Integridade e referências

O projector distingue ArtifactId inexistente de ficheiro ausente através dos métodos de metadata e bytes. Tamanho e SHA-256 são verificados pelo `DefinitionProjectionResolver`. `CheckIntegrityAsync` executa `PRAGMA integrity_check` e devolve as mensagens do SQLite.

### 3.4 Compatibilidade com workspaces antigos

`OpenExistingAsync(readOnly: true)` não executa `EnsureSchema`. `GetCaptureMetadataAsync` deteta individualmente a presença de `capture_id`, `capabilities_json` e `coverage_json`.

Workspaces antigos são lidos sem migração: colunas ausentes resultam em flags falsas e diagnostics; JSON nulo/inválido resulta em coleções indisponíveis; apenas `[]` explícito resulta numa coleção vazia válida. Nenhum método tenta atualizar o workspace para o tornar compatível.

## 4. Contrato mínimo de leitura recomendado

A API read-only foi implementada sem criar ou alterar tabelas:

| Método | Resultado |
|---|---|
| `GetCaptureMetadataAsync` | CaptureId, status, capabilities, coverage, flags de colunas e diagnostics |
| `GetArtifactsAsync` | Lista de artefactos com metadata e JSON original |
| `GetArtifactMetadataAsync(artifactId)` | Metadados de um artefacto específico |
| `ReadArtifactBytesAsync(artifactId)` | Bytes exatos do ficheiro |
| `CheckIntegrityAsync` | Resultado de `PRAGMA integrity_check` |

Salvaguardas implementadas:
- Métodos de leitura sem operações de escrita/migração.
- Compatibilidade com colunas ausentes em workspaces antigos.
- Validação do ArtifactId contra collector e caminho relativo.
- Rejeição de segmentos `.`/`..`, caminhos fora da raiz e reparse points/symlinks no caminho.
- `ReadArtifactTextAsync` reutiliza a validação de caminho.
- Metadata JSON original é preservado mesmo quando não pode ser desserializado.
- Ausência de metadata histórica não é convertida em arrays vazios.
- Nenhuma credencial ou connection string é exposta.

## 5. Decisão arquitetural recomendada

**Preferir a API read-only no Workspace**, porque:
1. o schema SQLite fica encapsulado no projeto que já o mantém;
2. Export trabalha com registos tipados e não precisa de conhecer SQL interno;
3. a validação de artefactos fica centralizada;
4. os testes de leitura read-only podem ser feitos no projeto Workspace;
5. o projector permanece testável com abstrações/fakes sem depender de SQLite.

Isto é uma alteração de superfície de leitura do projeto Workspace, não uma alteração ao schema SQLite. Não requer modificar collectors nem a recolha PHC/SQL.

Se o requisito de não alterar sequer o código do projeto Workspace for absoluto, usar um leitor interno no projeto Export, isolado e testado, como alternativa temporária.

## 6. Testes de aceitação do leitor

1. Abrir workspace atual em read-only e ler CaptureId, capabilities e coverage.
2. Abrir workspace antigo sem `capture_id`/coverage e obter metadata indisponível sem escrever.
3. Listar artefactos e confirmar metadata, content type, SHA-256 e byte length.
4. Ler bytes e confirmar igualdade byte-a-byte, incluindo BOM e CRLF.
5. Distinguir ArtifactId inexistente de ficheiro ausente.
6. Rejeitar caminho relativo malicioso/path traversal.
7. Executar integridade SQLite e validar o resultado `ok`.
8. Confirmar que `PRAGMA data_version` não muda durante as operações de leitura.
9. Verificar tamanho e SHA-256 divergentes no `DefinitionProjectionResolver`.

## 7. Implementação e sequência restante

Concluído:
1. API read-only mínima e DTOs de leitura.
2. Testes de compatibilidade com workspace antigo, leitura byte-a-byte, integridade SQLite e path traversal.
3. Núcleo puro do projector sobre input tipado.
4. Integração do leitor com o projector através de `ObjectIndexWorkspaceProjector`.
5. Leitura dos bytes apenas para artefactos efetivamente referenciados por objetos, evitando carregar artefactos não utilizados na projeção.
6. Cruzamento de operationIds com capabilities e coverage quando a metadata está disponível. Workspaces antigos sem essa metadata recebem diagnóstico explícito; metadata presente mas inconsistente bloqueia a projeção.

A integração do leitor read-only com o projector foi validada no GitHub Actions run `42418465891`: build concluído, 307 testes .NET aprovados e a saída C# de uma captura sintética mista validada pelo JSON Schema IKP 0.1 Draft 2020-12. A suite inclui leitura seletiva de artefactos referenciados, validação cruzada de capabilities/coverage, cenários sintéticos dos 21 kinds e determinismo com diferentes ordens de entrada.

Atualização 2026-11-10:
- O projector foi exercitado com a captura real `CSDefault202603` (16.618 objetos; 49 artefactos com tamanho/hash confirmados).
- O exportador IKP completo está implementado e integrado na CLI.
- Falta validar o ZIP IKP completo produzido a partir da captura real com o validador independente.
- A captura real cobre 8/21 kinds; os restantes continuam cobertos apenas sinteticamente.

Até existir evidência de uma lacuna concreta, manter inalterados schema SQLite, collectors, queries, IDs atuais e artefactos já produzidos.
