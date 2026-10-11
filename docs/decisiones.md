# Decisiones de diseño

Supuestos y decisiones tomadas donde el enunciado deja margen de interpretación.

## Tecnología

| Decisión | Motivo |
|---|---|
| **.NET 10 y C# 14** | Versión LTS vigente. El SDK queda fijado en `global.json` y el lenguaje en `Directory.Build.props`. |
| **SQL Server** | Afinidad con el ecosistema .NET y dominio del motor. El enunciado no exige uno en particular. |
| **Minimal APIs** | Endpoints agrupados por recurso con *route groups*; la idempotencia se aplica como *endpoint filter*. |
| **Handlers propios para CQRS** (sin MediatR) | Pocas necesidades: enviar un command o una query a su handler. MediatR requiere licencia desde la v13. Las interfaces son propias de Application, así que la decisión es reversible sin tocar los handlers. |
| **Warnings como errores** | El código no compila con advertencias pendientes. |

## Convenciones de nombres

| Decisión | Motivo |
|---|---|
| **PascalCase en el código y en los tests** | Es la convención oficial de C# para tipos, métodos y propiedades. Los tests siguen el formato `Método_Escenario_Resultado` (cada segmento en PascalCase), para que el nombre del test diga qué se rompió cuando falla. |
| **kebab-case en las rutas de los endpoints** (`/api/top-scorers`) | Es la convención recomendada por las guías de APIs REST de Google, Microsoft y Zalando, y la más usada en APIs públicas. Se prefirió a snake_case porque el guion bajo queda oculto cuando una URL se muestra subrayada como enlace. |
| **camelCase en los parámetros de query y en el JSON** | Los parámetros de paginación (`pageNumber`, `pageSize`, `sortBy`, `sortDirection`) vienen definidos así en el enunciado, y camelCase es el formato JSON por defecto de ASP.NET Core. |

El detalle completo está en la [guía de contribución](../CONTRIBUTING.md#convenciones-de-nombres).

## Alcance

| Decisión | Motivo |
|---|---|
| **Un solo torneo** | El enunciado describe un único campeonato. Varios torneos se proponen como extensión futura. |
| **Un jugador pertenece a un solo equipo**, con dorsal único dentro del equipo | El enunciado pide "registro de jugadores por equipo". |
| **El equipo tiene un país** | Dato descriptivo del equipo. |
| **Borrado lógico** en todas las entidades | Preserva el historial: un equipo eliminado sigue apareciendo en los partidos ya jugados. |

## Reglas del torneo

| Regla | Detalle |
|---|---|
| Puntos | Victoria 3, empate 1, derrota 0. |
| Orden de la tabla | Puntos → diferencia de gol → goles a favor → nombre del equipo (desempate determinista). |
| Partidos que cuentan | Solo los partidos `Finished`. |
| Partidos inválidos | Un equipo no puede jugar contra sí mismo (400) ni tener dos partidos a la misma hora (409). "A la misma hora" es el mismo instante, comparado en UTC; los partidos cancelados no cuentan. |
| Eliminar un equipo | No se puede si tiene partidos programados o en curso (409 `Team.HasActiveMatches`): esos partidos quedarían sin rival. Con partidos finalizados o cancelados sí se puede; quedan como historial. |
| Equipo eliminado en los reportes | Sale de la tabla de posiciones y de los goleadores, pero sus partidos siguen en el listado de partidos, con su nombre. |
| Posición en la tabla | Se calcula sobre todos los equipos activos y no cambia al filtrar por nombre. |
| Goleadores | Cuentan los goles no anulados que no son autogol, de partidos en curso o finalizados (la lista se actualiza en vivo; la tabla de posiciones, solo al finalizar). Los goles de un partido cancelado no cuentan. Un jugador dado de baja conserva sus goles. Los empatados comparten posición (1, 1, 3). |

## Ciclo de vida del partido

```
Scheduled ──start──▶ InProgress ──finish──▶ Finished
    │                    │
    └──────cancel────────┴──────▶ Cancelled
```

- Los **goles se registran en vivo**, solo con el partido en `InProgress`. Cada gol guarda jugador, minuto (de 1 a 120, para cubrir la prórroga) y si es autogol.
- El jugador debe pertenecer a uno de los dos equipos del partido; si no, la petición se rechaza con 400.
- Un **autogol** suma al equipo rival del jugador. El gol guarda quién lo hizo y a qué equipo suma.
- El **marcador se calcula a partir de los goles** y se guarda en el partido para leerlo sin recalcular. Solo cambia al registrar, anular o corregir goles, así que no puede contradecirlos.
- Un gol puede **anularse** mientras el partido sigue en curso. Anular un gol que ya estaba anulado no es un error.
- Al **finalizar**, los goles quedan fijos. Intentar modificarlos devuelve 409.
- Una **corrección posterior** (`PUT /matches/{id}/result`) recibe la lista completa de goles y un motivo obligatorio. Los goles anteriores quedan anulados, no se borran, y se guarda un historial con el marcador anterior, el nuevo y el motivo. Si la lista enviada es igual a la actual, no cambia nada: el PUT es idempotente.
- Solo se puede **reprogramar** un partido `Scheduled`.
- Solo se puede **eliminar** un partido `Scheduled` o `Cancelled`. Uno en curso o finalizado forma parte del historial del torneo (409).

## API

| Tema | Decisión |
|---|---|
| Rutas | kebab-case y recursos en plural (`/api/teams`, `/api/top-scorers`). Ver [convenciones de nombres](#convenciones-de-nombres). |
| Idempotencia | `Idempotency-Key` obligatorio en todo POST. Sin el header → 400. Misma clave con otro body → 409. Misma clave mientras la primera sigue en curso → la segunda **espera** (hasta 10 s) a que la primera termine y recibe su respuesta; si la espera vence, 409. Repetición idéntica → se devuelve la respuesta original. Las claves expiran a las 24 horas y se guardan en la misma transacción que los cambios del comando. |
| DELETE | Idempotente: 204 aunque el recurso ya estuviera eliminado; 404 si nunca existió. |
| Errores | ProblemDetails (RFC 9457) con `errorCode`, `traceId` y `metadata` (diccionario de datos adicionales para el frontend). Nunca incluye datos sensibles. |
| Idiomas | La API devuelve códigos, nunca texto traducido: `errorCode` y `metadata` en cada error, y un código por campo en los errores de validación (`"errors": { "name": ["Team.NameRequired"] }`). Todo valor que cambia dentro de un mensaje viaja en `metadata`, para que el frontend pueda armar la frase en cualquier idioma. El frontend traduce con sus catálogos en español e inglés, y un test verifica que cada código tenga su traducción. `detail` queda en español como descripción para quien consume la API directo (Swagger, Postman). |
| Solo POST exige la clave | PUT y DELETE ya son idempotentes por definición (RFC 9110) y por diseño (DELETE 204, corrección de resultado sin cambios si la lista es igual). El PATCH de reprogramación también: fija una fecha, no la suma. |
| Repetición visible | Una respuesta repetida lleva el header `Idempotent-Replayed: true` (como Stripe). El cuerpo y el status son los de la primera vez. |
| Códigos HTTP | `Validation` 400, `NotFound` 404, `Conflict` 409, `Failure` 500. Un body o parámetro mal formado es 400 `Request.Malformed`; un error inesperado, 500 `Server.Unexpected` sin el mensaje de la excepción. |
| `Location` de jugadores y goles | Apunta al agregado (`/api/teams/{teamId}`, `/api/matches/{matchId}`): jugadores y goles no existen fuera de su equipo o partido, y el detalle del agregado los incluye. |
| `traceId` | Los 32 caracteres del trace W3C, el mismo que guarda `DomainEvents`. Si el cliente manda `traceparent`, se respeta. |
| Paginación | Todos los listados aceptan `pageNumber` (desde 1), `pageSize` (de 1 a 100; 10 por defecto), `sortBy`, `sortDirection` (`asc` o `desc`, sin distinguir mayúsculas) y filtros por entidad. La respuesta trae `items`, `pageNumber`, `pageSize`, `totalRecords` y `totalPages`. |
| Ordenamiento | `sortBy` se valida contra una lista blanca por recurso (400 `Query.InvalidSortBy`) y el lector lo traduce a una columna con su propio diccionario: el texto del usuario nunca llega al SQL. Siempre se agrega un desempate único para que las páginas no repitan ni salten filas. |
| Filtros de texto | Contienen el texto, sin distinguir mayúsculas (intercalación de SQL Server). Los comodines de `LIKE` del usuario se buscan literalmente. |
| Fechas en la query string | ISO 8601 con zona (`2026-11-07T00:00:00Z`). El `+` de un offset debe ir codificado (`%2B`); sin codificar llega como espacio. |

## Seguridad

| Decisión | Motivo |
|---|---|
| **Lecturas públicas, escrituras con el rol `Admin`** | El público consulta resultados, calendario y tabla sin crear cuentas; solo el organizador modifica el torneo. |
| **JWT firmado con HS256** (clave simétrica) | Un solo servicio emite y valida los tokens. RS256 (clave privada para firmar, pública para validar) conviene cuando otros servicios validan sin poder emitir. |
| **La protección es una convención del grupo `/api`** (`RequireAdminForWrites`) | Igual que el filtro de idempotencia: todo endpoint que no sea solo `GET` exige `Admin`, y un endpoint nuevo no puede olvidarlo. Dos tests recorren todos los endpoints registrados: cada escritura sin token da 401 y cada lectura es pública. |
| **El login es una query** | No cambia estado: lee el usuario, verifica el hash y firma un token. Por eso no abre transacción ni exige `Idempotency-Key` (repetirlo no duplica nada). |
| **El emisor del token vive en la Api**, junto a la validación | Las mismas `JwtOptions` firman y construyen los `TokenValidationParameters`: emisor, audiencia, clave y algoritmo no pueden quedar desalineados. Application solo conoce `ITokenIssuer`. |
| **Un usuario administrador en la tabla `Users`**, creado al iniciar desde la configuración | Las credenciales vienen de variables de entorno (o user-secrets en desarrollo); la base guarda solo el hash. Si la contraseña configurada cambia, al reiniciar se actualiza el hash: la configuración es la fuente de verdad. No depende del seed de ejemplo, así que también existe en una base sin datos de demo. |
| **PBKDF2-HMAC-SHA256 con 600.000 iteraciones** y sal aleatoria de 16 bytes | Es la configuración que recomienda OWASP para PBKDF2 y viene en .NET sin paquetes (`Rfc2898DeriveBytes.Pbkdf2`). El hash guarda el algoritmo y las iteraciones, así que se pueden subir sin invalidar los existentes. La comparación es en tiempo constante (`CryptographicOperations.FixedTimeEquals`). |
| **El login no revela si el usuario existe** | Usuario inexistente y contraseña incorrecta devuelven el mismo 401 `Auth.InvalidCredentials`, y en los dos casos se calcula un PBKDF2 para que el tiempo de respuesta sea parecido. |
| **401 y 403 con `errorCode`** (`Auth.Unauthenticated`, `Auth.Forbidden`) | Como todo error de la API: el frontend traduce por código. El middleware de autenticación no escribe cuerpo; `UseStatusCodePages` lo convierte en ProblemDetails y el enriquecedor le agrega el código. |
| **La Api no arranca sin configuración válida** | Sin `Admin:Username`, con una contraseña de menos de 12 caracteres o con una clave de firma de menos de 32 bytes, el arranque falla con un mensaje que nombra el ajuste (`ValidateOnStart`). Es mejor que descubrirlo en el primer login. |
| **Secretos fuera del repositorio** | user-secrets en desarrollo y variables de entorno en Docker (Compose las toma de un `.env` que está en `.gitignore`; `.env.example` lista las variables). La Api no lee el `.env` directamente: lee variables de entorno, como cualquier aplicación .NET. Un test impide que `Admin:Password` o `Jwt:SigningKey` aparezcan en los `appsettings*.json`. |
| **Claves de idempotencia por usuario** | Se guardan como `{usuario}:{clave}`: dos usuarios con la misma clave no chocan y nadie recibe la respuesta guardada de otro. |
| **Token de 60 minutos, sin refresh token** | Alcance de la prueba: el administrador vuelve a iniciar sesión. |

Límites conocidos:

- **Un JWT no se puede revocar antes de que venza.** Cerrar sesión es descartar el token en el cliente. Para revocar haría falta una lista de `jti` revocados o tokens cortos con refresh token.
- **Sin límite de intentos en el login.** PBKDF2 hace caro cada intento, pero no frena un ataque de fuerza bruta. El paso siguiente es un rate limiter por IP en `/api/auth/login` (`AddRateLimiter`, nativo de ASP.NET Core).
- **No hay gestión de usuarios.** Si se cambia `Admin:Username`, se crea otro administrador y el anterior sigue activo hasta borrarlo a mano.
- **Dos instancias arrancando a la vez sobre una base sin administrador:** las dos intentan crearlo y la segunda falla por el nombre único (como el seed). Al reiniciarla, el administrador ya existe y arranca normal.

## Eventos de dominio

`TeamCreated`, `MatchStarted`, `GoalScored`, `GoalAnnulled`, `MatchFinished`, `MatchCancelled` y `MatchResultCorrected`. Cada cambio de estado del partido genera su propio evento.

Las entidades acumulan sus eventos y el Unit of Work los despacha **antes de guardar**, dentro de la misma transacción: la tabla de posiciones se actualiza junto con el partido, sin consistencia eventual. Cada evento se registra en la tabla `DomainEvents` y se loggea con el TraceId del request que lo originó.

## Datos de arranque

| Decisión | Motivo |
|---|---|
| **El seed usa los commands de la API**, no `INSERT` | Respeta todas las reglas (validación, dominio, choques de horario) y arma la tabla de posiciones con los mismos eventos que en producción. Si una regla cambia, el seed no puede saltársela. |
| **Solo con la base vacía** | Si ya hay equipos, no hace nada. Volver a iniciar la Api no duplica datos. |
| **Migraciones y seed por configuración** (`Database:ApplyMigrations`, `Database:Seed`) | Apagados por defecto y encendidos en Development. En Docker (#6) se encienden por variables de entorno. |
| **Fechas relativas al día de arranque** | La fecha 4 es "hoy" (un partido en curso), las fechas 1 a 3 ya se jugaron y la 5 es la próxima semana. La demo siempre muestra un torneo a mitad de camino. |
| **LocalDB en desarrollo** | `appsettings.Development.json` usa `(localdb)\MSSQLLocalDB` con autenticación de Windows: no hay contraseña en el repositorio. Docker (#6) usará variables de entorno. |

Límite conocido: si dos instancias de la Api arrancan a la vez sobre una base vacía, las dos intentan el seed y la segunda falla por el nombre único de los equipos. Con una sola instancia (el caso de la demo y de docker-compose) no ocurre; en producción el seed sería un paso de despliegue aparte.

## Extensiones futuras

- **Varios torneos:** entidad `Tournament` con inscripción de equipos (un equipo una sola vez por torneo). La tabla y los goleadores pasarían a calcularse por torneo.
- **Jugadores en varios equipos** (por ejemplo, selección y club): relación N:M mediante `TeamMembership`, con el dorsal por membresía.
- **Usuarios y roles:** endpoints para administrar usuarios, más roles (por ejemplo, un árbitro que solo registra goles de su partido), refresh tokens y rate limiting en el login.
