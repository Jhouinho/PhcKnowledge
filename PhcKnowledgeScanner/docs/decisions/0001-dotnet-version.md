# ADR 0001 ? .NET 10 LTS

- **Estado:** Aceite (substitu�do; era .NET 8 LTS na funda��o inicial)
- **Data original:** 2026-10-30
- **Atualizado:** 2026-10-30

## Contexto

� necess�rio um EXE de recolha t�cnica para Windows, com acesso a SQL Server e packing ZIP, independente do Scanner V1.

A funda��o do reposit�rio arrancou em **.NET 8 LTS** (SDK ent�o dispon�vel no ambiente). Antes de iniciar os collectors PHC, foi decidido alinhar a solu��o com **.NET 10 LTS**.

## Decis�o

Usar **.NET 10 (LTS)** e C# com `Nullable` habilitado.

`TargetFramework`: `net10.0`  
SDK pin em `global.json` (rollForward: `latestFeature`).

## Justifica��o

1. **LTS atual** ? .NET 10 � a linha LTS a utilizar para um projeto novo, com horizonte de suporte mais longo do que .NET 8.
2. **Momento certo** ? a migra��o ocorre antes dos collectors; evita churn de TFM/pacotes a meio da implementa��o de recolha.
3. **Ecossistema** ? tooling SQL Server, ZIP, console EXE e testes xUnit s�o suportados em `net10.0`.
4. **Suficiente para o host** ? console EXE; sem UI nesta fase.

## Alternativas consideradas

| Op��o | Motivo de rejei��o |
| --- | --- |
| Manter .NET 8 LTS | Funcional, mas n�o � a LTS alvo decidida para este projeto |
| .NET 9 | N�o LTS; sem vantagem face a .NET 10 para este horizonte |
| .NET Framework 4.x | Ciclo de vida legado; menos adequado a SDK-style moderno |
| Python / Node | Pior integra��o tipada com SQL Server e packing de EXE �nico no contexto atual |

## Consequ�ncias

- Todos os projetos da solu��o usam `net10.0`.
- Requer .NET SDK 10.x no ambiente de desenvolvimento e CI.
- Pacotes de teste s�o atualizados apenas se incompat�veis com `net10.0`.
