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
| Frontend | Next.js 16 (App Router), React 19, TypeScript, Tailwind CSS 4 |
| Tests | xUnit, Testcontainers (SQL Server real), NetArchTest; Vitest en el frontend |
| Infraestructura | Docker (imágenes multi-etapa sin root) + docker-compose con healthchecks |

## Estructura del repositorio

```
fixture-hub/
├── src/
│   ├── FixtureHub.Domain/          Reglas del torneo: entidades, estados, eventos, Result
│   ├── FixtureHub.Application/     Casos de uso (commands/queries) y contratos
│   ├── FixtureHub.Infrastructure/  EF Core, Dapper, SQL Server, seed, usuarios y hash de contraseñas
│   └── FixtureHub.Api/             Endpoints HTTP, middlewares, JWT, logs y trazas, composition root, Dockerfile
├── frontend/                       Next.js: páginas, server actions, idiomas es/en, cliente de la Api y Dockerfile
├── tests/
│   ├── FixtureHub.UnitTests/          Dominio, handlers y decorators, con fakes
│   ├── FixtureHub.IntegrationTests/   Persistencia, lecturas e idempotencia contra SQL Server en Docker; la Api por HTTP; logs y trazas
│   └── FixtureHub.ArchitectureTests/  Reglas de dependencia, visibilidad y convenciones
├── docs/                           Arquitectura y decisiones de diseño
├── docker-compose.yml              SQL Server, Api, frontend y Jaeger
├── .env.example                    Variables de entorno con secretos para docker-compose (copiar como .env)
├── dotnet-tools.json               Herramientas locales (dotnet-ef)
├── Directory.Build.props           Configuración común (C# 14, nullable, warnings como errores)
├── global.json                     Versión fija del SDK de .NET
└── FixtureHub.slnx
```

## Cómo levantarlo

### Con Docker Compose

Requisitos: Docker Desktop (o Docker Engine con Compose v2) con al menos 4 GB de memoria para los contenedores; SQL Server pide 2 GB.

**La primera vez**, copia [`.env.example`](.env.example) como `.env` y completa los valores. Cada variable tiene un comentario con sus requisitos y un comando de PowerShell para generar un valor aleatorio:

```bash
cp .env.example .env
```

Después:

```bash
docker compose up -d --build --wait
```

El comando vuelve cuando todo está sano. La primera vez construye las imágenes (unos 3 minutos) y descarga SQL Server (unos 1,5 GB) si no está en la máquina. Los servicios arrancan en orden: SQL Server; después la Api, que aplica las migraciones, crea el administrador y carga el torneo de ejemplo; y al final el frontend.

| Servicio | Dirección | Qué es |
|---|---|---|
| Frontend | `http://localhost:3000` | El sitio. Para administrar, inicia sesión con `Admin__Username` y `Admin__Password` del `.env` |
| Api | `http://localhost:5065` | El mismo puerto que con `dotnet run`: [`FixtureHub.Api.http`](src/FixtureHub.Api/FixtureHub.Api.http) y Postman funcionan igual |
| Jaeger | `http://localhost:16686` | Las trazas, bajo el servicio `fixturehub-api` |
| SQL Server | `localhost,1433` (usuario `sa`) | Para revisar la base con SSMS o Azure Data Studio |

Los puertos solo se abren en `127.0.0.1`. Si alguno está ocupado (por ejemplo, por `dotnet run` o `npm run dev`), detén ese proceso antes de levantar.

| Comando | Para qué |
|---|---|
| `docker compose ps` | Estado y salud de cada servicio |
| `docker compose logs -f api` | Logs de la Api (JSON, una línea por evento) |
| `docker compose down` | Detiene y borra los contenedores; los datos quedan en el volumen |
| `docker compose down -v` | Borra también la base: la próxima vez se crea de nuevo con el seed |

La contraseña de `sa` se fija la primera vez que se crea el volumen de SQL Server: si la cambias después en el `.env`, borra el volumen con `docker compose down -v`.

### En desarrollo, sin Docker

En Windows con SQL Server LocalDB (viene con Visual Studio).

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

#### Frontend

Con la Api corriendo, en otra terminal (Node.js 24):

```bash
cd frontend
```

```bash
npm ci
```

```bash
npm run dev
```

Queda en `http://localhost:3000`. Las consultas son públicas; para administrar el torneo (equipos, jugadores, partidos y goles en vivo) se inicia sesión con el usuario y la contraseña de `Admin` configurados arriba. El selector **ES / EN** cambia el idioma.

El navegador nunca llama a la Api: las páginas y los formularios corren en el servidor de Next.js, que la llama en `http://localhost:5065`. Para cambiar la dirección o la zona horaria del torneo, copia [`frontend/.env.example`](frontend/.env.example) como `frontend/.env.local`:

| Variable | Por defecto | Uso |
|---|---|---|
| `API_BASE_URL` | `http://localhost:5065` | Dirección de la Api, leída al ejecutarse (en docker-compose, `http://api:8080`) |
| `TOURNAMENT_TIME_ZONE` | `America/El_Salvador` | Zona en la que se muestran y se capturan las fechas |

Para una versión de producción: `npm run build` y `npm start`.

#### Logs y trazas

Los logs salen por consola con [Serilog](https://serilog.net/): en Development como texto, y en cualquier otro entorno como JSON, una línea por evento (`Observability__JsonConsole=true` lo activa también en desarrollo). Cada request deja un log con método, ruta, status y duración, y cada log lleva el TraceId del request.

Para ver las trazas, levanta [Jaeger](https://www.jaegertracing.io/) en Docker:

```bash
docker run -d --name jaeger -p 16686:16686 -p 4317:4317 jaegertracing/jaeger:2.22.0
```

El perfil `http` ya apunta la Api a `http://localhost:4317` (`OTEL_EXPORTER_OTLP_ENDPOINT`). Las trazas aparecen en `http://localhost:16686` bajo el servicio `fixturehub-api`: el request HTTP, el command o la query y cada consulta SQL. Toda respuesta trae su TraceId en el header `X-Trace-Id`, para buscar la traza o los logs de ese request. Sin Jaeger, la Api funciona igual. Antes de levantar todo con Docker Compose, detén este contenedor (`docker stop jaeger`): usan los mismos puertos.

### La API en una mirada

| | |
|---|---|
| Lecturas | `GET /api/teams`, `/api/teams/{id}`, `/api/teams/{id}/players`, `/api/matches`, `/api/matches/{id}`, `/api/standings`, `/api/top-scorers`, con `pageNumber`, `pageSize` (máx. 100), `sortBy`, `sortDirection` y filtros |
| Escrituras | POST, PUT, PATCH y DELETE sobre equipos, jugadores, partidos y goles |
| Autenticación | `POST /api/auth/login` devuelve un JWT (60 minutos). Las lecturas son públicas; las escrituras exigen `Authorization: Bearer <token>` con el rol Admin (401 sin token válido, 403 sin el rol) |
| Idempotencia | Todo POST exige el header `Idempotency-Key`; repetir la misma petición devuelve la respuesta original con `Idempotent-Replayed: true` |
| Errores | ProblemDetails (RFC 9457) con `errorCode`, `metadata` y `traceId` |
| Observabilidad | Header `X-Trace-Id` en toda respuesta; `traceparent` (W3C) se respeta; trazas en Jaeger |
| Salud | `GET /health/live` (el proceso responde) y `GET /health/ready` (además llega a SQL Server), fuera de `/api` y sin token |

El detalle está en [docs/arquitectura.md](docs/arquitectura.md#api-rest).

## Cómo correr los tests

Requisitos: SDK de .NET 10 y Docker Desktop encendido (los tests de integración levantan un SQL Server 2022 con [Testcontainers](https://dotnet.testcontainers.org/) y prueban la Api por HTTP con `WebApplicationFactory`).

```bash
dotnet tool restore
dotnet test
```

La primera corrida descarga las imágenes de SQL Server (unos 1,5 GB) y de Jaeger (unos 175 MB; un test exporta una traza a un Jaeger real). Para correr solo los tests que no necesitan Docker:

```bash
dotnet test tests/FixtureHub.UnitTests
dotnet test tests/FixtureHub.ArchitectureTests
```

Los tests de arquitectura incluyen `FrontendCatalogTests`: por reflexión, cada código de error de la Api debe tener su traducción en los catálogos del frontend (`frontend/src/i18n/messages/es.json` y `en.json`).

En el frontend (desde `frontend/`):

| Comando | Qué hace |
|---|---|
| `npm test` | Tests unitarios con Vitest: traducción, catálogos, errores de la Api, idioma, fechas, paginación, redirecciones y token |
| `npm run lint` | ESLint con las reglas de Next.js y de React |
| `npm run typecheck` | TypeScript estricto, con los tipos de las rutas que genera Next.js |
| `npm run format:check` | Prettier |

## Documentación

- [Arquitectura](docs/arquitectura.md): capas, regla de dependencias, flujo de un request, observabilidad, frontend y Docker.
- [Decisiones de diseño](docs/decisiones.md): qué se decidió y por qué.
- [Guía de contribución](CONTRIBUTING.md): convención de commits, ramas y pull requests.
