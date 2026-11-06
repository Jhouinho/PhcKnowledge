# Preparação do PHC Knowledge para RAG — decisão arquitetural

**Data:** 2026-11-06  
**Estado:** princípio estratégico aceite; não autoriza a implementação imediata de um motor RAG.

## 1. Decisão

O PHC Knowledge deve ser construído de forma a poder evoluir para um sistema RAG especializado em PHC, mas **não deve ser desenvolvido nesta fase como um produto RAG nem ficar dependente de uma tecnologia de IA específica**.

A prioridade é construir primeiro uma base de conhecimento estruturada, contextualizada por instalação, temporalmente consciente, rastreável e sustentada em evidências. O RAG será uma capacidade posterior que consome essa base.

> **Construir conhecimento fiável primeiro; acrescentar recuperação aumentada por IA depois. Preparar desde já a arquitetura para RAG, sem antecipar a sua implementação.**

## 2. Objetivo de longo prazo

Permitir que consultores e programadores façam perguntas técnicas sobre PHC e recebam respostas que cruzem fontes relevantes — estrutura SQL, personalizações, comportamento, documentação oficial, tickets e resoluções — sempre com contexto e evidências verificáveis.

O sistema deverá distinguir:
- factos recolhidos de interpretações automáticas;
- interpretações de conhecimento validado por pessoas;
- conhecimento PHC standard de informação específica de uma instalação;
- informação atual de observações históricas;
- evidência confirmada de hipótese ou associação por validar.

Uma resposta gerada por IA não passa a ser um facto só por ter sido produzida pelo modelo.

## 3. Porque é que o trabalho atual é uma fundação adequada

| Capacidade | Contributo futuro para RAG |
|---|---|
| Scanner autónomo | Recolha de evidências técnicas na origem |
| Object Index e identidades | Identificação dos objetos a que a informação diz respeito |
| Proveniência e coverage | Contexto sobre origem, âmbito e limites da recolha |
| Metadata e Definition Fingerprints | Deteção de alterações e validade temporal |
| Artefactos e hashes | Integridade e referência à evidência original |
| Relações e interpretações | Expansão contextual e navegação por dependências |
| IKP | Transporte consistente entre Scanner e plataforma |
| Capturas históricas | Comparação e recuperação de conhecimento no tempo |
| Anotações e validação humana | Incorporação controlada da experiência dos consultores |

Estas capacidades não constituem, por si só, um RAG. Criam a estrutura necessária para que a recuperação futura não dependa apenas da semelhança textual ou vetorial.

## 4. Princípios arquiteturais a preservar

### 4.1 Conhecimento independente da IA
O modelo de domínio, o Scanner, o IKP e o armazenamento de conhecimento não devem depender de um fornecedor, modelo LLM, biblioteca de embeddings ou base vetorial. Esses componentes devem poder ser substituídos sem reconstruir a base de conhecimento.

### 4.2 Evidência antes de resposta
Cada afirmação técnica deve poder remeter para a sua origem: instalação, captura, objeto, artefacto, documento, ticket ou anotação. Quando não existir evidência suficiente, o sistema deve declarar a lacuna.

### 4.3 Retrieval híbrido
Não assumir pesquisa vetorial como mecanismo único. A arquitetura futura deverá poder combinar:
- pesquisa lexical/exata para nomes, campos, código e identificadores;
- pesquisa semântica para documentação e descrições;
- consultas estruturadas para objetos, metadados, versões, estados e coverage;
- navegação relacional/grafo para dependências e contexto.

A estratégia concreta e as tecnologias serão escolhidas quando existirem dados e casos de avaliação representativos.

### 4.4 Contexto e isolamento
A recuperação deve respeitar sempre o âmbito: organização, instalação, ambiente, versão PHC e captura. Conhecimento específico de um cliente não pode aparecer como facto de outro cliente. Conhecimento generalizado só pode ser reutilizado após curadoria e promoção explícita.

### 4.5 Tempo e validade
Uma evidência tem contexto temporal. A resposta deve conseguir indicar em que captura foi observada e evitar apresentar como atual uma personalização que só existia numa captura antiga.

### 4.6 Separação entre factos e conhecimento derivado
Factos recolhidos, interpretações automáticas, projeções, anotações humanas e conhecimento geral permanecem distinguíveis. A IA pode sugerir relações ou explicações, mas não deve alterar silenciosamente os factos nem promover automaticamente conhecimento específico para geral.

### 4.7 Avaliação antes de confiança
Antes de disponibilizar respostas como apoio operacional, será necessário criar perguntas de referência, evidências esperadas e critérios para medir recuperação, correção, citações, isolamento entre instalações e tratamento de incerteza.

## 5. Arquitetura futura de referência

Fontes (Scanner, documentação PHC, código, tickets e resoluções)  
→ Ingestão e normalização com proveniência  
→ Base de conhecimento estruturada e histórica  
→ Retrieval híbrido (lexical, semântico, estruturado e relacional)  
→ Context Builder (instalação, versão, captura e permissões)  
→ LLM / motor de investigação substituível  
→ Resposta com evidências, referências e incerteza  
→ Validação e feedback humano  
→ Atualização controlada da base de conhecimento.

Esta é uma direção futura, não uma descrição de componentes já implementados. A base de conhecimento estruturada continua a ser útil mesmo sem a camada LLM.

## 6. Sequência de evolução

1. **Agora — Scanner e IKP fiáveis:** fechar P1.B, validar o object-index e produzir um pacote real íntegro.
2. **Plataforma central:** importar capturas, preservar histórico e garantir isolamento por instalação.
3. **Conhecimento consultável:** identidade lógica, comparação, relações, pesquisa textual/estruturada e anotações.
4. **Preparação de corpus:** incorporar documentação e outras fontes com metadados, direitos de utilização, versões e proveniência.
5. **RAG experimental:** implementar retrieval híbrido e respostas com citações num conjunto controlado de perguntas.
6. **Investigação assistida:** avaliação sistemática, feedback humano e expansão gradual dos casos de uso.

Não é necessário escolher já uma base vetorial, fornecedor LLM, modelo de embeddings ou framework RAG. Essas decisões devem resultar de requisitos medidos e experiências comparáveis.

## 7. Critérios para considerar a fundação pronta para RAG

Antes de avançar para um piloto RAG relevante, devemos conseguir:
- identificar sem ambiguidade a origem e o âmbito de cada evidência;
- filtrar corretamente por instalação, ambiente, versão e captura;
- preservar e consultar conteúdo original e respetiva integridade;
- distinguir ausência de informação de informação não recolhida;
- navegar entre objetos relacionados sem transformar correlação em causalidade;
- comparar capturas sem inferir remoções quando a coverage não o permite;
- apresentar referências que permitam verificar cada afirmação;
- manter conhecimento de clientes isolado e controlar a promoção de conhecimento geral;
- avaliar a qualidade da recuperação com um conjunto de perguntas e resultados esperados.

## 8. Relação com o roadmap

Esta decisão complementa, não substitui, o roadmap visual do PHC Knowledge de 2026-11-06. A Fase 4 — Investigação assistida — deve ser entendida como a futura camada RAG/investigação, construída sobre as fases anteriores.

O foco imediato mantém-se no P1.B e na validação do IKP com uma captura real. Não se deve desviar esforço para embeddings, chat UI ou integração LLM antes de a base de conhecimento suportar contexto, proveniência e evidência.

## 9. Regra de orientação para futuras decisões

Quando surgir uma proposta técnica, perguntar:
1. Ajuda a recolher, identificar, contextualizar, preservar, relacionar ou validar conhecimento?
2. Mantém proveniência, isolamento por instalação e distinção entre facto e inferência?
3. É independente do fornecedor/modelo de IA?
4. É necessária agora ou pertence à fase de retrieval/RAG?

Se uma proposta antecipar a camada RAG sem resolver as fundações, deve ser adiada ou tratada apenas como experiência isolada.

**Decisão resumida:** o PHC Knowledge é primeiro uma plataforma de conhecimento técnico fiável e reutilizável; a sua evolução natural poderá ser um sistema RAG híbrido, contextual e fundamentado em evidências.
