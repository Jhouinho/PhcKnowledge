# Decisões arquitecturais aprovadas — PHC Knowledge

- **Data de consolidação:** 21-11-2026
- **Repositório:** `Jhouinho/PhcKnowledge`
- **Estado:** decisões aprovadas pelo responsável do projecto
- **Natureza:** referência normativa actual para produto, domínio, captura, transporte e armazenamento
- **Implementação:** estas decisões orientam a documentação e o trabalho futuro; não significam que todas as capacidades já estejam implementadas

Este documento é a fonte de verdade das decisões aprovadas até esta data. Em caso de contradição, prevalece sobre propostas, modelos conceptuais e relatórios anteriores. Os documentos históricos devem ser preservados, mas identificados como históricos ou parcialmente substituídos.

## 1. Direcção de produto e organização

1. O PHC Knowledge é uma ferramenta interna da empresa que utiliza a plataforma, reservada aos seus colaboradores e com contas individuais.
2. Existe uma única organização no âmbito da plataforma: a empresa utilizadora. Não existe modelo SaaS multi-tenant nem várias organizações clientes.
3. Cada cliente PHC tem um registo próprio no Knowledge. O cliente é contexto técnico e fonte de conhecimento, não tenant nem organização de autenticação.
4. Cada base de dados PHC registada corresponde a uma instalação própria e tem o seu próprio `InstallationId`.
5. Ambiente e localização são inicialmente atributos da instalação; não exigem entidades autónomas nesta fase. Alterações relevantes podem ter histórico.
6. Uma instalação pode ter várias capturas; cada captura pertence exclusivamente a uma instalação.
7. Uma cópia de uma base de dados recebe uma nova identidade de instalação, mantendo uma relação de linhagem com a origem.
8. Uma mudança de servidor não cria necessariamente uma nova instalação, desde que seja possível confirmar a continuidade da mesma base de dados.
9. Comparações históricas respeitam a identidade da instalação e a cobertura efectiva das capturas.
10. A relação principal é: Cliente 1 → N Instalações 1 → N Capturas.
11. Capturas originam observações, artefactos, relações e histórico técnico, todos com proveniência.
12. Entidades centrais conceptuais: Cliente, Instalação, Captura, Objecto técnico/Observação, Artefacto e Proveniência. A Organização é contexto global da plataforma, não uma entidade de segregação por cliente.

## 2. Stack e armazenamento central

1. A base de dados central é **Microsoft SQL Server**.
2. PostgreSQL está excluído.
3. A edição e versão concretas do SQL Server continuam por decidir; não assumir SQL Server Express.
4. O armazenamento permanente de dados estruturados, metadados, histórico e artefactos originais é feito no SQL Server.
5. A decisão de usar SQL Server para os artefactos binários não aprova, por si só, um tipo de coluna, estratégia de particionamento ou dimensão máxima; esses detalhes serão especificados tecnicamente.
6. SQLite fica limitado ao workspace/cache local do Scanner.
7. O Scanner é autónomo e não depende do Knowledge em runtime. A transferência do pacote é manual através do Portal.

## 3. D-015 — PHC Standard e relações REL/DTL

**Estado: APROVADA.**

1. O PHC Standard é mantido no Knowledge num catálogo central, versionado e associado à edição/versão PHC aplicável.
2. O Scanner continua a recolher as relações declaradas REL/DTL existentes na instalação, incluindo a evidência original e a expressão de origem disponível.
3. REL/DTL são factos declarados pela instalação; não são reduzidos a chaves estrangeiras inferidas.
4. O Knowledge compara a evidência recolhida com a referência PHC Standard aplicável.
5. Correspondências, alterações e relações sem correspondência são classificadas com base em evidência. Uma ausência de correspondência não permite classificar automaticamente um elemento como personalizado ou standard.
6. A recolha REL/DTL não será removida antes de existir catálogo versionado e comparação suficientemente validados para preservar a detecção de desvios.
7. Factos originais e interpretações derivadas permanecem separados e rastreáveis.

## 4. D-016 — Política de captura e materialização

**Estado: APROVADA.**

1. O Scanner produz uma captura completa dentro do âmbito definido para cada operação/capability incluída na execução.
2. «Completa» não significa copiar indiscriminadamente toda a base de dados PHC ou todos os dados standard; significa executar integralmente o âmbito declarado e reportar a cobertura real.
3. O Scanner não consulta o histórico central para decidir o que recolher. Cada pacote é autónomo relativamente aos dados e evidências abrangidos pela captura.
4. O pacote transporta os dados recolhidos, originais disponíveis, metadados, identidade, proveniência, fingerprints, cobertura, avisos, erros e factos REL/DTL aplicáveis.
5. O Knowledge compara a captura com o histórico da mesma instalação e com o catálogo PHC Standard, materializando incrementalmente novas observações e versões.
6. Conteúdo inalterado pode ser deduplicado no armazenamento central, sem perder a proveniência das capturas em que foi observado.
7. A ausência de um objecto só pode originar uma conclusão de remoção quando a operação correspondente foi executada com sucesso e a cobertura daquele âmbito é comprovadamente completa.
8. Operações falhadas, não executadas, indisponíveis ou com cobertura parcial não permitem inferir remoções. A ausência de evidência não é evidência de ausência.
9. A captura e a sua cobertura são preservadas como histórico, mesmo quando o conteúdo idêntico é deduplicado.

## 5. ZIP de captura — apenas transporte temporário

**Estado: APROVADO.**

1. O ZIP é exclusivamente um meio de transporte entre o Scanner e o Portal/Knowledge.
2. O ZIP não é o arquivo permanente do conhecimento nem a forma de recuperação histórica da plataforma.
3. Após recepção, o Knowledge valida, analisa e prepara a materialização.
4. A informação estruturada, os metadados, o histórico e os artefactos originais necessários são persistidos no SQL Server.
5. O ZIP só pode ser eliminado depois de a materialização estar concluída e verificada.
6. A verificação tem de confirmar a persistência de todos os conteúdos esperados, integridade por hashes, referências, contagens, proveniência e capacidade de consulta/recuperação sem depender do ZIP.
7. Se a análise ou materialização falhar, ficar incompleta ou não puder ser verificada, o ZIP permanece disponível para retry e recuperação.
8. Depois da verificação bem-sucedida, o ZIP e os ficheiros temporários associados são eliminados.
9. A retenção actual do arquivo de recepção é transitória: não deve ser confundida com a arquitectura final. Não foi aprovada uma retenção fixa para pacotes rejeitados.

## 6. Separação de responsabilidades

| Componente | Responsabilidade |
|---|---|
| Scanner | Recolher o estado abrangido, preservar evidência, declarar cobertura e produzir pacote autónomo |
| IKP/ZIP | Transportar identidade, conteúdo, metadados, proveniência, fingerprints e cobertura |
| Knowledge | Validar, comparar, classificar, materializar, deduplicar e preservar histórico |
| SQL Server central | Armazenar permanentemente conhecimento estruturado, metadados, artefactos e histórico |

## 7. Estado de implementação e próximos passos

As decisões acima são aprovadas, mas a sua implementação não deve ser presumida.

Estado conhecido na data:
- Scanner e exportador IKP 0.1 existem.
- Recepção, validação, receipts e recuperação de pacotes existem.
- Análise IKP A+B está integrada na main.
- Materialização integral de observações, relações e artefactos no modelo de conhecimento ainda está por construir.
- O arquivo permanente actual do ZIP é uma solução transitória e não cumpre o ciclo de vida final aprovado.
- A edição/versão SQL Server continua por escolher.
- A matriz detalhada de classificação dos collectors ainda tem de ser produzida e validada com a captura real existente.

Próxima actividade: construir a matriz por collector/ObjectKind, identificando origem, conteúdo original/derivado, classificação (código, configuração, estrutura, metadado, diagnóstico ou relação), completude, fingerprint, proveniência e destino de materialização. Não alterar collectors nem workspace SQLite antes desta validação.

## 8. Governação documental

A cada decisão aprovada:
1. actualizar esta referência canónica;
2. actualizar todos os documentos activos afectados;
3. corrigir ou marcar explicitamente conteúdo contraditório como histórico/supersedido;
4. actualizar README, PROJECT_GUIDELINES e roadmap quando a orientação geral ou estado mudar;
5. preservar relatórios e artefactos históricos sem os reescrever retroactivamente;
6. verificar coerência e links entre documentos;
7. publicar as alterações numa branch de documentação. A main só é alterada após autorização explícita.

Não declarar builds, testes, materialização ou validações que não tenham sido efectivamente executados.
