# PHC Knowledge — Acordo de continuidade entre ferramentas

**Estado:** regra de trabalho aprovada  
**Âmbito:** ChatGPT, Cursor e qualquer outra ferramenta usada no desenvolvimento  
**Fonte partilhada:** este repositório GitHub

## 1. Regra fundamental

Nenhuma ferramenta deve continuar o desenvolvimento do PHC Knowledge exclusivamente com base na sua memória, no histórico de conversas ou em contexto local não partilhado.

Antes de analisar, planear ou alterar o projeto, deve consultar o estado publicado e relevante do repositório. Antes de terminar uma sessão, deve deixar informação suficiente para a ferramenta seguinte continuar sem reconstruir decisões ou adivinhar o estado do trabalho.

O GitHub é a fonte partilhada de verdade para o estado versionado do projeto. Não representa automaticamente alterações locais ainda não publicadas.

## 2. Leitura obrigatória antes de trabalhar

No início de cada sessão ou transição:

1. Confirmar repositório, branch e commit HEAD.
2. Consultar as instruções de projeto aplicáveis (incluindo `AGENTS.md`, regras em `.cursor/rules/` e instruções equivalentes).
3. Ler a baseline e as decisões vigentes referenciadas no índice de governação do projeto.
4. Ler `docs/governance/current-handoff.md`.
5. Consultar os documentos técnicos diretamente relacionados com a tarefa.
6. Verificar alterações recentes e, quando relevante, comparar a branch com a base apropriada.

Se a baseline, o handoff e o código divergirem, não escolher silenciosamente uma versão. Registar a divergência, identificar a evidência e pedir validação quando a decisão de produto não estiver clara.

## 3. Registo obrigatório de informação

As ferramentas devem distinguir explicitamente:

- **Decisão proposta:** ainda não aprovada pelo utilizador.
- **Decisão aprovada:** orientação de produto/arquitetura validada pelo utilizador.
- **Implementado:** existe no código; indicar localização e commit/branch.
- **Validado:** indicar testes, comandos e resultados efetivamente observados.
- **Pendente:** ainda não executado ou sem evidência suficiente.
- **Substituído:** decisão/documento antigo, com referência à decisão que o substituiu.

Uma proposta não se transforma em decisão por aparecer num documento, prompt, comentário ou implementação. Uma implementação não prova, por si só, que a decisão de produto foi aprovada.

## 4. Transição Cursor → GitHub → ChatGPT

Antes de terminar uma sessão no Cursor:

1. Atualizar `docs/governance/current-handoff.md`.
2. Registar o que foi alterado, com caminhos dos ficheiros.
3. Indicar testes executados e resultado exato; separar testes não executados.
4. Registar descobertas, decisões propostas/aprovadas, riscos e questões em aberto.
5. Identificar alterações locais não commitadas, commits locais ainda não publicados e outros estados que o GitHub não consiga ver.
6. Publicar as alterações apenas segundo o fluxo Git acordado pelo utilizador.

Ao retomar no ChatGPT, consultar o GitHub e o handoff. Se houver trabalho local não publicado, pedir ao utilizador o respetivo resumo/diff ou acesso ao estado local; nunca afirmar que foi consultado quando não foi.

## 5. Transição ChatGPT → Cursor

Quando o ChatGPT preparar trabalho para o Cursor:

1. Referenciar decisões e documentos vigentes, em vez de reescrever requisitos de memória.
2. Dar instruções delimitadas, com objetivo, âmbito, restrições e critérios de aceitação.
3. Distinguir claramente decisões aprovadas de propostas.
4. Indicar o que não deve ser alterado.
5. Pedir ao Cursor que valide o estado real do código antes de implementar.
6. Exigir atualização do handoff no fim da execução.

## 6. Disciplina de Git e alterações

- Não assumir que a branch ativa é `main`; confirmar sempre.
- Não assumir que o último commit inclui todas as alterações locais.
- Não fazer commit, push, merge, rebase ou mudança de branch sem autorização explícita do utilizador.
- Não sobrescrever trabalho local ou remoto desconhecido.
- Antes de qualquer escrita no GitHub, confirmar branch e estado atual do ficheiro.
- Uma alteração publicada pelo GitHub é um commit real; não a descrever como mera edição local.
- Não apagar documentação histórica durante uma consolidação sem inventário, justificação e validação.

## 7. Protocolo para conflitos e perda de contexto

Se duas fontes divergirem:

1. Preservar a evidência de ambas.
2. Identificar se a diferença é temporal, uma proposta, uma decisão aprovada ou uma implementação.
3. Consultar o utilizador quando a resolução exigir uma decisão de produto.
4. Atualizar a fonte canónica e marcar explicitamente a informação substituída.
5. Propagar a alteração ao handoff e às instruções relevantes.

Nunca reabrir uma decisão aprovada apenas por não estar presente no contexto da sessão. Consultar o registo de decisões primeiro.

## 8. Documentos operacionais

- `docs/governance/current-handoff.md`: estado de continuidade da sessão mais recente.
- `docs/governance/working-agreement.md`: este protocolo.
- `.cursor/rules/project-context.mdc`: instruções obrigatórias para o Cursor.

A baseline e o registo canónico de decisões serão identificados/consolidados durante a auditoria documental. Até essa auditoria estar concluída, não inventar caminhos nem presumir que um documento antigo é vigente.

## 9. Critério de conclusão de uma sessão

Uma sessão só fica pronta para transição quando outra ferramenta consegue perceber, sem depender da conversa anterior:

- em que branch e commit se trabalhou;
- qual era o objetivo;
- o que mudou;
- o que foi realmente testado;
- que decisões foram tomadas ou continuam pendentes;
- que alterações ainda não estão no GitHub;
- qual é o próximo passo seguro.
