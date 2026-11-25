# Investigação funcional PHC — CS Default: FT, FI e TD

**Data de consolidação:** 25-11-2026  
**Estado:** investigação exploratória em curso; não é uma especificação universal do PHC.  
**Âmbito inicial:** base `CSDefault202603`, tabelas FT, FI e TD.  
**Repositório:** `Jhouinho/PhcKnowledge`, branch `main`.

## 1. Objectivo e método

Investigar o papel funcional das tabelas FT (Documentos de Faturação), FI (Linhas de Documentos de Faturação) e TD (Configuração de Documentos de Faturação), cruzando três fontes:

1. Captura IKP da base CS Default.
2. Script de triggers PHC disponibilizado para esta análise.
3. Enciclopédia PHC disponibilizada em ZIP.

A análise separa:
- **Facto observado:** informação directamente presente numa das fontes.
- **Relação declarada:** relação PHC REL/DTL identificada e resolvida na captura.
- **Comportamento observado no código:** condição e efeito explícitos num trigger do script.
- **Interpretação funcional:** leitura conjunta das fontes, ainda circunscrita às evidências.
- **Por validar:** questão que exige esquema físico, execução controlada, outra fonte ou confirmação numa instalação.

Não se assume que o comportamento observado na CS Default seja universal a todas as versões, módulos, configurações ou instalações PHC.

## 2. Proveniência das fontes

### 2.1 Captura IKP

- Base de origem: `CSDefault202603`
- Contrato: IKP 0.1
- Scanner: `PhcKnowledgeScanner 1.0.0+960eed18828bcbe60d28d072ef50bc0e3e4baf35`
- PackageId: `c570ee50-9beb-4025-af5b-51282b911c02`
- CaptureId: `908d8e40-9b75-45ea-a3e3-30b016d9c696`
- Modo: `Full`
- MetadataStatus: `Complete`
- Object Index: 17 182 objectos
- Tabelas: 8 294
- Primary keys: 8 268
- Triggers: 564
- Stored procedures: 36
- Relações declaradas: 921 (295 REL + 626 DTL)

SHA-256 do ZIP recebido nesta análise: `dfb21a98a01fd40e0eb7452d1139b987c4415d43a98dbb70ed13a3b9d8875a2e`.

A captura tem inventário de tabelas e chaves primárias, mas **não recolheu colunas físicas, tipos de dados nem índices**. A operação de foreign keys SQL terminou com sucesso e conteúdo vazio (0); isso não invalida nem substitui as relações PHC REL/DTL.

A operação de triggers foi pedida e observou 564 triggers, mas terminou `SucceededWithWarnings`, com conteúdo `Partial`. As definições aparecem como `Unavailable` no Object Index. Para esta análise, o texto legível do script de triggers é uma fonte independente e complementar.

### 2.2 Script de triggers

- Ficheiro de origem no repositório: `docs/sources/triggers phc.sql`
- Script analisado: 1 215 789 bytes; 21 713 linhas.
- Declarações `CREATE TRIGGER`: 552; nomes únicos: 552.
- SHA-256 do ficheiro recebido: `ca947c507249a19f4b848ed05a03f321294a8ef81eff223f3cd1b98d0b4d6c08`.

Os triggers estão declarados no script com `WITH ENCRYPTION`, mas o texto fornecido está legível. Não se deve confundir a legibilidade deste ficheiro com a disponibilidade das definições na captura IKP.

### 2.3 Enciclopédia PHC

- ZIP recebido: 176 745 503 bytes.
- Entradas: 10 928.
- Conteúdo identificado: 4 393 ficheiros `.htm`, 3 888 `.gif`, 2 619 `.jpg`, 3 `.bmp`, 1 `.hhc`, 1 `.hhk`, 1 `.css` e 22 entradas sem extensão.
- SHA-256 do ZIP recebido: `f4129bb0e369fa3911c2edeac8d31e60a3c6fbad758891b33c03367de401fe0e`.

Foram abertos e lidos, nesta primeira passagem, os seguintes artigos:
- `funcionamentodafaturação.htm` — Funcionamento da Faturação.
- `configuraçãodedocumentosdefaturação.htm` — Configuração de Documentos de Faturação.
- `configuraçãodedocumentosdefaturação-integração.htm` — Configuração de Documentos de Faturação - Integração.
- `configuraçãodedocumentosdefaturação-configuração.htm` — Configuração de Documentos de Faturação - Configuração.
- `configuraçãodedocumentosdefaturação-logística.htm` — Configuração de Documentos de Faturação - Logística.
- `documentosdefaturação-dadosdetesouraria.htm` — Documentos de Faturação - Dados de Tesouraria.
- `introduçãodedocumentos.htm` — Introdução de Documentos.
- `comprasafornecedores.htm` — Compras a Fornecedores (referência contextual, não analisada em profundidade nesta fase).

**Confidencialidade:** a Enciclopédia é material oficial PHC. O ZIP, os seus artigos integrais, imagens e assets não são copiados para este repositório. Este relatório regista apenas títulos, referências de origem e sínteses próprias necessárias à investigação. O ZIP e a captura IKP permanecem fora do Git.

## 3. Identidade estrutural de FT, FI e TD

A captura confirma as seguintes chaves primárias:

| Tabela | Descrição PHC identificada na captura | Primary key |
|---|---|---|
| FT | Documentos de Faturação | `ndoc, fno, ftano` |
| FI | Linhas de Documentos de Faturação | `fistamp` |
| TD | Configuração de Documentos de Faturação | `ndoc` |

Estas chaves são factos da captura. Não permitem, por si só, inferir todas as regras de unicidade funcional, nem substituem a leitura das colunas, tipos, índices e constraints físicos.

## 4. Relações PHC declaradas relevantes

As relações abaixo foram encontradas em REL e/ou DTL. As tabelas de origem e destino foram resolvidas na captura e as expressões estão classificadas como `Exact`.

| Origem | Destino | Expressão declarada | Tipo(s) |
|---|---|---|---|
| FT | CL | `ft.no=cl.no and ft.estab=cl.estab` | REL + DTL |
| FT | FT2 | `ft.ftstamp=ft2.ft2stamp` | REL + DTL |
| FT | FT3 | `ft.ftstamp=ft3.ft3stamp` | REL + DTL |
| FT | TD | `ft.ndoc=td.ndoc` | REL + DTL |
| FI | FT | `fi.ftstamp=ft.ftstamp` | REL + DTL |
| FI | FI2 | `fi.fistamp=fi2.fi2stamp` | REL + DTL |
| FI | BI | `fi.bistamp=bi.bistamp` | REL + DTL |
| FI | SL | `fi.fistamp=sl.fistamp` | REL + DTL |
| FI | TD | `fi.ndoc=td.ndoc` | REL + DTL |
| TD | TD2 | `td.tdstamp=td2.td2stamp` | REL + DTL |

Estas são **relações PHC declaradas**, não foreign keys SQL. A captura indica zero foreign keys físicas na operação recolhida. A relação FI→BI é sustentada pela expressão declarada; não se deve inferir uma relação directa FI→ST apenas por aparecerem tabelas ST em código de outros triggers.

As relações dão uma primeira estrutura funcional:
- FT liga o documento ao cliente/estabelecimento, à sua informação complementar e à configuração do tipo de documento.
- FI liga as linhas ao cabeçalho, aos dados complementares das linhas, a linhas de dossiers internos quando aplicável e aos movimentos de stock.
- TD liga o código interno do documento à configuração que condiciona parte do comportamento dos triggers.

## 5. O que a Enciclopédia documenta

### 5.1 Configuração dos documentos

O artigo **Configuração de Documentos de Faturação** afirma que esta configuração permite criar documentos e definir a forma como se comportam na aplicação. Apresenta exemplos por tipo e resume os efeitos esperados em Stocks, Conta-Corrente e Tesouraria/Previsões.

O artigo **Configuração de Documentos de Faturação - Integração** identifica os tipos:
- 1 — Documento de faturação (ex.: fatura, fatura-recibo).
- 2 — Documento para débitos extra-faturação (ex.: nota de débito).
- 3 — Documento para créditos (ex.: nota de crédito).
- 4 — Documento para trânsito de mercadorias (ex.: guia de remessa).
- 5 — Outros (ex.: fatura proforma).

O mesmo artigo descreve opções de integração configuráveis, entre as quais:
- Lançamento de movimentos em stock e código do movimento de stock;
- Lançamento de movimentos em produtos vendidos;
- Lançamento automático em conta-corrente;
- Regularização automática de movimentos em conta-corrente;
- Regularização de adiantamentos;
- Emissão de recibo ao gravar;
- Código de movimento de conta-corrente;
- Lançamento em tesouraria real, código e local do movimento;
- Permissão para alterar o local de tesouraria no documento.

O artigo **Configuração de Documentos de Faturação - Logística** trata configurações específicas do processo de logística, incluindo morada de entrega e morada por linha.

O artigo **Documentos de Faturação - Dados de Tesouraria** descreve os dados de títulos e cobranças associados ao documento, incluindo local de tesouraria, título único, número do título, banco, valor, data e modo de cobrança.

O artigo **Introdução de Documentos** descreve a tabela de introdução de documentos como a principal tabela do POS Front. É uma referência específica desse módulo, não uma definição geral de FT.

### 5.2 Leitura funcional prudente

A Enciclopédia confirma que o comportamento dos documentos é parametrizável e que as opções de integração são independentes. Assim, não é correcto concluir que qualquer gravação de FT gera sempre movimentos de stock, conta-corrente ou tesouraria.

A configuração descrita na Enciclopédia deve ser cruzada com as condições concretas dos triggers e, quando necessário, com os valores efectivos de TD na base analisada.

## 6. Triggers directamente associados

A captura identifica os seguintes triggers DML activos associados às três tabelas:

| Tabela | Trigger | Evento | Linha no script |
|---|---|---|---:|
| FT | `tr_ft_insert` | INSERT | 10098 |
| FT | `tr_ft_update` | UPDATE | 17747 |
| FT | `tr_ft_delete` | DELETE | 5864 |
| FI | `tr_fi_insert` | INSERT | 20407 |
| FI | `tr_fi_update` | UPDATE | 14086 |
| FI | `tr_fi_delete` | DELETE | 20589 |
| TD | `tr_td_update` | UPDATE | 2350 |
| TD | `tr_td_delete` | DELETE | 6744 |

Na captura, as definições destes triggers estão marcadas `Unavailable`; as linhas indicadas são referências ao script independente.

### 6.1 FT — inserção

**Comportamentos observados no código:**
- Actualiza acumulados de facturação em CL em condições que incluem o tipo de documento e a configuração TD (por exemplo, `regrd=0`).
- Pode criar um título em CH quando o documento está configurado para título único/cheque e não está em multi-pagamento.
- Cria ou actualiza movimentos em CC quando a configuração e as condições do documento o determinam; há também lógica própria de regularização.
- Tem lógica de lançamento em OL condicionada pela configuração de tesouraria.
- Actualiza registos associados a LR/LP, MR e CSUP em condições específicas.

**Interpretação:** FT é o cabeçalho e ponto de coordenação de efeitos comerciais/financeiros do documento. A configuração TD é uma das fontes de decisão. Os efeitos são condicionais, não universais.

**Por validar:** separar integralmente todos os ramos de `tr_ft_insert`, em especial as condições exactas de CC, OL, títulos e regularizações, e cruzá-los com os valores/configurações de TD da CS Default.

### 6.2 FT — alteração

**Comportamentos observados no código:**
- Recalcula/reverte acumulados de CL com base nos valores anteriores e novos.
- Sincroniza ou recria movimentos relacionados com CH, CC e OL conforme os campos alterados e as condições do documento.
- Tem tratamento explícito para anulação: quando `anulado=1`, remove movimentos SL/SV associados às linhas FI e actualiza informação de BI; também altera estados relacionados com documentos associados.
- Contém lógica de sincronização para outras estruturas, incluindo LR/LP e MR.

**Interpretação:** alteração e anulação não são equivalentes a eliminação física. O trigger de UPDATE contém ramos específicos de anulação e sincronização.

**Por validar:** mapear as condições de cada bloco `IF UPDATE(...)` e os efeitos exactos por tabela, evitando resumir todos os updates como uma única operação.

### 6.3 FT — eliminação física

**Comportamentos observados no código:**
- Reverte acumulados de facturação em CL sob condições específicas.
- Elimina movimentos associados em CH, CC, OL e FTCC.
- Reverte valores associados a LR e LP.
- Actualiza estado de MR em determinadas situações.
- Elimina registos FTT ligados ao documento.

**Interpretação:** a eliminação física de FT tem efeitos de limpeza/reversão em tabelas relacionadas. Não deve ser confundida com a anulação lógica tratada em `tr_ft_update`.

### 6.4 FI — inserção

**Comportamentos observados no código:**
- Cria movimentos em SL quando `TD.lancasl=1` e a linha tem referência não vazia.
- O movimento de stock usa dados da linha, do cabeçalho FT e da configuração TD; o sinal da quantidade é invertido para documentos do tipo 3.
- Pode criar movimentos SV para diferenças de custo em determinadas notas de crédito (tipo 3), quando se verificam condições adicionais de configuração e diferença de custos.
- Actualiza FTREG para linhas de documentos de crédito com referência a esse registo.
- Actualiza BI quando a linha está associada a uma linha de dossier interno.
- Tem ainda lógica relativa a PN, FT e outros estados associados.

**Interpretação:** FI é o nível onde os efeitos por artigo/linha são materializados, sobretudo o movimento de stock. A existência de uma linha, por si só, não significa que seja criado SL: a configuração e a referência são condições explícitas.

### 6.5 FI — alteração

**Comportamentos observados no código:**
- Actualiza ou sincroniza SL a partir de campos da linha, do cabeçalho FT e de TD.
- A lógica de SL contempla campos de referência, armazém, lote, quantidade, custos e outros atributos.
- Actualiza ou remove SV em condições específicas.
- Recalcula componentes relacionados com FTREG, BI, PN e custos do documento.

**Interpretação:** a alteração de uma linha pode propagar-se a movimentos de stock e a estruturas de origem/integração. Não é seguro descrever a operação como simples update isolado de FI.

**Por validar:** identificar a matriz completa de campos que desencadeiam cada ramo de sincronização e as condições de remoção/recriação de SL/SV.

### 6.6 FI — eliminação física

**Comportamentos observados no código:**
- Elimina SL e SV associados pelo `fistamp`.
- Actualiza BI e FTREG.
- Actualiza estados em FT e outras tabelas relacionadas com facturação, custos e processos associados, incluindo PN, AGSTC, OPG, FMIN, BE, MR e RVP.

**Interpretação:** a eliminação de uma linha tem efeitos em cadeia. A relação por `fistamp` é uma evidência particularmente importante para a associação FI→SL/SV.

### 6.7 TD — alteração e eliminação

- `tr_td_update`: quando muda `nmdoc`, sincroniza o nome do documento em WKDOCS, WK e POSTD.
- `tr_td_delete`: elimina registos TD2 associados por `tdstamp = td2stamp`.

**Interpretação:** TD não é apenas uma tabela de descrição. É uma configuração consultada pelos triggers de FT/FI e possui informação complementar em TD2.

## 7. Divergência entre o script e o inventário da captura

A comparação dos nomes encontrou:
- 564 triggers no Object Index da captura;
- 552 declarações únicas no script;
- 552 nomes do script também presentes na captura;
- 12 triggers presentes na captura, mas não encontrados no script;
- nenhum trigger exclusivo do script.

Os 12 nomes presentes na captura e ausentes no script são:
`tr_ag_delete`, `tr_gc_update`, `tr_iditra_delete`, `tr_iditra_insert`, `tr_iditra_update`, `tr_oa_update`, `tr_tb_update`, `tr_tda_insert`, `tr_tda_update`, `tr_tfd_delete`, `tr_tfd_insert`, `tr_tfd_update`.

**Não concluir** que estes 12 triggers não existem na base, nem que estejam necessariamente encriptados ou indisponíveis por uma causa específica. A conclusão suportada é apenas que não aparecem no script fornecido. É uma lacuna de fonte a esclarecer.

## 8. Síntese funcional provisória

Com as fontes disponíveis, a leitura provisória é:

1. **TD define a configuração do tipo de documento**, incluindo opções de integração. A Enciclopédia descreve estas opções e os triggers consultam-nas.
2. **FT representa o cabeçalho do documento de faturação** e coordena efeitos ao nível do documento, como acumulados do cliente, títulos, conta-corrente e tesouraria, sempre sujeitos a condições.
3. **FI representa as linhas do documento** e materializa efeitos por artigo, como movimentos de stock, diferenças de custo e actualização de documentos/dossiers associados.
4. **As relações PHC REL/DTL e os triggers complementam-se**: as primeiras declaram ligações entre estruturas; os segundos mostram operações e efeitos concretos.
5. **Anulação lógica e eliminação física são percursos distintos**, pelo menos nos triggers observados.
6. A Enciclopédia descreve possibilidades de configuração; a captura e o script mostram uma instância concreta e o código disponível. Não se deve transformar uma possibilidade documentada numa regra universal.

Estas conclusões são provisórias e limitadas à CS Default e ao script recebido.

## 9. Lacunas e trabalho seguinte

### 9.1 Estrutura física

É necessária uma consulta SQL Server estritamente read-only para FT, FI e TD, usando catálogos `sys`:
- `sys.columns`, `sys.types`: nomes, tipos, nulabilidade, comprimento, precisão, escala e defaults;
- `sys.indexes`, `sys.index_columns`: índices, unicidade e ordem das colunas;
- `sys.foreign_keys`, `sys.foreign_key_columns`: foreign keys físicas efectivamente existentes.

Não executar alterações na base. A consulta deve limitar-se a FT, FI, TD e às relações directamente relevantes, e os resultados devem ser guardados fora do repositório até serem revistos.

### 9.2 Triggers

- Fazer leitura integral e estruturada dos oito triggers listados, por blocos condicionais.
- Construir matriz `evento → condição → tabela afectada → efeito`.
- Distinguir efeitos directos, efeitos indirectos e chamadas a outros mecanismos.
- Analisar triggers auxiliares relevantes (por exemplo, FT2, FI2, FTCC, FTREG e FTRD) apenas quando forem necessários para explicar o fluxo.

### 9.3 Enciclopédia

- Prosseguir a leitura artigo a artigo dos tópicos ligados a faturação, stock, conta-corrente e tesouraria.
- Registar título e nome do ficheiro de origem para cada evidência.
- Não copiar artigos integrais, imagens ou assets para o repositório.
- Distinguir documentação geral, funcionalidades dependentes de módulo/gama e particularidades de versão.

### 9.4 Validação funcional

- Comparar a descrição documental e os triggers com dados/configuração efectivos de TD, sem modificar dados.
- Quando necessário, preparar cenários de teste controlados numa base de testes, nunca executar operações destrutivas na CS Default.
- Só classificar como conclusão validada depois de evidência suficiente e âmbito explícito.

## 10. Regras de governação desta investigação

- A investigação é exploratória e não altera a base de dados, os ficheiros de origem, os triggers, o Scanner, o Portal ou os schemas.
- Não persistir conhecimento funcional na base central nesta fase.
- Não fazer alterações de código ou arquitectura por causa dos achados sem proposta e aprovação explícita.
- Não copiar a Enciclopédia PHC nem dados proprietários de clientes para o repositório público.
- Preservar proveniência e âmbito de cada conclusão.
- Em novos chats, continuar a partir deste relatório e actualizar este mesmo documento quando houver resultados materiais; evitar criar rascunhos dispersos.

## 11. Próximo passo proposto

Continuar pela análise integral dos oito triggers de FT/FI/TD, construindo a matriz de operações e efeitos, e preparar depois a consulta read-only do esquema físico. A análise de artigos adicionais da Enciclopédia será incorporada no mesmo relatório à medida que for concluída.

---

## Referências de origem

- Captura: `cs-default.ikp.zip` (artefacto de trabalho local, não versionado).
- Script: `docs/sources/triggers phc.sql` (fonte já existente no repositório).
- Enciclopédia: `enciclopedia.zip` (artefacto oficial PHC, local, não versionado).
- Ficheiros da captura analisados: `manifest.json`, `coverage.json`, `object-index.json`, `payload/relations/declared-relations.json`.


## 12. Segunda passagem — matriz detalhada dos triggers (25-11-2026)

Esta secção aprofunda a leitura integral dos oito blocos `CREATE TRIGGER` do script versionado. As linhas abaixo são **linhas absolutas do ficheiro** `docs/sources/triggers phc.sql`. Os nomes de tabelas e condições foram lidos no código; a interpretação funcional mantém-se limitada à CS Default/script fornecido.

### 12.1 Matriz de operações observadas

| Evento | Destino | Condição principal observada no código | Efeito |
|---|---|---|---|
| FT INSERT | CL | `tipodoc IN (1,3)`, `TD.regrd=0`, documento não anulado | Soma `ttiliq/ettiliq` a `CL.acmfact/eacmfact`; a identificação do cliente pode vir de FT ou FT2 conforme `TD.lancacli`. |
| FT INSERT | CH | `multi=0` e `cheque=1` | Cria título associado ao documento; valores são zero para `tipodoc=3`. |
| FT INSERT | CC | `TD.lancacc=1` e `multi=0` | Cria movimento de conta-corrente; para `tipodoc=3` o valor é lançado como crédito com sinal negativo nas expressões do trigger. |
| FT INSERT | CC | `TD.lancacc=1`, `TD.ccautoreg=1`, `multi=0` | Actualiza o movimento principal e cria movimento adicional de auto-regularização com stamp iniciado por `R`. |
| FT INSERT | OL | `TD.lancaol=1` | Cria movimento em OL com dados de local, contado, moeda e meios de pagamento provenientes de TD/FT2. |
| FT INSERT | CC | `TD.lancacc=0`, `rdtotal<>0`, `tipodoc=1`, `multi=0` | Cria movimento CC para o valor de regularizações indicado em `rdtotal`. |
| FT INSERT | LR / LP | Stamp de ligação não vazio | Acrescenta valores do documento aos acumulados `edebi/eedebi` da estrutura associada. |
| FT INSERT | MR | `mrstamp<>''` e `idft=1` | Marca `MR.ftpes=1`. |
| FT INSERT | CSUP | FT2 tem `csupstamp` | Actualiza em CSUP a referência ao documento emitido (stamp, nome, número e data). |
| FT UPDATE | CL | Alteração de `ndoc/no/tipodoc/ttiliq/ettiliq/anulado` | Reverte valores anteriores e reaplica os novos acumulados, respeitando `TD.regrd=0` e estado de anulação. |
| FT UPDATE | CH | Alteração de campos de título, cliente, moeda, tipo ou multi-pagamento | Remove/recria ou actualiza CH, conforme a combinação de campos e se o documento continua a ser título único. |
| FT UPDATE | CC | Alteração de campos de cabeçalho/valores e `TD.lancacc=1` | Remove/recria ou actualiza o movimento principal e o movimento de auto-regularização. |
| FT UPDATE | OL | Alteração de dados do documento e `TD.lancaol=1` | Actualiza OL; se ainda não existir, pode inserir o movimento. |
| FT UPDATE | LR / LP | Ligação anterior ou nova não vazia | Subtrai os valores de `deleted` e soma os valores de `inserted`, evitando acrescentar ao acumulado quando o novo documento está anulado. |
| FT UPDATE | SL / SV | Transição de `anulado=0` para `anulado=1` | Elimina movimentos de stock e valorização ligados às linhas FI do documento. |
| FT UPDATE | BI | Transição para anulado | Repõe referências/documento e ajusta quantidades/partes ligadas às linhas, condicionado por FI2/TS. |
| FT UPDATE | FTREG | Transição para anulado, linhas de tipo 3 ligadas a FTREG | Reverte valores acumulados e limpa a referência do último documento. |
| FT UPDATE | PN | Transição para anulado | Elimina os registos de produtos vendidos associados às linhas FI. |
| FT UPDATE | AGSTC / OPG / FMIN / BE / MR / RVP | Transição para anulado | Repõe estados de facturação/processamento e valores associados às origens correspondentes. |
| FT UPDATE | CC / FTRD / RD | Transição para anulado | Anula valores em conta-corrente, remove movimentos automáticos ligados e actualiza estado de integração das regularizações. |
| FT UPDATE | FT3 / FI | Transição para anulado | Limpa sinalização de arquivo digital e referências de documento facturador/origem em determinadas relações. |
| FT DELETE | CL | `tipodoc IN (1,3)`, `TD.lancacli=0`, `TD.regrd=0`, documento não anulado | Subtrai acumulados do cliente. |
| FT DELETE | CH / CC / OL / FTCC / FTT | Eliminação física de FT | Apaga os registos associados pelo `ftstamp`. |
| FT DELETE | LR / LP | Stamp de ligação não vazio | Reverte acumulados do documento eliminado. |
| FT DELETE | MR | `mrstamp<>''` e `idft=1` | Repõe `MR.ftpes=0`. |
| FI INSERT | SL | `TD.lancasl=1` e `ref` não vazia | Cria movimento de stock por linha, com quantidade invertida para `tipodoc=3`; usa configuração de código, custo e descrição de movimento em TD. |
| FI INSERT | SV | `lancasl=1`, `cmsl>50`, `cmsv<>0`, `tipodoc=3`, `qtt<>0`, referência não vazia e custo diferente | Cria movimento de valorização com diferença entre custo e preço/custo ponderado. |
| FI INSERT | FTREG | `tipodoc=3` e `ftregstamp` não vazio | Cria FTREG se ainda não existir e actualiza acumulados associados à regularização de IVA/valores. |
| FI INSERT | BI | `bistamp<>''` | Actualiza data/número/documento de origem, quantidades e partes; quantidade respeita sinal de tipo 3 e flags de FI2/TS. |
| FI INSERT | FT | `ndocft/fnoft` preenchidos e correspondência com documento de origem | Marca documento de origem como facturado e guarda identificação do documento facturador. |
| FI INSERT | PN | `TD.coms=1`, `qtt<>0`, `composto=0` | Cria registo de produto vendido por linha, com valores e sinal dependentes do tipo de documento. |
| FI INSERT | AGSTC / OPG / FMIN / BE / MR | Stamps/refs de origem preenchidos | Marca origens como facturadas/processadas e actualiza valores de venda ou estados de entrega/pesagem. |
| FI INSERT | FT | Linha não composta e documento não anulado | Acrescenta custo total da linha aos acumulados de custo em FT, com sinal invertido para tipo 3. |
| FI UPDATE | PN | Campos comerciais/custo alterados e `TD.coms=1`, `qtt<>0`, `composto=0` | Actualiza ou recria PN; remove PN quando a quantidade passa a zero. |
| FI UPDATE | SL | Campos de artigo, quantidade, custo, lote, armazém ou ligação alterados; `TD.lancasl=1` | Actualiza o movimento existente; elimina SL se a referência fica vazia; cria se não existir e o documento não estiver anulado. |
| FI UPDATE | SV | Mesmas condições específicas de valorização de nota de crédito | Remove SV se deixa de haver referência/diferença de custo; actualiza ou cria SV quando as condições se mantêm. |
| FI UPDATE | FTREG | Alteração de linha ligada a nota de crédito | Reverte valores de `deleted`, cria FTREG quando necessário e soma valores de `inserted`. |
| FI UPDATE | BI | `qtt`, `ftstamp` ou `bistamp` alterados | Retira o contributo anterior e aplica o novo, incluindo quantidade e partes conforme FI2/TS. |
| FI UPDATE | FT | `ndocft/fnoft` preenchidos | Marca documento de origem como facturado quando a correspondência existe e o documento de origem não está anulado. |
| FI UPDATE | FT | `qtt`, `custo` ou `ecusto` alterados | Subtrai o custo antigo e acrescenta o novo ao acumulado de custo do cabeçalho, para linhas não compostas. |
| FI UPDATE | BE / MR | Alteração da linha e referências associadas | Actualiza valor de venda/data em BE e estados de entrega/pesagem em MR. |
| FI DELETE | SL / SV | Eliminação de FI | Apaga movimentos de stock/valorização por `fistamp`. |
| FI DELETE | BI | `bistamp<>''` e TS usa partes | Subtrai partes associadas à linha. |
| FI DELETE | FTREG | Linha tipo 3 ligada a FTREG | Reverte acumulados de IVA/valores e limpa último documento. |
| FI DELETE | FT | Linha ligada a documento de origem e sem outras linhas equivalentes | Repõe `facturada=0` e limpa identificação do documento facturador. |
| FI DELETE | PN | Eliminação de FI | Apaga PN pelo `fistamp`. |
| FI DELETE | FT | Linha não composta | Subtrai custo da linha aos acumulados do cabeçalho. |
| FI DELETE | AGSTC / OPG / FMIN / BE / MR / RVP | Stamps/refs associados | Repõe estados de facturação/processamento/entrega e valores associados. |
| TD UPDATE | WKDOCS / WK / POSTD | `UPDATE(nmdoc)` | Sincroniza o nome do documento nas estruturas de configuração/uso de documentos e POS identificadas no trigger. |
| TD DELETE | TD2 | Eliminação de TD | Apaga TD2 pelo stamp de ligação. |

**Nota:** a matriz sintetiza os ramos encontrados, não constitui uma lista exaustiva de todas as colunas copiadas ou de todas as condições secundárias. Os triggers são multi-row (usam `inserted`/`deleted`); a matriz descreve efeitos lógicos observados, sem afirmar que cada ramo foi validado com execução em runtime.

### 12.2 Condições de assinatura digital em FT

Tanto `tr_ft_insert` como `tr_ft_update` incluem uma validação que provoca `RAISERROR` e `ROLLBACK` quando a assinatura em FT2 está vazia, em documentos que satisfaçam as condições do trigger:
- `TD.assinadoc=1` ou `TD.tipodoc` entre 1 e 4;
- `TD.assinaimp=0`;
- `TD.docsext=0`;
- `TD.docsimport=0`;
- `FT.fno>0`;
- `TD.fechada=0`.

O código constrói uma mensagem a indicar que a assinatura digital do documento está vazia. Esta é uma validação explícita no script; não se deve generalizar para todos os documentos ou instalações sem confirmar a configuração TD e a versão.

### 12.3 Contexto de execução e guardas

- `tr_ft_update` lê `Context_Info()` e termina imediatamente se for `0x55555`. Isto é uma guarda explícita contra reentrada em determinado contexto de execução.
- `tr_fi_update` tem um bloco de sincronização de PN condicionado a `Context_Info() = 0x88888`; há também blocos de PN fora desse bloco, com as suas próprias condições.
- `tr_fi_insert` define `Context_Info 0x55555` antes de actualizar custos acumulados em FT e repõe `0x66666` depois dessa operação. Este detalhe é relevante para compreender por que razão nem todas as alterações a FT originam necessariamente o mesmo percurso de trigger.

A semântica global dos valores de `Context_Info` depende dos restantes triggers e caminhos da aplicação. O que fica confirmado é a presença destas guardas e atribuições no código analisado; não se atribui ainda uma finalidade universal a cada constante.

### 12.4 Enciclopédia — artigos adicionais lidos nesta passagem

Foram também lidos os seguintes artigos do ZIP local:
- `movimentosdestocks.htm` — descreve a opção de movimentos de stocks como registo de entradas e saídas, indicando que a maioria dos movimentos é originada noutros programas, como faturação e compras.
- `constituiçãodatabelademovimentosdestocks.htm` — descreve a tabela de movimentos de stocks como sendo actualizada automaticamente pelos vários programas e identifica campos como referência, data, documento, quantidade, valor, armazém, lote e contabilizado.
- `códigosdemovimentaçãodestocks.htm` — descreve códigos de movimentos de stock, a sua relação com actualização de preço de custo ponderado e códigos de interligação contabilística; refere a convenção de códigos 1–49 para entradas e 50–99 para saídas, com restrições.
- `movimentosdetesouraria.htm` — descreve movimentos reais de tesouraria introduzidos directamente ou provenientes de documentos oficiais, incluindo documentos de faturação, compras, recibos e pagamentos.
- `documentosdefaturação-dadosdetesouraria.htm` — descreve título único, número e banco do título, valor, data, cobrança, cobrador/rota, tipo de título, forma de pagamento e campos de referências de pagamento.
- `códigosdetesouraria.htm` — descreve códigos de classificação de tesouraria, a distinção entre recebimentos e pagamentos e a utilização em movimentos e análises.
- `códigosdeconta-corrente.htm` — descreve códigos de movimentos de conta-corrente, débito/crédito e opções de lançamento/regularização.
- `funcionamentodafaturação.htm` — confirma que o formato e o funcionamento dos documentos de faturação são parametrizáveis individualmente.
- `configuraçãodedocumentosdefaturação.htm` — apresenta exemplos de comportamento por tipo, incluindo que uma fatura pode movimentar stock, que uma guia de remessa pode não movimentar, e que a nota de crédito é configurada para entrada de stock e valor total negativo.
- `configuraçãodedocumentosdefaturação-integração.htm` — enumera opções independentes de integração em stock, produtos vendidos, conta-corrente e tesouraria.

A documentação confirma o enquadramento funcional geral, mas não prova que cada opção esteja activa em cada registo TD da CS Default. Para isso é necessário consultar os valores efectivos da tabela TD e, quando relevante, os dados FT/FT2/FI.

### 12.5 Referências exactas no script

| Trigger | Linhas absolutas |
|---|---:|
| `tr_td_update` | 2350–2366 |
| `tr_ft_delete` | 5864–5887 |
| `tr_td_delete` | 6744–6748 |
| `tr_ft_insert` | 10098–10333 |
| `tr_fi_update` | 14086–14626 |
| `tr_ft_update` | 17747–18311 |
| `tr_fi_insert` | 20407–20585 |
| `tr_fi_delete` | 20589–20649 |

### 12.6 Revisão de precisão da síntese anterior

A primeira passagem dizia que FI INSERT “pode criar SV para diferenças de custo em determinadas notas de crédito”. A leitura integral confirma a formulação, mas acrescenta as condições exactas: `TD.lancasl=1`, `TD.cmsl>50`, `TD.cmsv<>0`, `TD.tipodoc=3`, quantidade não nula, referência não vazia e diferença entre `pcp/ecp` e `custo/ecusto`.

A primeira passagem também resumia a anulação em FT UPDATE como remoção de SL/SV e actualização de BI. A leitura integral mostra que o ramo de anulação tem um alcance mais amplo, incluindo FTREG, PN, AGSTC, OPG, FMIN, BE, MR, RVP, CC, OL, FT3 e referências de facturação entre documentos. Esta lista fica agora registada para evitar que a anulação seja descrita como simples eliminação de stock.

## 13. Consulta read-only proposta para fechar a estrutura física

A captura IKP não contém colunas, tipos e índices. A consulta seguinte é proposta para execução manual na base `CSDefault202603`, por alguém com acesso SQL Server. É apenas leitura dos catálogos do sistema e não consulta dados comerciais nem altera objectos.

```sql
/* Executar com CSDefault202603 seleccionada.
   Apenas leitura de metadados de FT, FI e TD. */

DECLARE @Targets TABLE (TableName sysname PRIMARY KEY);
INSERT INTO @Targets (TableName) VALUES (N'FT'), (N'FI'), (N'TD');

-- 1. Colunas, tipos, nulabilidade, identidade, computed e defaults
SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    c.column_id AS ColumnOrdinal,
    c.name AS ColumnName,
    ty.name AS TypeName,
    c.max_length,
    c.precision,
    c.scale,
    c.is_nullable,
    c.is_identity,
    c.is_computed,
    dc.definition AS DefaultDefinition,
    cc.definition AS ComputedDefinition,
    c.collation_name
FROM sys.tables AS t
JOIN sys.schemas AS s ON s.schema_id = t.schema_id
JOIN @Targets AS x ON x.TableName = t.name
JOIN sys.columns AS c ON c.object_id = t.object_id
JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
LEFT JOIN sys.default_constraints AS dc
    ON dc.object_id = c.default_object_id
LEFT JOIN sys.computed_columns AS cc
    ON cc.object_id = c.object_id AND cc.column_id = c.column_id
ORDER BY t.name, c.column_id;

-- 2. Índices e ordem das colunas
SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    i.name AS IndexName,
    i.type_desc,
    i.is_primary_key,
    i.is_unique,
    i.is_disabled,
    ic.key_ordinal,
    ic.is_included_column,
    c.name AS ColumnName,
    ic.is_descending_key
FROM sys.tables AS t
JOIN sys.schemas AS s ON s.schema_id = t.schema_id
JOIN @Targets AS x ON x.TableName = t.name
JOIN sys.indexes AS i ON i.object_id = t.object_id
JOIN sys.index_columns AS ic
    ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns AS c
    ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.index_id > 0
ORDER BY t.name, i.name, ic.key_ordinal, ic.index_column_id;

-- 3. Foreign keys físicas com origem ou destino em FT/FI/TD
SELECT
    fk.name AS ForeignKeyName,
    OBJECT_SCHEMA_NAME(fkc.parent_object_id) AS ParentSchema,
    OBJECT_NAME(fkc.parent_object_id) AS ParentTable,
    pc.name AS ParentColumn,
    OBJECT_SCHEMA_NAME(fkc.referenced_object_id) AS ReferencedSchema,
    OBJECT_NAME(fkc.referenced_object_id) AS ReferencedTable,
    rc.name AS ReferencedColumn,
    fk.is_disabled,
    fk.is_not_trusted
FROM sys.foreign_keys AS fk
JOIN sys.foreign_key_columns AS fkc
    ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns AS pc
    ON pc.object_id = fkc.parent_object_id
   AND pc.column_id = fkc.parent_column_id
JOIN sys.columns AS rc
    ON rc.object_id = fkc.referenced_object_id
   AND rc.column_id = fkc.referenced_column_id
WHERE OBJECT_NAME(fkc.parent_object_id) IN (N'FT', N'FI', N'TD')
   OR OBJECT_NAME(fkc.referenced_object_id) IN (N'FT', N'FI', N'TD')
ORDER BY ParentTable, fk.name, fkc.constraint_column_id;
```

Esta consulta não substitui a análise das relações PHC REL/DTL: devolve apenas constraints físicas SQL Server. Os resultados devem ser revistos antes de serem incorporados no relatório. Não executar qualquer operação de escrita na CS Default.

## 14. Próxima etapa

1. Obter e rever os resultados da consulta de metadados para FT/FI/TD.
2. Consultar, em modo read-only, os registos de configuração TD relevantes para as flags já identificadas (`tipodoc`, `lancasl`, `cmsl`, `cmsv`, `lancacc`, `ccautoreg`, `lancaol`, `regrd`, `lancacli`, `coms`, entre outras), sem expor dados de clientes.
3. Com esses valores, construir a matriz de tipos de documento e integrações efectivamente configuradas na CS Default.
4. Só depois aprofundar os triggers auxiliares (`FTCC`, `FTRD`, `FI2`, etc.) que forem necessários para explicar regularizações, multi-pagamento e ligações a dossiers.

A investigação continua exploratória e não autoriza alterações à base de dados, Scanner, Portal, schemas ou persistência central.


## 15. Terceira passagem — tesouraria, armazém e regularizações (25-11-2026)

Esta passagem alarga a análise aos triggers auxiliares que participam nos fluxos de faturação e recibos. As referências de linha são do script docs/sources/triggers phc.sql, com 21 713 linhas. A pesquisa textual foi feita sobre o ficheiro integral, não apenas sobre os oito triggers directamente associados a FT/FI/TD.

### 15.1 Origem dos dados de tesouraria: TD ou FT2

Nos blocos de tr_ft_insert e tr_ft_update, o código escolhe os valores de tesouraria conforme TD.altollocal:

| Campo de TD | Campo de FT2 quando altollocal=1 | Campo de OL/CH | Leitura suportada |
|---|---|---|---|
| vdollocal | vdollocal | OL.ollocal / CH.ollocal | Local de tesouraria |
| vdcontado | vdcontado | OL.contado / CH.contado | Campo contado |
| vdlocal | vdlocal | OL.local | Campo local do movimento |
| olmoeda | olmoeda | OL.moeda | Moeda do movimento |

Com TD.altollocal=1, os valores são lidos de FT2; caso contrário, são lidos de TD. A escolha é explícita nas atribuições de OL (linhas 18018–18021) e na criação de OL (linhas 10285–10288). O trigger de FT também usa a selecção para preencher dados do título CH (linhas 10154–10155 e 17832–17833).

**Condição adicional:** os ramos que criam OL em FT estão condicionados a TD.lancaol=1 (por exemplo, linhas 10297–10299). A presença de dados de local/moeda não significa, por si só, que seja criado um movimento OL.

O trigger tr_cc_insert contém ainda uma lógica de fallback para recibos: quando FT2.vdollocal está vazio ou TD.altollocal=0, usa valores de contexto do recibo (@m_banchab, @m_contado) e determina o campo local entre 'B' e 'C'; quando usa os dados FT2, passa também FT2.olmoeda (linhas 2940–2944). Esta lógica é específica do caminho de recibos e não deve ser confundida com a regra simples de selecção em FT.

A Enciclopédia, em documentosdefaturação-dadosdetesouraria.htm, confirma que o ecrã de dados de tesouraria apresenta o local do movimento quando o tipo de documento permite alterá-lo; por defeito apresenta o local definido na configuração, podendo o utilizador escolher outro. O artigo também distingue título único, número, banco, valor, data, cobrança, tipo de título e forma de pagamento.

### 15.2 TD.regrd não é TSRD.regrd

A pesquisa identificou o mesmo nome de campo em tabelas diferentes. A interpretação deve manter o alias/tabela explícito:

- **TD.regrd:** nos triggers de FT, condiciona a actualização dos acumulados de facturação em CL. O código filtra TD.regrd=0 nos ramos de inserção, alteração e eliminação (por exemplo, tr_ft_insert, linhas 10132–10137; tr_ft_update, linhas 17793–17800; tr_ft_delete, linhas 5867–5872). A evidência disponível não permite atribuir-lhe uma designação funcional mais ampla.
- **TSRD.regrd:** nos triggers de RD, controla a distribuição do valor entre crédito e débito nos movimentos CC e entre entrada e saída nos movimentos OL. Em tr_rd_insert, linhas 11606–11614 e 11657–11662, os valores são lançados em lados diferentes conforme o valor do campo. O mesmo trigger só cria CH no ramo TSRD.regrd=0 (linhas 11668–11682). tr_rd_update mantém esta distinção (a partir da linha 15578).

Assim, a observação anterior sobre regrd fica restringida: o comportamento descrito para recibos pertence a TSRD.regrd, não deve ser atribuído automaticamente a TD.regrd.

### 15.3 Armazém: origem efectiva no movimento de stock

Em tr_fi_insert, quando TD.lancasl=1 e a linha tem referência não vazia, o trigger cria SL e transfere directamente FI.armazem para SL.armazem (linhas 20411–20445). O mesmo campo da linha é usado na criação/actualização de SV para notas de crédito com diferença de custo (linhas 20446–20463; actualização em tr_fi_update, linhas 14210–14252).

A Enciclopédia, em constituiçãodatabelademovimentosdestocks.htm, descreve o armazém como parte dos dados de cada movimento de stock.

**Limite da conclusão:** não foi encontrada a expressão TD.armazem no script integral, nem refarm. Portanto, o código analisado confirma a transferência de FI.armazem para os movimentos; não confirma como a aplicação preenche previamente o armazém das linhas a partir da configuração TD, nem qual o papel funcional de TD.refarm.

### 15.4 Campos sem ocorrência no script

A pesquisa literal ao script integral não encontrou ocorrências de:
- tipomov;
- refarm;
- TD.armazem / td.armazem.

Isto é apenas uma ausência no script fornecido. Não prova ausência de utilização na aplicação, em Xbase, procedimentos, outros scripts ou mecanismos fora do âmbito desta fonte.

### 15.5 Referências do código e documentação consultada

| Evidência | Referência |
|---|---|
| Selecção TD/FT2 para local, contado e moeda na criação de OL | tr_ft_insert, linhas 10281–10299 |
| Actualização de OL com os campos de TD/FT2 | tr_ft_update, linhas 18014–18025 |
| Dados de CH condicionados por lancaol e altollocal | tr_ft_insert, linhas 10152–10157; tr_ft_update, linhas 17830–17834 |
| Fallback de local/contado/moeda no fluxo de recibos | tr_cc_insert, linhas 2940–2944 |
| TD.regrd nos acumulados CL | tr_ft_insert 10132–10137; tr_ft_update 17793–17800; tr_ft_delete 5867–5872 |
| TSRD.regrd nos movimentos de recibo | tr_rd_insert, linhas 11606–11614, 11637–11643, 11657–11682; tr_rd_update, a partir da linha 15578 |
| FI.armazem transferido para SL/SV | tr_fi_insert, linhas 20411–20445 e 20446–20463; tr_fi_update, linhas 14210–14252 |
| Local de tesouraria configurado e alterável no documento | Enciclopédia: documentosdefaturação-dadosdetesouraria.htm |
| Conteúdo dos movimentos de stock, incluindo armazém | Enciclopédia: constituiçãodatabelademovimentosdestocks.htm |

Os artigos da Enciclopédia são referenciados apenas pelo título/nome de ficheiro e por síntese própria. O ZIP oficial não foi adicionado ao repositório.

## 16. Matriz da configuração TD recebida — primeira leitura

A tabela seguinte regista os valores comunicados pelo utilizador para os 11 tipos de documento da CSDefault202603. É uma fotografia da configuração desta base, não uma definição universal do PHC.

| ndoc | Documento | tipodoc | coms | lancacc | lancasl | lancaol | regrd | lancacli | ccautoreg |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | Fatura | 1 | 1 | 1 | 1 | 0 | 0 | 0 | 0 |
| 2 | Guia de Remessa | 4 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 3 | Fatura Recibo | 1 | 1 | 0 | 1 | 1 | 0 | 0 | 0 |
| 4 | Nota de Crédito | 3 | 1 | 1 | 1 | 0 | 0 | 0 | 0 |
| 7 | Fatura Proforma | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 8 | Fatura Simplificada | 1 | 1 | 0 | 1 | 1 | 0 | 0 | 0 |
| 9 | Orçamento | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 10 | Nota de Débito | 2 | 0 | 1 | 0 | 0 | 0 | 0 | 0 |
| 11 | N/C Adiantamento | 3 | 0 | 0 | 0 | 0 | 1 | 0 | 0 |
| 12 | Consulta de Mesa | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

### 16.1 Leitura limitada às flags observadas

- **Stock:** Fatura, Fatura Recibo, Nota de Crédito e Fatura Simplificada têm lancasl=1. A criação de SL continua dependente de cada FI ter referência não vazia. A criação de SV aplica-se apenas às condições específicas de nota de crédito descritas em 6.4 e 15.3.
- **Produtos vendidos:** os mesmos quatro documentos têm coms=1. O trigger de FI cria/actualiza PN apenas para linhas com quantidade não nula e não compostas (tr_fi_insert, linhas 20550–20552). A Enciclopédia produtosvendidos.htm explica que o lançamento pela faturação é opcional, mas necessário para o funcionamento correcto de vendedores e comissões.
- **Tesouraria real:** Fatura Recibo e Fatura Simplificada têm lancaol=1; os restantes registos apresentados têm zero. A criação de OL no trigger FT está condicionada por esta flag. A existência de títulos CH é um ramo distinto e depende de multi=0 e cheque=1, não devendo ser confundida com a criação de OL.
- **Conta-Corrente:** Fatura, Nota de Crédito e Nota de Débito têm lancacc=1. Fatura Recibo e Fatura Simplificada têm lancacc=0, mas o trigger mantém o ramo especial de CC para tipodoc=1 quando rdtotal<>0 e multi=0 (linhas 10300 em diante). Logo, o valor zero não permite concluir ausência absoluta de movimentos CC em todos os cenários.
- **Auto-regularização:** todos os registos apresentados têm ccautoreg=0. Não há, nesta configuração recebida, documentos com auto-regularização automática activa através desta flag.
- **Segundo cliente:** todos os registos apresentados têm lancacli=0; a configuração recebida não activa a utilização do segundo cliente nestes tipos.
- **regrd:** todos os registos apresentados têm zero excepto N/C Adiantamento (ndoc 11), com valor 1. A consequência específica de TD.regrd=1 neste tipo de documento ainda não está funcionalmente explicada; não inferir o comportamento de TSRD.regrd.

### 16.2 Limites e validações pendentes

Esta matriz usa apenas as colunas recebidas e as flags já cruzadas com triggers. Ainda não avalia:
- códigos e descrições de movimento (cmcc, cmccn, cmccautoreg, cmccautoregn, cmmcc, cmmccn, cmsl, cmsv, cmsvn, olcodigo);
- valores de local, moeda e contado (vdollocal, vdcontado, vdlocal, olmoeda, altollocal);
- configuração de armazém e referências (armazem, refarm) fora dos triggers;
- a semântica de tipomov;
- parâmetros de assinatura, fecho, rascunho, autofaturação e validações;
- diferenças entre o comportamento do trigger e o comportamento executado pela aplicação antes/depois da gravação.

Os valores desta secção foram fornecidos pelo utilizador e não foram consultados directamente na base de dados pelo assistente. A matriz é, por isso, identificada como configuração comunicada, ainda sem validação independente por consulta read-only.

### 16.3 Próximo passo

Continuar a matriz por documento, cruzando os códigos de movimento com os catálogos de códigos de stock, Conta-Corrente e tesouraria e com os triggers auxiliares relevantes. Depois, rever os campos ainda sem interpretação e preparar uma consulta read-only específica para confirmar a configuração actual na base, se o utilizador a puder executar.


## 17. Códigos de integração configurados — segunda leitura da matriz

Esta secção cruza os valores de códigos comunicados para TD com a descrição dos campos na Enciclopédia e com a utilização encontrada nos triggers. Os números e designações abaixo são os valores da configuração recebida da CSDefault202603; não foram consultados directamente à base nesta sessão.

### 17.1 Conta-Corrente

| Documento | cmcc | cmccn | cmccautoreg | cmmcc |
|---|---:|---|---:|---:|
| Fatura | 1 | N/Factura | 0 | 0 |
| Guia de Remessa | 0 | vazio | 0 | 0 |
| Fatura Recibo | 1 | N/Factura | 0 | 0 |
| Nota de Crédito | 57 | N/Nt. Crédito | 0 | 0 |
| Fatura Proforma | 0 | vazio | 0 | 0 |
| Fatura Simplificada | 1 | N/Factura | 0 | 0 |
| Orçamento | 9 | vazio | 0 | 0 |
| Nota de Débito | 2 | N/Nt. Débito | 0 | 0 |
| N/C Adiantamento | 0 | vazio | 0 | 0 |
| Consulta de Mesa | 0 | vazio | 0 | 0 |

**Nota de leitura:** a coluna cmmcc é o código de movimento de C/C de multi-pagamento; a descrição da Enciclopédia confirma essa finalidade. Todos os registos recebidos têm cmmcc=0. Os valores 9 (Orçamento) e 2 (Nota de Débito) pertencem a cmcc, não a cmmcc.

Os códigos cmcc e respectivas descrições são passados pelo trigger para movimentos CC em ramos específicos. A Enciclopédia explica que os códigos de Conta-Corrente classificam movimentos de débito/crédito e que a disponibilidade para regularizações é uma propriedade do próprio código. Não recebemos, contudo, o registo integral do catálogo de códigos CC; por isso não se conclui, apenas pelo número, a natureza débito/crédito de cada código.

### 17.2 Stock

| Documento | lancasl | cmsl | cmsv | cmsvn |
|---|---:|---:|---:|---|
| Fatura | 1 | 75 | 0 | vazio |
| Guia de Remessa | 0 | 0 | 0 | vazio |
| Fatura Recibo | 1 | 76 | 0 | vazio |
| Nota de Crédito | 1 | 77 | 4 | Entrada p/ Devolução |
| Fatura Proforma | 0 | 0 | 0 | vazio |
| Fatura Simplificada | 1 | 76 | 0 | vazio |
| Orçamento | 0 | 0 | 0 | vazio |
| Nota de Débito | 0 | 78 | 0 | vazio |
| N/C Adiantamento | 0 | 0 | 0 | vazio |
| Consulta de Mesa | 0 | 0 | 0 | vazio |

A Enciclopédia descreve cmsl como o código de movimento de stock e cmsv como o código de movimento em valor de stocks para notas de crédito. O trigger de FI usa cmsl/cmsln para criar SL e, no ramo especial de SV, usa cmsv/cmsvn (tr_fi_insert, linhas 20437 e 20448–20463).

A Enciclopédia dos Códigos de Movimentação de Stocks indica a convenção numérica 1–49 para entradas e 50–99 para saídas, com a ressalva de que o código 50 não é permitido. Os códigos cmsl recebidos (75, 76, 77 e 78) pertencem, portanto, à gama documental de códigos de saída. **Não se deve deduzir o sentido líquido do movimento apenas deste intervalo**, porque o trigger também ajusta o sinal da quantidade em documentos do tipo 3 e a semântica final depende do código configurado e da operação. O registo do catálogo desses códigos não foi fornecido.

No caso da Nota de Crédito, cmsv=4 e cmsvn="Entrada p/ Devolução" são os valores recebidos para o movimento em valor SV. Não confundir este código com cmsl=77, que é o código usado no movimento SL.

### 17.3 Tesouraria

O valor recebido para olcodigo é "Caixa" nos documentos 1, 3, 4, 7, 8, 9, 10, 11 e 12; no documento 2 (Guia de Remessa) o campo foi fornecido vazio. O trigger usa TD.olcodigo para criar OL e associa o código ao grupo/subgrupo da tabela OC (tr_ft_insert, linhas 10281 e 10297–10299).

A Enciclopédia descreve os códigos de tesouraria como classificações configuráveis pelo utilizador, com grupo, subgrupo e tipo (Recebimentos/Pagamentos). O texto recebido não inclui o registo da tabela OC para o código "Caixa"; por isso não se afirma se, nesta base, está classificado como recebimento, pagamento, exploração ou extraexploração.

### 17.4 Campos que continuam sem interpretação

- **tipomov:** foram recebidos valores diferentes entre documentos, mas não há ocorrência literal no script de triggers. A função permanece por apurar na aplicação ou em documentação específica.
- **refarm:** o valor R10001 aparece em vários registos, mas não foi encontrado no script. Não atribuir significado sem consultar a definição do campo ou o código aplicacional.
- **armazem:** o valor recebido não explica, por si só, como é preenchido FI.armazem; os triggers apenas confirmam a transferência do valor de FI para SL/SV.
- **olcodigo:** o texto "Caixa" identifica o código configurado, mas a classificação efectiva requer o registo OC correspondente.
- **cmcc/cmmcc:** os códigos foram recebidos, mas falta o catálogo CC para confirmar propriedades de débito/crédito e disponibilidade para regularização.

### 17.5 Próximo passo

Para fechar a matriz sem inferências, falta obter apenas os registos de catálogo referenciados pelos códigos presentes nesta configuração:
1. Códigos de movimentos de stock: 75, 76, 77, 78 e 4, incluindo descrição, entrada/saída, actualização de preço de custo ponderado e último preço de custo.
2. Códigos de Conta-Corrente: 1, 2, 9 e 57, incluindo descrição, débito/crédito e disponibilidade para regularizações.
3. Código de tesouraria "Caixa": grupo, subgrupo e tipo Recebimento/Pagamento.
4. Definição física e/ou documentação aplicacional dos campos tipomov e refarm.

Até esses registos serem consultados, a matriz apresenta códigos configurados e evidência de utilização no trigger, mas não uma explicação completa da semântica de cada código.


## 18. Identificação dos catálogos físicos e validação do código de tesouraria

Esta passagem corrige a identificação inicial dos catálogos, com base nos metadados físicos e nos registos devolvidos pelo utilizador a partir da CSDefault202603.

### 18.1 Catálogos de movimentos

A estrutura física confirma a separação seguinte:

- **CM1 — movimentos de Conta-Corrente:** contém `cm` (código), `cmdesc` (descrição), `debito`, `regulariza`, associações de tesouraria e várias propriedades de integração. Os registos consultados para os códigos 1, 2, 4 e 57 foram devolvidos por esta tabela.
- **CM2 — movimentos de stock:** contém `cm` (código), `cmdesc` (descrição), `entrada`, `mudapcpond`, `mudapcult` e `obrigacpsl`. A estrutura identifica esta tabela como o catálogo a consultar para os códigos de stock configurados em TD.
- **OC — códigos de tesouraria:** contém `olcodigo`, `olgrupo`, `olsgrupo` e `oltipo`.

A tabela `CM` não é o catálogo de códigos procurado nesta análise: a estrutura apresentada contém `cmstamp`, `ano`, `valor` e campos de auditoria.

### 18.2 Registos CM1 efectivamente devolvidos

A consulta `dbo.CM1 WHERE cm IN (1, 2, 4, 9, 57, 75, 76, 77, 78)` devolveu quatro registos:

| cm | cmdesc | olcl | clgrupo | clsgrupo | olfl | flgrupo | flsgrupo | debito | fosl |
|---:|---|---|---|---|---|---|---|---:|---:|
| 1 | N/Factura | R10001 | Actividades Operacionais | Recebimentos de Clientes | vazio | vazio | vazio | 1 | 0 |
| 2 | N/Nt. Débito | R10001 | Actividades Operacionais | Recebimentos de Clientes | vazio | vazio | vazio | 1 | 0 |
| 4 | V/Av.Lançamen | vazio | vazio | vazio | P10004 | Actividades Operacionais | Outros pagamentos | 0 | 90 |
| 57 | N/Nt. Crédito | R10001 | Actividades Operacionais | Recebimentos de Clientes | vazio | vazio | vazio | 0 | 0 |

Os quatro registos devolvidos apresentam `inactivo=0`. O código 4 tem `fosl=90`; este valor é registado como observado, sem lhe atribuir significado funcional adicional nesta fase.

A consulta não devolveu registos CM1 para 9, 75, 76, 77 ou 78. Não se conclui que esses códigos não existam noutros catálogos: os códigos de stock devem ser procurados em CM2.

### 18.3 Código de tesouraria utilizado: R10001

A configuração TD recebida apresenta `olcodigo=R10001` e `vdollocal=Caixa`. A consulta a OC não devolveu qualquer registo para `olcodigo='Caixa'`, mas a listagem integral de OC fornecida pelo utilizador contém:

| olcodigo | olgrupo | olsgrupo | oltipo |
|---|---|---|---:|
| R10001 | Actividades Operacionais | Recebimentos de Clientes | 1 |

Fica assim esclarecida a distinção entre os campos: **R10001 é o código de tesouraria; Caixa é o valor de vdollocal**, isto é, o local indicado na configuração recebida. Não devem ser tratados como o mesmo identificador.

A classificação de R10001 como recebimento de clientes é sustentada pelo registo OC apresentado. O significado interno de cada valor possível de `oltipo` não é generalizado para além deste registo.

### 18.4 Próxima consulta necessária

Para fechar a leitura dos movimentos de stock, falta consultar em CM2 os códigos que aparecem na configuração TD:

- `TD.cmsl`: 75, 76, 77 e 78;
- `TD.cmsv`: 4.

A consulta deve usar `dbo.CM2`, não `CM1` nem `CM`. Os campos relevantes incluem `cm`, `cmdesc`, `entrada`, `mudapcpond`, `mudapcult` e `obrigacpsl`.

### 18.5 Proveniência e limites

As estruturas e linhas de dados desta secção foram fornecidas pelo utilizador a partir da base CSDefault202603. Não foram consultadas directamente pelo assistente. A identificação de CM1, CM2 e OC resulta dos nomes e tipos de colunas apresentados; as interpretações de códigos ficam limitadas aos valores efectivamente devolvidos.


### 18.6 Resultado dos códigos de movimento de stock em CM2

O utilizador forneceu os seguintes registos de `dbo.CM2`, filtrados pelos códigos 4, 75, 76, 77 e 78:

| cm | cmdesc | entrada | mudapcpond | mudapcult | obrigacpsl |
|---:|---|---:|---:|---:|---:|
| 4 | Entrada p/ Devolução | 1 | 1 | 1 | 0 |
| 75 | N/Factura | 0 | 0 | 0 | 0 |
| 76 | N/Fatura Recibo | 0 | 0 | 0 | 0 |
| 77 | N/Nt.Crédito | 0 | 0 | 0 | 0 |
| 78 | N/Nt.Débito | 0 | 0 | 0 | 0 |

Leitura limitada aos dados observados:

- Os códigos 75, 76, 77 e 78 estão classificados com `entrada=0`; no catálogo desta instalação, são movimentos de saída.
- O código 4, `Entrada p/ Devolução`, está classificado com `entrada=1` e tem `mudapcpond=1` e `mudapcult=1`. Assim, a configuração indica que este movimento altera tanto o preço de custo ponderado como o último custo.
- Todos os cinco registos têm `obrigacpsl=0`; não se deve inferir uma obrigação de criar um movimento SL a partir desta propriedade.
- A estrutura de CM2 não contém a coluna `inactivo`; não usar esse campo em consultas a esta tabela.

Cruzamento com a configuração TD anteriormente fornecida:

| Documento | tipodoc | lancasl | cmsl | cmsv |
|---|---:|---:|---:|---:|
| Fatura | 1 | 1 | 75 | 0 |
| Guia de Remessa | 4 | 0 | 0 | 0 |
| Fatura Recibo | 1 | 1 | 76 | 0 |
| Nota de Crédito | 3 | 1 | 77 | 4 |
| Fatura Proforma | 5 | 0 | 0 | 0 |
| Fatura Simplificada | 1 | 1 | 76 | 0 |
| Orçamento | 5 | 0 | 1 | 0 |
| Nota de Débito | 2 | 0 | 78 | 0 |
| N/C Adiantamento | 3 | 0 | 0 | 0 |
| Consulta de Mesa | 5 | 0 | 0 | 0 |

A combinação de `lancasl=1` com os códigos 75, 76 e 77 é coerente com os movimentos de saída configurados para Fatura, Fatura-Recibo, Fatura Simplificada e Nota de Crédito nesta base. A Nota de Crédito também tem `cmsv=4`; o trigger `tr_fi_insert` condiciona a criação de SV à configuração `lancasl=1`, `cmsl>50`, `cmsv<>0`, tipo de documento 3 e outras condições por linha (quantidade, referência e diferença de custo). O código 4 de CM2 é, portanto, o código associado ao movimento de entrada para devolução em valor, mas a confirmação do seu efeito em cada linha depende das condições do trigger.

A Nota de Débito apresenta `cmsl=78`, mas `lancasl=0`; por isso, o valor configurado não basta para concluir que o documento lança movimentos de stock. O mesmo cuidado aplica-se ao Orçamento, que apresenta `cmsl=1` com `lancasl=0`.

**Conclusão provisória:** os dados confirmam a distinção entre o código de movimento físico configurado em `cmsl` e o código de movimento em valor configurado em `cmsv`, bem como a necessidade de considerar o indicador `lancasl` e as condições adicionais dos triggers. A análise descreve exclusivamente a configuração e o comportamento observados na CSDefault202603.
