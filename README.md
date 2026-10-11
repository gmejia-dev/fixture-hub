# FixtureHub

Sistema de gestión de torneos de fútbol: equipos, jugadores, calendario de partidos, registro de goles en vivo, tabla de posiciones y goleadores.

> **Estado:** en desarrollo. Este README se completa conforme avanzan las [issues](https://github.com/gmejia-dev/fixture-hub/issues).

## Stack

| Área | Tecnología |
|---|---|
| Backend | .NET 10, C# 14, ASP.NET Core Minimal APIs |
| Arquitectura | Clean Architecture, CQRS con handlers propios |
| Escritura | EF Core + Unit of Work |
| Lectura | Dapper |
| Base de datos | SQL Server |
| Seguridad | JWT (lecturas públicas, escrituras con rol Admin) |
| Observabilidad | Serilog, CorrelationId/TraceId, OpenTelemetry + Jaeger |
| Frontend | Next.js |
| Tests | xUnit, Testcontainers (SQL Server real), NetArchTest |
| Infraestructura | Docker + docker-compose |

## Estructura del repositorio

```
fixture-hub/
├── src/
│   ├── FixtureHub.Domain/          Reglas del torneo: entidades, estados, eventos, Result
│   ├── FixtureHub.Application/     Casos de uso (commands/queries) y contratos
│   ├── FixtureHub.Infrastructure/  EF Core, Dapper, SQL Server, seed, usuarios y hash de contraseñas
│   └── FixtureHub.Api/             Endpoints HTTP, middlewares, JWT, composition root
├── tests/
│   ├── FixtureHub.UnitTests/          Dominio, handlers y decorators, con fakes
│   ├── FixtureHub.IntegrationTests/   Persistencia, lecturas e idempotencia contra SQL Server en Docker; la Api por HTTP
│   └── FixtureHub.ArchitectureTests/  Reglas de dependencia, visibilidad y convenciones
├── docs/                           Arquitectura y decisiones de diseño
├── .env.example                    Variables de entorno con secretos (copiar como .env)
├── dotnet-tools.json               Herramientas locales (dotnet-ef)
├── Directory.Build.props           Configuración común (C# 14, nullable, warnings como errores)
├── global.json                     Versión fija del SDK de .NET
└── FixtureHub.slnx
```

## Cómo levantarlo

La forma definitiva será `docker compose up` ([#6](https://github.com/gmejia-dev/fixture-hub/issues/6)). Mientras tanto, en Windows con SQL Server LocalDB (viene con Visual Studio).

**La primera vez**, configura el administrador y la clave con la que se firman los JWT. Se guardan con [user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets), fuera del repositorio:

```bash
dotnet user-secrets set "Admin:Username" "admin" --project src/FixtureHub.Api
```

```bash
dotnet user-secrets set "Admin:Password" "<al menos 12 caracteres>" --project src/FixtureHub.Api
```

```bash
dotnet user-secrets set "Jwt:SigningKey" "<al menos 32 caracteres aleatorios>" --project src/FixtureHub.Api
```

Para generar una clave aleatoria en PowerShell:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

Después:

```bash
dotnet run --project src/FixtureHub.Api --launch-profile http
```

En Development, la Api aplica las migraciones, crea el administrador y carga un torneo de ejemplo (6 equipos, 42 jugadores y 15 partidos, uno en curso) si la base está vacía. Queda en `http://localhost:5065`; el documento OpenAPI está en `/openapi/v1.json` y [`FixtureHub.Api.http`](src/FixtureHub.Api/FixtureHub.Api.http) tiene un ejemplo de cada endpoint, empezando por el login. Si falta un ajuste, la Api no arranca y el mensaje dice cuál.

Fuera de Development, los mismos valores llegan como variables de entorno: `Admin__Username`, `Admin__Password` y `Jwt__SigningKey` (ver [`.env.example`](.env.example)).

Para usar otro SQL Server, sobrescribe la cadena de conexión con una variable de entorno:

```bash
ConnectionStrings__FixtureHub="Server=localhost,1433;Database=FixtureHub;User Id=sa;Password=<tu-clave>;TrustServerCertificate=True" dotnet run --project src/FixtureHub.Api --launch-profile http
```

### La API en una mirada

| | |
|---|---|
| Lecturas | `GET /api/teams`, `/api/teams/{id}`, `/api/teams/{id}/players`, `/api/matches`, `/api/matches/{id}`, `/api/standings`, `/api/top-scorers`, con `pageNumber`, `pageSize` (máx. 100), `sortBy`, `sortDirection` y filtros |
| Escrituras | POST, PUT, PATCH y DELETE sobre equipos, jugadores, partidos y goles |
| Autenticación | `POST /api/auth/login` devuelve un JWT (60 minutos). Las lecturas son públicas; las escrituras exigen `Authorization: Bearer <token>` con el rol Admin (401 sin token válido, 403 sin el rol) |
| Idempotencia | Todo POST exige el header `Idempotency-Key`; repetir la misma petición devuelve la respuesta original con `Idempotent-Replayed: true` |
| Errores | ProblemDetails (RFC 9457) con `errorCode`, `metadata` y `traceId` |

El detalle está en [docs/arquitectura.md](docs/arquitectura.md#api-rest).

## Cómo correr los tests

Requisitos: SDK de .NET 10 y Docker Desktop encendido (los tests de integración levantan un SQL Server 2022 con [Testcontainers](https://dotnet.testcontainers.org/) y prueban la Api por HTTP con `WebApplicationFactory`).

```bash
dotnet tool restore
dotnet test
```

La primera corrida descarga la imagen de SQL Server (unos 1,5 GB). Para correr solo los tests que no necesitan Docker:

```bash
dotnet test tests/FixtureHub.UnitTests
dotnet test tests/FixtureHub.ArchitectureTests
```

## Documentación

- [Arquitectura](docs/arquitectura.md): capas, regla de dependencias y flujo de un request.
- [Decisiones de diseño](docs/decisiones.md): qué se decidió y por qué.
- [Guía de contribución](CONTRIBUTING.md): convención de commits, ramas y pull requests.
