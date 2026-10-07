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
| `FixtureHub.Domain` | Reglas del torneo: entidades (`Team`, `Player`, `Match`, `Goal`), estados del partido, eventos de dominio, errores y `Result` | Nada |
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
 → [Infrastructure] Dapper + SQL con agregaciones y paginación en la base de datos
 ← PagedResult → [Api] 200 OK
```

## Verificación de las reglas

Las reglas de dependencia se verifican con tests de arquitectura: si una capa interna referencia a una externa, el test falla.
