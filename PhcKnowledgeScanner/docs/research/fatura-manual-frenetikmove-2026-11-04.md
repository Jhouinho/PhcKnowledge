# Investigação PHC — Fatura manual e movimentos de stock

**Estado:** análise inicial consolidada  
**Data da análise:** 2026-11-04  
**Base de dados:** `Frenetikmove`  
**Cenário:** criação manual de uma fatura (sem documento de origem), com três artigos controlados por stock.

## 1. Fontes analisadas

- Conversa exportada: `docs/sources/analisar-base-phc-2026-11.txt` (338 páginas; SHA-256 `97289240b484381e886bbe84d34072ebc70fc70cb23eb21026061ff8a5f01a8a`). Extrato consolidado em `docs/architecture/extract-2026-11-conversation.md`.
- Exportação de triggers: `PhcKnowledgeScanner/triggers phc.sql` (957 KB; 352 triggers identificados; SHA `4ab635747718a8bee1e4243a0a9fcbb7d3ae0f39`).
- Resultados SQL e observações do Profiler reproduzidos na conversa exportada. O ficheiro bruto de trace (.trc) não foi localizado na árvore atual do repositório; por isso, a cronologia fina dos eventos continua dependente desses resultados transcritos.

## 2. Cenário observado

Configuração do documento em `TD`:

| Campo | Valor |
|---|---:|
| NDOC | 1 |
| TIPODOC | 1 |
| LANCASL | 1 |
| COMS | 1 |

Documento:

| Campo | Valor |
|---|---|
| FT.FTSTAMP | `ADM26110433780,615038245` |
| FT.FNO | 429 |
| FT.CUSTO | 145252.86018 |
| FT.ECUSTO | 724.518212 |

Linhas em `FI`:

| FISTAMP | REF | QTT | CUSTO | ECUSTO | COMPOSTO |
|---|---|---:|---:|---:|---:|
| ADM26110433780,6780000-1 | AG1636 | 1 | 57091.70461 | 284.772222 | 0 |
| ADM26110433842,1940000-2 | AG1669 | 1 | 64243.48677 | 320.445161 | 0 |
| ADM26110433842,4560000-3 | AG1678 | 1 | 23917.66880 | 119.300829 | 0 |

Os resultados observados mostram três registos em `SL` e três em `PN`, alinhados por `FISTAMP` com as três linhas de `FI`.

Os somatórios das linhas coincidem exatamente com os valores do cabeçalho:

- `SUM(FI.QTT * FI.CUSTO)` para linhas não compostas = **145252.86018**, igual a `FT.CUSTO`.
- `SUM(FI.QTT * FI.ECUSTO)` para linhas não compostas = **724.518212**, igual a `FT.ECUSTO`.

## 3. Fluxo confirmado nos triggers

### 3.1. `tr_fi_insert` — FI origina movimentos

O trigger `tr_fi_insert` contém, entre outras, estas regras:

1. **Criação de movimentos em SL**
   - Junta `inserted` a `FT` por `FTSTAMP` e a `TD` por `NDOC`.
   - Quando `TD.LANCASL = 1` e a referência da linha não está vazia, executa `INSERT INTO SL`.
   - O registo de movimento usa `SLSTAMP = FISTAMP` e `FISTAMP = FISTAMP`.
   - Define `ORIGEM = 'FT'`.
   - Obtém `CM` e `CMDESC` de `TD.CMSL` e `TD.CMSLN`.
   - A quantidade gravada em SL é positiva para documentos cujo `TIPODOC` não é 3; para `TIPODOC=3`, inverte o sinal.

2. **Criação de registos em PN**
   - Quando `TD.COMS = 1`, `FI.QTT <> 0` e `FI.COMPOSTO = 0`, executa `INSERT INTO PN`.
   - Usa `PNSTAMP = FISTAMP`, `FISTAMP = FISTAMP`, `FTSTAMP = FTSTAMP` e `ORIGEM = 'FT'`.
   - Os restantes valores são calculados/preenchidos a partir de `FI`, `FT`, `FT2` e `TD`.
   - A semântica funcional completa de PN ainda não deve ser assumida apenas com esta evidência.

3. **Atualização dos custos no cabeçalho**
   - O trigger atualiza `FT.CUSTO` e `FT.ECUSTO` somando os custos das linhas inseridas, excluindo linhas compostas (`COMPOSTO=0)).
   - Para `TIPODOC=3`, aplica sinal inverso.
   - Isto explica a igualdade observada entre os custos do cabeçalho e os somatórios das três linhas.

### 3.2. `tr_sl_insert` — SL atualiza o stock

O trigger `tr_sl_insert` confirma que a criação do registo em `SL` desencadeia a atualização dos saldos de stock:

- Usa `CM < 50` como ramo de entrada e `CM >= 50` como ramo de saída.
- Para movimentos normais, exige `TRFA = 0`.
- Atualiza `ST.STOCK` (stock global por referência).
- Atualiza `ST.QTTACIN` nas entradas e `ST.QTTACOUT` nas saídas.
- Atualiza `SA.STOCK` por referência/armazém.
- Atualiza `SX.STOCK` por referência/armazém/cor/tamanho.
- Cria registos em `SA` e, quando aplicável, em `SX`, caso ainda não existam.
- Considera uma data-limite derivada de `STIC` (`LANCA=1` e `JUSANTE=1`); movimentos anteriores à última data de corte são ignorados pelo trigger.

Para uma única linha inserida, a atualização de `ST` é equivalente a:

```sql
st.stock = st.stock + CASE WHEN inserted.cm < 50
                           THEN inserted.qtt
                           ELSE -inserted.qtt
                      END
```

Na mesma situação, `QTTACOUT` aumenta quando `CM > 50`, e `QTTACIN` aumenta quando `CM < 50`.

O trigger também pode atualizar custos médios/preços de custo em movimentos de entrada, condicionado por `CM2.MUDAPCPOND` e `CM2.MUDAPCULT`. Essas condições não se aplicam ao movimento observado com `CM=75`.

### 3.3. Eliminação e alteração

- `tr_fi_delete` elimina os movimentos `SL` e os registos `PN` associados à linha, além de ajustar os custos acumulados em `FT`.
- `tr_sl_delete` repõe os saldos de stock, invertendo o efeito do movimento eliminado.
- `tr_fi_update` atualiza/recria registos `PN` quando campos relevantes da linha mudam e recalcula os custos do cabeçalho quando mudam quantidade/custo.
- `tr_ft_update` contém lógica de anulação do documento que elimina movimentos `SL` e registos `PN` associados às linhas.

### 3.4. PN também participa no ciclo de recebimentos e processamento

A pesquisa transversal dos triggers exportados encontrou referências adicionais a `PN`:

- `tr_cc_update`: quando os valores de débito/crédito e respetivos valores em aberto são alterados, atualiza `PN.PAGO` e `PN.PDATA` para registos associados pelo `FTSTAMP`. A determinação do estado consulta movimentos/recibos através de `TSRE`, `RE`, `RL`, e, como alternativa para a data, `LI`/`LE`.
- `tr_cc_delete`: em determinadas condições de fecho/eliminação do registo de conta corrente, repõe `PN.PAGO=0`.
- `tr_vy_delete`: ao eliminar um registo de `VY`, percorre `VYDOC → RE → RL → CC → PN`, repõe `PN.PROCES=0` e subtrai os valores de venda processados a `PN.VALORPROC`/`PN.EVALORPROC`.

**Conclusão prudente:** `PN` é um registo associado à linha/documento que participa também no acompanhamento de estado de pagamento e de valores processados. Não é correto classificá-lo apenas como movimento de stock. A designação funcional exata da tabela deve ser confirmada no dicionário PHC e nas rotinas que a consomem.

## 4. Cruzamento com os resultados observados

Os registos apresentados na conversa mostram:

| REF | CM | QTT | ARMAZEM | ORIGEM |
|---|---:|---:|---:|---|
| AG1636 | 75 | 1 | 1 | FT |
| AG1669 | 75 | 1 | 1 | FT |
| AG1678 | 75 | 1 | 1 | FT |

Também foi consultada a configuração de `CM2`, tendo sido observado:

| CM | MUDAPCULT | MUDAPCPOND |
|---:|---:|---:|
| 75 | 0 | 0 |

Isto é coerente com a regra do trigger: `CM=75` pertence ao ramo de saída (`CM >= 50`), pelo que cada movimento reduz o stock e incrementa a quantidade acumulada de saídas, desde que `TRFA=0` e passe a validação da data de corte.

Os valores de stock atuais apresentados para os artigos são observações posteriores ao lançamento; sem os saldos anteriores ao movimento, não se deve afirmar numericamente qual era o stock antes da fatura.

## 5. Conclusões confirmadas

1. A criação das linhas em `FI` é o ponto de origem dos movimentos em `SL` neste cenário.
2. A geração de `SL` é condicionada por `TD.LANCASL=1` e por referência não vazia.
3. A atualização efetiva do stock não é feita diretamente pelo trigger de `FI`: é desencadeada pelo `INSERT` em `SL` e executada por `tr_sl_insert`.
4. A configuração do movimento (`CM`/`CMDESC`) vem de `TD.CMSL`/`TD.CMSLN`, não de colunas com esses nomes na tabela `CM2`.
5. `PN` é gerada separadamente pelo trigger de `FI`, condicionada por `TD.COMS=1`, quantidade diferente de zero e linha não composta.
6. Os custos `FT.CUSTO` e `FT.ECUSTO` são mantidos pelos triggers de `FI` e coincidem com os somatórios das linhas no documento observado.

## 6. Questões por validar

- Obter novamente o trace bruto do Profiler, caso seja necessária a sequência temporal completa e os comandos executados pela aplicação fora dos triggers.
- Confirmar a existência e o conteúdo de registos `STIC` que determinam a data de corte. A consulta transcrita na conversa apresentou `DataCorte = NULL`, mas o trigger usa como fallback `1900-01-01` quando não encontra data de corte válida.
- Comparar saldos de `ST`, `SA` e `SX` antes e depois de um movimento controlado, para provar quantitativamente cada nível de stock.
- Identificar funcionalmente a tabela `PN` através do dicionário PHC e dos processos que a consomem; por agora documenta-se apenas a regra de criação e os campos preenchidos.
- Analisar em detalhe `tr_sl_update`, movimentos com `TRFA<>0`, movimentos compostos e operações de anulação/eliminação.
- Confirmar se o trace observado contém operações diretas da aplicação sobre `FT`, `FI`, `SL` e `PN` ou se parte das operações observadas são exclusivamente efeitos dos triggers.

## 7. Próximo passo da investigação

Continuar a reconstrução do ciclo documental, seguindo os efeitos dos triggers de `FI` e `SL` para as tabelas de stock e para as restantes tabelas relacionadas. Manter sempre separadas:
- **Evidência observada:** resultados SQL/Profiler e código de trigger.
- **Regra confirmada:** comportamento explícito no SQL e coerente com os resultados.
- **Hipótese:** interpretação funcional ainda sem validação independente.

**Nota:** a análise foi feita sobre código exportado e resultados fornecidos na conversa. Não foram executadas alterações à base de dados PHC.


## 8. Aprofundamento — natureza funcional de PN e manutenção dos movimentos

### 8.1. Identificação funcional de PN

O cruzamento com documentação PHC identifica a tabela `PN` como **Produtos Vendidos**. Esta designação é coerente com os campos escritos pelos triggers e com os exemplos de análises PHC que consultam `PN` para obter vendas por cliente, artigo, vendedor e documento.

A evidência do trigger `tr_fi_update` mostra que PN não é apenas um apontador para FI. O trigger mantém um registo comercial com, entre outros:

- identificação: `PNSTAMP`, `FISTAMP`, `FTSTAMP`, `ORIGEM`;
- contexto documental/comercial: classe, referência, descrição, cliente, estabelecimento, número e tipo de documento, data, vendedor, zona e segmento;
- quantidades e valores: `QTT`, `tiliquido`/valor líquido, `PV`/preço unitário, valores em moeda alternativa, valores base e valores unitários;
- custos: `PCUSTO`, `TCUSTO`, `EPCUSTO`, `ETCUSTO`;
- atributos do artigo: referência, cor, tamanho, lote, família e campos de utilizador;
- estado de recebimento/processamento: `PAGO`, `PDATA`, `PROCES`, `VALORPROC`, `EVALORPROC`.

O nome físico do campo de valor líquido é `tiliquido`.

### 8.2. Como FI mantém PN quando uma linha é alterada

O trigger `tr_fi_update` implementa dois caminhos principais:

1. **Atualização do registo PN existente**
   - Quando os campos relevantes da linha são alterados, atualiza o registo PN correspondente por `FISTAMP`.
   - Recalcula quantidade, valor líquido, preço unitário, valores base, vendedor, atributos do artigo e custos.
   - Aplica sinal negativo à quantidade quando `TD.TIPODOC=3`.
   - A geração/atualização depende de `TD.COMS=1`, quantidade diferente de zero e `COMPOSTO=0`.

2. **Criação ou remoção do registo PN**
   - Se a linha passa a ter quantidade zero, o trigger elimina o PN associado, quando `TD.COMS=1` e a linha não é composta.
   - Noutro ramo, volta a inserir PN quando ainda não existe um registo com `PNSTAMP=FISTAMP`, desde que a fatura não esteja anulada e se mantenham as condições anteriores.
   - O campo `PAGO` é inicializado de acordo com a configuração de lançamento em conta corrente: quando `TD.LANCACC=1` e `TD.CCAUTOREG` não está ativo, é inicializado a 0; nos restantes casos é inicializado a 1. A data segue uma regra equivalente, usando uma data sentinela de 1900 quando ainda não há integração automática.

Isto confirma que PN é uma projeção comercial persistida das linhas de faturação, mantida pelos triggers, e não apenas um registo criado uma única vez no momento da inserção.

### 8.3. Como FI mantém SL quando uma linha é alterada

O mesmo `tr_fi_update` mantém os movimentos de stock associados à linha:

- Se a referência da linha fica vazia, elimina o movimento SL associado.
- Se já existe SL para o `FISTAMP`, atualiza os seus dados a partir de FI, FT e TD.
- Se não existe SL e as condições de lançamento se verificam, insere um novo movimento.
- Atualiza referência, armazém, quantidade, data, custo/preço, lote, cor, tamanho, tipo de movimento e restantes atributos.
- A quantidade do movimento é invertida quando `TIPODOC=3`.

A atualização de SL desencadeia `tr_sl_update`; a eliminação desencadeia `tr_sl_delete`. Assim, a alteração de uma linha FI não ajusta diretamente ST/SA/SX: altera ou elimina o movimento SL e deixa que o trigger de SL faça a manutenção dos saldos.

### 8.4. `tr_sl_update` — reversão do estado anterior e aplicação do novo

O trigger `tr_sl_update` confirma um padrão importante:

1. Usa a tabela lógica `deleted` para desfazer o efeito do movimento anterior.
2. Usa a tabela lógica `inserted` para aplicar o efeito do movimento atualizado.
3. Faz essa manutenção em `ST`, `SA` e `SX`, respeitando `TRFA=0` e a data de corte derivada de `STIC`.
4. Para operações com uma linha, usa atualizações diretas; para operações com várias linhas, agrega quantidades por referência e, quando aplicável, por armazém/cor/tamanho.

Consequência: mudar quantidade, tipo de movimento, referência, armazém, cor/tamanho ou data pode alterar os saldos, porque o trigger retira primeiro o efeito antigo e aplica depois o novo. Não se deve interpretar um UPDATE em SL como uma simples alteração descritiva.

O trigger também mantém datas e preços de referência em ST, incluindo última entrada/última saída e preços de custo, conforme o tipo de movimento, a data e as opções de CM2. Existe uma proteção por `CONTEXT_INFO() = 0x88888` que faz o trigger sair sem executar a lógica de stock, mecanismo usado para evitar determinados ciclos/recursões internos.

### 8.5. `tr_sl_delete` — reversão de stock e recuperação de último custo

A eliminação de SL repõe o efeito quantitativo do movimento eliminado:

- para `ST`, desfaz stock global e acumulados de entrada/saída;
- para `SA`, desfaz stock por armazém e quantidade acumulada de entradas;
- para `SX`, desfaz stock por referência/armazém/cor/tamanho.

Há ainda uma lógica adicional quando são eliminados movimentos de entrada (`CM<50`): o trigger procura o último movimento de entrada remanescente para a referência, com `CM2.MUDAPCULT=1), e pode restaurar em `ST` e `SE` os campos `PCULT`/`EPCULT` a partir desse movimento. Esta lógica usa data/hora de criação (`OUSRDATA`/`OUSRHORA`) e inclui tratamento defensivo para horas mal formatadas.

Portanto, eliminar um movimento de entrada pode ter efeitos para além da quantidade: pode obrigar a restaurar o último preço de custo, se a configuração do tipo de movimento assim o determinar.

### 8.6. `tr_ft_update` — anulação da fatura é um processo transversal

Quando uma fatura passa de não anulada para anulada, `tr_ft_update` executa um conjunto de operações que ultrapassa a simples marcação de `FT.ANULADO`:

- elimina os movimentos SL associados às linhas FI;
- elimina os registos SV associados;
- elimina os PN associados às linhas;
- repõe quantidades/documentos de origem em BI, quando as linhas estavam ligadas a dossiers internos;
- reverte valores acumulados de documentos em tabelas de regularização, quando aplicável;
- trata registos de conta corrente e ligações a regularizações/recibos, conforme `TD.LANCACC`, `TD.CCAUTOREG`, `FTCC` e `FTRD`;
- repõe indicadores de faturação em documentos relacionados;
- reverte os acumulados de faturação do cliente (`CL.ACMFACT` e `CL.EACMFACT`) para documentos aplicáveis.

A eliminação de SL chama, por sua vez, `tr_sl_delete`, que repõe os saldos de stock. A eliminação de PN remove a projeção comercial da linha anulada. A parte financeira depende das opções do tipo de documento e das relações existentes; não se deve generalizar um único percurso para todos os documentos.

### 8.7. Relação entre PN e conta corrente — interpretação refinada

O trigger `tr_cc_update` atualiza `PN.PAGO` e `PN.PDATA` quando os valores em aberto (`DEBF`, `CREDF`, `EDEBF`, `ECREDF`) são alterados e o saldo da conta corrente associado à fatura chega a zero numa das moedas consideradas.

- `PN.PAGO` é calculado a partir dos recibos ligados por `RL.CCSTAMP`, consultando `TSRE.MOVINC`.
- `PN.PDATA` procura primeiro a data de processamento do recibo (`RE.PROCDATA`); como alternativa usa a data do documento de liquidação (`LE.DATA`); se não encontrar nenhuma, usa a data/hora atual.
- Há uma condição que exclui documentos cujo tipo tenha `TD.CCAUTOREG=1`.

Isto é compatível com PN funcionar como tabela de produtos vendidos e, simultaneamente, manter indicadores usados em análises comerciais, incluindo o estado/data de pagamento.

### 8.8. Limites da evidência

- A identificação de PN como Produtos Vendidos é corroborada por documentação PHC pública e pelo próprio conteúdo dos triggers.
- O comportamento descrito é confirmado no SQL exportado, mas ainda não foi validado com uma experiência controlada de alteração/anulação na base Frenetikmove.
- O comportamento dos ramos de `TD.LANCACC`, `TD.CCAUTOREG`, `MULTI`, documentos de origem e regularizações depende da configuração concreta de cada tipo de documento.
- A lógica especial de recuperação de custos em `tr_sl_delete` merece validação com uma entrada real eliminada, pois depende de movimentos anteriores e de `CM2.MUDAPCULT`.

## 9. Próximas linhas de investigação

1. Mapear os campos e as relações de PN no dicionário PHC, distinguindo os campos comerciais, de custo, pagamento e processamento.
2. Analisar os triggers de `RE`, `RL`, `CC`, `TSRE`, `LI` e `LE` para reconstruir o ciclo completo de recebimento e liquidação.
3. Confirmar o comportamento de `tr_fi_update` com alteração de quantidade, referência, armazém e custo, incluindo os casos de quantidade zero e artigo composto.
4. Comparar a anulação de uma fatura com a eliminação de uma linha, para separar os efeitos de `tr_fi_delete`, `tr_ft_update`, `tr_sl_delete` e `tr_cc_update`.
5. Verificar se o processo observado no Profiler incluiu operações explícitas da aplicação sobre CC/FTCC/FTRD ou se os registos financeiros foram gerados exclusivamente pelos triggers.


## 10. Circuito de recebimentos — RE, RL, CC, LI e LE

A leitura dos triggers de recebimentos permite aprofundar a ligação entre PN e a conta corrente. O circuito confirmado no SQL é o seguinte:

### 10.1. RE — cabeçalho do recibo

O trigger `tr_re_insert` cria movimentos em `CC` com origem `RE`, além de registos em `OL` (quando aplicável). O recibo tem, portanto, efeitos próprios na conta corrente/tesouraria.

O trigger `tr_re_update` mantém esses movimentos quando o recibo é alterado:
- cria ou atualiza o movimento CC principal do recibo quando `PROCESS=1`;
- trata movimentos complementares identificados por prefixos nos stamps, incluindo diferenças/valores associados a IRS e outros componentes;
- elimina movimentos correspondentes quando o recibo deixa de estar processado;
- mantém os registos de operações/tesouraria em OL.

O trigger `tr_re_delete` elimina os movimentos CC associados ao recibo.

### 10.2. RL — aplicação do recibo às faturas

O trigger `tr_rl_insert` é o ponto onde a aplicação do recibo a uma determinada conta corrente atualiza os valores pagos/regularizados:

- só considera linhas com `RL.PROCESS=1`;
- usa `RL.CCSTAMP` para localizar o movimento de conta corrente da fatura;
- atualiza os valores regularizados em moeda base e moeda alternativa: `CC.DEBF`, `CC.CREDF`, `CC.DEBFM`, `CC.CREDFM`, `CC.EDEBF` e `CC.ECREDF`;
- considera diferenças cambiais, arredondamentos e valores de IVA/IRS associados;
- atualiza indicadores do recibo aplicado, como número/ano/documento do recibo e valores recebidos, exceto quando a aplicação corresponde ao recibo já registado no próprio movimento;
- quando o tipo de recibo tem `TSRE.INTROPAG=1), pode marcar o documento de regularização associado como integrado.

Os triggers `tr_rl_update` e `tr_rl_delete` seguem o princípio de reversão:
1. retiram do CC os valores da versão antiga da linha RL;
2. quando existe uma nova versão, aplicam os novos valores;
3. na eliminação, deixam apenas a reversão.

Isto confirma que RL é a ligação operacional entre o recibo e cada movimento de conta corrente liquidado. Não é o cabeçalho RE que, por si só, identifica toda a repartição do pagamento por fatura: essa repartição está nas linhas RL.

### 10.3. Encadeamento até PN.PAGO e PN.PDATA

O encadeamento técnico inferido diretamente dos triggers é:

`RE/RL` → alteração de `CC.DEBF/CREDF/EDEBF/ECREDF` → execução de `tr_cc_update` → atualização de `PN.PAGO` e `PN.PDATA`.

A regra de atualização de PN em `tr_cc_update` é acionada quando os valores regularizados são alterados e o saldo em aberto chega a zero numa das moedas consideradas. Nesse caso:
- consulta RL e RE para determinar o último recibo/processamento;
- consulta TSRE para distinguir recibo normal de recibo incobrável (`MOVINC`);
- grava `PN.PAGO` como 1 quando `TSRE.MOVINC=0`, e 0 quando é incobrável;
- grava `PN.PDATA` com a data de processamento do recibo; como alternativa usa a data de liquidação em LE, ou a data atual.

Esta cadeia explica por que razão o estado de pagamento é mantido na tabela de Produtos Vendidos sem que o trigger de PN tenha de ser executado diretamente pelo ecrã de recebimentos.

### 10.4. LI e LE — liquidações

O trigger `tr_li_insert` atualiza diretamente os valores regularizados em CC através de `LI.CCSTAMP`, incluindo montantes recebidos e arredondamentos em moeda base e alternativa. O trigger `tr_li_delete` reverte esses valores.

O trigger `tr_le_insert` cria um movimento em CC com origem `LE` para o documento de liquidação, usando o valor total líquido de arredondamento. O trigger `tr_le_update` também mantém linhas LI em determinadas alterações de configuração do plano.

Assim, a alternativa de data em `LE.DATA` usada por `tr_cc_update` não é arbitrária: existe um circuito de liquidação independente do recibo RE/RL, que também atualiza a conta corrente.

### 10.5. Modelo funcional consolidado

- `PN`: registo comercial de produto vendido, com indicadores de pagamento/processamento.
- `CC`: movimentos de conta corrente e respetivos valores em aberto/regularizados.
- `RE`: cabeçalho do recibo.
- `RL`: linhas que aplicam o recibo aos movimentos CC.
- `LI`: linhas de liquidação que atualizam valores regularizados em CC.
- `LE`: cabeçalho/documento de liquidação que origina movimento CC.
- `TSRE`: configuração do tipo de recibo, incluindo a distinção de movimento incobrável e integração de pagamentos.

**Importante:** os triggers comprovam as atualizações entre estas tabelas, mas não substituem a validação das regras funcionais de cada tipo de recibo/liquidação. Em particular, os ramos de câmbio, descontos, IVA de caixa, IRS, recibos incobráveis e pagamentos introduzidos por integração devem ser estudados separadamente.

## 11. Próximo passo

Seguir os campos de ligação e os triggers de CC/RE/RL/LI/LE num exemplo concreto da base Frenetikmove, se os dados estiverem disponíveis, e depois regressar à fatura 429 para verificar:
- se foi criado movimento CC para o documento;
- qual o valor inicial em aberto;
- que `CCSTAMP` está associado ao `FTSTAMP`;
- se existem linhas RL/LI ligadas;
- como os valores de pagamento se refletem em PN.

Sem um recibo/liquidação real ligado à fatura 429, a cadeia acima fica confirmada ao nível do código dos triggers, mas não ao nível dos dados deste caso.


## 12. Origem dos movimentos CC na criação da fatura

A análise do trigger `tr_ft_insert` completa o circuito financeiro e revela que a criação de CC depende de mais do que o tipo de documento. Os ramos principais são:

### 12.1. Tipo de documento com `TD.LANCACC=1`

Quando `TD.LANCACC=1` e `FT.MULTI=0`, o trigger cria movimentos em CC associados à fatura. Existem dois padrões relevantes:

- Movimento principal com `CCSTAMP=FTSTAMP`, `FTSTAMP=FTSTAMP` e `ORIGEM='FT'`.
- Quando `TD.CCAUTOREG=1`, cria também um movimento de conta corrente com stamp derivado (`'R'+FTSTAMP`, limitado a 25 caracteres), calculando o valor em aberto a partir do total da fatura menos os valores de regularização/recebimento imediato (`RDTOTAL`, `ERDTOTAL`, `RDTOTALM`).

O trigger também atualiza os valores regularizados do movimento CC quando `CCAUTOREG=1), recorrendo aos campos de IVA de caixa e valores de regularização em FT3.

### 12.2. Tipo de documento com `TD.LANCACC=0)

Existe outro ramo: se `TD.LANCACC=0`, `FT.RDTOTAL<>0) e `FT.TIPODOC=1`, o trigger cria CC com `CCSTAMP=FTSTAMP` e valores correspondentes ao montante de regularização/recebimento registado na fatura.

Isto significa que não se deve concluir que `LANCACC=0` implica sempre ausência de CC: a criação pode ocorrer neste ramo específico, desde que existam valores em `RDTOTAL`.

### 12.3. Consequências para a fatura 429

Os dados já recolhidos para a fatura 429 não incluem `TD.LANCACC`, `TD.CCAUTOREG`, `FT.RDTOTAL` nem a listagem de CC associada ao `FTSTAMP`. Por isso, ainda não é possível afirmar qual destes ramos foi seguido nesta fatura.

A confirmação exige uma consulta à configuração do tipo de documento e aos movimentos CC ligados ao `FTSTAMP`. Não é necessário alterar dados: basta leitura das tabelas.



## 13. Validação na fatura 429 — estado real dos registos PN

Consulta executada na base `Frenetikmove`, filtrada por `PN.FTSTAMP = 'ADM26110433780,615038245'`.

Foram devolvidos exatamente três registos PN:

| REF | QTT | PAGO | PDATA | PROCES | VALORPROC | EVALORPROC |
|---|---:|---:|---|---:|---:|---:|
| AG1636 | 1,000 | 0 | 1900-01-01 | 0 | 0,00000 | 0,000000 |
| AG1669 | 1,000 | 0 | 1900-01-01 | 0 | 0,00000 | 0,000000 |
| AG1678 | 1,000 | 0 | 1900-01-01 | 0 | 0,00000 | 0,000000 |

Em todos os registos:
- `PNSTAMP = FISTAMP`;
- `FTSTAMP` corresponde ao documento 429;
- a quantidade PN coincide com a quantidade das linhas FI;
- `PAGO=0`, sem data real de pagamento (data sentinela 1900-01-01);
- `PROCES=0`, sem valores processados.

### Conclusões confirmadas no caso concreto

1. A criação da fatura 429 produziu três registos PN, um por cada linha de artigo.
2. A chave de associação operacional entre PN e FI é o mesmo stamp: `PN.PNSTAMP = FI.FISTAMP), sendo também preenchido `PN.FISTAMP).
3. No momento da consulta, nenhum dos três produtos vendidos estava marcado como pago.
4. Não existiam valores registados como processados nos campos consultados.

### O que ainda não se pode concluir

- `PAGO=0` confirma o estado guardado em PN, mas, isoladamente, não prova que não exista qualquer movimento financeiro em CC. É necessário consultar CC e a configuração TD para determinar se a fatura foi lançada em conta corrente, se foi criada com recebimento imediato ou se segue outro ramo.
- A data 1900-01-01 é uma data sentinela observada no registo, não uma data efetiva de pagamento.
- `PROCES=0` não significa que os artigos não tenham sido vendidos ou faturados; significa apenas que o indicador de processamento está a zero.



## 14. Validação do lançamento em conta corrente — fatura 429

Consulta executada sobre `FT`, `TD` e `CC`, filtrada pelo `FTSTAMP` da fatura 429.

### Resultado em FT

- `NDOC=1`, `FNO=429`, `TIPODOC=1`.
- `ANULADO=0`: documento não anulado.
- `TOTAL=365017.57740`.
- `RDTOTAL=0.00000`: não foi observado recebimento/regularização imediata neste campo.

### Resultado em CC

Foi encontrado um movimento CC associado à fatura:

| Campo | Valor |
|---|---|
| `CCSTAMP` | Igual ao `FTSTAMP` |
| `FTSTAMP` | `ADM26110433780,615038245` |
| `ORIGEM` | `FT` |
| `FTFNO` | 429 |
| `DEB` | 365017.57740 |
| `CRED` | 0.00000 |
| `EDEB` | 1820.700000 |
| `ECRED` | 0.000000 |
| `DEBF` | 0.00000 |
| `CRED F` | 0.00000 |
| `RECIBADO` | 0 |
| `RESTAMP` | vazio |
| `DATAVEN` | 2026-11-05 |

### Conclusões confirmadas

1. A fatura 429 gerou um movimento em conta corrente com origem `FT`.
2. O movimento representa um débito de 365017.57740, sem crédito associado.
3. Não existe recibo associado no campo `RESTAMP` e `RECIBADO=0`.
4. O campo `RDTOTAL` da fatura está a zero.
5. O estado `PN.PAGO=0` observado nas três linhas é coerente com a existência deste débito ainda não recebido.

### Questões em aberto

- O movimento CC apresenta `DEB=365017.57740` e `EDEB=1820.700000`. A interpretação da relação entre estes valores deve ser confirmada com a moeda, taxa de câmbio e regras de conversão do documento; não assumir uma conversão simples sem validar esses dados.
- Embora a linha completa de TD tenha sido consultada, os indicadores específicos devem ser extraídos explicitamente numa consulta curta para evitar ambiguidades na leitura da linha extensa. Em particular: `LANCACC`, `CCAUTOREG`, `LANCASL`, `COMS`, `CMCC`, `CMSL`, `MODOPAGA`.
- Ainda falta percorrer um recibo real associado a este tipo de movimento para confirmar, com dados, o encadeamento `RE/RL → CC → PN.PAGO/PDATA`.


### Esclarecimento da dupla expressão monetária em CC

A relação numérica observada é:

`365017.57740 / 1820.700000 = 200.482`

O fator 200,482 corresponde à taxa fixa de conversão entre escudos portugueses e euros. Assim, os valores observados são consistentes com:
- `DEB=365017.57740`: montante em escudos (PTE);
- `EDEB=1820.700000`: montante equivalente em euros (EUR).

A designação de moeda devolvida em CC (`PTE ou EURO`) também é consistente com esta leitura. A interpretação deixa, portanto, de ser uma hipótese de câmbio variável: os dois campos representam a expressão do mesmo débito nas duas unidades monetárias da transição PTE/EUR.


## 15. Confirmação da configuração documental TD — NDOC 1

Consulta direta a `TD` para `NDOC=1`:

| Campo | Valor |
|---|---:|
| `LANCACC` | 1 |
| `CCAUTOREG` | 0 |
| `LANCASL` | 1 |
| `COMS` | 1 |
| `CMCC` | 1 |
| `CMSL` | 75 |
| `MODOPAGA` | 1 |
| `CMCCAUTOREG` | 0 |
| `CMCCAUTOREGN` | NULL / vazio |

### Correlação com os resultados observados na fatura 429

A configuração confirma os três ramos documentais observados:
- `LANCASL=1`: a inserção das linhas FI origina movimentos SL; o trigger `tr_fi_insert` usa `TD.CMSL=75`.
- `COMS=1`: as linhas elegíveis (quantidade diferente de zero e `COMPOSTO=0`) originam registos PN.
- `LANCACC=1` e `CCAUTOREG=0`: a fatura segue o ramo normal de criação de movimento CC em `tr_ft_insert`, sem o ramo especial de autorregularização.

O movimento CC observado confirma o resultado: `ORIGEM='FT'`, `CCSTAMP=FTSTAMP`, débito igual ao total da fatura, sem crédito, sem recibo associado e `RECIBADO=0`.

### Conclusão consolidada

Para este tipo documental (NDOC 1), a configuração e os dados concretos da fatura 429 demonstram o circuito de criação:
`FT/FI → SL → atualização ST/SA/SX`, `FI → PN` e `FT → CC`.

A atualização dos saldos de stock ocorre através dos triggers de SL; os registos PN ficam inicialmente por pagar; e o lançamento CC representa a dívida da fatura. O circuito posterior de recebimento continua por validar com um recibo efetivo.


## 16. Aprofundamento do trigger tr_cc_insert

Leitura direta do trigger `tr_cc_insert` no ficheiro de triggers disponibilizado.

Na inserção de movimentos em CC, o trigger:
1. Atualiza o saldo do cliente em `CL`:
   - `CL.SALDO += SUM(CC.DEB - CC.CRED + CC.DIFCAMBIO)`;
   - `CL.ESALDO += SUM(CC.EDEB - CC.ECRED + CC.EDIFCAMBIO)`.
2. Cria um registo em `OD` para o valor ainda não regularizado, quando os valores remanescentes nas duas expressões monetárias são diferentes de zero.
3. Define `CC.FORMAPAG=1` quando o campo chega vazio.
4. Tem ainda um ramo para movimentos de origem `CC` que referenciam outro movimento através de `CR`, `DOCREF` e `OCSTAMP`, atualizando os valores regularizados do movimento original.

Isto acrescenta uma consequência importante à criação da fatura: o lançamento CC não se limita a criar a dívida; atualiza também os acumulados do cliente e cria o respetivo registo auxiliar de pendente em `OD`, sujeito às condições do trigger.

A leitura do trigger permite identificar esta regra geral, mas ainda falta consultar os registos reais `OD` e `CL` da fatura 429 para confirmar os valores concretos produzidos neste caso.


## 17. Validação do registo OD pendente da fatura 429

Consulta executada em `OD` por `CCSTAMP = 'ADM26110433780,615038245'`.

Foi encontrado um registo:

| Campo | Valor |
|---|---|
| `ODSTAMP` | `ADM26110433780,615038245` |
| `CCSTAMP` | `ADM26110433780,615038245` |
| `ORIGEM` | `CC` |
| `DOCUMENTO` | N/Factura |
| `CHEQUE` | 429 |
| `ENTR` | 365017.57740 |
| `EENTR` | 1820.700000 |
| `SAID` / `ESAID` | 0 |
| `GRUPO` | Recebimentos de Clientes |
| `SGRUPO` | Clientes a crédito |
| `OLCODIGO` | `R00002` |
| `BOACOBRANCA` | 1 |
| `DATA` | 2026-11-20 |

### Conclusões confirmadas

1. Existe um registo OD associado diretamente ao movimento CC da fatura 429.
2. Os valores de entrada em OD coincidem exatamente com os valores de débito em CC, nas duas expressões monetárias: `ENTR=CC.DEB` e `EENTR=CC.EDEB`.
3. `SAID=0` e `ESAID=0`: o registo não apresenta saída/recebimento registado.
4. A classificação é `Recebimentos de Clientes / Clientes a crédito`.
5. Este OD é o registo auxiliar de pendente criado no circuito de inserção de CC; não é, por si só, um recibo.

### Datas a esclarecer

- `OD.DATA=2026-11-20`.
- `CC.DATAVEN=2026-11-05`.

As datas são diferentes. Não atribuir ainda a `OD.DATA` o significado de vencimento; é necessário analisar o código que a preenche e a semântica da agenda/pendentes em OD. O valor de `BOACOBRANCA=1` também deve ser interpretado de acordo com as regras do módulo de tesouraria/cobranças, sem inferir que o pagamento já ocorreu.


## 18. Origem da data e dos indicadores do pendente OD — tr_cc_insert

Foi relido diretamente o código de `tr_cc_insert` no ficheiro `triggers phc.sql`. A expressão que preenche a data do registo OD é:

`dateadd(day, cl.odatraso, inserted.dataven)`

Portanto:
- a data base é `CC.DATAVEN`;
- é-lhe somado o número de dias definido em `CL.ODATRASO`;
- o resultado é gravado em `OD.DATA`.

No caso concreto:
- `CC.DATAVEN = 2026-11-05`;
- `OD.DATA = 2026-11-20`;
- diferença = 15 dias.

Isto demonstra que a data OD não é a data normal de vencimento da fatura: é a data de agenda/pendente calculada com o atraso configurado no cliente. Os valores observados implicam `CL.ODATRASO=15`, a confirmar por consulta direta ao cliente.

O trigger também determina outros campos do OD:
- `OD.DOCUMENTO = CC.CMDESC`;
- `OD.CHEQUE = CC.NRDOC`;
- `OD.DESCRICAO = 'Rec :' + CC.NOME`;
- `OD.OLCODIGO`, `OD.GRUPO` e `OD.SGRUPO` vêm de `CM1), através do tipo de movimento `CC.CM`;
- `OD.BOACOBRANCA` recebe `CM1.BOACOBRANCA` quando `CL.COBNAO=0`, caso contrário recebe zero;
- os valores `OD.ENTR/EENTR` são os valores ainda não regularizados em CC, considerando também os campos de regularização e os ajustamentos de IVA ativado/registado.

Assim, o trigger confirma que OD é um registo de agenda/pendente de tesouraria derivado do movimento CC, e não um recibo nem uma segunda dívida independente.


## 20. Ciclo de vida do pendente OD associado a CC

A análise dos triggers confirma que não foi encontrado um trigger dedicado a INSERT/UPDATE/DELETE de OD para o circuito de conta corrente. O OD é mantido pelos triggers das tabelas de origem.

- `tr_cc_insert`: cria OD com `ODSTAMP=CCSTAMP`, desde que os valores remanescentes em moeda base e estrangeira sejam ambos diferentes de zero.
- `tr_cc_update`: elimina o OD associado ao movimento antigo (`OD.CCSTAMP=deleted.CCSTAMP`) e volta a inseri-lo a partir de `inserted`, se os dois saldos remanescentes continuarem diferentes de zero e `inserted.INCOBRA=0`. Recalcula data, valores, classificação e dados de localização/tesouraria.
- `tr_cc_delete`: elimina o OD associado ao movimento CC eliminado.
- `tr_rl_insert`, `tr_rl_update` e `tr_rl_delete`: alteram os campos de regularização de CC (`DEBF`, `CREDF`, `EDBF`, `ECREDF` e outros campos relacionados), provocando a execução de `tr_cc_update` e a reconstrução do OD.
- `tr_li_insert` e `tr_li_delete`: também atualizam os campos de regularização de CC, pelo que podem desencadear a reconstrução do OD.
- `tr_re_delete`: elimina movimentos CC associados ao recibo pelo `RESTAMP`; a eliminação de CC desencadeia `tr_cc_delete`, que elimina o OD.

Nota: a condição de criação do OD em `tr_cc_update` inclui `INCOBRA=0`; não se deve assumir que todos os movimentos CC originam sempre um pendente. A condição exige ainda saldo remanescente não nulo nas duas expressões monetárias.

Este circuito descreve o comportamento dos triggers analisados. A sequência efetiva de um recibo concreto deve ser confirmada com dados reais de RE/RL/CC/OD.


## 21. Estado inicial confirmado de CC e OD — fatura 429

Consulta conjunta de CC com LEFT JOIN a OD por CCSTAMP devolveu uma linha para o movimento da fatura 429.

| Campo | Valor | Conclusão |
|---|---:|---|
| CC.DEB | 365017.57740 | Débito original em moeda base |
| CC.DEBF | 0 | Sem débito regularizado |
| CC.CRED / CC.CREDF | 0 / 0 | Sem crédito nem crédito regularizado |
| CC.EDEB | 1820.700000 | Débito original em EUR |
| CC.EDEBF | 0 | Sem débito regularizado em EUR |
| CC.ECRED / CC.ECREDf | 0 / 0 | Sem crédito nem crédito regularizado em EUR |
| CC.INCOBRA | 0 | Não marcado como incobrável |
| CC.RECIBADO | 0 | Não marcado como recebido |
| CC.RECINO / RECIAN / RECINDOC | 0 / 0 / 0 | Sem referência de recibo nestes campos |
| CC.DATAVEN | 2026-11-05 | Vencimento da conta corrente |
| OD.ENTR / OD.EENTR | 365017.57740 / 1820.700000 | Pendente igual ao débito original |
| OD.SAID / OD.ESAID | 0 / 0 | Sem saída registada |
| OD.DATA | 2026-11-20 | Data de agenda calculada com ODATRASO |

Conclusão: o movimento está integralmente em aberto e o OD espelha exatamente os valores originais em aberto. Este resultado constitui o estado de referência anterior a qualquer regularização observada.


## 22. Primeiros exemplos reais de recibos e regularizações — RE/RL/CC

Foi alargada a pesquisa de recibos para toda a base, uma vez que não existiam linhas RL associadas ao cliente da fatura 429. A consulta devolveu vários recibos processados (RE.PROCESS=1, RL.PROCESS=1) e movimentos CC marcados como recebidos (CC.RECIBADO=1).

Exemplos observados:

- Recibo TN26102342812..., cliente 15922: movimento CC com CRED=15838.078 e CREDF=15838.078; o crédito está totalmente regularizado.
- Recibo RP26102254512..., cliente 5954: movimento CC com DEB=79992.318 e DEBF=79992.318; o débito está totalmente regularizado.
- Recibo ME26102062623..., cliente 15878: o mesmo recibo contém duas linhas RL para movimentos CC distintos, um com regularização negativa de 89.95 EUR e outro positiva de 89.95 EUR, correspondendo a movimentos de sinal oposto e valor equivalente.
- Recibo RP26101460123..., cliente 6613: duas linhas RL ligam movimentos CC distintos com valores opostos de 892.98 EUR, ilustrando regularização cruzada entre movimentos.

Nos exemplos devolvidos, o LEFT JOIN a OD não encontrou registos correspondentes. Isto é compatível com o comportamento já identificado em tr_cc_update, que elimina o OD anterior e só o recria quando existe saldo remanescente elegível. Contudo, estes exemplos não bastam, isoladamente, para provar a razão exata da ausência de cada OD.

A pesquisa devolveu exemplos de regularização total e de compensação entre movimentos, mas ainda não um caso inequívoco de regularização parcial. É necessário localizar um CC em que o valor regularizado seja diferente de zero e inferior ao valor original, e verificar se o OD contém apenas o remanescente.


## 23. Validação de regularização parcial — recibo 456 (Emboscada Lda)

Foi analisado o recibo real `ME26093051003,364000001`, número 456, processado em 2026-09-30 (`RE.PROCESS=1`, `RL.PROCESS=1`, `ANULADO=0`). O recibo contém duas linhas RL que regularizam parcialmente dois movimentos de débito CC distintos.

### Correspondência entre RL e acumulados CC

| Campo | Movimento 1 | Movimento 2 |
|---|---:|---:|
| CCSTAMP | TF26081140662,704328315 | TF26081143012,138055193 |
| CC.DEB | 423744.76966 | 2935.05648 |
| RL.ESCREC | 211001.29054 | 601.44600 |
| CC.DEBF | 211001.29054 | 601.44600 |
| CC.EDEB | 2113.630000 | 14.640000 |
| RL.EREC | 1052.470000 | 3.000000 |
| CC.EDEBF | 1052.470000 | 3.000000 |
| RL.REC | 0.00000 | 0.00000 |

Neste exemplo, as correspondências observadas são exatas:
- `RL.ESCREC = CC.DEBF`.
- `RL.EREC = CC.EDEBF`.

Assim, os valores em `ESCREC` e `EREC` das linhas RL correspondem aos acumulados de regularização que ficaram registados em CC. `RL.REC` é zero nas duas linhas, pelo que não deve ser usado isoladamente para inferir o montante regularizado. A interpretação dos campos deve ser validada noutros cenários/moedas antes de generalizar.

### Saldo remanescente e OD

Para o primeiro movimento:
- Saldo base: `DEB - DEBF = 212743.47912`.
- Saldo EUR: `EDEB - EDEBF = 1061.160000`.
- `OD.ENTR = 212743.47912` e `OD.EENTR = 1061.160000`.

Para o segundo movimento:
- Saldo base: `DEB - DEBF = 2333.61048`.
- Saldo EUR: `EDEB - EDEBF = 11.640000`.
- `OD.ENTR = 2333.61048` e `OD.EENTR = 11.640000`.

Os valores de OD coincidem exatamente com o saldo ainda não regularizado em CC, confirmando num caso real que o OD conserva o remanescente em aberto após regularização parcial. O recibo é comum às duas linhas RL e associa dois movimentos CC distintos.


## 24. Semântica dos campos de regularização RL confirmada pelo trigger

A leitura do código de `tr_rl_insert` explica a correspondência observada no recibo 456 e esclarece por que `RL.REC=0` não significa ausência de regularização.

No processamento de linhas RL com `PROCESS=1`, o trigger atualiza acumulados diferentes em CC consoante os campos e a condição cambial:

- `RL.ESCREC` alimenta `CC.DEBF` ou `CC.CREDF`, conforme o sinal. Quando `SUM(ECAMBIO)=0), o trigger considera também o arredondamento (`ESCREC - ARRED`); quando existe câmbio, usa `ESCREC).
- `RL.EREC` alimenta `CC.EDEBF` ou `CC.ECREDF`, também conforme o sinal, descontando `EARRED`.
- `RL.REC` alimenta os acumulados `CC.DEBFM` ou `CC.CREDFM` apenas quando `SUM(ECAMBIO)<>0), isto é, no ramo de regularização com componente cambial/moeda estrangeira.
- O trigger também atualiza acumulados de IVA/IRS ativados e diferenças associadas.

Isto explica o exemplo observado: `RL.REC=0`, enquanto `RL.ESCREC` e `RL.EREC` têm valores e correspondem aos acumulados `CC.DEBF` e `CC.EDEBF`. Não se deve interpretar `REC` isoladamente como valor pago/regularizado; a leitura correta depende da condição cambial e dos acumulados de destino.

O trigger exclui ainda do incremento normal os casos em que o recibo corresponde ao recibo já identificado em CC pelos campos `RECINO`, `RECIAN` e `RECINDOC`, evitando duplicar a regularização nesse cenário.


## 25. Ciclo inverso de regularizações — alteração e eliminação de RL

A leitura de `tr_rl_update` e `tr_rl_delete` confirma o mecanismo inverso dos acumulados de regularização.

### Alteração de RL (`tr_rl_update`)

O trigger executa duas fases distintas:
1. Usa a pseudo-tabela `deleted` para reverter em CC os efeitos da versão anterior das linhas RL processadas.
2. Usa a pseudo-tabela `inserted` para aplicar em CC os efeitos da nova versão das linhas RL processadas.

Na reversão e reaplicação são tratados, entre outros:
- `DEBF` / `CREDF`, com base em `ESCREC`, `CAMBIO` e arredondamentos;
- `DEBFM` / `CREDFM`, com base em `REC` e `ECAMBIO`;
- `EDEBF` / `ECREDF`, com base em `EREC` e `EARRED`;
- acumulados de IVA ativado e IRS.

O trigger também repõe/atualiza indicadores e dados de recibo em CC, incluindo `RECIBADO`, `RECINO`, `RECIAN`, `RECINDOC`, `VALRE`, `EVALRE` e `MVALRE`. A fase de aplicação da nova versão só considera linhas com `PROCESS=1), e inclui uma exclusão para o recibo já identificado em CC por `RECINO`, `RECIAN` e `RECINDOC`, evitando duplicação nesse caso.

### Eliminação de RL (`tr_rl_delete`)

O trigger reverte os acumulados de CC correspondentes às linhas eliminadas que estavam processadas (`deleted.PROCESS=1`). Reverte também os acumulados de IVA/IRS e os valores auxiliares de recibo. A alteração de CC desencadeia `tr_cc_update`.

### Consequência para OD

`tr_cc_update` elimina o OD associado ao movimento CC e volta a inseri-lo a partir dos valores atuais de `inserted), quando:
- o saldo remanescente em moeda base é diferente de zero;
- o saldo remanescente em euros é diferente de zero;
- `INCOBRA=0).

Os valores de entrada são recalculados a partir dos saldos de CC, incluindo os ajustamentos de IVA ativado/registado. Portanto, quando uma RL é alterada ou eliminada e isso modifica os acumulados de CC, o OD é reconstruído com o novo saldo elegível, em vez de ser simplesmente decrementado.

### Distinção: processamento do cabeçalho RE

O trigger `tr_re_update` tem lógica própria para o movimento CC do cabeçalho do recibo, com stamps derivados do `RESTAMP` (incluindo variantes com prefixos `I` e `F`). Quando `RE.PROCESS` passa a 0, elimina esses movimentos CC de cabeçalho e trata os respetivos movimentos auxiliares OL. Esta lógica é distinta da reversão das linhas de regularização RL; não se deve concluir, apenas por este código, que mudar `RE.PROCESS` elimina automaticamente as linhas RL.


## 26. Validação de PN após regularização parcial e regra de atualização de PAGO

Foi consultada a relação entre os dois movimentos CC parcialmente regularizados e PN, através de `CC.FTSTAMP = PN.FTSTAMP`. Resultado para ambos:

- `CC.RECIBADO=1);
- `CC.DEBF` e `CC.EDEBF` têm valores parciais;
- `PN.PAGO=0);
- `PN.PDATA=1900-01-01);
- `PN.PROCES=0), `PN.VALORPROC=0), `PN.EVALORPROC=0).

Os saldos permanecem positivos em ambas as moedas. Este resultado é coerente com `tr_cc_update`: a atualização de `PN.PAGO` associada a alterações dos acumulados de regularização só ocorre quando o saldo da conta corrente fica a zero em pelo menos uma das moedas:
- saldo base: `(DEB-DEBF)-(CRED-CREDF)=0), ou
- saldo EUR: `(EDEB-EDEBF)-(ECRED-ECREDF)=0).

Quando essa condição se verifica, o trigger atualiza `PN.PAGO` a partir do recibo mais recente associado à linha RL: consulta `TSRE.MOVINC) e atribui 1 quando `MOVINC=0), caso contrário 0. `PN.PDATA` recebe a data do recibo mais recente; na ausência de RL, usa a data mais recente de LI/LE ou, como último fallback, `GETDATE()).

O trigger também tem uma fase anterior que repõe `PN.PAGO=0) se a versão anterior do movimento CC estava totalmente liquidada e os acumulados relevantes forem alterados. A regra não se aplica a documentos cujo TD tenha `CCAUTOREG=1).

Conclusão: `CC.RECIBADO=1) não equivale a `PN.PAGO=1). O primeiro indicador pode estar ativo com regularização parcial; o segundo depende da condição de saldo liquidado e da configuração `TSRE.MOVINC). No caso analisado, o saldo parcial explica a manutenção de `PN.PAGO=0) e da data sentinela.


## 27. Semântica de CC.RECIBADO — não representa liquidação integral

A análise direta de `tr_rl_insert` explica o valor observado `CC.RECIBADO=1) nos dois movimentos parcialmente regularizados.

Quando uma linha RL processada (`PROCESS=1)) é associada a um movimento CC, o trigger atualiza a conta corrente com:
- `CC.RECIBADO=1);
- `CC.RECINO=RE.RNO);
- `CC.RECIAN=YEAR(RL.RDATA));
- `CC.RECINDOC=RE.NDOC);
- `CC.ULTDOC) com a descrição do recibo (ou recibo incobrável, conforme `TSRE.MOVINC)).

Esta atualização é feita no processamento da RL e não exige que o saldo da conta corrente fique a zero. Por isso, o recibo 456 deixou `CC.RECIBADO=1) nos dois movimentos, apesar de ambos continuarem parcialmente em aberto e de `PN.PAGO=0).

Conclusão confirmada pelo trigger: `CC.RECIBADO) significa que o movimento CC foi associado/identificado por um recibo processado; não é indicador de liquidação total. Para avaliar a dívida ainda em aberto, devem ser calculados os saldos de CC (`DEB-DEBF-CRED+CREDF) e equivalente em euros), e não usar `RECIBADO) isoladamente.


## 28. Recibo 456 — configuração TSRE e âmbito de regularização

A consulta a `RE`, `TSRE` e `RL` para `ME26093051003,364000001` confirmou:
- `RE.RNO=456), `RE.NDOC=1), `RE.PROCESS=1), `RE.ANULADO=0);
- `RE.PROCDATA=2026-09-30);
- `TSRE.MOVINC=0);
- `TSRE.INTROPAG=0);
- 16 linhas RL, todas com `RL.PROCESS=1).

O recibo regulariza, portanto, 16 movimentos CC distintos, incluindo os dois movimentos da Emboscada analisados em detalhe. Não se trata de um recibo limitado a esses dois documentos.

### Significado confirmado de MOVINC

O valor `MOVINC=0) significa que o tipo de recibo não está a seguir o ramo de movimento incobrável. Em `tr_cc_update`, quando uma conta corrente associada a uma fatura fica liquidada (saldo zero em pelo menos uma das expressões monetárias), o trigger consulta o recibo associado e atribui `PN.PAGO=1) quando `TSRE.MOVINC=0), caso se verifiquem as restantes condições, nomeadamente `TD.CCAUTOREG<>1).

### Significado observado de INTROPAG

A pesquisa de todas as ocorrências de `INTROPAG` no ficheiro de triggers encontrou utilização em `tr_rl_insert` e `tr_rl_delete` para atualizar `RD.INTEGRADO), através da ligação `CC.RDSTAMP = RD.RDSTAMP). Quando `TSRE.INTROPAG=1), o insert marca o registo RD como integrado; a eliminação da RL recalcula esse indicador considerando quantas contas correntes continuam ligadas ao mesmo RD. No recibo 456, `INTROPAG=0), pelo que este ramo não é ativado. Não atribuir outro significado a `INTROPAG` sem documentação adicional.

### Próxima validação sugerida

Para validar empiricamente a regra de `PN.PAGO), selecionar entre as 16 linhas RL deste recibo aquelas cuja conta corrente ficou com saldo zero numa das moedas e comparar o resultado com PN. A validação deve considerar `CC.FTSTAMP), `TD.CCAUTOREG) e a data/recibo associados, não apenas `CC.RECIBADO).


## 30. PN.PAGO é atualizado ao nível da fatura e pode ser revertido

A inspeção integral de `tr_cc_update` e `tr_cc_delete` acrescenta duas conclusões importantes:

1. O `UPDATE PN` usa `PN.FTSTAMP = CC.FTSTAMP`, não uma ligação por `PNSTAMP`/`FISTAMP`. Assim, quando uma conta corrente de uma fatura cumpre a condição, o trigger atualiza todas as linhas PN associadas à mesma fatura. Isto explica a multiplicação de linhas observada na consulta que ligou RL → CC → PN apenas por FTSTAMP.
2. A condição de saldo no trigger é uma disjunção: saldo base igual a zero **ou** saldo EUR igual a zero. Não exige que ambos sejam zero. O recibo 456 só demonstrou empiricamente o caso em que ambos são zero; ainda falta validar um caso em que apenas uma moeda tenha saldo zero.

### Reversão do estado

Em `tr_cc_update`, quando os campos de regularização são atualizados, o trigger primeiro pode definir `PN.PAGO=0) para as PN da fatura se a versão anterior do movimento CC estava liquidada (saldo zero em pelo menos uma moeda). Depois, se a versão nova continua a cumprir a condição de saldo zero, volta a calcular `PN.PAGO` e `PN.PDATA` com base no recibo/regularização mais recente.

O trigger `tr_cc_delete` também define `PN.PAGO=0) para as linhas PN da fatura quando o movimento CC eliminado estava liquidado (saldo zero em pelo menos uma moeda), exceto para documentos cujo tipo tenha `TD.CCAUTOREG=1).

Nota: o ramo que repõe `PN.PAGO=0) não repõe explicitamente `PN.PDATA); por isso não se deve assumir que a data é limpa em todas as reversões.

### Inventário de referências encontradas

Na pesquisa textual ao ficheiro global de triggers, as referências operacionais a `PN.PAGO` surgiram em `tr_cc_update` e `tr_cc_delete`. A primeira calcula o estado e a data a partir de RL/RE (com fallback para LI/LE ou data atual); a segunda repõe o indicador a zero em caso de eliminação do movimento CC liquidado.


## 31. Validação empírica: PN.PAGO=1 com saldo residual na moeda base

Foi executada uma pesquisa de movimentos CC em que exatamente uma das moedas tinha saldo zero, excluindo `TD.CCAUTOREG=1`. Surgiram vários casos classificados como `EUR ZERO`:

- `SALDO_EUR=0`
- `SALDO_BASE` diferente de zero, com resíduos positivos e negativos (por exemplo, 0,10; -0,06268; -0,29840; 0,47000).
- `PN.PAGO=1)
- `CC.RECIBADO=1)
- `TSRE.MOVINC=0)

Exemplo concreto:
- CCSTAMP/FTSTAMP: `TN25080745639,473270271`
- Saldo base: `0,10000`
- Saldo EUR: `0,000000`
- `PN.PAGO=1)
- `PN.PDATA=2025-08-07)
- Recibo associado: `TN25080745917,163000001`
- `MOVINC=0)

Outros exemplos de 2023 e 2022 confirmam o mesmo padrão, com resíduos base entre aproximadamente -0,50 e +0,50, enquanto o saldo EUR é exatamente zero.

### Conclusão validada

A condição de `tr_cc_update` é efetivamente uma disjunção: saldo base zero **OU** saldo EUR zero. O PHC marca `PN.PAGO=1) quando o saldo EUR fica a zero, mesmo que subsista um pequeno resíduo na moeda base. Isto é coerente com diferenças de arredondamento/conversão observadas nos exemplos, embora a causa exata de cada resíduo dependa do movimento e não fique demonstrada apenas por esta consulta.

A consulta agregou `PN.PAGO` com `MIN(CONVERT(INT, P.PAGO))` por FTSTAMP. Como o resultado foi 1 em todos os casos apresentados, todas as linhas PN de cada fatura tinham `PAGO=1), não apenas uma linha.

Os exemplos apresentados tinham `MOVINC=0), pelo que são recibos normais segundo o indicador observado em `TSRE`; não permitem concluir o comportamento de recibos incobráveis (`MOVINC=1).


## 32. PN.PAGO: recibo selecionado não é necessariamente o mais recente

Foi feita uma comparação em dados reais entre:
- o recibo usado pelo trigger para calcular `PN.PAGO`;
- o recibo usado para calcular `PN.PDATA`.

O código de `tr_cc_update` usa duas subconsultas distintas:

```sql
-- PN.PAGO
SELECT TOP 1
    CASE WHEN TSRE.MOVINC = 0 THEN 1 ELSE 0 END
FROM TSRE
INNER JOIN RE ON RE.NDOC = TSRE.NDOC
INNER JOIN RL ON RE.RESTAMP = RL.RESTAMP
WHERE RL.CCSTAMP = INSERTED.CCSTAMP
ORDER BY RE.PROCDATA, RL.OUSRHORA DESC

-- PN.PDATA
SELECT TOP 1 RE.PROCDATA
FROM RL
LEFT JOIN RE ON RL.RESTAMP = RE.RESTAMP
WHERE RL.CCSTAMP = INSERTED.CCSTAMP
ORDER BY RE.PROCDATA DESC
```

A primeira ordenação é crescente por `RE.PROCDATA` (não tem `DESC`); a segunda é decrescente. Assim, o recibo que determina `PN.PAGO` pode ser diferente do recibo que determina `PN.PDATA`.

A consulta de validação produziu estes exemplos:

| Cliente | Recibo escolhido para PAGO | Data | MOVINC | Recibo escolhido para PDATA | Resultado |
|---|---|---|---:|---|---:|
| Carla Alves & Juliana Pinto | ME16112135928,341000002 | 1900-01-01 | 0 | LV24041846577,549000001 (2024-04-18, MOVINC=1) | PAGO=1 |
| Tururac | PA24021650914,520000002 | 2024-02-16 | 1 | O mesmo recibo | PAGO=0 |
| Alto Decoracion | ME19111935598,668000001 | 2019-11-19 | 0 | PA24021646836,632000002 (2024-02-16, MOVINC=1) | PAGO=1 |
| Tavabu Multiaventura | ME19022740234,085000002 | 2019-02-27 | 0 | PA24021652704,960000002 (2024-02-16, MOVINC=1) | PAGO=1 |
| Nauticmar (FT NM180503...) | NM18050455205,759000001 | 2018-05-04 | 0 | ME22091940363,683000001 (2022-09-19, MOVINC=1) | PAGO=1 |
| Nauticmar (FT NM180822...) | ME22091940363,683000001 | 2022-09-19 | 1 | O mesmo recibo | PAGO=0 |

Em todos os exemplos, os saldos base e EUR são zero, `CC.RECIBADO=1`, e os valores `PN.PAGO_MIN` e `PN.PAGO_MAX` coincidem.

### Conclusão

O valor de `PN.PAGO` é calculado com base no primeiro recibo segundo `RE.PROCDATA ASC, RL.OUSRHORA DESC`, e não no último recibo nem necessariamente no recibo mais recente que esteja processado e não anulado.

A subconsulta de `PN.PAGO` não inclui filtros para `RE.PROCESS=1), `RE.ANULADO=0) ou `RL.PROCESS=1`. Portanto, associações a recibos antigos, incluindo registos com `PROCDATA=1900-01-01), podem determinar o estado final. O exemplo de Carla demonstra diretamente este efeito.

A subconsulta de `PN.PDATA` escolhe separadamente a maior `RE.PROCDATA). Por isso, é perfeitamente possível ter `PN.PAGO=1) com `PN.PDATA` correspondente a um recibo mais recente com `MOVINC=1).

Esta é uma característica importante do código observado; não deve ser interpretada como uma avaliação global de todos os recibos ou de todas as regularizações da fatura.


## 33. Validação do histórico RL: associações antigas e não processadas influenciam PN.PAGO

Foi consultado o histórico completo de RL/RE para seis movimentos CC que tinham recibos com `MOVINC=1`, saldo base e EUR zero, mas resultados distintos em `PN.PAGO`.

### Caso Carla Alves & Juliana Pinto (CC NM16111776872,747812370)

| Recibo | PROCDATA | RE.PROCESS | RE.ANULADO | RL.PROCESS | MOVINC | Ordem PAGO | Ordem DATA |
|---|---|---:|---:|---:|---:|---:|---:|
| ME16112135928,341000002 | 1900-01-01 | 0 | 0 | 0 | 0 | 1 | 4 |
| ME16112135858,074000001 | 2016-11-25 | 1 | 0 | 1 | 0 | 2 | 3 |
| PA24021646701,476000002 | 2024-02-16 | 0 | 1 | 0 | 1 | 3 | 2 |
| LV24041846577,549000001 | 2024-04-18 | 1 | 0 | 1 | 1 | 4 | 1 |

O recibo que determina `PN.PAGO` é o primeiro: data 1900-01-01, `RE.PROCESS=0`, `RL.PROCESS=0`, `RE.ANULADO=0`, `MOVINC=0`. Como a subconsulta de `PN.PAGO` não filtra estado de processamento nem anulação, este registo é selecionado e produz `PN.PAGO=1).

O recibo mais recente, de abril de 2024, é selecionado separadamente para `PN.PDATA), apesar de ser incobrável (`MOVINC=1)).

### Outros casos

- **Alto Decoracion:** dois recibos normais mais antigos (2019 e 2020), seguidos de recibo incobrável de 2024. O primeiro recibo normal determina `PN.PAGO=1); o de 2024 determina `PN.PDATA).
- **Tavabu Multiaventura:** recibo normal de 2019 antes do incobrável de 2024. `PN.PAGO=1).
- **Nauticmar (CC NM180503...):** dois recibos normais de 2018 antes do recibo incobrável de 2022. `PN.PAGO=1).
- **Nauticmar (CC NM180822...):** apenas o recibo incobrável de 2022 na associação consultada. `PN.PAGO=0).
- **Tururac:** apenas um recibo incobrável de 2024. `PN.PAGO=0).
- No histórico da Carla, também existe um recibo de 2024 anulado e não processado; ele aparece na ordenação de histórico, mas não ultrapassa os recibos anteriores.

### Conclusão técnica

A seleção para `PN.PAGO` considera as associações RL existentes para o CC, sem filtrar `RL.PROCESS), `RE.PROCESS) ou `RE.ANULADO). Um registo histórico não processado, com `RE.PROCDATA=1900-01-01) e `MOVINC=0), pode assim prevalecer sobre recibos processados posteriores com `MOVINC=1).

A seleção para `PN.PDATA` é independente e usa a maior `RE.PROCDATA). O resultado final pode, por isso, combinar o indicador de um recibo histórico antigo com a data de um recibo posterior.

Importante: esta observação explica a seleção feita pela subconsulta do trigger. Não prova que os registos RL não processados participem nos acumulados financeiros; esses acumulados são tratados por lógica própria e, nos exemplos, os saldos CC já estavam a zero.


## 34. Validação dos níveis de stock — fatura 429

Foi consultado o movimento de stock associado às três linhas da fatura `ADM26110433780,615038245`.

### Movimentos SL

Foram encontrados exatamente três movimentos, todos com:
- `ORIGEM='FT'`;
- `CM=75`;
- `ARMAZEM=1`;
- `QTT=1);
- `SLSTAMP=FISTAMP).

As referências são AG1636, AG1669 e AG1678. Isto confirma nos dados concretos que cada linha FI da fatura originou um movimento SL de saída no armazém 1.

### Stock global e por armazém

| REF | ST.STOCK | QTTACIN | QTTACOUT | SA.STOCK no armazém 1 |
|---|---:|---:|---:|---:|
| AG1636 | 1 | 28 | 27 | 1 |
| AG1669 | 6 | 114 | 108 | 6 |
| AG1678 | 0 | 95 | 95 | 0 |

Os valores de `ST.STOCK` coincidem com os de `SA.STOCK` no armazém 1 para os três artigos. Os restantes registos SA devolvidos para os armazéns existentes apresentam stock zero.

### Stock por cor/tamanho

A estrutura de `SX` foi inspecionada. Os campos dimensionais são `COR` e `TAM) (não existe uma coluna `TAMANHO)). A consulta por estas três referências não devolveu registos SX; apenas os cabeçalhos das colunas.

### Conclusão e limite

A evidência confirma o percurso FI → SL → atualização dos níveis ST e SA para esta fatura. Não há registos SX para estas referências na consulta efetuada, pelo que não se observou stock discriminado por cor/tamanho neste caso.

Os saldos e acumulados apresentados são valores atuais. Sem um snapshot anterior à fatura ou uma reconstrução completa do histórico de movimentos, não se deve atribuir a diferença entre entradas e saídas exclusivamente a esta fatura.


## 35. Proveniência dos custos — ligação SL → FN nas entradas de compra

Para os três artigos da fatura 429, foram identificados os últimos movimentos de entrada cujo tipo está configurado para atualizar custos (CM2.MUDAPCULT=1 e/ou MUDAPCPOND=1). Os três movimentos são de CM=1 (V/Factura), mas não têm SL.FISTAMP preenchido. A ligação documental está em SL.FNSTAMP, que corresponde ao FN.FNSTAMP.

Os registos FN consultados têm origem documental FO e apresentam estes valores:

| REF | FN.QTT | FN.SLVU | FN.ESLVU | SL.PCPOND | SL.EPCPOND | ST.PCULT atual | ST.EPCULT atual |
|---|---:|---:|---:|---:|---:|---:|---:|
| AG1636 | 10 | 52 660,20 | 262,67 | 57 091,31111 | 284,772222 | 52 660,20 | 262,67 |
| AG1669 | 20 | 63 763,40 | 318,05 | 64 243,81613 | 320,445161 | 63 763,40 | 318,05 |
| AG1678 | 8 | 24 846,40 | 123,93 | 23 944,84800 | 119,435008 | 24 846,40 | 123,93 |

### Conclusões suportadas pelos dados

- A associação correta destes movimentos é SL.FNSTAMP = FN.FNSTAMP; procurar por FI.FISTAMP não encontra as linhas, porque SL.FISTAMP está vazio nestes movimentos.
- Nos três exemplos, FN.SLVU e FN.ESLVU coincidem exatamente com ST.PCULT e ST.EPCULT atuais.
- Os valores SL.PCPOND/SL.EPCPOND são diferentes dos valores unitários FN.SLVU/FN.ESLVU. Não devem ser tratados como sinónimos do custo unitário da linha de compra.
- A configuração CM2 confirma que CM=1 pode atualizar último custo e custo ponderado (MUDAPCULT=1, MUDAPCPOND=1). Já os movimentos CM=13 (Sobra) e CM=47 (GT Transf. Interna E) têm ambas as opções a zero.
- Esta amostra mostra correspondência entre o custo unitário da linha de compra e o último custo atual, mas, sem reconstruir todos os movimentos posteriores e a lógica integral de atualização, não prova isoladamente que a última compra seja sempre a origem do valor atual.

### Limite da análise

A diferença entre SL.PCPOND e ST.PCPOND continua por explicar. A consulta atual não demonstra que SL.PCPOND seja o custo ponderado final aplicado ao artigo, nem permite reconstruir o cálculo histórico de ST.PCPOND. Manter esta questão separada da correspondência já confirmada para PCULT/EPCULT.


## 36. Custos da fatura 429 — FI, SL e custos atuais de ST

Foi feita uma comparação direta entre as linhas FI da fatura 429, os movimentos SL associados por FISTAMP e os custos atualmente guardados em ST.

| REF | QTT | FI.CUSTO | FI.PCP | SL.PCPOND | ST.PCULT | ST.PCPOND | FI.ECUSTO | FI.EPCP | SL.EPCPOND | ST.EPCULT | ST.EPCPOND |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| AG1636 | 1 | 57 091,70461 | 57 091,70461 | 57 091,70461 | 52 660,20000 | 63 434,79012 | 284,772222 | 284,772222 | 284,772222 | 262,670000 | 284,772222 |
| AG1669 | 1 | 64 243,48677 | 64 243,48677 | 64 243,48677 | 63 763,40000 | 67 455,99047 | 320,445161 | 320,445161 | 320,445161 | 318,050000 | 320,445161 |
| AG1678 | 1 | 23 917,66880 | 23 917,66880 | 23 917,66880 | 24 846,40000 | 71 474,52266 | 119,300829 | 119,300829 | 119,300829 | 123,930000 | 119,300829 |

### Conclusões confirmadas nesta fatura

- Nos três artigos, FI.CUSTO = FI.PCP = SL.PCPOND.
- Nos três artigos, FI.ECUSTO = FI.EPCP = SL.EPCPOND.
- A quantidade em FI e SL é 1 em todas as linhas, e o movimento associado é CM=75.
- Os custos unitários registados na saída da fatura são, portanto, os valores da própria linha FI; não são uma cópia dos valores atuais de ST.PCPOND.
- ST.PCULT difere do custo da fatura nos três artigos.
- ST.PCPOND também difere do custo da fatura nos três artigos. Para AG1678, a diferença é particularmente expressiva.
- Nesta amostra, ST.EPCPOND coincide com o custo em euros da fatura nos três artigos, apesar de ST.PCPOND divergir em moeda principal. Regista-se a coincidência observada, sem inferir uma regra geral.

### Limite da conclusão

Esta comparação fecha a proveniência dos custos usados nos três movimentos de saída da fatura 429. Não explica o algoritmo histórico que formou ST.PCPOND, nem demonstra por que motivo os valores atuais em moeda principal diferem. Essa investigação fica fora do âmbito desta análise, salvo se surgir uma necessidade concreta que dependa dela.
