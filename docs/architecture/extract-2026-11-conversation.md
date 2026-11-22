> **Documento de extracção histórica:** este ficheiro preserva conteúdo e propostas de conversas anteriores, não é especificação normativa. Qualquer referência a reutilização multi-organização, tenants ou isolamento por organização está supersedida: a plataforma tem uma única organização interna e os clientes são contextos técnicos. Ver [decisões aprovadas](decisoes-aprovadas-2026-11-21.md).

# Extrato — conversa «Analisar base PHC» (2026-11)

Relatório conciso do tratamento da fonte bruta.  
Fonte: [`../sources/analisar-base-phc-2026-11.txt`](../sources/analisar-base-phc-2026-11.txt).

## O que foi feito

| Ação | Resultado |
| --- | --- |
| Avaliar PDF + TXT | Mesma conversa ChatGPT; PDF sem valor extra |
| Remover PDF deste repositório | Evita ~8 MB de UI |
| Arquivar TXT | Proveniência em `docs/sources/` |
| Consolidar decisões de produto | [`product-boundaries.md`](product-boundaries.md) |
| Consolidar modelo de camadas | [`knowledge-layers.md`](knowledge-layers.md) |

## O que a conversa contém (mapa)

1. **Visão e fronteiras** Scanner vs PHC Knowledge vs IA — consolidado acima.
2. **Evolução do Scanner** — inventário de tabelas, REL/DTL, parser V1/V1.1, projeção KR/FieldMapping, sessão de consulta, Desktop, schema snapshot. Já documentado nos ADRs em `PhcKnowledgeScanner/docs/decisions/` (0004–0014). **Não duplicado** aqui.
3. **Auditoria de capacidades / Fase 1e** — lacunas para passar de Scanner a plataforma; proposta de histórico de scans, bindings, referências técnicas e anotações humanas — princípios em [`knowledge-layers.md`](knowledge-layers.md).
4. **Investigação fatura manual (Frenetikmove)** — consolidada em `PhcKnowledgeScanner/docs/research/fatura-manual-frenetikmove-2026-11-04.md` (conhecimento de instalação, não standard).
5. **Dumps SQL / Profiler** — dados de sessão e listagens de colunas; úteis como evidência da investigação, não como documentação de produto.

## Já validado / existente (não reimplementar)

No `PhcKnowledgeScanner` (código + ADRs):

- Collectors SQL + personalizações PHC + relações declaradas.
- Inventário completo de tabelas; PK/FK (âmbito definido nos ADRs).
- Interpretação `declared-expr` V1.1 com recuperação parcial.
- Projeção `RelationEvidence` / `KnowledgeRelation` / `FieldMapping`.
- `ScanKnowledgeSession` e exploração Desktop.
- Database Schema Snapshot (metadados estruturais partilháveis).

## Lacunas relevantes para o PHC Knowledge

Prioridade pela conversa (valor de produto, não facilidade):

| Lacuna | Desbloqueia |
| --- | --- |
| Histórico de scans imutável | Diff, anotações estáveis, proveniência temporal |
| Bindings personalização → inventário | Impacto e navegação |
| Referências técnicas com confiança baixa explícita | “Quem menciona X?” sem fingir dependência |
| Anotações humanas por instalação | Domínios implementação / experiência |
| Promoção curada a conhecimento geral | Reutilização multi-organização |
| Importação ZIP/JSON no PHC Knowledge | Ponte Scanner → plataforma |
| Indexação de documentação oficial PHC | Explicar comportamento standard com evidência |

## Decisões ainda em aberto (da conversa)

1. Armazenamento de scans: pastas imutáveis + ponteiro `current` (recomendado) vs DB versionado único.
2. Onde vive o conhecimento humano na fase inicial: local por instalação vs serviço partilhado.
3. Âmbito inicial de extracção de referências (kinds de personalização).
4. Quando alargar recolha de triggers standard além de `u_*`.
5. Momento da promoção a conhecimento geral (após 2–3 instalações anotadas?).
6. Stack definitiva da plataforma PHC Knowledge (fora do âmbito desta conversa).

## Problemas da fonte bruta

- Ruído de UI ChatGPT e páginas quase vazias (“continua”).
- Mistura de prompts, entregas de código e dumps SQL na mesma exportação.
- Alguns caminhos e nomes de projetos antigos (ex. ConsoleApp) — tratar como contexto histórico, não como arquitetura obrigatória.

## Próximo passo sugerido

Usar estes documentos + o código em `PhcKnowledgeScanner/` para decidir a arquitetura tecnológica do PHC Knowledge e o contrato de importação dos exports do Scanner — com validação conjunta antes de implementar.
