# Ensaio T-B — alteração controlada e reposição de definição

**Data:** 24-11-2026  
**Estado:** concluído; evidência experimental aceite no âmbito da âncora e ambiente testados.  
**Tipo:** validação funcional do Scanner, com mutações temporárias executadas manualmente na interface PHC entre capturas read-only.  
**Âmbito:** uma definição `phc.hidden_grid` (`dbo.formc`) na base de testes `dfblock`; não constitui política geral por ObjectKind.

## 1. Objectivo

Validar se o Scanner preserva a identidade observada quando a definição muda, detecta a alteração do conteúdo capturado, detecta a reposição exacta do conteúdo original e mantém separadas integridade do artefacto, DefinitionFingerprint semântico e MetadataFingerprint.

O ensaio não pretende validar normalização semântica, matching automático, identidade persistente universal ou comportamento de outros ObjectKinds.

## 2. Ambiente e método

- PHC Evolution CS: `2026.03.00.5`.
- Servidor/base de testes: `DESKTOP-GB9607Q\SQLEXPRESS / dfblock`.
- Scanner: `1.0.0+ba3bf7c0839acc634cb47da8c374f536e02d19f4`.
- Capturas: Full.
- Contrato: IKP 0.1 / Object Index schema v2.
- Âncora: `gau_abertas`, ecrã `PDU_34X0QNQFL`, tabela `dbo.formc`, campo de definição `prop`.
- As alterações temporárias foram feitas pela interface PHC. As capturas e comparações foram de leitura.
- Não foi incluído neste documento código Xbase nem conteúdo proprietário da definição.

## 3. Sequência e integridade das evidências

### 3.1 Captura T0 inicial — discrepância detectada, não aceite como baseline

A primeira captura T0 (`290ff953-4f20-4f66-a9bc-cc2c6a1e4dc3`; PackageId `3a9f73d5-ce16-44ff-9419-2f3fb9fa3a9f`) encontrou o marcador de teste já presente na definição:
- `prop`: 337 bytes;
- artefacto IKP: 473 bytes;
- MetadataFingerprint: igual ao valor da baseline anterior.

A comparação com T-A mostrou que, com CR e CRLF tratados como a mesma quebra, o texto continha o sufixo ` Teste` no primeiro comentário e a linha de marcador. Os oito CR originais também tinham sido gravados como CRLF, acrescentando oito bytes ao campo SQL. A linha tinha auditoria em 24-11-2026 às 17:02:24, utilizador `ADM`.

A investigação dos logs não encontrou UPDATE/INSERT nos registos consultados. O default trace não continha linhas, mas não regista alterações a colunas `text`. A gravação através do PHC pelo utilizador `ADM` é a hipótese mais suportada, não uma causa comprovada. Esta captura foi preservada, mas **não foi usada como baseline do ensaio**.

### 3.2 T0 recuperada — baseline aceite

Após reposição manual pela interface PHC da definição original, foi executada nova captura Full:
- CaptureId: `ec8be6c7-40aa-46de-a6ba-1f68b3ba0c6d`;
- PackageId: `b8793b27-95a8-47ed-b96f-bafb0716f8dc`;
- `prop`: 289 bytes, devido à representação CRLF;
- artefacto: 426 bytes;
- SHA-256 do artefacto: `ec217df6e0d2b11005e24fe56216fd3d0e833ad1f173f7acac7ac0ebe69e8811`.

O artefacto coincidiu byte a byte com o artefacto T-A (426 bytes, mesmo SHA-256). O texto, com quebras normalizadas, correspondeu à definição original. A diferença entre 281 bytes no snapshot anterior e 289 bytes nesta baseline resulta dos oito CR convertidos em CRLF; o formatter do collector normaliza essa representação e produz o mesmo artefacto.

A âncora foi encontrada uma vez na base e uma vez no índice. MetadataFingerprint: `4e48a43e64171510241eff50406dbf4e0b49e9f61fe20c33160d52d3b15b764d`. DefinitionFingerprint: `Unavailable / NoValidatedPolicy`.

### 3.3 T1 — alteração controlada

Foi acrescentada, pela interface PHC, apenas uma linha de comentário de teste independente. A instrução executável foi confirmada como inalterada e a grelha continuou a funcionar normalmente.

Captura T1:
- CaptureId: `484bdb48-3bba-4cf2-8108-65c0162090e1`;
- PackageId: `7f546c0d-d9b7-4b43-b55e-6a4bb534bd0b`;
- `prop`: 331 bytes;
- artefacto: 467 bytes;
- SHA-256 do artefacto: `0fc66d8074dcb41eb225ad02feea57ac7b1bfca100dcf213fd4db56044371469`.

A diferença de conteúdo relativamente a T0 foi exclusivamente a linha de comentário acrescentada. A diferença no artefacto foi de 41 bytes, correspondente à linha serializada pelo formatter. A gravação PHC foi auditada às 17:41:29, utilizador `ADM`.

SourceObjectId, SourceIdentity, metadata, MetadataFingerprint e envelope DefinitionFingerprint mantiveram-se iguais aos de T0. A definição permaneceu `Available`. As 23 operações de coverage e as 18 955 contagens de objectos mantiveram-se iguais. Não foi detectada alteração noutro objecto.

### 3.4 T2 — reposição exacta

A definição original foi reposta pela interface PHC. A comparação foi feita sobre os bytes de `dbo.formc.prop` e do artefacto, não apenas sobre o texto apresentado no editor.

Captura T2:
- CaptureId: `cabe1217-8af0-46ea-a73b-d2494862de13`;
- `prop`: 289 bytes, igual a T0 recuperada byte a byte;
- artefacto: 426 bytes, igual a T0 recuperada byte a byte;
- SHA-256 do artefacto: `ec217df6e0d2b11005e24fe56216fd3d0e833ad1f173f7acac7ac0ebe69e8811`.

Face a T1, a única diferença de conteúdo foi a remoção da linha de comentário de teste. A linha deixou de existir em qualquer registo de `dbo.formc`. A âncora manteve SourceObjectId, SourceIdentity, metadata, MetadataFingerprint, envelope DefinitionFingerprint e disponibilidade `Available`. Coverage e as 18 955 contagens mantiveram-se iguais. Nenhum outro objecto mudou.

A única diferença residual reportada fora da definição foi `usrhora`, que passou de 17:35:50 para 17:47:09 em 24-11-2026, utilizador `ADM`, consistente com a gravação da reposição. O logical digest global também diferiu porque o CaptureId integra o pacote; igualando esse identificador, object-index, coverage e declared-relations foram iguais.

## 4. Síntese T0 → T1 → T2

| Dimensão | T0 recuperada | T1 | T2 |
|---|---|---|---|
| Artefacto | 426 bytes | 467 bytes | 426 bytes |
| SHA-256 do artefacto | `ec217df6…e69e8811` | `0fc66d80…44371469` | igual a T0 |
| Conteúdo | Original | Uma linha de comentário adicional | Original, byte a byte igual a T0 |
| SourceObjectId / SourceIdentity | Baseline | Iguais a T0 | Iguais a T0 |
| MetadataFingerprint | `4e48a43e…` | Igual | Igual |
| DefinitionFingerprint | Unavailable / NoValidatedPolicy | Igual | Igual |
| Definição | Available | Available | Available |
| Coverage | 23 operações | Igual | Igual |
| Objectos | 18 955 | Igual | Igual |
| Outros objectos alterados | — | Nenhum detectado | Nenhum detectado |

## 5. Conclusões demonstradas

No âmbito desta âncora, build, instalação e sequência:
1. O conteúdo da definição foi alterado e o artefacto IKP correspondente mudou.
2. A identidade observada (SourceObjectId e SourceIdentity) e os metadados projectados (MetadataFingerprint) permaneceram estáveis durante a alteração.
3. A definição permaneceu Available, independentemente do estado do DefinitionFingerprint.
4. A reposição recuperou exactamente os bytes originais de `prop` da baseline T0 recuperada e o artefacto original de 426 bytes, com o mesmo SHA-256.
5. A diferença de logical digest entre capturas não deve ser confundida com mudança de conhecimento: CaptureId é incorporado no pacote. Os hashes de ficheiros que incluem CaptureId também variam por essa razão.

## 6. Limitações e não conclusões

- O ensaio prova detecção de alteração e reposição do **conteúdo capturado**, não equivalência ou diferença semântica.
- DefinitionFingerprint permaneceu `Unavailable / NoValidatedPolicy` nas três capturas. Não foi validado qualquer normalizador semântico.
- A igualdade de MetadataFingerprint significa apenas que a projecção de metadata aprovada permaneceu igual; não prova igualdade de definição.
- O resultado não prova identidade persistente, estabilidade universal de stamps, matching cross-installation, `SameLogicalEntity`, `SameStandardEntity` ou classificação.
- Não foram testadas renomeação, eliminação, recriação, colisões, outras famílias PHC, outras instalações ou outras versões.
- A hipótese sobre a origem da alteração que antecedeu a primeira T0 não foi provada; a captura discrepante permanece preservada como evidência separada.

## 7. Alterações e implementação

Não foram alterados código, Scanner, collectors, workspace SQLite, IKP, schemas executáveis, migrations, materialização SQL Server ou RAG. Não foram criadas correspondências persistentes nem políticas automáticas. As gravações temporárias da definição foram realizadas pela interface PHC e a baseline original foi reposta e confirmada byte a byte.

**Conclusão:** T-B fecha a evidência experimental de alteração e reposição de uma definição `phc.hidden_grid` na instalação testada. A matriz D-023 pode registar esta evidência como específica do kind/âncora, mantendo a política de reconhecimento e matching em estado draft.