# Visão futura — edição e publicação controlada pelo Scanner

**Data:** 2026-11-07  
**Estado:** ideia registada / visão futura  
**Âmbito:** aplicação Scanner do PHC Knowledge  
**Implementação:** não iniciada; não altera o âmbito da Fase 1

## 1. Origem

Durante a análise de uma instalação PHC, foi identificado um caso de utilização: depois de localizar todas as utilizações de um campo de utilizador, o técnico gostaria de corrigir um dos objetos encontrados e enviar a versão corrigida para a instalação PHC.

A ideia foi considerada uma evolução útil do Scanner: além de recolher e ajudar a compreender personalizações, poderá futuramente permitir editar e publicar alterações de forma controlada.

## 2. Decisão de produto

Esta capacidade pertence **exclusivamente ao Scanner**.

Não constitui uma nova área funcional da plataforma central PHC Knowledge, nem implica que a plataforma central passe a editar ou publicar alterações nas instalações dos clientes.

A plataforma central poderá, no futuro, consumir conhecimento e histórico de capturas relacionados com objetos, mas a interação de edição e publicação na instalação é responsabilidade da aplicação Scanner.

## 3. Fluxo funcional pretendido

1. **Localizar:** o técnico pesquisa um objeto ou campo e consulta as respetivas utilizações e dependências identificadas pelo Scanner.
2. **Inspecionar:** abre o conteúdo de um objeto suportado e consulta a origem e o contexto recolhido.
3. **Editar:** cria uma versão de trabalho corrigida, sem modificar a evidência original da captura.
4. **Comparar:** visualiza diferenças entre a versão recolhida e a versão proposta.
5. **Validar:** executa as validações disponíveis para o tipo de objeto, identifica dependências e apresenta avisos ou limitações.
6. **Publicar:** envia a alteração para a instalação PHC através de um mecanismo específico e controlado para esse tipo de objeto.
7. **Confirmar:** regista o resultado da publicação e permite verificar o estado resultante através de nova leitura/scan.

## 4. Princípios e salvaguardas

- **Separação entre leitura e escrita:** os collectors atuais mantêm o comportamento de recolha sem alterações na instalação. A publicação será uma capacidade explícita e separada.
- **Evidência original imutável:** editar nunca substitui nem reescreve silenciosamente o conteúdo originalmente recolhido.
- **Identidade e proveniência:** antes de publicar, confirmar que o objeto de destino corresponde ao objeto que foi inspecionado e que a versão atual não divergiu inesperadamente.
- **Diff obrigatório:** apresentar claramente o conteúdo anterior e o conteúdo proposto.
- **Validação por tipo:** não assumir que todos os objetos PHC podem ser atualizados da mesma forma. Cada tipo suportado necessita de uma estratégia de leitura, validação e publicação própria.
- **Aprovação explícita:** nenhuma alteração é publicada sem ação consciente do técnico.
- **Auditoria:** registar autor, instalação/ambiente, objeto, versão de origem, alteração proposta, momento e resultado da publicação.
- **Falhas seguras:** se a identidade, versão, permissões ou condições de publicação não puderem ser confirmadas, bloquear a operação em vez de tentar uma escrita especulativa.
- **Âmbito progressivo:** começar apenas com tipos de objetos cuja publicação possa ser feita e validada com segurança.

## 5. Fronteiras arquiteturais

A capacidade futura deverá residir na aplicação Scanner, mas não deve ser misturada com a lógica dos collectors:

- collectors: recolha e observação, sem efeitos de escrita;
- editor/proposta de alteração: gestão de versões de trabalho;
- validador: verificações específicas por tipo;
- publisher: adaptadores explícitos para aplicar alterações suportadas;
- verificação pós-publicação: nova leitura e comparação do estado efetivo.

A existência de um publisher não transforma o Scanner num executor SQL genérico. A publicação deve operar sobre objetos e operações explicitamente suportados, com contratos próprios e controlos de segurança.

## 6. Relação com o roadmap atual

Esta é uma visão futura para evolução do Scanner, posterior à estabilização das capacidades atuais de captura e identidade.

Não altera:
- o foco imediato em P1.B, Object Index e IKP;
- o requisito de validar o contrato com uma captura real;
- o princípio de que os scans e collectors atuais são read-only;
- a fronteira da plataforma central, que continua a consumir capturas e conhecimento, não a publicar alterações nas instalações.

A identidade, proveniência, fingerprints, dependências e comparação de versões em desenvolvimento na Fase 1 serão fundamentos úteis para esta capacidade, mas não significam que a publicação esteja implementada ou já seja segura.

## 7. Critério para retomar

Retomar o desenho detalhado quando a captura/identidade estiver estabilizada e houver um caso concreto que permita escolher um primeiro tipo de objeto suportado.

Antes de implementar, especificar para esse tipo:
- como identificar inequivocamente o objeto e a instalação;
- como detetar alterações concorrentes desde a captura;
- como validar a alteração;
- qual o mecanismo PHC suportado para a aplicar;
- como tratar erros, reversão ou recuperação;
- como confirmar o resultado através de nova leitura.

**Regra:** o Scanner poderá futuramente ajudar a corrigir e publicar personalizações PHC, mas apenas através de operações explícitas, validadas, auditáveis e suportadas por tipo de objeto.
