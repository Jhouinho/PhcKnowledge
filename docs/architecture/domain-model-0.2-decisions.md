# PHC Knowledge — Decisões de domínio 0.2

| Campo | Valor |
|---|---|
| Data | 2026-11-17 |
| Estado | **Decisões funcionais confirmadas; modelo consolidado para revisão, sem aprovação do schema físico** |
| Repositório | Jhouinho/PhcKnowledge |
| Branch de revisão | architecture/domain-model-0.2-review |
| Âmbito | Modelo de domínio da plataforma PHC Knowledge, alinhamento IKP 0.1 / P1 |
| Persistência central | Microsoft SQL Server Express |
| Persistência local | SQLite apenas para workspace/cache local |

Este documento consolida as decisões e propostas de domínio da revisão 0.2. As decisões confirmadas nesta etapa são: Observation permanece sem EntityIdentity até existir correspondência confirmada; a correção de contexto desencadeia reavaliação controlada dos elementos afetados; e o catálogo standard é publicado exclusivamente pela administração central, aceitando propostas das organizações. Mantêm-se também as decisões previamente alinhadas de identidade partilhável entre ambientes da mesma Installation mediante confirmação e de linhagem entre Installations sem fusão de identidades. Os restantes detalhes constituem o modelo conceptual a validar antes do desenho físico. Não representa uma migração de base de dados nem autoriza alterações ao Scanner, schemas JSON, SQLite ou collectors. O IKP 0.1 mantém a sua versão e formato; as alterações aqui descritas clarificam a semântica e as responsabilidades internas do PHC Knowledge.

## 1. Organização, instalação e ambiente

A hierarquia de domínio é:

`Organization 1 — N Installation 1 — N Environment`

- Organization é a fronteira de propriedade, administração e isolamento do conhecimento privado.
- Installation representa a unidade lógica PHC de uma organização.
- Environment representa um contexto operacional, normalmente Production, Test ou Development.
- DatabaseLocation representa a localização técnica e histórica de uma base de dados num Environment; não é identidade permanente da Installation.
- Uma cópia/restauro de Production pode representar outro Environment da mesma Installation, quando a continuidade é confirmada.
- Uma base de dados pertencente a outra empresa representa outra Installation.
- O Scanner/IKP não transporta OrganizationId, InstallationId, EnvironmentId ou DatabaseLocationId.

A continuidade entre ambientes não é inferida apenas por nome da base de dados, stamps, fingerprints ou semelhança estrutural.

## 2. Capture, PackageReceipt e artefactos

- Capture representa uma recolha lógica e é identificada por CaptureId.
- PackageReceipt representa cada receção física e é identificado por PackageId.
- Uma Capture pode ter vários PackageReceipts; cada PackageReceipt pertence a uma Capture.
- Repetir o mesmo PackageId com o mesmo checksum do ZIP é idempotente.
- O mesmo PackageId com checksum diferente é conflito de integridade; o pacote original nunca é substituído silenciosamente.
- PackageArtifact pertence a PackageReceipt.
- ObservationArtifact liga uma Observation aos artefactos que sustentam a sua definição.
- O ZIP original é preservado de forma imutável fora da base de dados relacional; SQL Server Express guarda metadados, estado e proveniência da receção.

## 3. Receção, materialização e associação de contexto

A receção do pacote não depende de a organização, instalação ou ambiente já serem conhecidos.

Sequência conceptual:

1. Receber, validar e arquivar o pacote original.
2. Registar Capture e PackageReceipt.
3. Numa fase posterior, materializar Observations, relações declaradas e referências a artefactos, mesmo que o contexto continue pendente.
4. Sugerir e confirmar o contexto através de CaptureContextAssignment.
5. Resolver correspondências e associar EntityIdentity apenas depois de contexto confirmado.
6. Executar comparações dependentes do ambiente quando a cobertura e a compatibilidade o permitirem.

A primeira fatia de implementação permanece Receive / Validate / Archive. A materialização, associação, resolução de identidade e comparação são fases posteriores e distintas.

## 4. CaptureContextAssignment e DatabaseLocation

- CaptureContextAssignment liga uma Capture a uma DatabaseLocation.
- Uma Capture pode não ter atribuição de contexto.
- O histórico admite várias atribuições, mas apenas uma atribuição ativa de cada vez.
- Corrigir contexto revoga a atribuição anterior e cria uma nova; não sobrescreve o histórico.
- A decisão regista autor, data, motivo e evidência.
- CaptureAssociationCandidate guarda sugestões não confirmadas.
- Quando DatabaseName é conhecido e ServerName não, cria-se uma DatabaseLocation provisória com DatabaseName e ServerName = null, no âmbito do Environment/Installation confirmado.
- A DatabaseLocation provisória é enriquecida quando o ServerName for posteriormente conhecido; a descoberta do servidor, por si só, não cria uma nova DatabaseLocation.
- O enriquecimento preserva o valor anterior, a data, a origem da evidência e o histórico da alteração; não reescreve a proveniência das Captures anteriores.
- Não se inventa nem infere o ServerName. A correspondência entre a localização provisória e a localização enriquecida deve ser sustentada por evidência e decisão controlada.
- A política de unicidade física e os índices de DatabaseLocation serão fechados no desenho SQL, não nesta revisão conceptual.

A reassociação não altera o pacote, Capture, PackageReceipt, PackageArtifact ou Observation. Desencadeia reavaliação controlada das correspondências e comparações afectadas, preservando resultados anteriores, contexto usado e motivo da reavaliação.

## 5. Observation e identidade lógica

- Observation representa um objeto efetivamente observado numa Capture.
- A plataforma atribui ObservationId próprio; não é enviado no IKP nem gerado pelo Scanner.
- A unicidade funcional é `UNIQUE(CaptureId, SourceObjectId)`.
- Observation não depende de InstallationId, EnvironmentId ou EntityIdentityId.
- Uma Observation pode ser materializada sem contexto confirmado e permanecer sem identidade lógica.
- EntityIdentity pertence a uma única Installation.
- EntityIdentity pode ser partilhada entre Environments da mesma Installation apenas após correspondência confirmada.
- EntityIdentity nunca é fundida automaticamente entre Installations.
- Quando não há correspondência confirmada, a Observation permanece sem EntityIdentity. Podem existir candidatos IdentityMatch pendentes ou rejeitados.
- Só se cria uma nova EntityIdentity quando existe decisão explícita de que se trata de uma entidade lógica nova; não se cria uma identidade provisória apenas para preencher uma relação.

No máximo uma IdentityMatch pode estar confirmada e ativa para cada Observation. Evidência, decisão, autor, data e justificação são preservados.

## 6. Estado por ambiente e ciclo de vida

IdentityEnvironmentState representa o estado de uma EntityIdentity num Environment. Os estados conceptuais são Present, NotObserved, PendingReview e Removed.

Removed é uma conclusão da plataforma, nunca um facto emitido pelo Scanner. Exige contexto confirmado, operações/capacidades compatíveis, scope comparável e cobertura relevante completa e bem-sucedida.

EntityIdentityLifecycleEvent é global à identidade e separado dos acontecimentos de presença/ausência num Environment. Uma alteração num ambiente não é automaticamente uma alteração global da identidade.

## 7. Relações declaradas e interpretações

- DeclaredRelation preserva os factos REL/DTL originais.
- RelationInterpretation guarda a interpretação versionada, parser, estado, confiança, AST, diagnósticos e referências resolvidas.
- REL/DTL permanecem no payload de relações; não são promovidas artificialmente a TechnicalObjectRecord no Object Index.
- Uma relação declarada pode ter várias ocorrências; SourceObjectId isolado não é chave suficiente para impor unicidade de relações.

## 8. Linhagem entre instalações

InstallationLineageEvent regista acontecimentos explícitos de cópia, restauro, migração ou separação entre Installations.

- Uma Installation pode ter várias origens e destinos ao longo do tempo; não se impõe uma árvore rígida.
- A linhagem pode gerar candidatos/evidências para comparação.
- A linhagem não funde EntityIdentity nem históricos e não implica sincronização.
- Transferências entre Organizations exigem autorização administrativa e auditoria.

## 9. Catálogo standard e conhecimento privado

O catálogo PHC standard é global e publicado exclusivamente pela administração central.

- StandardCatalog tem versões publicadas.
- CatalogEntry representa a identidade lógica de uma entrada standard.
- CatalogEntryVersion representa a versão concreta dessa entrada numa versão do catálogo.
- As organizações podem propor alterações, mas não publicar diretamente no catálogo global.
- O conhecimento privado é isolado por Organization, com entradas e versões próprias.
- Uma personalização observada numa instalação não altera automaticamente o catálogo standard.
- A promoção de conhecimento privado para standard exige curadoria e publicação central.

## 10. Persistência e limites tecnológicos

- Base de dados central: Microsoft SQL Server Express.
- SQLite: apenas workspace/cache local; não é a base de dados principal do Knowledge e não é lido diretamente pela plataforma.
- Arquivo dos ZIPs originais: armazenamento imutável fora da base relacional; SQL Server guarda metadados, estados, relações e proveniência.
- Não introduzir PostgreSQL.
- Esta revisão não escolhe ORM nem fecha o desenho físico das tabelas.
- Constraints, índices únicos filtrados, FKs e checks devem ser compatíveis com SQL Server.

## 11. Preparação para pesquisa e RAG

O modelo deve preservar proveniência, versões, evidências, contexto e relações para permitir pesquisa contextual e futura recuperação aumentada (RAG).

RAG é uma capacidade futura de consumo do conhecimento, não uma entidade obrigatória do modelo inicial. O conhecimento deve continuar utilizável por pesquisa textual e consultas estruturadas, independentemente da futura camada semântica.

## 12. Compatibilidade e não-alterações

Esta revisão:
- não altera ikpContractVersion 0.1;
- não altera schemas JSON, fixtures ou validador IKP;
- não altera SQLite, collectors, queries PHC/SQL ou código do Scanner;
- não altera SourceObjectId, SourceIdentity ou políticas de fingerprints já implementadas;
- não introduz InstallationId ou EnvironmentId no pacote;
- não implementa ainda a plataforma de receção, materialização ou resolução de identidade.

As alterações documentais clarificam a semântica do PHC Knowledge e corrigem pressupostos antigos que tratavam cada base de dados como uma Installation independente.

## 13. Questões reservadas ao desenho lógico/físico

1. A política de remoção está aprovada: duas capturas consecutivas, completas e comparáveis, seguidas obrigatoriamente de confirmação humana explícita e auditada. A confirmação nunca é automática.
2. Fechar a cardinalidade e índices de DatabaseLocation por Environment, incluindo a garantia de enriquecimento da localização provisória sem duplicação.
3. Definir o desenho físico do histórico de eventos e das projeções: catálogo/versionamento de eventos, payloads, chaves, índices, retenção e procedimento seguro de reconstrução. A regra funcional está aprovada: todas as alterações relevantes, decisões e avaliações geram eventos append-only; as projeções são reconstruíveis; evento e projeção são gravados na mesma transação SQL.
4. Definir permissões administrativas, papéis e auditoria para associações, decisões de identidade e transferências entre Organizations.
5. Definir a estratégia técnica de execução/retenção de reavaliações (fila/job, retries e observabilidade), sem alterar as regras funcionais.

Os estados e transições funcionais para contexto, identidade e presença estão descritos em [domain-model-0.2-state-transitions.md](domain-model-0.2-state-transitions.md). A política de remoção foi aprovada em 2026-11-17: duas ausências consecutivas completas e comparáveis e confirmação humana obrigatória. Os restantes pontos acima têm de ser fechados antes do schema relacional.

## 14. Artefactos da revisão e próximo passo

Os artefactos da revisão 0.2 são:

- [Diagramas ER e de classes — Scanner e plataforma central](diagramas-projeto-er-e-classes-2026-11-06.md), secção 5 para o domínio 0.2;
- [Roadmap visual PHC Knowledge](roadmap-visual-phc-knowledge-2026-11-06.md), secção «Revisão do roadmap — modelo de domínio 0.2»;
- [Estados e transições de contexto, identidade e presença](domain-model-0.2-state-transitions.md), proposta funcional para validação.

As decisões funcionais de remoção, DatabaseLocation e eventos/projeções estão aprovadas e refletidas nos documentos desta revisão. Permanecem por fechar a cardinalidade/índices de DatabaseLocation, o desenho físico dos eventos e projeções, permissões/auditoria e estratégia técnica de reavaliações. Depois preparar o desenho físico SQL Server Express (tabelas, PK/FK, índices, constraints, auditoria e transações) e os respetivos testes de integridade. A implementação continua a começar por Receive / Validate / Archive.
