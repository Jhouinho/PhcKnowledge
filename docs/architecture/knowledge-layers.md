# Camadas de conhecimento e proveniência

Fonte consolidada a partir de [`../sources/analisar-base-phc-2026-11.txt`](../sources/analisar-base-phc-2026-11.txt) e dos ADRs do Scanner em `PhcKnowledgeScanner/docs/decisions/`.  
Estado: princípios de domínio; não confundir com modelo físico já implementado. Para identidade/proveniência, prevalecem `p1-identity-provenance-contract.md` e `ikp-0.1-specification.md`.

## Princípio

Não misturar camadas. Cada afirmação deve indicar se é facto recolhido, interpretação automática, projeção consultável, anotação humana ou conhecimento generalizado.

## Camadas

| Camada | Conteúdo | Regra |
| --- | --- | --- |
| Factos | Objetos SQL, artefactos de personalização, REL/DTL, PK/FK | Só o que foi recolhido; preservar original |
| Interpretação automática | Parser de expressões, resoluções de referências | Determinística; com diagnósticos e confiança |
| Projeção consultável | `KnowledgeRelation`, `FieldMapping`, vistas derivadas | Não apaga factos; regenerável |
| Conhecimento humano | Anotações, validações, ligações sugeridas | Não altera factos; sobrevive a novos scans |
| Histórico de scans | Snapshots imutáveis por recolha | Diff entre versões; sem wipe cego |
| Conhecimento geral | Assets promovidos (standard / multi-org) | Só por curadoria humana, nunca automática |

## Evidências e relações (herança do Scanner)

Modelo já implementado no Scanner (ver ADR 0008 / 0009):

- **RelationEvidence** — evidência com kind (`SqlForeignKey`, `PhcRel`, `PhcDtl`, `ExpressionInterpretation`), endpoints, payload e timestamp.
- **KnowledgeRelation** — consolidação conservadora a partir de evidências; não agrupar só por pares de tabelas.
- **FieldMapping** — extracção de igualdades diretas da AST; sem inferência semântica.
- Diagnósticos acumulam-se; evidência “boa” noutra relação não esconde problemas.

Distinções obrigatórias:

- Relação estrutural ≠ dependência funcional.
- Interpretação técnica ≠ conhecimento validado.
- Referência textual num artefacto ≠ claim de dependência.

## Identidade, versão e anotações — contexto histórico e direção de domínio

Os nomes abaixo registam conceitos exploratórios da proposta 1e; não são entidades físicas aprovadas. A identidade da origem e os fingerprints têm semântica normativa nos contratos P1/IKP. O modelo persistente da plataforma será definido durante a implementação, sem assumir que estes nomes têm de se tornar tabelas:

- **CustomizationIdentity** — identidade estável da personalização na instalação.
- **CustomizationVersion** — instância num scan (hash de conteúdo).
- **TargetBinding** — personalização → alvo do inventário (com resolução e confiança).
- **TechnicalReference** — menção detetada no conteúdo, com método e dúvida explícita.
- **HumanAnnotation** — texto/validação humana (`Draft` / `Validated` / `NeedsReview` / `Superseded`).
- **KnowledgePromotion** — registo de promoção instalação → geral.

Comportamento desejado entre scans:

1. Cada scan em armazenamento imutável.
2. Ponteiro para o scan atual.
3. Diff Added / Removed / Changed / Unchanged (por hash).
4. Anotações validadas passam a `NeedsReview` quando a versão muda.
5. Store de anotações separado do workspace do scan (para não serem apagadas na recolha).

## Standard vs instalação

- Conhecimento PHC standard: reutilizável entre organizações, após validação.
- Conhecimento de instalação / personalização: isolado por organização.
- Nesta conversa confirmou-se (BD `interh`): `dbo.rel` pode ser 100% catálogo standard (`ADM*`) sem cobrir ligações `u_*`.

## O que não fazer

- Fundir mappings REL com referências de código num tipo opaco.
- Usar `KnowledgeRelation` genérico para tudo.
- Promover conhecimento geral por semelhança de nomes.
- Inventar significado funcional no Scanner ou na ingestão automática.


## Regras consolidadas adicionais

- A base central agrega conhecimento observado em várias instalações; instalação é dimensão de proveniência e filtro, não limite da pesquisa global.
- Distinguir PHC Standard confirmado por referência, conhecimento observado e personalização específica. A presença numa ou várias instalações não promove automaticamente um objeto a standard.
- Factos REL/DTL declarados e relações derivadas (por exemplo, FK SQL ou interpretações) mantêm-se separados.
- As dependências funcionais são distintas de relações estruturais e referências textuais; devem conservar tipo de evidência, confiança e proveniência.

## Conhecimento PHC standard e evidência recolhida pelo Scanner

**Decisão arquitetural aceite — 2026-11-20.** Esta secção regista a separação de responsabilidades acordada para incorporar o conhecimento do comportamento standard do PHC, começando pelas triggers.

### Princípio

> **O Scanner recolhe evidências da instalação. O PHC Knowledge mantém o conhecimento standard necessário para interpretar essas evidências.**

O Scanner não é a fonte de verdade do comportamento standard e não deve precisar de transportar consigo as definições standard desencriptadas. O conhecimento standard é gerido centralmente pelo PHC Knowledge e pode ser reutilizado entre instalações, respeitando a versão PHC aplicável.

### Fontes e responsabilidades

| Fonte / componente | Responsabilidade |
| --- | --- |
| CS Default da versão PHC de referência | Referência estrutural para identificar os objetos standard que devem existir nessa versão. |
| Ficheiros SQL de referência, como `triggers phc.sql` | Fonte de importação e estudo das definições desencriptadas e da lógica standard. Não são uma dependência operacional do Portal nem do Scanner. |
| PHC Knowledge | Importa, valida, versiona e preserva o conhecimento standard, incluindo definições disponíveis, explicações comportamentais, proveniência e aplicabilidade por versão/build. |
| Scanner | Recolhe evidências observáveis da instalação: identidade/nome do objeto, objeto associado, eventos e metadados disponíveis, existência e estado de recolha. Preserva limitações, como definições SQL encriptadas ou indisponíveis. |
| Comparação no PHC Knowledge | Cruza as evidências do Scanner com o catálogo standard central e apresenta conclusões com o nível de certeza suportado pelas evidências. |

### Caso das triggers standard encriptadas

As triggers standard distribuídas com o PHC podem existir nas instalações com o corpo SQL encriptado. A definição desencriptada disponível em fontes de referência serve para compreender o comportamento standard e deve ser incorporada no conhecimento central, com proveniência e versão aplicável.

Quando uma captura identifica uma trigger standard, mas a definição recolhida está encriptada, o PHC Knowledge pode:

- reconhecer a presença da trigger através da identidade e dos metadados disponíveis;
- associar essa evidência ao conhecimento standard correspondente;
- explicar a finalidade e o comportamento documentado da trigger;
- indicar explicitamente que o corpo instalado não foi comparado com a definição de referência.

**Limite obrigatório:** a correspondência de nome, tabela e eventos, por si só, não prova que o corpo interno da trigger é idêntico. Se a definição instalada estiver encriptada ou indisponível, o estado da verificação interna deve ser apresentado como não verificável, nunca como definição confirmada sem alterações.

A comparação deve distinguir, pelo menos:

- objeto standard observado e identificado;
- objeto standard esperado mas não observado, tendo em conta a cobertura efetiva da captura;
- objeto adicional sem correspondência no catálogo standard;
- objeto com metadados divergentes;
- definição interna comparável e coincidente;
- definição interna divergente;
- definição interna não verificável por encriptação ou indisponibilidade.

A ausência só pode ser afirmada quando a operação de recolha relevante tiver cobertura suficiente e resultado válido. Uma operação não executada, incompleta ou sem cobertura não prova que o objeto não existe.

### Importação e ciclo de vida das fontes SQL

O ficheiro `triggers phc.sql` é uma fonte de conhecimento para a fase de importação/curadoria. Depois de as definições e respetivos metadados terem sido importados, validados e persistidos no PHC Knowledge, o funcionamento normal do Portal não deve depender da presença desse ficheiro no repositório ou no ambiente de execução.

A fonte original e a sua proveniência devem, contudo, permanecer rastreáveis segundo as regras de arquivo e licenciamento aplicáveis. A decisão não autoriza apagar já o ficheiro: a sua eventual remoção do repositório só poderá ser considerada depois de confirmada a incorporação integral e verificável do conteúdo necessário, e de avaliada a necessidade de conservar a fonte original.

### Âmbito e estado

- A decisão estabelece o princípio para triggers e é extensível, mediante análise própria, a outros objetos standard (por exemplo, procedures, funções e views).
- A decisão não significa que o catálogo standard esteja já implementado ou completo.
- Não aprova alterações imediatas ao Scanner, ao contrato IKP, aos modelos persistentes ou às migrations.
- Não altera retroativamente o comportamento da captura já analisada.
- A cobertura real de cada tipo de objeto e as regras atuais de recolha do Scanner continuam a exigir auditoria técnica antes de qualquer implementação.

**Próxima etapa:** auditar e definir a incorporação versionada do conhecimento standard das triggers na estrutura existente do PHC Knowledge; só depois especificar as alterações necessárias à comparação e à recolha. 

