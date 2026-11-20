# PHC Knowledge — Acordo de continuidade entre ferramentas

Estado: regra de trabalho aprovada.
Âmbito: ChatGPT, Cursor e qualquer outra ferramenta usada no desenvolvimento.
Fonte partilhada: repositório GitHub Jhouinho/PhcKnowledge.

## 1. Regra fundamental
Nenhuma ferramenta deve continuar o desenvolvimento exclusivamente com base na memória, histórico de conversas ou contexto local não partilhado. GitHub é a fonte partilhada de verdade para o estado publicado; não representa alterações locais ainda não publicadas.

## 2. Leitura obrigatória
No início de cada sessão:
1. Confirmar repositório, branch, HEAD e estado local.
2. Consultar instruções aplicáveis, incluindo AGENTS.md e .cursor/rules/.
3. Ler docs/governance/README.md.
4. Ler PROJECT_BASELINE.md, DECISIONS.md, PROJECT_STATUS.md e docs/governance/current-handoff.md.
5. Consultar documentação técnica diretamente relacionada com a tarefa.
6. Verificar alterações recentes e comparar branch/base quando relevante.

Se baseline, handoff e código divergirem, identificar a evidência e não escolher silenciosamente uma versão.

## 3. Estados de evidência
- Proposta: ainda não aprovada.
- Decisão aprovada: validada explicitamente pelo responsável.
- Implementado: existe no código; indicar localização e commit/branch.
- Validado: indicar testes/comandos e resultados observados.
- Pendente: não executado ou sem evidência suficiente.
- Substituído: decisão/documento antigo, com referência à decisão vigente.

Uma proposta não se torna decisão por aparecer num documento ou implementação. Implementação não prova, por si só, aprovação de produto.

## 4. Transição Cursor → GitHub → ChatGPT
Antes de terminar no Cursor:
1. Atualizar docs/governance/current-handoff.md.
2. Registar ficheiros alterados e decisões.
3. Indicar testes executados e resultados exatos; separar testes não executados.
4. Registar riscos, questões e alterações locais/commits ainda não publicados.
5. Publicar apenas segundo a autorização do utilizador.

Ao retomar, consultar GitHub e handoff. Se houver trabalho local não publicado, pedir resumo/diff ou acesso ao estado local; nunca afirmar que foi consultado quando não foi.

## 5. Transição ChatGPT → Cursor
Preparar instruções delimitadas com objetivo, âmbito, restrições, critérios de aceitação e documentos canónicos. Exigir confirmação do estado real do código e atualização do handoff no fim.

## 6. Disciplina Git
- Não assumir que a branch ativa é main.
- Não assumir que o último commit inclui alterações locais.
- Não fazer commit, push, merge, rebase ou mudança de branch sem autorização explícita.
- Quando a publicação for autorizada, confirmar HEAD antes da escrita, publicar apenas na branch acordada e usar fast-forward sem force.
- Não sobrescrever trabalho local/remoto desconhecido.
- Não apagar documentação histórica sem inventário, justificação e validação.

## 7. Conflitos
Preservar ambas as evidências; identificar diferença temporal, proposta, decisão ou implementação; consultar o responsável quando necessário; atualizar a fonte canónica e o handoff.

## 8. Documentos canónicos
- PROJECT_BASELINE.md: objetivo, arquitetura e fronteiras.
- DECISIONS.md: decisões aprovadas, substituídas e questões abertas.
- PROJECT_STATUS.md: implementação, validação, limitações e próximos passos.
- docs/governance/current-handoff.md: continuidade da sessão.
- docs/governance/README.md: índice e ordem de leitura.
- .cursor/rules/project-context.mdc: instruções obrigatórias do Cursor.

## 9. Manter o repositório utilizável pela ferramenta seguinte
No fim de cada tarefa, atualizar a fonte canónica afetada e o handoff. Se o utilizador autorizou publicação para essa tarefa, publicar na branch acordada e verificar o HEAD remoto. Se não houver autorização, declarar explicitamente que o estado ficou apenas local e pedir autorização. Não deixar decisões importantes apenas em prompts ou conversas.

Uma sessão só fica pronta para transição quando a ferramenta seguinte consegue perceber branch/commit, objetivo, alterações, testes, decisões, pendências, alterações não publicadas e próximo passo.
