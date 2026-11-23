# P1.C — Testes de aceitação para DefinitionFingerprint

- **Data:** 23-11-2026
- **Estado:** proposta de testes; ainda não aprovada como suite final
- **Decisões normativas:** D-018, D-019, D-020 e D-021
- **Âmbito:** separação entre hash de integridade, DefinitionFingerprint semântico e comparação exacta
- **Restrição:** documento de desenho; não altera código, schemas executáveis, collectors, exporter, workspace SQLite, validator ou materialização SQL.

## 1. Objectivo

Garantir que o Scanner nunca apresenta um hash dos bytes integrais como fingerprint semântico, que preserva a verificação de integridade dos artefactos e que só emite um **valor** de `DefinitionFingerprint` quando existe uma política semântica específica, identificada, versionada e validada; sem essa política, representa explicitamente a indisponibilidade conforme D-021.

Os testes devem ser independentes do normalizador concreto. Os testes específicos de cada `objectKind` serão acrescentados quando a respectiva matriz estiver aprovada.

## 2. Invariantes obrigatórios

1. O hash de integridade é calculado sobre os bytes exactos do artefacto armazenado.
2. O hash de integridade não é reutilizado como `DefinitionFingerprint`.
3. Sem política de normalização semântica validada para o kind/variante, `DefinitionFingerprint` fica indisponível.
4. A comparação exacta, se disponibilizada, é identificada como modalidade separada e não preenche o campo de fingerprint semântico.
5. Um fingerprint semântico disponível inclui a identificação/versionamento da política que o produziu (`policy.id` e `policy.version`).
6. Para uma definição `Available`, o estado do fingerprint é explícito: `Available` ou `Unavailable`.
7. Um fingerprint indisponível usa um `reasonCode` do catálogo controlado, sem `algorithm`, `value` ou `policy`.
8. Fingerprints de políticas diferentes não são comparados como equivalentes sem regra explícita de migração/compatibilidade.
9. O original e os seus bytes nunca são substituídos pela representação canónica.
10. Estados de definição vazia, indisponível, não recolhida e fingerprint indisponível não podem ser confundidos.
11. Não existe obrigação de ler ou converter pacotes IKP de teste de iterações anteriores do Scanner que tenham sido descartados; a validação parte de novas capturas conformes com o contrato actual. Só versões/capacidades explicitamente incluídas na matriz de compatibilidade são entradas suportadas.

### 2.1 Distinção entre bytes do artefacto e conteúdo da origem

Os testes e a documentação usam «bytes exactos» para identificar exclusivamente os bytes persistidos no artefacto e abrangidos pelo hash de integridade. Não se deve chamar a esses bytes «bytes originais da autoria» quando a fonte não os fornece nessa forma.

Em particular, quando o SQL Server devolve uma definição através de `sys.sql_modules.definition` ou `OBJECT_DEFINITION`, o Scanner recebe texto do catálogo e pode codificá-lo em UTF-8. O hash de integridade cobre os bytes desse artefacto produzido pelo Scanner; não prova identidade byte-a-byte com o ficheiro, script ou texto originalmente submetido ao SQL Server.

Nos artefactos compostos, como os de `phc.user_tsql_function`, o hash de integridade cobre o artefacto completo armazenado, incluindo cabeçalhos gerados e quaisquer transformações efectuadas pelo collector. Uma eventual projecção do componente de definição tem de ser validada separadamente.

## 3. Casos de aceitação transversais

| ID | Cenário | Resultado esperado |
|---|---|---|
| DF-001 | Artefacto válido, bytes conhecidos e SHA-256 esperado correcto | Hash de integridade validado; resultado independente do estado do fingerprint semântico |
| DF-002 | Um byte do artefacto é alterado sem actualizar o hash esperado | Falha de integridade; não produzir fingerprint nem aceitar o artefacto como íntegro |
| DF-003 | Comprimento declarado diferente do conteúdo | Falha de integridade/consistência; não produzir fingerprint |
| DF-004 | Kind/variante sem normalizador registado e validado | `DefinitionFingerprint` indisponível; não usar SHA-256 dos bytes como fallback |
| DF-005 | Comparação exacta solicitada para dois artefactos sem normalizador | Resultado exacto separado (igual/diferente por bytes); campo `DefinitionFingerprint` continua indisponível |
| DF-006 | Dois conteúdos byte-a-byte iguais, com normalizador validado | Mesmo hash de integridade e mesmo fingerprint semântico sob a mesma política |
| DF-007 | Conteúdos com bytes diferentes mas representação canónica igual, num caso explicitamente coberto pela política | Hashes de integridade diferentes; fingerprint semântico igual |
| DF-008 | Alteração semanticamente relevante no conteúdo | Fingerprint semântico diferente |
| DF-009 | Mesma definição processada por versões diferentes da política | Não declarar equivalência directa entre fingerprints; exigir compatibilidade/migração explícita |
| DF-010 | Normalizador falha, não suporta sintaxe ou não consegue provar a transformação | Fingerprint semântico indisponível; preservar artefacto e hash de integridade |
| DF-011 | Definição vazia real | Manter o estado de definição vazia; não fabricar fingerprint de conteúdo vazio |
| DF-012 | Definição indisponível/NULL na origem | Manter estado indisponível; não confundir com definição vazia |
| DF-013 | Definição não solicitada/não recolhida | Manter estado não recolhido; não calcular fingerprint |
| DF-014 | Artefacto de origem recolhido e representação canónica produzida | Os bytes do artefacto armazenado permanecem inalterados; a representação canónica é derivada e reproduzível. O teste não afirma que os bytes do artefacto são os bytes originais de autoria quando a fonte apenas fornece texto de catálogo ou quando o collector transforma/compõe o conteúdo |
| DF-015 | Metadata muda sem alteração da definição | `MetadataFingerprint` só muda se o atributo alterado pertencer à projecção semântica aprovada e completa para aquele kind; `DefinitionFingerprint` mantém-se |
| DF-016 | Definição muda sem alteração da metadata projectada | `DefinitionFingerprint` pode mudar; `MetadataFingerprint` mantém-se |
| DF-017 | Pacote IKP de teste de uma iteração anterior do Scanner contém fingerprint produzido pela regra antiga | O pacote não é uma entrada suportada: não se exige leitura, preservação do valor legado, classificação retroactiva ou conversão; a validação usa uma nova captura conforme ao contrato actual |
| DF-018 | Resolução semântica sem política validada | O resolver mantém `definition.status=Available` e produz o resultado de domínio `DefinitionFingerprint.Unavailable` com motivo `NoValidatedPolicy`, sem fabricar valor semântico |

### 3.1 Estados e motivos de indisponibilidade

Os testes devem distinguir, no mínimo, os seguintes estados conceptuais:

- **Definição vazia:** a fonte foi consultada e a definição existe, mas tem conteúdo vazio segundo a semântica da fonte.
- **Definição indisponível:** a fonte foi consultada, mas não disponibilizou a definição (por exemplo, NULL ou falta de permissão).
- **Não recolhida:** a operação/capability não solicitou ou não recolheu a definição.
- **Fingerprint indisponível:** a definição pode estar disponível, mas não existe política segura para produzir fingerprint, ou a política não consegue processar aquele conteúdo.

Catálogo controlado inicial aprovado para `definitionFingerprint.reasonCode`: `NoValidatedPolicy`, `UnsupportedSyntax` e `NormalizationFailed`. `DefinitionUnavailable` e `NotCollected` não pertencem a este catálogo: são representados por `definition.status`. Os códigos são valores contratuais, não mensagens livres.

A ausência de fingerprint não deve colapsar estes estados numa única condição nem alterar o estado de disponibilidade da definição. Para `definition.status` igual a `Empty`, `Unavailable` ou `NotCollected`, omite-se `definitionFingerprint`; para `Available`, o resultado é explícito (`Available` ou `Unavailable`).

## 4. Casos específicos de artefactos compostos

Aplicáveis a `phc.user_tsql_function` e a qualquer outro kind cujo artefacto inclua cabeçalhos/metadados gerados. Estes casos devem ser numerados na suite executável como casos específicos (por exemplo, DF-COMP-001 em diante), mantendo a distinção face aos cenários transversais DF-001–DF-018:

1. Alterar apenas o cabeçalho gerado altera o hash de integridade do artefacto composto.
2. Se existir uma projecção validada que isole o componente de definição, alterar apenas o cabeçalho não altera o fingerprint semântico desse componente.
3. Alterar o código extraído altera o fingerprint semântico quando a política específica está validada.
4. Se a fronteira entre cabeçalho e conteúdo não puder ser determinada de forma inequívoca, o fingerprint semântico fica indisponível.
5. O teste não pode remover cabeçalhos por heurísticas frágeis, regex genérica ou procura de delimitadores não garantidos pelo formato.

### 4.1 Fixtures obrigatórias para artefactos compostos

Criar fixtures controladas, por variante de origem, pelo menos para:

| ObjectKind / variante | Fixture mínima | Verificação essencial |
|---|---|---|
| `phc.user_tsql_function` / `ftu` | Registo com `xbase`, stamp e metadados; artefacto com cabeçalho gerado | Hash de integridade inclui o cabeçalho; não remover o cabeçalho por heurística; fingerprint semântico indisponível enquanto a extracção do componente não estiver validada |
| `phc.user_tsql_function` / `sys.objects` | Definição obtida por `OBJECT_DEFINITION`, com cabeçalho gerado | Mesmo critério, sem assumir que texto de catálogo corresponde a bytes de autoria |
| Qualquer outro artefacto composto | Fixture que identifique separadamente fonte, conteúdo extraído e conteúdo projectado | Verificar proveniência, limites inequívocos da projecção e preservação do artefacto armazenado |

Os testes específicos de cada variante devem ser executáveis sem depender de uma base de dados PHC real; os dados reais servem para validar posteriormente se as fixtures representam fielmente os formatos recolhidos.

## 5. Testes específicos T-SQL — a completar após aprovação da matriz

Para cada um de `view`, `stored_procedure`, `trigger` e cada variante de `phc.user_tsql_function`, preparar fixtures reais e controladas que cubram:

- alteração de identificadores, operadores, literais, parâmetros, defaults e expressões;
- whitespace e quebras de linha em contextos onde a gramática permita provar equivalência;
- comentários comuns versus directivas/hints com impacto;
- strings, identificadores delimitados, SQL dinâmico e escaping;
- opções `SET` relevantes e propriedades de execução;
- triggers: eventos, estado disabled e `INSTEAD OF`, mantendo-os separados da definição textual quando pertencem à metadata;
- sintaxe/versões SQL Server abrangidas e sintaxe não suportada pelo parser.

Nenhuma transformação será considerada segura apenas porque os exemplos de teste passam. A política terá de documentar a gramática, as versões suportadas e os limites conhecidos.

A suite deve ainda declarar explicitamente que um fingerprint só demonstra igualdade segundo a política canónica, versão e âmbito suportados. Não prova equivalência absoluta de comportamento SQL, nem equivalência para sintaxe, opções de execução, contexto de base de dados ou funcionalidades fora do âmbito testado. Quando o normalizador não suportar ou não conseguir analisar com segurança um caso, o resultado é fingerprint indisponível — nunca uma transformação permissiva.

As fixtures T-SQL devem ser concretas e separadas por `ObjectKind` e variante de origem. Não basta uma lista abstracta de categorias: cada teste deve conter texto SQL de entrada, transformação aplicada, resultado esperado e justificação do que pode ou não ser normalizado.

## 6. Critérios para considerar a correcção concluída

- Todos os casos transversais aplicáveis passam.
- Cada kind/variante com fingerprint disponível tem `policy.id` e `policy.version` explícitos.
- Para cada definição `Available`, o estado do fingerprint é explícito; motivos de indisponibilidade usam apenas códigos do catálogo controlado.
- A propriedade `definitionFingerprint` é omitida para estados de definição `Empty`, `Unavailable` e `NotCollected`.
- Nenhum kind sem política validada emite `DefinitionFingerprint`.
- Hash de integridade continua a ser validado independentemente.
- Modelo, serialização, schema e validador concordam sobre a presença/ausência e a versão do fingerprint.
- Não existe requisito de compatibilidade para pacotes IKP de teste de iterações anteriores do Scanner explicitamente descartados; não se implementa preservação de fingerprints legados nem conversor para esses pacotes. A compatibilidade de outras versões só existe quando declarada na matriz correspondente.
- Testes de regressão demonstram que o fingerprint já não é igual, por construção, ao hash de integridade.
- Estados de definição vazia, indisponível, não recolhida e fingerprint indisponível são distinguíveis no resultado de teste.
- A documentação dos testes distingue sempre bytes do artefacto armazenado de bytes/texto de autoria na origem.
- Os testes T-SQL e de artefactos compostos têm fixtures concretas, identificadas por kind e variante.
- A suite limita expressamente a conclusão à igualdade segundo a política/versão e ao âmbito de sintaxe validado; não afirma equivalência absoluta de comportamento SQL.
- Testes são executados em CI e o resultado fica registado.

## 7. Estado desta revisão

A revisão técnica dos 18 cenários transversais identificou cinco melhorias de clareza e cobertura, agora incorporadas nesta proposta documental:

1. Distinguir bytes exactos do artefacto armazenado de bytes/texto originais da autoria.
2. Separar os estados de disponibilidade da definição dos motivos de indisponibilidade do fingerprint.
3. Não exigir leitura, preservação ou conversão dos pacotes IKP de teste descartados; não reinterpretar retroactivamente fingerprints antigos.
4. Explicitar os limites da normalização: igualdade segundo política validada, não prova de equivalência absoluta de comportamento SQL.
5. Numerar os testes de artefactos compostos e exigir fixtures concretas por `ObjectKind` e variante.

Esta revisão não transforma a suite em testes executados nem aprova uma política de normalização T-SQL. Os códigos `NoValidatedPolicy`, `UnsupportedSyntax` e `NormalizationFailed` foram entretanto aprovados para o contrato pela D-021; os restantes detalhes de fixtures e testes específicos continuam por fechar.

## 8. Decisões ainda necessárias

1. Completar as fixtures e os nomes finais dos testes executáveis por kind/variante.
2. Verificar a compatibilidade do envelope D-021 com o schema do object-index e consumidores existentes antes de alterar qualquer schema executável.
3. Aprovar a matriz de normalização por kind/variante e os limites de sintaxe suportados.
4. Só depois implementar projector, modelo, exporter/schema e validador em conjunto.


## 9. D-021 — testes da representação aprovada

A suite deve cobrir, adicionalmente:

| ID | Cenário | Resultado esperado |
|---|---|---|
| DF-019 | Definição disponível com política validada | Fingerprint `Available` com `algorithm`, `value`, `policy.id` e `policy.version` |
| DF-020 | Definição disponível sem política validada | Fingerprint `Unavailable` com `reasonCode=NoValidatedPolicy`; sem algoritmo, valor ou política |
| DF-021 | Sintaxe fora do âmbito suportado | Fingerprint `Unavailable` com `reasonCode=UnsupportedSyntax` |
| DF-022 | Falha segura durante normalização | Fingerprint `Unavailable` com `reasonCode=NormalizationFailed` |
| DF-023 | Definição `Empty`, `Unavailable` ou `NotCollected` | Omitir `definitionFingerprint`; preservar exactamente `definition.status` |
| DF-024 | Alteração da versão da política | Identidade de política/versão fica explícita; não comparar como equivalente sem compatibilidade/migração aprovada |
| DF-025 | Consumidor recebe `reasonCode` desconhecido | Preservar/tolerar o código; não o converter em estado de definição nem em erro de integridade |

## 10. Baseline de implementação D-022 — Object Index v2 e Analysis 0.3

Os seguintes cenários passam a fazer parte dos critérios de aceitação da implementação coordenada:

| ID | Cenário | Resultado esperado |
|---|---|---|
| DF-026 | Artefacto íntegro, definição Available, sem política de produção | Definição Available; fingerprint Unavailable/NoValidatedPolicy |
| DF-027 | Hash SHA-256 ou comprimento do artefacto inválido | Falha de integridade mantém-se independente; não emitir fingerprint semântico válido |
| DF-028 | Definição Available com política artificial de teste | Fingerprint Available com algorithm, value e policy id/version |
| DF-029 | MetadataFingerprint | Formato, cálculo e resultados existentes mantêm-se inalterados |
| DF-030 | Fingerprint indisponível e coverage/readiness | Não alterar coverage nem bloquear readiness apenas por essa indisponibilidade |
| DF-031 | Object Index schemaVersion 1 | Rejeitado pelo importador desta implementação |
| DF-032 | Object Index schemaVersion 2 | Aceite quando cumprir o contrato D-021 |
| DF-033 | PackageImportAnalysis schema 0.3 | Disponibilizar contagens separadas Available/Unavailable |
| DF-034 | Definição não Available | Omitir definitionFingerprint, mantendo o estado original da definição |

A primeira implementação não disponibiliza políticas de normalização semântica em produção. Os casos com fingerprint Available usam uma política artificial isolada nos testes. A aceitação destes cenários não autoriza nem implica normalização T-SQL em produção.

A versão global IKP permanece 0.1. A versão interna do Object Index é 2 e a versão da análise de importação é 0.3. Não existe migração para Object Index v1 nem compatibilidade com pacotes de teste antigos descartados.


## 11. Estado de implementação D-022 — 23-11-2026

Os cenários DF-026 a DF-034 foram traduzidos para alterações de código, schema, fixture e testes na PR #27 (`implementation/d022-definition-fingerprint-v2`). Foram acrescentados testes para integridade sem política, envelope D-021, reasonCode desconhecido, Object Index v1/v2, parsing pelo Importer e contagens da análise 0.3. Esta indicação significa apenas que os testes foram escritos; não significa que tenham sido executados ou aprovados. Builds, execução C#/Python e CI permanecem pendentes de confirmação antes do merge.
