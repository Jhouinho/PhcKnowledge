# Modelo de Domínio do Knowledge Store

**Estado:** proposta de arquitectura para revisão — não constitui contrato físico nem autorização para migrations  
**Data:** 2026-11-27  
**Âmbito:** documentação, conteúdo da Enciclopédia PHC, dicionário de dados, conhecimento técnico/funcional, relações e evidências.

> Esta proposta não substitui os contratos normativos IKP/P1/D-015 a D-023. Os conceitos aqui apresentados ainda não são entidades físicas aprovadas. Qualquer implementação exige validação do modelo, revisão dos schemas existentes e decisão explícita.

## 1. Objectivo

Definir um modelo de domínio comum para receber, preservar, estruturar, relacionar, pesquisar e reutilizar conhecimento PHC proveniente de fontes distintas, sem confundir:

1. fonte original;
2. conteúdo preservado ou extraído;
3. factos observados;
4. projecções e interpretações;
5. conhecimento validado e reutilizável;
6. apresentação visual no Portal.

O modelo deverá suportar documentação técnica e funcional, Enciclopédia PHC, dicionário de dados, conhecimento PHC Standard, observações de instalações importadas por IKP, personalizações, relações, evidências, validações humanas, pesquisa e Explorador Visual.

Não se pretende criar uma aplicação documental paralela nem um repositório indiferenciado de ficheiros. Pretende-se um Knowledge Store relacional, rastreável e evolutivo, apresentado através da experiência única do PHC Knowledge.

## 2. Fontes analisadas e enquadramento

A proposta foi construída a partir dos documentos existentes na main, em particular:

- PROJECT_GUIDELINES.md e README.md;
- docs/architecture/decisoes-aprovadas-2026-11-21.md;
- docs/architecture/product-boundaries.md;
- docs/architecture/knowledge-layers.md;
- docs/architecture/p1-identity-provenance-contract.md;
- docs/architecture/ikp-0.1-specification.md;
- docs/architecture/observation-identity-and-lineage-contract.md;
- docs/architecture/d-023-cross-installation-identity-correspondence-contract.md;
- docs/architecture/d-023-objectkind-recognition-matrix-2026-11-24.md;
- docs/architecture/collector-operation-objectkind-matrix-2026-11-24.md;
- docs/architecture/roadmap-visual-phc-knowledge-2026-11-06.md;
- docs/audits/identity-provenance-cross-capture-assessment-2026-11-24.md;
- docs/audits/investigacao-funcional-csdefault-ft-fi-td-2026-11-25.md;
- docs/audits/recuperacao-investigacao-funcional-vo-dossiers-2026-11-25.md.

A pesquisa de ficheiros do GitHub não devolveu resultados indexados nesta sessão; por isso, os documentos acima foram abertos directamente por caminho conhecido. A proposta não afirma uma auditoria integral de cada ficheiro de código, schema executável, migration ou recurso binário do repositório. O ZIP real da Enciclopédia não está versionado e o seu conteúdo integral não foi inspeccionado nesta análise.

### 2.1 Decisões existentes que condicionam o modelo

Estas decisões são restrições já aprovadas e não são reabertas por esta proposta:

- Existe uma única organização interna. Os clientes PHC são contextos técnicos, não tenants nem organizações de autenticação.
- A relação de contexto é Cliente → Instalação → Capturas. Cada base de dados PHC registada tem identidade de instalação; cópias/restauros recebem identidade própria e linhagem.
- A base de dados central é Microsoft SQL Server. PostgreSQL está excluído; edição e versão concretas continuam por decidir.
- O Scanner é autónomo e não é executado pelo Knowledge em runtime. A transferência é feita por pacote IKP através do Portal.
- O ZIP IKP é transporte temporário. Dados estruturados, histórico, metadados e artefactos originais necessários devem ser materializados no SQL Server. A eliminação do ZIP depende de verificação integral.
- O Scanner recolhe factos e evidências; o Knowledge compara, interpreta, classifica, materializa e mantém histórico.
- Factos originais, interpretações derivadas, anotações humanas e conhecimento generalizado permanecem separados.
- REL/DTL são factos declarados pela instalação, não equivalentes a foreign keys SQL inferidas.
- Relações estruturais, dependências funcionais e referências textuais são categorias distintas.
- Identidade da origem, identidade lógica, observação, fingerprint, correspondência e linhagem são conceitos separados.
- Identidades de instalações diferentes não são fundidas. Correspondências cross-installation são relações auditáveis e não equivalem a identidade, igualdade de definição ou autorização de reutilização.
- PHC Standard é mantido num catálogo central, curado e versionado.
- Ausência só pode sustentar remoção quando a operação relevante e a cobertura permitem essa conclusão.
- Conteúdo original não pode ser substituído por projecções ou descrições geradas.

### 2.2 Estado real da implementação

A documentação consultada regista como existentes na main o Scanner e exportador IKP, Receive/Validate/Archive, receipts e recuperação, análise IKP A+B, Object Index v2, PackageImportAnalysis 0.3 e o fluxo de decisão do Incremento C.

A mesma documentação identifica como ainda por construir ou validar:
- associação completa Cliente/Instalação/Captura no fluxo do Portal;
- catálogo PHC Standard operacional e comparação completa;
- materialização persistente de Capture, Observation, REL/DTL, metadados e artefactos no modelo de conhecimento;
- verificação pós-materialização e eliminação segura do ZIP;
- modelo físico final de armazenamento de artefactos;
- matriz completa validada por collector/operation/ObjectKind;
- pesquisa contextual, comparação histórica e RAG.

A existência de contratos de transporte e análise IKP não significa que o Knowledge Store proposto já exista.

## 3. Princípios do modelo

### P1 — Fonte, conteúdo e conhecimento são distintos
Uma fonte identifica a origem e o contexto de aquisição. O conteúdo é o material preservado ou extraído. O conhecimento é uma afirmação, interpretação, regra ou explicação que pode ser relacionada com outros elementos e sujeita a validação.

### P2 — Preservar o original; apresentar uma representação própria
As fontes fornecem conteúdo, não interfaces. O PHC Knowledge controla integralmente a experiência visual.

Para a Enciclopédia PHC, importamos exclusivamente o conteúdo editorial: textos, imagens, links e a estrutura semântica/editorial necessária para o apresentar. Não reproduzimos layout, navegação, CSS, componentes visuais ou identidade gráfica da aplicação original.

A documentação segue o mesmo princípio: o conteúdo é preservado e estruturado, mas a consulta e apresentação pertencem ao Portal e ao sistema de design Luminous Knowledge.

### P3 — Conteúdo editorial não é modelo técnico
Um artigo da Enciclopédia não é uma tabela PHC. Uma imagem não é um artefacto SQL. Um link não é automaticamente uma relação técnica. O modelo permite relacioná-los sem os converter no mesmo tipo de entidade.

### P4 — Factos não são interpretações
Um campo observado no SQL Server, uma relação REL/DTL, uma foreign key, uma menção num artigo e uma conclusão humana têm naturezas e garantias diferentes.

### P5 — A proveniência acompanha o conhecimento
Conteúdo, observação, afirmação e relação devem poder ser rastreados até à fonte, versão, localização no documento/artigo, captura, artefacto ou evidência.

### P6 — Standard e instalação não se confundem
O conhecimento standard é curado e versionado centralmente. Observações e personalizações pertencem à instalação de origem. A promoção de conhecimento específico para geral exige curadoria humana.

### P7 — O grafo é uma projecção
O Explorador Visual consulta entidades e relações persistidas. Não mantém um grafo paralelo nem inventa ligações.

### P8 — Pesquisa é derivada
Indexação, fragmentação, embeddings e outras projecções de pesquisa são regeneráveis. Não substituem conteúdo original nem identificadores de domínio.

### P9 — Versionamento sem reescrita histórica
Uma nova versão de fonte, documento, artigo, objecto standard ou conteúdo observado cria uma nova versão/observação. Não se reescreve o passado.

### P10 — O modelo físico fica para uma fase posterior
Os nomes deste documento são conceitos de domínio. Não implicam automaticamente uma tabela SQL por entidade.

## 4. Visão global do domínio

A arquitectura conceptual tem cinco áreas:

1. **Source & Acquisition:** identificação da fonte, versões e operações de aquisição/importação.
2. **Content:** documentos e conteúdo editorial preservado, incluindo a Enciclopédia.
3. **Technical Knowledge:** catálogo standard, contexto de instalações, dicionário de dados, observações e artefactos.
4. **Knowledge & Evidence:** afirmações, relações, evidências, interpretação e validação.
5. **Presentation & Search Projections:** representações derivadas para consulta, pesquisa e grafo.

Fluxo conceptual:

- Source tem SourceVersion e origina Acquisition.
- Acquisition preserva ou extrai OriginalContent.
- OriginalContent pode originar DocumentVersion, EncyclopediaEntry, MediaAsset ou TechnicalArtifact.
- TechnicalEntity é observada numa Installation/Capture ou definida no catálogo PHC Standard.
- Observation liga entidade técnica, captura e artefactos.
- KnowledgeItem contém KnowledgeClaims.
- KnowledgeClaim e KnowledgeRelation são sustentadas por Evidence.
- Evidence aponta para localização concreta numa fonte, documento, artigo, artefacto ou observação.
- Pesquisa e grafo projectam estes elementos sem se tornarem fontes autónomas.

Este fluxo é conceptual; cardinalidades e chaves físicas ainda não estão fechadas.

## 5. Entidades conceptuais

### 5.1 Source
Representa uma origem, não o conteúdo em si.

Exemplos: Enciclopédia PHC, manual técnico, script SQL de triggers, documentação interna, pacote IKP, catálogo PHC Standard ou relatório de investigação.

Atributos conceptuais: identificador, tipo, designação, produtor, referência de origem, versão/build/edição quando conhecida, âmbito de aplicabilidade, restrições de utilização/licenciamento, datas e histórico.

Uma fonte pode ser externa, interna, técnica, editorial ou uma aquisição de instalação. Não usar um único campo textual para misturar tipo de fonte e classificação semântica do conteúdo.

### 5.2 SourceVersion
Representa uma versão identificável da fonte: versão/build da Enciclopédia, edição de manual, versão de script SQL, catálogo standard ou documento interno.

Preserva versão declarada, datas de publicação/produção quando conhecidas, data de aquisição, hash de integridade do original, referência ao conteúdo preservado, estado de processamento e limitações.

Não inventar versão de produto quando a fonte não a declara. Data de aquisição não é data de publicação.

### 5.3 Acquisition
Regista uma operação de entrada/actualização: fonte e versão recebida, momento e via de aquisição, resultado, avisos, erros, contagens e possibilidade de retry idempotente.

No caso IKP, Acquisition não substitui PackageReceipt, PackageImportAnalysis, decisões append-only nem o ciclo de materialização existente. A integração deve reutilizar contratos e identificadores existentes, não duplicá-los.

### 5.4 OriginalContent
Representa conteúdo original preservado, binário ou textual: PDF, DOCX, SQL, imagem, HTML/pacote editorial ou artefacto técnico original.

Propriedades conceptuais: media type/formato, encoding, tamanho, hash de integridade, localização lógica, origem/versão, política de retenção e restrições.

A decisão aprovada determina que artefactos originais necessários são persistidos no SQL Server central. A estratégia física concreta (tipo de coluna, FILESTREAM ou alternativa suportada) continua por decidir. Não introduzir armazenamento externo permanente sem decisão explícita.

### 5.5 Document e DocumentVersion
Representam documentação técnica ou funcional que não é a Enciclopédia. Document identifica o recurso lógico; DocumentVersion representa uma versão concreta.

Deve permitir documentos com conteúdo extraído e/ou original binário, secções, títulos, páginas/âncoras, tabelas, listas, imagens, links, metadados, relações com fontes e entidades técnicas e histórico de versões.

Não assumir que todos os formatos têm a mesma estrutura de extracção. A representação normalizada deve preservar referência ao original e localização da evidência.

### 5.6 EncyclopediaEntry
Representa um artigo/conteúdo lógico da Enciclopédia PHC.

A Enciclopédia é uma fonte de conteúdo. O Knowledge importa e preserva:
- textos, títulos e subtítulos;
- hierarquia e ordem editorial;
- listas e tabelas;
- imagens e legendas;
- links internos e externos;
- referências e identificadores originais, quando existam.

Não importa nem replica layout, navegação, CSS, temas, menus, componentes, scripts de interface ou identidade visual original.

O Portal renderiza o conteúdo através do sistema Luminous Knowledge. A estrutura editorial preserva significado e ordem; não transporta a apresentação visual da origem.

#### EditorialContentBlock
Unidade ordenada de conteúdo de um artigo. Exemplos possíveis: heading, paragraph, lista, tabela, referência de imagem e link. Só devem ser criados tipos para elementos efectivamente encontrados na fonte real.

Cada bloco preserva ordem, conteúdo, atributos semânticos, referência de origem e âncoras. A representação deve ser independente de React, HTML específico do Portal ou componentes visuais. O renderer decide tipografia, espaçamento, cores, disposição e comportamento.

#### MediaAsset
Representa imagem ou recurso visual original associado ao conteúdo. Preserva original, hash, formato/dimensões quando disponíveis, legenda/texto alternativo original, posição, ligação ao bloco, proveniência e direitos/restrições.

Não gerar imagens substitutas automaticamente nem remover imagens porque o layout original não será reproduzido.

#### ContentLink
Representa link encontrado no conteúdo: URL/destino original, texto âncora, tipo de destino quando determinável, posição, origem e estado de resolução se verificado.

Um link não é automaticamente uma relação de domínio confirmada. Uma resolução de link interno pode criar associação navegável, mas preserva URL/texto original e evidência da resolução.

### 5.7 TechnicalEntity
Representa entidade técnica identificável: tabela, coluna, índice, chave/constraint, trigger, stored procedure, função, view, objecto PHC personalizado ou elemento do catálogo PHC Standard.

Para objectos observados aplicam-se P1, IKP e D-023:
- SourceObjectId é referência da origem na captura;
- SourceIdentity é evidência nativa com scheme e scope;
- EntityIdentity pertence à instalação;
- Observation identifica a observação numa captura;
- fingerprints são dimensões de conteúdo, não identidade;
- Correspondence é relação auditável, não merge de identidades.

A identidade standard é independente das identidades locais das instalações.

### 5.8 DataDictionaryObject e DataDictionaryField
O Dicionário de Dados é um domínio estruturado, não um Excel nem uma lista informal.

DataDictionaryObject representa estrutura identificada num âmbito: motor/edição/versão, base de dados/catálogo, schema, nome original, tipo, classificação, versão/observação e proveniência.

DataDictionaryField representa coluna/campo: nome original, ordinal, tipo de dados, comprimento/precisão/escala, nulabilidade, default, identidade/autoincremento, collation quando aplicável, descrição documentada e proveniência/versão.

O dicionário poderá incluir índices, constraints, chaves e relações estruturais, condicionado pelo inventário de ObjectKinds e contrato de materialização.

Distinções obrigatórias:
- estrutura física observada ≠ descrição funcional;
- PK/FK SQL ≠ REL/DTL;
- nome do campo ≠ significado funcional comprovado;
- campo existente numa instalação ≠ campo standard;
- ausência numa captura parcial ≠ remoção.

A consulta deverá permitir comparar estrutura observada por instalação/captura com referência standard aplicável sem as fundir.

### 5.9 Observation e TechnicalArtifact
Estas entidades são condicionadas pelos contratos existentes, não uma redefinição dos mesmos.

Observation é a ocorrência de uma entidade técnica numa captura, derivada na plataforma com base em InstallationId, CaptureId e SourceObjectId (e objectKind quando necessário), conforme P1.

TechnicalArtifact é conteúdo técnico original ou derivado, classificado segundo D-018, com hash de integridade, formato, encoding, origem, transformação e referências.

Deve preservar as dimensões independentes de D-018: origem do conteúdo (source-content, derived-projection, generated-description, generated-diagnostic), natureza semântica e proveniência. A referência definition.artifactIds fica reservada a artefactos que constituem efectivamente a definição técnica do objecto.

Não duplicar semântica do Object Index nem criar identidade paralela incompatível com P1.

### 5.10 KnowledgeItem e KnowledgeClaim
Representam conhecimento produzido ou curado no PHC Knowledge, não simples conteúdo importado.

KnowledgeItem é um artefacto consultável: explicação funcional, procedimento técnico, solução/implementação reutilizável, regra de negócio, nota de investigação, conclusão validada ou orientação de diagnóstico.

KnowledgeClaim é uma afirmação atómica que pode ser sustentada por evidências e relacionada com entidades. Esta separação permite que um artigo/explicação contenha afirmações com diferentes níveis de certeza.

Cada claim deve permitir texto, tipo, âmbito, estado de validação, autor/método, data/versão, evidências de suporte e contraditórias e relações com entidades, documentos e blocos editoriais.

Não converter automaticamente todo o texto de documento/artigo em claims factuais. Extracção automática pode propor candidatos, mas natureza e validação ficam explícitas.

### 5.11 KnowledgeRelation
Representa relação tipada entre dois elementos. Não substitui todas as relações existentes.

Famílias conceptuais:
- relação estrutural SQL;
- relação declarada PHC REL;
- relação declarada PHC DTL;
- interpretação de expressão;
- referência documental/editorial;
- dependência técnica;
- efeito comportamental observado em código;
- relação funcional validada;
- correspondência cross-installation;
- linhagem entre instalações/fontes/versões.

Cada relação preserva tipo/direcção, endpoints tipados, origem/âmbito, evidências, método, versão do analisador/política, estado e histórico de decisões.

Não agrupar relações apenas porque ligam o mesmo par de tabelas. Não transformar menções textuais em dependências. Não representar como seta directa uma cadeia de efeitos indirectos.

O Scanner distingue RelationEvidence, KnowledgeRelation e FieldMapping. A persistência central deve respeitar esta separação e preservar evidências, sem importar automaticamente o modelo interno do Scanner como esquema físico.

### 5.12 Evidence
Representa a prova que sustenta uma afirmação ou relação.

Exemplos: registo REL/DTL, FK SQL, fragmento de trigger, definição de procedure, campo/valor observado, parágrafo/página/bloco da Enciclopédia, secção de manual, resultado de consulta/teste controlado ou decisão humana fundamentada.

A evidência deve apontar para fonte/versão, aquisição/captura, objecto/artigo/documento, localização (linha, página, secção, bloco, campo ou fragmento), conteúdo ou referência imutável, método, integridade e limitações.

Uma citação genérica a um documento pode ser referência útil, mas não deve ser apresentada como evidência precisa de uma afirmação sem localização suficiente.

### 5.13 Validation / Review
Regista decisões humanas e processos de validação sem alterar factos de origem.

Deve suportar confirmar/rejeitar relação ou claim, pedir revisão, revogar validação anterior e registar responsável, data, motivo e evidência. Decisões relevantes mantêm histórico append-only.

Validar uma afirmação não transforma retroactivamente a fonte. Classificação de objecto, correspondência entre identidades e igualdade de definições continuam dimensões distintas.

## 6. Modelo específico da Enciclopédia

Regra de produto: **importamos conteúdo; não importamos a aplicação nem o seu aspecto visual.**

Pipeline editorial proposto:
1. registar fonte e versão;
2. preservar pacote/ficheiros originais necessários, sujeito a direitos e retenção;
3. extrair artigos e recursos;
4. preservar hierarquia, ordem, textos, imagens, links, tabelas e referências;
5. criar representação editorial neutra e independente da UI;
6. registar proveniência e localização de cada elemento;
7. relacionar artigos com conceitos e entidades técnicas apenas com evidência;
8. renderizar no Portal através de Luminous Knowledge.

A estrutura original pode ser preservada como metadado de origem (identificador e hierarquia), mas não determina navegação nem organização visual do Portal.

### 6.1 Imagens e links
- Preservar imagem original e associação ao artigo.
- Preservar legenda, texto alternativo e ordem quando existirem.
- Preservar URLs e texto âncora.
- Distinguir link original de destino resolvido pelo Knowledge.
- Registar links indisponíveis sem apagar o original.
- Não converter automaticamente URLs externas em entidades.
- Não incorporar recursos externos sem avaliar direitos, segurança e política de aquisição.

### 6.2 Lacunas sobre a Enciclopédia
A documentação consultada refere uma Enciclopédia PHC disponibilizada em ZIP e usada como fonte na investigação FT/FI/TD. O ZIP não está versionado e o conteúdo integral não foi inspeccionado nesta análise do GitHub. Ainda não estão confirmados formato interno, estrutura de artigos/IDs, representação de imagens/links, formatos de conteúdo, direitos/licenciamento, retenção ou mecanismo de extracção incremental.

Estas questões têm de ser resolvidas a partir do pacote real antes de fechar schema editorial ou implementar importador.

## 7. Modelo específico do Dicionário de Dados

O dicionário reúne duas perspectivas sem as confundir:

1. **Dicionário observado:** estruturas físicas recolhidas de uma base de dados/instalação/captura.
2. **Referência standard:** catálogo central versionado de estruturas PHC standard, associado à versão/build aplicável.

A consulta compara as perspectivas, mas cada observação mantém identidade e proveniência.

Conteúdo previsto:
- motores, bases de dados e schemas;
- tabelas e views;
- colunas/campos;
- tipos, comprimentos, precisão e nulabilidade;
- defaults e propriedades relevantes;
- PK, FK, índices e constraints;
- REL/DTL como factos separados;
- descrições documentadas e interpretações funcionais;
- versão PHC/SQL e contexto da observação.

O significado funcional de um campo não é preenchido por inferência do nome. Cada descrição deve indicar origem (documentação, enciclopédia, fonte interna ou interpretação), versão/âmbito, evidência, validação e se é específica de instalação.

O mesmo campo pode ter descrição standard, observações de uso numa instalação e interpretações adicionais; estas não se sobrescrevem.

O Scanner mantém a fronteira de aquisição e os contratos IKP. O Knowledge materializa os objectos observados, artefactos, REL/DTL, cobertura e proveniência. Não criar um segundo mecanismo de leitura SQL em runtime no Portal para alimentar o dicionário.

## 8. Proveniência, identidade e versionamento

Esta secção não redefine P1, IKP, D-015, D-016 ou D-023.

### 8.1 Percursos de proveniência
- Conteúdo editorial: Source → SourceVersion → Acquisition → Document/EncyclopediaEntry → bloco/media/link.
- Estrutura técnica: Installation → Capture → Observation → TechnicalEntity/TechnicalArtifact.
- Conhecimento standard: Source → SourceVersion/CatalogueVersion → StandardEntity/StandardDefinition.
- Claim/relação: KnowledgeItem/Claim/Relation → Evidence → origem concreta.

Não obrigar todos os tipos a preencher campos sem significado. Usar referências tipadas à origem e localização adequada a cada família.

### 8.2 Identidade versus conteúdo
Manter separadas identidade da fonte, identidade lógica do conteúdo/documento, identidade da entidade técnica, identidade da observação, identidade do artefacto, hash de integridade, fingerprint semântico e correspondência entre entidades.

Um hash pode provar igualdade de bytes; não prova identidade lógica, significado funcional ou equivalência semântica.

### 8.3 Versionamento
- Fonte alterada: nova SourceVersion.
- Documento alterado: nova DocumentVersion.
- Artigo alterado: nova versão editorial, preservando anterior e origem.
- Catálogo standard alterado: nova versão de catálogo/entidade de referência, com aplicabilidade explícita.
- Nova captura: novas Observations, mantendo histórico.
- Claim/relação revista: nova decisão/versão ou evento de revisão, sem apagar evidência anterior.
- Projecção de pesquisa reconstruída: nova versão técnica da projecção, sem alterar a fonte.

Estratégia exacta de versionamento por domínio e política de retenção continuam por especificar.

## 9. Standard, instalação e promoção de conhecimento

| Categoria | Âmbito | Regra |
|---|---|---|
| PHC Standard | Catálogo central versionado | Curadoria e aplicabilidade por versão |
| Observação de instalação | Uma Installation e Capture | Facto contextual, não generalizado automaticamente |
| Personalização/desenvolvimento | Instalação de origem | Isolado; não partilhado automaticamente |
| Interpretação derivada | Âmbito explícito | Método, evidência e incerteza preservados |
| Conhecimento validado | Âmbito/aplicabilidade definidos | Validação auditável |
| Conhecimento promovido | Catálogo geral interno, conforme governação futura | Curadoria humana e proveniência |

Uma relação com PHC Standard não promove automaticamente o objecto da instalação. Correspondência não autoriza reutilização. Promoção exige revisão de conteúdo, privacidade, direitos, aplicabilidade e evidência.

Permanece em aberto se haverá catálogo geral de conhecimento interno separado do catálogo PHC Standard ou uma categoria governada dentro do mesmo domínio.

## 10. Pesquisa e Explorador Visual

### 10.1 Pesquisa
A pesquisa deverá consultar projecções derivadas de texto documental, artigos/blocos da Enciclopédia, nomes/descrições do dicionário, artefactos/metadados autorizados, KnowledgeItems/claims, relações e evidências.

Cada resultado mantém ligação à origem e contexto. A pesquisa não apresenta uma projecção como se fosse o original.

SQL Server é a base central. A tecnologia concreta de indexação deve respeitar a avaliação existente e ser escolhida após conhecer volume, edição/versão, idiomas e requisitos. Full-Text Search foi identificado como hipótese inicial a validar, não como decisão física fechada.

Embeddings/RAG são capacidades futuras, não pré-requisito para o primeiro Knowledge Store.

### 10.2 Explorador Visual
O grafo é uma projecção navegável do domínio, com o estilo acordado: caixas, cores por categoria, setas direccionais, legendas nas ligações e agrupamento por área funcional.

Deve permitir visão global por domínio, expansão/recolha de vizinhanças, pesquisa de nós, filtros por categoria/instalação/versão/origem/estado, percursos por processo, consulta de campos/triggers/procedures/evidências e abertura da fonte.

Deve distinguir visualmente relações formais, código observado, dependências validadas e hipóteses. A ligação contínua/tracejada é apenas uma convenção visual a formalizar; não altera o estado de evidência.

Não criar relação universal opaca. O grafo unifica a navegação, mas preserva FK SQL, REL, DTL, FieldMapping, dependência detectada em código, efeito observado em execução, referência documental, correspondência D-023 e interpretação humana como tipos distintos.

## 11. Armazenamento: decidido e por decidir

### Decidido
- SQL Server central como armazenamento permanente.
- Dados estruturados, metadados, histórico e artefactos originais necessários no SQL Server.
- ZIP IKP temporário, eliminado apenas após verificação.
- SQLite apenas no workspace/cache local do Scanner.
- O funcionamento normal do Portal não deve depender da presença de fontes de importação no repositório após incorporação verificável do conteúdo necessário.

### Por decidir
1. edição/versão concreta do SQL Server;
2. estratégia física para binários (varbinary(max), FILESTREAM ou opção suportada);
3. limites de tamanho, particionamento, compressão, backup e recuperação;
4. formato editorial neutro e conjunto mínimo de blocos;
5. retenção de originais e versões antigas, sujeita a direitos/licenciamento;
6. extracção e actualização incremental da Enciclopédia;
7. tecnologia/política de indexação textual;
8. permissões e visibilidade para conteúdos internos, específicos de cliente e sensíveis;
9. deduplicação de binários sem perder proveniência;
10. política de eliminação/arquivo de fontes após importação.

Deduplicar conteúdo binário não significa eliminar proveniência. Um conteúdo pode ser armazenado uma vez e referenciado por várias aquisições.

## 12. Exemplo integrado: artigo sobre documentos de facturação

Exemplo conceptual; não afirma que o artigo ou todas as ligações estejam já importados:

1. Registar fonte Enciclopédia e versão.
2. Importar artigo como EncyclopediaEntry.
3. Preservar títulos, parágrafos, tabelas, imagens e links em blocos ordenados.
4. Apresentar com Luminous Knowledge, sem reproduzir layout original.
5. Relacionar bloco com FT ou FI apenas se o conteúdo sustentar a referência.
6. Registar claims da investigação técnica sobre tr_fi_insert com evidência no script SQL e limitações da versão analisada.
7. Apresentar no grafo artigo, tabelas e trigger com tipos de relação distintos.
8. A ligação FI → SL só aparece como efeito de código observado se condição e evidência do trigger estiverem registadas; não é convertida em FK.
9. Pesquisa devolve artigo e artefactos com origem e âmbito explícitos.

## 13. Sequência recomendada

### Fase A — Inventário e contratos de entrada
- Inventariar formatos reais de documentação e Enciclopédia.
- Rever IKP/Object Index e matriz collector/ObjectKind para determinar o que pode ser materializado.
- Identificar o que já existe no código e migrations do Knowledge Import.
- Separar requisitos cobertos por contratos existentes dos novos.
- Confirmar direitos e restrições das fontes.

### Fase B — Modelo lógico detalhado
- Fechar cardinalidades e identidades por área.
- Definir tipos de relação/evidência.
- Definir versionamento de documentos, artigos, catálogo standard e claims.
- Definir representação editorial neutra.
- Definir como o dicionário consome observações e catálogo standard.
- Definir limites de visibilidade por instalação/fonte.

### Fase C — Modelo físico SQL Server
- Mapear entidades para tabelas e relações.
- Definir chaves, constraints, índices e concorrência.
- Definir armazenamento binário e deduplicação.
- Definir migrations e compatibilidade.
- Validar volumes e consulta com dados representativos.

### Fase D — Materialização e ingestão
- Materializar IKP segundo contratos aprovados.
- Ingerir documentação e Enciclopédia por pipelines separados.
- Preservar originais e proveniência.
- Verificar integridade e recuperabilidade.
- Eliminar ZIP/staging apenas segundo D-016.

### Fase E — Experiência do Portal
- Pesquisa e consulta editorial.
- Dicionário de dados.
- Consulta de evidências e histórico.
- Explorador Visual.
- Comparação e, posteriormente, pesquisa contextual/RAG.

Cada fase termina com avaliação, documentação e validação conjunta. Não iniciar automaticamente a seguinte.

## 14. Questões para decisão

1. **Enciclopédia:** qual é o pacote real, formato interno e política de direitos/retenção? A estrutura editorial final depende dessa inspecção.
2. **Documentação:** que formatos/fontes entram na primeira versão (PDF, DOCX, HTML, Markdown, SQL ou outros)?
3. **Dicionário standard:** qual é a fonte oficial e a estratégia de versionamento do catálogo PHC Standard para além do catálogo estrutural derivado de CS Default e ficheiros de referência?
4. **Conhecimento geral interno:** catálogo separado de PHC Standard ou categoria governada no mesmo domínio?
5. **Visibilidade:** que conteúdos são acessíveis a todos os colaboradores e quais exigem restrição por cliente/instalação?
6. **Armazenamento binário:** qual a opção física SQL Server após avaliação da edição/versão e dimensão esperada?
7. **Formato editorial:** que blocos semânticos são necessários depois de inspeccionar a Enciclopédia real?
8. **Versionamento/retenção:** que política aplicar a versões antigas, fontes substituídas e conteúdos sujeitos a direitos?

## 15. Conclusão

A proposta é um Knowledge Store comum, com áreas especializadas e fronteiras explícitas:

- documentação e Enciclopédia preservam conteúdo editorial e proveniência;
- o dicionário representa estruturas e descrições técnicas versionadas;
- o catálogo PHC Standard representa referência curada e aplicável por versão;
- instalações fornecem observações e artefactos através do IKP;
- KnowledgeItems/Claims representam conhecimento interpretado e reutilizável;
- Evidence e KnowledgeRelation ligam afirmações e elementos sem apagar a natureza das fontes;
- pesquisa e grafo são projecções do conhecimento persistido;
- o Portal controla toda a apresentação visual.

A Enciclopédia fornece textos, imagens, links e estrutura editorial; não fornece o design do Portal. O Dicionário de Dados é estruturado e rastreável; não é uma lista informal de campos. O grafo apresenta relações com tipo e evidência; não é uma fonte autónoma de factos.

**Estado final:** base conceptual para revisão. Não aprova por si só tabelas SQL, migrations, schemas IKP, alterações ao Scanner, importadores ou implementação da UI.
