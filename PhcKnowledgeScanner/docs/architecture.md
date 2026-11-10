# Arquitetura — PhcKnowledgeScanner

**Estado em 2026-11-10:** documento de arquitetura inicial/histórico; a aplicação descrita evoluiu substancialmente. A arquitetura executável atual deve ser lida em conjunto com `PROJECT-STATUS.md`, ADRs e código. O Scanner tem collectors SQL/PHC, desktop WPF, CLI, workspace SQLite, análise relacional e exportador IKP 0.1.

## Visão

```text
                    +--------------------------+
                    ¦  PhcKnowledgeScanner.Cli ¦  EXE / args / config local
                    +--------------------------+
                                 ¦
                    +------------?-------------+
                    ¦  PhcKnowledgeScanner.Engine ¦  seleção + probe + execução
                    +--------------------------+
                                 ¦  ICollector
              +------------------+------------------+
              ?                  ?                  ?
     [Collector A]        [Collector B]      [Collector N]
     (futuro)             (futuro)           (futuro)
              ¦                  ¦                  ¦
              +------------------+------------------+
                                 ?
                    +----------------------------+
                    ¦ PhcKnowledgeScanner.Export ¦  ZIP + manifest.json
                    +----------------------------+
```

Os contratos partilhados vivem em `PhcKnowledgeScanner.Abstractions`.

## Projetos e responsabilidades

| Projeto | Responsabilidade | Não faz |
| --- | --- | --- |
| **Abstractions** | `ICollector`, `ScanContext`, artefactos, issues, manifesto | SQL, IO de instalação, packing |
| **Engine** | Registo/seleção de collectors, probe, execução, captura de falhas | Conhecer schema PHC |
| **Export** | Empacotar manifesto + artefactos em ZIP estruturado | Interpretar conteúdo PHC |
| **Cli** | Host EXE, argumentos, wiring de collectors | Lógica de domínio de recolha |
| **Collectors.\*** *(futuro)* | Um módulo por família de fontes (SQL, dic, código, UI, …) | Orquestração global |

### Porque não copiar a granularidade do Scanner V1

O Scanner V1 (`ConsoleApp`) evoluiu para muitos projetos `Sources.*` / testes espelhados. Para o V2 preferimos:

1. Começar com **poucos projetos com responsabilidade clara**.
2. Extrair `PhcKnowledgeScanner.Collectors.<Área>` **só quando** uma área tiver API, dependências ou ciclo de vida próprios.
3. Evitar project references cruzadas e “camadas” sem entregável.

Isto é uma **decisão de arranque** (ver ADR 0002). Pode ser revista após o primeiro collector real.

## Extensibilidade de collectors

Cada collector:

1. Expõe `Id` estável e `DisplayName`.
2. Implementa `ProbeAsync` (disponibilidade sem efeitos laterais).
3. Implementa `CollectAsync` (leitura; produz `CollectedArtifact` + `CollectionIssue`).
4. Nunca assume que um objeto existe em todas as instalações — reporta omissão.

O Engine:

- Corre **full**, **selective** ou **probe-only**.
- Trata exceções não capturadas pelo collector como `Failed` no manifesto (sem abortar silenciosamente o resto, nesta versão inicial).
- Não trunca conteúdos; o artefacto transporta bytes completos.

> **Sugestão ainda não validada:** política de “continuar após falha” vs “fail-fast” configurável. Ver `open-decisions.md`.

## Formato de exportação inicial — histórico

```text
scan.zip
  README.txt
  manifest.json
  collectors/
    <collector-id>/
      ... artefactos com RelativePath ...
```

Este formato descreve o primeiro ZIP legado e não é o contrato de intercâmbio atual. O Scanner mantém esse exportador por compatibilidade, mas para integração com PHC Knowledge emite IKP 0.1, definido em `../../docs/architecture/ikp-0.1-specification.md` e implementado em `PhcKnowledgeScanner.Export`.

## Princípios técnicos obrigatórios

1. **Read-only** face à instalação PHC (salvo decisão explícita futura e documentada).
2. **Preservação do original** — sem normalização destrutiva nesta ferramenta.
3. **Sem truncagem silenciosa** — se houver limites, devem ser erros/avisos explícitos.
4. **Inventário de cobertura** — manifesto distingue sucesso, parcial, skip e falha.
5. **Independência** — zero dependência de projetos `ConsoleApp` / Scanner V1.

## Riscos conhecidos

| Risco | Impacto | Mitigação inicial |
| --- | --- | --- |
| Heterogeneidade de versões PHC | Collectors partem em instalações reais | Probe + issues; sem schema universal hardcoded |
| Objetos homónimos com implementações diferentes | Confusão no consumo futuro | Guardar conteúdo + metadados de origem (versão, object id, timestamps quando existirem) |
| Volume de código/BLOB | ZIP/memória | Decisão pendente sobre streaming para disco antes do ZIP |
| Credenciais SQL | Segurança | Config local ignorada no Git; sem secrets no repositório |
| Tentação de reutilizar Scanner V1 | Acoplamento | Proibição explícita de project reference; só consulta documental |

## O que está implementado vs proposto

| Elemento | Estado |
| --- | --- |
| Contratos `ICollector` / manifesto / artefactos | **Implementado (esqueleto)** |
| `ScanEngine` (seleção / probe / execução) | **Implementado (sem PHC)** |
| `ZipScanExporter` | **Implementado (mínimo)** |
| CLI help / version / smoke | **Implementado** |
| Collectors PHC | **Implementados** para as famílias atualmente suportadas |
| Schema IKP 0.1 | **Definido e implementado**; ZIP legado mantém formato próprio |
| DI / System.CommandLine / Spectre | **Em aberto** |


## Atualização — 2026-11-10

Esta arquitetura de arranque não é a descrição completa do sistema atual. O Scanner atual inclui WPF, CLI, collectors reais, SQLite, relações REL/DTL, análise derivada, Object Index e exportador IKP 0.1. O PHC Knowledge é uma plataforma separada no mesmo monorepo; não é host do Scanner nem dependência runtime. Ver `../PROJECT-STATUS.md` (estado do Scanner) e `../../docs/architecture/ikp-0.1-specification.md` (contrato de transferência).
