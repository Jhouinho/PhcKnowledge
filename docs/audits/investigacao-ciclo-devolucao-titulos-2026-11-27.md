# Investigação funcional — ciclo de vida e devolução de títulos (2026-11-27)

> **Natureza:** dossier de investigação funcional, não especificação canónica do produto PHC Knowledge.  
> **Âmbito:** observações efectuadas numa cópia de testes PHC CS, base `cópia de testes do cliente`.  
> **Data dos testes:** 27-11-2026.  
> **Estado:** evidência empírica recolhida em dois cenários; mecanismo standard correlacionado com os resultados; ainda há validações pendentes.

## 1. Objectivo

Compreender o ciclo de vida de um cheque/título de cliente e os seus reflexos nas tabelas de títulos (`CH`/`CHL`), depósitos (`OY`/`OX`), tesouraria (`OL`) e conta corrente (`CC`), distinguindo:

1. criação/aceitação;
2. depósito;
3. devolução;
4. reflexo em conta corrente e tesouraria.

Todos os testes descritos foram efectuados pelo utilizador na interface PHC, sobre a cópia `cópia de testes do cliente`, nunca sobre a base original/produção.

## 2. Regras de interpretação

- Os valores `valor` e `evalor` foram preservados tal como devolvidos pelo SQL. Não se deve assumir que `valor` representa euros directos: os testes mostram também um valor em moeda estrangeira.
- Distinguir sempre dados observados na base, comportamento encontrado no ficheiro de triggers standard e inferências sobre a sequência funcional.
- O ficheiro de triggers do GitHub é uma fonte de código standard de referência. Os corpos dos triggers instalados em `cópia de testes do cliente` ainda não foram comparados integralmente com essa fonte.
- A ausência de linhas numa pesquisa específica não prova ausência de qualquer reflexo noutro local/tabela.
- Não assumir que os dois cenários esgotam todas as variantes de configuração PHC.

## 3. Cenário A — título criado directamente na gestão de títulos

### 3.1 Título e aceitação

Título:
- `CH.chstamp = CH-CENARIO-A`
- Cliente: Cliente A
- Cheque: `CHEQUE-A`, banco Abanca.
- `valor = [valor do título]`; `evalor = [equivalente do título]`.
- `origem = 'CH'`.
- Após devolução: `devol = 1`, `status = 3`.
- Após depósito: `jadepo = 1`, `contado = 1`.
- `restamp`, `ftstamp`, `ftccstamp` e `rdstamp` vazios.

Histórico `CHL`:
1. `cm=1`, descrição `Aceite`, valor [valor do título] / evalor [equivalente do título], `contado=1`, origem CH.
2. `cm=3`, descrição `Devolução do Título`, valor 0 / evalor 0, `contado=1`, origem CH, `olxorigem='Caixa'`.

### 3.2 Depósito

O depósito criou:
- um registo `OY` ligado ao título por `chstamp` e ao movimento `OX` por `oxstamp`;
- um registo `OX` com `cm=2`, `chope='Depósito do Título'`, valor [valor do título] / evalor [equivalente do título];
- dois movimentos `OL` de origem OX:
  - entrada no BCP: [valor do título] / [equivalente do título];
  - saída da Caixa: [valor do título] / [equivalente do título].

### 3.3 Devolução

A devolução criou dois movimentos `OL` ligados ao movimento `CHL` de devolução através de `chlstamp`:
- entrada na Caixa: [valor do título] / [equivalente do título];
- saída do BCP: [valor do título] / [equivalente do título].

Os movimentos de devolução têm `origem='CH'`, `olcodigo='Trf'`, grupo/subgrupo `Transferências`, `transf=1`, sem `oxstamp` e sem `restamp`. São a inversão dos dois movimentos de depósito observados.

Não foram encontradas linhas em `CC) através das pesquisas efectuadas por `chstamp`, número do cheque e nome/data neste cenário. Isto não permite concluir que todos os circuitos PHC dispensem reflexos em CC.

## 4. Cenário B — título criado através de recibo

### 4.1 Recibo e título

Recibo:
- `RE.restamp = RE-CENARIO-B`
- `rno = 551`, data 27-11-2026.
- Cliente: Cliente B
- `total = [total do recibo]`; `etotal = [equivalente do recibo]`.
- `chtotal = [valor do título]`; `echtotal = [equivalente do título]`.
- `temch = 1).
- `olcodigo='R[equivalente do título]1'`; `ollocal='Caixa'`.
- `contado=0).
- `process=0), `revdata=1900-01-01), `anulado=0).

Título criado pelo recibo:
- `CH.chstamp = CH-CENARIO-B`
- Cheque `CHEQUE-B).
- `valor=[valor do título]); `evalor=[equivalente do título]).
- `origem='RE'`; `restamp=RE-CENARIO-B`.
- Antes do depósito: `devol=0), `status=1), `jadepo=0).
- Após depósito: `jadepo=1), `dataoy=2026-11-27), `status=2), `devol=0).

O registo `RECH` do cheque tinha `rechstamp) igual a `CH.chstamp), e ligava o cheque ao recibo por `restamp).

### 4.2 Movimentos originais do recibo

Antes da devolução foram observados:

**Conta corrente (`CC`):**
- `ccstamp = RE-CENARIO-B`
- `restamp = RE-CENARIO-B`
- `origem='RE'`, `cmdesc='N/Recibo'`, `nrdoc=551).
- Crédito [total do recibo] / 1568.67.

**Tesouraria (`OL`):**
- `olstamp = RE-CENARIO-B`
- `restamp = RE-CENARIO-B`
- `origem='RE'), entrada [total do recibo] / 1568.67.
- `ollocal='Caixa'`, `local='C'`.
- Grupo `Actividades Operacionais`, subgrupo `Recebimentos de Clientes`, código `R[equivalente do título]1).

Estes movimentos reflectiam o total do recibo, não apenas o valor individual do cheque. A diferença entre o total do recibo e o cheque não foi explicada pelos campos de pagamento consultados; não se deve inferir a sua composição.

### 4.3 Depósito

O depósito criou:
- `OY.oystamp = OY-CENARIO-B`, com número CHEQUE-B, valor [valor do título] / evalor [equivalente do título], `chstamp` do título, `restamp` do recibo e `oxstamp` do movimento OX.
- `OX.oxstamp = OX-CENARIO-B`, `cm=2), operação `Depósito do Título`, origem/localização Caixa → BCP.
- Dois movimentos `OL` com `origem='OX'`, `olcodigo='Trf'`, grupo/subgrupo `Transferências`, `transf=1):
  - entrada no BCP: [valor do título] / [equivalente do título], `contado=1);
  - saída da Caixa: [valor do título] / [equivalente do título], `contado=0).

O histórico `CHL` não recebeu um movimento adicional de depósito; manteve apenas o movimento de aceitação (`cm=1), origem RE.

### 4.4 Devolução

Após a devolução:
- `CH.devol=1), `CH.status=3), `CH.jadepo=1), `CH.contado=0).
- O título manteve `origem='RE'` e o `restamp` do recibo.
- Foi criado `CHL`:
  - `chlstamp = CHL-DEVOLUCAO-CENARIO-B`;
  - `cm=3), descrição `Devolução do Título`;
  - valor 0 / evalor 0;
  - `contado=3), `origem='CH'), `olxorigem='Caixa'`.
- O movimento de aceitação anterior manteve `cm=1), valor [valor do título] / evalor [equivalente do título], `origem='RE'`.

A devolução criou dois movimentos `OL` ligados pelo `chlstamp` da devolução:
- entrada na Caixa: [valor do título] / [equivalente do título], `contado=0);
- saída do BCP: [valor do título] / [equivalente do título], `contado=1).

Ambos têm `origem='CH'`, `olcodigo='Trf'`, grupo/subgrupo `Transferências`, `transf=1), sem `oxstamp` e sem `restamp`. Também neste cenário os movimentos invertem a transferência do depósito.

## 5. Desaparecimento dos movimentos originais de RE

No cenário B, depois da devolução, as pesquisas pelos identificadores exactos deixaram de encontrar:
- o movimento `CC` cujo `ccstamp` e `restamp` eram o `RE.restamp`;
- o movimento `OL` cujo `olstamp` e `restamp` eram o `RE.restamp`.

O recibo permaneceu em `RE`, com os valores originais, `anulado=0), `process=0) e `revdata=1900-01-01).

Uma pesquisa adicional em `CC` por `nrdoc=551) devolveu documentos de vários anos. Não foi possível identificar um movimento compensatório de 27-11-2026 nessa consulta; o número do documento não é chave única.

### 5.1 Evidência temporal

Auditoria do recibo:
- criação: `RE.ousrdata=2026-11-27), `RE.ousrhora=15:55:02), utilizador de teste;
- última alteração: `RE.usrdata=2026-11-27), `RE.usrhora=16:07:12), utilizador de teste.

Auditoria do depósito:
- `OY.ousrhora=16:04:11).

Auditoria do movimento CHL de devolução:
- `CHL.ousrdata=2026-11-27), `CHL.ousrhora=16:07:12), utilizador de teste;
- `CHL.usrdata=2026-11-27), `CHL.usrhora=16:07:12), utilizador de teste.

A coincidência exacta entre a última alteração do recibo e a criação do movimento de devolução é evidência temporal forte de que ambas fazem parte da mesma operação funcional. Não constitui, isoladamente, prova da ordem interna das instruções SQL.

## 6. Correlação com o código standard PHC

Fonte consultada:
- Repositório: `Jhouinho/PhcKnowledge`.
- Ficheiro: [`docs/sources/triggers phc.sql`](../sources/triggers%20phc.sql).
- Blob SHA consultado: `f9949635b32e265129f2aed3803d12307633c842).

### 6.1 `tr_chl_insert`

O bloco de `cm=3` procura os movimentos originais do depósito através da cadeia `OL.oxstamp → OX → OY → CH` e cria movimentos inversos em `OL`, com `origem='CH'` e `chlstamp` do movimento CHL inserido.

O comportamento corresponde aos movimentos observados nos dois cenários. O trigger não contém instruções de eliminação nem referências directas a `CC` ou `RE` no bloco de devolução analisado.

### 6.2 `tr_re_update`

O trigger é executado em UPDATE de `RE` e inclui `update(process)) entre os campos que activam o bloco principal.

Quando a linha em `inserted` tem `process=0), `revdata='19000101'`:
- elimina de `CC` o movimento cujo `ccstamp` corresponde directamente a `inserted.restamp`;
- elimina de `CC` movimentos auxiliares com identificadores derivados de `I + restamp` e `F + restamp`;
- elimina de `OL` o movimento cujo `restamp` corresponde ao recibo;
- elimina de `OL` o movimento associado ao identificador derivado de `2 + restamp`, nas condições previstas no código.

O trigger contém ainda lógica de criação/actualização de movimentos quando `process=1).

**Correspondência observada:** os valores finais de `RE.process` e `RE.revdata` satisfazem as condições dos blocos de limpeza, e os movimentos principais de `CC` e `OL` com o `restamp` do recibo deixaram de existir após a operação de devolução.

**Limite da conclusão:** não foi capturada a instrução UPDATE concreta que o PHC executou em `RE`. A igualdade de hora de auditoria entre `RE) e `CHL), juntamente com o código standard, sustenta fortemente a associação funcional, mas não prova por si só a ordem exacta nem que o trigger instalado na cópia é byte-a-byte equivalente ao ficheiro de referência.

### 6.3 Triggers `tr_cc_*`

Na cópia `cópia de testes do cliente` foram encontrados os triggers activos `tr_cc_delete`, `tr_cc_insert) e `tr_cc_update`. A consulta a `OBJECT_DEFINITION` devolveu NULL, pelo que os corpos instalados não ficaram disponíveis nessa consulta.

No ficheiro standard, os triggers `tr_cc_*` não foram identificados como origem da decisão de apagar os movimentos do recibo. `tr_cc_delete` reage a eliminações em CC; não é, por si, a instrução que determina a eliminação do registo.

### 6.4 `tr_re_delete`

O código standard contém eliminação de movimentos `CC` e `OL` quando um registo `RE` é eliminado. Contudo, no cenário B o recibo continua presente em `RE`. Portanto, `tr_re_delete` não é a explicação demonstrada para o desaparecimento observado.

## 7. Conclusões suportadas até ao momento

1. Nos dois cenários testados, a devolução de um título depositado cria um movimento `CHL.cm=3` e dois movimentos `OL` que invertem a transferência de depósito.
2. O comportamento da inversão de tesouraria é consistente entre um título criado directamente e um título criado através de recibo.
3. No cenário originado em recibo, o recibo cria inicialmente um movimento de crédito em `CC` e um movimento de entrada em `OL`, ambos pelo total do recibo.
4. Após a devolução, os movimentos originais de `CC` e `OL` identificados pelo `restamp` do recibo deixaram de ser encontrados, enquanto o recibo e o título permaneceram.
5. O código standard de `tr_re_update` contém blocos de limpeza compatíveis com os identificadores e condições observados.
6. A última alteração do recibo e a criação do movimento CHL de devolução têm a mesma data/hora e utilizador.
7. Ainda não está identificado qualquer movimento compensatório em `CC`; a pesquisa feita por `nrdoc=551) não é conclusiva por devolver documentos de vários anos.

## 8. Questões em aberto e próximos passos

1. **Confirmar o efeito em CC:** pesquisar movimentos do cliente Cliente B na data do teste, com critérios que não dependam apenas de `nrdoc`, para identificar eventual movimento compensatório.
2. **Confirmar o UPDATE de RE:** se necessário, repetir o teste com auditoria SQL/Extended Events ou outro mecanismo de captura numa cópia descartável, para observar a instrução que actualiza `RE` e a ordem dos triggers.
3. **Comparar triggers instalados:** obter os corpos dos triggers instalados em `cópia de testes do cliente` por um mecanismo autorizado (a consulta anterior a `OBJECT_DEFINITION` devolveu NULL) e compará-los com o ficheiro standard do GitHub.
4. **Investigar `contado`:** explicar a diferença observada no `CHL.cm=3` entre os cenários (`contado=1) no cenário A e `contado=3) no cenário B).
5. **Explicar a composição do recibo:** determinar a origem da diferença entre o total do recibo ([total do recibo] / 1568.67) e o cheque ([valor do título] / [equivalente do título]), sem inferir meios de pagamento não observados.

## 9. Consultas e identificadores de referência

Base de testes: `cópia de testes do cliente`.

| Elemento | Identificador |
|---|---|
| Cenário A — CH | `CH-CENARIO-A` |
| Cenário B — RE | `RE-CENARIO-B` |
| Cenário B — CH | `CH-CENARIO-B` |
| Cenário B — OY | `OY-CENARIO-B` |
| Cenário B — OX | `OX-CENARIO-B` |
| Cenário B — CHL devolução | `CHL-DEVOLUCAO-CENARIO-B` |

---

**Nota de proveniência:** este documento regista uma investigação realizada sobre uma base de testes de um cliente. Os dados identificativos e os valores são mantidos apenas para permitir a reprodução e rastreabilidade desta análise; não devem ser tratados como dados de referência PHC universais nem como conteúdo do baseline standard CSDefault.


## 10. Verificação adicional — origem de CHL.contado na devolução

Esta secção regista a continuação da análise efectuada após a primeira versão deste dossier. Todas as verificações SQL foram realizadas na cópia `cópia de testes do cliente), e a análise dos triggers usou o ficheiro standard versionado no GitHub.

### 10.1 Triggers instalados em CH e CHL

A consulta a `sys.triggers` confirmou exactamente os seis triggers seguintes, todos activos e do tipo AFTER (não INSTEAD OF):

| Tabela | Trigger | Estado |
|---|---|---|
| CH | `tr_ch_delete` | Activo |
| CH | `tr_ch_insert` | Activo |
| CH | `tr_ch_update` | Activo |
| CHL | `tr_chl_delete` | Activo |
| CHL | `tr_chl_insert` | Activo |
| CHL | `tr_chl_update` | Activo |

Não foram encontrados triggers adicionais em `CH) ou `CHL) na cópia testada.

### 10.2 Comparação do campo contado no cenário B

Consulta ao título `CH-CENARIO-B` e aos respectivos registos de histórico:

| Histórico | cm | Descrição | CH.contado | CHL.contado |
|---|---:|---|---:|---:|
| Aceitação | 1 | Aceite | 0 | 0 |
| Devolução | 3 | Devolução do Título | 0 | 3 |

O título principal permaneceu com `CH.contado=0). O registo de aceitação também permaneceu com `CHL.contado=0). O valor 3 aparece apenas no novo registo de devolução.

### 10.3 Análise dos triggers standard

Foi consultado o conteúdo do blob `f9949635b32e265129f2aed3803d12307633c842` do ficheiro `docs/sources/triggers phc.sql` na branch `main`.

- `tr_chl_insert): no bloco `cm=3`, cria os movimentos inversos em `OL`, copiando `contado` de `inserted). Não calcula nem actualiza `CHL.contado`.
- `tr_chl_update): trata alterações ao campo `plano`; não altera `contado`.
- `tr_ch_update): pode copiar `CH.contado` para `CHL.contado` apenas no bloco de substituição, quando `CH.choristamp` está preenchido e o histórico tem `cm=4`. Não abrange a devolução observada (`cm=3), nem os títulos testados com `choristamp` vazio.
- `tr_ch_insert): cria históricos de aceitação (`cm=1)) ou substituição (`cm=4)); não cria o histórico de devolução.
- `tr_chl_delete): não participa na inserção observada do histórico de devolução.

A consulta de inventário aos módulos SQL instalados que continham referências simultâneas a `CHL` e `contado` não devolveu resultados. A pesquisa anterior por módulos com referências a `CHL` e operações de escrita devolveu apenas procedimentos internos de controlo de versões do SQL Server, sem relação demonstrada com o circuito de títulos.

### 10.4 Conclusão e limite

Na cópia testada, os seis triggers instalados em `CH` e `CHL` coincidem em nome e estado com o conjunto standard consultado. A análise dos corpos standard não identificou lógica que calcule o valor `CHL.contado=3` no registo de devolução.

A evidência permite concluir que o valor 3 já está presente em `inserted.contado` quando `tr_chl_insert` executa. Não permite identificar definitivamente quem o atribui. A aplicação PHC é uma origem possível, mas não foi inspeccionado o código da aplicação nem capturada a instrução SQL que insere o registo.

**Questão não resolvida:** significado funcional e origem exacta dos valores `CHL.contado=1` e `CHL.contado=3` nos dois cenários.

**Decisão de encerramento desta linha de análise:** não continuar a percorrer triggers, procedimentos ou módulos SQL sem uma nova evidência concreta que indique um ponto de entrada. A investigação fica documentada com a conclusão limitada acima; não se deve transformar a hipótese sobre a aplicação PHC num facto confirmado.

