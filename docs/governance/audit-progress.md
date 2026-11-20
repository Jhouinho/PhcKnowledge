# PHC Knowledge — Progresso da auditoria documental

**Estado:** em curso; não é uma baseline aprovada nem uma auditoria concluída.  
**Última atualização:** 2026-11-20  
**Repositório:** `Jhouinho/PhcKnowledge`  
**Branch de trabalho:** `feature/increment-c-package-import-analysis-confirmation`  
**Commit de referência antes da atualização documental:** `9a9df92ff82a98eafa2a5c1ff5134d8226ea7ce5`

## Objetivo e limites

Verificar a coerência entre decisões de produto/arquitetura, documentação, código e testes; identificar documentos vigentes, desatualizados, históricos, redundantes ou que devam ser preservados como contratos/fixtures.

Durante esta auditoria:
- não alterar implementação;
- não apagar nem reescrever documentação técnica;
- não fazer merge nem mudar de branch;
- não declarar implementado ou validado sem evidência;
- apresentar o inventário e a proposta de tratamento antes de qualquer limpeza documental.

## Contexto de produto confirmado pelo utilizador

- A plataforma é uma ferramenta de uso interno da equipa, para gerir e reutilizar conhecimento técnico dos clientes acompanhados; não é um produto comercial multiempresa.
- O Scanner corre no ambiente do cliente e produz um ZIP que é transportado para o ambiente interno.
- O ZIP é meio de transporte. O objetivo aprovado é materializar e persistir no SQL Server a informação necessária e eliminar o ZIP/temporários após confirmação de importação íntegra; a implementação atual ainda tem de ser reconciliada com esse objetivo.
- A base de dados central escolhida é SQL Server.
- O Scanner está no monorepo, mas mantém responsabilidade e execução autónomas; não deve ser introduzida dependência de runtime entre Scanner e Knowledge.
- RAG é um objetivo futuro; não selecionar tecnologia nem antecipar implementação nesta fase.
- Preservar proveniência por cliente, instalação, ambiente/base de dados, captura e artefacto, sem colapsar estes conceitos.
- O utilizador pretende pesquisa e reutilização dos artefactos/código recolhidos, com contexto de origem, para compreender customizações existentes e planear implementações.
- As regras de classificação Desktop/Web acordadas incluem Fox/Visual FoxPro e SQL puro como Desktop; C# como Web; HTML/JS/CSS isolados como Web; HTML/JS/CSS encapsulados em Fox como Desktop. A classificação deve ser baseada em evidência e não inferida para a instalação inteira a partir de um único artefacto. A implementação desta classificação ainda deve ser verificada.

## Constatações técnicas já registadas durante a auditoria

1. A branch consultada anteriormente era `feature/increment-c-package-import-analysis-confirmation`, com HEAD `170631ea8e17b584ac70cd6b379312570c0c948a`. O conteúdo do handoff anterior referia `e2d896ff67f7ef0df903c8a13940da9b826a764d`; essa referência não coincide com o HEAD confirmado pela API da branch e deve ser tratada como desatualizada.
2. A solução contém áreas separadas para Host, Import e Portal, migrations e testes. A existência dos testes não significa que tenham sido executados nesta auditoria.
3. Foram identificadas migrations para receipts, tentativas, organizações, análises de importação e decisões de análise.
4. O fluxo de análise consegue reabrir o pacote através de `OpenArchivedPackageReadAsync`; existe arquivo permanente no fluxo atual. O cleanup observado trata staging antigo não protegido por tentativas pendentes. Isto diverge do objetivo de ZIP transitório e requer análise antes de qualquer alteração.
5. A análise do pacote é persistida em JSON e existem decisões de confirmação/rejeição. Até ao ponto inspecionado, não foi identificada uma camada equivalente que materialize o conteúdo integral dos artefactos numa base de conhecimento pesquisável.
6. O exportador IKP inclui artefactos de definição referenciados pelos objetos da captura, valida metadata, bytes, tamanho, SHA-256 e ContentType. Não se deve assumir que todos os ficheiros do workspace são exportados.
7. A pesquisa `TableSearch` observada é centrada em tabelas e critérios como nome/schema/ObjectId; não equivale a pesquisa global sobre o conteúdo dos artefactos de código.
8. Não foi ainda encontrada evidência suficiente de um classificador Desktop/Web que implemente as regras acordadas.
9. Os documentos P1/P1B contêm contratos de identidade, proveniência e fingerprints relevantes. Em particular, SourceObjectId é referência dentro da captura, não identidade permanente; DefinitionFingerprint e MetadataFingerprint são distintos; ausência de um artefacto numa captura não prova remoção.
10. A matriz de compatibilidade Scanner/IKP documenta validação com captura real para uma parte dos tipos mapeados, não para todos.
11. Foram identificados documentos do Scanner que afirmam que a integração/importação no Knowledge ainda não existe. Devem ser avaliados como registos históricos face ao estado mais recente, sem os apagar nesta fase.
12. Existem ADRs do Scanner numerados 0001–0014. A orientação preliminar é preservá-los como histórico de decisões do Scanner e reconciliar decisões superadas com evidência atual.

Estas constatações são um registo intermédio. A auditoria ainda não concluiu a inspeção de todos os collectors, mapeamentos IKP, testes e documentos.

## Classificação preliminar — não aprovada como inventário final

- Preservar: especificação IKP, schemas, fixtures/validações, contratos P1/P1B e ADRs, salvo prova de redundância ou substituição explícita.
- Atualizar após auditoria: handoff, estado do projeto, limites do produto, avaliação tecnológica/importação, decisões em aberto e especificação da experiência do Portal.
- Marcar como histórico quando descrevem propostas ou fases já ultrapassadas: arquitetura/propostas antigas do Scanner e handoffs de fases concluídas.
- Rever profundamente antes de tratar como modelo implementado: `knowledge-domain-model-0.2.md`.
- Não remover documentos antes do inventário final e validação do utilizador.

## Trabalho ainda por concluir

1. Inspecionar collectors, artefactos produzidos e mapeamentos para IKP; distinguir recolha, indexação, referência no pacote e exportação efetiva.
2. Cruzar os tipos acordados — brule/ebrule, eventos/eeventos, udefs/eudefs, ti/tc, campos físicos u_*, escr, jsu, cssjs, formc, formb, ftu, Fx_*/User_*, rel/dtl — com código e testes, sem presumir cobertura.
3. Inspecionar testes de receipts, recovery, análise, decisões, endpoints HTTP e integração SQL Server.
4. Registar separadamente testes existentes, testes executados e resultados documentados. O teste SQL Server depende de configuração de ambiente; não declarar execução sem confirmação.
5. Ler e reconciliar os relatórios de fase, documentos de ingestão/recovery, documentação do Portal, roadmap, investigação de cobertura, RAG, P1/P1B, protótipos, README e regras de projeto.
6. Produzir inventário completo dos 79 ficheiros documentais identificados, com classificação, justificação e destino/referência substituta.
7. Apresentar a proposta ao utilizador e aguardar aprovação antes de alterar ou remover documentos.

## Próximo passo

Retomar a inspeção técnica dos collectors e do mapeamento IKP. Não retomar D1/D2 nem iniciar a consolidação da baseline até a auditoria estar concluída e o inventário ser validado.


## Atualização de governação — 2026-11-20

O responsável autorizou a atualização documental nesta branch. Foram criadas fontes canónicas PROJECT_BASELINE.md, DECISIONS.md, PROJECT_STATUS.md e docs/governance/README.md; PROJECT_GUIDELINES, README, regras Cursor e handoff foram alinhados.

Correções consolidadas:
- SQL Server é a base central; PostgreSQL excluído; edição/versão SQL Server não fixada.
- SQLite limitado ao workspace/cache local do Scanner.
- Ferramenta interna da equipa, não produto comercial multiempresa.
- ZIP IKP é transporte temporário; arquivo permanente no código é divergência por resolver.
- PHC Standard curado separado de observações/personalizações dos clientes.
- React Portal já existe; documentos que o descrevem como inexistente são históricos.
- StandardCatalog contém extração/parsing de triggers; pipeline completa de catálogo ainda não demonstrada.

A auditoria permanece em curso. Esta publicação não executou testes nem alterou código. O inventário individual e qualquer decisão de eliminação/fusão documental continuam pendentes.


## Etapa 2 — inventário individual e reconciliação de estado (2026-11-20)

- HEAD de entrada: `6fdb4060a8fdb2de8aec9080620b91b4c9fa89f5`.
- Inventário inicial: 83 ficheiros Markdown/texto; após criar o próprio inventário, total: 84.
- Criado `docs/governance/document-inventory.md`, com classe e tratamento recomendado para cada ficheiro.
- Atualizados avisos de precedência/estado em Scanner PROJECT-STATUS, open-decisions, product-boundaries, P2.B, receipt recovery/host, Incremento C, RAG e roadmap.
- Corrigido `PhcKnowledgeScanner/samples/README.md` para refletir a estrutura realmente versionada.
- Confirmado no código da branch que existem endpoints/serviços/Portal/testes de decisão do Incremento C; a existência dos ficheiros prova implementação parcial, não conclusão funcional total.
- Sem alterações de código, sem builds/testes e sem eliminação de documentos.
- Commit desta etapa: `070e456b06f7054c0764a3f4cac6532150381743`.
