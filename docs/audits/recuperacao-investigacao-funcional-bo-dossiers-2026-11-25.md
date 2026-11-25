# Recuperação e estado da investigação funcional — BO e dossiers internos

**Data:** 25-11-2026  
**Estado:** recuperação documental efectuada; investigação funcional parcialmente reconstruída e com validações pendentes.  
**Repositório:** `Jhouinho/PhcKnowledge`.

## 1. Correcção do âmbito

A auditoria anterior usou uma sigla errada. O tema que o utilizador pretendia recuperar é a tabela **BO** (dossiers internos), não outra tabela. A nota anterior com `vo` no nome e no conteúdo foi criada por uma interpretação incorrecta e é substituída por esta.

## 2. Trabalho já existente antes desta consolidação

Já tinham sido identificados na CS Default 202603:
- estrutura e chave da BO;
- relações PHC declaradas com BI e tabelas relacionadas;
- triggers BO de INSERT, UPDATE e DELETE;
- nove configurações TS/TS2, para os tipos de dossier 1, 2, 3, 4, 5, 6, 7, 10 e 11;
- configuração de stock dos tipos 4 a 7.

A base standard não tinha registos transaccionais BO/BI/BI2, pelo que o comportamento não podia ser validado por ensaio operacional nessa base.

## 3. Recuperação feita nesta passagem

Foi reutilizado o ficheiro de triggers SQL já enviado pelo utilizador e cruzado com a Enciclopédia PHC. A análise confirmou:
- `tr_bo_insert` e `tr_bo_update`: validação condicional da assinatura digital através de BO2, TS e TS2;
- `tr_bo_delete`: eliminação de BOTXT associado ao cabeçalho;
- `tr_bi_insert`, `tr_bi_update` e `tr_bi_delete`: criação, actualização e reversão de movimentos SL e reservas ST/SA, além da gestão de linhas ligadas por `obistamp`;
- `tr_bo2_update`: ao mudar `BO2.anulado` de 0 para 1, reverte movimentos, reservas e registos auxiliares derivados das linhas.

A síntese funcional, com condições, tabelas afectadas, cruzamento com a Enciclopédia e limitações, está em [Investigação funcional PHC — BO, BI e movimentos de stock](investigacao-funcional-bo-bi-sl-2026-11-25.md).

## 4. Estado actual

| Área | Estado |
|---|---|
| Estrutura BO e relações previamente inventariadas | Identificadas |
| Triggers BO | Analisados |
| Triggers BI | Analisados ao nível dos principais ramos |
| Anulação lógica através de BO2 | Analisada no código |
| Configuração TS/TS2 por tipo | Parcial; falta reconciliação campo a campo |
| Circuito BO → BI → SL / ST / SA | Parcialmente reconstruído |
| Cópia/satisfação entre dossiers por obistamp | Mecanismo SQL identificado; cenários de aplicação por validar |
| Ensaios funcionais controlados | Pendentes |

## 5. Próximos passos

1. Consultar os valores efectivos de TS/TS2 por `ndos`, incluindo códigos CM, transferência, produção, reservas, cativação e assinatura.
2. Cruzar todas as relações REL/DTL relevantes com as ligações observadas nos triggers.
3. Analisar os eventos, regras e rotinas da aplicação que executam cópias, fechos e anulações.
4. Validar os cenários numa cópia de teste autorizada, sem escritas na CS Default ou em bases de clientes.
5. Actualizar o relatório e esta nota apenas quando existirem novas evidências.

## 6. Relação com FT/FI/TD

A análise de BO/BI complementa a investigação de FT/FI/TD em [Investigação funcional PHC — CS Default: FT, FI e TD](investigacao-funcional-csdefault-ft-fi-td-2026-11-25.md). A relação FI → BI já identificada deve ser lida em conjunto com a ligação BO → BI e com a actualização da quantidade 2 da linha de origem. Não se devem fundir os circuitos: faturação e dossiers internos têm triggers e configurações próprias.
