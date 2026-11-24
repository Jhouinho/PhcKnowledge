# Decisões arquitecturais aprovadas — PHC Knowledge

- **Data de consolidação:** 21-11-2026
- **Repositório:** `Jhouinho/PhcKnowledge`
- **Estado:** decisões aprovadas pelo responsável do projecto
- **Natureza:** referência normativa actual para produto, domínio, captura, transporte e armazenamento
- **Implementação:** estas decisões orientam a documentação e o trabalho futuro; não significam que todas as capacidades já estejam implementadas

Este documento é a fonte de verdade das decisões aprovadas até esta data. Em caso de contradição, prevalece sobre propostas, modelos conceptuais e relatórios anteriores. Os documentos históricos devem ser preservados, mas identificados como históricos ou parcialmente substituídos.

## 1. Direcção de produto e organização

1. O PHC Knowledge é uma ferramenta interna da empresa que utiliza a plataforma, reservada aos seus colaboradores e com contas individuais.
2. Existe uma única organização no âmbito da plataforma: a empresa utilizadora. Não existe modelo SaaS multi-tenant nem várias organizações clientes.
3. Cada cliente PHC tem um registo próprio no Knowledge. O cliente é contexto técnico e fonte de conhecimento, não tenant nem organização de autenticação.
4. Cada base de dados PHC registada corresponde a uma instalação própria e tem o seu próprio `InstallationId`.
5. Ambiente e localização são inicialmente atributos da instalação; não exigem entidades autónomas nesta fase. Alterações relevantes podem ter histórico.
6. Uma instalação pode ter várias capturas; cada captura pertence exclusivamente a uma instalação.
7. Uma cópia de uma base de dados recebe uma nova identidade de instalação, mantendo uma relação de linhagem com a origem.
8. Uma mudança de servidor não cria necessariamente uma nova instalação, desde que seja possível confirmar a continuidade da mesma base de dados.
9. Comparações históricas respeitam a identidade da instalação e a cobertura efectiva das capturas.
10. A relação principal é: Cliente 1 → N Instalações 1 → N Capturas.
11. Capturas originam observações, artefactos, relações e histórico técnico, todos com proveniência.
12. Entidades centrais conceptuais: Cliente, Instalação, Captura, Objecto técnico/Observação, Artefacto e Proveniência. A Organização é contexto global da plataforma, não uma entidade de segregação por cliente.

## 2. Stack e armazenamento central

1. A base de dados central é **Microsoft SQL Server**.
2. PostgreSQL está excluído.
3. A edição e versão concretas do SQL Server continuam por decidir; não assumir SQL Server Express.
4. O armazenamento permanente de dados estruturados, metadados, histórico e artefactos originais é feito no SQL Server.
5. A decisão de usar SQL Server para os artefactos binários não aprova, por si só, um tipo de coluna, estratégia de particionamento ou dimensão máxima; esses detalhes serão especificados tecnicamente.
6. SQLite fica limitado ao workspace/cache local do Scanner.
7. O Scanner é autónomo e não depende do Knowledge em runtime. A transferência do pacote é manual através do Portal.

## 3. D-015 — PHC Standard e relações REL/DTL

**Estado: APROVADA.**

1. O PHC Standard é mantido no Knowledge num catálogo central, versionado e associado à edição/versão PHC aplicável.
2. O Scanner continua a recolher as relações declaradas REL/DTL existentes na instalação, incluindo a evidência original e a expressão de origem disponível.
3. REL/DTL são factos declarados pela instalação; não são reduzidos a chaves estrangeiras inferidas.
4. O Knowledge compara a evidência recolhida com a referência PHC Standard aplicável.
5. Correspondências, alterações e relações sem correspondência são classificadas com base em evidência. Uma ausência de correspondência não permite classificar automaticamente um elemento como personalizado ou standard.
6. A recolha REL/DTL não será removida antes de existir catálogo versionado e comparação suficientemente validados para preservar a detecção de desvios.
7. Factos originais e interpretações derivadas permanecem separados e rastreáveis.

## 4. D-016 — Política de captura e materialização

**Estado: APROVADA.**

1. O Scanner produz uma captura completa dentro do âmbito definido para cada operação/capability incluída na execução.
2. «Completa» não significa copiar indiscriminadamente toda a base de dados PHC ou todos os dados standard; significa executar integralmente o âmbito declarado e reportar a cobertura real.
3. O Scanner não consulta o histórico central para decidir o que recolher. Cada pacote é autónomo relativamente aos dados e evidências abrangidos pela captura.
4. O pacote transporta os dados recolhidos, originais disponíveis, metadados, identidade, proveniência, fingerprints, cobertura, avisos, erros e factos REL/DTL aplicáveis.
5. O Knowledge compara a captura com o histórico da mesma instalação e com o catálogo PHC Standard, materializando incrementalmente novas observações e versões.
6. Conteúdo inalterado pode ser deduplicado no armazenamento central, sem perder a proveniência das capturas em que foi observado.
7. A ausência de um objecto só pode originar uma conclusão de remoção quando a operação correspondente foi executada com sucesso e a cobertura daquele âmbito é comprovadamente completa.
8. Operações falhadas, não executadas, indisponíveis ou com cobertura parcial não permitem inferir remoções. A ausência de evidência não é evidência de ausência.
9. A captura e a sua cobertura são preservadas como histórico, mesmo quando o conteúdo idêntico é deduplicado.

## 5. ZIP de captura — apenas transporte temporário

**Estado: APROVADO.**

1. O ZIP é exclusivamente um meio de transporte entre o Scanner e o Portal/Knowledge.
2. O ZIP não é o arquivo permanente do conhecimento nem a forma de recuperação histórica da plataforma.
3. Após recepção, o Knowledge valida, analisa e prepara a materialização.
4. A informação estruturada, os metadados, o histórico e os artefactos originais necessários são persistidos no SQL Server.
5. O ZIP só pode ser eliminado depois de a materialização estar concluída e verificada.
6. A verificação tem de confirmar a persistência de todos os conteúdos esperados, integridade por hashes, referências, contagens, proveniência e capacidade de consulta/recuperação sem depender do ZIP.
7. Se a análise ou materialização falhar, ficar incompleta ou não puder ser verificada, o ZIP permanece disponível para retry e recuperação.
8. Depois da verificação bem-sucedida, o ZIP e os ficheiros temporários associados são eliminados.
9. A retenção actual do arquivo de recepção é transitória: não deve ser confundida com a arquitectura final. Não foi aprovada uma retenção fixa para pacotes rejeitados.

## 6. Separação de responsabilidades

| Componente | Responsabilidade |
|---|---|
| Scanner | Recolher o estado abrangido, preservar evidência, declarar cobertura e produzir pacote autónomo |
| IKP/ZIP | Transportar identidade, conteúdo, metadados, proveniência, fingerprints e cobertura |
| Knowledge | Validar, comparar, classificar, materializar, deduplicar e preservar histórico |
| SQL Server central | Armazenar permanentemente conhecimento estruturado, metadados, artefactos e histórico |

## 7. Estado de implementação e próximos passos

As decisões acima são aprovadas, mas a sua implementação não deve ser presumida.

Estado conhecido na data:
- Scanner e exportador IKP 0.1 existem.
- Recepção, validação, receipts e recuperação de pacotes existem.
- Análise IKP A+B está integrada na main.
- Materialização integral de observações, relações e artefactos no modelo de conhecimento ainda está por construir.
- O arquivo permanente actual do ZIP é uma solução transitória e não cumpre o ciclo de vida final aprovado.
- A edição/versão SQL Server continua por escolher.
- A matriz detalhada de classificação dos collectors ainda tem de ser produzida e validada com a captura real existente.

Próxima actividade: construir a matriz por collector/ObjectKind, identificando origem, conteúdo original/derivado, classificação (código, configuração, estrutura, metadado, diagnóstico ou relação), completude, fingerprint, proveniência e destino de materialização. Não alterar collectors nem workspace SQLite antes desta validação.

## 8. Governação documental

A cada decisão aprovada:
1. actualizar esta referência canónica;
2. actualizar todos os documentos activos afectados;
3. corrigir ou marcar explicitamente conteúdo contraditório como histórico/supersedido;
4. actualizar README, PROJECT_GUIDELINES e roadmap quando a orientação geral ou estado mudar;
5. preservar relatórios e artefactos históricos sem os reescrever retroactivamente;
6. verificar coerência e links entre documentos;
7. consolidar as decisões aprovadas e os documentos activos afectados directamente na main, após autorização explícita do responsável; branches temporárias podem ser usadas para análise técnica, mas não devem criar uma fonte documental concorrente.

Não declarar builds, testes, materialização ou validações que não tenham sido efectivamente executados.


## 9. D-017 — Contrato de cobertura IKP e ausência segura

**Data: 22-11-2026 — Estado: APROVADA.**

A cobertura deve descrever separadamente o que foi lido na origem, o que foi emitido pelo Scanner, o que foi filtrado e o que falhou durante o processamento. O estado global de uma operação ou um contador de objetos emitidos não substitui estas evidências.

### 9.1 Métricas obrigatórias por operação

| Métrica | Semântica normativa |
|---|---|
| `SourceRowsRead` | Número de registos efetivamente lidos na fonte dentro do âmbito declarado. |
| `ObjectsEmitted` | Número de objetos técnicos emitidos pelo Scanner para o pacote/Object Index. Não é uma contagem de linhas da origem. |
| `RowsFiltered` | Número de registos lidos que foram excluídos por filtros declarados. |
| `RowsFailed` | Número de registos lidos cujo processamento falhou. Falhas de consulta sem linha identificável são registadas no resultado/issues, não inventadas como linhas falhadas. |

As métricas têm unidades e semânticas diferentes. Não se impõe uma igualdade aritmética entre `SourceRowsRead`, `ObjectsEmitted`, `RowsFiltered` e `RowsFailed`: um registo pode originar zero, um ou vários objetos técnicos. Contagens desconhecidas permanecem explicitamente desconhecidas; não são preenchidas com zero.

### 9.2 Completude da origem

Cada operação declara `SourceCompleteness`:

- `Complete`: há evidência de que todo o âmbito declarado foi percorrido;
- `Partial`: há evidência de que apenas parte do âmbito foi percorrida;
- `Unknown`: não existem evidências suficientes para determinar a completude.

A completude refere-se sempre ao âmbito e filtros declarados, não à totalidade indiscriminada da base de dados. Deve ser independente do estado de resultado da operação, da disponibilidade da estrutura e da disponibilidade do conteúdo.

### 9.3 Ausência e remoção seguras

O Knowledge só pode concluir que um objeto foi removido quando, cumulativamente:

1. A operação correspondente foi suportada, solicitada e executada;
2. A fonte foi identificada e a sua disponibilidade/visibilidade foi confirmada;
3. A operação terminou sem falhas que afectem o âmbito relevante;
4. `SourceCompleteness=Complete` para o âmbito declarado;
5. O âmbito e a semântica dos filtros são conhecidos e comparáveis com a observação anterior;
6. A identidade e a cobertura da observação anterior permitem uma comparação válida.

Um registo contado em `RowsFiltered` não constitui, por si só, prova de que o objeto correspondente não existe. Quando um filtro exclui registos que contêm identidade de objeto, esses registos não podem ser tratados como removidos; o âmbito/filtro deve ser representado e a conclusão limitada ao universo que efetivamente foi inventariado.

Se qualquer condição não for demonstrada, o resultado é inconclusivo/NotObserved. Nunca se converte falta de evidência em `Removed`.

### 9.4 Compatibilidade

A evolução do contrato de cobertura é versionada separadamente da versão principal do pacote IKP. O IKP 0.1 mantém-se; o schema de cobertura evolui de versão 1 para versão 2.

A compatibilidade de leitura do coverage schema v1 é uma regra específica de versionamento desse schema e não constitui uma obrigação geral de suportar qualquer pacote antigo. Quando um pacote v1 é aceite no âmbito da matriz de compatibilidade, o campo legado `observedCount` não é reinterpretado automaticamente como `SourceRowsRead`, e um resultado `Empty` ou `observedCount=0` não prova, isoladamente, que a origem estava vazia ou que a consulta foi completa. Quando não houver evidência equivalente explícita, `SourceCompleteness` é tratado como `Unknown` para decisões de ausência. Esta regra não obriga a manter compatibilidade com pacotes IKP de teste explicitamente descartados.

### 9.5 Testes de aceitação obrigatórios antes da implementação ser considerada concluída

- Origem realmente vazia, consultada integralmente e visível;
- Origem preenchida com todos os registos filtrados;
- Leitura parcial/interrompida;
- Falha de permissões/visibilidade;
- Operação não solicitada ou não executada;
- Falha de processamento de registos;
- Demonstração de que nenhum cenário sem evidência suficiente gera falsos `Removed`;
- Leitura retrocompatível de pacotes de cobertura v1 sem atribuir garantias não existentes.

Esta decisão aprova o contrato e os critérios de aceitação. Não autoriza, por si só, alterações ao código do Scanner, ao exportador IKP, ao workspace SQLite ou à materialização SQL. Essas alterações só serão planeadas depois de os contratos relacionados serem fechados e a documentação consolidada.


## 10. D-018 — Contrato de classificação e proveniência de artefactos

**Data: 23-11-2026 — Estado: APROVADA.**

O PHC Knowledge deve distinguir explicitamente a origem do conteúdo, a sua natureza semântica e a proveniência. Estas dimensões não podem ser condensadas num único campo nem inferidas apenas pelo tipo de objeto ou pelo nome do ficheiro.

### 10.1 Origem do conteúdo

- `source-content`: conteúdo obtido diretamente da fonte, preservado sem transformação semântica. Quando a representação exportada não for byte-a-byte igual à fonte, essa limitação tem de ser declarada; não pode ser apresentado como cópia exata.
- `derived-projection`: conteúdo extraído, normalizado, formatado, combinado ou transformado a partir de uma ou mais fontes.
- `generated-description`: descrição produzida pelo Scanner, incluindo descrições construídas a partir de metadados ou estrutura.
- `generated-diagnostic`: aviso, erro, diagnóstico ou relatório produzido pelo processo de recolha.

### 10.2 Natureza semântica

Independentemente da origem, cada artefacto deve declarar a natureza aplicável: código, configuração, estrutura SQL, metadados, descrição, relação ou diagnóstico. A origem não determina, por si só, a natureza: uma descrição pode ser conteúdo original da fonte ou uma descrição gerada.

### 10.3 Proveniência

Quando aplicável, cada artefacto deve identificar a fonte/tabela, campo ou propriedade original, objeto associado, formato e codificação, transformação aplicada e respetiva versão, hash de integridade e relação com o artefacto de origem. Campos não aplicáveis ou desconhecidos não devem ser preenchidos com valores inventados.

### 10.4 Associação a objetos e definição técnica

A referência `definition.artifactIds` fica reservada a artefactos que representem efetivamente uma definição técnica do objeto. Artefactos descritivos, estruturais, diagnósticos e outras evidências podem ser associados ao objeto por uma relação genérica própria, sem serem promovidos a definição.

Metadados disponíveis não implicam definição original disponível. A ausência de definição recolhida deve manter o estado correspondente como não recolhido/desconhecido, conforme a evidência; nunca deve ser inferida a partir de uma descrição.

### 10.5 Integridade e fingerprints

- O conteúdo original disponível deve ser preservado sem substituição por projeções.
- Uma projeção ou descrição nunca substitui a evidência original.
- O conteúdo original disponível é preservado sem substituição por projeções. O SHA-256 dos bytes exatos do artefacto original é o seu hash de integridade e muda perante qualquer diferença de bytes, incluindo encoding, espaços ou quebras de linha.
- O DefinitionFingerprint representa a definição técnica em termos semânticos: é calculado por SHA-256 sobre uma representação canónica cuja normalização é específica do objectKind, determinística, identificada e versionada.
- A normalização só pode ignorar diferenças de apresentação quando houver garantia documentada de que não altera o significado. Não existe normalizador universal para todo o código/conteúdo PHC.
- Quando não houver normalização segura e validada para um objectKind, a comparação é conservadora: usa-se a representação original sem remover diferenças, ou declara-se o fingerprint semântico indisponível até existir política validada. Nunca se presume equivalência semântica.
- Hashes de integridade de artefactos e hashes de projeções derivadas permanecem distintos do DefinitionFingerprint.

### 10.6 Compatibilidade e âmbito da aprovação

A leitura de pacotes de versões anteriores não é uma garantia genérica do produto: só são aceites versões/capacidades incluídas numa matriz explícita de compatibilidade. Esta regra não cria obrigação de suportar pacotes IKP de teste de iterações antigas do Scanner que tenham sido descartados; em particular, não se exigem adaptadores nem preservação de fingerprints desses pacotes. Não se atribuem retroativamente classificações ou garantias de proveniência.

D-018 aprova a semântica e os princípios deste contrato. Não autoriza, por si só, alterações aos collectors, exporter, workspace SQLite, schemas executáveis, validator ou materialização SQL. A matriz por collector/ObjectKind e os testes de aceitação devem ser preparados antes da implementação.


## 11. D-019 — Fingerprint semântico da definição técnica

**Data: 23-11-2026 — Estado: APROVADA.**

O PHC Knowledge deve distinguir alterações semânticas à definição técnica de diferenças meramente de apresentação, sem perder a evidência original.

1. O artefacto original é preservado integralmente. O seu SHA-256 sobre os bytes exatos funciona como hash de integridade e detecta qualquer alteração de bytes.
2. O DefinitionFingerprint passa a representar a definição técnica semântica, calculada por SHA-256 sobre uma representação canónica normalizada.
3. A normalização é específica por objectKind, determinística, identificada e versionada. Só pode remover diferenças de apresentação quando for demonstrável que não altera o significado.
4. Não se aplica normalização universal nem se removem espaços, comentários, casing ou quebras de linha indiscriminadamente.
5. Quando não existir normalizador seguro e validado, a comparação é conservadora: não se declara equivalência semântica. Pode usar-se a representação original como comparação exacta, identificando esse modo, ou deixar o fingerprint semântico indisponível até existir política validada.
6. O conteúdo original nunca é substituído pela representação canónica. A normalização é uma projeção de comparação, reproduzível a partir do original.
7. A versão da política/normalizador tem de ser conhecida para interpretar e comparar fingerprints. Mudanças de versão do normalizador exigem tratamento explícito; não se comparam valores produzidos por políticas diferentes como se fossem equivalentes.
8. MetadataFingerprint mantém o contrato independente já aprovado: hash dos metadados semanticamente relevantes após normalização por categoria e canonicalização JSON.
9. Esta decisão altera a semântica normativa de DefinitionFingerprint anteriormente descrita como hash direto do conteúdo original. O hash direto mantém-se como hash de integridade do artefacto, não como substituto do fingerprint semântico.
10. A aprovação fecha o princípio e a separação conceptual. Não autoriza alterações aos collectors, exporter, workspace SQLite, schemas executáveis, validator ou materialização SQL. Antes da implementação, é obrigatória uma matriz por objectKind com regras de normalização e testes de aceitação, incluindo casos em que a normalização não é segura.


## 12. D-020 — Fingerprint semântico indisponível sem normalizador validado

**Data: 23-11-2026 — Estado: APROVADA.**

Quando não existir uma política de normalização semântica segura, específica para o `objectKind` e validada pelos respectivos testes de aceitação, o Scanner não emite um **valor de fingerprint semântico** para essa definição. A representação explícita da indisponibilidade (`status: Unavailable` e `reasonCode`) segue a D-021.

Regras:

1. A ausência de normalizador validado significa `DefinitionFingerprint` indisponível; não se calcula nem se publica o SHA-256 dos bytes originais como substituto.
2. O SHA-256 dos bytes exactos do artefacto mantém-se como hash de integridade, independente do fingerprint semântico.
3. A comparação byte-a-byte pode ser usada como operação/modo de comparação exacta separado, mas nunca é apresentada, serializada ou interpretada como `DefinitionFingerprint` semântico.
4. A comparação exacta não permite concluir equivalência semântica. Diferenças de encoding, espaços, comentários ou quebras de linha podem produzir resultado diferente, mesmo quando o comportamento técnico não muda.
5. A disponibilidade de `DefinitionFingerprint` exige uma política identificada e versionada, aplicada à representação canónica específica do `objectKind`/variante e coberta por testes de aceitação aprovados.
6. A ausência de valor de fingerprint semântico por falta de normalizador não é tratada como definição vazia, hash vazio, erro de integridade ou prova de que duas definições são diferentes; representa ausência de comparação semântica validada. A indisponibilidade é transportada pelo envelope definido em D-021.
7. Não existe obrigação de compatibilidade com pacotes IKP de teste produzidos por iterações anteriores do Scanner que tenham sido explicitamente descartados. Esses pacotes não são uma entrada suportada: não se exige que continuem legíveis, que os seus fingerprints antigos sejam preservados como legado, nem que exista um adaptador/conversor. O trabalho parte de novas capturas produzidas pelo Scanner actualizado. Esta exclusão não apaga código, documentação, histórico Git ou testes úteis do repositório.
8. A verificação do hash de integridade e do comprimento do artefacto mantém-se obrigatória e independente do cálculo do fingerprint.

Esta decisão fecha a regra de fallback. Não aprova ainda qualquer normalizador T-SQL, não define versões de política por `objectKind` e não autoriza alterações aos collectors, exporter, workspace SQLite, schemas executáveis, validator ou materialização SQL. A matriz por `objectKind` e os testes de aceitação continuam obrigatórios antes da implementação.

## 13. D-021 — Estado, política e motivos do DefinitionFingerprint

Aprovado em 23-11-2026.

1. O estado do `DefinitionFingerprint` é independente do estado de disponibilidade da definição. Para uma definição `Available`, o envelope `definitionFingerprint` é representado explicitamente com estado `Available` ou `Unavailable`; neste último caso não contém valor de fingerprint semântico.
2. Um fingerprint disponível contém `algorithm`, `value` e `policy.id`/`policy.version`. A identificação da política é específica do `objectKind`/variante; a versão identifica exactamente as regras de normalização aplicadas.
3. A representação de um fingerprint indisponível contém `status: Unavailable` e um `reasonCode` de catálogo controlado, sem algoritmo, valor ou política aplicada.
4. Para definições `Empty`, `Unavailable` ou `NotCollected`, a propriedade `definitionFingerprint` é omitida. Estes estados pertencem à disponibilidade da definição e não são motivos de indisponibilidade do fingerprint.
5. O catálogo inicial de motivos é: `NoValidatedPolicy`, `UnsupportedSyntax` e `NormalizationFailed`. Os códigos são valores contratuais, não texto livre. Consumidores devem tolerar códigos desconhecidos sem os reinterpretar.
6. A versão do Scanner e a versão da capability não substituem a identificação/versionamento da política.
7. A incompatibilidade entre versões de política é tratada na comparação/migração; não altera nem reescreve o fingerprint originalmente produzido.
8. A decisão fecha a representação documental, mas não autoriza alterações aos schemas executáveis, modelos, serialização, exporter, validator, collectors, workspace SQLite ou materialização SQL. A compatibilidade com consumidores existentes e a versão do schema do object-index têm de ser verificadas antes da implementação.

## 14. D-022 — Baseline técnico para implementação do DefinitionFingerprint

**Data: 23-11-2026 — Estado: APROVADA.**  
**Âmbito:** decisão de desenho técnico e sequência de implementação. O estado efectivo da implementação é registado na secção 14.7.

A D-022 fecha a forma como D-019, D-020 e D-021 serão concretizadas no Scanner e no importador IKP. Mantêm-se integralmente as decisões anteriores; esta decisão especifica a sua tradução técnica.

### 14.1 Separação de responsabilidades

A implementação deve manter quatro dimensões independentes:

1. **Integridade do artefacto:** validar comprimento e SHA-256 dos bytes exactos. Este digest nunca alimenta o DefinitionFingerprint.
2. **Disponibilidade da definição:** preservar Available, Empty, Unavailable e NotCollected, de acordo com a evidência do artefacto.
3. **Fingerprint semântico:** calcular apenas através de política validada, específica por objectKind/variante, identificada e versionada; sem política, fica indisponível.
4. **MetadataFingerprint:** permanece independente e sem alteração de semântica ou representação nesta fase.

A indisponibilidade do fingerprint semântico não torna a definição indisponível, não altera a cobertura da operação e não bloqueia, por si só, a análise ou preparação para materialização.

### 14.2 Representação D-021 no Object Index

Para definition.status=Available, a propriedade definitionFingerprint é obrigatória e assume exactamente uma das formas:

Disponível:
```json
{
  "status": "Available",
  "algorithm": "sha256",
  "value": "<digest>",
  "policy": { "id": "<policy-id>", "version": "<policy-version>" }
}
```

Indisponível:
```json
{
  "status": "Unavailable",
  "reasonCode": "NoValidatedPolicy"
}
```

O estado indisponível não contém algorithm, value ou policy. Para definições Empty, Unavailable ou NotCollected, a propriedade é omitida. Os códigos emitidos inicialmente são NoValidatedPolicy, UnsupportedSyntax e NormalizationFailed; consumidores preservam/toleram códigos desconhecidos sem reinterpretar a definição.

### 14.3 Versionamento dos contratos

1. O IKP global mantém-se em 0.1.
2. O object-index.json evolui de schemaVersion=1 para schemaVersion=2, porque a representação de definitionFingerprint é estruturalmente incompatível.
3. O importador desta implementação aceita Object Index v2. Object Index v1 não é aceite por esta via; não se cria conversão ou compatibilidade para pacotes de teste antigos descartados.
4. MetadataFingerprint mantém o formato e a semântica existentes. O schema genérico de fingerprint fica explicitamente associado a metadata; é criado schema próprio para DefinitionFingerprint quando os schemas executáveis forem alterados na fase de implementação.
5. PackageImportAnalysis evolui de schema 0.2 para 0.3, substituindo a contagem ambígua por DefinitionFingerprintAvailableCount e DefinitionFingerprintUnavailableCount. A análise guarda contagens/resumo; o objecto inspeccionado mantém o estado e os detalhes completos do fingerprint.
6. A mudança do schema do Object Index não altera a versão global IKP nem a versão do coverage schema.

### 14.4 Política semântica inicial

A primeira implementação não activa políticas de normalização semântica em produção. Para artefactos íntegros cuja definição está disponível, o Scanner emite definition.status=Available e definitionFingerprint.status=Unavailable, com reasonCode=NoValidatedPolicy.

Políticas artificiais podem ser usadas exclusivamente em testes para validar o caminho Available; não podem ser registadas como políticas de produção. Não implementar parser/normalizador T-SQL, normalização genérica por trim/remoção de comentários, heurísticas de cabeçalhos PHC, comparação byte-a-byte como fingerprint ou suporte a múltiplos artefactos por definição nesta fase.

### 14.5 Componentes e invariantes de implementação

- DefinitionProjectionResolver: separar verificação de integridade da resolução do fingerprint semântico.
- ObjectIndexProjector: emitir o envelope D-021 e Object Index v2.
- ObjectIndexContractValidator: validar as duas formas do fingerprint e a sua relação com definition.status.
- IkpPackageExporter: manter a cobertura baseada na disponibilidade/conclusão da definição, nunca na disponibilidade do fingerprint.
- IkpPackageInspectionEngine e IkpInspectedObject: ler v2 e transportar o estado, reasonCode ou algorithm/value/policy.
- IkpPackageAnalysisBuilder: produzir análise 0.3 e contagens distintas.
- MaterializationReadinessEvaluator: não bloquear por fingerprint indisponível quando a definição está disponível e os restantes critérios estão satisfeitos.
- Validação Python, fixtures e CI: usar o mesmo schema/contrato v2.

O MetadataFingerprint e os percursos de recolha/SQLite que não sejam necessários para esta separação permanecem inalterados.

### 14.6 Testes e sequência

Os testes devem demonstrar: integridade correcta sem política → definição Available/fingerprint Unavailable; hash ou comprimento incorrecto → definição indisponível conforme a regra de integridade; definição Empty → sem propriedade fingerprint; política de teste → fingerprint Available com política/versionamento; sintaxe não suportada e falha de normalização → reasonCodes próprios; reasonCode desconhecido aceite; MetadataFingerprint inalterado; cobertura e readiness independentes do fingerprint; Object Index v1 rejeitado e v2 aceite; análise 0.3 com contagens distintas.

Sequência aprovada:
1. Contrato documental e schemas/fixtures executáveis em alteração coordenada;
2. Scanner e projector;
3. Exporter e reconciliação de cobertura;
4. Importer e validação;
5. Análise 0.3 e readiness;
6. Fixtures, testes automatizados, validação Python e CI;
7. revisão integrada do diff e dos resultados de execução.

**Limites desta aprovação:** D-022 aprova o baseline técnico e a sequência, não autoriza por si só a implementação de código ou a alteração dos schemas executáveis. Não autoriza alterações a collectors, workspace SQLite, materialização SQL, suporte a múltiplos artefactos, nem políticas semânticas T-SQL em produção. Builds, testes, validação runtime e SQL Server só podem ser declarados depois de efectivamente executados.

### 14.7 Estado da implementação — 23-11-2026

A implementação aprovada foi concluída e integrada na `main` através da PR #27, merge commit `ad42f53fc3d4f7b654d61dea56f0f501af0905b8`.

- Scanner: integridade do artefacto separada do fingerprint semântico; sem política validada, definição disponível com fingerprint `Unavailable/NoValidatedPolicy`.
- Object Index: schema v2 e envelope D-021.
- Importer: leitura do v2 e preservação do estado/detalhes do fingerprint.
- PackageImportAnalysis: schema 0.3, com contagens Available e Unavailable separadas.
- Schemas, fixture, checksums, testes Python/.NET e CI actualizados.
- MetadataFingerprint preservado sem alteração semântica.
- Sem políticas semânticas de produção; sem alterações a collectors, workspace SQLite ou materialização SQL; sem suporte a múltiplos artefactos ou normalizador T-SQL de produção.

**Validação CI:** passaram as execuções de contrato IKP, suites .NET do Scanner e Knowledge Import, integração SQL Server 2022 e validação/build do repositório. A suite dedicada do Scanner reportou 291 testes aprovados e 2 ignorados. Estes resultados confirmam os pipelines executados, não substituem a validação funcional em instalações PHC reais.

A integração não altera os limites de âmbito aprovados em D-022. Qualquer expansão futura — em particular políticas semânticas de produção, normalização T-SQL, múltiplos artefactos ou alterações à recolha/SQLite/materialização — exige decisão própria.



## 14. D-023 — Identidade e correspondência cross-installation

**Data: 24-11-2026 — Estado: APROVADA.**

O contrato normativo completo está em [D-023 — Identidade e correspondência cross-installation](d-023-cross-installation-identity-correspondence-contract.md).

1. Cada instalação mantém as suas próprias `EntityIdentity`. Identidades de instalações diferentes nunca são fundidas, mesmo quando stamps, nomes, definições, estruturas ou origem histórica aparentam coincidir.
2. O PHC Standard tem identidades de referência próprias num catálogo central, curado e versionado. Uma referência standard não substitui a identidade local do objecto observado.
3. A correspondência é uma relação autónoma entre identidades, com evidências e histórico auditável. Não é merge e não implica igualdade de definições.
4. A cardinalidade não é presumida como universalmente 1:1. Os tipos conceptuais incluem `SameStandardEntity`, `SameLogicalEntity`, `RenamedFrom/To`, `DerivedFrom`, `Replaces` e `RelatedTo`, sujeitos a validação por `objectKind`.
5. Correspondências têm estados `Candidate`, `Confirmed`, `Rejected` e `Revoked`. Decisões conservam evidências, método, motivo/responsável quando humano, data e versões de política/catálogo. Não se introduzem scores numéricos sem calibração validada.
6. Identidade, correspondência, comparação de definição e classificação são dimensões separadas. As classificações conceptuais são `StandardIntact`, `StandardModified`, `InstallationDevelopment` e `Unknown`; ausência no catálogo só sustenta desenvolvimento quando versão e cobertura forem suficientes.
7. A correspondência com PHC Standard não autoriza automaticamente reutilização, publicação ou exposição de conteúdo de uma instalação. Proveniência, governação e permissões mantêm-se obrigatórias; o futuro RAG deve respeitar estes limites.
8. Antes de qualquer implementação, é obrigatória uma matriz de reconhecimento por `objectKind` e validação com capturas reais CS default e dfblock, sem alterar os pacotes originais.
9. D-023 aprova o contrato conceptual, mas não autoriza alterações ao Scanner, collectors, SQLite, schemas executáveis IKP, migrations/materialização SQL Server ou RAG. Essas alterações exigem proposta técnica e aprovação posterior.

D-023 complementa D-015 a D-022 e prevalece em caso de conflito sobre identidade e correspondência entre instalações.
