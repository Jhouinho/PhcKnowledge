# Investigação funcional PHC — CS Default: BO (primeira passagem)

**Data:** 26-11-2026  
**Base de referência:** `CSDefault202603`  
**Estado:** primeira passagem de análise estática dos triggers; análise funcional incompleta.  
**Repositório:** `Jhouinho/PhcKnowledge`.

## 1. Objectivo e relação com a investigação anterior

Esta passagem dá continuidade à investigação funcional de FT/FI/TD e ao levantamento posterior de dossiers internos. O foco inicial foi a tabela `BO` e os triggers associados.

Este documento não substitui o relatório [FT, FI e TD](investigacao-funcional-csdefault-ft-fi-td-2026-11-25.md). Regista apenas os elementos adicionais identificados nesta passagem e explicita o que continua por demonstrar.

## 2. Fontes e método

Fontes a cruzar nesta investigação:

1. Captura e base de referência `CSDefault202603`.
2. Script de triggers PHC já disponibilizado para o projecto e referenciado no relatório FT/FI/TD como `docs/sources/triggers phc.sql`.
3. Dicionário de dados PHC (`Dic`).
4. Relações declaradas PHC (`Rel` / REL / DTL).
5. Enciclopédia PHC.
6. Documentação pública oficial PHC e outras fontes técnicas públicas, quando relevantes e verificáveis.

A análise deve distinguir:
- **Estrutura/documentação:** o que `Dic` e `Rel` declaram.
- **Código:** condições e efeitos expressos nos triggers.
- **Dados da instalação:** registos efectivamente existentes em `CSDefault202603`.
- **Interpretação funcional:** leitura conjunta das fontes, sempre limitada pela evidência.
- **Por validar:** qualquer comportamento que dependa de dados transaccionais, configuração ou execução controlada.

### 2.1 Correcção metodológica

O dicionário `Dic` e as relações `Rel` são fontes que já fazem parte do conhecimento PHC reunido no projecto. Devem ser consultados antes de solicitar ao utilizador consultas SQL para redescobrir nomes de campos ou relações declaradas.

Uma consulta à base real só deve ser pedida quando responder a uma questão concreta que não possa ser resolvida com as fontes existentes — por exemplo, confirmar presença de registos, valores de configuração ou comportamento físico específico da instalação.

As relações PHC declaradas não devem ser confundidas com foreign keys SQL, nem tomadas isoladamente como prova de que existem registos relacionados.

## 3. BO — evidência identificada

### 3.1 Estado dos dados

Foi reportado para esta passagem que a consulta de contagem a `dbo.BO` devolveu **0 registos** na base `CSDefault202603`.

Este resultado limita a validação empírica: não permite observar documentos BO reais, nem confirmar percursos de criação, alteração, transformação ou eliminação através de dados transaccionais desta captura.

O resultado não prova que a tabela não exista, que não seja utilizada noutras instalações, nem que os triggers não tenham comportamento relevante.

### 3.2 Triggers identificados no script

Na leitura anterior do script de triggers foram identificados:
- `tr_bo_insert`;
- `tr_bo_update`;
- `tr_bo_delete`.

Os triggers de INSERT e UPDATE incluem validações relacionadas com assinatura digital/documentos importados, cruzando informação de `BO`, `BO2`, `TS` e `TS2`.

As condições observadas nessa leitura incluem os campos:
- `TS2.assinaimp`;
- `TS2.docsimport`;
- `TS2.assinadoc`;
- `BO2.assinatura`;
- `BO.obrano`.

Foi identificada uma validação que considera a combinação de `TS2.assinaimp = 0`, `TS2.docsimport = 0`, `TS2.assinadoc = 1`, assinatura vazia em `BO2.assinatura` e `BO.obrano > 0`, produzindo erro e rollback quando a condição de validação é violada.

**Limite:** esta síntese regista o comportamento encontrado na leitura estática do script. Não constitui prova de execução na base `CSDefault202603`, nem permite generalizar a regra a todas as versões/configurações PHC. A expressão completa e a mensagem exacta devem ser novamente conferidas no script antes de qualquer especificação detalhada.

### 3.3 Eliminação de texto associado

No trigger `tr_bo_delete` foi identificada a operação de limpeza de registos em `botxt` associados pelo campo `bostamp` aos registos BO eliminados.

A operação observada é equivalente a:

```sql
DELETE FROM botxt
FROM deleted
WHERE botxt.bostamp = deleted.bostamp;
```

A conclusão limitada é que o trigger remove texto associado à chave `bostamp` quando ocorre a eliminação de BO. Não se infere daqui o ciclo de vida completo de anexos, linhas, movimentos de stock ou documentos relacionados.

## 4. Relações e entidades a aprofundar

A análise seguinte deve partir de `Dic` e `Rel`, e depois seguir o código dos triggers. Os elementos abaixo são pistas de investigação, não relações funcionais já demonstradas:

| Elemento | Pergunta a resolver |
|---|---|
| BO / BO2 | Papel de cabeçalho e extensão/complemento; campos de ligação e cardinalidade |
| TS / TS2 | Como a configuração do tipo de dossier condiciona assinatura e importação |
| BI / BI2 | Se e em que condições existe ligação ao circuito de dossiers internos |
| SL / SV | Se há movimentos de stock associados e quais os triggers/condições envolvidos |
| BOTXT | Ciclo de vida do texto associado a BO |
| Dossiers relacionados | Transformações, cópias, estados e documentos de origem/destino |

Não se deve inferir que BO gera BI, movimentos SL/SV ou outros documentos apenas pela existência de relações declaradas ou por nomes de campos. Cada percurso exige evidência no código e/ou nos dados.

## 5. Questões em aberto

1. Confirmar no `Dic` os campos e descrições funcionais de BO, BO2, TS, TS2, BI, BI2 e BOTXT.
2. Extrair de `Rel` as relações relevantes de BO e distinguir REL/DTL de foreign keys SQL.
3. Ler integralmente os três triggers BO e registar as condições completas, mensagens, tabelas afectadas e operações.
4. Cruzar as regras de assinatura/importação com os artigos relevantes da Enciclopédia PHC.
5. Identificar os triggers de BO que afectam linhas, stock, tesouraria ou outros dossiers, se existirem.
6. Só depois determinar se é necessária alguma consulta read-only adicional à base real.
7. Não executar INSERT/UPDATE/DELETE nem testes que alterem a base de referência.

## 6. Conclusão desta passagem

Fica registado que:
- a contagem comunicada para `dbo.BO` na `CSDefault202603` foi zero;
- foram identificados no script os triggers `tr_bo_insert`, `tr_bo_update` e `tr_bo_delete`;
- os triggers de INSERT/UPDATE incluem validações associadas a assinatura digital/importação, envolvendo BO, BO2, TS e TS2;
- o trigger de DELETE limpa registos de `botxt` ligados por `bostamp`;
- a análise funcional de BO, das suas relações e dos efeitos sobre outros dossiers permanece incompleta.

**Estado:** descoberta inicial registada; não declarar o circuito BO fechado. A próxima passagem deve começar por `Dic`, `Rel`, leitura integral dos triggers e Enciclopédia, evitando pedir consultas genéricas de estrutura que as fontes existentes já permitem resolver.
