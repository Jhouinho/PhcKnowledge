# Integração do catálogo PHC Standard com o IKP

| Campo | Valor |
|---|---|
| Revisão | 2026-11-20 |
| Estado | Especificação técnica proposta — aguarda aprovação antes de implementação |
| Âmbito inicial | Triggers DML PHC Standard |
| Base de dados central | SQL Server Express |
| Contratos relacionados | IKP 0.1; P1 identidade/proveniência; modelo de domínio 0.2 |

Este documento transforma a decisão arquitetural sobre conhecimento PHC Standard numa proposta de integração entre o Scanner, o pacote IKP e o PHC Knowledge. Não aprova alterações ao código, ao contrato IKP, ao SQLite, às migrations ou aos testes. Os pontos explicitamente assinalados como pendentes exigem decisão antes da implementação.

## 1. Objetivo e princípio

O objetivo é permitir que o PHC Knowledge reconheça objetos standard de uma instalação, associe-os ao conhecimento central versionado e explique o seu comportamento, mesmo quando a instalação não permite obter a definição SQL.

Princípio normativo:

> O Scanner recolhe evidências da instalação. O PHC Knowledge mantém o conhecimento standard necessário para interpretar essas evidências.

Consequências:

- O Scanner não é a fonte de verdade do comportamento PHC standard.
- As definições desencriptadas de referência são importadas e curadas no PHC Knowledge; não são necessárias no cliente nem em cada captura.
- Uma correspondência de identidade/metadados não prova equivalência do corpo SQL.
- Ausência só pode ser concluída quando a operação relevante foi executada e a cobertura permite essa conclusão.
- Conhecimento standard é central, reutilizável e versionado por aplicabilidade PHC; observações de instalações mantêm a sua proveniência e isolamento organizacional.
- SQL Server Express continua a ser a base relacional central. SQLite permanece limitado ao workspace/cache local do Scanner.

## 2. Estado técnico confirmado

### 2.1 Percurso IKP atual

No `PhcKnowledgeScanner/src/PhcKnowledgeScanner.Collectors/Sql/SqlServerStructureCollector.cs`, `CollectTriggersAsync` consulta `sys.triggers`, mas filtra as triggers para tabelas cujo nome começa por `u_`:

```sql
WHERE tr.parent_class_desc = 'OBJECT_OR_COLUMN'
  AND t.name LIKE N'u[_]%'
```

O registo `TechnicalObjectRecord` guarda atualmente identidade técnica, nome, schema, parent, collector, referência opcional ao artefacto de definição, SummaryJson e timestamp. Para triggers, o SummaryJson atual contém `isDisabled`. A rotina não recolhe eventos da trigger.

Resultado: o percurso IKP atual não transporta evidência de triggers associadas a tabelas PHC standard.

### 2.2 Percurso estrutural existente

O `DatabaseSchemaSnapshotExtractor`, em `PhcKnowledgeScanner/PhcKnowledgeScanner.Collectors/Sql/SchemaSnapshot/`, já recolhe triggers DML sem o filtro `u_`. A consulta estrutural usa `SchemaCatalogQueries.Triggers` e recolhe triggers de objetos SQL (`OBJECT_OR_COLUMN`); a query atual não restringe o parent a tabelas, pelo que também pode abranger outros objetos suportados pelo SQL Server. Para a primeira fatia PHC, o âmbito deve ser explicitamente limitado a tabelas, sem perder a possibilidade de extensão futura. Recolhe:

- schema e nome da trigger;
- schema e nome do objeto/tabela pai;
- tipo do objeto pai;
- estado desativado;
- indicação `INSTEAD OF`;
- disponibilidade da definição;
- definição SQL, quando solicitada e acessível;
- eventos associados, através de `SchemaCatalogQueries.TriggerEvents`.

Este percurso produz um `DatabaseSchemaSnapshot` separado. Não foi encontrada evidência de integração direta deste snapshot no exportador IKP atual.

A classificação `sql_system / candidate_custom / unknown` é heurística de inventário. Não identifica de forma autoritativa um objeto como PHC Standard.

### 2.3 Persistência no PHC Knowledge

O modelo conceptual `knowledge-domain-model-0.2.md` já prevê `StandardCatalog`, `CatalogEntry`, `CatalogEntryVersion`, `KnowledgeEntry`, `KnowledgeEntryVersion` e `KnowledgeProposal`.

A implementação física inspecionada contém receção, arquivo e análise de pacotes, incluindo `PackageImportAnalyses`. Não foram encontradas tabelas nem entidades persistentes para catálogo standard, versões de entradas standard, observações materializadas ou explicações funcionais de triggers.

Portanto, receber e analisar IKP ainda não materializa o conhecimento técnico descrito nesta proposta.

## 3. Arquitetura lógica proposta

A solução mantém três responsabilidades separadas:

1. **Scanner:** recolhe e exporta evidências observáveis da instalação.
2. **PHC Knowledge — catálogo standard:** mantém identidades, versões aplicáveis, definições de referência, proveniência e conhecimento funcional curado.
3. **PHC Knowledge — materialização e interpretação:** associa observações IKP ao catálogo, calcula estados de comparação e apresenta resultados com limites explícitos.

Fluxo:

```text
Fonte SQL de referência + versão/build PHC
        |
        v
Importação/curadoria do catálogo standard
        |
        v
SQL Server central: entradas + versões + definições + conhecimento
        ^
        |
Scanner -> IKP -> receção/validação -> materialização de observações
                                            |
                                            v
                                  correspondência e comparação
                                            |
                                            v
                                  consulta técnica no Portal
```

O catálogo standard não é recriado a cada captura. Uma captura acrescenta observações imutáveis ligadas ao CaptureId e ao PackageReceipt; não substitui nem altera a definição de referência.

## 4. Contrato de evidência de trigger no IKP

### 4.1 Âmbito da primeira fatia

A primeira fatia cobre triggers DML associadas a tabelas, isto é, objetos de `sys.triggers` com `parent_class_desc = 'OBJECT_OR_COLUMN'` e parent de tipo tabela.

Triggers de âmbito de base de dados/server-level não fazem parte desta primeira fatia. Se forem necessárias, devem ter âmbito e identidade próprios, não ser misturadas com triggers de tabela.

### 4.2 Evidência mínima por trigger

A observação exportada deve permitir reconstruir, no mínimo:

| Campo lógico | Origem SQL Server | Regra |
|---|---|---|
| SourceObjectId | identidade determinística do objeto na captura | Referência local à captura, nunca identidade permanente |
| Name | `sys.triggers.name` | Preservar nome original |
| SchemaName | schema do objeto trigger | Preservar schema |
| ParentObjectId | identidade do objeto pai na captura | Referenciar a tabela observada quando aplicável |
| ParentSchema | schema da tabela pai | Obrigatório para trigger de tabela |
| ParentName | `sys.tables.name` | Obrigatório para trigger de tabela |
| IsDisabled | `sys.triggers.is_disabled` | Booleano |
| IsInsteadOf | `sys.triggers.is_instead_of_trigger` | Booleano |
| Events | `sys.trigger_events.type_desc` | Conjunto ordenado e sem duplicados |
| DefinitionStatus | resultado da leitura de `sys.sql_modules.definition` | Available ou Unavailable; Empty apenas se conteúdo genuinamente vazio |
| DefinitionArtifactId | artefacto IKP, quando Available | Ausente quando não há bytes |
| DefinitionFingerprint | SHA-256 do conteúdo efetivamente recolhido | Só quando há definição disponível |

O contrato deve ainda preservar a operação/capability que produziu a observação e o seu estado de cobertura. Não se deve deduzir sucesso global apenas porque algumas triggers foram devolvidas.

### 4.3 Disponibilidade da definição

`sys.sql_modules.definition IS NULL` significa que a definição não está disponível através dessa consulta. Por si só, não permite concluir que está encriptada: também pode resultar de permissões ou outras limitações.

Assim:

- `Available`: definição obtida e artefacto íntegro referenciado.
- `Unavailable`: tentativa de leitura sem definição acessível; causa pode ser desconhecida.
- `NotCollected`: a definição não foi solicitada/recolhida.
- `Empty`: só se o sistema provar que a definição existe e é genuinamente vazia; não usar como substituto de NULL.

A causa detalhada (por exemplo, encriptação confirmada, falta de VIEW DEFINITION, erro de consulta) pode ser representada como diagnóstico separado quando houver evidência suficiente. Não inventar uma causa a partir de NULL.

### 4.4 Reutilização no Scanner

A recomendação é reutilizar a consulta e o mapeamento de metadados já existentes no percurso `DatabaseSchemaSnapshotExtractor`, evitando manter duas implementações divergentes de leitura de triggers.

A integração pode ser feita através de uma abstração comum de leitura de triggers ou de um componente interno reutilizável pelo snapshot e pelo collector IKP. A escolha concreta deve respeitar dependências atuais dos projetos e testes existentes.

Não se recomenda que o IKP dependa do ficheiro completo de DatabaseSchemaSnapshot como payload apenas para obter triggers. O exportador deve continuar a emitir o Object Index e artefactos segundo o contrato IKP, sem introduzir um segundo inventário paralelo.

### 4.5 Operação e compatibilidade IKP

A implementação deve decidir se a recolha passa a integrar a operação/capability SQL existente ou se exige uma operação específica. Essa decisão depende da inspeção completa do catálogo de capabilities, do tracker de coverage, dos schemas e dos validadores.

Critérios obrigatórios:

- A coverage deve contar a população relevante e refletir falhas/parcialidade.
- A ausência de trigger só pode ser avaliada se a operação correspondente tiver sido executada com sucesso e cobertura suficiente.
- Uma versão antiga do Scanner que não transporte estas evidências não pode ser interpretada como instalação sem triggers standard.
- A extensão não pode quebrar a leitura de pacotes IKP 0.1 já emitidos.
- Se a semântica da capability mudar de forma incompatível, usar versionamento de capability/contrato conforme as regras IKP; não alterar silenciosamente o significado de uma capability existente.
- A decisão entre extensão aditiva de capability e nova capability fica pendente até à auditoria final do catálogo de operações e dos schemas executáveis.

## 5. Catálogo PHC Standard — modelo lógico

Reutilizar os conceitos já existentes no modelo de domínio 0.2. Não criar um segundo catálogo nem assumir que cada conceito tem de corresponder imediatamente a uma tabela isolada.

### 5.1 StandardCatalog

Representa um catálogo de referência de uma família/produto PHC e do seu âmbito.

Informação lógica:
- identificador estável;
- produto/linha PHC;
- descrição e estado;
- política de curadoria;
- datas de criação/atualização.

### 5.2 CatalogEntry

Representa a identidade lógica do objeto standard, independente de uma versão específica.

Para triggers:
- kind = SQL Trigger;
- schema e nome lógico;
- objeto/tabela pai esperado;
- chave de correspondência canónica;
- estado (ativo, descontinuado, em revisão).

A identidade lógica não deve incluir o hash da definição, pois uma alteração entre versões PHC não deve criar uma identidade inteiramente nova quando o objeto continua a ser o mesmo.

### 5.3 CatalogEntryVersion

Representa a definição e os metadados de uma entrada para uma versão/build ou intervalo de aplicabilidade PHC.

Informação lógica:
- CatalogEntryId;
- produto/versão/build e intervalo de aplicabilidade;
- metadados esperados (schema, parent, eventos, tipo);
- DefinitionStatus da fonte;
- definição SQL desencriptada, quando disponível;
- DefinitionFingerprint SHA-256;
- data/estado de validação;
- origem de importação;
- notas de compatibilidade e substituição.

A definição SQL é conteúdo de referência e deve ser persistida no SQL Server, por exemplo como texto Unicode, com hash calculado sobre bytes UTF-8 normalizados segundo política explícita. A política de canonicalização/hash tem de ser definida antes de comparar definições recolhidas em ambientes diferentes. O hash da fonte não deve ser confundido com o hash bruto de um artefacto IKP se as regras de encoding/canonicalização diferirem.

### 5.4 KnowledgeEntry e KnowledgeEntryVersion

Guardam explicação funcional curada, ligada à entrada e, quando necessário, à versão de catálogo aplicável.

Conteúdo possível:
- finalidade da trigger;
- quando é executada;
- efeitos funcionais e técnicos;
- tabelas/campos afetados;
- dependências conhecidas;
- pressupostos e limitações;
- nível de validação e autor/revisor;
- referências a evidências e versões analisadas.

A explicação não substitui a definição SQL. Deve ser possível consultar a explicação mesmo quando a definição instalada está indisponível, sem sugerir que o corpo instalado foi verificado.

### 5.5 Proveniência da importação

Cada lote/importação deve preservar:
- nome e identificação da fonte;
- SHA-256 dos bytes originais;
- formato e encoding;
- data de aquisição/importação;
- produto e versão/build PHC declarados;
- âmbito da fonte (por exemplo, conjunto de triggers standard);
- ferramenta/versão de importação;
- resultado da validação;
- responsável/revisor e decisão de curadoria.

A proveniência pode ser modelada como entidade de importação ligada às versões de catálogo. A forma física final (tabela própria ou estrutura equivalente) será decidida no desenho SQL; a rastreabilidade não é opcional.

O ficheiro `docs/sources/triggers phc.sql` é fonte de importação e análise. Depois da importação validada, o runtime do Portal não deve depender da presença desse ficheiro. Não o apagar nesta fase: preservar a fonte e avaliar a política de arquivo/licenciamento depois de provar a incorporação integral.

## 6. Materialização das observações IKP

A observação de trigger é um facto de uma Capture, não uma entrada do catálogo standard.

A materialização deve respeitar o modelo de domínio 0.2:

- Observation identifica o objeto observado na Capture.
- SourceObjectId é único no âmbito da Capture, não é identidade permanente.
- MetadataFingerprint e DefinitionFingerprint são distintos.
- DefinitionStatus mantém a semântica do contrato IKP.
- PackageReceipt e Capture preservam a proveniência da receção.
- A observação mantém-se mesmo quando não existe correspondência no catálogo.
- A correspondência ao catálogo é uma relação/projeção da plataforma, não altera o facto original.
- Observações de instalações permanecem isoladas por Organization; o catálogo standard é global e curado.

A materialização deve ser idempotente por CaptureId + SourceObjectId e não deve regravar conteúdo imutável se a mesma evidência já tiver sido materializada. A política de atualização de metadados derivados e resultados de matching deve ser separada da persistência do facto original.

## 7. Matching e comparação

### 7.1 Matching por etapas

A correspondência deve ser conservadora e explicável:

1. Restringir candidatos ao produto/versão/build PHC aplicável, quando conhecido.
2. Comparar kind, schema, nome e objeto pai.
3. Comparar eventos e outros metadados estruturais.
4. Usar identificadores PHC estáveis, quando existirem e forem aplicáveis ao tipo de objeto.
5. Se houver mais do que um candidato ou conflito, manter estado ambíguo/pendente de revisão.

Nome isolado não é chave suficiente. O nome da tabela pai e os eventos aumentam a força da correspondência, mas não provam equivalência do corpo SQL.

### 7.2 Estados de comparação propostos

| Estado | Significado |
|---|---|
| `StandardMatched` | Observação associada a uma entrada standard com correspondência suficientemente forte |
| `StandardNotObserved` | Entrada esperada não observada numa captura com coverage válida e suficiente |
| `AdditionalObject` | Objeto observado sem correspondência no catálogo consultado |
| `MetadataMismatch` | Candidato provável, mas metadados estruturais divergem |
| `DefinitionMatched` | Definição disponível, comparável e coincidente segundo a política de hash/canonicalização |
| `DefinitionDifferent` | Definição disponível e comparável, mas divergente |
| `DefinitionUnverifiable` | Não há definição instalada comparável, por indisponibilidade, não recolha ou outra limitação |
| `MatchAmbiguous` | Mais do que um candidato plausível ou evidência insuficiente para escolher |

Estes estados não devem ser tratados como um único enum mutuamente exclusivo sem considerar duas dimensões: resultado de identidade/metadados e resultado de verificação da definição. Por exemplo, uma trigger pode estar identificada como standard e ter definição não verificável.

### 7.3 Ausência e coverage

`StandardNotObserved` só pode ser calculado quando:
- a capability/operation foi executada;
- terminou com sucesso, ou tem estado parcial cuja semântica permita provar cobertura completa do subconjunto relevante;
- a população de triggers abrangida está definida;
- não existem erros/limitações que possam ocultar objetos.

Se a operação não foi executada, falhou, está incompleta ou não é suportada pela versão do Scanner, o resultado é `NotEvaluated`/desconhecido, nunca ausência.

### 7.4 Definição coincidente

Só declarar `DefinitionMatched` quando a definição instalada está disponível e foi comparada com a versão de catálogo aplicável através da mesma política de canonicalização. Se a definição estiver indisponível, a correspondência de identidade continua útil, mas a verificação interna permanece não verificável.

## 8. Segurança, privacidade e governação

- Definições standard de referência são conteúdo central curado, não dados de uma organização.
- Definições personalizadas recolhidas de clientes continuam sujeitas a isolamento organizacional, autorização e políticas de partilha.
- Não promover automaticamente código de uma instalação a standard.
- Não inferir comportamento funcional apenas a partir do nome da trigger.
- A importação de fontes standard exige validação técnica, proveniência e revisão humana.
- Aplicar controlos de acesso ao código SQL e às explicações, incluindo auditoria de alterações.
- Preservar versões históricas; descontinuar em vez de apagar quando uma definição deixa de ser aplicável.
- RAG é consumidor futuro do conteúdo validado; não exige embeddings nem base vetorial nesta fase.

## 9. Plano de implementação proposto

A implementação deve ser faseada e cada fase só começa após aprovação da anterior.

| Fase | Âmbito | Critério de conclusão |
|---|---|---|
| 0 — Fecho de contrato | Auditar capabilities, coverage, schemas, exporter, reader e compatibilidade; decidir operação/capability | Contrato de evidência aprovado e fixtures definidas |
| 1 — Scanner | Reutilizar leitura estrutural de triggers no IKP; exportar metadados e estados de definição; atualizar coverage | Captura real demonstra triggers standard e personalizadas; ZIP passa validator e testes |
| 2 — Importação do catálogo | Definir formato de importação, parser/validador, proveniência e revisão humana | Fonte SQL de referência importada integralmente e reconciliada |
| 3 — Persistência standard | Desenhar e implementar entidades/repositorios/migrations SQL Server para catálogo, versões, definições e proveniência | Dados sobrevivem a restart e são consultáveis sem ficheiro-fonte |
| 4 — Materialização | Persistir observações de triggers a partir de IKP com idempotência e isolamento | Captura real materializada sem perda de evidência |
| 5 — Matching | Aplicabilidade, correspondência, coverage-aware absence e comparação de definição | Casos positivos, negativos, ambíguos e indisponíveis cobertos por testes |
| 6 — Consulta no Portal | Apresentar definição standard, explicação, origem e limites de verificação | Utilizador consegue compreender o objeto sem confundir standard esperado com código instalado verificado |

Não começar a fase 1 antes de fechar a fase 0. Não criar migrations durante a auditoria/especificação.

## 10. Testes e critérios de aceitação

### Scanner / IKP
- Trigger standard com definição disponível.
- Trigger standard com definição NULL/indisponível.
- Trigger personalizada em tabela `u_`.
- Trigger adicional em tabela standard sem correspondência no catálogo.
- Trigger desativada e trigger `INSTEAD OF`.
- Um ou vários eventos por trigger.
- Operação sem permissões suficientes.
- Falha parcial da consulta.
- Capability não suportada por Scanner antigo.
- Exportação, checksums, leitura e validação de ZIP IKP 0.1.
- Compatibilidade com fixtures e pacotes antigos.

### Catálogo
- Importação repetida idempotente da mesma fonte/hash.
- Nova versão PHC com definição alterada.
- Fonte com objeto duplicado ou metadados contraditórios.
- Definição SQL ausente/inválida.
- Proveniência completa e verificável.
- Revisão/curadoria antes de publicação.
- Consulta depois de reiniciar a aplicação, sem acesso ao ficheiro SQL original.

### Matching
- Correspondência exata de identidade e metadados.
- Nome igual em tabela diferente.
- Mesmo objeto com eventos divergentes.
- Vários candidatos.
- Definição igual após canonicalização.
- Definição diferente.
- Definição indisponível.
- Ausência com coverage completa.
- Ausência sem coverage ou com operação falhada.

## 11. Decisões pendentes antes da implementação

1. **Aplicabilidade da fonte:** identificar a versão/build PHC exata a que corresponde `triggers phc.sql` e confirmar a relação com a CS Default usada como referência.
2. **Chave de matching:** aprovar a combinação de kind, schema, nome, parent e eventos, e determinar se existem stamps/identificadores PHC estáveis para triggers.
3. **Contrato de capability:** escolher entre extensão compatível de uma capability SQL existente ou nova capability/operação, depois de inspecionar o catálogo completo e as regras de coverage.
4. **Semântica de indisponibilidade:** definir quais as causas que o Scanner consegue distinguir de forma fiável e quais permanecem genericamente `Unavailable`.
5. **Hash de definição:** fixar encoding e canonicalização para comparar SQL de referência e SQL instalado sem falsos positivos por diferenças irrelevantes.
6. **Governação da fonte:** confirmar responsável pela validação funcional e política de preservação/licenciamento do SQL desencriptado.

Nenhum destes pontos deve ser preenchido por suposição.

## 12. Fora de âmbito

Esta especificação não aprova:
- implementação de procedures, funções, views ou regras de negócio standard; apenas estabelece uma arquitetura extensível;
- remoção de REL/DTL do Scanner ou alterações ao D-015;
- mudanças ao contrato IKP sem decisão de compatibilidade;
- mudanças ao schema SQLite/workspace;
- migrations, alterações ao código, endpoints ou UI;
- promoção automática de personalizações para conhecimento standard;
- implementação de RAG, embeddings ou base vetorial;
- alteração da política de arquivo dos ZIPs IKP.

## 13. Referências canónicas

- [Modelo de domínio 0.2](knowledge-domain-model-0.2.md)
- [Camadas de conhecimento e proveniência](knowledge-layers.md)
- [Especificação IKP 0.1](ikp-0.1-specification.md)
- [Contrato P1 — identidade e proveniência](p1-identity-provenance-contract.md)
- [Matriz de compatibilidade Scanner/IKP](ikp-0.1-scanner-compatibility-matrix.md)
