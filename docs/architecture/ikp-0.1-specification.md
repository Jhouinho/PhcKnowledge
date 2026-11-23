# IKP 0.1 — Installation Knowledge Package


> **Decisão de ciclo de vida — 21-11-2026:** o ZIP IKP é apenas um contentor temporário de transporte. Não é arquivo permanente do Knowledge. Após materialização integral e verificada, o ZIP e o staging são eliminados; se a materialização falhar ou ficar incompleta, o pacote mantém-se disponível para retry. Dados estruturados, histórico e artefactos originais necessários são persistidos no SQL Server central. Ver [Decisões arquitecturais aprovadas](decisoes-aprovadas-2026-11-21.md).

| Campo | Valor |
|---|---|
| Data da revisão | 2026-11-17 |
| Contrato | ikpContractVersion = 0.1 |
| Content-Type | phc.installation.knowledge.package |
| Transporte canónico | ZIP |
| Estado | **Contrato IKP 0.1 consolidado; emissor no Scanner; primeira fatia Import no Knowledge implementada** |
| Implementação | Exportador no Scanner e serviço Receive / Validate / Archive implementados; API HTTP e materialização por implementar |
| Contrato complementar normativo | [P1 — Identidade, proveniência e fingerprints](p1-identity-provenance-contract.md) |
| Contexto arquitetural | [Contrato de observação, identidade e linhagem](observation-identity-and-lineage-contract.md) |

Este documento define o pacote de transferência entre o Scanner PHC independente e a plataforma PHC Knowledge. Distingue o contrato pretendido da capacidade atualmente existente no Scanner. O Scanner já emite IKP 0.1 através de `IkpPackageExporter` e dos comandos CLI `scan-ikp` e `export-ikp`. O núcleo PhcKnowledge.Import já implementa validação, arquivo imutável e receipt SQL; a integração HTTP/autenticação e a materialização de conhecimento ainda não existem. Para a semântica normativa de SourceObjectId, SourceIdentity, estados individuais de definição, proveniência e fingerprints, prevalece o [contrato P1 de identidade e proveniência](p1-identity-provenance-contract.md); este documento define a sua representação no pacote.

## 1. Objetivo e fronteiras

O IKP é o formato normalizado que o PHC Knowledge aceita como entrada de conhecimento técnico de uma instalação PHC.

Responsabilidades:

- **Scanner:** recolhe factos e definições técnicas, identifica objetos na origem, declara capacidades e cobertura, preserva artefactos originais e produz o IKP.
- **IKP:** transporta uma captura imutável, com identidade de captura, capacidades, cobertura, índice de objetos, artefactos, integridade e proveniência.
- **PHC Knowledge:** associa o pacote a uma instalação, valida e importa, mantém entidades e histórico, calcula/compara observações, gere correspondências de identidade e linhagem.

O PHC Knowledge não executa o Scanner em runtime. O Scanner não depende do PHC Knowledge nem dos seus identificadores internos.

O IKP parte do Database Schema Snapshot atual, mas não é a junção direta dos três ZIPs existentes nem a recuperação do formato histórico do ConsoleApp.

## 2. Princípios obrigatórios

1. Uma captura pode conter categorias opcionais; a ausência de uma categoria não significa que não existam objetos.
2. Cobertura, capacidades suportadas e inventário de objetos são dimensões distintas.
3. Ausência de um objeto só pode fundamentar remoção após comparação compatível e cobertura completa.
4. O Scanner produz observações; a plataforma mantém entidades e histórico.
5. Identidade lógica, identidade nativa da origem, identidade da observação e fingerprints são conceitos separados.
6. Duas bases de dados PHC diferentes são duas instalações diferentes, mesmo que uma resulte de cópia/restauro da outra.
7. Linhagem regista proveniência entre instalações; nunca funde os seus históricos.
8. O pacote não transporta o InstallationId interno da plataforma.
9. Artefactos originais e factos declarados não devem ser substituídos por interpretações derivadas.
10. Receber ou validar um pacote não equivale a publicar conhecimento.

## 3. Estrutura do pacote

### 3.1 Layout ZIP

~~~text
{packageId}.zip
├── manifest.json
├── capabilities.json
├── coverage.json
├── object-index.json
├── checksums.json
└── payload/
    ├── schema/
    │   ├── source-manifest.json
    │   ├── tables/
    │   ├── relationships/
    │   ├── indexes/
    │   ├── views/
    │   ├── triggers/
    │   ├── procedures/
    │   ├── functions/
    │   ├── types/
    │   ├── synonyms/
    │   └── documentation/
    ├── customizations/
    ├── relations/
    ├── knowledge/
    └── diagnostics/
~~~

Ficheiros de topo obrigatórios: manifest.json, capabilities.json, coverage.json, object-index.json e checksums.json.

Pastas de payload são opcionais, mas devem ser coerentes com as capacidades, cobertura, índice de objetos e manifesto. O manifesto do Schema Snapshot original é preservado em payload/schema/source-manifest.json, quando esse perfil é incluído. O manifesto principal IKP é único e não deve duplicar campos de identidade da captura.

### 3.2 Perfis de payload

| Perfil | Conteúdo | Regra |
|---|---|---|
| schema | Schema físico SQL: tabelas, colunas, PK/FK, índices, views, procedures, triggers, funções, tipos e synonyms | Opcional |
| customizations | Definições e metadados de personalizações PHC | Opcional |
| relations | Factos REL/DTL/FK e interpretações versionadas | Opcional |
| knowledge | Projeções derivadas, como KR/FieldMapping | Opcional e regenerável |
| diagnostics | Issues e diagnósticos adicionais | Opcional |

O pacote pode transportar apenas um perfil ou vários. A ausência de knowledge não significa ausência de factos relacionais. As projeções devem ser identificadas como derivadas, com versão do analisador e referências às evidências de origem.

### 3.3 Extensibilidade

- Categorias e operações novas são declaradas em capabilities.json e coverage.json.
- Novas pastas de payload podem ser adicionadas sem alterar a semântica dos perfis existentes.
- Consumidores devem ignorar propriedades opcionais desconhecidas em estruturas extensíveis, mas não ignorar campos obrigatórios desconhecidos que afetem identidade, integridade ou interpretação.
- Uma versão de contrato incompatível é rejeitada; não se deve inferir compatibilidade apenas porque o JSON pode ser lido.

## 4. Manifesto principal

### 4.1 Campos

| Campo | Obrigatório | Significado |
|---|---:|---|
| ikpContractVersion | Sim | Versão do contrato, inicialmente 0.1 |
| contentType | Sim | phc.installation.knowledge.package |
| packageId | Sim | UUID global que identifica este pacote IKP físico; gerado pelo Scanner em cada exportação |
| producedAtUtc | Sim | Data/hora de criação do ZIP |
| producer.productId | Sim | Identificador do produto emissor, ex. PhcKnowledgeScanner |
| producer.productVersion | Sim | Versão do produto/exportador que produziu este pacote; não representa necessariamente a versão do Scanner que realizou a captura |
| producer.exportTool | Não | CLI, Desktop ou outro host |
| capture.captureId | Sim | UUID gerado pelo Scanner no início da recolha |
| capture.startedAtUtc | Condicional | Início da recolha; pode ser omitido/nulo quando o manifesto original não foi preservado |
| capture.completedAtUtc | Condicional | Fim da recolha; pode ser omitido/nulo quando desconhecido ou se a captura foi interrompida |
| capture.mode | Sim | Modo/escopo geral; usar Unknown quando não puder ser recuperado |
| capture.scannerVersion | Sim | Versão que realizou a captura; usar Unknown quando não puder ser recuperada |
| capture.metadataStatus | Sim | Complete ou Partial, conforme a disponibilidade dos metadados históricos |
| capture.unknownFields | Condicional | Lista dos campos históricos não disponíveis; não preencher com valores inferidos
| origin.engine | Sim | Motor de origem, nesta fase sqlserver |
| origin.databaseName | Recomendado | Nome técnico da base de dados, sujeito a política de privacidade |
| origin.serverLabel | Opcional | Etiqueta não sensível, não necessariamente FQDN |
| origin.originFingerprint | Opcional | Fingerprint técnico para ajudar a reconhecer a origem; nunca é InstallationId |
| origin.phcVersion | Opcional | Versão PHC detetada |
| origin.phcSignatureDetected | Opcional | Evidência/assinatura da deteção PHC |
| sharing.safeForPublicShare | Não | Declaração opcional do emissor, não certificação de segurança |
| sharing.assessment | Opcional | Informação sobre verificações realizadas e respetiva versão |

Não existe EnvironmentId no modelo. O utilizador não precisa de separar ambientes dentro da mesma instalação: cada base de dados registada é uma instalação própria. Uma base de dados de testes restaurada de produção é uma instalação diferente, ligada por linhagem.

### 4.2 Identificadores e âmbito

| Conceito | Origem | Regra |
|---|---|---|
| PackageId | Scanner | Identidade global do pacote físico; novo por cada exportação produzida |
| CaptureId | Scanner | Identifica uma recolha; pode originar vários pacotes |
| InstallationId | PHC Knowledge | Atribuído/confirmado na importação; nunca incluído como ID autoritativo no IKP |
| SourceObjectId | Scanner | Identificador do objeto na captura, referenciável dentro do pacote |
| SourceIdentity | Origem PHC/SQL | Identificadores nativos e respetivo âmbito |
| EntityIdentity | PHC Knowledge | Identidade lógica da entidade no âmbito da instalação |
| ObservationIdentity | PHC Knowledge | Entidade observada numa captura |
| MetadataFingerprint | Scanner/plataforma | Fingerprint dos metadados relevantes |
| DefinitionFingerprint | Scanner/plataforma | Fingerprint semântico da definição técnica, calculado segundo política versionada |

CaptureId e PackageId nunca entram na identidade lógica da entidade. O mesmo SHA-256 do ZIP implica os mesmos bytes e, num IKP válido, o mesmo PackageId do manifesto. Um CaptureId pode originar vários PackageIds. No modelo de destino do Knowledge, Package é global e PackageReceipt representa cada receção por Organization; a migration atual ainda mantém os índices de PackageId e ZipSha256 no âmbito da Organization. Stamps PHC, nomes SQL, object_id e column_id são evidências com âmbito e estabilidade diferentes; nenhum deve ser tratado como identificador universal.

### 4.2.1 Cálculo dos fingerprints

- Todos os fingerprints usam SHA-256 e hexadecimal minúsculo de 64 caracteres.
- MetadataFingerprint: SHA-256 dos bytes UTF-8 do objeto `metadata` serializado segundo JSON Canonicalization Scheme (RFC 8785). Excluir datas de recolha, timestamps voláteis, caminhos temporários e dados de execução que não descrevam o objeto. A política de inclusão de cada campo pertence ao contrato da respetiva categoria.
- O SHA-256 dos bytes exatos de cada artefacto original é o respetivo hash de integridade; qualquer diferença de bytes altera esse hash.
- DefinitionFingerprint: SHA-256 de uma representação canónica semântica da definição, com normalização específica por objectKind/variante, determinística, identificada e versionada. Só pode ignorar diferenças de apresentação quando a regra estiver validada como semanticamente segura. Sem normalizador seguro, não emitir fingerprint semântico. A representação transporta status, algorithm, value e policy.id/policy.version quando disponível; quando indisponível, transporta status e um reasonCode do catálogo controlado. A comparação exacta por bytes é uma operação separada e não preenche este campo.
- Se a definição for composta por vários artefactos, calcular o fingerprint composto sobre uma lista determinística ordenada por `artifactId`; cada entrada usa `artifactId + LF + componentDefinitionFingerprint + LF`, codificada em UTF-8, incluindo a versão da política de normalização no envelope canónico. O resultado é novamente hashed com SHA-256. Os hashes de integridade dos artefactos componentes continuam preservados separadamente.
- definition.status descreve a disponibilidade do conteúdo (Available, Empty, Unavailable, NotCollected) e é independente de definition.definitionFingerprint.status (Available, Unavailable). Quando a definição está Available, o resultado do fingerprint é explícito: disponível com algoritmo, valor e política; ou indisponível com código de motivo. Para Empty, Unavailable e NotCollected, não existe cálculo de fingerprint e a propriedade definitionFingerprint é omitida. A omissão nesses estados não equivale a hash vazio.

### 4.3 Associação à instalação

Na importação:
1. A organização é única e corresponde à empresa utilizadora da plataforma.
2. O utilizador selecciona o cliente PHC e associa a captura a uma instalação existente, cria uma nova instalação quando se trata de uma base de dados PHC ainda não registada, ou deixa a associação pendente quando há ambiguidade.
3. A decisão e as evidências de associação ficam registadas na plataforma.
4. O conteúdo recebido do pacote não é alterado durante a validação/análise; a plataforma não escreve o seu InstallationId no ZIP. Esta imutabilidade durante o processamento não implica retenção permanente: o ZIP é eliminado após materialização verificada.
5. Reimportar o mesmo PackageId com o mesmo checksum do ZIP é idempotente. Mesmo CaptureId em pacotes diferentes deve ser tratado como a mesma captura com múltiplas representações/pacotes, não como recolhas independentes.

A correspondência por fingerprint da origem ou hints é sugestão, não associação automática irrevogável.

## 5. Capacidades — capabilities.json

Este ficheiro declara o que a versão do Scanner sabe recolher, independentemente do que foi solicitado ou executado nesta captura.

Cada operação tem:
- categoryId: categoria de conhecimento estável;
- operationId: identificador estável da operação lógica;
- capabilityVersion: versão da semântica/âmbito dessa capacidade;
- collectorId: collector responsável;
- supported: declaração explícita de suporte;
- description: descrição curta opcional.

Exemplo:

~~~json
{
  "schemaVersion": "1",
  "scannerVersion": "2.0.0",
  "operations": [
    {
      "categoryId": "phc.customizations.brule",
      "operationId": "phc.customizations.brule.inventory",
      "capabilityVersion": "1.0",
      "collectorId": "phc.customizations",
      "supported": true
    },
    {
      "categoryId": "phc.customizations.brule",
      "operationId": "phc.customizations.brule.definition",
      "capabilityVersion": "1.0",
      "collectorId": "phc.customizations",
      "supported": true
    }
  ]
}
~~~

Inventário e recuperação de definições são operações separadas quando podem ter resultados independentes. O mesmo princípio aplica-se a outras categorias quando necessário; não se deve multiplicar operações por cada coluna ou consulta SQL sem benefício semântico.

A plataforma mantém regras explícitas de compatibilidade entre versões de capacidade. O mesmo operationId não prova automaticamente comparabilidade entre versões.

## 6. Cobertura — coverage.json

### 6.1 Dimensões independentes

Cada entrada corresponde a uma operação lógica declarada em capabilities.json. A cobertura mantém separadas as dimensões seguintes:

**Resultado da operação:**
- Succeeded
- SucceededWithWarnings
- Partial
- Failed
- Skipped
- NotAttempted

**Disponibilidade da estrutura:**
- Present
- NotPresent
- NotApplicable
- Unknown

**Disponibilidade do conteúdo:**
- Available
- Partial
- Unavailable
- NotCollected
- Empty

**Completude da origem (schemaVersion 2):**
- Complete
- Partial
- Unknown

Estas dimensões não se substituem. Uma operação pode terminar com sucesso e ainda assim não ter evidência suficiente de completude; uma consulta pode encontrar zero registos; uma estrutura pode estar ausente; um inventário pode ser completo apesar de algumas definições não estarem disponíveis.

### 6.2 Métricas de cobertura (schemaVersion 2)

Cada operação declara, quando aplicável:

| Campo | Semântica |
|---|---|
| `sourceRowsRead` | Número de registos efetivamente lidos na fonte dentro do âmbito declarado |
| `objectsEmitted` | Número de objetos técnicos emitidos pelo Scanner; não é uma contagem de linhas da origem |
| `rowsFiltered` | Número de registos lidos e excluídos por filtros declarados |
| `rowsFailed` | Número de registos lidos cujo processamento falhou |

As quatro métricas têm unidades/semânticas diferentes. Não se impõe uma igualdade aritmética entre elas: um registo pode originar zero, um ou vários objetos. Contagens desconhecidas devem permanecer desconhecidas e nunca ser preenchidas artificialmente com zero. Falhas de consulta sem linha identificável são representadas no resultado/issues e na completude, não inventadas como linhas falhadas.

`sourceCompleteness` descreve se todo o âmbito declarado foi percorrido:
- `Complete`: há evidência de percurso integral do âmbito;
- `Partial`: há evidência de percurso incompleto;
- `Unknown`: a evidência disponível não permite determinar a completude.

A completude é relativa ao âmbito e aos filtros declarados, não à totalidade indiscriminada da base de dados.

### 6.3 Exemplo — schemaVersion 2

~~~json
{
  "schemaVersion": "2",
  "captureId": "b1b2c3d4-1111-4222-8333-123456789abc",
  "operations": [
    {
      "operationId": "phc.customizations.brule.inventory",
      "capabilityVersion": "2.0",
      "requested": true,
      "result": "Succeeded",
      "sourceAvailability": "Present",
      "sourceCompleteness": "Complete",
      "contentAvailability": "Available",
      "scope": {
        "source": "dbo.brule",
        "filter": "all records"
      },
      "sourceRowsRead": 120,
      "objectsEmitted": 85,
      "rowsFiltered": 35,
      "rowsFailed": 0,
      "issueIds": []
    }
  ]
}
~~~

### 6.4 Regras de interpretação e ausência segura

- Operação não suportada pela versão do Scanner: não consta como capacidade suportada.
- Operação suportada mas não solicitada: requested=false e resultado Skipped; nunca é ausência/remoção.
- Operação solicitada mas não iniciada: NotAttempted.
- Estrutura não encontrada: NotPresent; não significa automaticamente NotApplicable.
- Estrutura não aplicável: só declarar NotApplicable quando houver evidência suficiente.
- Uma operação concluída com zero registos só prova uma origem vazia quando a fonte está Present/visível, o âmbito foi integralmente percorrido e `sourceCompleteness=Complete`.
- Inventário concluído e conteúdo não recolhido: manter a observação do objeto e declarar NotCollected para a definição.
- Registos filtrados não são, por si só, prova de ausência dos objetos que representam. Os filtros devem ser conhecidos e o universo de comparação deve ser compatível.
- Falhas e limitações referenciam issues identificadas no payload de diagnósticos ou no manifesto.
- Cobertura por operação não precisa de ser detalhada por coluna/consulta. Descer ao nível de suboperação apenas quando os resultados forem independentes.

O Knowledge só pode concluir `Removed` quando a operação correspondente foi suportada, solicitada e executada; a fonte foi identificada e a visibilidade confirmada; não existem falhas que afectem o âmbito; `sourceCompleteness=Complete`; o âmbito/filtros são conhecidos e comparáveis; e a observação anterior permite comparação válida. Se qualquer condição não estiver demonstrada, o estado é inconclusivo/`NotObserved`.

`Removed` não é estado do Scanner nem do IKP. É uma conclusão da plataforma após comparação de capturas compatíveis e cobertura relevante completa.

### 6.5 Retrocompatibilidade

O IKP 0.1 mantém a sua versão principal. A versão do schema de cobertura evolui independentemente: schemaVersion 1 continua a ser aceite com a semântica histórica; schemaVersion 2 introduz as métricas e `sourceCompleteness`.

Em pacotes schemaVersion 1, `observedCount` não é reinterpretado automaticamente como `sourceRowsRead`. Um resultado `Empty`, `observedCount=0` ou `Succeeded` não prova isoladamente que a origem estava vazia ou que o âmbito foi percorrido integralmente. Na ausência de evidência equivalente explícita, a completude é considerada `Unknown` para decisões de ausência. Não se atribuem garantias retroativas a capturas antigas.

## 7. Índice de objetos — object-index.json

Este índice enumera os objetos observados na captura e liga-os às respetivas evidências e artefactos. Não é um histórico; cada IKP descreve apenas a captura a que pertence.

Campos mínimos por objeto:
- sourceObjectId: ID único no âmbito da captura/pacote, referenciável por relações e artefactos;
- objectKind: tipo normalizado;
- sourceIdentity: identificadores nativos disponíveis e âmbito;
- name e schemaName, quando aplicáveis;
- metadata: metadados estruturados relevantes;
- metadataFingerprint, quando calculável;
- definition: estado de disponibilidade e referência(s) a artefactos;
- definitionFingerprint, quando calculável;
- operationId: operação que observou o objeto;
- parentSourceObjectId, quando exista relação hierárquica.

Exemplo:

~~~json
{
  "schemaVersion": "1",
  "captureId": "b1b2c3d4-1111-4222-8333-123456789abc",
  "objects": [
    {
      "sourceObjectId": "phc.brule:7F3A...",
      "objectKind": "phc.brule",
      "sourceIdentity": {
        "scheme": "phc.stamp",
        "value": "7F3A...",
        "scope": "dbo.brule"
      },
      "name": "Regra de validação",
      "schemaName": "dbo",
      "metadata": {
        "table": "FT",
        "inactive": false,
        "order": 10
      },
      "metadataFingerprint": {
        "algorithm": "sha256",
        "value": "..."
      },
      "definition": {
        "status": "Available",
        "artifactIds": ["artifact-brule-7f3a"],
        "definitionFingerprint": {
          "status": "Available",
          "algorithm": "sha256",
          "value": "...",
          "policy": {
            "id": "phc.brule.definition",
            "version": "1.0"
          }
        }
      },
      "operationId": "phc.customizations.brule.inventory"
    }
  ]
}
~~~

Representação do resultado do fingerprint:

- `Available`: requer `algorithm`, `value`, `policy.id` e `policy.version`. O algoritmo é actualmente `sha256`; o valor é hexadecimal minúsculo com 64 caracteres.
- `Unavailable`: requer `reasonCode` do catálogo controlado; não inclui `algorithm`, `value` ou `policy`.
- `definitionFingerprint` só é representado quando `definition.status` é `Available`. Para os restantes estados de definição, a propriedade é omitida.
- `policy.id` identifica a política específica do `objectKind`/variante; `policy.version` identifica a versão exacta das regras de normalização. A versão do Scanner e a versão da capability não substituem estes campos.

Catálogo controlado inicial de `reasonCode` para fingerprint indisponível:

| Código | Significado |
|---|---|
| `NoValidatedPolicy` | Não existe política específica, aprovada e validada para o kind/variante. |
| `UnsupportedSyntax` | A política existe, mas a definição contém sintaxe fora do âmbito suportado. |
| `NormalizationFailed` | A política aplicável não conseguiu produzir a representação canónica com segurança. |

Os códigos são valores fechados do contrato, não mensagens livres. Um consumidor deve tolerar códigos desconhecidos sem os reinterpretar. Falta de conteúdo na origem pertence a `definition.status`, não a `reasonCode`. Incompatibilidade entre versões de política é uma condição de comparação/migração, não um motivo para reescrever o resultado original.

Estados individuais de definição:
- Available: conteúdo recuperado e referenciado;
- Empty: a definição existe, mas o conteúdo é genuinamente vazio;
- Unavailable: tentou-se recuperar, mas não foi possível;
- NotCollected: a definição não foi solicitada/recolhida.

A ausência de um objeto no índice só pode ser interpretada à luz da cobertura da operação correspondente. Não criar objetos artificiais para representar ausências.

### 7.1 Identidade da origem

O Scanner deve preservar, quando disponíveis e úteis:
- stamp PHC;
- object_id SQL Server;
- column_id SQL Server;
- schema, tabela, nome e outros atributos nativos;
- tipo e âmbito em que o identificador é válido.

Os identificadores nativos são evidência contextual. Não substituem a identidade lógica da plataforma. Correspondências incertas são resolvidas na plataforma, sem fusão automática baseada apenas em nome ou conteúdo semelhante.

## 8. Artefactos e integridade

### 8.1 Manifesto de artefactos

O manifesto principal inclui artifacts[]:

| Campo | Obrigatório | Descrição |
|---|---:|---|
| artifactId | Sim | Identificador único dentro do pacote |
| path | Sim | Caminho relativo no ZIP |
| category | Sim | Perfil/categoria do artefacto |
| format | Sim | MIME type ou formato identificado |
| formatVersion | Não | Versão do formato interno |
| byteLength | Sim | Tamanho exato em bytes |
| sha256 | Sim | Hash dos bytes exatos do ficheiro |
| required | Sim | Se a ausência invalida o pacote |
| role | Não | Papel semântico do ficheiro |

Os objetos do índice referenciam artefactos por artifactId, não por caminhos reconstruídos ou nomes de ficheiro adivinhados.

### 8.2 Três níveis de integridade

1. **Checksum do ZIP recebido:** SHA-256 dos bytes exatos do ZIP, calculado pela plataforma à receção e guardado no registo de importação. Não é possível incluí-lo no próprio ZIP sem autorreferência.
2. **SHA-256 por ficheiro:** checksums.json contém hashes dos ficheiros de contrato e payload, exceto o próprio checksums.json.
3. **Digest canónico do conteúdo lógico:** calculado a partir de caminhos normalizados, tamanhos e hashes dos ficheiros, ordenados de forma determinística; exclui checksums.json e o próprio digest. Permite comparar conteúdo independentemente da recompressão ZIP.

O snapshotHash estrutural existente pode ser preservado como fingerprint específico do schema, mas não substitui estes mecanismos.

### 8.3 Exemplo de checksums

~~~json
{
  "schemaVersion": "1",
  "algorithm": "SHA-256",
  "files": [
    { "path": "manifest.json", "sha256": "..." },
    { "path": "capabilities.json", "sha256": "..." },
    { "path": "coverage.json", "sha256": "..." },
    { "path": "object-index.json", "sha256": "..." },
    { "path": "payload/customizations/brule/7F3A.prg", "sha256": "..." }
  ],
  "logicalContentDigest": {
    "algorithm": "SHA-256",
    "value": "..."
  }
}
~~~

O digest lógico é calculado sobre todos os ficheiros do pacote exceto `checksums.json`, incluindo os ficheiros de contrato e payload. Para cada ficheiro, normalizar o caminho para separadores `/`, rejeitar caminhos absolutos, segmentos `.`/ `..` e duplicados após normalização; usar o caminho relativo em Unicode NFC; ordenar por comparação ordinal dos caminhos; calcular o tamanho em bytes; usar o SHA-256 minúsculo dos bytes exatos. Para cada entrada concatenar exatamente `path + LF + byteLength decimal + LF + sha256 + LF`, sem BOM, em UTF-8. O digest lógico é SHA-256 dos bytes dessa concatenação. `checksums.json` é excluído para evitar autorreferência. Checksums provam integridade, não autoria. Assinatura digital pode ser considerada numa versão futura.

## 9. Conteúdo técnico e proveniência

### 9.1 Schema

Reutilizar os artefactos estruturais do Database Schema Snapshot, preservando:
- tabelas e colunas;
- PK/FK e índices;
- views, procedures, triggers e funções;
- tipos e synonyms;
- classificação conservadora (sql_system, candidate_custom, unknown).

A classificação do Scanner não deve afirmar por si só que um objeto é PHC Standard. A distinção PHC Standard vs personalização deve respeitar os mapeamentos e convenções PHC existentes e a responsabilidade da plataforma.

### 9.2 Personalizações

O perfil deve transportar inventário estruturado e definições originais. Não limitar o inventário a objetos com código executável.

Exemplos: tabelas/campos de utilizador, BRULE/EBRULE, eventos, UDEFS/EUDEFS, teclas, ecrãs, grelhas, PDU, análises, IDU, funções e objetos SQL personalizados, conforme as capacidades efetivamente suportadas.

Não recolher dados operacionais dos clientes (faturação, clientes, documentos, movimentos, stocks, etc.). Recolher metadados e definições técnicas. Elementos standard devem ser omitidos quando reconhecidos como standard, salvo metadados de alteração/customização que justifiquem a sua observação.

### 9.3 Relações

O perfil de relações preserva factos e interpretações em duas coleções lógicas dentro de um único ficheiro físico:

- `payload/relations/declared-relations.json`: coleção `declaredRelations`, com factos PHC provenientes de REL e DTL, mantendo `sourceObjectId`, `sourceType`, `sourceStamp`, tabelas de origem/destino, expressão original, resolução dos extremos, correspondência e `interpretationIds`;
- coleção `interpretations`: interpretações associadas por `sourceObjectId`, incluindo `interpretationId`, `parserId`, `parserVersion`, estado, confiança, AST, diagnósticos e métricas de referências.

O envelope emitido pelo Scanner contém `schemaVersion`, `captureId`, `declaredRelations` e `interpretations`. Não existe no exportador atual um segundo ficheiro `declared-relation-interpretations.json`, nem um envelope comum `records`. A separação entre factos e interpretações é lógica e de domínio, não exige ficheiros físicos distintos. O ficheiro é declarado no manifesto e coberto por checksums como qualquer outro payload.

A cobertura REL/DTL deve corresponder ao número de factos persistidos por `sourceType`; divergências entre `observedCount` e os registos do workspace bloqueiam a exportação, em vez de produzirem um pacote aparentemente completo.

As foreign keys físicas SQL continuam a ser evidência estrutural distinta. Não fundir uma relação PHC com uma FK SQL apenas porque têm os mesmos extremos. Correspondências prováveis não devem ser apresentadas como factos confirmados. As interpretações são derivadas, versionadas e regeneráveis; não substituem nem alteram a expressão original.

### 9.4 Conhecimento derivado

payload/knowledge/ pode transportar projeções como KR/FieldMapping, desde que identifique:
- derived: true;
- versão do analisador;
- referências às evidências de origem;
- confiança e diagnósticos, quando aplicável.

As projeções são regeneráveis. A sua ausência não significa ausência de factos.

### 9.5 Privacidade e partilha

O IKP pode declarar safeForPublicShare, mas essa declaração não certifica o pacote nem substitui a validação técnica e de segurança. O campo é opcional.

Antes de partilha global, deve existir validação técnica e de segurança efetiva, incluindo deteção/tratamento de:
- dados pessoais;
- credenciais e segredos;
- informação confidencial;
- código proprietário ou outros conteúdos que não devam ser divulgados.

A política de partilha pode permitir ou bloquear um pacote conforme o resultado da validação. Partilhar um pacote não transforma automaticamente o seu conteúdo em PHC Standard; a proveniência e o contexto original mantêm-se.

## 10. Linhagem entre instalações

O IKP identifica a origem técnica da captura, mas não é o registo autoritativo da linhagem global.

A plataforma mantém acontecimentos de cópia/restauro entre instalações:
- instalação de origem e instalação de destino;
- tipo de operação (cópia, restore, clone ou outra);
- instante, método e ator, quando conhecidos;
- evidências e origem da declaração;
- estado de confirmação;
- CaptureId de origem/destino, quando conhecidos.

Hints de linhagem podem ser transportados se forem úteis e não autoritativos. Stamps iguais, hashes iguais ou semelhança de schema não provam que um conteúdo foi transportado por uma operação de cópia. Igualdade de conteúdo prova igualdade do conteúdo observado, não a trajetória.

Uma cópia/restauro não funde instalações nem históricos. Permite comparar estados relacionados e distinguir, quando existe evidência, alterações locais de alterações transportadas. Sem evento ou evidência de cópia, a direção causal não pode ser afirmada.

## 11. Validação e importação

### 11.1 Validação estrutural e de segurança

Na receção, a plataforma deve:
1. calcular e guardar o SHA-256 dos bytes exatos do ZIP;
2. validar ZIP legível e limites configuráveis de tamanho, entradas e ficheiros;
3. rejeitar caminhos absolutos, traversal (..), caminhos duplicados após normalização e entradas malformadas;
4. exigir os cinco ficheiros de contrato de topo;
5. validar versão, tipos, campos obrigatórios e referências cruzadas;
6. validar que todas as entradas são declaradas e que não existem entradas extra;
7. validar existência, tamanho e SHA-256 dos artefactos;
8. validar coerência entre capacidades, cobertura, índice de objetos e payload;
9. aplicar as verificações de privacidade e segurança antes de partilha/publicação;
10. rejeitar JSON inválido, encoding incompatível e IDs duplicados.

Qualquer falha bloqueia a materialização/publicação. O blob recebido pode ser retido em quarentena segundo política de auditoria, sem ser tratado como conhecimento válido.

### 11.2 Pipeline conceptual

~~~text
Receção
  → armazenamento imutável do pacote
  → validação estrutural e integridade
  → associação guiada à organização/instalação
  → interpretação de capacidades e cobertura
  → staging de observações e artefactos
  → comparação coverage-aware
  → relatório de validação/publicação
  → publicação atómica
  → registo de auditoria e notificação
~~~

Estados conceptuais:
- Received: pacote recebido;
- Rejected: contrato/integridade/segurança inválidos;
- Validated: contrato e integridade válidos;
- AssociationPending: falta associar a instalação;
- Staged: observações preparadas, ainda não publicadas;
- ReadyToPublish: validações concluídas;
- Published: revisão publicada;
- Failed: processamento falhou sem substituir a revisão ativa.

A revisão ativa anterior mantém-se intacta até publicação atómica bem-sucedida. Não existe operação destrutiva ReplaceExisting sobre o conhecimento publicado.

## 12. Comparação de observações

A plataforma compara observações, não listas de ficheiros.

Estados conceptuais:
- Added: objeto observado pela primeira vez numa operação comparável, com cobertura anterior adequada;
- Modified: metadados e/ou definição alterados;
- Unchanged: sem alterações relevantes;
- NotObserved: não há evidência suficiente para concluir presença ou ausência;
- Removed: conclusão da plataforma após ausência numa captura compatível e com cobertura relevante completa.

O comparador deve separar alteração de metadados e alteração de definição. Não pode produzir Removed quando a operação não era suportada, não foi solicitada, falhou, ficou parcial ou não era aplicável.

A confirmação de remoção em capturas completas sucessivas pode ser introduzida como política da plataforma para reduzir falsos positivos; não altera a semântica do IKP.

## 13. Versionamento e compatibilidade

- ikpContractVersion identifica a estrutura e semântica do pacote.
- capabilityVersion identifica a semântica de cada operação do Scanner.
- A versão global do Scanner identifica o build do produtor, mas não substitui as versões das capacidades.
- A plataforma mantém uma matriz de compatibilidade explícita por operação/capabilityVersion.
- Categorias e operações novas podem ser opcionais; consumidores antigos não devem inferir cobertura para operações desconhecidas.
- Contratos incompatíveis são rejeitados ou encaminhados para conversor apenas quando esse conversor estiver explicitamente suportado na matriz de compatibilidade.
- Não existe obrigação de criar adaptadores para pacotes IKP de teste produzidos por iterações anteriores do Scanner que tenham sido descartados. Esses pacotes não são entradas suportadas e não têm de continuar legíveis. Qualquer necessidade futura de recuperar um pacote histórico exige decisão explícita e um conversor delimitado; não constitui compatibilidade implícita.

## 14. Estado real e lacunas Scanner ↔ IKP

**Estado confirmado em 2026-11-17:** o Scanner contém o exportador IKP 0.1 e os comandos CLI scan-ikp e export-ikp. A captura real CSDefault202603 foi examinada: 16.618 objetos; 8.307 SourceIdentity; 16.581 MetadataFingerprint; 49 definições associadas; 921 factos REL/DTL e 921 interpretações. O ZIP real foi inspecionado e a estrutura combinada REL/DTL foi confirmada.

O núcleo PhcKnowledge.Import já implementa IkpPackageValidator, IkpPackageIngestionService, FileSystemImmutablePackageStore e SqlServerPackageReceiptRepository, com migration e testes sobre fixture IKP / ZIP sintético → Validator / Ingestion. Estes testes não demonstram SQL Server real, API HTTP/autenticação ou materialização do Knowledge.

Lacunas e validações restantes:
- executar build e testes atuais do monorepo e confirmar o resultado CI mais recente;
- alargar a validação real aos kinds ausentes na captura atual e a novas capturas;
- manter schemas/fixtures alinhados com o payload REL/DTL efetivamente emitido;
- confirmar que os perfis de schema e customizations estão efetivamente incluídos, para além do índice, artefactos referenciados, diagnósticos e REL/DTL;
- rever a política de exportação de workspaces antigos sem ScanManifest completo; os valores desconhecidos devem continuar explícitos e nunca inferidos;
- validar os limites de segurança do ZIP e a coerência entre manifesto, checksums, capabilities, coverage, object-index e payload;
- testar a persistência do receipt contra SQL Server real e definir reconciliação/retry para arquivo sem receipt;
- a associação de instalação, materialização de observações, comparação e publicação permanecem responsabilidades futuras da plataforma.

A matriz de compatibilidade deve descrever o código atual e as lacunas remanescentes, não repetir o estado histórico anterior à implementação do exportador.

## 15. Decisões já fechadas

| Tema | Decisão |
|---|---|
| Emissor | Scanner produz IKP nativamente; plataforma valida/importa |
| PackageId | Gerado pelo Scanner por pacote; identidade global no modelo de destino |
| Package / Receipt | Package global; PackageReceipt por Organization; a migration atual ainda tem unicidade por Organization |
| CaptureId | Gerado pelo Scanner no início da recolha |
| Manifestos | Um manifesto IKP na raiz; manifesto original do Schema Snapshot preservado em payload/schema/source-manifest.json |
| Extensibilidade | Categorias/payloads opcionais com regras comuns de extensão |
| Partilha | Permitida após validação técnica e de segurança efetiva |
| Integridade | SHA-256 do ZIP recebido, SHA-256 por ficheiro e digest canónico lógico |
| Instalação | Um InstallationId por base de dados PHC registada |
| Ambientes | Não existe entidade EnvironmentId no modelo |
| Histórico | Mantido na plataforma, não no Scanner nem em cada IKP |
| Identidade | Identidade lógica separada de identidade da origem e fingerprints |
| Linhagem | Relações/eventos de cópia/restauro sem fusão de instalações |
| Remoções | Conclusão da plataforma, dependente de cobertura compatível e completa |

## 16. Próximas tarefas

1. ~~Criar uma fixture JSON completa com referências cruzadas e checksums calculados.~~ **Concluído:** [fixture mínima IKP 0.1](fixtures/ikp-0.1/minimal-valid/README.md), com [relatório de validação](fixtures/ikp-0.1/minimal-valid/VALIDATION.md).
2. ~~Criar matriz de compatibilidade entre o contrato e os modelos/collectors/exporters atuais do Scanner.~~ **Concluído:** [matriz de compatibilidade](ikp-0.1-scanner-compatibility-matrix.md).
3. ~~Criar schemas JSON executáveis para os cinco ficheiros de contrato.~~ **Concluído:** JSON Schema Draft 2020-12 em [schemas/ikp-0.1](schemas/ikp-0.1/README.md).
4. ~~Criar catálogo de casos negativos e de fronteira e validador executável.~~ **Concluído:** [casos negativos](fixtures/ikp-0.1/negative-cases/cases.json) e [validador/testes Python](../../tools/ikp-validation/README.md). Os 18 cenários positivos/negativos foram executados localmente (18/18 PASS); o workflow CI está preparado para repetição automática, sem resultado CI confirmado nesta revisão.
5. Executar e estabilizar a validação CI; depois definir exemplos de compatibilidade de capabilities entre versões e fechar os casos de linhagem.
6. ~~PHC Knowledge: primeira fatia Receive / Validate / Archive.~~ **Implementada no núcleo Import**; faltam testes SQL Server real, API/autenticação e reconciliação do arquivo com receipt.

O emissor IKP está implementado no Scanner e o serviço Receive / Validate / Archive já existe em PhcKnowledge.Import. A captura real confirmou o payload combinado REL/DTL e a dimensão observada de SourceObjectId. O próximo passo é robustecer a persistência/reconciliação e desenhar HTTP/autenticação. Associação à instalação, materialização de observações, comparação e publicação permanecem fora desta fatia.

## Referências

- [Contrato de observação, identidade e linhagem](observation-identity-and-lineage-contract.md)
- [Investigação de cobertura dos collectors](collector-coverage-investigation-2026-11-06.md)
- [Fixture IKP 0.1 mínima](fixtures/ikp-0.1/minimal-valid/README.md)
- [Relatório de validação da fixture](fixtures/ikp-0.1/minimal-valid/VALIDATION.md)
- [JSON Schemas IKP 0.1](schemas/ikp-0.1/README.md)
- [Casos negativos e de fronteira](fixtures/ikp-0.1/negative-cases/cases.json)
- [Validador e testes automatizados](../../tools/ikp-validation/README.md)
- [Avaliação tecnológica e contrato de importação](technology-and-import-contract-assessment.md)
- [Orientações do projeto](../../PROJECT_GUIDELINES.md)
- Código canónico do Scanner: [`PhcKnowledgeScanner/`](../../PhcKnowledgeScanner/), neste monorepo.


## Nota de alinhamento do contrato — 2026-11-17

A especificação descreve a identidade lógica do pacote, não o schema físico atual do Knowledge. O modelo de destino separa Package global de PackageReceipt por Organization. O arquivo é content-addressed global por SHA-256; o receipt é organizacional. O mesmo SHA-256 implica os mesmos bytes e, num pacote IKP válido, o mesmo PackageId do manifesto. A migration atual não foi alterada e ainda impõe unicidade no âmbito da Organization.

Na captura real examinada, SourceObjectId teve máximo de 54 caracteres (16.618 registos; zero duplicados ordinais, espaços nas extremidades ou valores não-ASCII). O modelo de destino propõe NVARCHAR(256), unicidade por CaptureId e comparação ordinal, com validação de vazio e espaços nas extremidades. Esta proposta não é ainda uma coluna/migration implementada.


## Ciclo de vida aprovado do pacote — 21-11-2026

1. Recepção e validação do ZIP em área temporária.
2. Análise de identidade, proveniência, fingerprints, cobertura, conteúdos e referências.
3. Materialização no SQL Server dos dados estruturados, histórico e artefactos originais necessários.
4. Verificação de contagens, hashes, referências, proveniência e capacidade de consulta/recuperação sem o ZIP.
5. Eliminação do ZIP e ficheiros temporários apenas após verificação bem-sucedida.

A recepção concluída, a análise Ready ou a decisão humana não substituem a verificação da materialização. A ausência de objectos só pode ser interpretada como remoção quando a operação correspondente foi executada com sucesso e cobertura completa. Capturas parciais/falhadas não produzem falsos Removed.


## Ciclo de vida do ZIP — decisão aprovada em 21-11-2026

O ZIP IKP é exclusivamente um envelope temporário de transporte. Pode ser mantido em staging/arquivo transitório durante recepção, validação, análise e retry. O Knowledge extrai e materializa no SQL Server os dados estruturados, histórico e artefactos originais necessários. Após verificação de integridade, contagens, referências e proveniência, elimina o ZIP e os temporários. Em caso de falha ou materialização incompleta, mantém o pacote recuperável para retry. O PackageId e o checksum identificam o envelope durante o processamento; não criam obrigação de arquivo permanente.


## Contrato de classificação e proveniência de artefactos — D-018 (23-11-2026)

A classificação de artefactos separa três dimensões independentes:

1. **Origem do conteúdo:** `source-content`, `derived-projection`, `generated-description` ou `generated-diagnostic`.
2. **Natureza semântica:** código, configuração, estrutura SQL, metadados, descrição, relação ou diagnóstico.
3. **Proveniência:** fonte/tabela, campo/propriedade, objeto associado, formato/codificação, transformação e versão, hash e relação com o conteúdo de origem, quando aplicável.

`source-content` significa conteúdo obtido diretamente da fonte e preservado sem transformação semântica. Se a representação exportada não preservar exatamente os bytes da origem, essa limitação deve ser explícita. Conteúdo extraído, normalizado, formatado, combinado ou transformado é `derived-projection`. Descrições e diagnósticos produzidos pelo Scanner são classificados separadamente como `generated-description` e `generated-diagnostic`.

A natureza semântica não é deduzida da origem. Uma descrição pode ser original ou gerada; código pode ser original ou derivado. As dimensões devem permanecer independentes.

A referência `definition.artifactIds` fica reservada a artefactos que constituam efetivamente a definição técnica do objeto. Artefactos descritivos, estruturais e diagnósticos são associados por relações próprias, sem serem classificados como definições. Metadados disponíveis não provam que a definição original tenha sido recolhida.

O conteúdo original disponível nunca é substituído por uma projeção. O hash de integridade do artefacto preserva a identidade byte-a-byte do conteúdo recolhido. O DefinitionFingerprint é calculado a partir de uma representação canónica derivada do original, segundo a política semântica versionada por objectKind. Projeções e descrições derivadas têm hashes próprios e não substituem nem alteram o original.

Pacotes antigos continuam legíveis sem receber classificações ou garantias retroativas não transportadas. D-018 fecha a semântica do contrato, mas não altera collectors, exporter, workspace SQLite, validator, schemas executáveis ou materialização SQL. A implementação depende da matriz por collector/ObjectKind e dos testes de aceitação.


## Regra de disponibilidade do DefinitionFingerprint — D-020 (23-11-2026)

Quando não existir uma política de normalização semântica segura, específica para o `objectKind`/variante e validada pelos testes de aceitação, o `DefinitionFingerprint` fica indisponível. O SHA-256 dos bytes exactos do artefacto é exclusivamente um hash de integridade e nunca pode ser reutilizado como fingerprint semântico.

A comparação byte-a-byte pode ser disponibilizada como modalidade exacta separada, mas não preenche `definition.definitionFingerprint` nem demonstra equivalência semântica. A ausência de fingerprint por falta de normalizador não significa definição vazia, erro de integridade ou diferença semântica.

Não se exige leitura nem compatibilidade com os pacotes IKP de teste de iterações anteriores do Scanner que foram descartados. Os fingerprints produzidos nesses pacotes pela regra anterior não têm de ser preservados, classificados como legado ou convertidos; não será criada uma via de compatibilidade para esses pacotes. As novas capturas devem cumprir D-019/D-020/D-021. O contrato executável terá de transportar a política/versão e a distinção de estados necessárias; esta nota documental não altera por si só os schemas nem o validador.

## Representação versionada do DefinitionFingerprint — D-021 (23-11-2026)

O estado do fingerprint é independente do estado de disponibilidade da definição. Para uma definição `Available`, o pacote representa explicitamente `definitionFingerprint.status` como `Available` ou `Unavailable`. Quando a definição é `Empty`, `Unavailable` ou `NotCollected`, a propriedade `definitionFingerprint` é omitida, pois não há conteúdo disponível para calcular o fingerprint.

Um fingerprint disponível contém `algorithm`, `value` e `policy` com `id` e `version`. Um fingerprint indisponível contém `reasonCode` de catálogo controlado, sem algoritmo, valor ou política aplicada. A política identifica o normalizador específico por kind/variante e a versão exacta das regras; não pode ser inferida apenas da versão do Scanner ou da capability.

Catálogo inicial fechado de motivos: `NoValidatedPolicy`, `UnsupportedSyntax` e `NormalizationFailed`. Os códigos não substituem os estados de disponibilidade da definição. Códigos futuros exigem evolução explícita do contrato; consumidores devem preservar e tolerar códigos desconhecidos.

Esta decisão fecha a representação documental. Não altera ainda schemas JSON executáveis, modelos, serialização, exportador, validador ou materialização. A compatibilidade com consumidores existentes e a versão de schema do object-index têm de ser verificadas antes da implementação; não se presume que a alteração estrutural seja retrocompatível.

## Object Index v2 e análise de importação 0.3 — baseline D-022 (23-11-2026)

O IKP global permanece na versão 0.1. A versão interna do Object Index evolui para schemaVersion=2, para transportar a representação D-021 sem ambiguidade. Esta alteração não é retrocompatível: o importador desta implementação aceita Object Index v2 e rejeita v1. Não se cria um adaptador para pacotes de teste de iterações anteriores do Scanner que foram descartados.

Quando definition.status=Available, definition.definitionFingerprint é obrigatório e contém uma destas formas:

- Available: status, algorithm, value e policy com id e version;
- Unavailable: status e reasonCode, sem algoritmo, valor ou política.

Para definition.status=Empty, Unavailable ou NotCollected, a propriedade é omitida. A indisponibilidade do fingerprint não altera o estado da definição, a cobertura da operação ou a prontidão de materialização. Os reasonCodes iniciais emitidos são NoValidatedPolicy, UnsupportedSyntax e NormalizationFailed; consumidores toleram códigos desconhecidos.

O hash SHA-256 dos bytes exactos e a validação do comprimento continuam a ser verificações de integridade independentes. Nunca são reutilizados como DefinitionFingerprint. O MetadataFingerprint mantém o contrato actual, sem alteração nesta fase.

A análise de importação evolui para schema 0.3, com DefinitionFingerprintAvailableCount e DefinitionFingerprintUnavailableCount. O objecto inspeccionado conserva os detalhes do fingerprint; a análise persistida conserva as contagens/resumo.

A primeira implementação não activa políticas semânticas em produção: definições disponíveis sem política validada produzem fingerprint Unavailable/NoValidatedPolicy. Uma política artificial pode existir apenas nos testes. A implementação não inclui normalizador T-SQL, heurísticas textuais, comparação byte-a-byte como fingerprint ou alteração de collectors/SQLite.

Este baseline documental não altera ainda schemas JSON executáveis, fixtures, código, validator ou CI. Esses artefactos serão actualizados coordenadamente na fase de implementação aprovada.


## Estado da implementação D-022 — 23-11-2026

A implementação inicial do Object Index v2 e do DefinitionFingerprint D-021 está em revisão na PR #27, na branch `implementation/d022-definition-fingerprint-v2`; ainda não foi integrada na `main`. O Scanner separa a verificação de integridade do fingerprint semântico e emite `Unavailable/NoValidatedPolicy` para definições disponíveis sem política validada. O Importer lê o envelope D-021 e a análise evoluiu para 0.3 com contagens separadas. Os schemas e a fixture foram actualizados para Object Index v2. Não existem políticas semânticas de produção activas. Os testes foram acrescentados, mas builds, testes automatizados e CI ainda não foram confirmados. Consultar a matriz de compatibilidade para o estado e os limites de validação.
