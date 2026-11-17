# Roadmap visual do PHC Knowledge — visão, fases e estado atual

**Data de referência:** 2026-11-17  
**Estado:** roadmap revisto em 2026-11-17; Scanner/IKP e primeira fatia Import já implementados. API, SQL real, reconciliação e materialização continuam por concluir.  
**Nota:** as percentagens não são estimadas; os estados indicam maturidade qualitativa.

## 1. Objetivo do produto

Construir uma plataforma de conhecimento técnico reutilizável, contextualizado por instalação PHC e sustentado em evidências verificáveis.

A plataforma deverá permitir:
- encontrar implementações semelhantes realizadas anteriormente;
- compreender como funcionalidades e comportamentos PHC são produzidos;
- identificar dependências e impactos antes de alterações;
- relacionar documentação oficial PHC com a realidade de cada instalação;
- recuperar problemas, causas, soluções e decisões técnicas anteriores;
- apoiar consultores e programadores com respostas fundamentadas e rastreáveis.

Não é um clone do PHC, um explorador SQL genérico, um repositório de ficheiros ou apenas um chatbot com documentação solta.

## 2. Roadmap visual

Legenda:
- **Concluído / existente** — existe implementação funcional ou decisão consolidada.
- **Em curso** — investigação, desenho ou implementação parcial.
- **Por construir** — ainda não existe implementação funcional no produto central.
- **Dependente de validação** — precisa de captura real ou validação técnica antes de fechar.

```mermaid
flowchart TB
    V["VISÃO FINAL<br/>Conhecimento PHC reutilizável, contextualizado e comprovável"]

    subgraph P0["FASE 0 — Aquisição técnica"]
      A1["Scanner autónomo<br/>Ligação read-only, collectors SQL/PHC"]
      A2["Inventário + artefactos<br/>Objetos, definições, issues e coverage"]
      A3["Workspace local SQLite<br/>Exploração, relações e análise"]
      A1 --> A2 --> A3
    end

    subgraph P1["FASE 1 — Captura fiável e pacote normalizado"]
      B1["Identidade e proveniência<br/>SourceObjectId / SourceIdentity"]
      B2["Object-index<br/>MetadataFingerprint / DefinitionFingerprint"]
      B3["Cobertura e estados individuais<br/>Ausente ≠ vazio ≠ não recolhido"]
      B4["Validação sintética do Object Index<br/>21 kinds + JSON Schema oficial"]
      B5["Exportador IKP 0.1<br/>Implementado no Scanner"]
      B6["Validação do ZIP real<br/>Schema, desempenho e validador independente"]
      B7["Validação com captura real<br/>Demo standard → instalação personalizada"]
      B1 --> B2
      B2 --> B3
      B3 --> B4 --> B5 --> B6 --> B7
    end

    subgraph P2["FASE 2 — Plataforma central"]
      C1["Registo de instalações"]
      C2["Import service<br/>Validator, arquivo imutável e receipt SQL"]
      C3["Armazenamento de capturas<br/>Histórico imutável"]
      C1 --> C2 --> C3
    end

    subgraph P3["FASE 3 — Evolução e conhecimento"]
      D1["Identidade lógica e observações"]
      D2["Diff entre capturas<br/>Added / Removed / Changed / Unchanged"]
      D3["Dependências e impacto"]
      D4["Anotações e validação humana"]
      D5["Conhecimento standard vs instalação"]
      D1 --> D2 --> D3
      D3 --> D4 --> D5
    end

    subgraph P4["FASE 4 — Investigação assistida"]
      E1["Pesquisa contextual"]
      E2["Perguntas sobre implementações e comportamento"]
      E3["IA com evidências e proveniência"]
      E4["Feedback e resolução alimentam conhecimento"]
      E1 --> E2 --> E3 --> E4
    end

    V
    P0 --> P1 --> P2 --> P3 --> P4 --> V

    classDef done fill:#d9ead3,stroke:#38761d,color:#173b13
    classDef progress fill:#fff2cc,stroke:#bf9000,color:#5b4300
    classDef pending fill:#eeeeee,stroke:#777,color:#333
    classDef validation fill:#fce5cd,stroke:#e69138,color:#783f04

    class A1,A2,A3 done
    class B1,B2,B3,B4 done
    class B5 done
    class B6,B7 validation
    class C1,C3,D1,D2,D3,D4,D5,E1,E2,E3,E4 pending\n    class C2 progress
```

> O diagrama é sequencial para comunicar dependências, mas algumas atividades podem decorrer em paralelo. A análise relacional já existente no Scanner é uma capacidade reaproveitável; não significa que a plataforma central esteja implementada.

## 3. Onde estamos hoje

### Fase 0 — Aquisição técnica

**Estado: existente / funcional no Scanner**

Já temos:
- aplicação Scanner autónoma, com Desktop e CLI;
- collectors para estrutura SQL Server e personalizações PHC;
- recolha de objetos, definições/artefactos, issues e cobertura por operação;
- workspace SQLite;
- inventário pesquisável e exportação ZIP de personalizações;
- relações declaradas REL/DTL;
- interpretação de expressões e projeção relacional com evidências, relações de conhecimento e field mappings;
- testes automatizados no projeto Scanner.

Limites conhecidos:
- a cobertura atual tem diferenças semânticas entre operações e tipos de recolha;
- alguns collectors filtram registos sem conteúdo antes de os emitir;
- a cobertura atual não permite concluir remoções com segurança em todos os casos;
- o exportador IKP 0.1 já existe e está integrado na CLI; a validação end-to-end do ZIP produzido a partir de captura real continua por confirmar.

### Fase 1 — Captura fiável e pacote normalizado

**Estado: Object Index implementado; projeção validada com captura real; exportador IKP 0.1 implementado; validação end-to-end do ZIP real pendente**

**Atualização de implementação — 2026-11-07**

#### P1.B — Object Index: implementado; validação real parcial

Implementado em `PhcKnowledgeScanner.Export`:
- `ObjectOperationResolver`: mapping explícito dos 21 kinds emitidos para operações observadoras;
- `SourceIdentityResolver`: stamps PHC e IDs nativos SQL, com scopes resolvidos dentro da captura;
- `MetadataProjectionResolver`, `CanonicalJsonSerializer` e `MetadataFingerprintCalculator`: projeção semântica e fingerprints determinísticos;
- `DefinitionProjectionResolver`: estados individuais Available / Empty / Unavailable / NotCollected, sustentados por evidência;
- `ObjectIndexProjectionInputReader`: leitura read-only de CaptureId, coverage, objetos, issues, metadados e bytes de artefactos referenciados;
- `ObjectIndexProjector`: projeção pura e determinística de `object-index.json`;
- `ObjectIndexWorkspaceProjector`: ligação read-only entre workspace e projector.

**Validação confirmada — GitHub Actions run `42419610023`:**
- Build .NET aprovado;
- **275 testes .NET aprovados**;
- cenário sintético do projector cobre os **21 kinds**;
- JSON produzido pelo C# validado contra o JSON Schema oficial IKP 0.1 Draft 2020-12.

A validação sintética cobre os 21 kinds. Em 2026-11-08, a projeção foi também exercitada com a captura real `CSDefault202603`: 16.618 objetos projetados, 8.307 SourceIdentity e 16.581 MetadataFingerprint; foram confirmadas correspondências de tamanho/hash dos 49 artefactos. Esta validação real cobriu apenas os kinds presentes nessa captura (8/21), pelo que não demonstra cobertura real exaustiva nem estabilidade de identidade entre capturas.

Não foram alterados schema SQLite, collectors ou queries PHC/SQL nesta etapa.

#### P1.C / IKP — exportador implementado; validação real do pacote pendente

Investigação registada em [`p1b-ikp-package-export-readiness-investigation-2026-11-07.md`](p1b-ikp-package-export-readiness-investigation-2026-11-07.md).

O pacote IKP 0.1 exige cinco documentos de raiz:
1. `manifest.json`
2. `capabilities.json`
3. `coverage.json`
4. `object-index.json`
5. `checksums.json`

O validador de referência também verifica referências cruzadas, paths seguros, artefactos, tamanhos, SHA-256 e `logicalContentDigest`.

**Implementação concluída:** `IkpPackageExporter` dedicado em `PhcKnowledgeScanner.Export`, integrado na CLI através de `scan-ikp` e `export-ikp`. O exportador recebe o manifesto e o workspace como entradas separadas, valida a correspondência, gera os cinco documentos obrigatórios, checksums e digest lógico, e preserva os bytes dos artefactos.

Limitação identificada: o workspace atual não persiste timestamps reais de início/fim, ScanMode, ScannerVersion nem um origin descriptor estruturado. O exportador não pode inventar esses valores nem inferi-los de `created_at_utc` ou do label da instalação. Para a primeira versão, o manifesto completo terá de estar disponível em memória no momento da exportação.

#### P1.C.1 — Desempenho da validação do Object Index: causa isolada; correção pendente

Em 2026-11-11 foi executado um teste A/B com o mesmo pacote real `CSDefault202603-ikp-validation.zip`, usando JsonSchema.Net 7.3.1:

- Com `uniqueItems: true` no array principal `objects` de `object-index.schema.json`, a validação ultrapassou 120 segundos sem terminar.
- Numa cópia temporária do schema, removendo apenas esse `uniqueItems`, a avaliação do schema terminou em 2,64 segundos; o diagnóstico completo terminou em 2,82 segundos e o pacote foi aceite.
- A leitura do JSON (9.000.983 bytes) demorou cerca de 0,10 segundos e o carregamento do schema cerca de 0,05 segundos. A diferença observada está, portanto, na avaliação da regra `uniqueItems` sobre o array grande, não na leitura do ficheiro.
- `IkpPackageValidator.ValidateObjectIndex` já verifica duplicados de `sourceObjectId` com `HashSet<string>` e `StringComparer.Ordinal`, preservando a unicidade dos identificadores em tempo linear.

**Decisão técnica:** remover exclusivamente `uniqueItems: true` do array raiz `objects`. Manter `uniqueItems: true` em `definition.artifactIds`, que é uma lista pequena e cuja unicidade continua a ser relevante. Não alterar outras regras do schema, o contrato IKP 0.1, o código do Scanner, SQLite, collectors ou a lógica de identidade nesta intervenção.

**Estado:** decisão sustentada por teste A/B; a alteração ao schema ainda não foi aplicada nem validada no repositório. O teste A/B confirma o comportamento do schema com o pacote usado, mas não substitui a validação end-to-end final do ZIP exportado.

#### Próximos passos da Fase 1

1. Fazer backup e remover apenas `uniqueItems: true` do array principal `objects` em `docs/architecture/schemas/ikp-0.1/object-index.schema.json`; manter a regra em `definition.artifactIds`.
2. Confirmar no código que `ValidateObjectIndex` continua a rejeitar `sourceObjectId` duplicados através do `HashSet`.
3. Executar `dotnet clean PhcKnowledge.sln`, `dotnet test PhcKnowledge.sln` e `dotnet build PhcKnowledge.sln`; confirmar também que `artifactIds` duplicados continuam a ser rejeitados.
4. Revalidar `CSDefault202603-ikp-validation.zip`, registar tempos e confirmar aceitação. Depois, executar a validação end-to-end do ZIP efetivamente produzido por `scan-ikp` / `export-ikp`, inspeccionando os cinco documentos, payloads, checksums e digest lógico.
5. Confirmar categorias/artefactos efetivamente exportados e compatibilidade entre manifesto, capabilities, coverage e Object Index.
6. Manter por validar os kinds não presentes na captura real (13/21) e a estabilidade de identidade entre capturas.
7. Não alterar SQLite/collectors sem evidência concreta de lacuna.

#### Fase 1 — estado resumido

- **Concluído:** P0 CaptureId/capabilities/coverage.
- **Implementado:** P1.B Object Index, identidade, fingerprints e estados individuais; 21 kinds cobertos sinteticamente.
- **Validado com captura real:** projeção para 8/21 kinds presentes na `CSDefault202603`, com hashes/tamanhos de artefactos verificados.
- **Implementado:** exportador IKP 0.1 e comandos CLI `scan-ikp` / `export-ikp`.
- **Pendente imediato:** aplicar a correção localizada de `uniqueItems` no schema e executar testes/build; concluir validação end-to-end do ZIP exportado com validador independente.
- **Pendente adicional:** kinds não observados na captura real; identidade entre capturas.

### Fase 2 — Plataforma central

**Estado: por construir; próxima fase de implementação aprovada**

O núcleo `PhcKnowledge.Import` já contém validator, serviço de ingestão, arquivo imutável e repositório SQL de receipts, com testes P1–N1. A implementação atual não demonstra API HTTP/autenticação nem execução contra SQL Server real. Depois, e em etapas separadas, construir:
- registo e gestão de instalações;
- receção HTTP, validação estrutural/de segurança/integridade e arquivo imutável de pacotes IKP;
- ingestão transacional e idempotente;
- armazenamento de capturas imutáveis e respetivos artefactos;
- consulta do inventário importado.

A plataforma deverá consumir IKP; não deverá ligar-se diretamente às bases de dados dos clientes nem executar o Scanner em runtime.

### Fase 3 — Evolução e conhecimento

**Estado: arquitetura conceptual / por implementar na plataforma**

Falta construir:
- entidades lógicas e observações por captura;
- correspondência de identidade entre scans;
- comparação segura entre capturas;
- deteção de dependências e análise de impacto;
- anotações humanas persistentes e estados de validação;
- separação entre conhecimento standard PHC e conhecimento específico de cada instalação;
- promoção controlada de conhecimento reutilizável.

A regra essencial é que uma ausência só pode ser considerada remoção quando a cobertura e a compatibilidade permitem essa conclusão.

### Fase 4 — Investigação assistida por IA

**Estado: visão futura / por construir**

Só depois de existir conhecimento estruturado e rastreável faz sentido construir:
- pesquisa semântica contextual;
- perguntas sobre implementações e comportamento;
- explicações que cruzem documentação oficial, estrutura e personalizações;
- respostas com evidências, origem, confiança e incerteza explícita;
- ciclo de feedback em que problemas e soluções validados enriquecem a base de conhecimento.

A IA não deverá inventar factos nem substituir a proveniência. Deve investigar sobre conhecimento já organizado.

## 4. Próximos marcos recomendados

| Ordem | Marco | Estado / critério de conclusão |
|---:|---|---|
| 1 | P0 — CaptureId, capabilities e coverage | **Concluído.** Contratos e testes de captura/cobertura existentes. |
| 2 | P1.B — Object Index | **Implementado.** 21 kinds cobertos sinteticamente; captura real validou 8/21 kinds e 49 artefactos (tamanho/hash). |
| 3 | Exportador IKP 0.1 | **Implementado no Scanner.** `IkpPackageExporter`, `scan-ikp` e `export-ikp`. |
| 4 | Correção de desempenho do schema IKP | **Decisão tomada; alteração pendente.** Remover `uniqueItems` apenas de `objects`; manter unicidade de `sourceObjectId` no validador C# e de `artifactIds` no schema. Repetir testes e medição. |
| 5 | Validação end-to-end de IKP real | **Pendente.** Gerar pacote da captura default existente e validar com schemas/validador independente. |
| 6 | Receive / Validate / Archive | **Implementado como serviço Import.** Endpoint HTTP/autenticação, SQL Server real e reconciliação ainda pendentes. |
| 7 | Associação, materialização e comparação | Posterior; não faz parte da primeira fatia. |
| 8 | Histórico, identidade e diff | Comparar capturas sem confundir instalação, objeto observado e entidade lógica; só inferir remoções com cobertura suficiente. |
| 9 | Dependências, conhecimento e pesquisa | Consultas devolvem objetos, evidências, contexto e anotações rastreáveis. |
| 10 | Investigação assistida por IA | Respostas fundamentadas em evidências, proveniência e incerteza explícita. |

## 5. Critério para não perder o rumo

Cada fase deve produzir uma capacidade verificável:
1. **Recolher** sem alterar a instalação.
2. **Descrever** exatamente o que foi observado e o que não foi.
3. **Transportar** a captura sem perder integridade ou proveniência.
4. **Importar** sem misturar instalações nem apagar histórico.
5. **Comparar** apenas quando a cobertura permite.
6. **Relacionar** factos, interpretações e conhecimento humano sem os confundir.
7. **Investigar** com respostas justificadas por evidências.

## 6. Referências de arquitetura

- `README.md` — fronteira entre Scanner e plataforma.
- `docs/architecture/product-boundaries.md` — objetivo e limites do produto.
- `docs/architecture/knowledge-layers.md` — camadas de conhecimento e proveniência.
- `docs/architecture/ikp-0.1-specification.md` — pacote de transferência.
- `docs/architecture/p1-identity-provenance-contract.md` — identidade, proveniência e fingerprints.
- `docs/architecture/p1b-static-consistency-audit-2026-11-06.md` — auditoria estática do object-index.
- `docs/architecture/p1b-projector-workspace-read-contract-2026-11-06.md` — leitura read-only do workspace.

**Nota de rigor:** este roadmap é uma síntese do estado documental, do código, dos resultados de CI e dos testes comunicados até 2026-11-11. Não representa percentagens de conclusão. O teste A/B de desempenho foi executado com um pacote real e uma cópia temporária do schema; não equivale à validação end-to-end final do ZIP produzido pelo exportador.


## 7. Decisão transversal — preparação para RAG

O PHC Knowledge deve ser preparado desde já para poder evoluir para um RAG híbrido e fundamentado em evidências, mas a implementação do motor RAG fica para depois da construção da base de conhecimento estruturada. Não antecipar escolhas de LLM, embeddings ou base vetorial.

Princípio: **conhecimento fiável primeiro; recuperação aumentada por IA depois**.

A decisão arquitetural, os princípios a preservar e os critérios de prontidão estão documentados em [rag-readiness-and-knowledge-first-decision-2026-11-06.md](rag-readiness-and-knowledge-first-decision-2026-11-06.md).


## 8. Visão futura do Scanner — edição e publicação controlada

Foi registada uma possível evolução **da aplicação Scanner**: permitir localizar um objeto/personalização, editar uma versão de trabalho, comparar e validar a alteração e, quando suportado, publicá-la na instalação PHC com aprovação e auditoria.

Esta capacidade pertence ao Scanner, não à plataforma central. Os collectors mantêm-se read-only; edição e publicação serão componentes separados, com adaptadores explícitos por tipo de objeto. A evidência original da captura deverá permanecer imutável.

É uma visão futura e não altera o foco atual em P1.B, Object Index, IKP e validação com captura real. Não existe implementação nem decisão sobre os primeiros tipos de objetos a publicar.

Detalhe e salvaguardas: [scanner-controlled-change-management-vision-2026-11-07.md](scanner-controlled-change-management-vision-2026-11-07.md).


## 9. Estado consolidado — 2026-11-17

A sequência atual é: **Scanner/IKP implementado e captura real examinada → Receive / Validate / Archive implementado como serviço em PhcKnowledge.Import → robustecer SQL/arquivo e desenhar HTTP/autenticação → materializar conhecimento**.

A primeira fatia inclui IkpPackageValidator, IkpPackageIngestionService, FileSystemImmutablePackageStore, SqlServerPackageReceiptRepository, migration 001_package_ingestion_receipts.sql e testes P1–N1. Os testes usam workspace sintético; não provam execução SQL Server real, API HTTP/autenticação nem validação end-to-end com o ZIP real CSDefault202603 no serviço Import.

O arquivo acontece antes da gravação SQL do receipt. Falha SQL posterior pode deixar ficheiro órfão; reconciliação/retry é um bloqueador técnico antes de alargar a ingestão. O modelo de destino separa Package global de PackageReceipt por Organization; a migration atual ainda tem unicidade por Organization.

A captura real confirmou 16.618 SourceObjectIds, máximo 54, sem duplicados ordinais, espaços nas extremidades ou valores não-ASCII. O destino proposto é NVARCHAR(256) com comparação ordinal. REL/DTL está num ficheiro físico único com coleções declaredRelations e interpretations; as entidades de domínio permanecem separadas.

A base central escolhida é SQL Server Express; versão concreta e adequação operacional continuam por validar. ZIPs originais ficam em arquivo imutável fora da base relacional. SignalR mantém-se na arquitetura.
