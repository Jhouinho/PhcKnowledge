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
| Novo pacote recebido e concluído | `201 Created` | Resposta com DTO público e referência de consulta, se aplicável. |
| Pacote aceite, mas ainda pendente | `202 Accepted` | Incluir `AttemptId`, estado público e `statusUrl`. |
| Reenvio idempotente já concluído | `200 OK` | Devolver o read model público do resultado concluído. |
| Request estruturalmente inválido | `400 Bad Request` | Sem detalhes internos. |
| Token ausente, inválido ou expirado | `401 Unauthorized` | Tratado pelo middleware de autenticação. |
| Token sem scope necessário | `403 Forbidden` | Tratado pela autorização. |
| Tentativa/pacote inexistente ou de outra organização | `404 Not Found` | Não revelar a existência noutra organização. |
| Conflito de estado/ingestão | `409 Conflict` | Mapeamento a fechar com as exceções reais. |
| Request acima do limite | `413 Payload Too Large` | Inclui rejeição no limite do servidor. |
| Content-Type não suportado | `415 Unsupported Media Type` | Esperado `application/zip`. |
| ZIP/pacote IKP inválido | `422 Unprocessable Entity` | Resposta pública sem ValidationSummaryJson ou paths. |
| Erro interno inesperado | `500 Internal Server Error` | Detalhes apenas em logs internos protegidos. |

Não assumir que qualquer estado pendente ou exceção atual pode ser convertido automaticamente em `202`. Em particular, os casos `NeedsAttention` e `PendingArchive` têm de ser analisados à luz do fluxo real e das respetivas garantias de recuperação.

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

Estes pontos não estão decididos por este documento e devem ser resolvidos antes da aprovação final:

1. **Identity Provider:** identificar o IdP e fixar Authority/Issuer, Audience e validação de tokens.
2. **Claim de organização:** fixar o nome concreto do claim configurável e o formato esperado do GUID.
3. **Scopes:** confirmar que o IdP emite os scopes `ikp.upload` e `ikp.read` com a semântica proposta.
4. **Mapeamento de estados:** definir a correspondência exata entre resultados/exceções do serviço e códigos HTTP, incluindo `NeedsAttention`, `PendingArchive` e conflitos.
5. **Idempotência:** fechar o comportamento para reenvio quando já existe uma tentativa pendente, em recovery ou concluída, incluindo a resposta HTTP e o read model devolvido.
6. **Erros públicos:** decidir se existe catálogo público de códigos de erro. `LastErrorCode` não deve ser exposto diretamente sem uma allowlist e semântica pública aprovadas.
7. **Health/readiness:** definir endpoints, informação exposta e critérios de readiness.
8. **TLS e deployment:** confirmar se TLS termina no Kestrel ou num reverse proxy confiável e como são encaminhados os headers de proxy.
9. **Recursos temporários:** definir limites de concorrência, quotas/alertas de disco temporário e política de limpeza.
10. **CORS:** confirmar se algum cliente browser consumirá diretamente a API.

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
