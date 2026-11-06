# PHC Knowledge — Orientações do projeto

Documento central de orientação. Linguagem de documentação: Português de Portugal (pt-PT).

## Objetivo

O PHC Knowledge é uma plataforma de conhecimento técnico sobre PHC, destinada a recolher, organizar, relacionar, pesquisar e reutilizar conhecimento.

Não é apenas um repositório de ficheiros, uma coleção de código, um clone da aplicação PHC, nem uma simples ferramenta de pesquisa SQL.

Pretende-se uma base de conhecimento estruturada, com proveniência, evidências, relações, dependências e contexto técnico — integrando conhecimento PHC standard com conhecimento específico de instalações e desenvolvimentos personalizados.

## Decisões de produto

### Utilização

- Interface web.
- Interface desktop.
- Possibilidade de utilização local.
- Possibilidade de utilização num servidor.

### Modelo de utilização

- Plataforma central partilhada por várias organizações.
- Cada organização terá os seus dados isolados.
- Um utilizador poderá pertencer a várias organizações.
- Vários consultores poderão trabalhar simultaneamente sobre a mesma instalação.
- As alterações e descobertas deverão ficar disponíveis aos restantes utilizadores autorizados.

### Conhecimento

- O conhecimento PHC standard deverá ser reutilizável entre organizações.
- O conhecimento específico de uma instalação deverá respeitar o isolamento da respetiva organização.
- Cada informação deverá preservar a sua proveniência.
- Distinguir factos confirmados, informação recolhida, inferências e conclusões ainda não validadas.

## Tecnologias candidatas

A arquitetura tecnológica **não está fechada**. Candidatas a avaliar numa etapa própria:

- .NET 10 e C# para o núcleo tecnológico.
- ASP.NET Core para os serviços e API.
- React e TypeScript para a interface web.
- WPF ou alternativa multiplataforma para a interface desktop.
- PostgreSQL como candidato à base de dados central.
- SQLite para armazenamento e utilização local.
- Pesquisa textual e eventual motor de pesquisa especializado.

Não instalar dependências nem criar projetos de produção até essa decisão ser tomada.

## Projetos anteriores

Projetos que poderão conter conhecimento, código ou componentes reutilizáveis:

- `PhcKnowledgeScanner` — Scanner, recolha de informação PHC, workspace local e exportação de snapshots.
- `ConsoleApp` — exploração de conhecimento e componentes associados.

Regras:

- Analisar numa etapa posterior, antes de reutilizar.
- Não copiar, mover, referenciar ou importar código automaticamente.
- Não recuperar arquiteturas, mecanismos ou modelos de processamento já abandonados.
- Aproveitar apenas o que demonstrar valor real para a nova solução.

## Estrutura do repositório

```text
PhcKnowledge/
├── README.md
├── PROJECT_GUIDELINES.md
├── .gitignore
├── docs/
│   └── architecture/
├── src/
├── tests/
└── tools/
```

Não criar pastas adicionais sem justificação.

## Regras de trabalho

### Desenvolvimento controlado

- Não implementar funcionalidades sem uma direção previamente validada.
- Não assumir decisões arquiteturais que ainda estejam em aberto.
- Não adicionar tecnologias ou dependências sem justificação.
- Privilegiar simplicidade, modularidade e capacidade de evolução.

### Aproveitamento de conhecimento anterior

- Analisar antes de reutilizar.
- Não transportar automaticamente código ou arquitetura antiga.
- Preservar conhecimento PHC standard já confirmado.
- Não confundir conhecimento standard com personalizações de clientes.

### Documentação

- Evitar documentação excessiva.
- Não criar documentos intermédios sem valor efetivo.
- No final de cada etapa, consolidar num relatório conciso: o que foi feito, implementado, validado, problemas encontrados, estado atual e próximo passo.
- Evitar múltiplos documentos que descrevam o mesmo assunto.

### Reflexão obrigatória

No final de cada fase:

- Avaliar o estado real do projeto.
- Identificar o que existe e funciona.
- Identificar o que falta.
- Registar o que aprendemos.
- Avaliar se a arquitetura continua adequada.
- Discutir o próximo passo com o responsável do projeto.

Não avançar automaticamente para a fase seguinte sem validação conjunta.

## Próxima etapa prevista

Analisar o que pode ser aproveitado dos projetos anteriores e definir a arquitetura tecnológica definitiva do PHC Knowledge.
