# IKP — Captura completa e materialização incremental

**Data:** 2026-11-20  
**Estado:** decisão funcional aprovada (D-016); desenho técnico ainda por fechar.  
**Decisão relacionada:** `DECISIONS.md`, D-016.  
**Âmbito:** relação entre o conteúdo transportado pelo Scanner e a escrita do conhecimento no SQL Server.

## 1. Regra aprovada

O Scanner transporta sempre a captura completa da instalação, incluindo os artefactos descritivos, mesmo que o conteúdo já tenha sido enviado numa captura anterior.

A comparação não é responsabilidade do Scanner. É feita pelo PHC Knowledge durante a materialização, no destino.

- Conteúdo novo para a instalação: persistir.
- Conteúdo já persistido e rigorosamente igual: não voltar a gravar o conteúdo.
- Conteúdo existente com alterações: persistir a nova versão, preservando a anterior e a respetiva proveniência.
- Captura repetida sem alterações: registar a nova captura e a sua ligação ao conteúdo já conhecido, sem duplicar a escrita do conteúdo.

O ZIP continua a ser um transporte completo e autónomo. Não depende da disponibilidade de um scan anterior nem de comunicação direta Scanner→Knowledge. O operador transfere manualmente o ZIP e faz o upload no Portal.

## 2. Separar captura, observação e conteúdo persistido

A implementação futura deve distinguir três níveis:

1. **Captura:** cada execução/ZIP recebido, com Receipt, CaptureId, instalação, data, análise e decisão próprios.
2. **Observação na captura:** prova de que determinado objeto/artefacto foi encontrado naquela instalação e naquela captura. Esta associação tem de ser preservada mesmo quando o conteúdo é igual ao já conhecido.
3. **Conteúdo materializado:** metadados, definição ou bytes efetivamente armazenados no conhecimento da instalação. Pode ser reutilizado por várias observações da mesma instalação quando for rigorosamente igual.

Assim, evitar uma segunda escrita do conteúdo não significa ignorar a captura nem perder o histórico de quando foi observado.

## 3. Comparação e identidade

A comparação deve ser feita dentro do âmbito da mesma instalação. Não fundir automaticamente conhecimento de instalações/clientes diferentes, mesmo quando os bytes ou hashes coincidem.

Usar a identidade de origem estável quando existir evidência suficiente, em conjunto com fingerprints separados:

- `MetadataFingerprint` para metadados canónicos;
- `DefinitionFingerprint` para conteúdo original de definição, quando aplicável;
- SHA-256 e tamanho para integridade dos bytes transportados.

Não usar `SourceObjectId` como identidade permanente: é uma referência dentro da captura. Quando não existir identidade forte, manter o registo limitado à captura em vez de inventar uma identidade transversal.

A igualdade de hash ajuda a verificar igualdade de conteúdo; não substitui a identidade de origem nem a proveniência.

## 4. Artefactos descritivos de campos personalizados

Os ficheiros descritivos produzidos para campos físicos `u_*` continuam a ser transportados em todas as capturas.

Não devem ser classificados como `source-definition` quando representam apenas descrição de metadados e o Object Index indica `definition.status = NotCollected`. O contrato IKP deverá atribuir-lhes um papel próprio de evidência/metadados (nome final e compatibilidade do schema ainda por definir), mantendo a ligação ao objeto e à captura.

A materialização pode evitar voltar a persistir os mesmos bytes/metadados quando os fingerprints e a identidade de origem confirmarem igualdade, mas deve preservar a observação da captura atual.

## 5. Regras de segurança e histórico

- A ausência de um objeto numa captura posterior não significa, por si só, que foi eliminado da instalação.
- Não apagar nem substituir o histórico de versões quando o conteúdo muda.
- Não partilhar automaticamente observações ou personalizações entre instalações.
- Não promover conteúdo de uma instalação a PHC Standard.
- A materialização só pode ser considerada Verified quando todos os elementos da captura abrangidos pelo contrato estiverem contabilizados, incluindo os que reutilizam conteúdo já persistido.
- O ZIP só pode ser eliminado depois de materialização verificada e confirmação de persistência/recuperabilidade, de acordo com D-007.

## 6. Consequências para o desenho e testes

Antes da implementação, o contrato técnico deve especificar:

1. A identidade estável por tipo de objeto e por instalação, respeitando os limites de P1/P1B.
2. Como associar uma nova observação/captura a conteúdo já persistido sem duplicar bytes ou versões.
3. Como distinguir igualdade de metadados, igualdade de definição e igualdade apenas de bytes.
4. Como criar uma nova versão quando só os metadados mudam, só a definição muda ou ambos mudam.
5. Como tratar objetos sem identidade forte e artefactos sem objeto proprietário inequívoco.
6. Como reconciliar concorrência: duas capturas da mesma instalação podem ser materializadas em simultâneo.
7. Como contabilizar todos os itens no relatório de materialização, mesmo quando a ação é `Unchanged/Reused` em vez de `Inserted/Updated`.

Testes mínimos:
- primeira captura insere o conteúdo;
- segunda captura idêntica não volta a gravar conteúdo, mas cria a observação/proveniência da nova captura;
- alteração apenas de metadados;
- alteração apenas da definição;
- alteração de ambos;
- mesmo conteúdo em instalações diferentes não funde a propriedade nem a proveniência;
- ausência numa captura posterior não elimina conteúdo anterior;
- repetição concorrente e retry não criam duplicados;
- artefacto descritivo `phc.user_field` é transportado e classificado como evidência/metadados, não como definição de código.

## 7. Limites desta decisão

D-016 fecha a regra funcional de captura completa e escrita incremental. Não aprova, por si só:

- nomes físicos de tabelas/entidades;
- algoritmo final de identidade por cada ObjectKind;
- nomes de novos roles no schema IKP;
- granularidade de batches/transações;
- implementação, migrations ou alteração dos collectors/exporter;
- política de reversão/correção de materializações.

Esses pontos continuam sujeitos a desenho técnico e aprovação antes de implementação.

**Estado de implementação:** não implementado. Esta publicação atualiza apenas a documentação; não altera código, schemas, migrations ou testes.

## 8. Evidência técnica atual para campos `u_*`

A auditoria do código atual confirmou que:

- cada campo `u_*` é recolhido de `sys.columns` e recebe um ficheiro descritivo `.txt` no workspace;
- os mesmos dados principais também são guardados em `SummaryJson` e projetados para o Object Index;
- o `MetadataFingerprint` cobre schema, tabela, coluna, tipo, comprimento, precisão, escala e nulabilidade;
- existe `SourceIdentity` baseada em `sqlserver.column_id`, scoped à base de dados e ao `object_id` da tabela pai. Na materialização, deve ser combinada com a identidade da instalação;
- o resolver classifica corretamente a definição do campo como `NotCollected`, pois o ficheiro é descritivo e não a definição original da coluna.

O workspace não possui atualmente um purpose/role semântico explícito para este ficheiro e o exporter atribui-lhe `source-definition` por omissão. O desenho futuro deverá introduzir uma classificação explícita e preservar o ficheiro no ZIP; não deve inferir a finalidade apenas a partir do nome/extensão do caminho.

Esta evidência é uma fotografia do código auditado, não uma autorização para alterar o collector ou o contrato nesta etapa.


## 9. Outros artefactos que exigem preservação semântica

A auditoria identificou dois casos adicionais no collector PHC:

- **FORMB:** o ficheiro transportado contém `descricao` e `ordem`, que não estão integralmente representadas no Object Index atual. Deve manter-se no ZIP como evidência/configuração descritiva, não ser descartado nem presumido como código executável.
- **FORMC:** o ficheiro atual contém a expressão `Xexpressao` extraída e formatada de `formc.prop`; o valor original completo de `prop` não é preservado no percurso atual. O desenho deve transportar separadamente o original e a representação derivada, sem confundir os dois.

A classificação final dos roles só pode ser fechada depois de inventariar, por ObjectKind/collector, os dados presentes no Object Index, os bytes originais, as representações derivadas e os metadados adicionais. D-016 exige captura completa; não autoriza descartar informação apenas por parecer descritiva.


## 10. Classificação parcial de outros conteúdos

A auditoria confirma que:
- CSSJS transporta `source` e `sourcedefault`;
- ESCR transporta `expressao`, `cmen` e `cmenhtml`;
- JSU transporta o JavaScript;
- estes corpos não estão integralmente representados no Object Index e devem permanecer como conteúdo-fonte;
- FORMB é conteúdo descritivo/configuracional, embora o resolver atual o projete como definição `Available`;
- FORMC é uma representação formatada de `Xexpressao`, não o original integral de `formc.prop`, embora o resolver atual também o projete como `Available`.

A futura materialização e o contrato IKP devem classificar o conteúdo pela sua finalidade real, não apenas pela existência de `DefinitionArtifactId`. A matriz ainda é parcial e não fecha a taxonomia final de roles.
