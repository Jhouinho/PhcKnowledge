# P1.B — Contrato de leitura do workspace para o object-index

| Campo | Valor |
|---|---|
| Data | 2026-11-06 |
| Estado | Análise estática; sem implementação |
| Âmbito | Superfície de leitura SQLite + artefactos necessária ao projector |
| Restrição | Não alterar schema SQLite, dados existentes ou collectors |

## 1. Conclusão

O Scanner já permite abrir um workspace existente em modo read-only. Porém, a API pública atual de `SqliteScanWorkspace` não expõe todos os dados necessários para construir e validar o object-index através de métodos tipados.

Isto não impede a implementação do projector, mas obriga a decidir como será feita a leitura:
- pequena API read-only no projeto Workspace; ou
- leitor SQLite dedicado no projeto Export.

A opção recomendada é uma API read-only mínima no Workspace, sem alterar o schema nem o comportamento de escrita. Se se preferir não tocar no projeto Workspace, o Export poderá usar uma implementação interna de leitura SQLite, mas isso duplica conhecimento do schema e acopla o exportador à estrutura física da base de dados.

## 2. O que já existe

| Necessidade | API/comportamento atual | Estado |
|---|---|---|
| Abrir workspace existente | `SqliteScanWorkspace.OpenExistingAsync(rootPath, readOnly: true)` | Disponível |
| Impedir escrita SQLite | Abre `SqliteOpenMode.ReadOnly`, sem `EnsureSchema`, com `PRAGMA query_only=ON` | Disponível |
| CaptureId | Propriedade `CaptureId`, carregada na abertura quando a coluna existe | Disponível, pode faltar em workspaces antigos |
| Objetos | `GetObjectsAsync`, `GetObjectAsync` | Disponível |
| Issues | `GetIssuesAsync` | Disponível |
| Texto de artefacto | `ReadArtifactTextAsync` | Disponível, mas não devolve bytes exatos nem metadata do artefacto |
| Relações derivadas | `GetRelationsAsync` | Disponível, fora do primeiro object-index |
| Relações declaradas | `GetDeclaredRelationsAsync` | Disponível, fora do primeiro object-index |

## 3. Lacunas concretas

### 3.1 Capture metadata

O workspace grava `capabilities_json` e `coverage_json` em `scan_meta`, através da operação de escrita de metadados da captura.

Não foi encontrado método público correspondente para ler:
- o catálogo de `DeclaredCapability`;
- a lista de `OperationCoverage`;
- o estado de captura e os metadados de origem necessários ao relatório de projeção.

O CaptureId é carregado na abertura, mas capabilities/coverage não são expostos por getters.

### 3.2 Artefactos

A tabela `artifacts` contém:
- `artifact_id`;
- `collector_id`;
- `relative_path`;
- `content_type`;
- `sha256`;
- `byte_length`;
- `metadata_json`.

A API atual não expõe uma listagem tipada destes registos nem permite consultar um artefacto por ID com os seus metadados.

`ReadArtifactTextAsync`:
- consulta apenas `collector_id` e `relative_path`;
- lê o ficheiro como UTF-8 para string;
- devolve `null` tanto quando o registo de artefacto não existe como quando o ficheiro não existe.

Isto não é suficiente para verificar rigorosamente o tamanho e SHA-256 dos bytes originais. A validação do DefinitionFingerprint não deve usar texto redecodificado e novamente codificado como substituto dos bytes originais.

### 3.3 Integridade e referências

O projector precisa de distinguir:
- ArtifactId inexistente na tabela `artifacts`;
- registo existente, ficheiro ausente;
- ficheiro presente, mas tamanho divergente;
- ficheiro presente, mas SHA-256 divergente;
- artefacto válido.

A API atual de leitura de texto não permite distinguir todos estes casos. O projector também precisa de executar uma verificação read-only de integridade SQLite, ou de receber um resultado equivalente.

### 3.4 Compatibilidade com workspaces antigos

`OpenExistingAsync(readOnly: true)` não executa `EnsureSchema`, o que é correto para não modificar workspaces antigos.

A abertura tem fallback específico para a ausência de `capture_id`. Não existe ainda uma abstração equivalente para a ausência de `capabilities_json` ou `coverage_json`.

O projector deve:
- detetar a presença das colunas antes de as consultar;
- tratar coverage/capabilities ausentes como informação indisponível, nunca como coverage vazia/conclusiva;
- emitir diagnóstico de compatibilidade;
- não tentar atualizar o workspace para o tornar compatível.

## 4. Contrato mínimo de leitura recomendado

Sem criar ou alterar tabelas, o Workspace poderá expor métodos equivalentes a:

| Método conceptual | Resultado necessário |
|---|---|
| `GetCaptureMetadataAsync` | CaptureId, status, capabilities e coverage, indicando explicitamente campos indisponíveis em workspaces antigos |
| `GetArtifactsAsync` | Lista de artefactos com ID, collector, caminho relativo, content type, SHA-256, byte length e metadata |
| `GetArtifactAsync(artifactId)` | Metadados de um artefacto específico |
| `ReadArtifactBytesAsync(artifactId)` | Bytes exatos do ficheiro, sem conversão de encoding |
| `CheckIntegrityAsync` | Resultado de `PRAGMA integrity_check` ou diagnóstico equivalente |

Os nomes são indicativos, não uma decisão de API final.

Regras obrigatórias:
- Todos os métodos usados pelo projector funcionam em modo read-only.
- Nenhum método executa `EnsureSchema`, INSERT, UPDATE, DELETE, CREATE ou migração.
- `ReadArtifactBytesAsync` não normaliza encoding, BOM, quebras de linha ou conteúdo.
- O caminho do artefacto é resolvido e validado relativamente à raiz do workspace; rejeitar path traversal.
- A API não deve devolver credenciais nem connection strings.
- A ausência de metadata histórica deve ser explícita, não convertida em listas vazias com semântica de sucesso.

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

1. Abrir workspace atual em read-only e confirmar `query_only=ON`.
2. Confirmar que a abertura não cria tabelas nem colunas.
3. Ler CaptureId, capabilities e coverage de workspace atual.
4. Abrir workspace antigo sem `capture_id`/coverage e produzir estado de metadata indisponível sem escrever.
5. Listar artefactos e confirmar todos os campos da tabela.
6. Ler bytes de artefacto e confirmar igualdade byte-a-byte com o ficheiro.
7. Distinguir ArtifactId inexistente de ficheiro ausente.
8. Detetar byteLength divergente.
9. Detetar SHA-256 divergente.
10. Detetar caminho relativo malicioso ou fora da raiz.
11. Executar integridade SQLite e propagar falha como diagnóstico.
12. Verificar que nenhuma operação de leitura altera o hash do ficheiro SQLite ou dos artefactos.

## 7. Sequência proposta

1. Fechar a API read-only mínima e os DTOs de leitura.
2. Implementar testes do Workspace sem alterar o schema.
3. Construir o núcleo puro do projector sobre DTOs e bytes fornecidos.
4. Integrar a leitura do workspace com o projector.
5. Executar contra captura real quando o SQLite e a pasta completa de artefactos estiverem disponíveis.

Até à validação com captura real, não alterar schema SQLite, collectors, queries, IDs atuais ou artefactos já produzidos.
