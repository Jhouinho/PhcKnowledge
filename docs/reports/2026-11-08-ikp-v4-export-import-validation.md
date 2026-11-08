# Relatório de trabalho — 2026-11-08

## Objetivo da sessão

Fechar a etapa de transporte IKP 0.1 com uma exportação real do Scanner, preparar a validação do lado da plataforma e atualizar a visão de arquitetura do PHC Knowledge.

## 1. Exportação IKP 0.1

O exportador IKP foi concluído em `PhcKnowledgeScanner.Export`. A exportação parte de um workspace existente e é read-only: não volta a executar os collectors nem altera a captura original.

Foi produzido o pacote real V4 a partir da base `CSDefault202603`.

| Indicador | Resultado |
|---|---:|
| PackageId | `5fc15128-b7ab-4a23-ad61-93757aedb0f7` |
| CaptureId | `a1f2fc33-7b0a-4c8f-b049-073a33ef7978` |
| Entradas ZIP | 58 |
| Artefactos de payload | 53 |
| Objetos no Object Index | 16.618 |
| Procedures indexadas | 36 |
| Definições de procedures indisponíveis | 6 |
| Relações declaradas REL | 295 |
| Relações declaradas DTL | 626 |
| Interpretações analisadas | 921 |
| Falhas de integridade | 0 |

**Integridade**
- SHA-256 do ZIP: `0b1545e010c9fa180380c150da91cb6e57cbdbba74df9f02cf2e2db7531805ac`.
- Logical digest: `74ef34d92d0cc7a5fbc976c1e160d96b6a234b3653f01fd5b1ed6eb3a556e7b7`.
- Os 53 artefactos referenciados passaram a verificação de checksum e comprimento.
- Factos REL/DTL e interpretações foram transportados separadamente.
- Ficheiros antigos existentes na pasta de artefactos, mas sem referência no workspace, não foram incluídos.

**Limitação identificada:** a captura V4 não contém campos físicos `u_*`. A validação da identidade desses campos continua pendente de uma captura adequada.

## 2. Fundação de validação do importador

Foi aberto o PR [#2 — Add IKP 0.1 import validation foundation](https://github.com/Jhouinho/PhcKnowledge/pull/2), na branch `feature/ikp-import-validation-staging-20261108`.

O PR permanece **Draft, aberto e não integrado**.

Foi criado o projeto `src/PhcKnowledge.Import`, sem dependência do Scanner. O validador:
- lê o ZIP em memória, sem o extrair;
- verifica paths inseguros, entradas duplicadas, estrutura e limites;
- valida os cinco documentos contratuais contra JSON Schema Draft 2020-12;
- verifica checksums SHA-256 e logical digest;
- verifica conjunto de ficheiros, referências a artefactos, capabilities, coverage e Object Index;
- verifica relações declaradas e ligações às respetivas interpretações;
- suporta as formas reais dos payloads produzidos pelo exportador;
- devolve um resultado estruturado de validação.

Foram acrescentados testes para pacote válido, payloads reais do exportador, interpretação órfã, identificadores duplicados, schema inválido, conteúdo adulterado e path traversal.

## 3. Diagnóstico local do pacote real

Foi criada a ferramenta `tools/PhcKnowledge.IkpDiagnostic`. Recebe o caminho do ZIP, executa o validador real e apresenta um relatório JSON com SHA-256, identificadores, contagens e erros. Não extrai nem altera o pacote.

Comando a executar quando houver acesso ao PC:

```powershell
dotnet run --project tools/PhcKnowledge.IkpDiagnostic -- "C:\Temp\phc-capture-ikp-v4.zip" --report "C:\Temp\phc-capture-ikp-v4-validation.json"
```

O relatório deve ser partilhado como texto na conversa. Não é necessário enviar o ZIP.

**Estado:** ferramenta criada; execução contra o pacote V4 real ainda pendente. A exportação válida pelo Scanner e a validação sintética/CI não substituem este teste de integração.

## 4. CI

GitHub Actions run `42524895821` — workflow `.NET — PhcKnowledgeScanner` — terminou com sucesso.

Passaram:
- restore;
- build;
- testes da solução;
- validação do Object Index gerado em C# contra o JSON Schema IKP 0.1;
- restantes passos do workflow.

## 5. Atualização da documentação de arquitetura

Foram atualizados:
- `docs/architecture/roadmap-visual-phc-knowledge-2026-11-06.md` — estado real da Fase 1 e da fundação da Fase 2, próximos marcos e bloqueio atual;
- `docs/architecture/diagramas-projeto-er-e-classes-2026-11-06.md` — distinção entre o ER do workspace SQLite do Scanner, o pipeline IKP, a validação atualmente implementada e o ER conceptual futuro da plataforma.

O diagrama ER conceptual da plataforma está explicitamente identificado como proposta, não como schema implementado. A validação do importador não foi confundida com staging ou ingestão.

## 6. Decisões e limites preservados

- Scanner e plataforma continuam independentes; o importador não referencia o Scanner.
- O pacote IKP é imutável e não transporta um InstallationId autoritativo da plataforma.
- PackageId, CaptureId, SourceObjectId e identidade lógica mantêm âmbitos distintos.
- Não foi implementado staging persistente nem ingestão PostgreSQL.
- Não foram alterados collectors nem schema SQLite para acomodar a exportação.
- O PR #2 não foi promovido a Ready nem integrado na `main`.

## 7. Próximo passo

1. Executar `PhcKnowledge.IkpDiagnostic` sobre o ZIP V4 real.
2. Analisar o relatório e corrigir eventuais incompatibilidades.
3. Só após validação, desenhar staging persistente e respetiva ingestão transacional.

## Reflexão da sessão

A etapa de exportação deixou de ser apenas uma especificação: existe um pacote IKP real, com milhares de objetos e integridade verificada. Do lado da plataforma já existe uma primeira barreira de validação independente do Scanner, testada em CI.

O risco técnico imediato passou a ser a compatibilidade efetiva entre o pacote V4 real e o importador. Por isso, a próxima decisão de arquitetura deve aguardar o diagnóstico local. Avançar já para persistência criaria o risco de consolidar um modelo de staging sobre pressupostos ainda não verificados.
