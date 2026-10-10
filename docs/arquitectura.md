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
    Task BeginTransactionAsync(CancellationToken cancellationToken);

    Task<Result> CommitAsync(CancellationToken cancellationToken);

    Task RollbackAsync(CancellationToken cancellationToken);
}

// FixtureHub.Infrastructure
internal sealed partial class UnitOfWork(FixtureHubDbContext context, ...) : IUnitOfWork { ... }
```

En tiempo de compilación, Infrastructure depende de Application. En tiempo de ejecución, Application invoca código de Infrastructure. La dependencia de código va en sentido contrario al flujo de control: ese es el principio de inversión de dependencias (la *D* de SOLID).

## Composition root

`Program.cs` es el único lugar donde se conectan las interfaces con sus implementaciones:

```csharp
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration.GetConnectionString("FixtureHub")!);
```

Por eso la Api referencia a Infrastructure. Las clases de Infrastructure son `internal`, así que los endpoints no pueden usarlas directamente; solo son públicas la extensión de registro y las migraciones. Un test de arquitectura lo verifica.

## CQRS

Las escrituras y las lecturas siguen caminos distintos:

| | Commands (escritura) | Queries (lectura) |
|---|---|---|
| Acceso a datos | EF Core | Dapper |
| Pasa por el dominio | Sí: las entidades validan sus reglas | No: proyecta directamente a DTOs |
| Transacción | Unit of Work con commit explícito | No aplica |
| Paginación | No aplica | En SQL (`OFFSET/FETCH`), nunca en memoria |

Los handlers implementan interfaces propias (`ICommandHandler<TCommand>` e `ICommandHandler<TCommand, TResponse>`) y **no hay dispatcher**: el endpoint recibe por inyección el handler ya envuelto por los decorators. [Scrutor](https://github.com/khellang/Scrutor) registra los handlers por convención y aplica los decorators con `Decorate`; el último que se registra queda más afuera.

```
endpoint → ICommandHandler<CreateTeamCommand, Guid>
  LoggingDecorator          nombre del comando, duración y resultado (nunca su contenido)
  └ ValidationDecorator     FluentValidation; si falla, 400 sin abrir la transacción
     └ TransactionDecorator  BEGIN → … → COMMIT, o ROLLBACK si algo falla
        └ IdempotencyDecorator  candado por clave, repite la respuesta guardada
           └ CreateTeamHandler  dominio + repositorios; nunca hace commit
```

- **El commit lo hace `TransactionDecorator`, nunca el handler.** Así la idempotencia guarda la respuesta en la misma transacción que los cambios, y ningún handler puede olvidar cerrarla.
- Los handlers y validadores son `internal`: nadie puede inyectarlos directo y saltarse el pipeline. Scrutor los registra como servicios con clave y solo expone la cadena decorada.
- Un test de arquitectura exige que cada command tenga exactamente un handler.

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

### Despacho de eventos de dominio

Los agregados acumulan sus eventos en memoria (`Raise`). Al confirmar, el Unit of Work:

1. Busca en el `ChangeTracker` de EF Core los agregados con eventos pendientes y los vacía.
2. Por cada evento, agrega una fila a la tabla `DomainEvents` (tipo, JSON, fecha y TraceId del request) y lo entrega a `IDomainEventDispatcher`, que llama a todos los `IDomainEventHandler<TEvento>` registrados.
3. Repite mientras aparezcan eventos nuevos (un handler podría generar otros).
4. Llama a `SaveChanges` y confirma la transacción: el cambio, la proyección y el registro de eventos se guardan juntos o no se guarda nada.

Los handlers de eventos viven en Application (`Standings/`) y Scrutor los registra igual que los de comandos. Un evento sin handlers (por ejemplo, `GoalScored`) solo queda registrado en `DomainEvents`.

## Idempotencia

`IdempotencyDecorator` actúa solo si el request trae una clave (`IdempotencyContext.Key`, que llena el filtro del endpoint en los POST). Corre **dentro** de la transacción:

```
BEGIN TRANSACTION
  sp_getapplock 'idempotency:{clave}' (LockOwner = Transaction, espera hasta 10 s)
  ¿existe la clave y no venció?   misma huella → devuelve el valor guardado, sin ejecutar
                                  otra huella  → 409 Idempotency.KeyReused
  si no → ejecuta el handler → si tuvo éxito, agrega la fila de la clave
COMMIT  (la clave y los cambios del comando se confirman juntos; el candado se libera solo)
```

- **Huella:** SHA-256 del nombre completo del comando y su JSON. Distingue "el mismo pedido repetido" de "la clave reusada con otro contenido".
- **Se guarda el valor del `Result`** (por ejemplo, el Id creado), no la respuesta HTTP, y solo de los éxitos. Un fallo hizo rollback, así que repetirlo es seguro.
- **Mismo pedido en paralelo:** el segundo espera en el candado a que el primero termine y recibe su respuesta. Si la espera supera 10 s, 409 `Idempotency.RequestInProgress`.
- Las claves expiran a las 24 horas; una clave vencida se reemplaza. La tabla tiene un índice por `ExpiresAt` para una limpieza periódica.

## Persistencia

- **Fluent API** en `Infrastructure/Persistence/Configurations`, un archivo por entidad. El dominio no tiene atributos de EF Core.
- **Colecciones encapsuladas:** EF Core lee y escribe los campos privados (`_players`, `_goals`, `_corrections`); las propiedades públicas son de solo lectura.
- **Borrado lógico** con filtro global (`HasQueryFilter`) en las raíces `Team` y `Match`. `IgnoreQueryFilters()` se usa solo para el `DELETE` idempotente.
- **Índices únicos filtrados** (`WHERE [IsDeleted] = 0`): nombre de equipo y dorsal por equipo. Un nombre o dorsal liberado por un borrado se puede reutilizar.
- **Choques con índices únicos** (errores 2601/2627 de SQL Server): `UniqueIndexViolation` los traduce a `Team.NameTaken` o `Player.ShirtNumberTaken`. Son la red de seguridad para dos requests simultáneos que pasan juntos la verificación del handler.
- **Migraciones** con `dotnet-ef` como herramienta local (`dotnet tool restore`). Se aplican al iniciar la base de los tests de integración, así que un modelo sin migración rompe los tests.

## Flujo de un request

### Escritura: `POST /api/matches/{id}/goals`

```
HTTP
 → [Api] middleware CorrelationId/TraceId → autenticación JWT → filtro Idempotency-Key → endpoint
 → [Application] ICommandHandler<RegisterGoalCommand, Guid> (ya decorado, por inyección)
     Logging → Validation → Transaction (BEGIN) → Idempotency (candado + clave)
     → RegisterGoalHandler
         → IMatchRepository.GetByIdAsync, ITeamRepository (plantel de los dos equipos)
         → match.AddGoal(...)             [Domain] valida la regla y genera GoalScored
     ← Idempotency agrega la fila de la clave
     → Transaction: IUnitOfWork.CommitAsync
 → [Infrastructure] despacha los eventos (proyección + tabla DomainEvents), SaveChanges, COMMIT
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
| `Infrastructure_EveryTypeExceptTheRegistrationAndTheMigrations_IsInternal` | Infrastructure solo expone `DependencyInjection` y las migraciones |
| `Application_HandlersOfTheContract_AreInternal` y `Application_Validators_AreInternal` | Handlers de comandos, de eventos y validadores son `internal` |
| `EveryCommand_HasExactlyOneHandler` | Ningún command queda sin handler (fallaría en tiempo de ejecución) ni con dos |

Cada regla que filtra tipos verifica primero que el filtro seleccione los tipos esperados (**anti-vacío**). Sin esa comprobación, un filtro mal escrito no selecciona nada y la regla pasa sin revisar ningún tipo.

Las reglas se revisan en dos niveles:

- **El código compilado** (NetArchTest): detecta el **uso real** de un tipo prohibido.
- **Los `.csproj`** (`ProjectReferenceTests`): el compilador elimina las referencias que no se usan, así que una referencia nueva todavía sin usar no aparece en el código compilado. Leer el `.csproj` la detecta el mismo día en que se agrega, antes de que alguien empiece a usarla.
