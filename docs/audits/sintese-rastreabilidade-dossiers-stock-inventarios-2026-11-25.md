# Síntese e rastreabilidade — dossiers, stock e inventários PHC

**Data:** 25-11-2026  
**Estado:** síntese documental da evidência consolidada; investigação técnica permanece aberta.  
**Âmbito:** dossiers internos, movimentos de stock, reservas, stock cativo/pré-cativo e inventários físicos.  
**Repositório:** `Jhouinho/PhcKnowledge`.

## 1. Finalidade

Este documento funciona como ponto de entrada para a investigação funcional consolidada em:

1. [Dossiers internos, stock cativo e pré-cativo](investigacao-funcional-dossiers-internos-stock-cativo-precativo-2026-11-25.md).
2. [Stock e inventários físicos](investigacao-funcional-stock-inventarios-2026-11-25.md).
3. [Grafos PHC — modelo funcional e relações estruturais observadas](grafos-phc-funcional-e-estrutural-2026-11-25.md).

Não substitui esses relatórios nem cria uma terceira descrição integral dos mesmos temas. Resume o modelo conjunto, identifica as ligações entre operações e dados, regista a proveniência das conclusões e organiza o trabalho que falta.

## 2.1 Grafos de leitura

O documento [Grafos PHC — modelo funcional e relações estruturais observadas](grafos-phc-funcional-e-estrutural-2026-11-25.md) apresenta duas vistas complementares: o grafo funcional dos processos e o grafo estrutural mínimo dos predicados de ligação efectivamente observados. O segundo não é um ERD físico completo: as foreign keys, chaves e relações ainda não verificadas permanecem explicitamente por validar.

## 2. Modelo funcional conjunto

A leitura conjunta das fontes sustenta quatro áreas relacionadas, mas que não devem ser confundidas:

| Área | Pergunta a que responde | Elementos principais |
|---|---|---|
| Documentos e linhas | Que operação/documento originou a necessidade ou o movimento? | BO, BO2, BI e documentos de faturação/compra |
| Movimentos físicos | Que entradas e saídas alteraram o stock e quando? | SL, CM2 e origem documental |
| Estado e reservas | Quanto stock existe/acumula por artigo/armazém e quanto está reservado/cativado? | ST, SA e flags/quantidades nas linhas BI |
| Contagem e regularização | O que foi contado e que acertos foram lançados? | STIC, STIL e movimentos SL associados |

**Regra central:** ST/SA representam informação de estado/acumulados; SL representa o histórico de movimentos; STIC/STIL representam o processo de inventário físico; as reservas e cativações são compromissos/afectações de quantidades e não devem ser confundidas com saídas físicas.

O saldo actual não substitui o histórico. Para reconstruir stock numa data passada, a evidência disponível aponta para a análise dos movimentos SL, com sinal interpretado através da configuração CM2 e com validação da origem e da data de cada movimento.

## 3. Mapa de rastreabilidade das operações

A tabela seguinte é um mapa de leitura e não uma especificação exaustiva de todos os ramos do PHC.

| Operação | Origem funcional | Dados/mecanismos observados | Efeito conhecido | Estado da evidência |
|---|---|---|---|---|
| Criar/alterar dossier interno | Cabeçalho e linhas do dossier | BO, BO2, BI; triggers BO/BI | Pode aplicar validações condicionais e efeitos de reservas/stock, conforme configuração | Código SQL fornecido e documentação PHC; condições detalhadas nos relatórios |
| Inserir linha com movimento de stock | Linha BI de dossier configurado para movimentar stock | BI, TS/TS2, CM2, SL | Pode gerar movimento SL; não acontece incondicionalmente em todas as linhas | Trigger observado; depende de configuração e estado |
| Reservar/cativar quantidade | Linha BI com flags de reserva/cativo | BI.qtt, BI.qtt2, flags; ST/SA | Actualiza acumulados de reservas/cativo, usando em vários ramos qtt - qtt2 | Trigger observado e comportamento funcional descrito na Enciclopédia |
| Copiar/passagem entre dossiers | Linha de dossier de origem para dossier de destino | BI.obistamp, BI.qtt2 e opções de cópia | Pode actualizar Qtt2 da origem, preservar ligação e afectar reservas/quantidades | Enciclopédia e trigger; comportamento varia com opções/configuração |
| Anular dossier | Alteração do estado de BO2 | BO2.anulado e trigger de BO2 | Reverte determinados acumulados, elimina SL associado e limpa estruturas auxiliares | Ramo de trigger observado; não equivale a prova de eliminação física |
| Pré-cativar encomenda de cliente | Relação entre encomenda de cliente e encomenda a fornecedor | Enciclopédia; BIPRECAT referenciada no script | Associa procura futura a quantidade ainda não recebida | Conceito e processo funcional documentados; mecanismo técnico de escrita não identificado |
| Receber mercadoria pré-cativada | Entrada por compra/dossier/guia de recepção | Aplicação/Monitor; BIPRECAT por esclarecer | Pode converter pré-cativo em cativo, automática ou manualmente | Comportamento descrito pela Enciclopédia; mecanismo de implementação por identificar |
| Lançar inventário físico | Contagem por artigo/armazém | STIC, STIL | Regista o inventário e pode gerar movimentos de acerto SL | Modelo funcional consolidado; algoritmo interno não reconstruído integralmente |
| Reconstruir stock histórico | Movimentos até à data de corte | SL, CM2 e cadeia documental | Permite calcular saldo teórico, se histórico e configuração forem completos | Validado num inventário concreto da Digicanola; não universal |
| Valorizar stock | Movimentos/elementos de valorização | SV e outros dados ainda por identificar | Efeito e circuito de valorização ainda não fechados | Parcial |

### 3.1 Ligações importantes e limites

- `BI.bostamp` liga a linha BI ao cabeçalho BO.
- `BO.bostamp = BO2.bo2stamp` foi observado no código consultado.
- `SL.bistamp = BI.bistamp` é uma ligação usada no script para movimentos associados a linhas BI.
- `BI.obistamp` é usado para relacionar uma linha de destino com uma linha de origem. Não deve ser classificado como relação REL/DTL sem validar a declaração.
- `SL.sticstamp` associa movimentos ao cabeçalho do inventário STIC.
- Não foi encontrado `SL.stilstamp`; não existe evidência de uma relação directa obrigatória 1:1 entre STIL e SL.
- A origem documental de movimentos pode passar por dossiers internos, faturação ou compras. A estrutura concreta deve ser seguida na base e versão em análise; não presumir uma cadeia FI/FN universal.
- `CM2.entrada` foi determinante na reconstrução Digicanola. A descrição do movimento ou o nome do documento não substituem a configuração efectiva do código de movimento.

## 4. Regras de interpretação que ficam consolidadas

1. **Separar estado e histórico:** ST/SA não bastam para reconstituir stock passado; usar SL e validar completude e classificação.
2. **Não inferir o sinal pelo texto:** interpretar o movimento com a configuração CM2 aplicável.
3. **Não confundir reserva com saída:** reservas/cativações alteram disponibilidade e acumulados, mas não são automaticamente movimentos físicos.
4. **Não assumir um movimento por linha de inventário:** STIL e SL não têm correspondência 1:1 demonstrada.
5. **Não interpretar lançamento como prova de acerto:** STIC.lanca = 1 não garante que todas as linhas tenham originado SL.
6. **Não generalizar casos de uma base:** blivo2026, Digicanola e dfblock são conjuntos distintos; resultados e contagens devem permanecer separados.
7. **Não universalizar código/configuração:** os triggers fornecidos, parâmetros e registos de assistência têm âmbito limitado à fonte/instalação/versão observada.
8. **Não confundir anulação lógica com eliminação física:** o ramo observado em BO2 reverte efeitos e limpa estruturas, mas não prova a eliminação física de BO/BI.
9. **Não assumir que a capability de recolha equivale a cobertura real:** a declaração de uma capability Scanner não prova que todos os tipos de objectos ou definições estejam efectivamente presentes na captura.
10. **Não alterar Scanner, Portal ou dados PHC para colmatar lacunas antes de identificar o mecanismo real.**

## 5. Matriz de evidências e proveniência

| Tema/conclusão | Fonte principal | Fonte complementar | Limite |
|---|---|---|---|
| Estrutura BO/BO2/BI e ligações observadas | Script de triggers PHC fornecido | Configuração/captura CSDefault202603 | Não equivale a validação em todas as versões |
| Validações condicionais e efeitos de triggers | Definições SQL dos triggers identificados nos relatórios | Configuração TS/TS2 | Só os ramos presentes no script fornecido |
| Opções de cópia de dossiers | Enciclopédia PHC, artigo “Opções na Cópia de Dossiers” | Trigger BI e campos Qtt2/obistamp | Variantes Web/desktop podem diferir |
| Funcionamento cativo/pré-cativo | Enciclopédia PHC: funcionamento, Monitor e coluna informativa | Triggers BI/BO2 e referências BIPRECAT | Criação e conversão técnica de pré-cativo não identificadas |
| Modelo ST/SA/SL/CM2/STIC/STIL | Esquemas e consultas nas bases analisadas | Reconciliação do inventário Digicanola | Resultados específicos das bases consultadas |
| Inventários blivo2026 | Dados da base blivo2026 | Relações STIC/STIL/SL | Não generalizar para outras instalações |
| Reconstrução Digicanola | Dados Digicanola, SL e CM2 | Cadeia documental FN/`fnstamp` | Um inventário concreto; depende da integridade histórica |
| Comportamentos de assistência | Registos de assistência citados no relatório de dossiers | Enciclopédia e código quando aplicável | Casos concretos, não regras standard |
| Cobertura Scanner | Documentação/auditoria de capabilities e contratos de captura | Conteúdo real das capturas | Capability declarada não prova recolha completa |

### 5.1 Fontes de referência

- Enciclopédia PHC, artigos e nomes de ficheiro identificados no relatório de dossiers e no relatório de stock/inventários.
- Script `docs/sources/triggers phc.sql`, na versão fornecida para esta investigação.
- Cópia textual do script analisada, com 21.713 linhas; as referências de linha são relativas a essa cópia.
- Configuração/captura `CSDefault202603`, usada para configuração, não como prova de transacções reais.
- Bases de dados `blivo2026`, `Digicanola` e `dfblock`, sempre analisadas separadamente.
- Registos de assistência, apenas como evidência contextual de casos específicos.

Os relatórios individuais contêm os títulos dos artigos, tabelas, contagens, exemplos, identificadores e limitações. Esta síntese não substitui esses elementos de detalhe.

## 6. Resultados empíricos que não devem ser misturados

| Base | Evidência consolidada | O que permite afirmar |
|---|---|---|
| CSDefault202603 | BO/BI/BI2 sem documentos transaccionais; configuração de dossiers disponível | Permite estudar configuração, não validar um circuito transaccional real |
| blivo2026 | 226 STIC; 1.405 STIL; 176 inventários lançados, dos quais 126 com pelo menos um SL associado e 50 sem SL associado | Demonstra que lançamento e geração de movimentos não são equivalentes; requer análise caso a caso |
| Digicanola | Inventário de 31-12-2025: 442 combinações artigo/armazém; 441 coincidentes e uma divergência explicada pela classificação CM2 de um movimento documental | Valida a reconstrução naquele inventário, não uma regra universal |
| dfblock | 20 STIC e 9.778 STIL identificados | Inventário de trabalho por concluir; ainda não permite conclusões sobre lançamento/movimentos |

Os números completos e exemplos de cada base permanecem nos respectivos relatórios. Não somar contagens nem inferir que as bases representam a mesma instalação.

## 7. Plano de investigação em aberto

A ordem abaixo privilegia primeiro as lacunas que bloqueiam a compreensão funcional e só depois a validação de casos adicionais.

| Prioridade | Questão | Evidência necessária | Resultado esperado |
|---|---|---|---|
| P1 | Como é criada/actualizada a associação BIPRECAT e como ocorre a conversão pré-cativo → cativo? | Esquema BIPRECAT; pesquisa read-only em procedures, funções, views e triggers; análise do mecanismo aplicacional/Monitor | Identificar a fonte de escrita e o ciclo técnico, ou documentar que reside no executável |
| P1 | Qual é a lógica completa de lançamento de inventários? | Código disponível, configuração efectiva e testes controlados em base de testes | Explicar condições de geração, sinal, armazém, quantidades e ausência de SL |
| P1 | Porque existem inventários lançados sem movimentos SL associados? | Análise por STIC/STIL, diferenças e movimentos; casos sem diferença e casos excepcionais | Classificar os casos sem assumir falha |
| P2 | Qual é o circuito de valorização SV? | Esquema e dados SV, relações documentais, configuração e exemplos de movimentos | Distinguir valorização de regularização física |
| P2 | Como se comporta o inventário com lotes/localizações? | Base de teste com gestão activa de lotes/localizações e registos relevantes | Validar dimensões que não estavam representadas em blivo2026 |
| P2 | O que significam e quando são preenchidos STIL.stockatual, diferenca e valstock? | Comparação antes/depois de operações controladas e código disponível | Determinar se são campos transitórios, calculados ou específicos de certos processos |
| P2 | O que revela a amostra dfblock? | Cruzamento dos 20 STIC e 9.778 STIL com lanca/tipolanca e SL | Fechar ou refinar a evidência da terceira base |
| P3 | Como detectar reconstruções históricas incompletas? | Casos de movimentos apagados, alterações retroactivas, documentos anulados/reprocessados e alterações CM2 | Definir critérios de confiança para reconstrução histórica |
| P3 | A cobertura real do Scanner é suficiente para localizar o mecanismo? | Inventário real de objectos recolhidos por tipo e validação de definições | Decidir se é necessário ajustar a recolha, sem assumir antecipadamente uma alteração |

### 7.1 Condições de execução

- Qualquer inspecção de bases de dados deve ser estritamente read-only.
- Testes funcionais devem ocorrer numa base de testes, nunca na CSDefault202603 nem em bases de produção.
- Antes de pedir alterações ao Scanner ou Portal, identificar a fonte concreta que falta e confirmar se a recolha actual a consegue obter.
- Se o mecanismo residir no executável PHC e não estiver disponível em SQL, registar explicitamente essa limitação em vez de inventar uma explicação.
- Preservar a separação entre evidência standard, configuração concreta, personalização e assistência.

## 8. Critérios para considerar a investigação fechada

A investigação funcional poderá ser considerada suficientemente consolidada quando:

1. O ciclo de dossiers, movimentos e reservas estiver descrito com condições e excepções relevantes, suportado por documentação/código/configuração.
2. O ciclo de pré-cativo estiver explicado desde a associação à encomenda de fornecedor até à conversão ou cancelamento, ou a limitação de acesso ao mecanismo estiver demonstrada.
3. O lançamento de inventário estiver explicado ao nível necessário para distinguir contagem, stock teórico, acerto e valorização.
4. Os inventários lançados sem SL estiverem classificados por causa, pelo menos nas amostras investigadas.
5. A reconstrução histórica tiver critérios explícitos de completude e confiança.
6. Lotes/localizações e a amostra dfblock estiverem validados ou formalmente delimitados como fora do âmbito.
7. Cada conclusão importante tiver fonte e âmbito identificáveis, sem misturar bases ou versões.

Fechar a documentação não significa que todas as rotinas internas do PHC tenham de ser reproduzidas. Significa que sabemos o que está demonstrado, o que é específico de uma instalação, o que continua desconhecido e quais as consequências dessa incerteza.

## 9. Estado final desta consolidação

**Documentado:** modelo conjunto, mapa de rastreabilidade, proveniência das fontes, resultados por base, regras de interpretação e plano de lacunas.

**Ainda não demonstrado:** mecanismo técnico de BIPRECAT/conversão de pré-cativo; algoritmo integral de lançamento de inventários; circuito SV; semântica completa de alguns campos STIL; comportamento com lotes/localizações; análise dfblock; critérios completos de integridade histórica.

Este documento fecha a organização e rastreabilidade do conhecimento actualmente disponível. Não declara encerrada a investigação técnica.
