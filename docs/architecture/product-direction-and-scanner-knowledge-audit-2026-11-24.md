# Novo rumo do produto e validação do Scanner — PHC Knowledge

**Data:** 2026-11-24  
**Repositório:** `Jhouinho/PhcKnowledge`  
**Estado:** direção de produto acordada para orientar o planeamento; ainda não autoriza alterações a código, collectors, contratos executáveis ou persistência.

## 1. Objetivo desta decisão

Simplificar a primeira versão do PHC Knowledge, validar a qualidade e a utilidade do conhecimento produzido pelo Scanner e entregar valor utilizável mais cedo, preservando a possibilidade de evoluir para histórico, identidade avançada, correspondência entre instalações e RAG.

A prioridade deixa de ser implementar desde início todo o mecanismo de identidade e evolução temporal. Passa a ser construir uma base de conhecimento fiável sobre:

1. o PHC standard;
2. cada instalação de cliente e as respetivas personalizações;
3. as diferenças entre ambos, com evidência e proveniência.

**Princípio orientador:** primeiro garantir que sabemos produzir e interpretar o conhecimento técnico necessário; depois aprofundar a inteligência construída sobre esse conhecimento.

## 2. Âmbito funcional da primeira versão

### 2.1 PHC Standard Knowledge

Manter uma referência central baseada numa instalação PHC CS Default, sem personalizações de cliente, devidamente identificada e versionada.

A referência deve registar, na medida em que a informação esteja disponível:

- produto e edição;
- versão e build/revisão;
- módulos e âmbito funcional abrangidos;
- origem, data e proveniência da captura;
- estrutura física e objetos standard recolhidos;
- definições e código disponíveis;
- relações e mecanismos de comportamento identificados.

A CS Default é uma referência concreta, não uma definição universal e imutável de todas as versões ou configurações PHC. A comparação deve considerar compatibilidade de versão, build, módulos e âmbito. A evolução poderá introduzir referências para outras versões/configurações.

### 2.2 Installation Knowledge

Na primeira versão, cada instalação de cliente terá:

- uma instalação identificada;
- um ambiente considerado: produção;
- uma captura ativa;
- conhecimento persistido e pesquisável no portal.

A limitação a uma captura ativa é uma decisão de âmbito funcional inicial, não uma afirmação de que uma instalação só poderá ter uma captura ao longo da vida. Os identificadores, a proveniência e o contexto da captura devem ser preservados para permitir histórico numa fase posterior.

### 2.3 Experiência inicial do portal

A experiência deverá permitir, pelo menos:

1. explorar o conhecimento PHC standard;
2. explorar o conhecimento de cada instalação;
3. pesquisar globalmente o conhecimento disponível, distinguindo a origem standard e a origem específica da instalação.

A consulta deverá evoluir para fichas de conhecimento de objetos, reunindo estrutura, definição, código, relações, proveniência e diferenças quando esses dados existirem e estiverem validados.

## 3. Comparação com o standard e classificação

A primeira comparação relevante é entre uma captura de instalação e uma referência standard compatível.

As diferenças devem ser apresentadas com evidência, sem inferências excessivas. A classificação conceptual inicial poderá distinguir:

- **Standard igual/compatível:** objeto presente na referência e sem divergência demonstrada segundo critérios validados;
- **Standard alterado:** objeto correspondente ao standard, com divergência demonstrada;
- **Adicional:** objeto encontrado na instalação e não encontrado na referência consultada;
- **Ausente:** objeto presente na referência e não encontrado na instalação;
- **Por determinar:** evidência, cobertura, versão ou política insuficientes.

Estas categorias são uma orientação funcional inicial, não uma política automática já aprovada para todos os ObjectKinds.

Não classificar automaticamente um objeto como desenvolvimento de cliente apenas por não existir na CS Default. Diferenças podem resultar, por exemplo, de versão, build, módulos, configuração, âmbito de recolha ou limitações da captura.

## 4. Scanner Knowledge Audit — prioridade imediata

Antes de alargar a implementação do portal ou a inteligência de identidade, validar se o Scanner recolhe a informação necessária, com completude, integridade, contexto e relações suficientes.

A auditoria parte do catálogo e dos collectors existentes. Não se deve presumir que uma capacidade está validada apenas por constar no catálogo ou por existir um collector.

### 4.1 Tipos de informação a considerar

A validação deverá cobrir os tipos/operações atualmente declarados pelo Scanner, incluindo, conforme aplicável e existente nas bases de teste:

- inventário da base de dados e tabelas;
- chaves primárias e estrangeiras;
- views, stored procedures e triggers SQL Server;
- regras de utilizador PHC;
- regras de utilizador PHC Web;
- teclas de utilizador;
- valores por defeito;
- valores por defeito PHC Web;
- eventos PHC;
- eventos PHC Web;
- campos físicos de utilizador (`u_*`);
- scripts Web;
- JavaScript de utilizador;
- CSS/JS Web;
- grelhas e objetos ocultos (`formc`);
- ecrãs personalizados (`formb`);
- funções T-SQL de utilizador e funções instaladas associadas;
- relações declaradas PHC em REL e DTL.

O inventário exato deve continuar a ser o catálogo implementado no Scanner e a matriz Collector / Operation / ObjectKind versionada. A lista acima é uma descrição funcional, não substitui essa matriz.

Pontos a esclarecer durante a auditoria:

- se os campos de utilizador físicos são suficientes ou se também são necessários metadados funcionais de PHC, nomeadamente de `dbo.campos`;
- o âmbito real das grelhas recolhidas em `formc`;
- a completude dos conteúdos e associações de `formb`, eventos, regras e scripts;
- a recolha de triggers SQL, incluindo objetos encriptados e o processo de desencriptação disponível;
- a cobertura de desenvolvimento PHC Web e a ligação entre eventos, regras, scripts, JavaScript e ecrãs;
- a distinção entre stored procedures, funções, views e outros objetos SQL;
- a qualidade das relações e referências entre os objetos recolhidos.

A menção a views corresponde a objetos SQL Server. Outros termos ou mecanismos PHC ainda ambíguos deverão ser clarificados antes de serem convertidos em requisitos de recolha.

### 4.2 Método de validação: um exemplo real por tipo/operação

A unidade inicial de validação será cada tipo/operação atualmente declarado pelo Scanner, não apenas um pequeno conjunto de exemplos escolhidos informalmente.

Para cada tipo/operação:

1. escolher uma base de testes onde exista um exemplo conhecido;
2. identificar o objeto concreto (nome, ecrã, tabela ou outro contexto);
3. registar o resultado esperado com base no conhecimento do utilizador e/ou na origem PHC/SQL;
4. executar o Scanner e preservar a captura e os artefactos;
5. localizar o resultado correspondente;
6. avaliar existência, completude, integridade e relações/contexto;
7. registar o resultado e a evidência;
8. classificar como validado, parcialmente validado, falhado ou não testado.

Se um tipo não existir na base escolhida, isso não constitui uma falha do Scanner: fica por testar e procura-se outro exemplo noutra base. Não se deve inventar um resultado esperado nem assumir que uma operação opcional foi validada por ter zero resultados.

### 4.3 Critérios de qualidade

Cada caso deve avaliar, quando aplicável:

- **Existência:** o objeto foi encontrado?
- **Completude:** foram recolhidos os campos e conteúdos relevantes?
- **Integridade:** o conteúdo recolhido corresponde à origem?
- **Contexto e relações:** sabemos a que ecrã, evento, tabela, função ou outro objeto está associado?
- **Proveniência e estado:** sabemos de onde veio, se foi recolhido, e se está disponível, vazio, parcial ou indisponível?

Após validação humana inicial, os casos estáveis deverão ser transformados em testes automatizados de regressão, para reduzir a necessidade de verificações manuais repetidas.

## 5. Auditoria diferencial: PHC Default vs. personalizado

Em paralelo com a auditoria de cobertura, comparar exemplos equivalentes para descobrir padrões que permitam distinguir o standard das personalizações.

O primeiro exemplo proposto pelo utilizador é:

- um ecrã original/default do PHC;
- um ecrã personalizado cuja natureza seja conhecida.

A análise deverá comparar, conforme os dados disponíveis:

- estrutura e campos;
- grelhas visíveis e ocultas;
- eventos e regras;
- código e expressões;
- JavaScript, scripts e CSS;
- associações a tabelas, campos e outros objetos;
- diferenças de comportamento que possam ser demonstradas.

As comparações devem privilegiar a mesma versão/build, módulos e contexto funcional. Diferenças observadas não devem ser automaticamente classificadas como desenvolvimento: podem resultar de versão, módulos, configuração ou âmbito diferente.

Os padrões descobertos poderão ser aplicados progressivamente a outros tipos de objetos, como campos, eventos, regras, triggers, funções e estruturas SQL.

Esta auditoria não deve, por si só, originar novas regras automáticas de identidade ou classificação. Primeiro recolhe-se evidência e documentam-se os padrões.

## 6. Automação e esforço de validação

A validação deve aproveitar as bases de dados de teste já existentes no ambiente do utilizador, sempre que sejam adequadas e seguras.

Pretende-se construir progressivamente um processo repetível:

- lista de casos e resultados esperados (manifesto de validação);
- execução do Scanner;
- análise automática dos artefactos e metadados;
- comparação com os resultados esperados;
- relatório de aprovação, falha, cobertura parcial ou ausência de teste;
- testes de regressão para casos já confirmados.

A validação inicial requer confirmação do utilizador sobre os resultados esperados. A automação reduz o esforço futuro, mas não substitui essa confirmação nem pode declarar correto um resultado sem uma referência fiável.

As experiências devem decorrer em bases de teste ou cópias controladas, nunca através de alterações não autorizadas em produção.

## 7. Sequência de trabalho proposta

1. **Preparação e auditoria estática:** rever o estado atual do repositório, catálogo, collectors, matriz e protocolo de validação.
2. **Scanner Knowledge Audit:** selecionar um exemplo por cada operação/tipo declarado e validar os resultados reais.
3. **Auditoria diferencial:** comparar exemplos PHC Default e personalizados, começando por ecrãs equivalentes.
4. **Fecho das lacunas:** distinguir problemas de recolha, integridade, interpretação, relações e cobertura; propor alterações apenas com evidência.
5. **PHC Standard Knowledge:** consolidar e persistir a referência standard, com versão e proveniência.
6. **Installation Knowledge:** importar e persistir uma captura ativa de produção por instalação.
7. **Comparação e portal:** apresentar diferenças fundamentadas, fichas de conhecimento e pesquisa global.
8. **Evolução posterior:** histórico, múltiplas capturas, comparação temporal, identidade/correspondência avançadas e RAG.

A preparação da experiência do portal pode decorrer em paralelo quando não depender de dados ainda não validados.

## 8. Trabalho anterior: preservar, não descartar

O trabalho já realizado continua a ser relevante, nomeadamente:

- IKP e identificação de capturas;
- proveniência;
- Object Index;
- contratos de identidade e fingerprints;
- matriz Collector / Operation / ObjectKind;
- evidência e relatórios de T-A e T-B;
- decisões D-023 e contratos relacionados.

A prioridade de implementação muda, mas não se deve remover ou alterar estes contratos sem análise de impacto e decisão explícita.

T-C e os restantes testes de ciclo de vida ficam adiados enquanto se valida a fundação do conhecimento e a utilidade da captura.

## 9. Fora do âmbito imediato e salvaguardas

Esta direção não autoriza, por si só:

- alterações a collectors, queries ou ao comportamento do Scanner;
- alterações ao workspace SQLite ou a schemas executáveis IKP;
- alterações a migrations ou à materialização SQL Server;
- implementação de matching automático ou classificação universal;
- implementação de histórico/múltiplas capturas;
- implementação de RAG;
- alterações ou mutações nas bases de dados de teste sem aprovação específica.

Qualquer lacuna identificada deverá ser documentada com exemplo, origem, resultado esperado, resultado observado e impacto. Só depois será apresentada uma proposta de correção para aprovação.

O conhecimento específico de uma instalação continua sujeito a proveniência, permissões e governação. A existência de uma correspondência com PHC Standard não torna código ou informação de cliente automaticamente partilhável ou reutilizável por outras instalações.

## 10. Decisões e pontos ainda por fechar

### Direção acordada

- Simplificar a primeira versão e priorizar valor utilizável.
- Usar uma CS Default identificada e versionada como referência standard inicial.
- Limitar inicialmente cada instalação a produção e uma captura ativa.
- Validar o Scanner por tipo/operação, com um exemplo real de cada capacidade declarada.
- Usar bases de teste existentes e conhecimento do utilizador para estabelecer resultados esperados.
- Automatizar progressivamente os casos validados.
- Comparar ecrãs PHC Default e personalizados para descobrir padrões de diferenciação.
- Adiar histórico, comparação temporal e inteligência avançada sem descartar os contratos existentes.

### A validar quando houver acesso ao computador

- Qual a base de teste adequada para cada tipo/operação.
- Quais os exemplos concretos e respetivos resultados esperados.
- Se a captura atual contém informação suficiente para cada caso.
- Qual o âmbito real e as limitações dos mecanismos PHC Web.
- Como são recolhidos e desencriptados os triggers protegidos.
- Quais os dois ecrãs equivalentes a usar no primeiro exercício diferencial.
- Se a referência CS Default disponível cobre os módulos e a versão pretendidos.

**Estado final:** direção de produto e plano de validação registados. A matriz operacional do Scanner continua dependente de validação com capturas reais. Nenhuma alteração de código ou de contratos executáveis foi aprovada por este documento.
