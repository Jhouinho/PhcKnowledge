# P1.C — Testes de aceitação para DefinitionFingerprint

- **Data:** 23-11-2026
- **Estado:** proposta de testes; ainda não aprovada como suite final
- **Decisões normativas:** D-018, D-019 e D-020
- **Âmbito:** separação entre hash de integridade, DefinitionFingerprint semântico e comparação exacta
- **Restrição:** documento de desenho; não altera código, schemas executáveis, collectors, exporter, workspace SQLite, validator ou materialização SQL.

## 1. Objectivo

Garantir que o Scanner nunca apresenta um hash dos bytes integrais como fingerprint semântico, que preserva a verificação de integridade dos artefactos e que só emite `DefinitionFingerprint` quando existe uma política semântica específica, identificada, versionada e validada.

Os testes devem ser independentes do normalizador concreto. Os testes específicos de cada `objectKind` serão acrescentados quando a respectiva matriz estiver aprovada.

## 2. Invariantes obrigatórios

1. O hash de integridade é calculado sobre os bytes exactos do artefacto armazenado.
2. O hash de integridade não é reutilizado como `DefinitionFingerprint`.
3. Sem política de normalização semântica validada para o kind/variante, `DefinitionFingerprint` fica indisponível.
4. A comparação exacta, se disponibilizada, é identificada como modalidade separada e não preenche o campo de fingerprint semântico.
5. Um fingerprint semântico inclui a identificação/versionamento da política que o produziu.
6. Fingerprints de políticas diferentes não são comparados como equivalentes sem regra explícita de migração/compatibilidade.
7. O original e os seus bytes nunca são substituídos pela representação canónica.
8. Estados de definição vazia, indisponível, não recolhida e fingerprint indisponível por falta de política não podem ser confundidos.
9. A leitura de pacotes legados mantém compatibilidade, sem atribuir retroactivamente semântica D-019/D-020 a valores antigos.

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
| DF-014 | Artefacto original preservado e representação canónica produzida | Bytes originais permanecem idênticos; representação canónica é derivada e reproduzível |
| DF-015 | Metadata muda sem alteração da definição | `MetadataFingerprint` pode mudar; `DefinitionFingerprint` mantém-se |
| DF-016 | Definição muda sem alteração da metadata projectada | `DefinitionFingerprint` pode mudar; `MetadataFingerprint` mantém-se |
| DF-017 | Pacote legado contém fingerprint produzido pela regra antiga | Pacote continua legível; valor legado não é apresentado como fingerprint semântico conforme a D-019/D-020 |
| DF-018 | Campo fingerprint ausente por falta de política | Ausência não é interpretada como definição vazia, erro de integridade ou prova de diferença semântica |

## 4. Casos específicos de artefactos compostos

Aplicáveis a `phc.user_tsql_function` e a qualquer outro kind cujo artefacto inclua cabeçalhos/metadados gerados:

1. Alterar apenas o cabeçalho gerado altera o hash de integridade do artefacto composto.
2. Se existir uma projecção validada que isole o componente de definição, alterar apenas o cabeçalho não altera o fingerprint semântico desse componente.
3. Alterar o código extraído altera o fingerprint semântico quando a política específica está validada.
4. Se a fronteira entre cabeçalho e conteúdo não puder ser determinada de forma inequívoca, o fingerprint semântico fica indisponível.
5. O teste não pode remover cabeçalhos por heurísticas frágeis, regex genérica ou procura de delimitadores não garantidos pelo formato.

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

## 6. Critérios para considerar a correcção concluída

- Todos os casos transversais aplicáveis passam.
- Cada kind com fingerprint disponível tem política e versão explícitas.
- Nenhum kind sem política validada emite `DefinitionFingerprint`.
- Hash de integridade continua a ser validado independentemente.
- Modelo, serialização, schema e validador concordam sobre a presença/ausência e a versão do fingerprint.
- Pacotes antigos continuam legíveis sem reclassificação silenciosa.
- Testes de regressão demonstram que o fingerprint já não é igual, por construção, ao hash de integridade.
- Testes são executados em CI e o resultado fica registado.

## 7. Decisões ainda necessárias

1. Aprovar esta suite transversal e definir fixtures/nomes finais dos testes.
2. Fechar a representação no contrato IKP para política/versão e ausência de fingerprint.
3. Aprovar a matriz de normalização por kind/variante.
4. Só depois implementar projector, modelo, exporter/schema e validador em conjunto.

