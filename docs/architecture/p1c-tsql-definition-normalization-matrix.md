# P1.C — Matriz inicial de normalização de definições T-SQL

- **Data:** 23-11-2026
- **Repositório:** `Jhouinho/PhcKnowledge`
- **Base de análise:** branch `main`, código dos collectors e projector consultado nesta data
- **Estado:** inventário técnico preliminar; **não aprovado como política de normalização**
- **Âmbito:** `view`, `stored_procedure`, `trigger` e `phc.user_tsql_function`
- **Restrição:** não altera collectors, exporter, workspace SQLite, schemas executáveis, validator ou materialização SQL.

Este documento inicia a matriz exigida pela D-019. Regista o comportamento actualmente observado e separa factos de código de regras ainda por decidir. Não autoriza a implementação de normalizadores.

## 1. Resumo do estado actual

| ObjectKind | Fonte consultada | Construção do artefacto actual | Conteúdo original preservado? | Situação para fingerprint semântico |
|---|---|---|---|---|
| `view` | `sys.views` + `sys.schemas` + `sys.sql_modules.definition` | A string `definition` é codificada em UTF-8 e guardada sem cabeçalho ou trim explícito | Preserva o texto devolvido por `sys.sql_modules`; não é uma cópia byte-a-byte do ficheiro/origem de autoria | Sem normalizador T-SQL validado; comparação conservadora |
| `stored_procedure` | `sys.procedures` + `sys.schemas` + `sys.sql_modules.definition` | Igual a `view`: string devolvida, UTF-8, sem cabeçalho ou trim explícito | Preserva o texto devolvido pelo catálogo SQL Server, não os bytes de autoria | Sem normalizador T-SQL validado; comparação conservadora |
| `trigger` | `sys.triggers` + tabela/pai + `sys.sql_modules.definition` | A definição é guardada em UTF-8 sem cabeçalho ou trim explícito; metadados adicionais seguem em `SummaryJson` | Preserva o texto devolvido pelo catálogo SQL Server, não os bytes de autoria | Sem normalizador T-SQL validado; comparação conservadora |
| `phc.user_tsql_function` / `source=ftu` | `dbo.ftu.xbase` | Acrescenta cabeçalho gerado, metadados PHC e grava `xbase.TrimEnd()` | **Não**: artefacto composto e derivado; o campo de origem já sofre trim final | Não calcular sobre o artefacto inteiro; requer separar/provar a projecção do código |
| `phc.user_tsql_function` / `source=sys.objects` | `sys.objects` + `OBJECT_DEFINITION(object_id)` | Acrescenta cabeçalho gerado e grava `def.TrimEnd()` | **Não**: artefacto composto e derivado; o texto do catálogo sofre trim final | Não calcular sobre o artefacto inteiro; requer separar/provar a projecção do código |

## 2. Evidência observada no código

### 2.1 Views

O collector consulta `sys.views`, `sys.schemas` e `sys.sql_modules`. Quando `m.definition` não é NULL, passa a string directamente para `SaveDefinitionAsync`. Esse método converte a string em UTF-8 e guarda os bytes; não acrescenta cabeçalho nem executa `TrimEnd()`.

Quando a definição é NULL, é emitido o aviso `sql.view.definition_missing`. O Object Index identifica o tipo como `view`.

### 2.2 Stored procedures

O collector consulta `sys.procedures`, `sys.schemas` e `sys.sql_modules`. A definição segue o mesmo percurso das views: string devolvida pelo catálogo, conversão UTF-8 e gravação sem transformação textual explícita no collector.

Quando a definição é NULL, é emitido `sql.procedure.definition_missing`.

### 2.3 Triggers

O collector consulta triggers DML associadas a tabelas e obtém `m.definition` através de `sys.sql_modules`. A definição é guardada sem cabeçalho ou trim explícito.

O `SummaryJson` contém também:
- identificadores SQL de trigger e objecto-pai;
- schema e nome do pai;
- eventos associados;
- `isDisabled`;
- `isInsteadOf`;
- `definitionStatus`.

Estes campos não devem ser misturados automaticamente com o texto da definição. A política de `MetadataFingerprint` terá de decidir explicitamente quais são atributos semânticos do estado/configuração do trigger. O estado disabled e o tipo INSTEAD OF podem alterar o comportamento observável, mesmo sem alteração do texto SQL.

### 2.4 Funções T-SQL PHC

O mesmo `ObjectKind` agrega duas origens distintas, identificadas em `SummaryJson.source`.

**Origem `ftu`:**
- lê `ftustamp`, `nome`, `classif`, `xbase` e `inactivo` de `dbo.ftu`;
- ignora registos em que nome e xbase estejam ambos vazios;
- aplica `TrimEnd()` ao conteúdo de `xbase`;
- cria um artefacto com cabeçalho gerado contendo stamp, nome, classificação e estado;
- guarda o resultado como ficheiro SQL.

**Origem `sys.objects`:**
- procura funções não Microsoft dos tipos SQL `FN`, `IF` e `TF`, com nomes `Fx_*` ou `User_*`;
- obtém a definição com `OBJECT_DEFINITION(object_id)`;
- ignora definições vazias/NULL e nomes já encontrados em `ftu`;
- aplica `TrimEnd()`;
- acrescenta cabeçalho gerado com schema, nome e estado;
- guarda o resultado como ficheiro SQL.

Consequências:
1. `phc.user_tsql_function` não pode ter uma regra de fingerprint aplicada cegamente ao artefacto completo.
2. A variante `ftu` e a variante `sys.objects` precisam de regras explícitas de extracção/projecção, mesmo que ambas acabem por conter T-SQL.
3. A deduplicação actual de nomes entre as duas fontes é uma regra de recolha, não prova de equivalência semântica entre as definições.
4. O trim final elimina informação de apresentação do conteúdo recolhido; não deve ser descrito como preservação byte-a-byte do valor original.

## 3. Separação obrigatória de hashes

À luz da D-019, a matriz tem de distinguir:

- **Hash de integridade:** SHA-256 dos bytes exactos do artefacto guardado. Detecta qualquer alteração de bytes, incluindo cabeçalhos, encoding, espaços e quebras de linha.
- **DefinitionFingerprint:** SHA-256 da representação canónica semântica, específica do kind e da variante de origem, com política/normalizador versionado.
- **MetadataFingerprint:** fingerprint separado, baseado apenas em atributos de metadata explicitamente aprovados para a categoria.

O hash do artefacto composto de `phc.user_tsql_function` continua útil para integridade, mas não pode ser tratado como fingerprint semântico do T-SQL sem uma projecção validada que separe código e cabeçalho gerado.

## 4. Divergência de implementação a resolver

Na versão de `DefinitionProjectionResolver` consultada na `main`, depois de validar o SHA-256 e o comprimento do artefacto, o código cria actualmente:

```csharp
new DefinitionFingerprintValue("sha256", digest)
```

O valor `digest` é calculado directamente sobre `artifact.Content`, ou seja, sobre os bytes integrais do artefacto. Isto corresponde ao hash de integridade, não à nova semântica de `DefinitionFingerprint` aprovada na D-019.

Para views, procedures e triggers, esse digest é pelo menos um hash exacto do artefacto produzido a partir do texto do catálogo. Para `phc.user_tsql_function`, inclui ainda cabeçalhos gerados e conteúdo transformado. Em nenhum destes casos a implementação actual demonstra uma normalização semântica versionada.

**Estado:** divergência documental/implementacional identificada; não corrigida nesta etapa. A correcção pertence à fase de implementação posterior, depois de a matriz e os testes estarem aprovados.

## 5. Candidatos a política — ainda não aprovados

| Categoria | Candidato conservador | O que não se deve ignorar sem prova |
|---|---|---|
| Views | Comparação exacta do texto devolvido pelo catálogo, identificada como modo exacto, até existir parser/regra validada | Literais, comentários com directivas, hints, delimitadores, casing de identificadores em contextos relevantes, SET options e semântica de collation |
| Stored procedures | Igual a views | Igual a views; em particular parâmetros, defaults, ordem, SET options e SQL dinâmico |
| Triggers | Comparação exacta do texto da definição; fingerprint de metadata separado para eventos/estado/configuração após decisão | Tudo o que possa alterar execução; não fundir `isDisabled`, `isInsteadOf` ou eventos no hash do código sem contrato explícito |
| Funções `ftu` | Extrair `xbase` como componente de definição apenas quando a proveniência e os limites forem inequívocos; caso contrário, sem fingerprint semântico | Não remover cabeçalho por heurística textual; não tratar trim aplicado pelo collector como prova de equivalência |
| Funções `sys.objects` | Obter/identificar separadamente o texto retornado por `OBJECT_DEFINITION`; até existir projecção validada, comparação exacta claramente identificada ou fingerprint semântico indisponível | Não confundir texto de catálogo com bytes de autoria; não remover cabeçalho gerado por pesquisa de delimitadores frágeis |

A comparação exacta é uma alternativa conservadora e deve ser identificada como tal. Não pode ser apresentada como equivalência semântica. Se o contrato de implementação não conseguir distinguir o modo exacto do fingerprint semântico, deve deixar o `DefinitionFingerprint` indisponível até essa distinção existir.

## 6. Testes de aceitação necessários

Antes de aprovar qualquer normalizador T-SQL, preparar testes por categoria/variante que demonstrem:

1. Mesmos bytes de definição → mesmo hash de integridade e mesmo resultado de comparação.
2. Alteração de token executável, operador, literal, parâmetro, valor por omissão ou expressão → diferença detectada.
3. Alteração de espaços/quebras de linha apenas → só é ignorada se a regra específica demonstrar equivalência para aquele contexto.
4. Comentários comuns e comentários/directivas/hints → tratados segundo a gramática e contexto; nunca removidos globalmente por regex.
5. Strings, identificadores delimitados, comentários aninhados quando aplicável e SQL dinâmico → preservados sem reinterpretação insegura.
6. Mudanças de `SET` options relevantes → não colapsadas como cosméticas.
7. Trigger: alteração de eventos, `isDisabled` ou `isInsteadOf` → detectada pela dimensão de metadata definida para o efeito.
8. `ftu` e `sys.objects`: alteração de cabeçalho gerado sem alteração do código → não deve alterar o fingerprint semântico do componente SQL, se a projecção tiver sido validada; o hash de integridade do artefacto deve mudar.
9. Definição NULL, vazia, indisponível e não recolhida → estados distintos, sem fingerprint inventado.
10. Política/versão de normalizador diferente → fingerprints não comparados como equivalentes sem migração/regra explícita.

## 7. Decisões ainda em aberto

- Qual o parser/lexer T-SQL suportado e quais as versões/dialetos SQL Server abrangidos.
- Se a primeira versão de cada kind terá apenas comparação exacta identificada ou um normalizador semântico validado.
- Como representar explicitamente o modo exacto versus o fingerprint semântico no contrato IKP.
- Como separar e preservar os componentes de código original/extraído dos cabeçalhos gerados em `phc.user_tsql_function`.
- Quais os atributos do trigger incluídos na política de `MetadataFingerprint`.
- Como tratar metadata SQL relevante que não está actualmente no `SummaryJson`, sem recolher campos adicionais antes de demonstrada a necessidade.

## 8. Próximo passo

Inspeccionar as opções de metadata SQL disponíveis para views, procedures e triggers, e fechar a matriz de **conteúdo versus metadata** para estes quatro kinds. Em paralelo, definir os casos de teste T-SQL e escolher a estratégia de parser/lexer. Não alterar código nem schemas executáveis antes de essas decisões serem aprovadas.
