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
