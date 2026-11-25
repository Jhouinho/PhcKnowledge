# Investigação funcional PHC — BO, BI e movimentos de stock

**Data:** 25-11-2026  
**Estado:** investigação documental exploratória; conclusões circunscritas ao script PHC e à documentação consultados.  
**Base de referência:** `CSDefault202603`.  
**Âmbito:** cabeçalho de dossiers internos (`BO`), linhas (`BI`), informação complementar (`BO2`), configuração (`TS`/`TS2`) e movimentos/reservas (`SL`, `ST`, `SA`).

## 1. Objectivo e método

Reconstruir o circuito funcional dos dossiers internos sem repetir o inventário já realizado. Foram cruzadas três fontes:

1. Script de triggers PHC disponibilizado na conversa e mantido no repositório em `docs/sources/triggers phc.sql`.
2. Enciclopédia PHC, ZIP disponibilizado pelo utilizador.
3. Resultados anteriormente obtidos na base `CSDefault202603`: estrutura, relações PHC REL/DTL e configuração de `TS`/`TS2`.

A análise distingue:
- **Código observado:** operação e condição explícitas no trigger.
- **Documentação PHC:** explicação funcional publicada na Enciclopédia.
- **Configuração observada:** valores consultados na CS Default.
- **Interpretação:** leitura conjunta das fontes.
- **Por validar:** comportamento que exige consulta adicional ou ensaio funcional.

Não se assume que o comportamento seja universal a todas as versões, módulos ou instalações PHC.

## 2. Fontes consultadas

### 2.1 Script de triggers

Foi localizado o código dos seguintes triggers no ficheiro de triggers enviado pelo utilizador:

| Tabela | Trigger | Evento | Linha na cópia textual recebida |
|---|---|---:|---:|
| BO | `tr_bo_insert` | INSERT | 389 |
| BO | `tr_bo_update` | UPDATE | 2448 |
| BO | `tr_bo_delete` | DELETE | 1973 |
| BO2 | `tr_bo2_update` | UPDATE | 11765 |
| BI | `tr_bi_insert` | INSERT | 6535 |
| BI | `tr_bi_delete` | DELETE | 11588 |
| BI | `tr_bi_update` | UPDATE | 11592 |

As definições no script estão declaradas com `WITH ENCRYPTION`, mas o texto fornecido é legível. A legibilidade deste ficheiro não significa que a captura IKP tenha conseguido recolher as definições SQL dos triggers.

### 2.2 Enciclopédia PHC

Foram consultados os artigos:
- **Dossiers Internos**;
- **Configuração de Dossiers**;
- **Constituição da Tabela de Dossiers**;
- **Funcionamento do Stock Cativo e Pré-cativo**;
- **Registo de Dossiers**;
- **Opções na cópia de Dossiers**.

O ZIP oficial não é copiado para este repositório. Este relatório contém apenas sínteses próprias e referências aos títulos dos artigos.

### 2.3 CS Default 202603

Resultados anteriormente recolhidos:
- `BO`, `BI`, `BI2` sem registos transaccionais na base standard.
- `TS` e `TS2` com nove registos de configuração.
- Tipos de dossier identificados em `TS`: 1, 2, 3, 4, 5, 6, 7, 10 e 11.
- Configuração de stock já observada: `ndos` 1–3 sem movimento de stock; `ndos` 4–7 com `stocks=1` e códigos `cmstocks` 49, 98, 54 e 97, respectivamente; `ndos` 10–11 sem movimento de stock.

A ausência de documentos na CS Default impede validar empiricamente os efeitos dos triggers com documentos reais. Os valores de configuração devem ser novamente consultados se for necessário fechar cada ramo individual.

## 3. Modelo funcional e ligações

### 3.1 Cabeçalho e linhas

| Elemento | Ligação observada | Leitura |
|---|---|---|
| BO → BO2 | `BO.bostamp = BO2.bo2stamp` | Informação complementar do cabeçalho e estado `anulado` |
| BO → BI | `BI.bostamp = BO.bostamp` | Linhas pertencentes ao dossier |
| BI → SL | `SL.bistamp = BI.bistamp` | Movimentos de stock originados numa linha |
| BI → BI de origem | `BI.obistamp = BI.bistamp` da linha de origem | Ligação de satisfação/fornecimento entre linhas de dossiers |
| TS → TS2 | `TS.tsstamp = TS2.ts2stamp` | Configuração complementar do tipo de dossier |

As relações BO/BI/SL incluem relações PHC declaradas já identificadas no inventário REL/DTL. A ligação por `obistamp` é aqui registada como comportamento observado no código dos triggers; não é apresentada como relação REL/DTL sem confirmação independente.

### 3.2 Configuração por tipo de dossier

A Enciclopédia explica que cada tipo de dossier é configurável e que essa configuração determina o comportamento na aplicação. Entre as opções documentadas estão:
- Movimenta Stocks e código de movimento;
- Código de movimento 1 para saídas e código de movimento 2 para entradas;
- Movimenta reservas de clientes;
- Movimenta reservas de fornecedores;
- Movimenta quantidade em recepção;
- Permite Cativar Stock e Permite Pré-Cativar Stock;
- Dossier de transferência entre armazéns;
- Dossier de produção.

A Enciclopédia descreve ainda que a quantidade 2 nas linhas pode representar quantidade previsional/fornecida e pode ser actualizada por outro documento. Isto é consistente com a utilização de `qtt2` pelos triggers para registar quantidades satisfeitas por linhas de outros dossiers.

## 4. Triggers do cabeçalho BO

### 4.1 `tr_bo_insert` e `tr_bo_update`

Ambos aplicam a mesma validação de assinatura digital.

A validação ocorre quando:
- existe registo correspondente em `BO2`, ligado por `bostamp = bo2stamp`;
- `BO2.assinatura` está vazia;
- o tipo de dossier liga a `TS` e `TS2`;
- `TS2.assinaimp = 0`;
- `TS2.docsimport = 0`;
- `TS2.assinadoc = 1`;
- `BO.obrano > 0).

Quando as condições se verificam, o trigger constrói uma mensagem com o nome e número do dossier, executa `RAISERROR` e `ROLLBACK`. Portanto, a gravação ou alteração é recusada nesse cenário.

**Interpretação:** a validação da assinatura é condicionada pela configuração do tipo de dossier e pelo conteúdo da informação complementar em BO2. Não se deve concluir que todos os dossiers exigem assinatura: a regra depende das flags de TS2 e de a assinatura estar vazia.

Na CS Default, já tinha sido observado `TS2.assinadoc=1` nos tipos 1, 3, 10 e 11. Para concluir em quais deles a validação bloqueia efectivamente, é necessário confirmar também `assinaimp`, `docsimport` e o estado de `BO2.assinatura` em cada cenário.

### 4.2 `tr_bo_delete`

O trigger executa a eliminação dos registos de `BOTXT` cujo `bostamp` corresponde ao cabeçalho eliminado.

O trigger BO de DELETE, por si só, não contém instruções para eliminar as linhas BI. Não se deve inferir que este trigger assegura toda a eliminação do dossier: o fluxo completo de eliminação pela aplicação pode envolver outras operações.

## 5. Linhas BI: criação de movimentos e reservas

### 5.1 `tr_bi_insert`

O trigger é o ponto principal de materialização dos efeitos das linhas.

#### Movimentos SL

Quando a linha tem referência, `emconf=0`, o dossier não está anulado (`BO2.anulado=0`) e o tipo está configurado com `TS.stocks=1), o trigger cria um movimento em `SL`.

O movimento:
- fica associado à linha por `SL.bistamp = BI.bistamp`;
- identifica a origem como `BO`;
- recebe artigo, armazém, quantidade, lote, custo, IVA e restantes dados a partir de BI e do cabeçalho BO;
- usa normalmente `TS.cmstocks` e `TS.cmdesc`;
- pode usar `TS.cm2stocks` e `TS.cm2desc` quando se verifica a condição de dossier de produção com linha de produção, ou de troca de equipamento configurada em TS2.

Existe ainda um segundo ramo, condicionado por `TS.trfa=1`, que cria outro movimento SL:
- com stamp iniciado por `T` seguido do `bistamp`;
- com o armazém da segunda localização da linha (`BI.ar2mazem`);
- usando `TS.cm2stocks` e `TS.cm2desc`.

**Interpretação:** o código suporta um circuito de dois movimentos associado à mesma linha BI quando a configuração activa esse ramo. A Enciclopédia documenta os códigos de movimento 1/2 como códigos para saídas e entradas e descreve a configuração de dossiers de transferência entre armazéns. A associação concreta de `trfa` a cada tipo de dossier deve ser confirmada nos valores TS da instalação antes de classificar cada caso como transferência.

#### Reservas e quantidades especiais

O trigger actualiza os acumulados de `ST` e, quando aplicável, de `SA`, com base em `BI.qtt - BI.qtt2`, desde que a linha tenha referência, não esteja fechada e o dossier não esteja anulado.

| Flag da linha BI | Campo actualizado em ST | Campo actualizado em SA |
|---|---|---|
| `rescli=1` | `ST.qttcli` | `SA.rescli` |
| `cativo=1` | `ST.qttcat` | `SA.rescat` |
| `resfor=1` | `ST.qttfor` | `SA.resfor` |
| `resusr=1` | `ST.usrqtt` | — |
| `resrec=1` | `ST.qttrec` | `SA.qttrec` |

Os ramos de reserva aplicam a quantidade ainda não satisfeita (`qtt-qtt2`) e evitam acrescentar valores negativos em vários dos cálculos. O trigger pode também criar o registo de SA em falta para linhas que movimentam reservas de clientes, fornecedores ou quantidade em recepção.

A Enciclopédia confirma o significado funcional destas opções:
- reservas de clientes afectam o stock previsto;
- reservas de fornecedores são tratadas de forma equivalente no sentido do fornecedor;
- quantidade em recepção permite disponibilizar quantidades ainda não integradas no stock físico;
- cativação reserva stock para um dossier específico e depende da configuração de reservas de clientes;
- pré-cativação associa encomendas de fornecedor a encomendas de cliente.

### 5.2 Ligação a uma linha de dossier de origem: `obistamp`

Quando uma linha inserida tem `obistamp` preenchido e quantidade diferente de zero, o trigger actualiza a linha BI de origem identificada por esse stamp.

O código:
- actualiza a data, número, nome e tipo do documento de destino na linha de origem;
- incrementa `BI.qtt2` da linha de origem com a quantidade da linha de destino;
- incrementa `BI.partes2` quando a configuração do dossier de origem utiliza os campos de partes correspondentes.

**Interpretação:** este é o mecanismo SQL que liga uma linha de um dossier posterior à linha do dossier que lhe deu origem e materializa a quantidade fornecida/satisfeita na quantidade 2 da origem. A Enciclopédia descreve a cópia entre dossiers e a opção «Atualiza a segunda quantidade do dossier de origem», que permite passar a quantidade do novo dossier para a quantidade 2 do dossier original.

A utilização efectiva desta ligação depende do processo de cópia e das opções seleccionadas; não se deve assumir que toda a cópia de dossiers actualiza automaticamente a quantidade 2.

## 6. Alteração de linhas: `tr_bi_update`

O trigger cobre vários cenários, não apenas a edição simples de uma linha.

### 6.1 Entrada e saída de conferência

Quando uma linha passa de `emconf=1` para `emconf=0`, o trigger cria os movimentos SL e incrementa as reservas aplicáveis, respeitando a configuração do dossier e a condição de não anulado.

Quando passa de `emconf=0` para `emconf=1`, o trigger:
- remove o movimento SL associado à linha;
- reverte os acumulados de reservas em ST/SA;
- reverte, quando aplicável, a quantidade satisfeita na linha de origem ligada por `obistamp`.

A Enciclopédia descreve o estado de conferência como um estado em que os efeitos da gravação são retirados das tabelas afectadas (com excepção de BO e BI) e novamente executados quando o dossier sai de conferência. O comportamento do trigger é consistente com essa descrição para os efeitos de BI aqui observados.

### 6.2 Alteração de campos da linha

O trigger também trata alterações a campos como referência, quantidade, quantidade 2, armazém, flags de reserva, estado de conferência e outros campos usados na criação de SL.

Nos cenários aplicáveis:
- reverte os valores anteriores das reservas;
- aplica os novos valores;
- actualiza o movimento SL existente ou cria o movimento que falta;
- elimina SL quando a referência deixa de existir;
- sincroniza dados do movimento com os dados alterados na linha BI.

Quando a quantidade de uma linha ligada por `obistamp` é alterada, o trigger retira o efeito da quantidade anterior na linha de origem e aplica a nova quantidade, actualizando também os campos de documento de origem.

## 7. Eliminação de linhas: `tr_bi_delete`

Quando uma linha BI é eliminada e pertence a um dossier não anulado, o trigger reverte as reservas aplicáveis em ST/SA.

Também:
- elimina o movimento SL associado à linha;
- repõe a linha de origem ligada por `obistamp`, limpando os campos de identificação do documento de destino e subtraindo a quantidade anteriormente satisfeita de `BI.qtt2`;
- reverte `BI.partes2` quando aplicável;
- elimina registos auxiliares associados à linha em `OFC`, `OCI`, `OMI`, `OCP`, `OPT` e `BIPRECAT`, sob as condições presentes no código.

**Interpretação:** a eliminação de uma linha não é apenas a remoção de BI. O trigger tenta reverter os efeitos de stock, reservas, satisfação de uma linha de origem e estruturas auxiliares dependentes.

## 8. Anulação lógica do dossier: `tr_bo2_update`

Esta é uma descoberta central para compreender o ciclo de vida.

O trigger `tr_bo2_update` reage especificamente à transição:
- `deleted.anulado = 0`;
- `inserted.anulado = 1`.

Ao anular logicamente o dossier, o trigger percorre as linhas BI associadas e:
- reverte reservas de clientes, cativas, de fornecedores, de utilizador e de recepção em ST/SA;
- elimina movimentos SL associados às linhas;
- limpa estruturas auxiliares ligadas às linhas: OFC, OCI, OMI, OCP, OPT e BIPRECAT;
- repõe a quantidade 2 e os dados de documento na linha de origem quando existem linhas ligadas por `obistamp`;
- reverte partes fornecidas quando a configuração o exige.

O código não elimina fisicamente o cabeçalho BO nem as linhas BI neste processo: a anulação é representada pelo estado `BO2.anulado` e pela reversão dos efeitos derivados.

A Enciclopédia, no artigo **Registo de Dossiers**, documenta a possibilidade de anular o documento e, nos documentos certificados, registar o motivo da anulação. A lógica de reversão encontrada em `tr_bo2_update` é a evidência técnica de como essa alteração de estado repercute nos movimentos e reservas.

## 9. Ciclo funcional reconstruído

A leitura conjunta das fontes permite representar o circuito principal:

1. **Configuração:** TS/TS2 definem se o tipo movimenta stock, que códigos de movimento usa e que reservas/cativações estão disponíveis.
2. **Cabeçalho:** BO guarda os dados gerais; BO2 guarda informação complementar e o estado de anulação.
3. **Linhas:** BI contém artigos/serviços, quantidades, armazéns, flags de reserva e ligações a linhas de origem.
4. **Efeitos:** tr_bi_insert / tr_bi_update materializam movimentos SL e acumulados ST/SA, conforme configuração e estado.
5. **Satisfação de origem:** obistamp liga a linha de destino à linha de origem e actualiza qtt2/partes2 na origem.
6. **Alteração ou eliminação:** os triggers BI revertem e recalculam efeitos associados.
7. **Anulação do dossier:** a transição BO2.anulado 0→1 reverte movimentos, reservas e ligações derivadas, sem apagar BO/BI.

Este modelo descreve o circuito demonstrado pelos triggers consultados. Não pretende cobrir todas as rotinas da aplicação, eventos, regras de gravação, cópias, integrações ou módulos adicionais.

## 10. O que ainda falta validar

1. Confirmar, por consulta read-only, todos os valores relevantes de TS/TS2 para cada `ndos`: `stocks`, `cmstocks`, `cm2stocks`, `trfa`, `producao`, `trocaequi`, reservas, cativação, pré-cativação e flags de assinatura.
2. Confirmar os códigos CM1/CM2 correspondentes e a sua direcção de entrada/saída em cada tipo de dossier.
3. Verificar as relações REL/DTL específicas de BO, BO2, BI, SL e tabelas auxiliares, distinguindo-as das ligações apenas observadas nos triggers.
4. Analisar os triggers de BO2 e BI em conjunto com os eventos/regras/rotinas da aplicação que criam, copiam, fecham e anulam dossiers.
5. Fazer um ensaio controlado numa base de teste com: dossier sem movimento; dossier com movimento de stock; reserva de cliente; stock cativo; pré-cativação; transferência entre armazéns; cópia que actualiza qtt2; passagem por conferência; alteração; eliminação de linha; anulação lógica.
6. Confirmar os casos de produção e troca de equipamento, incluindo os ramos que seleccionam CM2.

Não se deve executar testes de escrita na CS Default nem em bases de clientes. Os ensaios devem ocorrer numa cópia de teste autorizada.

## 11. Conclusão

A BO não é, por si só, o motor dos efeitos de stock. O cabeçalho tem validação de assinatura e limpeza de BOTXT; o circuito transaccional das linhas está sobretudo em BI. BO2 é igualmente central porque contém o estado de anulação e o trigger de BO2 reverte os efeitos derivados do dossier.

O circuito BO → BI → SL, com configuração TS/TS2, reservas em ST/SA e satisfação de linhas por obistamp, está agora parcialmente reconstruído com evidência directa do código e cruzamento com a Enciclopédia. A validação dos valores de configuração e o ensaio funcional continuam pendentes.

## 12. Relação com a investigação FT/FI/TD

A investigação FT/FI/TD já documentada em [Investigação funcional PHC — CS Default: FT, FI e TD](investigacao-funcional-csdefault-ft-fi-td-2026-11-25.md) identificou a ligação FI → BI e efeitos da faturação sobre linhas de dossiers. Este relatório aprofunda o circuito próprio dos dossiers internos. Os dois relatórios devem ser lidos em conjunto: a passagem de um dossier interno para faturação cruza o circuito de origem (BO/BI) com o circuito de destino (FT/FI), mas os triggers e configurações de cada lado mantêm responsabilidades distintas.
