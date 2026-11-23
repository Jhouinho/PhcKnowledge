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


## 9. Validação da divergência D-019 — auditoria estática

**Data:** 23-11-2026  
**Base de código:** `main`  
**Resultado:** divergência confirmada; não foram feitas alterações ao código nem executados testes.

### 9.1 Evidência directa

No ficheiro `PhcKnowledgeScanner/src/PhcKnowledgeScanner.Export/ObjectIndex/DefinitionProjectionResolver.cs`:

1. As linhas 148–160 calculam SHA-256 de `artifact.Content` e comparam-no com `ExpectedSha256`. Esta verificação é correcta como validação de integridade do artefacto.
2. As linhas 168–170 reutilizam exactamente o mesmo `digest` para construir `DefinitionFingerprintValue("sha256", digest)`.
3. Não existe nesse percurso uma representação canónica semântica, uma política de normalização, uma versão de normalizador ou uma distinção entre comparação exacta e fingerprint semântico.
4. O tipo `DefinitionFingerprintValue`, no fim do mesmo ficheiro, está documentado como “SHA-256 fingerprint of the exact bytes of a collected definition artifact”, descrição que corresponde ao contrato antigo e contradiz a D-019.

### 9.2 Os testes confirmam o comportamento antigo

Ficheiro `PhcKnowledgeScanner/tests/PhcKnowledgeScanner.Tests/DefinitionProjectionResolverTests.cs`:

- `Phc_Definition_Is_Available_And_Fingerprints_Exact_Artifact_Bytes` (linhas 47–63) exige explicitamente que o fingerprint seja igual ao SHA-256 dos bytes exactos.
- `Sql_View_With_Definition_Artifact_Is_Available` (linhas 66–82) também exige o hash exacto do conteúdo do artefacto.
- Existem testes para mismatch do hash de integridade, mismatch do comprimento, bytes em falta e definição vazia. Esses cenários são úteis e devem ser preservados.
- Não foram encontrados testes que demonstrem normalização semântica por `objectKind`, versão de política, comparação entre versões ou distinção entre hash de integridade e `DefinitionFingerprint`.

Assim, não é apenas uma descrição desactualizada: os testes automatizados actualmente protegem o comportamento anterior à D-019.

### 9.3 O que já está correctamente separado

O cálculo de `MetadataFingerprint` está separado em `MetadataProjectionResolver` e `MetadataFingerprintCalculator`:

- a projecção selecciona os atributos semanticamente relevantes;
- a canonicalização JSON é determinística;
- o SHA-256 é calculado sobre a projecção canónica;
- os testes cobrem alterações semânticas, exclusão de identificadores/datas não relevantes e ausência de fingerprint quando a projecção é incompleta.

No caso de triggers, a projecção de metadata exige informação completa e inclui no fingerprint, quando válida, `isDisabled`, `isInsteadOf` e `events`. Isto é coerente com a separação entre texto da definição e estado/configuração do trigger, embora a política de negócio desses atributos ainda deva ser validada na matriz.

Para views e stored procedures, a projecção de metadata é intencionalmente vazia e não produz `MetadataFingerprint`. A adequação desta opção deve ser avaliada separadamente; não é a causa da divergência D-019.

### 9.4 Lacuna no contrato serializado

O modelo serializado actual de `object-index.json` expõe:

- `definition.status`;
- `definition.artifactIds`;
- `definition.definitionFingerprint` (`algorithm` e `value`);
- `metadataFingerprint` separado.

O validador confirma o formato SHA-256, mas não consegue verificar se o valor deriva de bytes exactos ou de uma representação semântica, nem qual a versão da política que o produziu. A estrutura actual não transporta explicitamente essa distinção nem a versão do normalizador.

A D-019 exige que essa versão seja conhecida e que fingerprints de políticas diferentes não sejam comparados como equivalentes. Portanto, a implementação terá de definir onde essa informação fica representada no contrato, sem assumir que o campo actual `algorithm=sha256` é suficiente.

### 9.5 Conclusão da validação

<badge>Conclusão técnica</badge>

A divergência está **confirmada em três níveis**:

1. **Implementação:** o projector usa o hash de integridade como `DefinitionFingerprint`.
2. **Modelo/documentação do código:** o tipo ainda define o fingerprint como hash dos bytes exactos.
3. **Testes:** os testes exigem o comportamento antigo e não cobrem a semântica D-019.

O cálculo de `MetadataFingerprint` já tem uma arquitectura separada e testes próprios; não deve ser confundido com esta correcção.

### 9.6 Correcção necessária — ainda não implementada

A futura alteração deverá, no mínimo:

1. Continuar a validar `ExpectedSha256` contra os bytes exactos do artefacto. Esta verificação não pode ser removida nem enfraquecida.
2. Manter o hash de integridade do artefacto separado do fingerprint semântico.
3. Calcular `DefinitionFingerprint` apenas através de política de normalização explícita, específica por kind/variante e versionada.
4. Se ainda não existir normalizador seguro, não emitir um fingerprint semântico inventado. A comparação exacta poderá existir como modo distinto, claramente identificado, se o contrato for actualizado para o representar.
5. Actualizar o modelo, o projector, o contrato/schema IKP e o validador em conjunto, para que a versão da política seja transportada e validada.
6. Substituir os testes que hoje exigem igualdade entre fingerprint e hash dos bytes por testes independentes de integridade e semântica, mantendo os testes de corrupção/bytes/length.
7. Garantir compatibilidade explícita com pacotes antigos: não reinterpretar silenciosamente fingerprints antigos como se fossem semânticos D-019.

**Não foi feita qualquer alteração de código.** Esta secção é uma auditoria estática da `main`; não equivale à execução da suite de testes nem à validação do comportamento em runtime.
