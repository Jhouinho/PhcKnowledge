# Consolidação global do conhecimento PHC — 27-11-2026

- **Estado:** auditoria em curso; este documento é o ponto de controlo da consolidação, não uma conclusão final.
- **Repositório:** `Jhouinho/PhcKnowledge`
- **Branch de trabalho:** `docs/consolidacao-global-conhecimento-phc-2026-11`
- **Base auditada:** `main`, árvore Git consultada em 27-11-2026.
- **Regra de governação:** não alterar a `main` directamente; preservar relatórios históricos; não promover hipóteses a factos.

## 1. Objectivo

Consolidar o conhecimento técnico e funcional PHC já investigado, cruzando dicionário de dados, relações, triggers, procedimentos, documentação funcional e conclusões anteriores. O resultado deverá permitir navegar de um objecto (tabela/campo/trigger) para as relações, circuitos, regras de negócio e evidências que o explicam.

Esta consolidação não é uma nova auditoria ao produto PHC Knowledge nem uma alteração ao Scanner. O foco é o conhecimento sobre o PHC e as fontes já reunidas no repositório.

## 2. Fotografia verificada do repositório

Na consulta recursiva da árvore Git de `main` em 27-11-2026 foram observados:

- 613 entradas na árvore Git;
- 90 ficheiros Markdown;
- 49 ficheiros Markdown em `docs/architecture/`;
- 6 relatórios em `docs/audits/`;
- 3 ficheiros em `docs/sources/`.

Fontes e documentos de maior relevância já identificados:

| Recurso | Conteúdo / papel | Tratamento nesta consolidação |
|---|---|---|
| `docs/sources/analisar-base-phc-2026-11.txt` | Exportação de conversa com 338 páginas, com investigação acumulada sobre PHC, Scanner, tabelas e relações | Fonte histórica a percorrer e indexar; não assumir que todas as conclusões nela contidas continuam válidas |
| `docs/sources/triggers phc.sql` | Script SQL legível de triggers PHC | Fonte primária para análise estática dos corpos SQL; preservar hash e proveniência |
| `docs/audits/investigacao-funcional-csdefault-ft-fi-td-2026-11-25.md` | Investigação funcional de FT, FI e TD com captura CS Default, relações PHC e triggers | Relatório existente a integrar; não duplicar |
| `docs/audits/recuperacao-investigacao-funcional-vo-dossiers-2026-11-25.md` | Recuperação da investigação de dossiers | Relatório existente a integrar |
| `docs/architecture/knowledge-layers.md` | Separação entre factos, interpretação, projecção, conhecimento humano e histórico | Princípio de modelação a respeitar |
| `docs/architecture/decisoes-aprovadas-2026-11-21.md` | Decisões arquitecturais aprovadas do PHC Knowledge | Referência normativa para decisões da plataforma; não confundir com regras funcionais PHC |
| `PhcKnowledgeScanner/samples/database-schema-snapshot/` | Fixture de snapshot de esquema com tabelas, índices, relações, triggers, procedimentos, funções, tipos e vistas | Evidência do formato/capacidade do Scanner; não é, por si só, o dicionário completo da base CS Default investigada |
| `PhcKnowledgeScanner/src/.../Inventory/` e `SchemaSnapshot/` | Modelos e análise de inventário, relações declaradas/derivadas e snapshot de esquema | Fonte técnica para compreender o que o Scanner consegue representar; separar capacidade implementada de dados efectivamente recolhidos |

**Limite importante já confirmado:** o relatório FT/FI/TD descreve uma captura CS Default com 17 182 objectos, 8 294 tabelas, 8 268 chaves primárias, 564 triggers e 921 relações declaradas (295 REL + 626 DTL). Essa captura não recolheu colunas físicas, tipos de dados nem índices. A operação de foreign keys SQL terminou com zero resultados; isso não substitui as relações PHC REL/DTL. A recolha de triggers ficou parcial/indisponível na captura, pelo que o script SQL é uma fonte independente e complementar.

Os números acima pertencem à captura e ao relatório identificados; não devem ser apresentados como inventário universal de todas as versões ou instalações PHC.

## 3. Modelo de consolidação

A consolidação será organizada em registos interligados, sem criar cinco inventários isolados:

1. **Objectos e dicionário:** tabela, campo, tipo, nulabilidade, valor por defeito, chave, índice e descrição, sempre com fonte e estado de recolha.
2. **Relações estruturais:** REL, DTL, FK SQL e outras relações declaradas, mantendo a natureza e a expressão original de cada evidência.
3. **Dependências técnicas:** trigger, procedimento, função, vista ou código; evento; tabelas/colunas lidas e escritas; condições; efeitos; reversões.
4. **Circuitos funcionais:** sequência de documentos, configurações, movimentos e saldos, com variantes e excepções.
5. **Evidência e interpretação:** afirmação, fonte exacta, excerto/linhas quando aplicável, âmbito, confiança, contradições e validação pendente.

Relação estrutural, dependência funcional e referência textual não são equivalentes. Uma interpretação não substitui nem altera o facto de origem.

## 4. Estados de evidência

Cada afirmação deverá usar um estado explícito:

- **Confirmado no esquema/captura:** observado directamente em metadados recolhidos, com identificação da captura.
- **Confirmado no SQL:** condição ou efeito explícito no código SQL analisado, com trigger/procedimento e localização.
- **Documentado pelo fabricante:** descrito em documentação PHC/Manufactor identificada.
- **Inferido por cruzamento:** interpretação coerente de duas ou mais fontes, explicitamente marcada como interpretação.
- **Por validar em execução:** requer teste controlado numa instalação.
- **Desconhecido / evidência insuficiente:** as fontes disponíveis não permitem concluir.

A ausência de um elemento numa captura parcial ou numa fonte incompleta nunca será registada como prova de inexistência.

## 5. Domínios já identificados para integração

A lista seguinte é um índice inicial de áreas mencionadas nos relatórios e na investigação em curso; não significa que todas estejam integralmente analisadas.

- **Stock e logística:** SL, ST, SA, SAL, SX, SV, BI, FI, FN, TS, TD, CM2 e inventários.
- **Documentos comerciais:** FT, FI, TD e estruturas relacionadas, incluindo ligações a clientes e dossiers.
- **Tesouraria e reconciliação:** BA, BABR, BL, BR, BX e OL.
- **Produção:** ordens e operações do Manufactor, consumos, entradas de produto acabado, reservas e dossiers de integração com PHC Gestão.
- **Personalizações e desenvolvimento:** regras, eventos, valores por defeito, formulários, scripts e objectos SQL personalizados.
- **Estrutura transversal:** dicionário, relações declaradas, chaves físicas, triggers, procedimentos, funções e vistas.

## 6. Primeiras conclusões e cautelas a preservar

### 6.1 Stock

A investigação em curso identificou BI, FN e FI como origens de movimentos SL sob condições de configuração diferentes. Os triggers de SL analisados actualizam/revertem ST e SA; a lógica observada trata transferências de forma distinta dos movimentos normais. A criação de dois movimentos SL para uma linha BI de transferência foi identificada no trigger BI, usando os armazéns e códigos de movimento configurados.

Continuam por fechar, entre outros pontos: a validação dos códigos CM2 e do sentido de ambos os movimentos; o ciclo integral de update/delete/anulação em todos os produtores; a manutenção de SAL e SX; os restantes produtores de SL; e a confirmação em execução real.

### 6.2 Produção

A documentação Manufactor consultada distingue reservas, quantidades planeadas/a fabricar, consumos efectivos e entradas de produto acabado. Consumos e entradas podem ocorrer em momentos diferentes. Ainda não está demonstrada, nas fontes SQL analisadas, a ligação técnica completa entre os movimentos próprios do Manufactor e a SL do PHC Gestão. Não inferir que BI.producao descreve todo o circuito Manufactor.

### 6.3 Tesouraria

Foram identificadas estruturas BA, BABR, BL, BR e BX e campos de ligação a OL. A descrição funcional, cardinalidades e ciclo de reconciliação ainda necessitam de ser consolidados a partir das fontes originais.

## 7. Sequência de trabalho desta auditoria

1. Inventariar todos os documentos de auditoria/arquitectura relevantes e identificar a respectiva autoridade temporal.
2. Percorrer a exportação histórica de 338 páginas e extrair um índice de tabelas, campos, relações, triggers, procedimentos, circuitos e decisões, sem copiar cegamente conclusões antigas.
3. Reanalisar o script SQL de triggers por grupos funcionais e produzir uma matriz de dependências (evento → condições → leituras → escritas → reversões).
4. Cruzar cada objecto e relação com a captura CS Default, a Enciclopédia e as restantes fontes disponíveis, registando limitações de cobertura.
5. Consolidar os circuitos funcionais e as lacunas de evidência.
6. Rever a coerência entre os novos documentos e os relatórios existentes; preservar documentos históricos e evitar duplicação.
7. Apresentar a consolidação para revisão antes de qualquer merge em `main`.

## 8. Critérios de conclusão

A consolidação só será considerada concluída quando:

- existir um índice pesquisável dos objectos, relações, triggers e circuitos encontrados;
- cada conclusão material tiver fonte identificável e estado de evidência;
- as relações entre tabelas e dependências funcionais estiverem separadas;
- as contradições e lacunas estiverem explicitamente registadas;
- os relatórios anteriores tiverem sido integrados sem apagar o seu contexto histórico;
- for possível navegar de uma tabela/campo para os triggers, documentos e circuitos que o utilizam;
- os limites da captura e das fontes estiverem visíveis;
- o conteúdo estiver coerente com as decisões vigentes e pronto para revisão por pull request.

## 9. Estado desta primeira passagem

**Feito:** leitura da árvore actual de `main`; identificação das fontes brutas, dos seis relatórios de auditoria, da documentação de camadas de conhecimento e da fixture/modelos de snapshot do Scanner; criação de branch isolada para a consolidação.

**Ainda não feito:** extração integral da conversa histórica, análise exaustiva do SQL, cruzamento completo com a Enciclopédia e construção final das matrizes. Não declarar estes trabalhos concluídos nesta fase.
