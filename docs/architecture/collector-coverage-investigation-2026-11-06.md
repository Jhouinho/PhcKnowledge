# Investigação — cobertura real dos collectors do Scanner

| Campo | Valor |
|---|---|
| Data | 2026-11-06 |
| Estado | Investigação histórica concluída; contrato P0/IKP e exportador implementados posteriormente |
| Repositório analisado | `Jhouinho/PhcKnowledgeScanner`, branch `main` |
| Âmbito | Cobertura, resultados vazios, ausência de estruturas, falhas e limites de observação |
| Implementação | Nenhuma alteração ao código do Scanner ou ao contrato IKP nesta investigação |

Este documento regista a análise realizada em 2026-11-06 e descreve o comportamento então encontrado. Desde essa data, foram implementados no Scanner o catálogo de capabilities, coverage por operação e exportador IKP 0.1. As conclusões abaixo são históricas; o estado atual é descrito na matriz `ikp-0.1-scanner-compatibility-matrix.md`.

## 1. Objetivo

Determinar se o Scanner atual consegue descrever, com rigor suficiente, o que foi observado em cada captura, para que o PHC Knowledge possa comparar capturas sem confundir:

- objeto novo, alterado, inalterado ou removido;
- objeto que deixou de ser observado;
- categoria não solicitada;
- estrutura inexistente ou não aplicável;
- consulta sem resultados;
- consulta que falhou;
- inventário disponível, mas definição/conteúdo indisponível.

A regra central mantém-se: **a ausência de um objeto só pode ser interpretada como remoção quando a captura atual observou integralmente o âmbito relevante e a captura anterior também permite uma comparação válida.**

## 2. Fontes analisadas

No Scanner independente foram consultados:

- `src/PhcKnowledgeScanner.Collectors/Phc/PhcCustomizationsCollector.cs`
- `src/PhcKnowledgeScanner.Collectors/Phc/PhcDeclaredRelationsCollector.cs`
- `src/PhcKnowledgeScanner.Collectors/Sql/SqlServerStructureCollector.cs`
- `src/PhcKnowledgeScanner.Abstractions/Collectors/CollectorResult.cs`
- `src/PhcKnowledgeScanner.Abstractions/Collectors/CollectionIssue.cs`
- `src/PhcKnowledgeScanner.Abstractions/Collectors/CollectorStatus.cs`
- `src/PhcKnowledgeScanner.Abstractions/Collectors/ICollector.cs`
- `src/PhcKnowledgeScanner.Abstractions/Collectors/ScanContext.cs`
- `src/PhcKnowledgeScanner.Abstractions/Manifest/ScanManifest.cs`

A análise incidiu sobre o código da branch `main`. Não foi executada uma recolha numa base PHC real nesta etapa. As conclusões são, por isso, sobre o comportamento do código e não constituem prova de resultados em todas as versões/instalações PHC.

## 3. Modelo atual de resultados dos collectors

O contrato atual de `CollectorResult` contém:

- `CollectorId`
- `Status`
- `Artifacts`
- `Issues`
- `Duration`

Os estados disponíveis em `CollectorStatus` são:

- `Succeeded`
- `SucceededWithWarnings`
- `Skipped`
- `Failed`
- `Partial`

Cada `CollectionIssue` contém severidade, código, mensagem e, opcionalmente, nome de objeto e detalhe.

O manifesto `ScanManifest` agrega os resultados dos collectors, juntamente com instalação, datas, modo de scan e versão do Scanner.

**Limitação estrutural:** o resultado é essencialmente agregado por collector. Não existe no contrato analisado um registo de cobertura autónomo por categoria/subconsulta, com âmbito, estado, contagem observada e evidência da execução. Quando um collector agrupa muitas categorias, o seu estado global não permite concluir que cada categoria foi observada com sucesso.

## 4. Análise por collector

### 4.1. Personalizações PHC — `PhcCustomizationsCollector`

O collector tem o identificador `phc.customizations` e executa várias recolhas sequenciais dentro de uma única execução.

| Categoria | Origem consultada | Tratamento no código | Observação |
|---|---|---|---|
| BRULE | `dbo.brule` | Consulta direta | Falha gera issue; não há cobertura própria |
| EBRULE | `dbo.ebrule` | Opcional, verificação prévia de existência | Se ausente, é ignorada sem issue estruturada de cobertura |
| Teclas | `dbo.ti` + `dbo.tc` | Consulta conjunta | Resultado agregado; não separa cobertura das duas tabelas |
| UDEFS | `dbo.udefs` | Consulta direta | Falha gera issue; não há cobertura própria |
| EUDEFS | `dbo.eudefs` | Opcional | Ausência ignorada sem cobertura estruturada |
| Eventos | `dbo.eventos` | Consulta direta | Falha gera issue; não há cobertura própria |
| Eventos Web | `dbo.eeventos` | Opcional | Ausência ignorada sem cobertura estruturada |
| Campos físicos de utilizador | `sys.columns`, `sys.tables`, `sys.schemas`, `sys.types` | Procura colunas com prefixo `u_` | Não consulta o catálogo funcional `dbo.campos` |
| Scripts Web | `dbo.escr` | Opcional | Ausência ignorada sem cobertura estruturada |
| JavaScript | `dbo.jsu` | Opcional | Ausência ignorada sem cobertura estruturada |
| CSS/JS | `dbo.cssjs` | Opcional | Ausência ignorada sem cobertura estruturada |
| Grelhas | `dbo.formc` | Opcional | Recolha parcial focada em grelhas ocultas |
| Ecrãs | `dbo.formb` | Opcional | Recolha de ecrãs personalizados |
| Funções T-SQL PHC | `dbo.ftu` e catálogo SQL de funções instaladas | Opcional para `ftu`; pesquisa adicional no catálogo | Ausência/resultado não são formalizados por categoria |

#### Disponibilidade global

O `ProbeAsync` considera o collector disponível se encontrar pelo menos uma das tabelas `brule`, `eventos` ou `udefs`. Isto apenas demonstra que existe pelo menos uma estrutura core reconhecida; não garante a disponibilidade nem a recolha das restantes categorias.

#### Categorias opcionais

O método local `CollectOptionalAsync` verifica se a tabela existe e, quando não existe, apenas reporta progresso com a indicação de que foi ignorada. Não regista uma issue de cobertura que permita ao manifesto distinguir ausência, não aplicabilidade ou falha de deteção.

A verificação `TableExistsAsync` consulta `INFORMATION_SCHEMA.TABLES`. No fluxo de categorias opcionais, essa verificação não está isolada por um tratamento de exceção próprio. Se falhar, a exceção pode interromper o restante `CollectAsync`; o tratamento global do ScanEngine poderá então registar falha do collector, embora já possam existir objetos persistidos no workspace. Nesse cenário, a informação por categoria que foi concluída ou ficou por executar não é formalizada num resultado de cobertura.

#### Filtros que afetam o inventário

Foram confirmados filtros de conteúdo em várias categorias, já identificados na investigação anterior:

- BRULE: ignora registos sem `expressao`.
- UDEFS: ignora registos sem conteúdo nos campos de valor/código XBase/SQL considerados pelo collector.
- Eventos e eventos Web: ignoram registos sem conteúdo nas expressões/condições consideradas.
- Teclas: ignora registos sem `retorno`.

Estes filtros podem excluir definições PHC que continuam a ser personalizações mesmo sem código executável. Para inventário, a recomendação é separar **existência/metadados do registo** de **conteúdo executável disponível**.

#### Campos de utilizador

A recolha atual consulta apenas colunas físicas `u_*` em `sys.columns`. Não liga essas colunas aos metadados funcionais da tabela PHC `campos`. Assim, o resultado não representa a cobertura completa da definição funcional de campos de utilizador. Esta lacuna já estava identificada na comparação entre o provider antigo e o collector atual e deve manter-se no contexto desta investigação.

### 4.2. Relações declaradas — `PhcDeclaredRelationsCollector`

Este collector apresenta o comportamento mais próximo de uma cobertura granular.

Consulta separadamente:

- `dbo.rel`
- `dbo.dtl`

Para cada uma distingue:

| Situação | Comportamento atual |
|---|---|
| Tabela encontrada e com registos | Recolhe os registos e reporta contagem |
| Tabela encontrada e vazia | Regista issue informativa `*.empty` |
| Tabela não encontrada | Regista aviso `*.absent` |
| Erro ao consultar | Regista issue de erro `*.read` |

Se uma das tabelas falhar e a outra for recolhida, os factos disponíveis podem ser persistidos. O estado global é calculado a partir das issues e dos factos recolhidos.

É uma boa referência para a futura implementação de cobertura por categoria, mas ainda não constitui o contrato de cobertura pretendido: a informação está expressa em issues e mensagens, não num objeto de cobertura formal e uniforme.

A resolução de tabelas ambíguas ou não resolvidas é reportada separadamente e não elimina os factos originais. Isto deve ser preservado: uma dificuldade de interpretação não equivale a falha da recolha do facto declarado.

### 4.3. Estrutura SQL Server — `SqlServerStructureCollector`

O collector executa operações distintas para:

| Categoria | Fonte | Notas |
|---|---|---|
| Metadados da base de dados | Funções/metadados SQL Server | Informação básica da BD |
| Tabelas | `sys.tables` e catálogos associados | Inventário de tabelas |
| Chaves primárias | Catálogos SQL Server | Recolha de colunas e ordem |
| Chaves estrangeiras | Catálogos SQL Server | Recolha de extremos e colunas |
| Views | `sys.views` + `sys.sql_modules` | Definição pode não estar disponível |
| Stored procedures | `sys.procedures` + `sys.sql_modules` | Definição pode não estar disponível |
| Triggers | `sys.triggers` + `sys.sql_modules` | Filtro atual limitado a tabelas `u_*` |

#### Inventário versus definição

Nas views, procedures e triggers, o código tenta guardar o objeto mesmo quando a definição SQL não está disponível. Nesse caso, o objeto é persistido sem artefacto de definição e é registado um aviso.

Isto revela duas dimensões diferentes:

1. **Cobertura do inventário:** foi possível determinar que o objeto existe.
2. **Cobertura do conteúdo:** foi possível recuperar a sua definição.

O modelo futuro deve representar ambas. A ausência de definição não pode ser interpretada como ausência do objeto.

#### Resultados vazios e permissões

O collector já tem tratamento específico para alguns casos, como catálogo de tabelas vazio, que gera aviso por poder representar falta de permissões. Para outras operações, a consulta pode terminar sem objetos e o progresso indicar zero, sem que exista necessariamente uma issue explícita de resultado vazio.

O contrato de cobertura deverá uniformizar esta distinção. Uma consulta bem-sucedida com zero resultados é diferente de uma consulta que falhou ou cujo resultado foi condicionado por permissões insuficientes.

#### Âmbito dos triggers

A consulta atual filtra triggers associados a tabelas cujo nome começa por `u_`. Logo, o resultado não é um inventário de todos os triggers SQL da base de dados. A cobertura deve declarar explicitamente este âmbito, por exemplo, triggers em tabelas candidatas a personalização, e nunca apresentar a categoria como globalmente completa sem essa qualificação.

## 5. Matriz consolidada — capacidade de cobertura atual

| Área | Granularidade observável atualmente | Ausência/falha diferenciadas? | Zero resultados explícito? |
|---|---|---|---|
| Personalizações PHC | Principalmente collector global | Parcialmente | Nem sempre |
| REL | Tabela individual via issues | Sim | Sim |
| DTL | Tabela individual via issues | Sim | Sim |
| Tabelas SQL | Operação individual via issues | Parcialmente | Aviso em catálogo vazio |
| Chaves primárias | Operação individual via issues | Parcialmente | Não uniformemente |
| Chaves estrangeiras | Operação individual via issues | Parcialmente | Reportado no progresso |
| Views | Operação individual via issues | Parcialmente | Não uniformemente |
| Procedures | Operação individual via issues | Parcialmente | Não uniformemente |
| Triggers | Operação individual via issues | Parcialmente | Não uniformemente |

“Parcialmente” significa que existem issues e estados globais, mas não um registo formal de cobertura por categoria com âmbito e resultado inequívocos.

## 6. Modelo de cobertura recomendado

A recomendação é evoluir para um registo explícito por categoria/capability, sem substituir o manifesto de captura nem o resultado global dos collectors.

### 6.1. Estados recomendados para cobertura

| Estado | Significado |
|---|---|
| `Complete` | O âmbito declarado foi consultado integralmente |
| `Partial` | Apenas parte do âmbito foi observada |
| `Failed` | Não foi possível obter um resultado fiável |
| `NotRequested` | A categoria não fazia parte da captura |
| `NotPresent` | A estrutura esperada não foi encontrada |
| `NotApplicable` | Foi confirmado que a categoria não se aplica à instalação |

A distinção entre `NotPresent` e `NotApplicable` é intencional. Uma tabela ausente não prova, por si só, que a funcionalidade não se aplica: pode refletir versão, variante de schema, permissões ou uma alteração de estrutura.

Estes estados são uma recomendação para a cobertura por categoria; **não são estados já implementados no Scanner nem devem ser assumidos como alteração aprovada ao IKP**.

### 6.2. Dimensões independentes

A cobertura do inventário deve ser separada da disponibilidade do conteúdo de cada objeto.

Exemplos:

- Uma view foi identificada, mas a definição SQL não está acessível.
- Uma BRULE foi identificada, mas não tem expressão executável.
- Um evento foi inventariado, mas não foi possível recuperar um dos seus campos de código.
- Uma categoria foi consultada integralmente e não devolveu registos.

Assim, uma categoria pode ter inventário completo mesmo quando o conteúdo de alguns objetos está indisponível.

### 6.3. Informação mínima por categoria

Como orientação para o contrato, cada registo de cobertura deverá conseguir declarar:

- identificador estável da categoria/capability;
- âmbito efetivamente consultado (incluindo filtros);
- estado;
- número de objetos observados, quando conhecido;
- início/fim ou referência temporal da operação, se necessário;
- fonte/consulta ou identificador da operação;
- issues e limitações relevantes;
- versão do Scanner/collector responsável.

A estrutura final deve ser decidida na revisão do IKP, evitando duplicar desnecessariamente dados já presentes no manifesto.

## 7. Comparação de capturas — consequências

O PHC Knowledge deve comparar **observações documentadas**, não apenas listas de objetos.

Estados conceptuais de comparação:

| Estado | Significado |
|---|---|
| `Added` | Objeto novo, ausente numa captura anterior com cobertura comparável e completa |
| `Modified` | Objeto existente cujo conteúdo ou metadados relevantes mudaram |
| `Unchanged` | Objeto existente sem alterações relevantes |
| `NotObserved` | A captura atual não permite determinar se o objeto continua a existir |
| `Removed` | Objeto anteriormente conhecido, ausente numa captura atual com cobertura relevante completa |

`NotObserved` é uma condição da observação, não uma alteração confirmada do objeto.

Uma captura seletiva, uma categoria não solicitada, uma consulta falhada ou uma cobertura parcial não pode produzir remoções nessa categoria. Uma categoria completa com zero resultados pode suportar a conclusão de remoção, desde que o âmbito e a identidade sejam comparáveis.

Poderá ser avaliada mais tarde uma confirmação de remoções em duas capturas completas consecutivas, para reduzir falsos positivos. Não é uma decisão fechada nesta investigação.

## 8. Relação com IKP 0.1

Foi consultada a especificação existente em `docs/architecture/ikp-0.1-specification.md`, especialmente a secção 7.

O IKP 0.1 já prevê:

- `coverage.json` obrigatório;
- cobertura por categoria;
- `captureId`;
- estados alinhados, quando aplicável, com os estados dos collectors;
- distinção entre `Observed`, `NotObserved` e `Removed`;
- remoção apenas como conclusão da plataforma após comparação de capturas com cobertura relevante completa.

A secção 7.1 enumera atualmente `Succeeded`, `SucceededWithWarnings`, `Partial`, `Failed`, `Skipped` e `NotAttempted`. A investigação mostra que este modelo ainda precisa de maior precisão semântica para representar ausência de estrutura, não aplicabilidade, resultados vazios e disponibilidade do conteúdo por objeto.

**Não foi alterada a especificação IKP nesta etapa.** A recomendação é reconciliar o contrato existente com as necessidades encontradas no código antes de aprovar novos estados ou formatos. Deve evitar-se manter duas taxonomias concorrentes: uma para o Scanner e outra para o IKP, sem mapeamento explícito.

## 9. Princípios a preservar

1. Não interpretar ausência de objeto como remoção sem cobertura completa.
2. Não interpretar estrutura ausente automaticamente como não aplicável.
3. Distinguir consulta bem-sucedida com zero resultados de consulta falhada.
4. Distinguir inventário do objeto e disponibilidade da respetiva definição/conteúdo.
5. Declarar o âmbito real das consultas e filtros.
6. Não considerar o estado global `Succeeded` de um collector agregado como prova de cobertura completa de todas as categorias.
7. Preservar factos recolhidos mesmo quando a interpretação ou resolução de referências é incompleta.
8. Manter a estratégia seletiva de recolha de personalizações: não exigir recolha integral de todas as estruturas standard PHC para produzir cobertura.
9. Não recolher dados operacionais dos clientes; o âmbito é metadados e definições técnicas de personalização.
10. Não alterar o código nem aprovar o contrato IKP sem validação conjunta.

## 10. Conclusões da investigação

- O Scanner já tem modularidade e issues suficientes para servir de base a um modelo de cobertura mais rigoroso.
- O collector de personalizações é o ponto mais frágil, porque agrega muitas categorias e ignora silenciosamente algumas estruturas opcionais ausentes.
- O collector REL/DTL já distingue individualmente ausência, vazio e falha, constituindo uma referência útil.
- O collector SQL separa, em alguns casos, a existência do objeto da disponibilidade da definição, mas não generaliza essa distinção como cobertura formal.
- Os estados globais atuais não permitem comparar capturas com segurança por categoria.
- O IKP 0.1 já tem o lugar conceptual para esta informação (`coverage.json`), mas a sua semântica precisa de ser revista à luz dos collectors reais.

**Conclusão principal:** o PHC Knowledge não deve comparar apenas dois inventários. Deve comparar duas observações, cada uma com a sua cobertura, âmbito, proveniência e capacidade efetiva de recolha.

## 11. Próximo passo recomendado à data — histórico

Este passo foi executado posteriormente: a semântica de capabilities/coverage foi incorporada no contrato IKP 0.1 e implementada no Scanner. Consultar a matriz de compatibilidade para validações ainda pendentes.

Ordem recomendada:

1. Rever a semântica de cobertura no IKP 0.1.
2. Definir a granularidade necessária (categoria/subcategoria/operação e, quando necessário, conteúdo por objeto).
3. Definir como os collectors declaram âmbito, contagens, resultados vazios, ausência e falhas.
4. Definir o mapeamento dos estados do Scanner para o IKP.
5. Só depois avaliar alterações ao Scanner e testes de comparação de capturas.

Não iniciar ainda a implementação do comparador nem alterar os collectors antes de validar este contrato.


## Atualização de estado — 2026-11-10

O contrato IKP 0.1 e o Scanner já dispõem de coverage por operação, capabilities e exportador dedicado. A validação real do Object Index cobriu 8/21 kinds da captura default; não se deve generalizar essa validação aos restantes kinds. A investigação original permanece preservada como diagnóstico da situação de 2026-11-06.
