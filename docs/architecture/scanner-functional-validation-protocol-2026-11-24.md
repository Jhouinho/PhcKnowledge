# Protocolo de validação funcional do PHC Knowledge Scanner

**Data:** 2026-11-24  
**Repositório:** `Jhouinho/PhcKnowledge`  
**Branch de referência:** `main`  
**Estado:** procedimento preparado contra o código actual. As duas recolhas reais ainda não foram executadas. Este documento não declara a validação funcional concluída.

## 1. Objectivo

Executar duas recolhas reais com exactamente a mesma versão do Scanner, guardar os pacotes IKP em separado e só depois comparar os resultados.

1. **CS default** — primeira base e referência desta validação. Serve para observar o comportamento actual do Scanner e confrontá-lo com o histórico documental disponível, em especial o contrato D-022, `DefinitionFingerprint`, `MetadataFingerprint`, `ObjectKind` e a cobertura das operações.
2. **Base PHC com desenvolvimento** — segunda base, escolhida pelo operador. Serve para encontrar personalizações e estruturas que a CS default não represente.

A matriz estática [Collector / Operation / ObjectKind](collector-operation-objectkind-matrix-2026-11-24.md) descreve o que o código declara. Não substitui este protocolo e não é evidência de que uma operação tenha corrido.

Não alterar collectors, queries SQL, schema SQLite, contratos de captura nem a semântica de recolha antes de as duas recolhas existirem e de haver decisão explícita sobre o que corrigir. Não criar dados artificiais para simular uma captura.

## 2. Versão do Scanner a utilizar

O comando `version` imprime `ScannerVersion.Current`, lido de `AssemblyInformationalVersion` do assembly `PhcKnowledgeScanner.Engine`. No SDK .NET 10 esta cadeia inclui o commit Git compilado.

No commit `ccec3624ffc19c98c943a3a45f6afc260ac8a35a`, antes deste documento, o comando imprimiu:

```text
1.0.0+ccec3624ffc19c98c943a3a45f6afc260ac8a35a
```

Este documento não altera código do Scanner. Um rebuild limpo noutro commit passa a imprimir `1.0.0+<sha desse commit>`. A versão que conta para as duas recolhas é a cadeia impressa no momento da execução, não um número fixo separado do Git.

Regras:

- Fazer checkout do commit que contém este protocolo e confirmar a working tree limpa.
- Correr `--version` uma vez e registar a cadeia completa.
- Usar essa mesma working tree, sem pull, commit ou rebuild a partir de outro HEAD, nas duas recolhas.
- No pacote produzido por `scan-ikp`, `manifest.json` → `capture.scannerVersion` tem de ser igual a essa cadeia e `capture.metadataStatus` tem de ser `Complete`.
- `manifest.json` → `producer.productVersion` é a versão de assembly de três componentes (`Assembly.GetName().Version`, hoje `1.0.0`). Não a usar como identidade da build. A identidade é `capture.scannerVersion`.

Requisito de SDK: .NET SDK 10.0.403 ou posterior compatível com `global.json`, a partir da raiz do monorepo.

```powershell
git rev-parse --abbrev-ref HEAD
git rev-parse HEAD
git status --porcelain
dotnet run --project PhcKnowledgeScanner/src/PhcKnowledgeScanner.Cli --no-launch-profile -- --version
```

`git status --porcelain` tem de ficar vazio. A branch tem de ser `main`.

## 3. O que o código executa

O pacote que o PHC Knowledge analisa é o ZIP IKP 0.1 produzido pelo comando CLI `scan-ikp`.

| Comando | O que produz | Usar nesta validação |
|---|---|---|
| `scan-ikp` | Recolha completa e ZIP IKP 0.1 no mesmo processo, com o `ScanManifest` em memória | Sim |
| `export-ikp` | IKP a partir de um workspace já gravado | Não, como pacote principal. O CLI passa versão, início, fim e modo a nulo, pelo que `capture.scannerVersion` fica `Unknown` e `capture.metadataStatus` fica `Partial` |
| `scan` | ZIP de personalizações PHC em pastas fixas (`WorkspaceExportService`), não o pacote IKP | Não |
| Desktop, exportação actual | O mesmo ZIP de personalizações, via `ExportCurrentAsync` | Não. A aplicação Desktop não chama `IkpPackageExporter` |
| `schema-snapshot` | Snapshot estrutural à parte, com regras de privacidade próprias | Não faz parte destas duas recolhas |

`scan-ikp` sem `--collectors` usa `ScanMode.Full` e pede as 23 operações do catálogo: 7 de estrutura SQL, 14 de personalizações PHC e 2 de REL/DTL. Não passar `--collectors`. Uma operação não pedida fica com resultado `Skipped` e não conta como recolha completa.

A ligação é construída em `SqlConnectionStringBuilderService`: `Encrypt=true`, `ApplicationIntent=ReadOnly`, `ApplicationName=PhcKnowledgeScanner`, timeout por omissão 15 segundos. `TrustServerCertificate` fica `true` porque `SqlConnectionRequest` tem esse valor por omissão e a CLI não expõe opção para o alterar. A password não é escrita em logs: o log seguro contém servidor, base, modo de autenticação e utilizador.

Parâmetros de ligação aceites pela CLI:

- `--server` e `--database` são obrigatórios.
- `--auth windows` é o valor por omissão. `--auth sql` exige `--user`.
- A password é lida de `PHC_SCANNER_SQL_PASSWORD`. A opção `--password` existe no código e não deve ser usada: ficaria no histórico e na linha de comandos.
- Não há parâmetros de workspace, timeout, `TrustServerCertificate` nem etiqueta de servidor no `scan-ikp`.

O destino IKP da origem, em `scan-ikp`, recebe apenas `DatabaseName`. `serverLabel`, `originFingerprint`, `phcVersion` e `phcSignatureDetected` não são preenchidos. O servidor tem de ser registado pelo operador fora do pacote. No SQLite, `scan_meta.installation_label` guarda `{servidor}/{base}`.

## 4. Onde ficam os artefactos

`scan-ikp` usa sempre a mesma pasta:

```text
%TEMP%\PhcKnowledgeScanner\cli-ikp-workspace
```

Dentro ficam `workspace.db` e `artifacts\`. No início de cada recolha, `InitializeAsync` apaga as linhas de `objects`, `artifacts`, `issues`, `relations`, `declared_relations`, `declared_relation_interpretations` e `scan_meta`. Não apaga os ficheiros já existentes em `artifacts\`.

Consequências:

- A segunda recolha destrói no SQLite o conteúdo da primeira se ambas usarem esta pasta.
- Uma cópia da pasta pode conter ficheiros de uma execução anterior que já não estão referenciados na base. Esses ficheiros não entram no ZIP: o exportador só inclui artefactos referenciados pela captura corrente.
- O pacote a entregar é o ZIP indicado em `--output`, não a pasta temporária.

Guardar cada ZIP, o log da consola e uma cópia do workspace **fora do repositório Git**, em pastas separadas. `*.zip` está no `.gitignore`, mas logs e notas dentro da árvore do repositório podem ser commitados por engano. Não colocar passwords, connection strings nem estes artefactos em ficheiros versionados.

Pastas sugeridas, noutro directório que não seja o clone:

```text
<PASTA_LOCAL>\cs-default\
<PASTA_LOCAL>\phc-desenvolvimento\
```

Cada uma deve receber:

- `*.ikp.zip` — pacote a analisar;
- `scan-ikp-console.log` — stdout/stderr da execução;
- `workspace\` — cópia feita **depois** dessa recolha e **antes** da seguinte;
- uma nota local, não versionada, com servidor, nome da base, utilizador sem password, data, `git rev-parse HEAD` e a cadeia de `--version`.

## 5. Preparar a sessão

Definir a password só na sessão actual de PowerShell e nunca a gravar. Limpar a variável no fim.

Autenticação SQL:

```powershell
$env:PHC_SCANNER_SQL_PASSWORD = "<definir localmente nesta sessão>"
```

Autenticação Windows: não definir password e usar `--auth windows`, ou omitir `--auth`.

Se já existir `%TEMP%\PhcKnowledgeScanner\cli-ikp-workspace` de uma execução anterior, renomear essa pasta antes da primeira recolha desta validação. O CLI cria a pasta de novo. Isto só isola ficheiros antigos; não altera a recolha.

Não passar `--collectors`. Não usar `--password`.

## 6. Execução A — CS default

Substituir `<SERVIDOR>`, `<BASE_CS_DEFAULT>`, `<UTILIZADOR>` e `<PASTA_LOCAL>` pelos valores locais. A base de referência histórica documentada no catálogo standard é `CSDefault202603`; confirmar que é mesmo a base a que se está a ligar. O nome que ficar em `--database` é o que o pacote grava em `origin.databaseName`.

Teste de ligação:

```powershell
dotnet run --project PhcKnowledgeScanner/src/PhcKnowledgeScanner.Cli --no-launch-profile -- test-connection --server "<SERVIDOR>" --database "<BASE_CS_DEFAULT>" --auth sql --user "<UTILIZADOR>"
```

Para Windows, trocar `--auth sql --user "<UTILIZADOR>"` por `--auth windows`.

Recolha e pacote IKP:

```powershell
$dest = "<PASTA_LOCAL>\cs-default"
New-Item -ItemType Directory -Force -Path $dest | Out-Null
$zip = Join-Path $dest "cs-default.ikp.zip"
$log = Join-Path $dest "scan-ikp-console.log"
dotnet run --project PhcKnowledgeScanner/src/PhcKnowledgeScanner.Cli --no-launch-profile -- scan-ikp --server "<SERVIDOR>" --database "<BASE_CS_DEFAULT>" --auth sql --user "<UTILIZADOR>" --output $zip 2>&1 | Tee-Object -FilePath $log
$LASTEXITCODE
```

Se `$LASTEXITCODE` não for 0, a recolha ou a exportação falhou. Não iniciar a segunda base. Conservar o log. Se a excepção ocorrer depois do scan e antes do ZIP, a pasta temporária pode ainda ter o workspace dessa tentativa.

Se terminar com 0, a consola escreve o caminho do ZIP, `PackageId`, `CaptureId`, SHA-256 do ZIP, logical content digest e contagens de ficheiros. Estas linhas não listam o resultado de cada operação. Código 0 também não prova que todas as operações tenham tido sucesso: uma operação pode ficar `Failed`, `Partial`, `Empty` ou `NotPresent` e o processo continuar, desde que a projecção do Object Index não seja rejeitada.

Copiar o workspace imediatamente:

```powershell
$workspace = Join-Path $env:TEMP "PhcKnowledgeScanner\cli-ikp-workspace"
Copy-Item -Recurse -Force $workspace (Join-Path $dest "workspace")
```

Confirmar no ZIP, sem o alterar:

- `manifest.json`: `ikpContractVersion` = `0.1`, `capture.mode` = `Full`, `capture.metadataStatus` = `Complete`, `capture.scannerVersion` igual ao `--version`, `origin.databaseName` igual a `<BASE_CS_DEFAULT>`.
- `object-index.json`: `schemaVersion` = `2`.
- `capabilities.json` e `coverage.json` presentes.
- `checksums.json` presente.

O histórico `CSDefault202603` (16.618 objectos, 8 de 21 kinds, 49 definições, 921 factos REL/DTL na documentação anterior) é uma captura de uma versão anterior do Scanner. Pode ser usado como referência qualitativa se o operador ainda o tiver fora do repositório. Não é o pacote desta validação, não está no Git e não prova o comportamento D-022. Não o substituir ao ZIP agora produzido.

## 7. Execução B — base PHC com desenvolvimento

Repetir no mesmo checkout, com a mesma cadeia de `--version` e sem `--collectors`. Mudar apenas servidor e base, se a segunda instalação for outra, e o directório de saída.

```powershell
$dest = "<PASTA_LOCAL>\phc-desenvolvimento"
New-Item -ItemType Directory -Force -Path $dest | Out-Null
$zip = Join-Path $dest "phc-desenvolvimento.ikp.zip"
$log = Join-Path $dest "scan-ikp-console.log"
dotnet run --project PhcKnowledgeScanner/src/PhcKnowledgeScanner.Cli --no-launch-profile -- scan-ikp --server "<SERVIDOR_2>" --database "<BASE_PHC_DESENVOLVIMENTO>" --auth sql --user "<UTILIZADOR_2>" --output $zip 2>&1 | Tee-Object -FilePath $log
$LASTEXITCODE
$workspace = Join-Path $env:TEMP "PhcKnowledgeScanner\cli-ikp-workspace"
Copy-Item -Recurse -Force $workspace (Join-Path $dest "workspace")
```

A segunda execução reutiliza a pasta temporária e apaga o SQLite da CS default. A cópia feita na Execução A é a única preservação do workspace da primeira base.

No fim:

```powershell
Remove-Item Env:PHC_SCANNER_SQL_PASSWORD -ErrorAction SilentlyContinue
```

Entregar os dois ZIP. Não os commit. Identificar cada um por `PackageId`, `CaptureId`, SHA-256 impresso na consola e `origin.databaseName`. Os dois `capture.scannerVersion` têm de ser iguais entre si e iguais ao `--version` registado.

## 8. Conteúdo do ZIP a comparar

Ficheiros gerados pelo `IkpPackageExporter` quando a exportação tem sucesso:

| Caminho | Uso na comparação |
|---|---|
| `manifest.json` | Versão do Scanner, modo, estado dos metadados da captura, nome da base, `packageId`, `captureId` |
| `capabilities.json` | Operações declaradas e versão de cada capability |
| `coverage.json` | Operações efectivamente pedidas e o resultado gravado. `schemaVersion` emitido pelo exportador actual é `1` |
| `object-index.json` | Objectos projectados, ObjectKind, operação, identidade, fingerprints e estado da definição. `schemaVersion` = `2` |
| `checksums.json` | Integridade dos ficheiros do pacote e logical content digest |
| `payload/diagnostics/issues.json` | Avisos e erros de recolha |
| `payload/diagnostics/object-index-projection.json` | Só existe se a projecção tiver produzido diagnósticos |
| `payload/relations/declared-relations.json` | Factos REL/DTL e interpretações. Só é escrito quando há relações ou a cobertura pedida de REL/DTL está `Available`, `Partial` ou `Empty` |
| `payload/artifacts/...` | Bytes das definições referenciadas pelos objectos |

`coverage.json` é reconciliado na exportação: se uma operação estava `Available` e algum objecto ficou com definição `Unavailable`, o exportador pode passar o conteúdo a `Partial` e um resultado `Succeeded` a `SucceededWithWarnings`. Comparar o ficheiro do ZIP, não uma suposição feita só a partir do catálogo.

`scan-ikp` não imprime a lista `CollectorId: Status` que o comando `scan` imprime. A fonte do resultado por operação é `coverage.json`.

## 9. Verificações depois de existirem os dois pacotes

Estas verificações ainda não foram feitas. Só se aplicam aos ZIP produzidos por este protocolo.

Para cada pacote, e depois entre os dois:

1. **Identidade da execução.** Mesmo `capture.scannerVersion` e mesmo commit. `capture.mode` = `Full`. `metadataStatus` = `Complete`. Bases diferentes em `origin.databaseName`. Servidor confirmado na nota local ou em `scan_meta.installation_label` da cópia do workspace, porque o manifesto IKP não traz `serverLabel`.
2. **Operações declaradas e executadas.** Cruzar as 23 operações de `capabilities.json` com `coverage.json`. Campos a ler em cada operação: `operationId`, `capabilityVersion`, `requested`, `result`, `sourceAvailability`, `contentAvailability`, `observedCount`, `issueIds` e `scope` quando existir. `requested` tem de ser verdadeiro nas 23. Triggers usam capability `2.0`; as restantes operações do catálogo usam `1.0`.
3. **Estado de cada operação.** `result` pode ser `Succeeded`, `SucceededWithWarnings`, `Partial`, `Failed`, `Skipped` ou `NotAttempted`. `sourceAvailability` pode ser `Present`, `NotPresent`, `NotApplicable` ou `Unknown`. `contentAvailability` pode ser `Available`, `Partial`, `Unavailable`, `NotCollected` ou `Empty`.
4. **Classificação obrigatória de ausências.** Separar, com o trio resultado / origem / conteúdo e os issues:
   - ausência legítima de dados: origem presente, execução com sucesso e conteúdo `Empty` ou `observedCount` = 0;
   - estrutura não aplicável: `sourceAvailability` = `NotApplicable` ou `NotPresent` numa operação opcional, sem erro de execução;
   - operação não executada: `requested` = false, `Skipped` ou `NotAttempted`;
   - falha de recolha: `Failed`, `Unavailable` com issue, ou excepção no log;
   - dados recolhidos sem fingerprint semântico: definição `Available` e `definitionFingerprint.status` = `Unavailable` com `reasonCode` = `NoValidatedPolicy`.
5. **ObjectKinds.** Contar `objectKind` em `object-index.json`. A matriz estática mapeia 21 kinds para operações. Os kinds `schema`, `column`, `index` e `phc.campo_definition` não têm operação no resolver; a ausência deles no índice não é, por si, falha. Um kind presente tem de ter `operationId` coerente com a matriz e, nos objectos PHC, `metadata` alinhado com a variante `source` que o resolver exige.
6. **Quantidade e diversidade.** Comparar contagens por `objectKind` e por `operationId`. A base com desenvolvimento deve ser lida à procura de kinds ou variantes em falta na CS default, não como um mínimo numérico obrigatório.
7. **DefinitionFingerprint (D-022).** Para `definition.status` = `Available`, o envelope é obrigatório. Sem política de produção aprovada, o comportamento actual é `status` = `Unavailable` e `reasonCode` = `NoValidatedPolicy`, sem `algorithm`, `value` ou `policy`. Isso não torna a definição indisponível nem falha a operação. Para `Empty`, `Unavailable` e `NotCollected`, a propriedade `definitionFingerprint` é omitida. Contar os quatro estados de definição e os `reasonCode` observados. Um `Available` com algoritmo e política seria inesperado neste código, porque não há política semântica de produção.
8. **MetadataFingerprint.** Contar objectos com e sem `metadataFingerprint`. A omissão é válida quando a projecção não tem campos semânticos aprovados para o kind; distingue-se de um diagnóstico em `payload/diagnostics/object-index-projection.json`. Não tratar a omissão como falha de DefinitionFingerprint.
9. **Object Index e proveniência.** Verificar `sourceObjectId` único, `sourceIdentity` (`scheme`, `value`, `scope`) quando existir, `operationId`, `collector` implícito pela operação, `parentSourceObjectId` e `captureId` do documento. `PackageId` e `CaptureId` identificam o transporte, não a identidade lógica do objecto.
10. **Personalizações PHC.** Regras, eventos, valores por defeito, campos `u_*`, ecrãs, grelhas e scripts, incluindo variantes web opcionais: `phc.user_rule`, `phc.web_user_rule`, `phc.hotkey`, `phc.default_value`, `phc.web_default_value`, `phc.event`, `phc.web_event`, `phc.user_field`, `phc.web_script`, `phc.user_javascript`, `phc.web_cssjs`, `phc.hidden_grid`, `phc.custom_screen`.
11. **Funções `Fx_*` e `User_*`.** Pertencem à operação `phc.customizations.ftu` e ao kind `phc.user_tsql_function`, com origem `ftu` ou `sys.objects`. Confirmar no pacote qual das origens apareceu. A ausência de `dbo.ftu` não prova que a descoberta em `sys.objects` tenha corrido, nem o inverso: ler `scope`, `result` e os objectos.
12. **Estrutura SQL.** `database`, `table`, `primary_key`, `foreign_key`, `view`, `stored_procedure` e `trigger`. A operação de triggers declara âmbito restrito a triggers DML de tabelas; não a ler como inventário de todos os triggers.
13. **REL/DTL.** Continuam a ser recolhidos pelo código actual e comparados mais tarde com o catálogo PHC Standard. São factos em `payload/relations/declared-relations.json`, não ObjectKinds. Comparar contagens, `sourceType`, estado de interpretação e diagnósticos. Ausência do ficheiro só é aceitável quando a regra de emissão do exportador não se verifica; cruzar com a cobertura de `phc.declared-relations.rel` e `phc.declared-relations.dtl`.
14. **Avisos, erros e diagnóstico.** Ler `issues.json`, o log da consola e, se existir, `object-index-projection.json`. Associar cada issue a `operationId`, `severity`, `code` e `objectName`.
15. **Diferenças entre as bases.** Registar kinds, operações e estados que existam só na base com desenvolvimento. Não converter essa diferença em alteração de collector neste passo.
16. **Histórico da CS default.** Se for comparado com os números documentados de `CSDefault202603`, descrever a diferença e lembrar que o Scanner não é o mesmo. Não classificar a diferença como regressão sem o ZIP antigo e sem o ZIP novo lado a lado.

Uma operação com zero objectos não é falha. Uma definição sem fingerprint semântico não é objecto em falta. Uma operação declarada no código não foi executada só porque existe em `capabilities.json`: a prova é `coverage.json` com `requested` = true e um `result` diferente de `Skipped` e `NotAttempted`.

## 10. Limitações actuais, sem alteração de código

- Não existe parâmetro para escolher a pasta de workspace do `scan-ikp`. As duas recolhas têm de ser separadas por cópia externa.
- O IKP de `scan-ikp` não transporta o servidor, a versão PHC nem um fingerprint de origem. A identificação da instalação no pacote é o nome da base.
- `export-ikp` não recupera a versão do Scanner a partir do SQLite.
- Desktop e o comando `scan` não produzem o IKP 0.1.
- A consola de `scan-ikp` não substitui `coverage.json`.
- Não há política semântica de produção: definições disponíveis devem sair com fingerprint `Unavailable` / `NoValidatedPolicy`.
- O coverage emitido usa `schemaVersion` `1`. Métricas de coverage `schemaVersion` 2 descritas na especificação não são escritas por este exportador.
- A captura histórica `CSDefault202603` não valida esta build.

Qualquer problema observado nas recolhas fica registado como observação, com o ZIP, o log e o excerto de código. Só depois se propõem alterações ao Scanner.
