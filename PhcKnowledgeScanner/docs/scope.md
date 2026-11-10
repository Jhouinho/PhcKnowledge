# Âmbito funcional previsto

Lista **orientadora**, não especificação fechada. Para cada categoria: intenção, fontes candidatas e limitações conhecidas *a priori*. Fontes concretas serão confirmadas com o pack offline / dicionário PHC e com instalações reais — **sem inventar tabelas ou FKs**.

| # | Categoria | Fontes candidatas (a validar) | Limitações técnicas previstas |
| --- | --- | --- | --- |
| 1 | Estrutura SQL Server | Catálogos sistema (`sys.*` / `INFORMATION_SCHEMA`), metadados de objetos | Permissões; objetos encriptados; cross-DB; naming por instalação |
| 2 | Dicionário de dados PHC | Tabelas de dicionário (ex. export `dbo.dic` no pack de conhecimento) | Diferenças por versão; custom fields; inconsistências históricas |
| 3 | Personalizações | Tabelas/objetos de customização; código utilizador | Volume; mistura standard vs custom; identificação fiável |
| 4 | Consultas e parâmetros | Definições de consultas / análises no ERP | Formatos proprietários; parâmetros opcionais ausentes |
| 5 | Código XBase e SQL | Campos de código, scripts, triggers de aplicação | Encoding; tamanho; código gerado vs manual |
| 6 | Ecrãs, componentes, eventos, regras | Metadados de UI / regras | Dependência forte da versão CS; UI Web vs Desktop |
| 7 | Views, funções, SPs, triggers SQL | Definições SQL Server | Definicões `NULL` / encriptadas; ordem de dependências |
| 8 | Configurações e parâmetros | Tabelas de parâmetros / ficheiros de config | Segredos; valores ambiente-específicos |
| 9 | Dependências entre objetos | Factos `dbo.rel`/`dbo.dtl` (collector `phc.declared-relations`) + FK SQL (`sys.foreign_keys`); inferência por código fica fora desta fase | Dependências dinâmicas / string-built SQL; parser REL/DTL determinístico V1.1 implementado, mas não cobre todas as expressões e referências |
| 10 | Ficheiros externos, relatórios, integrações | File system, Crystal/Reporting, configs de integração | Paths locais; acessos de rede; binários grandes |

## Fora de âmbito (fase atual e próximas imediatamente)

- Construção e publicação do **modelo de conhecimento central** (responsabilidade do PHC Knowledge)
- Alteração de bases PHC (DDL/DML de escrita) pelos collectors de aquisição
- Alteração do `ConsoleApp` / Scanner V1
- Edição/publicação controlada de objetos PHC: visão futura separada, não faz parte dos collectors read-only

## Critérios de sucesso da recolha (futuro)

Uma execução bem-sucedida deve permitir responder:

1. Que collectors correram e com que status?
2. Que objectos foram omitidos e porquê?
3. Os conteúdos exportados são byte-a-byte os originais (quando aplicável)?
4. O ZIP é reproduzível em estrutura (não necessariamente em timestamps)?


## Estado atual — 2026-11-10

A lista acima é orientadora e histórica, não um backlog de capacidades todas por iniciar. O Scanner já implementa recolha SQL/PHC, parser REL/DTL V1.1, projeções relacionais e exportação IKP 0.1. O estado por família e as limitações atuais devem ser confirmados no código/ADRs e na matriz `../../docs/architecture/ikp-0.1-scanner-compatibility-matrix.md`. A plataforma central de conhecimento permanece fora do Scanner.
