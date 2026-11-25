# Recuperação da investigação funcional — FT, VO e dossiers internos

**Data:** 25-11-2026  
**Estado:** auditoria documental concluída; recuperação funcional de VO pendente de evidência da conversa/consultas originais.  
**Repositório:** `Jhouinho/PhcKnowledge`, branch `main`.

## 1. Objectivo

Recuperar o conhecimento produzido na investigação funcional PHC realizada em 25-11-2026, verificar o que ficou persistido e impedir que conclusões não comprovadas sejam apresentadas como conhecimento adquirido.

Esta nota é uma auditoria de persistência documental. Não substitui a investigação funcional das tabelas nem constitui uma especificação universal do PHC.

## 2. Evidência confirmada no repositório

A branch `main` contém a investigação:

- [Investigação funcional PHC — CS Default: FT, FI e TD](investigacao-funcional-csdefault-ft-fi-td-2026-11-25.md)

O relatório regista proveniência das fontes, relações PHC REL/DTL, triggers de FT/FI/TD, configuração efectiva consultada em TD/OC, movimentos de stock e o circuito de tesouraria/cheques FT → OL/CH → CHL. Distingue factos observados, relações declaradas, comportamento observado no código, interpretação funcional e questões por validar.

O relatório cobre, entre outros pontos:
- FT como cabeçalho de faturação e ponto de coordenação de efeitos comerciais/financeiros condicionais;
- FI como nível das linhas e materialização de efeitos por artigo, incluindo SL e SV;
- TD como configuração do tipo de documento, cujos parâmetros condicionam os triggers;
- distinção entre anulação lógica e eliminação física;
- separação entre criação de OL e criação de CH;
- criação do movimento CHL de aceitação (`cm=1`) sem criação de OL nesse ramo;
- ramos adicionais de CHL (`cm=3`) que podem originar OL sob condições específicas, mantendo a semântica funcional geral por validar;
- limites da validação empírica, dado que as tabelas transaccionais consultadas não tinham documentos para ensaio.

O README da raiz já referencia este relatório.

## 3. O que a auditoria do histórico confirma

Os commits de 25-11-2026 na `main` alteraram e aprofundaram o relatório FT/FI/TD. Em particular, os commits de consolidação inicial, aprofundamento dos triggers e fecho da análise de tesouraria acrescentaram conteúdo ao mesmo ficheiro.

A listagem actual de `docs/audits` contém o relatório FT/FI/TD, mas não contém um relatório funcional autónomo de VO ou dossiers internos.

A pesquisa de código no repositório não encontrou documentação indexada por `VO`, `VOstamp`, `tr_vo_insert`, `tr_vo_update` ou `tr_vo_delete`. A pesquisa de texto não é prova de inexistência de qualquer referência indirecta, mas, em conjunto com o inventário de auditorias e os commits recentes, não permite confirmar que a análise de VO tenha sido persistida.

O arquivo histórico `docs/sources/analisar-base-phc-2026-11.txt` contém contexto técnico anterior, incluindo a investigação de FT/FI e efeitos em PN, SL e BI. Não contém uma sequência recuperável e suficientemente explícita de conclusões da investigação de VO realizada nesta sessão. Não deve ser usado para reconstruir por suposição aquilo que não está registado.

## 4. Estado por assunto

| Assunto | Estado documental | O que podemos afirmar |
|---|---|---|
| FT | Documentado | Investigação funcional e triggers registados no relatório FT/FI/TD |
| FI | Documentado | Relações e efeitos por linha, incluindo SL, SV e BI, registados no mesmo relatório |
| TD | Documentado | Configuração e dependências relevantes dos triggers registadas |
| OL / CH / CHL | Documentado | Circuitos e condições observados no código, com limites explícitos |
| VO | Não confirmado | Não existe relatório autónomo nem evidência suficiente para reconstruir as conclusões |
| Dossiers internos | Parcial | O relatório regista a relação FI → BI declarada e efeitos de triggers sobre BI; não equivale a uma análise integral do circuito dos dossiers internos |
| Inventários | Em investigação separada | A análise da Digicanola e a próxima validação dfblock pertencem a outro eixo de trabalho |

## 5. Limite importante: VO e dossiers internos

Não se deve concluir que VO é sinónimo de BI, nem que a análise de BI substitui a análise de VO. Também não se deve inferir o papel funcional de VO apenas pelo nome da tabela.

Para recuperar correctamente o trabalho de VO, são necessárias as evidências originais que sustentaram a investigação: nomes das tabelas relacionadas, expressões REL/DTL, colunas/stamps, triggers e consultas/resultados SQL utilizados. Sem essas evidências, criar agora uma descrição funcional seria inventar conhecimento.

## 6. Próxima passagem de recuperação

Quando as evidências da sessão estiverem disponíveis, completar a análise pela seguinte ordem:

1. Identificar exactamente a tabela VO e o significado funcional confirmado na instalação/versão analisada.
2. Inventariar as relações PHC REL/DTL de VO e distinguir relações declaradas de foreign keys SQL.
3. Seguir as ligações documentais por stamps e chaves efectivamente observadas.
4. Identificar triggers e operações INSERT/UPDATE/DELETE relevantes, registando condições e efeitos por tabela.
5. Cruzar com a configuração e com resultados read-only da base, quando disponíveis.
6. Construir uma matriz de ciclo de vida e estados do documento/dossier.
7. Separar o circuito de VO do circuito de dossiers internos (incluindo BI), ligando-os apenas onde exista evidência.
8. Actualizar o relatório funcional e o índice/README no mesmo commit de consolidação.

Não alterar dados PHC, triggers, collectors, schemas ou código da plataforma como parte desta recuperação documental.

## 7. Conclusão da auditoria

O conhecimento FT/FI/TD e o aprofundamento de tesouraria/cheques estão efectivamente persistidos em `docs/audits/investigacao-funcional-csdefault-ft-fi-td-2026-11-25.md` e referenciados no README.

A análise de VO e o percurso completo dos dossiers internos **não estão documentalmente comprovados** no estado actual do repositório. O conhecimento de BI que já consta do relatório é parcial e não deve ser apresentado como conclusão integral sobre VO/dossiers internos.

**Resultado:** persistência FT/FI/TD confirmada; lacuna VO/dossiers internos explicitamente registada; nenhuma conclusão funcional foi inventada para preencher essa lacuna.
