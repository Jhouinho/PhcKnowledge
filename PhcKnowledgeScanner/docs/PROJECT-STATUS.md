# Estado do projeto e próximos passos

**Última atualização:** 2026-11-23  
**Repositório:** [Jhouinho/PhcKnowledge](https://github.com/Jhouinho/PhcKnowledge) — monorepo canónico; Scanner em `PhcKnowledgeScanner/`  
**Branch de referência:** `main`  
**Nota de referência:** o histórico de commits e ADRs do Scanner mantém-se no monorepo; esta revisão documental não declara uma nova execução de build/testes.

Este documento é o ponto único de situação do projeto. Deve ser atualizado no final de cada ciclo de trabalho, distinguindo sempre o que está implementado, o que foi validado, o que está em curso e o que é apenas uma ideia. Não substitui ADRs nem handoffs técnicos específicos.

---

## 1. Objetivo e princípios

O PhcKnowledgeScanner é uma aplicação **independente** para recolha e exploração técnica de instalações PHC (CS Desktop / Web / XL / Manufactor).

Objetivos principais:

- Recolher estrutura SQL e personalizações PHC sem alterar a instalação.
- Preservar os conteúdos originais recolhidos e a respetiva proveniência.
- Registar objetos não recolhidos, limitações e erros de forma explícita.
- Guardar a recolha num workspace local SQLite e em artefactos.
- Permitir exportação ZIP e exploração posterior de workspaces existentes.
- Separar factos recolhidos, interpretações determinísticas e inferências.

Princípios que se mantêm:

- SQL de instalação em modo read-only.
- Nunca alterar a base de dados PHC.
- Não normalizar nem reescrever destrutivamente o código original.
- Não truncar silenciosamente conteúdos.
- Não confundir este componente com `Jhouinho/ConsoleApp` (exploração histórica / Scanner V1). O produto PHC Knowledge actual está no mesmo repositório `Jhouinho/PhcKnowledge`; o Scanner está integrado no monorepo em `PhcKnowledgeScanner/`, mantendo autonomia funcional e executável.
- Aplicação desktop nativa WPF; não introduzir uma interface web, HTTP ou WebView.

## 2. Estado atual — resumo

A aplicação Scanner evoluiu para desktop WPF + CLI, com recolha SQL/PHC, workspace SQLite, análise relacional e exportação IKP 0.1. O código é autónomo, mas o repositório canónico é agora o monorepo `Jhouinho/PhcKnowledge`.

| Área | Estado atual |
|---|---|
| Solução e projetos .NET | Implementados; solução atualmente em .NET 10 |
| Aplicação desktop WPF | Implementada |
| Host CLI | Mantido para operações headless |
| Teste de ligação SQL | Implementado |
| Recolha estrutural SQL | Implementada |
| Recolha de personalizações PHC | Implementada para as famílias já identificadas no collector |
| Workspace SQLite | Implementado |
| Exportação ZIP legado | Implementada; mantém compatibilidade com fluxos existentes |
| Exportação IKP 0.1 | Implementada em `PhcKnowledgeScanner.Export`; CLI `scan-ikp` e `export-ikp`; testes dedicados |
| Inventário de tabelas e chaves | Implementado |
| Recolha e interpretação de relações declaradas REL/DTL | Implementada, com parser determinístico V1.1 |
| Extração de FieldMappings | Implementada |
| Abertura de workspace existente em sessão read-only | Implementada |
| Exploração desktop de tabelas, relações e FieldMappings | Implementada |
| Grafo visual de dependências | Por implementar |
| Explicação funcional do código PHC por blocos | Ideia planeada; não implementada como funcionalidade |
| Comentários explicativos gerados por IA | Ideia futura; não implementada |

## 3. Trabalho concluído

### 3.1 Fundação e recolha desktop

- Criada a solução independente e os projetos por responsabilidade.
- Evolução para stack .NET 10 com aplicação WPF.
- Implementados teste de ligação, recolha estrutural SQL e recolha de personalizações PHC.
- Criado workspace SQLite para persistência local dos resultados.
- Implementada exportação ZIP alinhada com a estrutura de personalizações exportada.
- Mantido o acesso SQL em leitura.

Commits de referência:
- [Fundação inicial — 2026-10-30](https://github.com/Jhouinho/PhcKnowledgeScanner/commit/510a15bab99c5e81631e0f4977b92842cb33644a)
- [Aplicação desktop, collectors e exportação — 2026-11-03](https://github.com/Jhouinho/PhcKnowledgeScanner/commit/0efe81900efe8709bcc7fc2f7d22d3a5d03deb63)

### 3.2 Inventário relacional

Implementado:

- Inventário de tabelas, chaves primárias e chaves estrangeiras SQL.
- Recolha de relações declaradas em `REL`/`DTL`.
- Separação entre o facto original recolhido e a sua interpretação.
- Parser determinístico de expressões, com preservação da estrutura lógica `AND`/`OR` e parênteses.
- Evolução do parser para V1.1, incluindo literais PHC, operador `+`, chamadas de funções e recuperação parcial.
- Extração de FieldMappings e respetiva proveniência.

A interpretação não executa as expressões SQL/PHC. A existência física de colunas e a validação semântica completa não são assumidas quando não estão comprovadas.

Commits e decisões:
- [Camada de conhecimento relacional — 2026-11-03](https://github.com/Jhouinho/PhcKnowledgeScanner/commit/d26ea2645a00a90e8a0d033edea14b3647a8837a)
- [ADR 0005 — interpretação V1](decisions/0005-declared-expression-interpretation-v1.md)
- [ADR 0006 — interpretação V1.1](decisions/0006-declared-expression-interpretation-v1.1.md)
- [ADR 0007 — inventário PK/FK](decisions/0007-full-pk-fk-inventory.md)
- [ADR 0008 — projeção relacional](decisions/0008-relational-knowledge-projection-v1.md)
- [ADR 0009 — FieldMappings](decisions/0009-field-mapping-extraction-v1.md)

### 3.3 Exploração no Desktop

Implementado na página **Inventário**:

- Abertura de uma pasta de workspace ou de `workspace.db` existente.
- Sessão de exploração em memória, carregada a partir de SQLite read-only.
- Pesquisa de tabelas com prioridade a correspondências exatas e por prefixo.
- Relações de saída e de entrada de uma tabela.
- Detalhe da relação com evidências e proveniência REL/DTL/FK.
- FieldMappings no detalhe da relação.
- Reutilização da mesma projeção durante a sessão, sem voltar a projetar a cada navegação.
- Abertura de um novo workspace sem perder a sessão anterior se a abertura falhar.

A página Inventário é distinta do Explorer de personalizações `u_*`.

Referências:
- [Implementação Desktop — 2026-11-03](https://github.com/Jhouinho/PhcKnowledgeScanner/commit/3e8bea674ed1b8505127d37b68f87f0e3ae5740d)
- [Handoff inventário](handoff-1d.2.1-desktop-inventory.md)
- [Handoff relações](handoff-1d.2.2-desktop-relations.md)
- [Handoff FieldMappings](handoff-1d.2.3-field-mappings.md)
- [ADR 0011 — sessão de workspace](decisions/0011-desktop-inventory-workspace-session.md)
- [ADR 0012 — exploração de relações](decisions/0012-desktop-inventory-relation-exploration.md)
- [ADR 0013 — exploração de FieldMappings](decisions/0013-desktop-field-mapping-exploration.md)

## 4. Validações conhecidas

Os seguintes resultados estão registados nos handoffs e referem-se a uma cópia de workspace de validação, não a todas as instalações PHC:

| Verificação | Resultado registado |
|---|---:|
| ScanId | `9ece6cad736140de8f370987f3e83c2c` |
| Tabelas no inventário | 8 229 |
| KnowledgeRelations | 464 |
| FieldMappings | 772 |
| Relações FD → PE | 4 mapeamentos DirectFieldEquality (2 REL + 2 DTL) |
| Relações de saída / entrada de BO | 7 / 11 |
| Relações de saída / entrada de CL | 1 / 17 |
| ProjectionBuildCount após navegação | 1 |
| SHA-256 antes/depois da exploração | Igual: `80A2BF010AC1FA6C534C2B41B47C184BFDD896F658F1442D30C3DF9097AF6880` |

Na cópia validada não existiam evidências FK SQL (0); a cobertura de FK está assegurada por fixtures de teste. Não generalizar este resultado para outras bases de dados.

O parser V1.1 também tem resultados documentados para o workspace de validação: 719 expressões Parsed, 0 Invalid, 709 High e 10 Medium. Consultar o ADR 0006 para os detalhes e comparação com V1.

**Nota:** este documento consolida validações já registadas no repositório. Não significa que tenha sido executada uma nova bateria de testes nesta atualização.

## 5. Trabalho pendente e próximos passos

A sequência abaixo é uma proposta de prioridade. Só passa a trabalho em curso quando for iniciada explicitamente.

### P0 — Consolidar a experiência atual

1. Validar a abertura de workspaces em diferentes estados e confirmar a experiência completa de exploração sem ligação à instalação PHC.
2. Rever a experiência de erro, carregamento, fecho e troca de workspace.
3. Confirmar a cobertura dos collectors e a forma como o manifesto comunica sucessos, omissões, recolhas parciais e falhas.
4. Manter os testes de não alteração do workspace e da base de dados de origem.

### P1 — Completar a interpretação e navegação relacional

1. Identificar expressões e construções ainda não suportadas pelo parser e registar exemplos reais.
2. Avaliar a cobertura de estruturas relacionadas com `tliga`/`expliga` e `lt`/`ltli`, sem assumir que todas estão totalmente interpretadas.
3. Melhorar diagnósticos de expressões parciais ou não suportadas, sem perder o texto original.
4. Só depois avaliar uma representação visual/grafo de dependências, mantendo clara a distinção entre relações declaradas, FK SQL e inferências.

### P2 — Melhorar a leitura e compreensão do código PHC

Esta linha resulta das ideias discutidas em novembro de 2026. É uma **evolução planeada**, não uma capacidade já entregue.

Objetivo: permitir que o consultor compreenda mais rapidamente o que uma personalização faz, preservando sempre o código original.

Passos propostos:

1. **Mapear a cobertura:** identificar que tipos de personalização e campos de código são recolhidos e quais podem ser analisados. Não alterar consultas, âmbito ou critérios de recolha apenas para facilitar a análise.
2. **Melhorar a formatação:** evoluir o formatador FoxPro existente, que atualmente faz uma separação heurística de instruções em código achatado. A evolução deve reconhecer blocos e níveis de indentação, respeitando strings, literais e comentários.
3. **Análise estrutural:** identificar blocos `IF/ELSE/ENDIF`, `DO CASE`, ciclos, procedimentos, chamadas, consultas e alterações de dados, preservando a hierarquia e a ordem.
4. **Explicação por blocos:** produzir explicações associadas aos blocos lógicos, distinguindo factos identificados de interpretações e assinalando incerteza.
5. **Contexto PHC:** enriquecer as explicações com tabelas, campos, relações e metadados já recolhidos pelo Scanner.
6. **IA opcional:** avaliar IA apenas depois de existir uma representação estrutural fiável. A IA deve receber o código original e o contexto comprovado, não inventar tabelas, campos ou intenções.
7. **Apresentação separada:** disponibilizar, sem substituir o original, três representações: original, formatada e documentada/comentada.

Critérios obrigatórios desta evolução:

- O código original nunca é alterado.
- Formatação e comentários são artefactos derivados, nunca código para execução automática.
- Não se aplicam sugestões de melhoria ao código original.
- Explicações devem indicar quando são inferências ou quando a evidência é insuficiente.
- A análise não deve executar código PHC nem SQL recolhido.

### P3 — Integração com PHC Knowledge

- **Contrato de intercâmbio:** IKP 0.1 definido por especificação, schemas, fixtures, validador e matriz de compatibilidade.
- **Exportação IKP:** implementada em `PhcKnowledgeScanner.Export`, com comandos CLI `scan-ikp` e `export-ikp`.
- **Validação real:** Object Index projetado com a captura PHC default `CSDefault202603` (8/21 kinds presentes; 16.618 objetos e 49 artefactos com tamanho/hash confirmados). Falta validar o ZIP completo produzido pelo exportador com o validador independente.
- **Integração no Knowledge:** Receive / Validate / Archive, receipts/recovery, API HTTP e análise IKP A+B já estão implementados no núcleo central. A confirmação explícita (Incremento C) está numa PR aberta, não integrada na main; a materialização persistente continua por implementar.
- Preservar a independência do Scanner: não criar dependência runtime ou referência de projeto para a plataforma Knowledge.

## 6. Fora de âmbito / não assumir como decidido

- Grafo visual de dependências: ainda não implementado.
- Inferência automática de relações apenas a partir de código: não faz parte da camada atual de factos REL/DTL/FK.
- Comentários funcionais automáticos por IA: ainda não implementados.
- Reescrita ou correção automática de código PHC: não pretendida.
- Escrita na base de dados PHC: fora de âmbito.
- Materialização persistente do conhecimento no PHC Knowledge: ainda não implementada. O emissor IKP 0.1 e a recepção/análise central já existem; a materialização SQL Server, verificação integral e eliminação do ZIP continuam pendentes.
- Qualquer alteração ao `ConsoleApp`/PHC Knowledge: projeto separado.

As decisões técnicas ainda em aberto continuam registadas em [open-decisions.md](open-decisions.md). Não converter automaticamente esses pontos em requisitos.

## 7. Regra de atualização deste documento

No final de cada ciclo de implementação, atualizar apenas o necessário:

1. Mover os itens concluídos para **Trabalho concluído**, com referência ao commit/ADR/handoff.
2. Atualizar o estado dos itens pendentes.
3. Registar validações com dados concretos e respetivo contexto.
4. Distinguir sempre **Implementado**, **Validado**, **Parcial** e **Planeado**.
5. Não criar um novo relatório de estado para cada pequena alteração; este é o ponto de situação central. ADRs continuam a registar decisões e handoffs continuam a registar validações específicas.

---

## 8. Estado de referência para retomar

**Estado de referência — 2026-11-10:** Scanner desktop/CLI, collectors SQL/PHC, workspace SQLite, análise REL/DTL e exportador IKP 0.1 implementados. A plataforma Knowledge não é executada pelo Scanner. A próxima validação do lado Scanner é o ZIP IKP real; a próxima implementação do lado Knowledge é Receive / Validate / Archive.

**Próxima atividade recomendada:** consolidar e validar a experiência atual de abertura/exploração de workspaces; depois fechar a cobertura das expressões e só então iniciar a evolução da compreensão do código PHC.

**Não começar já:** grafo visual, explicações por IA ou reescrita de código. Primeiro confirmar cobertura, estrutura e critérios de validação.


## 9. Integração com o PHC Knowledge — fronteira atual

O Scanner é produtor de factos e pacotes. Não conhece nem chama a API do Knowledge, não acede à base central e não mantém histórico multi-captura. O IKP transporta uma captura e a respetiva cobertura/proveniência. A plataforma central validará e arquivará o pacote, associará instalações e materializará conhecimento em etapas próprias.


## Actualização de integração com o PHC Knowledge — 2026-11-21

As decisões actuais da plataforma estão em [Decisões arquitecturais aprovadas](../../docs/architecture/decisoes-aprovadas-2026-11-21.md) e prevalecem sobre planos anteriores deste estado do Scanner.

- O Scanner continua autónomo e não consulta o histórico central para seleccionar o que recolhe.
- Cada execução produz uma captura completa dentro do âmbito das operações declaradas e reporta cobertura/estado de cada operação.
- A recolha REL/DTL mantém-se; o Knowledge compara os factos declarados com o catálogo PHC Standard versionado.
- O ZIP é transporte temporário, não arquivo permanente.
- A plataforma materializa dados estruturados e artefactos originais no SQL Server e só elimina o ZIP após verificação integral.
- Não alterar collectors, workspace SQLite ou semântica dos dados antes da matriz de classificação por collector/ObjectKind ser validada com a captura real existente.

Esta actualização regista decisões de integração; não declara alterações ou validações de código do Scanner.


## Estado de integração actualizado — 23-11-2026

Esta secção prevalece sobre as fotografias anteriores deste documento sempre que descrevam a integração Scanner ↔ Knowledge.

- O repositório canónico é `Jhouinho/PhcKnowledge`; este projecto vive em `PhcKnowledgeScanner/` dentro do mesmo monorepo. Mantém autonomia funcional, solução, dependências, CLI/desktop e testes próprios.
- O Scanner produz IKP 0.1. A implementação D-022 está integrada na main (PR #27, merge `ad42f53fc3d4f7b654d61dea56f0f501af0905b8`): Object Index v2 e envelope de DefinitionFingerprint D-021. CI associado: 291 testes Scanner aprovados, 2 ignorados, além dos restantes workflows verdes.
- A integração D-022 ainda precisa de validação com uma captura nova numa instalação PHC real. A captura CSDefault202603 anterior não deve ser apresentada como validação desta nova implementação.
- No Knowledge já existem Receive / Validate / Archive, receipts/recovery, API HTTP e análise IKP A+B. Isto não significa que o conhecimento esteja materializado.
- A confirmação explícita da análise e a UI React estão propostas na PR #20, ainda fora da main. A branch está divergente da main actual e exige reconciliação/revisão antes de qualquer merge.
- A materialização persistente de observações, relações REL/DTL, metadados e artefactos originais no SQL Server central, bem como a verificação integral e eliminação do ZIP, continuam por implementar.
- O Scanner não deve consultar o histórico central para decidir o que recolhe. Não alterar collectors, workspace SQLite ou semântica de recolha sem decisão explícita e validação baseada em evidência real.

O estado global, sequência e dependências estão em `docs/architecture/roadmap-visual-phc-knowledge-2026-11-06.md` (secção 10) e `README.md`. Esta actualização documental não executou build/testes localmente nem validou uma instalação PHC.

---

## Estado da integração com o PHC Knowledge — 2026-11-24

Esta atualização prevalece sobre as fotografias anteriores deste documento no que respeita à integração Scanner ↔ Knowledge.

- A baseline do monorepo `Jhouinho/PhcKnowledge` é a `main`, merge commit `dfb44b5ce4370ccf5b43856c87328f1915c58a99`.
- A PR #28 integrou o Incremento C sobre D-022. A confirmação/rejeição de `PackageImportAnalysis` e a UI React do Portal estão agora na `main`; a PR #20 original foi preservada e não deve ser apresentada como trabalho ainda por integrar.
- D-022 permanece na baseline: Object Index v2 e DefinitionFingerprint D-021. IKP global continua em 0.1.
- Os quatro checks associados à PR #28 passaram, incluindo build/testes .NET, Portal React e integração SQL Server 2022/Scanner. Não foi declarado novo build local nesta atualização.
- A integração D-022 continua a precisar de validação com recolhas reais: primeiro a CS default, depois uma base PHC com desenvolvimento, com a mesma versão do Scanner e artefactos separados. O procedimento está em [Protocolo de validação funcional](../../docs/architecture/scanner-functional-validation-protocol-2026-11-24.md). Foi preparado em 2026-11-24 e as recolhas ainda não foram executadas. A captura CSDefault202603 anterior não comprova o comportamento da implementação D-022.
- O Scanner mantém autonomia funcional e não deve ser executado pelo Knowledge em runtime, nem consultar o histórico central para decidir o que recolhe.
- Não alterar collectors, workspace SQLite, queries ou semântica de recolha antes de fechar a matriz collector/operation/ObjectKind com evidência de captura e aprovação explícita.
- Próximo trabalho: matriz de classificação e cobertura; depois contrato físico de materialização SQL Server. A materialização persistente, verificação integral e eliminação segura do ZIP continuam fora do estado implementado.

A revisão documental não executou uma nova captura PHC nem uma nova bateria de testes local. CI aprovado não equivale a validação numa instalação PHC real.

