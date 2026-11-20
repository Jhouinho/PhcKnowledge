# Auditoria da captura PHC Standard — CSDefault202603

> Auditoria read-only da captura real de 16-11-2026. Distingue os dados observados, as definições transportadas e as lacunas de cobertura. Não aprova alterações ao Scanner nem ao contrato IKP.

- Repositório consultado: `Jhouinho/PhcKnowledge`
- Branch do repositório consultada: `feature/increment-c-package-import-analysis-confirmation`
- HEAD do repositório no início da auditoria: `e81c10d80c63414080a64cb5c93ab63a3dd47c31`
- Branch/commit que produziu a captura: `feature/ikp-0.1-export-validate-integration` / `5e3308182e57486732b659b39664bcec8c0d99ed`
- Base de referência: `CSDefault202603`
- Captura UTC: `2026-11-16 18:15:46.978` a `18:15:52.243`
- CaptureId: `49826eeb-0388-48bb-a495-2031f30a58a4`
- PackageId: `b0b7e61f-8ea4-4cf0-a57d-99d37ab220ea`
- Scanner: `1.0.0+5e3308182e57486732b659b39664bcec8c0d99ed`

## 1. Proveniência e integridade

O ficheiro `PHC-Knowledge-Real-Capture.zip`, enviado em 16-11, contém o diretório `run-20261116-181545`, o ZIP IKP, `workspace.db`, a pasta de artefactos, `object-index.json`, `coverage.json`, `capabilities.json`, `verification.json`, logs e relatório da captura.

A cópia enviada em 18-11 é byte a byte igual à de 16-11 (SHA-256 do ZIP exterior: `1c1270b23b30aa7ddd73320ec3d0bafe36df024fcfd4840195c96b364c5df8c4`). Foi usada uma única execução lógica, sem duplicar contagens.

O ZIP exterior e o ZIP IKP interno passam a verificação estrutural. Os 56 hashes declarados em `checksums.json` foram verificados sem divergências. A verificação independente registada no pacote indica:
- 16.618 objetos no SQLite e no Object Index;
- zero `sourceObjectId` duplicados;
- 52 artefactos no manifesto e 52 ficheiros no payload;
- 49 artefactos associados no workspace/SQLite;
- zero referências de artefacto em falta;
- 921 relações declaradas e 921 interpretações persistidas no workspace;
- 6 issues, todas relativas a definições de procedures indisponíveis.

O `workspace.db` isolado existente na Biblioteca, com data de 08-11, é anterior e não foi usado para representar esta captura. Foi usado o workspace que vem dentro do ZIP de 16-11.

## 2. Inventário efetivamente capturado

| ObjectKind | Quantidade |
|---|---:|
| database | 1 |
| table | 8.294 |
| primary_key | 8.268 |
| stored_procedure | 36 |
| phc.hidden_grid | 11 |
| phc.user_tsql_function | 6 |
| phc.custom_screen | 1 |
| phc.hotkey | 1 |
| **Total** | **16.618** |

Estado das definições no Object Index:
- `NotCollected`: 16.563;
- `Available`: 49;
- `Unavailable`: 6.
- Metadata fingerprints: 16.581.
- Definition fingerprints: 49.
- Source identities: 8.307.

A indicação `metadataStatus=Complete` no manifesto não significa que as definições de todos os objetos tenham sido recolhidas. Significa que a captura concluiu a recolha de metadados segundo as operações executadas. A maioria dos objetos não tem definição-fonte associada.

## 3. Cobertura das operações

### SQL Server Structure

| Operação | Resultado | Observados |
|---|---|---:|
| database | Succeeded | 1 |
| tables.inventory | Succeeded | 8.294 |
| primary_keys | Succeeded | 8.268 |
| foreign_keys | Succeeded / vazio | 0 |
| views | Succeeded / vazio | 0 |
| procedures | SucceededWithWarnings / parcial | 36 |
| triggers | Succeeded / vazio | 0 |

As 8.294 tabelas têm metadados de identificação, tipo, classificação provisória e datas de criação/alteração. O `summary_json` das tabelas não contém colunas. Os objetos `primary_key` incluem apenas as colunas que compõem cada chave primária; não substituem um inventário integral de campos.

As seis procedures sem definição disponível são:
- `dbo.usp_Act_Extratos_Bancarios`
- `dbo.usp_aicfgsales`
- `dbo.usp_LimparTabelasTemporariasSpid`
- `dbo.usp_prdmrat`
- `dbo.usp_RepararUserDboOrfao`
- `dbo.usp_stinvclc`

As 30 definições SQL transportadas correspondem a procedures `dbo.dt_*` de suporte a diagramas/source control do SQL Server. Não devem ser confundidas com definições de negócio PHC.

### PHC Customizations

| Operação | Observados |
|---|---:|
| brule | 0 |
| ebrule | 0 |
| teclas | 1 |
| udefs | 0 |
| eudefs | 0 |
| eventos | 0 |
| eeventos | 0 |
| user_fields | 0 |
| escr | 0 |
| jsu | 0 |
| cssjs | 0 |
| formc | 11 |
| formb | 1 |
| ftu | 6 |

As 19 definições PHC transportadas distribuem-se por FORMB (1), FORMC (11), FTU (6) e teclas (1). Devem ser preservadas como evidência da base de referência; a sua finalidade semântica continua sujeita à matriz de artefactos já em auditoria.

### REL / DTL

A captura contém 295 registos `REL` e 626 registos `DTL`, num total de 921 relações declaradas. O workspace tem 921 interpretações persistidas. A tabela `relations` de relações derivadas tem zero registos.

Por D-015, REL/DTL pertencem ao PHC Standard Catalog, como conhecimento comum versionado, e não ao conjunto de personalizações a recolher integralmente em cada captura futura. Estes dados da CS Default são uma fonte de referência para a primeira construção do catálogo.

## 4. Diferença entre o pacote de 10-11 e o de 16-11

Foi também encontrado o pacote IKP anterior de 10-11. Os dois pacotes têm 16.618 objetos e os mesmos 8 tipos de objeto, sem diferenças nos registos do Object Index.

Há, contudo, diferenças no payload:
- pacote de 10-11: 53 artefactos; inclui `declared-relation-interpretations.json` e `collection-issues.json`;
- pacote de 16-11: 52 artefactos; inclui `issues.json`, mas não inclui um artefacto separado `declared-relation-interpretations.json`.

No pacote de 16-11, as 921 interpretações existem no `workspace.db`, mas não são transportadas como ficheiro autónomo no ZIP IKP. A verificação que conta 921 interpretações está a consultar o workspace; não prova que estejam no payload IKP. Esta diferença deve ser reconciliada com D-015 e com o contrato de exportação, sem assumir que é intencional ou que é uma falha antes de rever o desenho.

## 5. Outras fontes standard já existentes

O ficheiro `docs/sources/triggers phc.sql` contém 552 instruções `CREATE TRIGGER`. Foi indicado pelo responsável como fonte de triggers standard PHC e o extrator existente em `PhcKnowledge.StandardCatalog` já tem parser e testes para este tipo de fonte.

A operação `sql.server.structure.triggers` da captura de 16-11 observou zero triggers na base de dados. Portanto, o ficheiro SQL e a observação da base de dados são fontes distintas: o ficheiro é evidência do catálogo standard; a captura não confirma que essas 552 triggers estejam instaladas na base consultada.

## 6. Lacuna estrutural e capacidade existente

A captura IKP de 16-11 não contém um snapshot estrutural completo. Não há inventário de colunas, índices, chaves estrangeiras, views ou triggers no Object Index/payload desta captura. A listagem de tabelas e PK não permite reconstruir tipos, comprimentos, nulabilidade, defaults, colunas calculadas ou índices.

No código da branch consultada existe uma funcionalidade separada `Database Schema Snapshot`. As consultas/modelos já suportam:
- tabelas e colunas (tipo SQL, comprimento, precisão, escala, nulabilidade, identity, defaults, colunas calculadas e collation);
- PK, FK e índices;
- views, triggers, procedures, funções e parâmetros;
- tipos definidos pelo utilizador e synonyms;
- assinatura PHC e versão do SQL Server.

Esta funcionalidade é auxiliar e não faz parte da captura IKP analisada. O ZIP de 16-11 não contém um ficheiro de schema snapshot autónomo. A classificação estrutural existente é conservadora (`sql_system`, `candidate_custom` por prefixo `u_`, ou `unknown`); não identifica, por si só, objetos PHC standard. A CS Default continua a ser a baseline que permite fazer essa classificação, não uma regra heurística baseada apenas no nome.

O manifesto IKP regista `versionProperty=957` no metadado da base, mas não fornece uma versão/build PHC legível e validada. Não mapear o valor 957 para uma versão comercial sem fonte de confirmação.

## 7. Conclusões e próximo trabalho

1. A captura de 16-11 é válida e íntegra como evidência do inventário que efetivamente recolheu; não é um catálogo físico completo.
2. Podemos aproveitar já as tabelas, PK, os 921 factos REL/DTL, as 19 definições PHC e a fonte independente de 552 triggers, mantendo proveniência e tipo de fonte.
3. Não classificar automaticamente as 8.294 tabelas como PHC standard só porque aparecem na CS Default; a classificação deve ser vinculada à versão/aplicabilidade da baseline e à cobertura comprovada.
4. Antes de fechar o catálogo estrutural, reconciliar o snapshot de colunas/índices/FK com os ficheiros já fornecidos. Não pedir uma nova execução enquanto não se confirmar se existe outro artefacto já enviado.
5. Rever a diferença do payload de interpretações REL/DTL entre os pacotes de 10-11 e 16-11.
6. Identificar a versão PHC aplicável à CS Default e registar a origem da confirmação.

## 8. Limites desta auditoria

- Inspeção read-only dos ZIPs, manifestos, Object Index, coverage, checksums, workspace SQLite e código existente.
- Nenhum código, schema, migration, collector ou teste foi alterado.
- Não foram executados builds, testes ou consultas à base SQL Server.
- Não foi publicado qualquer dado de cliente; o relatório regista apenas metadados e contagens da fonte de referência.
