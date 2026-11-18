# P2.B — Contrato proposto de âmbito e integração HTTP

| Campo | Valor |
|---|---|
| Fase | P2.B |
| Estado | **Proposta documental — aprovação final pendente** |
| Baseline de referência | `6e20d98c495651fbada41267f087c280fd639103` |
| Branch de referência | `main` |
| Data | 2026-11-18 |
| Natureza | Contrato de âmbito; não constitui autorização de implementação |

Este documento consolida as decisões de arquitetura e o contrato HTTP proposto para P2.B. Regista as escolhas já confirmadas e identifica os pontos ainda por fechar. Não altera nem substitui os contratos IKP 0.1, o processo de ingestão/recovery existente ou os contratos de domínio.

**P2.B não está aprovada para implementação.** A aprovação final deste documento — incluindo a resolução dos pontos em aberto — é uma condição prévia para criar branch de implementação ou alterar código.

## 1. Objetivo

Disponibilizar uma fronteira HTTP autenticada para receção de pacotes IKP e consulta do respetivo estado, reutilizando os serviços e repositórios de ingestão já existentes.

A fase deve introduzir a API sem antecipar materialização de dados de domínio, sem alterar o formato IKP 0.1 e sem expor estruturas internas de persistência ou arquivo.

## 2. Decisões arquiteturais confirmadas

As quatro decisões seguintes foram escolhidas pelo utilizador e ficam registadas como decisões da proposta P2.B:

| Tema | Decisão | Consequência |
|---|---|---|
| Processo | **Single** | Um único `WebApplication`/Kestrel aloja a API e os workers internos de recovery, cleanup e monitorização. |
| Upload | **Raw ZIP** | O endpoint recebe diretamente o corpo `application/zip`; não usa `multipart/form-data`. |
| Claim de organização | **Configurable** | O nome do claim que transporta o GUID da organização é configurável por ambiente. |
| Arquivo | **Global** | Mantém-se o arquivo content-addressed global por SHA-256; o isolamento por organização é assegurado pela metadata SQL. |

Estas escolhas não significam que todos os detalhes de configuração e operação estejam fechados. Os pontos em aberto encontram-se na secção 10.

## 3. Baseline técnica e limites existentes

A baseline indicada contém o fluxo de ingestão por serviço, o arquivo imutável e o modelo de receipts/tentativas com consultas org-scoped.

Factos relevantes para o desenho:

- `IkpPackageIngestionService.ReceiveValidateArchiveAsync` recebe a organização através do caller. Na integração HTTP, esse valor tem de ser obtido exclusivamente do principal autenticado.
- As consultas `GetAttemptAsync(organizationId, attemptId)` e `GetByPackageIdAsync(organizationId, packageId)` aplicam o filtro de organização na consulta SQL.
- `GetCompletedReceiptAsync(organizationId, attemptId)` também está org-scoped, mas não será exposto como endpoint nem o seu `PackageReceipt` será serializado para o cliente.
- `PackageIngestionStatus` inclui dados internos, nomeadamente `StorageKey` e `LastErrorCode`; não pode ser devolvido diretamente como DTO HTTP.
- O Host atual é baseado em Generic Host / `Microsoft.NET.Sdk.Worker`, não em `WebApplication`; a evolução para HTTP é uma alteração estrutural do bootstrap.
- Os workers de recovery e cleanup continuam a ser necessários e devem permanecer ativos no processo único.
- O arquivo usa chaves content-addressed globais no formato `sha256/{2}/{hash}.ikp.zip`. A separação lógica entre organizações reside na metadata SQL.

A existência de serviços de ingestão e workers não significa que o Host atual já esteja preparado para receber HTTP: a integração Kestrel, autenticação, endpoints, limites de request e respetivos testes ainda não existem nesta baseline.

## 4. Processo e composição do Host

Evoluir `PhcKnowledge.Host` para um único `WebApplication` com Kestrel.

O processo deve alojar:
- endpoints HTTP de ingestão e consulta;
- os workers existentes de recovery, cleanup e monitorização;
- as dependências de ingestão, validação, arquivo e repositórios necessárias à API.

Não criar um segundo processo/serviço nesta fase. A alteração do bootstrap deve preservar o comportamento e o ciclo de vida dos workers já validados.

## 5. Autenticação, autorização e identidade da organização

### 5.1 Modelo de autenticação

- OAuth2 Client Credentials, com JWT Bearer emitido por um Identity Provider externo.
- Validação do token segundo a configuração do IdP, incluindo issuer/authority e audience.
- Scopes propostos:
  - `ikp.upload` — submissão de pacotes;
  - `ikp.read` — consulta de tentativas e pacotes.

Não será criado um mecanismo proprietário para emitir Bearer Tokens.

### 5.2 Resolução de OrganizationId

A organização é derivada exclusivamente do principal autenticado:

1. Ler o claim cujo nome está configurado no ambiente.
2. Validar que o valor é um GUID não vazio.
3. Converter o valor para `OrganizationId`.
4. Passar esse valor aos serviços e repositórios org-scoped.

Nunca aceitar `OrganizationId` do body, query string ou headers enviados pelo cliente. Claim ausente, inválido ou não convertível não pode resultar numa organização por defeito.

O nome concreto do claim, Authority, Audience e a configuração de scopes dependem do Identity Provider e permanecem por fechar.

## 6. Contrato HTTP proposto

Os paths e verbos seguintes são a proposta de superfície mínima da API.

### 6.1 Receber pacote

`POST /api/v1/ikp/packages`

- Autorização: scope `ikp.upload`.
- Content-Type: `application/zip`.
- Body: bytes do pacote ZIP IKP.
- Organização: resolvida a partir do principal autenticado.
- Transporte: streaming do request para o serviço de ingestão, sem carregar o ZIP completo em memória e sem multipart.

O endpoint deve reutilizar `IkpPackageIngestionService.ReceiveValidateArchiveAsync`. Não deve duplicar validação, registo de tentativa, arquivo ou conclusão de receipt.

### 6.2 Consultar tentativa

`GET /api/v1/ikp/attempts/{attemptId}`

- Autorização: scope `ikp.read`.
- Serviço de leitura: `GetAttemptAsync(organizationId, attemptId)`.
- Tentativa inexistente ou pertencente a outra organização: `404 Not Found`.

### 6.3 Consultar pacote

`GET /api/v1/ikp/packages/{packageId}`

- Autorização: scope `ikp.read`.
- Serviço de leitura: `GetByPackageIdAsync(organizationId, packageId)`.
- Pacote inexistente ou pertencente a outra organização: `404 Not Found`.

Não expor endpoints para `GetCompletedReceiptAsync`, `PackageReceipt`, claim/complete de leases ou operações internas de recovery.

## 7. DTOs e fronteira de informação

Criar DTOs HTTP próprios com allowlist explícita. Não serializar diretamente entidades, records ou resultados internos do Import.

### 7.1 Campos permitidos propostos

| Campo | Exposição |
|---|---|
| `AttemptId` | Permitida |
| `PackageId` | Permitida |
| `CaptureId` | Permitida |
| `Status` | Permitida |
| `AttemptCount` | Permitida |
| `ObjectCount` | Permitida |
| `ArtifactCount` | Permitida |
| `CreatedAtUtc` | Permitida |
| `UpdatedAtUtc` | Permitida |
| `CompletedAtUtc` | Permitida |

A presença e nulabilidade de cada campo dependem do estado e dos dados efetivamente disponíveis no read model. Não fabricar valores para campos que não existam num determinado resultado.

### 7.2 Campos internos que não podem ser expostos

Não incluir nos DTOs públicos:

- `OrganizationId`;
- `StorageKey`;
- `LastErrorCode` ou `LastErrorMessage`;
- `ValidationSummaryJson`;
- `ZipSha256`;
- `StagingKey`;
- `ReceiptId`;
- paths locais, detalhes de staging, mensagens de exceção ou conteúdo interno de validação.

Mensagens de validação podem conter paths ou outros detalhes internos. A fronteira HTTP deve mapear erros para respostas públicas controladas, sem serializar mensagens internas.

## 8. Respostas HTTP propostas

A tabela seguinte é uma proposta de mapeamento. A correspondência exata entre resultados, estados e exceções atuais do serviço tem de ser validada no código antes da implementação.

| Situação | Código proposto | Observações |
|---|---:|---|
| Submissão concluída, nova ou idempotente | `200 OK` | O serviço atual não transporta para o resultado HTTP a informação necessária para distinguir com segurança uma tentativa nova de uma já existente. |
| Pacote aceite, mas ainda pendente | `202 Accepted` | Incluir `AttemptId`, estado público e `statusUrl`. |
| Request estruturalmente inválido | `400 Bad Request` | Sem detalhes internos. |
| Token ausente, inválido ou expirado | `401 Unauthorized` | Tratado pelo middleware de autenticação. |
| Token sem scope necessário | `403 Forbidden` | Tratado pela autorização. |
| Tentativa/pacote inexistente ou de outra organização | `404 Not Found` | Não revelar a existência noutra organização. |
| PackageId/SHA incompatíveis ou tentativa em `NeedsAttention` | `409 Conflict` | Devolver código público estável e, quando exista, `AttemptId`; nunca a mensagem interna. |
| Inconsistência interna entre tentativa, receipt e arquivo | `500 Internal Server Error` | Não a apresentar como conflito do cliente; requer diagnóstico interno. |
| Request acima do limite | `413 Payload Too Large` | Inclui rejeição no limite do servidor. |
| Content-Type não suportado | `415 Unsupported Media Type` | Esperado `application/zip`. |
| ZIP/pacote IKP inválido | `422 Unprocessable Entity` | Resposta pública sem ValidationSummaryJson ou paths. |
| Erro interno inesperado | `500 Internal Server Error` | Detalhes apenas em logs internos protegidos. |

Mapeamento confirmado pelo fluxo atual de `IkpPackageIngestionService`:
- `PackageReceiptPendingException` → `202 Accepted`; a tentativa foi registada e aguarda recovery ou não conseguiu obter lease/processamento imediato.
- `PackageReceiptNeedsAttentionException` → `409 Conflict`; a tentativa existe, mas exige intervenção. A API devolve apenas o identificador e o estado público.
- `PackageReceiptConflictException` por PackageId/SHA incompatíveis → `409 Conflict`.
- Falhas de consistência interna (por exemplo, tentativa concluída sem blob ou receipt correspondente) devem resultar em `500 Internal Server Error`, não num erro imputável ao cliente. A implementação terá de distinguir estas causas internamente sem inspecionar texto de exceções.
- `IkpPackageValidationException` → `422 Unprocessable Entity` para pacote vazio/inválido; o limite de tamanho é imposto também no Kestrel para que requests excessivos sejam rejeitados como `413` antes de depender da exceção de validação.

O serviço atual não expõe no `IkpIngestionResult` a flag `PackageReceiptAttemptRegistration.AlreadyExists`. Por isso, todas as submissões concluídas usam `200 OK` nesta fase. Não acrescentar um campo ao resultado interno apenas para emitir `201` sem necessidade funcional aprovada.

## 9. Limites, armazenamento temporário e operação

### 9.1 Limites ZIP existentes no validador

| Limite | Valor atual identificado |
|---|---:|
| ZIP comprimido | 250 MiB |
| Conteúdo expandido | 500 MiB |
| Entrada individual | 100 MiB |
| Número máximo de entradas | 10.000 |
| Compression ratio | 200 |

O validador também aplica proteção contra path traversal através da normalização/validação de caminhos.

O limite do request body do Kestrel, o limite configurável do serviço de upload e o limite do validador têm de estar alinhados. Não basta manter apenas os limites internos do ZIP: o servidor HTTP tem de rejeitar requests excessivos antes de consumir recursos desnecessários.

### 9.2 Riscos operacionais a tratar

- O validador usa atualmente `Path.GetTempPath()`; a exposição HTTP exige avaliar espaço disponível, concorrência, isolamento e limpeza em falhas.
- Definir limites de concorrência e rate limiting para uploads.
- Definir timeouts e comportamento de cancelamento do request.
- Garantir limpeza de ficheiros temporários em sucesso, falha e cancelamento.
- TLS obrigatório na fronteira de acesso; pode terminar num reverse proxy.
- CORS só é necessário se um cliente browser consumir diretamente a API; não o ativar por defeito sem esse requisito.
- Definir health/readiness e comportamento de shutdown gracioso do Host.

## 10. Decisões e detalhes ainda por fechar

A revisão técnica propõe fechar os detalhes de implementação da seguinte forma. A seleção do fornecedor concreto de identidade e os valores específicos do ambiente de deployment não são decisões de arquitetura da aplicação e serão fornecidos na configuração de cada instalação.

1. **Identity Provider:** manter integração agnóstica ao fornecedor, baseada em JWT Bearer e metadata OIDC/JWKS. Authority/Issuer e Audience são configuração obrigatória por ambiente; não fixar um fornecedor nem valores no código.
2. **Claim de organização:** usar `organization_id` como nome convencional por defeito, mantendo `OrganizationClaimType` configurável. O valor tem de ser um GUID não vazio. Não aceitar fallback para outro claim nem organização default.
3. **Scopes:** exigir os scopes OAuth2 `ikp.upload` e `ikp.read`, lidos do claim `scope` (string separada por espaços), com validação explícita. Configurar a validação JWT sem mapeamentos implícitos de claims que alterem os nomes esperados.
4. **Mapeamento de estados:** adotar a tabela da secção 8 e os casos concretos aí descritos: `PendingArchive` → 202; `NeedsAttention` → 409; conflito de identidade do pacote → 409; inconsistência interna → 500; pacote inválido → 422; limite de request → 413.
5. **Idempotência:** adotar a semântica detalhada na secção 15. Submissões concluídas devolvem 200; pendentes devolvem 202; colisões parciais PackageId/SHA devolvem 409.
6. **Erros públicos:** usar catálogo HTTP estável próprio, sem expor `LastErrorCode`, `LastErrorMessage`, ValidationSummaryJson, paths ou exceções. Formato recomendado: Problem Details com `code`, `traceId` e `attemptId` opcional.
7. **Health/readiness:** disponibilizar `GET /health/live` e `GET /health/ready`, com respostas mínimas sem detalhes de infraestrutura.
8. **TLS e deployment:** assumir TLS terminado num reverse proxy confiável como configuração preferencial. Kestrel só confia em proxies explicitamente configurados; se não existir proxy, o próprio Kestrel termina TLS.
9. **Recursos temporários:** começar com uma submissão concorrente por processo, configurável. Aplicar admissão limitada, monitorização de espaço livre e quotas/reservas configuradas no deployment. O staging e o diretório temporário usado pelo validador têm de ser contabilizados em conjunto.
10. **CORS:** desativado por defeito. Só ativar origins explícitas se um cliente browser vier a consumir diretamente a API.

Estes valores fecham o contrato técnico proposto sem escolher um fornecedor de identidade, endereço de deployment, capacidade de disco ou origem browser que ainda não foram definidos.

## 11. Arquivo global e isolamento multi-organização

Manter o arquivo físico global content-addressed por SHA-256. Conteúdo idêntico pode corresponder ao mesmo blob físico, mesmo quando recebido por organizações diferentes.

Nesta fase:
- a metadata e as consultas continuam isoladas por `OrganizationId`;
- todas as consultas da API usam os métodos org-scoped existentes;
- a API não expõe `StorageKey`, paths nem URLs diretas para blobs;
- não introduzir diretórios ou namespaces físicos por organização.

A partilha física de bytes idênticos é uma decisão explícita desta proposta; não equivale a partilha de metadata, receipts ou permissões entre organizações.

## 12. Testes de aceitação obrigatórios

### Host e workers
- arranque e shutdown do `WebApplication`;
- workers de recovery, cleanup e monitorização continuam registados e funcionais;
- recuperação de tentativas pendentes após restart;
- ausência de regressões no comportamento do Host anterior.

### Autenticação e isolamento
- token ausente, inválido e expirado;
- scope insuficiente;
- claim de organização ausente, vazio ou malformado;
- organização derivada apenas do principal;
- tentativa e pacote de outra organização devolvem `404`;
- nenhuma entrada do cliente consegue substituir a organização autenticada.

### HTTP e DTOs
- upload raw `application/zip`;
- rejeição de multipart e Content-Type não suportado;
- respostas de sucesso, pendência, reenvio idempotente e conflito;
- mapeamento dos erros sem mensagens internas;
- verificação explícita de que os DTOs não contêm campos internos proibidos.

### Limites e concorrência
- request acima do limite Kestrel;
- ZIP acima dos limites do validador;
- ZIP inválido e path traversal;
- uploads concorrentes;
- disco temporário insuficiente, cancelamento e falha durante a validação;
- limpeza de temporários após os diferentes resultados.

### Regressão
- testes unitários de validator e ingestion;
- testes de recovery e workers;
- testes SQL Server de isolamento org-scoped e receipts;
- validadores independentes do contrato IKP 0.1 e restantes verificações já existentes.

## 13. Fora do âmbito de P2.B

Ficam explicitamente excluídos:

- materialização de Observation, REL ou DTL;
- minting de Bearer Token proprietário;
- alteração do processo de ingestão/recovery já validado, para além da integração necessária à fronteira HTTP;
- exposição de `GetCompletedReceiptAsync` ou de `PackageReceipt` completo;
- criação de novo schema de Organizations ou Users;
- integração ou alteração do Scanner;
- alterações ao contrato IKP 0.1;
- armazenamento físico separado por organização;
- remoção ou substituição do caminho legado `IPackageReceiptRepository` sem decisão específica;
- migrations de base de dados, salvo se uma necessidade concreta for identificada e aprovada separadamente.

## 14. Critério de aprovação e próximo passo

P2.B só fica pronta para implementação quando:

1. os quatro princípios arquiteturais confirmados nesta proposta forem preservados;
2. os pontos da secção 10 estiverem decididos ou explicitamente adiados com justificação e impacto;
3. o contrato de endpoints, DTOs, estados e erros estiver fechado;
4. os testes de aceitação e as exclusões forem aceites;
5. o utilizador der aprovação final explícita.

Até essa aprovação, este documento é apenas uma proposta de âmbito. Não autoriza criação de branch de implementação, alterações ao código, migrations, configuração de produção ou integração com um Identity Provider.


## 15. Fecho técnico dos pontos anteriormente em aberto

Esta secção regista a revisão do código da baseline e as decisões recomendadas para retirar ambiguidades antes da aprovação final.

### 15.1 Identity Provider e validação JWT

A aplicação não deve ficar acoplada a um fornecedor específico. A configuração obrigatória por ambiente será:
- `Authentication:Authority` — issuer/authority do IdP;
- `Authentication:Audience` — audience destinada à API;
- `Authentication:OrganizationClaimType` — por defeito `organization_id`;
- configuração de HTTPS metadata/JWKS conforme o ambiente.

O middleware valida assinatura, issuer, audience e validade temporal. A aplicação não emite tokens e não guarda client secrets de consumidores. A escolha do IdP e o registo dos clients pertencem ao deployment/operador.

### 15.2 Claim e scopes

- Claim de organização por defeito: `organization_id`; nome configurável por ambiente.
- Valor: GUID não vazio.
- Scopes: `ikp.upload` e `ikp.read`.
- Interpretar o claim OAuth2 `scope` como lista delimitada por espaços; validar o scope exigido em cada endpoint.
- A validação deve usar os nomes de claims esperados sem inbound claim mapping implícito.
- Falta ou invalidade do claim de organização deve impedir o acesso aos endpoints de negócio; nunca usar organização default.

### 15.3 Mapeamento dos estados reais

O fluxo de ingestão observado no código atual apresenta três estados persistidos relevantes:
- `PendingArchive`;
- `NeedsAttention`;
- `Completed`.

Comportamento:
- `PendingArchive`: a tentativa pode ser processada imediatamente ou estar ocupada por uma lease ativa/aguardar retry. Se o serviço lançar `PackageReceiptPendingException`, responder 202 e permitir consulta por AttemptId.
- `NeedsAttention`: o serviço lança `PackageReceiptNeedsAttentionException`. Responder 409 com `attemptId` e estado público `NeedsAttention`; não publicar o motivo interno.
- `Completed`: o serviço verifica o blob arquivado e recupera o receipt org-scoped. Responder 200 com o DTO público.
- conflito de PackageId/SHA: responder 409.
- pacote inválido: responder 422.
- falha de integridade interna (por exemplo, estado Completed sem arquivo ou receipt): responder 500, com diagnóstico apenas nos logs.

A implementação não deve mapear erros através de comparação de mensagens de exceção. Se as exceções atuais não distinguirem conflito de input de inconsistência interna, introduzir classificação interna específica na fronteira HTTP, sem alterar a semântica nem a sequência do processo de ingestão.

### 15.4 Idempotência e reenvios

A implementação SQL atual de `CreateOrGetPendingAsync` procura tentativa existente na mesma organização quando coincide `PackageId` **ou** `ZipSha256`. Depois, `EnsureSameAttempt` exige correspondência do `OrganizationId`, `PackageId`, `ZipSha256` e `StorageKey`.

Contrato resultante:
- Mesmo OrganizationId + mesmo PackageId + mesmo SHA: reutilizar a tentativa existente.
- Tentativa existente `PendingArchive` sem lease ativa: a operação pode atualizar a referência de staging para o novo upload e tentar processá-la.
- Tentativa `PendingArchive` com lease ativa ou sem conclusão imediata: responder 202 com o AttemptId existente.
- Tentativa `NeedsAttention`: responder 409 com o AttemptId existente; não criar uma segunda tentativa nem contornar a intervenção necessária.
- Tentativa `Completed`: verificar arquivo e receipt; se ambos estiverem consistentes, responder 200 com o resultado já concluído.
- Mesmo PackageId com SHA diferente, ou mesmo SHA com PackageId diferente, na mesma organização: responder 409; não criar outra tentativa.
- Outra organização: pode ter a sua própria tentativa e metadata. O blob físico pode ser partilhado por SHA, conforme a decisão de arquivo global.

A chave lógica de idempotência é, portanto, a combinação consistente de organização, PackageId e SHA, com colisões parciais tratadas como conflito. Não introduzir uma nova tabela ou chave HTTP de idempotência nesta fase.

### 15.5 Catálogo de erros públicos

Usar respostas Problem Details com códigos públicos estáveis, por exemplo:
- `invalid_package`;
- `upload_too_large`;
- `package_conflict`;
- `ingestion_pending`;
- `ingestion_needs_attention`;
- `not_found`;
- `unauthorized`;
- `forbidden`;
- `internal_error`.

O payload pode incluir `status`, `title`, `code`, `traceId` e `attemptId` quando aplicável. Não incluir `LastErrorCode`, `LastErrorMessage`, ValidationSummaryJson, paths, SQL details ou texto de exceções. Os códigos acima são da API e não espelham os códigos internos do receipt.

### 15.6 Health e readiness

- `GET /health/live`: indica apenas que o processo HTTP está vivo.
- `GET /health/ready`: indica que a aplicação completou o preflight de schema e consegue aceder às dependências essenciais e aos diretórios configurados.
- Respostas não autenticadas, pequenas e sem connection strings, paths, nomes de servidor/base de dados ou detalhes de exceções.
- Falha de readiness deve retirar a instância do tráfego; liveness não deve falhar apenas porque uma dependência temporária está indisponível.

### 15.7 TLS, proxy e CORS

- Preferência: TLS termina no reverse proxy/ingress de deployment.
- Kestrel confia apenas nos proxies/rede explicitamente configurados para forwarded headers; nunca aceitar indiscriminadamente headers `X-Forwarded-*` de qualquer origem.
- Sem reverse proxy, configurar TLS diretamente no Kestrel.
- CORS permanece desativado por defeito; se necessário, configurar allowlist explícita de origins, métodos e headers. Não usar wildcard com credenciais.

### 15.8 Concorrência e disco temporário

- Limite inicial recomendado: uma submissão concorrente por instância, configurável por `Upload:MaxConcurrentUploads`.
- Usar admissão limitada antes de iniciar staging/validação; quando o limite estiver ocupado, responder 429 ou 503 com `Retry-After`, sem acumular requests ilimitados.
- O orçamento de disco tem de considerar simultaneamente o ZIP staged (até 250 MiB) e a expansão temporária do validador (até 500 MiB) por upload ativo, mais margem operacional.
- Configurar o diretório temporário do processo de forma explícita e monitorizar o mesmo volume que contém staging/temporários.
- Definir reserva mínima de espaço livre e alertas no deployment; não codificar uma quota universal em GB sem conhecer o volume e a capacidade do ambiente.
- Garantir cancelamento, limpeza e recovery compatíveis com os mecanismos atuais. Não apagar staging referenciado por tentativa pendente.

### 15.9 CORS e tipo de cliente

A API é server-to-server por defeito. A existência de um cliente desktop/WebView não implica automaticamente CORS: CORS só é aplicado por browsers a pedidos cross-origin. Se no futuro existir consumo direto a partir de uma origem browser, essa origem tem de ser explicitamente aprovada e adicionada à allowlist.

### 15.10 Consequências para o contrato e aprovação

Com estas decisões:
- os dez pontos anteriormente abertos ficam tecnicamente resolvidos ao nível do contrato;
- a escolha do IdP concreto, Authority, Audience, certificados/chaves, proxies, origins browser e capacidade de disco permanecem parâmetros de deployment, não lacunas do desenho;
- a semântica HTTP evita alterações ao serviço de ingestão apenas para distinguir primeira submissão de repetição;
- a implementação continua sujeita a aprovação final explícita.

A revisão foi feita contra o código disponível na baseline indicada, em particular `IkpPackageIngestionService`, `IPackageReceiptAttemptRepository`, `SqlServerPackageReceiptAttemptRepository`, `PackageReceiptAttempt`, `PackageIngestionStatus`, `ReceiptHostSettings` e `PhcKnowledge.Host/Program.cs`. Não foram feitas alterações de código nesta revisão.
