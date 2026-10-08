# Arquitectura

FixtureHub sigue **Clean Architecture**: el código se organiza en capas concéntricas y las dependencias de código **siempre apuntan hacia adentro**, hacia las reglas de negocio. El dominio no conoce la base de datos, el framework web ni ninguna librería de infraestructura.

```
┌─────────────────────────────────────────┐
│  Api                                    │
│  ┌───────────────────────────────────┐  │
│  │  Infrastructure                   │  │
│  │  ┌─────────────────────────────┐  │  │
│  │  │  Application                │  │  │
│  │  │  ┌───────────────────────┐  │  │  │
│  │  │  │  Domain               │  │  │  │
│  │  │  └───────────────────────┘  │  │  │
│  │  └─────────────────────────────┘  │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

## Capas

| Proyecto | Responsabilidad | Depende de |
|---|---|---|
| `FixtureHub.Domain` | Reglas del torneo: entidades (`Team`, `Player`, `Match`, `Goal`), estados del partido, la fila de la tabla de posiciones (`TeamStanding`), eventos de dominio, errores y `Result` | Nada |
| `FixtureHub.Application` | Casos de uso: commands, queries, sus handlers y decorators. Define los **contratos** que necesita (`IUnitOfWork`, repositorios, lectores) sin implementarlos | Domain |
| `FixtureHub.Infrastructure` | Implementa los contratos: EF Core (escritura), Dapper (lectura), Unit of Work, idempotencia, seed, JWT | Application |
| `FixtureHub.Api` | Entrada HTTP con Minimal APIs, middlewares, filtros, mapeo de `Result` a HTTP y *composition root* | Application, Infrastructure |

### Qué no puede aparecer en cada capa

- **Domain:** EF Core (ni sus atributos), Dapper, ASP.NET Core, logging. El mapeo a tablas se hace con Fluent API en Infrastructure.
- **Application:** EF Core, Dapper, SQL Server, `HttpContext`. Solo conoce interfaces.
- **Api:** lógica de negocio. Recibe el request, lo despacha y traduce el resultado a HTTP.

## Inversión de dependencias

Application necesita persistir, pero no puede depender de Infrastructure. Por eso **Application define la interfaz e Infrastructure la implementa**:

```csharp
// FixtureHub.Application
public interface IUnitOfWork
{
    Task<Result> CommitAsync(CancellationToken ct);
}

// FixtureHub.Infrastructure
internal sealed class UnitOfWork(FixtureHubDbContext db) : IUnitOfWork { ... }
```

En tiempo de compilación, Infrastructure depende de Application. En tiempo de ejecución, Application invoca código de Infrastructure. La dependencia de código va en sentido contrario al flujo de control: ese es el principio de inversión de dependencias (la *D* de SOLID).

## Composition root

`Program.cs` es el único lugar donde se conectan las interfaces con sus implementaciones:

```csharp
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);
```

Por eso la Api referencia a Infrastructure. Las clases de Infrastructure son `internal`, así que los endpoints no pueden usarlas directamente; solo es pública la extensión de registro.

## CQRS

Las escrituras y las lecturas siguen caminos distintos:

| | Commands (escritura) | Queries (lectura) |
|---|---|---|
| Acceso a datos | EF Core | Dapper |
| Pasa por el dominio | Sí: las entidades validan sus reglas | No: proyecta directamente a DTOs |
| Transacción | Unit of Work con commit explícito | No aplica |
| Paginación | No aplica | En SQL (`OFFSET/FETCH`), nunca en memoria |

El despacho usa interfaces propias (`ICommandHandler`, `IQueryHandler`) y un dispatcher. Las preocupaciones transversales (validación, logging, transacción) se aplican con decorators.

### La tabla de posiciones es una proyección

La tabla no se calcula en cada consulta: es un **modelo de lectura** (tabla `TeamStandings`) que la escritura mantiene al día con los eventos de dominio, dentro de la misma transacción.

```
match.Finish()  ──▶  MatchFinished(local, visitante, 2, 1)
                         │   el Unit of Work despacha el evento antes del commit
                         ▼
     TeamStanding(local).RecordResult(2, 1)       → +3 puntos
     TeamStanding(visitante).RecordResult(1, 2)   → +0 puntos

GET /api/standings  ──▶  Dapper: ORDER BY Points DESC, GoalDifference DESC, GoalsFor DESC, Name
                                  OFFSET / FETCH
```

| Evento | Efecto en la tabla |
|---|---|
| `TeamCreated` | Crea la fila del equipo en cero, para que aparezca aunque no haya jugado |
| `MatchFinished` | `RecordResult` para cada equipo, con su marcador |
| `MatchResultCorrected` | `RevertResult` con el marcador anterior y `RecordResult` con el nuevo |

- **La regla de puntos vive una sola vez, en C#** (`TeamStanding`), y se probó con TDD. Dapper solo ordena y pagina; no calcula puntos.
- Puntos y diferencia de gol se guardan como columnas para poder ordenar e indexar en SQL sin repetir la fórmula.
- Como la tabla se actualiza en la **misma transacción** que el partido, no hay consistencia eventual: si algo falla, se revierte todo.
- Si la proyección se desincronizara (por ejemplo, por un cambio manual en la base), se puede reconstruir recorriendo los partidos `Finished`.
- Se descartó calcular la tabla con SQL en cada consulta: la regla de puntos habría quedado duplicada, en el SQL y en C#.

## Flujo de un request

### Escritura: `POST /api/matches/{id}/goals`

```
HTTP
 → [Api] middleware CorrelationId/TraceId → autenticación JWT → filtro Idempotency-Key → endpoint
 → [Application] dispatcher → decorators (validación, logging) → RegisterGoalHandler
     → IMatchRepository.GetByIdAsync
     → match.AddGoal(...)                 [Domain] valida la regla y genera GoalScored
     → IUnitOfWork.CommitAsync
 → [Infrastructure] transacción EF Core: guarda cambios, guarda la idempotency key,
                    registra y loggea los eventos de dominio, commit
 ← Result → [Api] 201 Created / 400 / 404 / 409
```

### Lectura: `GET /api/standings`

```
HTTP
 → [Api] middleware CorrelationId/TraceId → endpoint
 → [Application] GetStandingsHandler → IStandingsReader
 → [Infrastructure] Dapper lee la proyección TeamStandings, ordenada y paginada en SQL
 ← PagedResult → [Api] 200 OK
```

## Verificación de las reglas

Las reglas de dependencia se verifican con tests de arquitectura (`tests/FixtureHub.ArchitectureTests`, con [NetArchTest.eNhancedEdition](https://github.com/NeVeSpl/NetArchTest.eNhancedEdition)). Si una capa interna usa un tipo de una capa externa, el test falla y nombra el tipo que rompe la regla.

| Regla | Qué verifica |
|---|---|
| `Domain_DoesNotDependOnAnyOtherLayer` | Domain no usa nada de Application, Infrastructure ni Api |
| `Application_DependsOnlyOnDomain` | Application no usa nada de Infrastructure ni Api |
| `Infrastructure_DoesNotDependOnApi` | Infrastructure no usa nada de Api |
| `Domain_DoesNotDependOnDataAccessOrWebFrameworks` | Domain no usa EF Core, Dapper, ADO.NET, SQL Server, ASP.NET Core, logging, inyección de dependencias ni `System.Text.Json` |
| `Application_DoesNotDependOnDataAccessOrWebFrameworks` | Application no usa EF Core, Dapper, ADO.NET, SQL Server ni ASP.NET Core |
| `EachLayer_ReferencesExactlyTheProjectsItShould` | Cada `.csproj` referencia exactamente los proyectos que le corresponden |
| `Domain_DoesNotReferenceAnyNuGetPackage` | El `.csproj` de Domain no tiene ningún paquete NuGet |

Las reglas se revisan en dos niveles:

- **El código compilado** (NetArchTest): detecta el **uso real** de un tipo prohibido.
- **Los `.csproj`** (`ProjectReferenceTests`): el compilador elimina las referencias que no se usan, así que una referencia nueva todavía sin usar no aparece en el código compilado. Leer el `.csproj` la detecta el mismo día en que se agrega, antes de que alguien empiece a usarla.
