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
│   ├── FixtureHub.Infrastructure/  EF Core, Dapper, SQL Server, seed, JWT
│   └── FixtureHub.Api/             Endpoints HTTP, middlewares, composition root
├── tests/
│   ├── FixtureHub.UnitTests/          Dominio, handlers y decorators, con fakes
│   ├── FixtureHub.IntegrationTests/   Persistencia e idempotencia contra SQL Server en Docker
│   └── FixtureHub.ArchitectureTests/  Reglas de dependencia, visibilidad y convenciones
├── docs/                           Arquitectura y decisiones de diseño
├── dotnet-tools.json               Herramientas locales (dotnet-ef)
├── Directory.Build.props           Configuración común (C# 14, nullable, warnings como errores)
├── global.json                     Versión fija del SDK de .NET
└── FixtureHub.slnx
```

## Cómo levantarlo

_Pendiente: se documentará al completar la dockerización ([#6](https://github.com/gmejia-dev/fixture-hub/issues/6))._

Requisitos previstos: Docker Desktop con al menos 4 GB de RAM asignados.

## Cómo correr los tests

Requisitos: SDK de .NET 10 y Docker Desktop encendido (los tests de integración levantan un SQL Server 2022 con [Testcontainers](https://dotnet.testcontainers.org/)).

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
