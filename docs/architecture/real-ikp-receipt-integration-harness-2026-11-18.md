# Harness de integração com captura IKP real

## Objetivo

Este harness exercita o percurso já implementado em `PhcKnowledge.Import` usando a captura real `CSDefault202603`:

- `IkpPackageValidator`;
- `IkpPackageIngestionService`;
- `SqlServerPackageReceiptAttemptRepository`;
- `FileSystemImmutablePackageStore`;
- `PackageReceiptRecoveryService`.

Não introduz endpoint HTTP, não altera o Generic Host e não materializa entidades do domínio 0.2. O objetivo é validar a fronteira atual de receção, receipt, arquivo e recovery.

## Segurança da captura

O ZIP contém código e configuração PHC reais. **Não o adicionar ao repositório, a fixtures, a GitHub Actions, a artifacts de CI ou a logs.**

O teste recebe apenas um caminho local através de `PHC_KNOWLEDGE_REAL_IKP_PATH`. O ficheiro esperado é o ZIP IKP interior `CSDefault202603-ikp.zip`, não o ZIP exterior que contém o pacote, a cópia extraída e o workspace SQLite.

O teste confirma PackageId, CaptureId, SHA-256 e tamanho conhecidos da captura para evitar validar acidentalmente outro ficheiro.

## Pré-requisitos

- .NET SDK 10;
- uma instância SQL Server de teste, local e isolada;
- permissões para criar a base de dados de teste, tabelas, índices e triggers;
- o ZIP IKP interior da captura real disponível num caminho local.

Usar exclusivamente a base de dados `PhcKnowledgeReceiptTests`. Nunca apontar esta suite a uma base PHC, desenvolvimento partilhado ou produção.

## Execução em PowerShell

Definir as variáveis apenas na sessão local. **Não** guardar passwords no repositório nem as escrever em logs.

Integrated Security (quando a conta Windows estiver habilitada no SQL Server):

```powershell
$env:PHC_KNOWLEDGE_SQL_TEST_CONNECTION_STRING = "Server=localhost\SQLEXPRESS;Database=PhcKnowledgeReceiptTests;Integrated Security=True;TrustServerCertificate=True;Encrypt=False"
$env:PHC_KNOWLEDGE_REAL_IKP_PATH = "D:\Capturas\CSDefault202603-ikp.zip"

dotnet test tests/PhcKnowledge.Import.SqlServer.Integration.Tests/PhcKnowledge.Import.SqlServer.Integration.Tests.csproj --configuration Release --logger "console;verbosity=normal"
```

SQL Authentication (substituir `USER`/`PASSWORD` pelos valores locais; a base continua a ser exclusivamente `PhcKnowledgeReceiptTests`):

```powershell
$env:PHC_KNOWLEDGE_SQL_TEST_CONNECTION_STRING = "Server=localhost\SQLEXPRESS;Database=PhcKnowledgeReceiptTests;User ID=USER;Password=PASSWORD;TrustServerCertificate=True;Encrypt=False"
$env:PHC_KNOWLEDGE_REAL_IKP_PATH = "D:\Capturas\CSDefault202603-ikp.zip"

dotnet test tests/PhcKnowledge.Import.SqlServer.Integration.Tests/PhcKnowledge.Import.SqlServer.Integration.Tests.csproj --configuration Release --logger "console;verbosity=normal"
```

Os testes criam a base de dados se necessário e aplicam as migrations 001 e 002. Cada teste usa um OrganizationId aleatório, remove os seus receipts/tentativas no fim e elimina as pastas temporárias de staging/arquivo.

## Testes adicionados

### 1. Receção real e idempotência

1. Envia o ZIP real ao `IkpPackageIngestionService` através de um `FileStream`.
2. Executa a validação IKP real.
3. Confirma PackageId, CaptureId, SHA-256 e tamanho esperados.
4. Confirma receipt e arquivo físico content-addressed.
5. Confirma que o arquivo está marcado como ReadOnly.
6. Envia novamente o mesmo pacote para a mesma organização.
7. Confirma reutilização do receipt/arquivo e apenas uma linha em cada tabela.

### 2. Interrupção depois do arquivo e antes do receipt

1. Valida o ZIP e coloca-o em staging.
2. Regista a tentativa durável em SQL Server.
3. Arquiva o ZIP no filesystem.
4. Interrompe intencionalmente o percurso antes da transação SQL de conclusão.
5. Confirma o estado intermédio: `PendingArchive`, arquivo presente e zero receipts.
6. Cria novas instâncias de repository, store e recovery service, simulando o reinício do processo.
7. Executa `ProcessDueAsync`.
8. Confirma receipt, tentativa `Completed`, integridade dos metadados e remoção do staging, sem novo upload.

A simulação não mata um processo do sistema operativo; cria deliberadamente o mesmo estado persistido que fica após a falha nessa fronteira e inicia o recovery com novas instâncias.

## Execução em CI

O workflow SQL Server 2022 existente executa estes testes juntamente com a suite de recovery. Como a captura não está presente em CI, os dois testes específicos da captura ficam ignorados pela condição `PHC_KNOWLEDGE_REAL_IKP_PATH`.

Assim, a CI continua a testar SQL Server real sem receber nem armazenar dados PHC do cliente. A execução com a captura real é local e opt-in.

## Limites

- Não valida API HTTP ou autenticação: ainda não fazem parte do Host atual.
- Não materializa Capture, Observation, Artifact ou DeclaredRelation no domínio 0.2.
- Não valida a UI nem pesquisa.
- Não substitui a execução local com o ZIP real.
- A passagem dos testes sintéticos de SQL Server em CI não deve ser descrita como execução bem-sucedida deste harness real.

## Resultados reais — 2026-11-18

### Identificação da captura (sem conteúdo PHC)

| Campo | Valor |
|---|---|
| Base de origem | `CSDefault202603` |
| Ficheiro IKP interior | `CSDefault202603-ikp.zip` |
| PackageId | `b0b7e61f-8ea4-4cf0-a57d-99d37ab220ea` |
| CaptureId | `49826eeb-0388-48bb-a495-2031f30a58a4` |
| ZipByteLength | 783.363 |
| ZipSha256 | `dc06320f4fea6bbaaff2f63f699ce42031a47e8704a6e021512f37d43d266cd5` |
| Object Index | 16.618 objetos |
| Artefactos | 52 |

O ZIP real permanece fora do repositório e é referenciado apenas por `PHC_KNOWLEDGE_REAL_IKP_PATH`.

### Alteração de schema associada

Foi removido `uniqueItems: true` exclusivamente do array raiz `objects` em `docs/architecture/schemas/ikp-0.1/object-index.schema.json`.

Mantém-se:
- `uniqueItems: true` em `definition.artifactIds` (JSON Schema);
- rejeição de `sourceObjectId` duplicado em `IkpPackageValidator.ValidateObjectIndex` com `HashSet<string>(StringComparer.Ordinal)`.

### Camadas de validação (não confundir)

| Camada | O que cobre |
|---|---|
| Testes unitários do validador | Fixture mínima sintética; regressões de unicidade sem captura real |
| Integração SQL Server sintética | Recovery/receipt com pacotes de teste; CI sem ZIP real |
| Inspeção estrutural da captura | Métricas e hashes do ZIP real, fora do pipeline de materialização |
| Harness real (opt-in local) | Receção, arquivo, idempotência e recovery com o ZIP IKP real |

Esta PR **não** valida materialização de Observation/REL/DTL, associação a Installation/Environment, API HTTP nem publicação de conhecimento.

### Tempos medidos com o ZIP real

| Cenário | Antes da remoção de `objects.uniqueItems` | Depois |
|---|---|---|
| Recovery após arquivo e antes do receipt | ~6 m 16 s | **3 s** (PASS) |
| Receção + segunda receção idempotente | Não terminou; execução interrompida ~21 min | **5 s** (PASS) |

Causa do custo anterior: avaliação JSON Schema de `uniqueItems` sobre ~16.618 objectos. Com a regra removida do array raiz, o harness real volta a tempos compatíveis com I/O e SQL locais.

### Limitações desta evidência

- Execução local e opt-in; CI continua a ignorar os testes reais na ausência de `PHC_KNOWLEDGE_REAL_IKP_PATH`.
- A simulação de interrupção cria o estado persistido pós-arquivo; não mata um processo do SO.
- Não prova kinds ausentes na captura, estabilidade entre capturas nem o validador Python independente end-to-end como substituto deste harness.
