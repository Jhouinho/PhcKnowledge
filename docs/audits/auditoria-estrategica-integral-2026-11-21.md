# Auditoria estratégica e técnica integral — PHC Knowledge

- **Data:** 21-11-2026
- **Repositório:** `Jhouinho/PhcKnowledge`
- **Branch deste documento:** `docs/auditoria-estrategica-2026-11-21`
- **Base de código de referência da auditoria:** `main`, com fotografia identificada durante a auditoria no commit `b8a188175eb6792210e2a242bc237c6747b19f68`
- **Natureza:** relatório de auditoria e preservação de contexto; não é uma aprovação de decisões nem uma autorização de implementação.
- **Estado:** análise consolidada; alinhamento estratégico e decisões técnicas ainda por fechar.

> Este documento regista o diagnóstico produzido numa revisão intensiva do repositório, da documentação, das branches, dos contratos e do estado funcional conhecido. Distingue factos observados, interpretação, propostas e decisões pendentes. Não deve ser usado para converter propostas em decisões aprovadas.

---

## 1. Objectivo e âmbito

O objectivo desta auditoria foi fazer uma pausa antes de continuar a acrescentar funcionalidades e responder a cinco perguntas:

1. O que é, afinal, o PHC Knowledge e para quem está a ser construído?
2. O que está realmente implementado e integrado na `main`?
3. Que trabalho existe em branches e pull requests, e qual o seu grau de maturidade?
4. Que contratos, decisões e documentos estão desactualizados, contraditórios ou incompletos?
5. Qual é a sequência mais segura para transformar a base técnica actual numa plataforma de conhecimento útil no dia-a-dia?

A revisão incidiu sobre:
- estrutura do repositório e documentação;
- Scanner e collectors;
- formato IKP e pipeline de recepção/análise;
- identidade, proveniência e fingerprints;
- relações REL/DTL e catálogo PHC Standard;
- materialização e ciclo de vida do ZIP;
- backend, autenticação e Portal;
- branches, pull requests e evidência de CI;
- pesquisa, reutilização do conhecimento e RAG;
- roadmap e governação documental.

A auditoria foi de leitura. **Não foram alterados ficheiros de produto, migrations, código ou decisões na `main`.** Este relatório é o primeiro artefacto criado para preservar o diagnóstico.

## 2. Síntese executiva

O projecto já tem uma base técnica significativa. Existe um Scanner funcional, um contrato de transporte IKP, recepção de pacotes, validação, recuperação e análise. Existem também contratos relevantes de identidade e proveniência, e trabalho de implementação para decisões humanas, Portal React e PHC Standard Catalog em branches.

Mas ainda não existe o ciclo completo que materializa o conhecimento de uma instalação na plataforma central e o torna pesquisável e reutilizável.

A lacuna central não é simplesmente «falta de interface» ou «falta de IA». É a ausência de uma pipeline completa, comprovável e recuperável que transforme uma captura em conhecimento persistente, contextualizado, comparável e pesquisável.

**Conclusão principal:** não é aconselhável recomeçar o projecto nem fazer uma grande refactorização da `main`. É necessário fechar primeiro os contratos de domínio, identidade, classificação de artefactos e materialização; depois validar e integrar o trabalho existente em incrementos pequenos.

## 3. Direcção de produto a preservar

A direcção de produto confirmada pelo utilizador e que deve prevalecer sobre documentos históricos é:

- Ferramenta interna da equipa da empresa que trabalha com PHC.
- Acesso reservado a colaboradores, com contas individuais.
- Instalação central num Windows Server da empresa, acessível pela rede interna ou VPN.
- A autenticação deve funcionar sem dependência de Internet quando o utilizador está na rede interna.
- Os clientes são contextos e fontes de conhecimento, não organizações/tenants da plataforma nem contas de utilizador.
- O Scanner é executado no ambiente do cliente; o operador transfere o ZIP manualmente para o ambiente do Portal e faz o upload.
- Não existe comunicação directa Scanner → Knowledge, nem o Scanner depende do backend central em runtime.
- A base de dados central é Microsoft SQL Server. PostgreSQL está excluído.
- A edição/versão concreta do SQL Server ainda não está escolhida; não assumir SQL Server Express como decisão.
- .NET 10 / ASP.NET Core no backend, React + TypeScript no Portal e WebView2 como opção de cliente Windows.
- SQLite é apenas workspace/cache local do Scanner.
- RAG é um objectivo futuro, não uma prioridade de implementação nesta fase.

O PHC Knowledge não é:
- um SaaS comercial multiempresa;
- um clone do ERP PHC;
- um Digital Twin;
- apenas um explorador SQL;
- um chatbot que responde sem fontes ou proveniência.

## 4. Modelo de conhecimento pretendido

A plataforma deve manter separadas pelo menos estas camadas:

1. **PHC Standard:** conhecimento geral proveniente de fontes identificadas, versionado e sujeito a curadoria.
2. **Observações de instalações:** factos recolhidos numa instalação concreta, com contexto, captura e proveniência.
3. **Personalizações:** objectos e definições específicos de uma instalação; não são partilhados automaticamente com outros clientes.
4. **Interpretações e projecções:** resultados derivados, versionados e acompanhados da evidência que os suporta; nunca substituem silenciosamente os factos originais.
5. **Conhecimento reutilizável:** conteúdo promovido por curadoria explícita, mantendo origem, âmbito e histórico.

Uma observação encontrada em vários clientes não se torna automaticamente PHC Standard. Uma personalização de um cliente não deve ser partilhada com outros clientes sem curadoria e autorização adequadas.

## 5. Fotografia técnica do repositório

### 5.1 Inventário observado

Na fotografia da `main` usada na auditoria foram identificados:
- 524 entradas na árvore Git;
- 76 documentos Markdown/texto;
- 18 projectos `.csproj`;
- duas soluções .NET;
- quatro workflows GitHub Actions;
- quatro migrations SQL, numeradas 001 a 004.

A estrutura contém:
- vários projectos do Scanner (WPF/CLI, collectors, workspace SQLite, exportação e testes);
- `PhcKnowledge.Host`;
- `PhcKnowledge.Import`;
- projectos e testes associados ao backend/importação.

Na `main` consultada ainda não existiam:
- o frontend React do Portal;
- o projecto `PhcKnowledge.StandardCatalog`;
- a materialização completa de Observation/REL/DTL.

Os números são uma fotografia da auditoria e não uma contagem dinâmica. Devem ser actualizados numa futura revisão do repositório.

### 5.2 Main e evidência de validação

A `main` é a referência publicada. O trabalho de Scanner Trigger Capability 2.0 foi integrado através do PR #21. As decisões de Fase 0 aprovadas pelo utilizador foram:
1. Capability 2.0;
2. chave de identificação;
3. fingerprints e comparação SQL.

Os workflows associados ao commit integrado `b8a1881` terminaram com sucesso, incluindo build/testes .NET e integração SQL Server 2022. Isto é evidência para aquele commit e aqueles testes; não prova que toda a plataforma esteja funcional ou validada em produção.

A implementação actual do collector de triggers recolhe triggers DML de forma abrangente, incluindo tabela associada, eventos, estado disabled, INSTEAD OF e disponibilidade da definição. A verificação da visibilidade do catálogo permite assinalar quando não é possível garantir a completude.

### 5.3 Recepção e análise IKP na main

O backend já possui:
- validação do ZIP e do formato IKP;
- registo de receipt/tentativa de recepção;
- armazenamento e recuperação do pacote;
- análise persistida em `PackageImportAnalyses`;
- operações de recuperação e limpeza de staging.

A análise não equivale a materialização. A existência de um receipt, de uma análise ou de uma decisão humana não significa que as observações e artefactos já estejam persistidos no modelo de conhecimento.

A análise de readiness também não deve ser interpretada como garantia de cobertura total. `Ready` significa que o pacote passou as regras de integridade/compatibilidade actualmente implementadas; não prova, por si só, que todas as operações esperadas foram recolhidas ou que o pacote está associado ao cliente/instalação correctos.

### 5.4 Lacunas do percurso operacional

O percurso aprovado é upload manual pelo Portal, com sessão autenticada. Na fotografia da `main`:
- o login usa ASP.NET Core Identity/cookies;
- a API IKP de upload está protegida por JWT Bearer;
- não foi identificado um endpoint de upload através da sessão cookie normal do Portal;
- por isso, o percurso manual Portal → upload ainda não está funcionalmente fechado.

Também precisam de validação de produção:
- persistência e protecção das chaves ASP.NET Data Protection;
- persistência e permissões das pastas de staging e arquivo;
- configuração de HTTPS, cookies e reverse proxy no Windows Server;
- estratégia de backup/restore coordenada entre SQL Server e armazenamento de ficheiros.

## 6. Scanner, conteúdo e classificação dos artefactos

A revisão dos collectors mostrou que nem tudo o que é exportado deve ser tratado como «código-fonte».

Exemplos observados:
- **FORMB:** recolhe registos de configuração de ecrãs e cria artefactos textuais com cabeçalho/atributos; não é necessariamente código.
- **FORMC:** recolhe registos com `udcVisible=.F.` e `Xexpressao`; guarda a expressão extraída/formatada, não necessariamente o registo `prop` completo nem toda a tabela `formc`.
- **BRULE:** o artefacto inclui stamp, tabela, mensagem, descrição, estado e expressão. Um fingerprint do artefacto completo pode mudar por alterações de metadados, mesmo quando a expressão/código é igual.
- **Campos físicos `u_*`:** a recolha via `sys.columns` inclui tipo, comprimento, precisão, escala, nullability e `column_id`; o artefacto criado é descritivo, não uma definição de código.
- **Outros collectors** como `escr`, `jsu`, `cssjs`, `ftu` e funções associadas também precisam de classificação explícita.

O exportador IKP atribui actualmente `source-definition` a artefactos referenciados como `DefinitionArtifactId`. A auditoria identificou o risco de essa classificação ser demasiado ampla: descrições de metadados e configurações podem acabar representadas como definições de origem.

### Trabalho necessário

Antes de fechar a materialização, criar uma matriz por collector/ObjectKind com:
- tabela/entidade de origem;
- conteúdo original efectivamente recolhido;
- conteúdo derivado ou formatado;
- campos de metadados;
- tipo semântico (código, configuração, descrição, diagnóstico, estrutura, relação);
- objecto ao qual pertence;
- âmbito de identidade;
- fingerprint aplicável;
- destino de persistência e estratégia de pesquisa.

A plataforma deve preservar o conteúdo original quando disponível e assinalar claramente quando só possui uma projecção ou expressão extraída.

## 7. Identidade, fingerprints e proveniência

A arquitectura P1/P1.B está conceptualmente bem encaminhada e não há razão identificada para a redesenhar de raiz.

Conceitos que devem permanecer separados:

| Conceito | Responsabilidade |
|---|---|
| `CaptureId` | Identifica uma recolha/captura |
| `PackageId` | Identifica o pacote IKP de transporte |
| `SourceObjectId` | Referência de um objecto dentro de uma captura |
| `SourceIdentity` | Evidência de identidade nativa PHC/SQL |
| `ObservationId` | Identifica uma observação histórica na plataforma |
| `EntityIdentity` | Identidade lógica persistente de uma entidade numa instalação |
| `DefinitionFingerprint` | SHA-256 do conteúdo original da definição |
| `MetadataFingerprint` | SHA-256 de metadados canónicos |

O Scanner deve transportar evidências. A plataforma deve decidir e registar a continuidade entre observações. Não se deve converter automaticamente um ID temporário, nome ou hash em identidade permanente.

O projector/resolver reconhece, entre outros:
- `object_id` SQL com âmbito da base de dados;
- `column_id` com âmbito da tabela-pai;
- stamps PHC com âmbito da tabela de origem.

Limites e riscos a preservar:
- `object_id` e `column_id` são identificadores internos à base de dados e ao contexto do objecto; não são IDs globais permanentes;
- podem mudar após recriação de objectos, migração ou outras operações;
- nomes e schema ajudam a interpretar, mas também podem mudar;
- stamps PHC são evidência forte dentro do âmbito conhecido, mas a estabilidade entre versões/famílias precisa de validação representativa;
- REL/DTL e objectos sem stamp não devem receber artificialmente uma identidade forte;
- ausência de cobertura, falha de recolha ou operação não aplicável não pode produzir falsos `Added`/`Removed`.

O contrato de linhagem indica que cada base de dados PHC registada tem o seu próprio `InstallationId` e histórico. Cópias/restauros podem ter relações de linhagem, mas não devem fundir automaticamente identidades ou históricos. Semelhança técnica não prova linhagem.

## 8. Cliente, instalação, ambiente e base de dados

Existe um ponto de domínio que tem de ser reconciliado antes de fechar o esquema definitivo.

O modelo conceptual 0.2 representa uma hierarquia semelhante a:

`Organization → Installation → Environment → DatabaseLocation`

O contrato de linhagem, por outro lado, afirma que cada base de dados PHC registada tem o seu próprio `InstallationId`.

A direcção de produto confirma que:
- a organização é a empresa que utiliza internamente a plataforma;
- clientes são contextos de conhecimento, não organizações/tenants;
- as contas são de colaboradores.

Proposta conceptual para discussão (não aprovada como modelo físico):
- **Organização:** única empresa utilizadora, podendo existir como raiz técnica de segurança no código;
- **Cliente:** entidade de contexto que agrupa conhecimento proveniente desse cliente;
- **Instalação:** identidade técnica de uma base de dados PHC, se mantivermos o contrato de linhagem;
- **Ambiente/localização:** atributos/contexto (produção, testes, servidor, base de dados, localização e alterações ao longo do tempo);
- **Linhagem:** registo explícito de cópia/restauro/derivação, sem fusão automática de históricos.

É necessário decidir se produção e uma cópia de testes são instalações independentes ou ambientes de uma instalação, e quais as consequências para identidade e comparação. Não usar `Organization` para representar clientes.

## 9. REL/DTL e decisão D-015

REL/DTL contêm factos declarados pelo PHC, expressões, referências e interpretações. Não são equivalentes a foreign keys físicas inferidas do SQL Server.

A auditoria da captura real de 16/11 reportou 921 relações declaradas (295 REL e 626 DTL), 921 interpretações com estado Parsed e zero interpretações órfãs. Foi também reportado que o ficheiro `declared-relations.json` tinha cerca de 1,94 MB sem compressão. Estes valores são notas da análise dessa captura e devem ser reconfirmados directamente nos artefactos antes de servirem como métricas de baseline.

### D-015 — transferir REL/DTL standard para o PHC Standard Catalog

**Estado: proposta pendente de aprovação do utilizador.**

A proposta tem lógica: as relações PHC standard podem ser mantidas uma vez num catálogo comum, versionado por versão/edição PHC, em vez de serem repetidas integralmente em cada captura.

Mas não devemos simplesmente retirar a recolha do Scanner antes de existir comparação fiável entre a referência standard aplicável e a instalação.

É necessário definir:
- como identificar versão/edição PHC aplicável;
- como comparar expressão original e metadados;
- como detectar relações standard inalteradas;
- como representar relações alteradas;
- como representar relações acrescentadas/localizadas;
- como preservar evidência da instalação;
- como lidar com relações sem correspondência ou com fontes standard incompletas.

A separação correcta parece ser catálogo de referência + observações de diferenças, mantendo factos e proveniência. Esta é uma recomendação de desenho, não a aprovação de D-015.

## 10. Captura completa e decisão D-016

**D-016 — Scanner enviar captura completa e Knowledge materializar incrementalmente: proposta pendente de aprovação do utilizador.**

É necessário separar três conceitos:
1. recolher uma captura;
2. enviar/exportar os dados dessa captura;
3. materializar/persistir esses dados no Knowledge.

Uma possibilidade é o Scanner produzir um pacote completo e autónomo, enquanto o Knowledge:
- preserva a proveniência de cada captura;
- compara com o histórico da mesma instalação;
- evita duplicar bytes/versões de conteúdo rigorosamente igual;
- regista alterações e novas versões;
- não confunde falta de cobertura com remoção.

Nesta interpretação, «incremental» descreve a persistência e a comparação no Knowledge, não necessariamente o envio de apenas deltas pelo Scanner.

Há, contudo, impacto em tamanho do ZIP, tempo de recolha, transferência, armazenamento e processamento. A proposta deve ser avaliada com dados reais e em conjunto com D-015, a matriz de artefactos e o contrato de materialização.

**Não registar D-015 nem D-016 como aprovadas.** Alguns documentos na branch da PR #20 classificam-nas como aprovadas; essa classificação não reflecte a indicação do utilizador e deve ser corrigida antes de qualquer consolidação documental.

## 11. PHC Standard Catalog e triggers

A Fase 0 do contrato de triggers foi aprovada e o Scanner Trigger Capability 2.0 foi integrado na `main`.

Na branch de trabalho da PR #20 foi encontrado um extractor da fonte `docs/sources/triggers phc.sql`:
- 552 triggers;
- 258 tabelas;
- definições de origem com `WITH ENCRYPTION`;
- extracção de batches, eventos, tabela associada e opções `ANSI_NULLS`/`QUOTED_IDENTIFIER`;
- SHA-256 exacto do texto original.

Este trabalho não equivale ainda a um catálogo persistente e operacional. Faltam:
- persistência e versionamento;
- aplicabilidade por versão/edição PHC;
- curadoria e proveniência;
- comparação com observações das instalações;
- consulta no Portal;
- política para triggers instaladas encriptadas.

Quando uma definição instalada está encriptada, pode ser possível comparar identidade e metadados, mas não confirmar igualdade do código. O resultado deve ser inconclusivo quanto à definição, não uma falsa confirmação de igualdade.

O ficheiro SQL de origem só deve ser eliminado do GitHub depois de o conteúdo estar importado no SQL Server do Portal e validado, conforme a intenção já expressa pelo utilizador.

## 12. Materialização e ciclo de vida do ZIP

A direcção funcional está definida: o ZIP IKP é um meio temporário de transporte, não o arquivo permanente do conhecimento.

Fluxo-alvo:
1. upload manual no Portal;
2. validação e registo de receipt;
3. análise e apresentação de evidências/contexto;
4. decisão humana necessária;
5. materialização idempotente de observações, metadados, relações e artefactos;
6. verificação de integridade, hashes, referências, contagens e proveniência;
7. eliminação segura do ZIP e temporários, com registo da conclusão.

A implementação actual usa um armazenamento de ficheiros content-addressed por SHA-256, com verificação de integridade e recuperação. O ZIP final fica arquivado permanentemente; staging temporário tem limpeza.

Isto diverge do objectivo de ZIP temporário, mas não se deve apagar o arquivo actual antes de existir uma alternativa de recuperação e de o processo de materialização estar completo e verificado.

Manter separados:
- **ZIP:** transporte temporário;
- **artefactos originais:** conteúdo que precisa de continuar recuperável e pesquisável;
- **dados estruturados:** objectos, metadados, relações, fingerprints, observações e histórico.

A proposta `varbinary(max)` para bytes de artefactos no SQL Server é tecnicamente possível, mas **não está aprovada pelo utilizador**. Deve comparar-se com armazenamento de ficheiros no Windows Server, com referências e integridade registadas no SQL Server. A decisão depende de volume, crescimento, backup/restore, desempenho, edição SQL Server e operação.

Também não está aprovada a retenção de 30 dias para pacotes rejeitados. A auditoria encontrou classificações contraditórias dessa retenção e de `varbinary(max)` em documentos da branch C.

Estados a distinguir:
- receipt recebido;
- análise persistida;
- decisão humana registada;
- materialização executada;
- materialização verificada;
- ZIP elegível para eliminação;
- eliminação confirmada.

Não equiparar `Completed` no receipt, `Ready` na análise ou decisão confirmada a materialização verificada.

## 13. Portal e experiência operacional

Na branch da PR #20 existe uma base React/TypeScript com login e página de decisão. Contudo, a experiência ainda não fecha o processo completo.

Lacunas observadas:
- upload manual de ZIP pelo Portal ainda não está integrado;
- o ecrã de decisão apresenta estado/histórico, mas não mostra suficientemente análise, evidências, avisos, objectos e artefactos para uma decisão informada;
- a aplicação Host não serve ainda o build React nem fallback SPA na fotografia consultada;
- a página inicial continua a apresentar estados anteriores (Incrementos A/B).

O Portal deve permitir ao operador compreender:
- que pacote carregou;
- a que cliente/instalação/ambiente o associou;
- se a análise é completa ou parcial;
- que objectos e artefactos foram encontrados;
- que avisos e limitações existem;
- que decisão está a tomar e com que evidência;
- o que foi efectivamente materializado;
- se o pacote já pode ser eliminado.

A decisão humana não deve ser um botão de confirmação sem contexto.

## 14. Pesquisa e RAG

A primeira grande capacidade de valor para a equipa será pesquisa técnica determinística. Exemplo: encontrar todas as utilizações de `u_peso`, perceber em que objectos aparecem e em que clientes/instalações foram observadas.

Antes de RAG, é necessário:
- persistir conteúdo original e metadados com classificação correcta;
- indexar o texto integral dos artefactos;
- relacionar resultados com objecto, cliente, instalação, captura e origem;
- suportar pesquisa exacta, por fragmento, por tipo e por contexto;
- distinguir conteúdo standard, observação específica, interpretação e conhecimento curado;
- apresentar sempre a fonte/proveniência.

A pesquisa deve começar pelas capacidades integradas no SQL Server que forem adequadas à edição escolhida. Não introduzir já motor externo, vector store, embeddings ou fornecedor de IA.

RAG é uma fase futura de consumo do conhecimento estruturado. Não deve ser usado para compensar ausência de dados, indexação ou proveniência.

## 15. Branches e pull requests

Fotografia comparativa observada durante a auditoria:

| Linha | Relação com main | Observação |
|---|---:|---|
| `feature/increment-c-backend` | 19 commits ahead, 0 behind | Linha mais delimitada para backend C; precisa de validação integral |
| `feature/increment-c-package-import-analysis-confirmation` / PR #20 | 97 commits ahead, 11 behind | Alterações muito abrangentes e misturadas |
| `feature/portal-frontend-foundation` / trabalho de Portal | Separada da main | Deve ser reconciliada com backend e experiência operacional |

A PR #20 apresentava cerca de 118 ficheiros alterados na comparação consultada e reunia:
- governação e baseline;
- backend de decisões;
- migration 005;
- UI React;
- StandardCatalog;
- contratos e propostas de materialização;
- políticas de ciclo de vida ZIP;
- documentação de estado.

A branch está atrás da `main` e inclui trabalho posterior a commits que já foram integrados, incluindo Trigger Capability 2.0. Não deve ser integrada em bloco nem tratada como uma simples continuação linear.

A recomendação é validar e integrar por incrementos independentes:
1. backend C;
2. frontend/Portal;
3. StandardCatalog;
4. governação documental;
5. materialização, quando o contrato estiver fechado.

A existência de testes no código não prova que os testes foram executados com sucesso. A integração de cada incremento deve ter evidência de build, testes relevantes e compatibilidade com a `main` actualizada.

## 16. Estado consolidado por área

| Área | Estado observado | Leitura |
|---|---|---|
| Scanner | Implementado | Base funcional; continuar validação de cobertura e semântica dos artefactos |
| IKP 0.1 | Implementado | Contrato e exportação existentes |
| Identidade/fingerprints | Implementado parcialmente | Bom desenho; validação de estabilidade e famílias PHC incompleta |
| Recepção/recuperação | Implementado | Existe na main; validar operação de produção |
| Análise IKP A/B | Integrada | Análise persistida; não é materialização |
| Decisão humana C | Branch | Código existe fora da main; validar |
| Portal React | Branch | Fundação existente; percurso operacional incompleto |
| PHC Standard Catalog | Parcial/branch | Extractor de triggers; catálogo ainda não operacional |
| Cliente/instalação/ambiente | Por reconciliar | Modelo conceptual e contrato de linhagem precisam de alinhamento |
| Materialização | Por construir | Principal lacuna funcional |
| Pesquisa de conhecimento | Por construir | Depende da persistência/classificação/indexação |
| RAG | Futuro | Não antecipar |

## 17. Documentação e governação

A `main` tinha 76 documentos Markdown/texto na fotografia da auditoria. A branch da PR #20 tinha um inventário documental que reportava 86 ficheiros.

A proposta de governação nessa branch tem elementos positivos:
- distingue documentos canónicos, contratos, ADRs, relatórios, investigações, fontes e históricos;
- preserva documentos antigos em vez de os reescrever retroactivamente;
- recomenda que implementação e validação sejam confirmadas contra código e CI;
- propõe baseline, decisões, estado e handoff como documentos de entrada.

Mas não deve ser integrada sem revisão:
- D-015 e D-016 aparecem classificados como aprovados, contra a indicação do utilizador;
- algumas opções de armazenamento/retenção são apresentadas como decisões sem aprovação confirmada;
- a baseline nessa branch contém formulações que não devem ser promovidas sem reconciliar as decisões.

### Documentos prioritários a reconciliar na main

| Documento | Problema identificado |
|---|---|
| `PROJECT_GUIDELINES.md` | Referências antigas a multi-organização e SQL Server Express |
| `README.md` | Fotografia anterior à existência do frontend React em branch |
| `docs/architecture/product-boundaries.md` | Modelo organizacional antigo |
| `docs/architecture/knowledge-domain-model-0.2.md` | Conceitos e relações de domínio a reconciliar |
| `docs/architecture/roadmap-visual-phc-knowledge-2026-11-06.md` | Roadmap histórico que não inclui todo o trabalho posterior |
| `PhcKnowledgeScanner/docs/PROJECT-STATUS.md` | Fotografia de 10/11, não estado actual |
| `docs/architecture/standard-catalog-integration-specification.md` | Partes ainda descrevem Fase 0 como proposta, apesar das decisões de triggers aprovadas |

Regra de documentação:
- preservar históricos;
- não reescrever relatórios antigos como se descrevessem o presente;
- acrescentar notas de precedência quando necessário;
- consolidar baseline/decisões/estado/handoff no fim de cada fase;
- classificar sempre cada afirmação como decisão, proposta, implementação ou validação.

## 18. Decisões e questões pendentes

Esta tabela é o registo de trabalho desta auditoria. Não transforma as propostas em decisões.

| Tema | Estado nesta auditoria | Observação |
|---|---|---|
| Plataforma interna, não SaaS | Direcção confirmada | Só colaboradores; clientes são contexto |
| SQL Server central | Direcção confirmada | Edição/versão por escolher |
| PostgreSQL | Excluído | Não reintroduzir |
| Scanner autónomo + upload manual | Direcção confirmada | Sem ligação directa ao Knowledge |
| ZIP temporário | Objectivo confirmado | Implementação actual ainda arquiva permanentemente |
| Trigger Capability 2.0 | Aprovado e integrado | Fase 0: capability, chave de identificação, fingerprints/comparação SQL |
| D-015 — REL/DTL no catálogo Standard | Pendente de aprovação | Avaliar referência standard e detecção de desvios |
| D-016 — captura completa + materialização incremental | Pendente de aprovação | Avaliar recolha, transporte e persistência separadamente |
| `varbinary(max)` para artefactos | Pendente | Comparar com file store no Windows Server |
| Retenção de 30 dias para pacotes rejeitados | Pendente | Não está confirmada |
| Semântica Cliente/Instalação/Ambiente | Pendente de reconciliação | Alinhar com contrato de linhagem |
| Edição/versão SQL Server | Pendente | Avaliar capacidade, operação e custos |
| Política de remoção/retensão para ZIP rejeitado | Pendente | Definir segurança, auditoria e recuperação |
| Regras de confirmação de remoções | Pendente de desenho | Não inferir remoção com cobertura parcial |

## 19. Roadmap recomendado

A ordem recomendada é deliberada: primeiro decisões e contratos; depois experiência operacional e persistência; só depois pesquisa avançada e IA.

### Fase 1 — Alinhamento estratégico e contratos

- Consolidar a baseline de produto interno.
- Fechar Cliente/Instalação/Ambiente/Base de dados e linhagem.
- Avaliar e decidir D-015 e D-016.
- Fechar a matriz semântica de artefactos por collector.
- Decidir a estratégia de armazenamento de artefactos e política de ZIP.
- Escolher a edição/versão de SQL Server com base em requisitos reais.
- Corrigir a classificação de decisões nos documentos da branch C.

**Critério de saída:** contratos coerentes, decisões explicitamente aprovadas e nenhuma proposta apresentada como decisão.

### Fase 2 — Fechar o percurso operacional do Portal

- Validar backend de decisões C isoladamente.
- Integrar autenticação do Portal com upload manual por sessão.
- Apresentar análise, cobertura, evidências, avisos e artefactos.
- Permitir decisão humana fundamentada e auditável.
- Servir o build React a partir do Host ou definir formalmente o hosting.
- Validar configuração de produção no Windows Server.

**Critério de saída:** operador consegue carregar, compreender e decidir sobre um pacote através do Portal.

### Fase 3 — PHC Standard Catalog

- Fechar extractor/parser e testes.
- Importar triggers standard com fonte, versão e proveniência.
- Definir aplicabilidade PHC.
- Tratar definições encriptadas como não comparáveis ao nível do código.
- Disponibilizar consulta no Portal.
- Só depois de validar a importação, retirar a fonte SQL do repositório se essa continuar a ser a decisão operacional.

### Fase 4 — Materialização V1

- Definir transacções e idempotência.
- Persistir observações, identidades, metadados, relações e artefactos.
- Preservar conteúdo original e fingerprints.
- Registar cobertura e limitações.
- Implementar verificação de integridade e recuperação.
- Implementar estados de materialização e eliminação segura do ZIP.

**Critério de saída:** uma captura pode ser materializada, verificada, recuperada e auditada sem depender do ZIP permanente.

### Fase 5 — Contexto e comparação

- Associar captura a cliente/instalação/ambiente.
- Comparar apenas dentro do âmbito correcto.
- Distinguir alterações de definição e metadados.
- Tratar cobertura parcial e falhas sem falsos Added/Removed.
- Registar linhagem sem fundir instalações.

### Fase 6 — Pesquisa técnica

- Pesquisa integral de código e conteúdo.
- Pesquisa por nome, tabela, campo, expressão, tipo e cliente.
- Navegação de dependências e relações.
- Resultados com evidência, captura e proveniência.
- Pesquisa standard separada de observações específicas.

### Fase 7 — Conhecimento curado e RAG

- Promover soluções e explicações por curadoria.
- Ligar soluções a versões, módulos, objectos e casos.
- Introduzir RAG apenas quando a base estruturada tiver qualidade e cobertura suficientes.
- Respostas sempre acompanhadas de fontes e âmbito.

## 20. Recomendação final

Não recomeçar o projecto. Não fazer uma refactorização global da `main`. Não integrar a PR #20 em bloco. Não começar por RAG.

O investimento prioritário deve ser fechar as fronteiras e contratos que sustentam a materialização:
- modelo Cliente/Instalação/Ambiente;
- D-015 e D-016;
- semântica e proveniência dos artefactos;
- estratégia de armazenamento e ciclo de vida do ZIP;
- edição SQL Server.

Depois, validar o backend C, o Portal e o StandardCatalog em incrementos pequenos e integrá-los com evidência.

O resultado pretendido é uma plataforma onde a equipa consegue consultar o conhecimento standard PHC, perceber o que foi encontrado em cada instalação, comparar alterações, pesquisar código e configurações e reutilizar soluções com proveniência. A IA deve aumentar o valor desse conhecimento, não substituir a sua estrutura.

## 21. Protocolo para a continuação do trabalho

1. Usar este relatório como contexto de auditoria, não como baseline final aprovada.
2. Manter a `main` como referência publicada.
3. Não integrar branches nem alterar decisões sem autorização explícita.
4. Não declarar testes/builds validados sem resultados verificáveis.
5. Não pedir nova captura nesta fase sem demonstrar primeiro por que razão a captura existente não chega.
6. Não apagar documentos históricos nem o ZIP arquivado antes de existir decisão e mecanismo de substituição seguros.
7. Tratar D-015, D-016, `varbinary(max)`, retenção de 30 dias e edição SQL Server como pendentes.
8. Avançar uma decisão de cada vez, explicando recomendação, alternativas e consequências.
9. No fim do alinhamento, consolidar a baseline, decisões, estado, roadmap e handoff canónicos.
10. Só depois iniciar a próxima fase de implementação.

---

**Fim do relatório de auditoria.**

Este documento preserva o diagnóstico e as recomendações produzidas em 21-11-2026. Qualquer evolução posterior deve acrescentar evidência e actualizar o estado, sem apagar o contexto histórico aqui registado.
