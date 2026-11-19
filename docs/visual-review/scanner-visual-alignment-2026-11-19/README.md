# Visual review — Scanner alignment (2026-11-19)

Revisão visual da modernização **Luminous Knowledge** do PHC Knowledge Scanner, alinhada ao Portal.

## Captura

| Campo | Valor |
| --- | --- |
| Data | 2026-11-19 |
| Branch | `feature/scanner-visual-alignment` |
| Commit da UI visualizada | working tree (implementação ainda **não** commitada); base `5711e7aa0bfda1f1ff8763e9512cd57335babe39` |
| Resolução | 1440×900 (janela da aplicação) |
| Dados | **Fixture demonstrativa sintética** em `%TEMP%\phc-scanner-visual-demo\workspace` (não é instalação/cliente real) |
| Método | Screenshots reais da app WPF em execução |

## Imagens

| Ficheiro | Conteúdo |
| --- | --- |
| [00-overview.png](./00-overview.png) | Visão geral — página Ligação com formulário DEMO |
| [01-connection.png](./01-connection.png) | Ligação — campos, botões secundário/primário/ghost, estado OK DEMO |
| [02-collection.png](./02-collection.png) | Recolha — progresso 100%, contadores, log DEMO |
| [03-dashboard.png](./03-dashboard.png) | Dashboard — KPIs, badge Danger («Com erros»), gráfico Activo vs Inactivo |
| [04-inventory.png](./04-inventory.png) | Inventário — sessão aberta, lista de tabelas, detalhe e relação seleccionada |
| [05-explorer.png](./05-explorer.png) | Explorador — filtros, categorias, objecto seleccionado, editor de código |
| [06-analysis.png](./06-analysis.png) | Análise — grelha com relações FK sintéticas |
| [07-export.png](./07-export.png) | Exportação — cartões, botões primários, checkbox |
| [08-contact-sheet.png](./08-contact-sheet.png) | Composição das sete páginas |
| [09-components.png](./09-components.png) | Detalhe de componentes (KPIs, badge semântico, cartões, gráficos) |

## Estados demonstrados

- Sidebar / navegação selected
- Cartões, superfícies, tipografia e accent steel (sem dourado)
- Formulário de ligação com valores DEMO
- Botões primário / secundário / ghost
- Progress bar preenchida
- KPIs e gráficos Activo/Inactivo
- Badge semântico **Danger** («Com erros») com avisos/erros na fixture
- Inventário: selected list item, tabs, relação de saída
- Explorador: accordion expandido, selected object, editor AvalonEdit com código
- Análise: DataGrid com linhas
- Exportação: checkbox e CTAs

## Limitações

- Não há ligação SQL real; a fixture foi injectada só para captura (gancho temporário revertido, não commitado).
- Hover/disabled de todos os controlos não foram filmados frame-a-frame; o primário «Iniciar recolha» aparece muted quando a app o desactiva por regras existentes.
- Badge Warning/OK puros não aparecem nesta fixture (há erros → badge Danger); o gráfico e os KPIs cobrem verde/vermelho semânticos.
- O caminho `%TEMP%\...` visível no Inventário é da fixture local, não de um workspace de cliente.
- A implementação visual (4 ficheiros Desktop) permanece **fora** deste commit de documentação.
