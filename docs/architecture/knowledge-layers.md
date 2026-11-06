# Camadas de conhecimento e proveniência

Fonte consolidada a partir de [`../sources/analisar-base-phc-2026-11.txt`](../sources/analisar-base-phc-2026-11.txt) e dos ADRs do Scanner em `PhcKnowledgeScanner/docs/decisions/`.  
Estado: princípios orientadores; implementação na plataforma PHC Knowledge ainda não definida.

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

## Identidade, versão e anotações (proposta 1e)

Entidades lógicas propostas na conversa (ainda não fechadas para a plataforma):

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
