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
    .AddInfrastructure(connectionString)
    .AddApi();

var app = builder.Build();

await app.Services.InitializeDatabaseAsync(
    applyMigrations: app.Configuration.GetValue<bool>("Database:ApplyMigrations"),
    seed: app.Configuration.GetValue<bool>("Database:Seed"));
```

Por eso la Api referencia a Infrastructure. Las clases de Infrastructure son `internal`, así que los endpoints no pueden usarlas directamente; solo son públicas sus dos puertas de entrada (`AddInfrastructure` e `InitializeDatabaseAsync`) y las migraciones. Un test de arquitectura lo verifica, y otro impide que la Api use EF Core, Dapper o `SqlClient`.

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
- Un test de arquitectura exige que cada command y cada query tengan exactamente un handler.

Las queries tienen su propio contrato (`IQueryHandler<TQuery, TResponse>`) y un pipeline más corto: solo validación. No abren transacción ni pasan por la idempotencia, porque no cambian nada.

```
endpoint → IQueryHandler<GetStandingsQuery, PagedResult<StandingItem>>
  ValidationDecorator      página, tamaño (1–100), sortBy en lista blanca, dirección
  └ GetStandingsHandler    delega en el lector; traduce "no existe" a un error del dominio
     └ IStandingsReader     (Application) ← StandingsReader con Dapper (Infrastructure)
```

### Lectura con Dapper

Application define los lectores (`ITeamReader`, `IMatchReader`, `IStandingsReader`, `ITopScorerReader`) y los modelos de lectura (records como `TeamSummary` o `StandingItem`). Infrastructure los implementa con Dapper sobre su propia conexión (`SqlConnectionFactory`), sin pasar por el `DbContext`.

Todas las listas usan `PagedSql`, que arma dos consultas en un solo viaje a la base (`QueryMultipleAsync`):

```sql
WITH [Rows] AS (<consulta base del lector>)
SELECT COUNT(*) FROM [Rows] WHERE <filtros>;

WITH [Rows] AS (<consulta base del lector>)
SELECT <columnas> FROM [Rows] WHERE <filtros>
ORDER BY <columna de la lista blanca> ASC|DESC, <desempate>
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
```

- **Lista blanca en dos niveles.** El validador rechaza un `sortBy` que no esté en `XxxQuery.SortFields` (400 `Query.InvalidSortBy`). El lector traduce ese nombre a una columna con su propio diccionario y lanza una excepción si no la encuentra: el texto del usuario nunca se concatena en el SQL. Un test verifica que las dos listas coincidan.
- **Filtros siempre parametrizados**, y solo se agregan los que llegan (sin `@x IS NULL OR …`). En `LIKE`, los comodines del usuario (`%`, `_`, `[`) se escapan: buscar `50%` encuentra el texto literal.
- **Orden total:** cada lista agrega un desempate único (el Id o la posición). Sin eso, `OFFSET/FETCH` puede repetir o saltar filas entre páginas.
- **El offset es `long`:** con `pageNumber` muy grande, `(pageNumber - 1) * pageSize` desbordaría un `int` y SQL Server rechazaría el offset negativo.
- **La posición en la tabla** se calcula con `ROW_NUMBER()` dentro del CTE, antes del filtro por nombre: filtrar no renumera la tabla. **Los goleadores** usan `RANK()`: los empatados comparten posición y la siguiente se salta (1, 1, 3).

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

Tiene dos partes, una en cada capa:

| Parte | Capa | Qué hace |
|---|---|---|
| `IdempotencyKeyFilter` | Api | Filtro de endpoint aplicado al grupo `/api`. En los POST exige el header `Idempotency-Key` (400 `Idempotency.KeyRequired` si falta, está vacío o viene repetido; 400 `Idempotency.KeyTooLong` si supera 100 caracteres) y lo copia a `IdempotencyContext.Key`. Al volver, si la respuesta fue repetida, agrega el header `Idempotent-Replayed: true`. Los demás métodos pasan sin revisar. |
| `IdempotencyDecorator` | Application | La lógica: candado, huella, repetir o ejecutar y guardar. Solo actúa si `IdempotencyContext.Key` tiene valor. |

Como el filtro está en el grupo, ningún POST puede olvidarlo; un test recorre todos los POST registrados y verifica que rechacen el request sin clave.

`IdempotencyDecorator` corre **dentro** de la transacción:

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

## API REST

Minimal APIs agrupadas por recurso (`MapGroup`), todas bajo `/api`:

| Recurso | Endpoints |
|---|---|
| Equipos | `GET /teams`, `GET /teams/{teamId}`, `POST /teams`, `PUT /teams/{teamId}`, `DELETE /teams/{teamId}` |
| Jugadores | `GET /teams/{teamId}/players`, `POST /teams/{teamId}/players`, `PUT /teams/{teamId}/players/{playerId}`, `DELETE /teams/{teamId}/players/{playerId}` |
| Partidos | `GET /matches`, `GET /matches/{matchId}`, `POST /matches`, `PATCH /matches/{matchId}`, `DELETE /matches/{matchId}`, `POST /matches/{matchId}/start`, `/finish`, `/cancel` |
| Goles y resultado | `POST /matches/{matchId}/goals`, `POST /matches/{matchId}/goals/{goalId}/annul`, `PUT /matches/{matchId}/result` |
| Reportes | `GET /standings`, `GET /top-scorers` |

Cada endpoint hace tres cosas y nada más: arma el command o la query con lo que llega por la ruta, el body y la query string; pide el handler por inyección, y traduce el `Result`:

| `Result` | HTTP |
|---|---|
| Éxito de una creación | 201 Created, `Location` y `{ "id": "…" }` |
| Éxito sin respuesta | 204 No Content |
| Éxito de una lectura | 200 OK con el modelo de lectura |
| `ErrorType.Validation` / `NotFound` / `Conflict` / `Failure` | 400 / 404 / 409 / 500 como ProblemDetails |

**ProblemDetails (RFC 9457).** Todo error sale con el mismo formato, venga de un `Result`, de una excepción o de una ruta inexistente:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "El dorsal 10 ya pertenece a otro jugador del equipo.",
  "instance": "/api/teams/…/players",
  "errorCode": "Player.ShirtNumberTaken",
  "metadata": { "shirtNumber": 10 },
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

- `ProblemMapping` convierte un `Error` en ProblemDetails; `ProblemDetailsEnricher` (el `CustomizeProblemDetails` de ASP.NET Core) agrega a **todas** las respuestas de error el `instance` y el `traceId`.
- El `traceId` son los 32 caracteres del trace W3C, el mismo valor que se guarda en la tabla `DomainEvents`. Si el cliente manda un header `traceparent`, se respeta.
- `GlobalExceptionHandler` (`IExceptionHandler`): un body o un parámetro mal formado (JSON inválido, `"diez"` en un número) es 400 `Request.Malformed`; cualquier otra excepción es 500 `Server.Unexpected`, sin el mensaje de la excepción. `RouteHandlerOptions.ThrowOnBadRequest = true` hace que los errores de binding también pasen por ahí, en todos los entornos.

## Base de datos al iniciar

`InitializeDatabaseAsync` aplica las migraciones (`Database:ApplyMigrations`) y carga el seed (`Database:Seed`). Las dos opciones están apagadas en `appsettings.json` y encendidas en Development.

El seed (`DatabaseSeeder`) **no inserta filas a mano**: envía los mismos commands que la API (crear equipos y jugadores, programar, iniciar, registrar goles, anular, finalizar, cancelar), cada uno en su propio scope, como si fueran requests. Pasan por validación, transacción y eventos, así que la tabla de posiciones se arma sola y ninguna regla del dominio se salta. Solo corre si la base no tiene equipos.

## Persistencia

- **Fluent API** en `Infrastructure/Persistence/Configurations`, un archivo por entidad. El dominio no tiene atributos de EF Core.
- **Colecciones encapsuladas:** EF Core lee y escribe los campos privados (`_players`, `_goals`, `_corrections`); las propiedades públicas son de solo lectura.
- **Borrado lógico** con filtro global (`HasQueryFilter`) en las raíces `Team` y `Match`. `IgnoreQueryFilters()` se usa solo para el `DELETE` idempotente.
- **Índices únicos filtrados** (`WHERE [IsDeleted] = 0`): nombre de equipo y dorsal por equipo. Un nombre o dorsal liberado por un borrado se puede reutilizar.
- **Choques con índices únicos** (errores 2601/2627 de SQL Server): `UniqueIndexViolation` los traduce a `Team.NameTaken` o `Player.ShirtNumberTaken`. Son la red de seguridad para dos requests simultáneos que pasan juntos la verificación del handler.
- **Migraciones** con `dotnet-ef` como herramienta local (`dotnet tool restore`). Se aplican al iniciar la Api en Development y al iniciar la base de los tests de integración, así que un modelo sin migración rompe los tests.

## Flujo de un request

### Escritura: `POST /api/matches/{id}/goals`

```
HTTP
 → [Api] manejador de excepciones → (JWT, #4) → binding del body → filtro Idempotency-Key → endpoint
 → [Application] ICommandHandler<RegisterGoalCommand, Guid> (ya decorado, por inyección)
     Logging → Validation → Transaction (BEGIN) → Idempotency (candado + clave)
     → RegisterGoalHandler
         → IMatchRepository.GetByIdAsync, ITeamRepository (plantel de los dos equipos)
         → match.AddGoal(...)             [Domain] valida la regla y genera GoalScored
     ← Idempotency agrega la fila de la clave
     → Transaction: IUnitOfWork.CommitAsync
 → [Infrastructure] despacha los eventos (proyección + tabla DomainEvents), SaveChanges, COMMIT
 ← Result → [Api] 201 Created / 400 / 404 / 409 (+ Idempotent-Replayed si fue una repetición)
```

### Lectura: `GET /api/standings`

```
HTTP
 → [Api] manejador de excepciones → binding de la query string (PageParameters) → endpoint
 → [Application] IQueryHandler<GetStandingsQuery, …>: Validation → GetStandingsHandler → IStandingsReader
 → [Infrastructure] StandingsReader: Dapper lee la proyección TeamStandings con JOIN Teams,
                    calcula la posición con ROW_NUMBER, filtra, ordena y pagina en SQL
 ← PagedResult<StandingItem> → [Api] 200 OK
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
| `Api_DoesNotAccessTheDatabaseDirectly` | La Api no usa EF Core, Dapper ni `SqlClient`: todo pasa por los handlers |
| `Infrastructure_EveryTypeExceptItsEntryPointsAndTheMigrations_IsInternal` | Infrastructure solo expone `DependencyInjection`, `DatabaseInitializer` y las migraciones |
| `Application_HandlersOfTheContract_AreInternal` y `Application_Validators_AreInternal` | Handlers de comandos, de queries, de eventos y validadores son `internal` |
| `EveryCommand_HasExactlyOneHandler` y `EveryQuery_HasExactlyOneHandler` | Ningún command ni query queda sin handler (fallaría en tiempo de ejecución) ni con dos |
| `AppSettingsFile_IsValidJson` | Los `appsettings*.json` de la Api se pueden leer; los tests de integración corren en otro entorno y no cargan `appsettings.Development.json` |

Cada regla que filtra tipos verifica primero que el filtro seleccione los tipos esperados (**anti-vacío**). Sin esa comprobación, un filtro mal escrito no selecciona nada y la regla pasa sin revisar ningún tipo.

Las reglas se revisan en dos niveles:

- **El código compilado** (NetArchTest): detecta el **uso real** de un tipo prohibido.
- **Los `.csproj`** (`ProjectReferenceTests`): el compilador elimina las referencias que no se usan, así que una referencia nueva todavía sin usar no aparece en el código compilado. Leer el `.csproj` la detecta el mismo día en que se agrega, antes de que alguien empiece a usarla.
