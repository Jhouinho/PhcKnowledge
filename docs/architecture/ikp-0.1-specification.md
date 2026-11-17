# IKP 0.1 — Installation Knowledge Package

| Campo | Valor |
|---|---|
| Data da revisão | 2026-11-10 |
| Contrato | ikpContractVersion = 0.1 |
| Content-Type | phc.installation.knowledge.package |
| Transporte canónico | ZIP |
| Estado | **Contrato IKP 0.1 consolidado; schemas, fixtures, validador e emissor no Scanner** |
| Implementação | Exportador no Scanner implementado; importador da plataforma central por implementar |
| Contrato complementar normativo | [P1 — Identidade, proveniência e fingerprints](p1-identity-provenance-contract.md) |
| Contexto arquitetural | [Contrato de observação, identidade e linhagem](observation-identity-and-lineage-contract.md) |

Este documento define o pacote de transferência entre o Scanner PHC independente e a plataforma PHC Knowledge. Distingue o contrato pretendido da capacidade atualmente existente no Scanner. O Scanner já emite IKP 0.1 através de `IkpPackageExporter` e dos comandos CLI `scan-ikp` e `export-ikp`. A plataforma central ainda não o importa. Para a semântica normativa de SourceObjectId, SourceIdentity, estados individuais de definição, proveniência e fingerprints, prevalece o [contrato P1 de identidade e proveniência](p1-identity-provenance-contract.md); este documento define a sua representação no pacote.

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
6. A distinção entre Installation e Environment pertence ao PHC Knowledge: uma Installation pode conter vários Environments, incluindo Production, Test e Development.
7. Bases de dados de empresas/instalações distintas mantêm Installations independentes. Uma cópia/restauro pode representar outro Environment da mesma Installation quando essa continuidade for confirmada; quando representar uma Installation distinta, a relação é registada por linhagem.
8. Linhagem regista proveniência entre Installations; nunca funde os seus históricos.
9. O pacote não transporta identificadores internos de Organization, Installation, Environment, DatabaseLocation, Observation ou EntityIdentity.
10. Artefactos originais e factos declarados não devem ser substituídos por interpretações derivadas.
11. Receber ou validar um pacote não equivale a publicar conhecimento.

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
| packageId | Sim | UUID único deste pacote físico |
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

O IKP 0.1 não transporta OrganizationId, InstallationId, EnvironmentId nem DatabaseLocationId. Estes identificadores pertencem ao modelo interno do PHC Knowledge e são atribuídos/confirmados pela plataforma. O pacote pode transportar origem técnica, como databaseName, serverLabel e originFingerprint, apenas como evidência não autoritativa. A plataforma decide se uma base de dados corresponde a um Environment de uma Installation existente ou a uma Installation distinta.

### 4.2 Identificadores e âmbito

| Conceito | Origem | Regra |
|---|---|---|
| PackageId | Scanner | Identifica o pacote físico; novo por cada pacote produzido |
| CaptureId | Scanner | Identifica uma recolha; pode originar vários pacotes |
| InstallationId | PHC Knowledge | Atribuído/confirmado na importação; nunca incluído como ID autoritativo no IKP |
| SourceObjectId | Scanner | Identificador do objeto na captura, referenciável dentro do pacote |
| SourceIdentity | Origem PHC/SQL | Identificadores nativos e respetivo âmbito |
| EntityIdentity | PHC Knowledge | Identidade lógica da entidade no âmbito da instalação |
| ObservationId | PHC Knowledge | Identificador interno próprio da Observation; não é transportado no IKP |
| MetadataFingerprint | Scanner/plataforma | Fingerprint dos metadados relevantes |
| DefinitionFingerprint | Scanner/plataforma | Fingerprint da definição técnica original |

CaptureId e PackageId nunca entram na identidade lógica da entidade. Stamps PHC, nomes SQL, object_id e column_id são evidências com âmbito e estabilidade diferentes; nenhum deve ser tratado como identificador universal.\n\n### 4.2.1 Cálculo dos fingerprints\n\n- Todos os fingerprints usam SHA-256 e hexadecimal minúsculo de 64 caracteres.\n- MetadataFingerprint: SHA-256 dos bytes UTF-8 do objeto `metadata` serializado segundo JSON Canonicalization Scheme (RFC 8785). Excluir datas de recolha, timestamps voláteis, caminhos temporários e dados de execução que não descrevam o objeto. A política de inclusão de cada campo pertence ao contrato da respetiva categoria.\n- DefinitionFingerprint: SHA-256 dos bytes exatos da definição original, sem normalização de espaços, encoding ou quebras de linha. O encoding e o formato devem ser identificados pelo artefacto.\n- Se uma definição for composta por vários artefactos, calcular um digest composto a partir da lista ordenada ordinalmente de `artifactId + LF + sha256 + LF`, em UTF-8, e aplicar SHA-256 ao resultado.\n- Não calcular DefinitionFingerprint para estados `Empty`, `Unavailable` ou `NotCollected`. A ausência de fingerprint nesses estados não equivale a hash vazio.

### 4.3 Receção e associação ao contexto

A receção do IKP não exige que a organização, Installation ou Environment sejam conhecidos. O processo é separado em responsabilidades:

1. Receber o ZIP e calcular o checksum dos bytes recebidos.
2. Validar a estrutura, os ficheiros obrigatórios, os schemas, as referências cruzadas e os checksums.
3. Arquivar o pacote original de forma imutável e registar PackageReceipt/Capture no PHC Knowledge.
4. Permitir que a Capture permaneça sem associação de contexto enquanto não existir decisão suficientemente fundamentada.
5. Materializar Observations numa fase posterior, sem exigir contexto confirmado e sem atribuir automaticamente EntityIdentity.
6. Quando o contexto for conhecido, registar CaptureContextAssignment para uma DatabaseLocation; a partir daí resolvem-se Environment, Installation e Organization.

Capture representa a recolha lógica identificada por CaptureId. PackageReceipt representa cada pacote físico recebido, identificado por PackageId. Uma Capture pode ter vários PackageReceipts; cada PackageReceipt pertence a uma Capture. Repetir o mesmo PackageId com o mesmo checksum é idempotente. O mesmo PackageId com checksum diferente é conflito de integridade e não pode substituir o pacote original. Pacotes diferentes com o mesmo CaptureId são representações/receções da mesma captura lógica.

A associação de contexto é uma decisão da plataforma, registada com autor, data, evidência e motivo. Pode permanecer pendente e pode ser corrigida através de nova atribuição histórica, mantendo no máximo uma atribuição ativa. Fingerprints da origem, nome da base de dados e hints são sugestões, não associação automática irrevogável.

O IKP permanece imutável. Nenhum ID interno de organização, instalação, ambiente, localização, observação ou identidade lógica é escrito no pacote.

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

### 6.1 Três dimensões independentes

Cada entrada corresponde a uma operação lógica declarada em capabilities.json.

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

Estas dimensões não se substituem. Uma consulta bem-sucedida pode encontrar zero registos; uma estrutura pode estar ausente; um inventário pode ser completo apesar de algumas definições não estarem disponíveis.

### 6.2 Exemplo

~~~json
{
  "schemaVersion": "1",
  "captureId": "b1b2c3d4-1111-4222-8333-123456789abc",
  "operations": [
    {
      "operationId": "phc.customizations.brule.inventory",
      "capabilityVersion": "1.0",
      "requested": true,
      "result": "Succeeded",
      "sourceAvailability": "Present",
      "contentAvailability": "Available",
      "scope": {
        "source": "dbo.brule",
        "filter": "all records"
      },
      "observedCount": 65,
      "issueIds": []
    },
    {
      "operationId": "phc.customizations.brule.definition",
      "capabilityVersion": "1.0",
      "requested": true,
      "result": "Partial",
      "sourceAvailability": "Present",
      "contentAvailability": "Partial",
      "scope": {
        "source": "dbo.brule"
      },
      "observedCount": 61,
      "issueIds": ["issue-004"]
    }
  ]
}
~~~

### 6.3 Regras de interpretação

- Operação não suportada pela versão do Scanner: não consta como capacidade suportada.
- Operação suportada mas não solicitada: requested=false e resultado Skipped; nunca é ausência/remoção.
- Operação solicitada mas não iniciada: NotAttempted.
- Estrutura não encontrada: NotPresent; não significa automaticamente NotApplicable.
- Estrutura não aplicável: só declarar NotApplicable quando houver evidência suficiente.
- Operação concluída com zero registos: resultado bem-sucedido e observedCount=0.
- Inventário concluído e conteúdo não recolhido: manter a observação do objeto e declarar NotCollected para a definição.
- Falhas e limitações referenciam issues identificadas no payload de diagnósticos ou no manifesto.
- Cobertura por operação não precisa de ser detalhada por coluna/consulta. Descer ao nível de suboperação apenas quando os resultados forem independentes.

Removed não é estado do Scanner nem do IKP. É uma conclusão da plataforma após comparação de capturas compatíveis e cobertura relevante completa.

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
          "algorithm": "sha256",
          "value": "..."
        }
      },
      "operationId": "phc.customizations.brule.inventory"
    }
  ]
}
~~~

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

O perfil de relações preserva factos e interpretações em ficheiros distintos:

- `payload/relations/declared-relations.json`: registos declarados PHC provenientes de REL e DTL, mantendo `sourceType`, `sourceRecordId`, tabelas de origem/destino, expressão original, metadados de auditoria, estado de resolução e correspondência;
- `payload/relations/declared-relation-interpretations.json`: interpretações associadas por `relationId`, incluindo versão do parser, estado, confiança, AST, diagnósticos e métricas de referências.

Cada ficheiro usa um envelope com `schemaVersion`, `captureId` e `records`. O primeiro é incluído quando existem factos REL/DTL ou quando a cobertura declara que a operação foi recolhida, mesmo que o resultado seja uma lista vazia. O segundo só é incluído quando existem interpretações persistidas. Os artefactos são declarados no manifesto e cobertos por checksums como qualquer outro payload.

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
  → validação estrutural e integridade
  → arquivo imutável e registo Capture/PackageReceipt
  → interpretação de capacidades e cobertura
  → staging/materialização de Observations e artefactos
  → associação de contexto, quando confirmada
  → resolução de IdentityMatch e EntityIdentity
  → comparação coverage-aware
  → relatório de validação/publicação
  → publicação atómica
  → registo de auditoria e notificação
~~~

A materialização de Observations não depende da associação de contexto. A resolução de EntityIdentity e as comparações que dependem da instalação/ambiente ficam adiadas até existir CaptureContextAssignment confirmado. Esta sequência descreve a evolução funcional da plataforma; a primeira fatia continua limitada a Receive / Validate / Archive.

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
- Contratos incompatíveis são rejeitados ou encaminhados para conversor explicitamente suportado.
- Adaptadores para ZIPs antigos são conversores de legado, não uma segunda via de aquisição permanente. A direção preferida é o Scanner emitir IKP nativamente.

## 14. Estado real e lacunas Scanner ↔ IKP

**Estado confirmado no código atual:** o Scanner já contém um exportador IKP 0.1 dedicado (IkpPackageExporter), integrado na CLI através dos comandos scan-ikp e export-ikp. O exportador produz os cinco ficheiros de contrato obrigatórios, calcula checksums por ficheiro e digest lógico, inclui os artefactos referenciados, diagnósticos e factos REL/DTL com interpretações separadas. Existem testes automatizados específicos do exportador.

Isto confirma a existência da implementação do emissor, mas **não confirma ainda uma validação end-to-end com uma captura real completa** nem a integração funcional com a plataforma central.

Lacunas e validações restantes:
- executar build e testes atuais do monorepo e confirmar o resultado CI mais recente;
- validar um pacote produzido a partir de uma captura real do Scanner, começando pela base PHC default já disponível como referência;
- confirmar o ZIP real com o validador independente e schemas IKP 0.1;
- confirmar que os perfis de schema e customizations estão efetivamente incluídos, para além do índice, artefactos referenciados, diagnósticos e REL/DTL;
- rever a política de exportação de workspaces antigos sem ScanManifest completo; os valores desconhecidos devem continuar explícitos e nunca inferidos;
- validar os limites de segurança do ZIP e a coerência entre manifesto, checksums, capabilities, coverage, object-index e payload;
- implementar no PHC Knowledge a receção, validação, arquivo imutável e registo da importação;
- a associação de instalação, materialização de observações, comparação e publicação permanecem responsabilidades futuras da plataforma.

A matriz de compatibilidade deve descrever o código atual e as lacunas remanescentes, não repetir o estado histórico anterior à implementação do exportador.

## 15. Decisões já fechadas

| Tema | Decisão |
|---|---|
| Emissor | Scanner produz IKP nativamente; plataforma valida/importa |
| PackageId | Gerado pelo Scanner por pacote |
| CaptureId | Gerado pelo Scanner no início da recolha |
| Manifestos | Um manifesto IKP na raiz; manifesto original do Schema Snapshot preservado em payload/schema/source-manifest.json |
| Extensibilidade | Categorias/payloads opcionais com regras comuns de extensão |
| Partilha | Permitida após validação técnica e de segurança efetiva |
| Integridade | SHA-256 do ZIP recebido, SHA-256 por ficheiro e digest canónico lógico |
| Instalação | Uma Installation pode conter vários Environments; a associação de uma captura é feita através de DatabaseLocation e CaptureContextAssignment |
| Ambientes | Production, Test e Development são contextos de uma Installation quando a continuidade é confirmada |
| Observation | ObservationId interno atribuído pela plataforma; UNIQUE(CaptureId, SourceObjectId); não depende de contexto nem de EntityIdentity |
| Identidade lógica | EntityIdentity pertence a uma Installation e só é associada após correspondência confirmada |
| Receção | Capture e PackageReceipt são distintos; uma Capture pode ter vários PackageReceipts |
| Associação | Pode ficar pendente e ser corrigida com histórico; no máximo uma atribuição ativa por Capture |
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
6. **PHC Knowledge:** implementar receção, validação de segurança/integridade e arquivo imutável de pacotes IKP, com testes de aceitação. A associação de contexto, materialização de Observations e publicação são fases distintas e ficam fora desta primeira fatia.

O emissor IKP já está implementado no Scanner. O próximo resultado de produto é Receive / Validate / Archive na plataforma central, conforme a matriz de compatibilidade. A validação do ZIP real do exportador permanece uma lacuna própria do Scanner/contrato. A primeira fatia do Knowledge não associa contexto, não materializa Observations, não resolve EntityIdentity e não compara capturas. Essas responsabilidades avançam em fases posteriores, sem depender umas das outras além dos pré-requisitos de contexto para identidade e comparação.

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
