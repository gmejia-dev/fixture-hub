# Decisiones de diseño

Supuestos y decisiones tomadas donde el enunciado deja margen de interpretación.

## Tecnología

| Decisión | Motivo |
|---|---|
| **.NET 10 y C# 14** | Versión LTS vigente. El SDK queda fijado en `global.json` y el lenguaje en `Directory.Build.props`. |
| **SQL Server** | Afinidad con el ecosistema .NET y dominio del motor. El enunciado no exige uno en particular. |
| **Minimal APIs** | Endpoints agrupados por recurso con *route groups*; la idempotencia se aplica como *endpoint filter*. |
| **Handlers propios para CQRS** (sin MediatR) | Pocas necesidades: enviar un command o una query a su handler. MediatR requiere licencia desde la v13. Las interfaces son propias de Application, así que la decisión es reversible sin tocar los handlers. |
| **Warnings como errores** | El código no compila con advertencias pendientes. Lo mismo con EF Core: un `Take` sin `OrderBy` (resultados impredecibles) lanza una excepción en vez de solo advertir. |

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
| Idempotencia | `Idempotency-Key` obligatorio en todo POST. Sin el header → 400. Misma clave con otro body → 409. Misma clave mientras la primera sigue en curso → la segunda **espera** (hasta 10 s) a que la primera termine y recibe su respuesta; si la espera vence, 409. Repetición idéntica → se devuelve la respuesta original. Las claves expiran a las 24 horas y se guardan en la misma transacción que los cambios del comando. Un servicio en segundo plano borra las vencidas cada hora, en lotes de 1000. |
| DELETE | Idempotente: 204 aunque el recurso ya estuviera eliminado; 404 si nunca existió. |
| Errores | ProblemDetails (RFC 9457) con `errorCode`, `traceId` y `metadata` (diccionario de datos adicionales para el frontend). Nunca incluye datos sensibles. |
| Idiomas | La API devuelve códigos, nunca texto traducido: `errorCode` y `metadata` en cada error, y un código por campo en los errores de validación (`"errors": { "name": ["Team.NameRequired"] }`). Todo valor que cambia dentro de un mensaje viaja en `metadata`, para que el frontend pueda armar la frase en cualquier idioma. El frontend traduce con sus catálogos en español e inglés, y un test verifica que cada código tenga su traducción. `detail` queda en español como descripción para quien consume la API directo (Swagger, Postman). |
| Solo POST exige la clave | PUT y DELETE ya son idempotentes por definición (RFC 9110) y por diseño (DELETE 204, corrección de resultado sin cambios si la lista es igual). El PATCH de reprogramación también: fija una fecha, no la suma. |
| Repetición visible | Una respuesta repetida lleva el header `Idempotent-Replayed: true` (como Stripe). El cuerpo y el status son los de la primera vez. |
| Códigos HTTP | `Validation` 400, `NotFound` 404, `Conflict` 409, `Failure` 500. Un body o parámetro mal formado es 400 `Request.Malformed`; un error inesperado, 500 `Server.Unexpected` sin el mensaje de la excepción. Una ruta inexistente es 404 `Route.NotFound` y un método que la ruta no acepta, 405 `Route.MethodNotAllowed` (con el header `Allow`): ningún error sale sin `errorCode`. |
| `Location` de jugadores y goles | Apunta al agregado (`/api/teams/{teamId}`, `/api/matches/{matchId}`): jugadores y goles no existen fuera de su equipo o partido, y el detalle del agregado los incluye. |
| `traceId` | Los 32 caracteres del trace W3C, el mismo que guarda `DomainEvents`. Si el cliente manda `traceparent`, se respeta. Toda respuesta, con error o sin él, lo trae también en el header `X-Trace-Id`. |
| Paginación | Todos los listados aceptan `pageNumber` (desde 1), `pageSize` (de 1 a 100; 10 por defecto), `sortBy`, `sortDirection` (`asc` o `desc`, sin distinguir mayúsculas) y filtros por entidad. La respuesta trae `data`, `pageNumber`, `pageSize`, `totalRecords` y `totalPages`, la forma que fija el enunciado. |
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

Las entidades acumulan sus eventos y el Unit of Work los despacha **antes de guardar**, dentro de la misma transacción: la tabla de posiciones se actualiza junto con el partido, sin consistencia eventual. Cada evento se registra en la tabla `DomainEvents`, se loggea con el TraceId del request que lo originó y aparece como evento en el span del command.

## Observabilidad

| Decisión | Motivo |
|---|---|
| **Serilog como proveedor de `ILogger`**, no como API | El código sigue escribiendo con `ILogger<T>` y `[LoggerMessage]` (el estándar de .NET, casi sin costo cuando el nivel está apagado). Cambiar de Serilog a otro proveedor toca solo la Api. Dos tests impiden que Domain, Application o Infrastructure lo referencien. |
| **JSON compacto fuera de Development**, texto en Development | En un contenedor, los logs los recoge la plataforma (Docker, Loki, Elastic): JSON con propiedades se filtra sin parsear texto (`StatusCode >= 500`, `UserId = …`). En la terminal de desarrollo se lee mejor el texto. `Observability:JsonConsole` permite elegir. |
| **Un log por request** (`UseSerilogRequestLogging`) en vez de los de ASP.NET Core | Un solo evento con método, ruta, status, duración, plantilla de la ruta y usuario, en lugar de dos o tres eventos sueltos por request. Va primero en el pipeline para medir todo, incluido el manejo de errores. |
| **5xx como `Error`, 4xx como `Information`** | Un 404 o un 409 es un resultado esperado (el cliente pidió algo que no se puede); una alerta por nivel `Error` debe significar que el servidor falló. |
| **El CorrelationId es el TraceId W3C** | Un solo identificador para todo: logs, ProblemDetails, `DomainEvents`, el header `X-Trace-Id` y la traza de Jaeger. `traceparent` es el estándar para propagarlo entre servicios; inventar otro header obligaría a mantener dos Ids sincronizados. |
| **`X-Trace-Id` en toda respuesta**, escrito con `Response.OnStarting` | El cliente puede reportar el Id de cualquier request, no solo de los errores. `OnStarting` lo escribe justo antes de mandar los headers, así que sobrevive al manejador de excepciones, que borra los headers antes de escribir el 500 (un test lo verifica). |
| **El Id del usuario en los logs**, nunca el nombre ni datos del request | Con el `sub` del token se sabe quién hizo cada escritura sin guardar datos personales en los logs. Nunca se loguean bodies, contraseñas, tokens ni la query string. |
| **OpenTelemetry con exportación OTLP** | Es el estándar abierto: el código no sabe a qué backend van las trazas. Jaeger recibe OTLP de forma nativa (el exportador específico de Jaeger quedó obsoleto en OpenTelemetry .NET). Cambiar a Tempo, Zipkin o Azure Monitor es cambiar el endpoint. |
| **Endpoint por la variable estándar `OTEL_EXPORTER_OTLP_ENDPOINT`**, y sin ella no se exporta | Es la variable que reconoce cualquier SDK de OpenTelemetry. Sin endpoint, los tests y una máquina sin Jaeger no intentan conectarse. |
| **Un span propio por command y por query** | Las instrumentaciones de ASP.NET Core y SqlClient muestran el request y el SQL, pero no qué caso de uso corrió. El decorator agrega ese nivel y el resultado (`success` o `rejected` con el código). Usa `ActivitySource` de .NET, así que Application no depende de OpenTelemetry. |
| **Un rechazo de negocio no es un error en la traza** | Un 409 marcado en rojo esconde los errores reales. Se marca como error solo una excepción, igual que la convención de OpenTelemetry para HTTP (un 4xx no es error del servidor). |
| **SQL con la instrumentación de SqlClient** | EF Core y Dapper usan el mismo driver, así que una instrumentación cubre escrituras y lecturas. No registra los valores de los parámetros. |
| **Los eventos de dominio como eventos del span** | Un evento de dominio es algo que pasó en un instante, no una operación con duración: encaja como evento del span del command, con su Id para cruzarlo con la tabla `DomainEvents`. |

Límites conocidos:

- **Se trazan todos los requests.** Con mucho tráfico convendría muestrear (por ejemplo, `ParentBasedSampler` con una proporción) o hacerlo en un OpenTelemetry Collector.
- **Jaeger guarda las trazas en memoria:** se pierden al reiniciarlo. En producción usaría un almacenamiento persistente (Elasticsearch, Cassandra) o un servicio administrado.
- **Los logs solo van a la consola.** En Docker los guarda el motor; para buscarlos entre varias instancias haría falta enviarlos a Seq, Elastic o Loki (o por OTLP a un Collector).
- **No hay métricas.** El paso siguiente es `WithMetrics` con las métricas de ASP.NET Core y de SqlClient, exportadas a Prometheus.
- **El arranque no deja trazas:** las migraciones, el administrador y el seed corren antes de que el host inicie OpenTelemetry. Sí dejan logs.

## Datos de arranque

| Decisión | Motivo |
|---|---|
| **El seed usa los commands de la API**, no `INSERT` | Respeta todas las reglas (validación, dominio, choques de horario) y arma la tabla de posiciones con los mismos eventos que en producción. Si una regla cambia, el seed no puede saltársela. |
| **Solo con la base vacía** | Si ya hay equipos, no hace nada. Volver a iniciar la Api no duplica datos. |
| **Migraciones y seed por configuración** (`Database:ApplyMigrations`, `Database:Seed`) | Apagados por defecto y encendidos en Development. En docker-compose se encienden con las variables `Database__ApplyMigrations` y `Database__Seed`. |
| **Fechas relativas al día de arranque** | La fecha 4 es "hoy" (un partido en curso), las fechas 1 a 3 ya se jugaron y la 5 es la próxima semana. La demo siempre muestra un torneo a mitad de camino. |
| **LocalDB en desarrollo** | `appsettings.Development.json` usa `(localdb)\MSSQLLocalDB` con autenticación de Windows: no hay contraseña en el repositorio. En Docker, la cadena de conexión llega por variable de entorno con la contraseña del `.env`. |

Límite conocido: si dos instancias de la Api arrancan a la vez sobre una base vacía, las dos intentan el seed y la segunda falla por el nombre único de los equipos. Con una sola instancia (el caso de la demo y de docker-compose) no ocurre; en producción el seed sería un paso de despliegue aparte.

## Frontend

| Decisión | Motivo |
|---|---|
| **Next.js 16 con App Router**, versión fija 16.3.8 | Next.js lo pide el enunciado. App Router es el modelo actual (Server Components y Server Actions). Se fijó el último parche de la línea 16.3 en lugar de la 16.4.0, publicada cinco días antes de empezar. |
| **El navegador nunca llama a la Api**: lee con Server Components y escribe con Server Actions | El token no tiene que estar en JavaScript del navegador (no lo puede robar un script inyectado), no hace falta CORS y la Api no se expone a internet. La dirección de la Api se lee al ejecutarse (`API_BASE_URL`), así que la misma imagen de Docker sirve en cualquier entorno. Costo: cada página pasa por el servidor de Next.js. |
| **JWT en una cookie `HttpOnly`, `Secure`, `SameSite=Lax`**, con el vencimiento del token | `HttpOnly`: JavaScript no la puede leer. `Secure`: solo viaja por HTTPS (los navegadores aceptan `http://localhost` como seguro). `SameSite=Lax`: no viaja en peticiones de otros sitios, una defensa contra CSRF que se suma a la comprobación de `Origin` que hace Next.js en cada Server Action. |
| **El frontend no valida la firma del token**; solo lee `name`, `role` y `exp` | Para validarla necesitaría la clave de firma, que solo debe tener la Api. Esos datos sirven para mostrar u ocultar la interfaz de Admin; la seguridad la pone la Api en cada escritura. Si la Api responde 401 (token vencido o inválido), el frontend borra la cookie y lleva al login con la página a la que debe volver. |
| **Cada Server Action valida lo que recibe** antes de llamar a la Api | Una Server Action es un endpoint `POST` público aunque el botón esté oculto. Los Ids se validan como Guid antes de armar la ruta (un `../` no puede cambiar el endpoint) y el destino después del login debe ser una ruta interna (sin redirecciones abiertas). |
| **Filtros, orden y página en la URL** | Las listas se pueden compartir, el botón Atrás funciona y los filtros son un formulario `GET` que no necesita JavaScript. El orden se valida contra la misma lista blanca que la Api; un valor inválido en la URL vuelve al valor por defecto. |
| **Los errores de la Api son resultados, no excepciones** (`ApiResult`) | Como el Result Pattern del backend: la página decide qué mostrar (un 404 lleva a la página de no encontrado; otro error, al mensaje traducido con el código de seguimiento). Las excepciones quedan para lo inesperado (`error.tsx`). |
| **`Idempotency-Key` generada por el servidor al mostrar el formulario** | La clave identifica *ese* envío: se reutiliza si el usuario reintenta (doble clic, red caída, error corregido) y se renueva cuando la operación sale bien. Como la Api solo guarda las respuestas exitosas, reintentar con datos corregidos después de un error no choca. Generarla en el servidor evita una diferencia entre el HTML del servidor y el del navegador. |
| **Catálogos de idiomas propios en JSON**, sin librería | Son pocas pantallas y una función de 20 líneas cubre lo necesario (claves planas y marcadores `{valor}`). Al ser JSON, el test de .NET los lee directamente. TypeScript obliga a que `en.json` tenga todas las claves de `es.json` y a que el código solo use claves que existen. |
| **Idioma:** el elegido (cookie) → el del navegador (`Accept-Language`) → español | Lo pide la issue. El cambio de idioma es un formulario: funciona sin JavaScript y el servidor vuelve a armar la página en el idioma nuevo. |
| **Los límites constantes se escriben en el texto** ("no puede superar 100 caracteres") | Los errores de validación llegan por campo y solo con el código, sin `metadata`. Los límites son constantes del dominio, así que el catálogo los escribe; un test de .NET compara el número del texto con el del dominio y falla si alguien cambia uno sin el otro. Los valores que sí cambian (el dorsal repetido, el nombre del equipo) usan marcadores con la `metadata`. |
| **Fechas en la zona horaria del torneo** (`TOURNAMENT_TIME_ZONE`, por defecto `America/El_Salvador`) | El torneo se juega en un lugar: "19:00" significa lo mismo para todos. El servidor formatea con esa zona, así que el HTML no depende de la zona del contenedor ni del navegador. Al programar un partido, la hora se convierte a UTC con el desfase vigente en esa fecha. |
| **Goles en vivo por refresco periódico**: `router.refresh()` cada 10 s en el partido en curso | Vuelve a pedir la página al servidor sin recargar el navegador ni perder lo que se está escribiendo en un formulario. Se pausa con la pestaña oculta. Es suficiente para un torneo interno; la alternativa (SignalR o Server-Sent Events) está en extensiones futuras. |
| **La Api es la que valida**; el formulario solo pide los campos obligatorios y el tipo numérico | Las reglas viven en un solo lugar. Los mensajes de la Api se muestran traducidos junto a cada campo. |
| **Sin `loading.tsx`** | Con una página de carga, Next.js empieza a enviar la respuesta antes de saber si el equipo existe, y un equipo inexistente responde 200 en vez de 404. La Api responde en milisegundos, así que el indicador de carga no aporta y se prefiere el 404 real. |
| **Tailwind CSS sin librería de componentes** | La interfaz no se evalúa: clases utilitarias y unos pocos componentes propios bastan. |
| **Sin fuentes de Google, salida `standalone`, sin `X-Powered-By`** | El build no descarga nada de internet (importa en Docker), `standalone` produce un servidor de Node.js mínimo (unos 41 MB con los archivos estáticos) para la imagen de Docker, y no se anuncia la tecnología del servidor. |
| **Prettier, ESLint y saltos de línea LF** en el frontend | Formato y reglas de React verificables con un comando, como `dotnet format`. `frontend/.gitattributes` fija LF para que Git en Windows no convierta los archivos a CRLF y Prettier no los marque como mal formateados. |

Límites conocidos:

- **Actualización por consulta periódica**, no por eventos: un gol aparece en las pantallas abiertas hasta 10 segundos después.
- **Los selectores de equipos cargan hasta 100 equipos** (el máximo por página de la Api). Para más, harían falta un buscador o paginación en el selector.
- **Sin pruebas de punta a punta en el navegador** (Playwright): los flujos se verificaron a mano contra la Api real. Son el siguiente paso de las pruebas del frontend.
- **La cookie `Secure` exige HTTPS fuera de `localhost`.** En producción el frontend va detrás de HTTPS.
- **Un jugador dado de baja que marcó goles** no aparece en el selector del formulario de corrección de resultado.
- **Las trazas empiezan en la Api:** el servidor de Next.js no propaga `traceparent`, así que la traza no incluye el tramo del frontend. Ver extensiones futuras.

## Docker

| Decisión | Motivo |
|---|---|
| **Un `docker-compose.yml` con SQL Server, Api, frontend y Jaeger** | `docker compose up -d --build --wait` deja el sistema completo funcionando con un comando, sin instalar .NET, Node.js ni SQL Server. |
| **Imágenes multi-etapa** | El SDK de .NET y las dependencias de desarrollo de Node.js solo están en la etapa de compilación. La imagen final lleva el runtime y lo publicado: menos tamaño y menos superficie de ataque. |
| **Versiones fijas en todas las imágenes** (`sdk:10.0.401`, `aspnet:10.0.12`, `node:24.21.0-alpine`, `mssql/server:2022-CU27-ubuntu-22.04`, `jaeger:2.22.0`) | Un build de hoy y uno de dentro de un mes producen lo mismo. Con `latest`, una versión nueva puede romper el build o cambiar el comportamiento sin ningún cambio en el repositorio. Tests verifican que el SDK de la imagen sea el de `global.json` y que SQL Server y Jaeger sean los mismos que usan los tests de integración: se prueba con lo que se despliega. |
| **Sin root dentro de los contenedores** | Las imágenes corren con el usuario `app` (Api) y `node` (frontend). Si alguien explotara una vulnerabilidad, no tendría permisos de administrador en el contenedor. Un test lo verifica. |
| **Healthchecks en cadena** con `depends_on: condition: service_healthy` | Sin ellos, la Api arrancaría antes de que SQL Server acepte conexiones (tarda de 15 a 30 s) y fallaría al migrar. Cada servicio espera a que el anterior esté sano, y `--wait` sabe cuándo está todo listo. |
| **Dos health checks en la Api: `live` y `ready`** | Responden preguntas distintas: "¿hay que reiniciar el proceso?" y "¿le mando tráfico?". Si `live` consultara la base, una caída de SQL Server haría reiniciar la Api en bucle sin arreglar nada. Son los mismos conceptos que las sondas de Kubernetes (*liveness* y *readiness*). |
| **Los health checks no dejan logs ni trazas** | Docker los consulta cada 10 segundos: llenarían los logs y Jaeger de ruido. Un check que falla sí se registra como `Error`. |
| **`curl` en la imagen de la Api** | El healthcheck de Docker corre dentro del contenedor, y la imagen `aspnet` no trae `curl` ni `wget`. Agrega unos 6,5 MB. Alternativas descartadas: la imagen Alpine (trae `wget`, pero `SqlClient` necesita además las librerías ICU) o un programa propio de healthcheck. En Kubernetes no haría falta: la sonda la hace el kubelet desde afuera. |
| **Migraciones con `MigrateAsync` al arrancar** | Lo más simple para una sola instancia. EF Core toma un candado en la base antes de migrar (`Acquiring an exclusive lock for migration application`), así que dos instancias no aplican la misma migración dos veces. En producción las aplicaría un paso del pipeline (un *migration bundle* o un script SQL revisado). |
| **Secretos desde `.env` con `${VARIABLE:?mensaje}`** | Si falta un secreto, `docker compose` se detiene antes de crear contenedores y dice cuál falta. Cada contenedor recibe solo las variables que usa: el frontend no recibe ningún secreto. Un test impide escribir un secreto en el compose y otro exige que `.env.example` documente todas las variables. |
| **Puertos publicados solo en `127.0.0.1`** | Los servicios se usan desde la máquina; nadie de la red local llega a SQL Server ni a la Api. La Api se publica en el 5065, el mismo de `dotnet run`, para que el `.http` y Postman sirvan en los dos modos. |
| **Volumen con nombre para SQL Server** | Los datos sobreviven a `docker compose down`. El seed solo corre con la base vacía, así que reiniciar no duplica datos. |
| **`TrustServerCertificate=True` en la cadena de conexión** | SQL Server en el contenedor usa un certificado autofirmado, y `SqlClient` cifra la conexión por defecto y lo rechazaría. La conexión sigue cifrada; lo que no se valida es quién firmó el certificado. Es aceptable dentro de la red interna de Docker; en producción, un certificado de una autoridad de confianza. |

Límites conocidos:

- **La Api se conecta con `sa`.** En producción tendría un usuario con permisos mínimos (leer y escribir sus tablas) y las migraciones correrían con otro usuario.
- **Dos advertencias de Data Protection al arrancar** (claves guardadas dentro del contenedor y sin cifrar). ASP.NET Core inicializa Data Protection aunque la Api no lo use: el JWT viaja en un header y no hay cookies ni antiforgery. Si se usara, las claves irían a un volumen o a un almacén compartido y se cifrarían con un certificado.
- **Sin HTTPS.** Los navegadores tratan `http://localhost` como seguro, así que la cookie `Secure` funciona en la demo. En producción, un proxy (o Kestrel con certificado) terminaría TLS delante del frontend.
- **La contraseña de `sa` se fija la primera vez que se crea el volumen.** Cambiarla después en el `.env` no la cambia en SQL Server: hay que borrar el volumen (`docker compose down -v`) o cambiarla con `ALTER LOGIN`.
- **SQL Server no tiene imagen nativa para ARM** (Mac con chip M): corre con emulación, más lento.
- **Las imágenes se construyen en la máquina.** El paso siguiente es construirlas en CI y publicarlas en un registro con su versión.

## Colección de Postman y diagrama

| Decisión | Motivo |
|---|---|
| **La colección es un recorrido del torneo en orden**, con verificaciones en cada request | Sirve para la demo y como prueba de humo de la Api desplegada: se corre completa con el Runner o con Newman contra Docker Compose, y cada request guarda los Ids que usan los siguientes (equipo, jugador, partido, gol). |
| **Las verificaciones comunes van en el script de la colección** | `X-Trace-Id` en toda respuesta, ProblemDetails con `errorCode` y el mismo `traceId` en los errores, y el contrato de paginación en los listados se comprueban en todas las respuestas sin repetirlos en cada request. |
| **El environment tiene la configuración; las variables de la colección, lo que genera la corrida** | `baseUrl`, el usuario y la contraseña cambian según dónde corre la Api; el token y los Ids son de cada corrida. La contraseña es de tipo `secret` y se sube vacía. Dos tests impiden subir la contraseña o los valores de una corrida. |
| **Cada corrida es independiente** | Los nombres llevan un sufijo aleatorio y la última carpeta borra lo creado: la colección se puede correr varias veces sobre la misma base, también la de la demo. |
| **Un test exige un request por endpoint** | Recorre los endpoints registrados en la Api: si se agrega uno, la colección no puede quedarse atrás. |
| **Newman con versión fija** (`newman@6.2.2`) | Igual que las imágenes de Docker: el comando del README produce lo mismo hoy y dentro de un mes. Se descartó la 6.2.3, publicada dos días antes de armar la colección. |
| **El diagrama de arquitectura en Mermaid**, dentro del Markdown | Es texto: se revisa en el diff de un pull request y GitHub lo dibuja. Una imagen exportada se desactualiza sin que nadie lo note. Un test verifica que el del README sea el mismo de `docs/arquitectura.md`. |
| **Las flechas del diagrama son el flujo de un request** | Muestran por dónde pasan una escritura y una lectura. Entre Application e Infrastructure, la dependencia de código va al revés (Infrastructure implementa las interfaces de Application); eso se explica en la sección de capas de `docs/arquitectura.md`. |

Límites conocidos:

- **La colección no prueba el 403:** el único rol que existe es `Admin`. Lo prueban los tests de integración con un token de otro rol firmado dentro del test.
- **Newman prueba la Api, no el frontend.** Los flujos del frontend se verificaron a mano; Playwright está en extensiones futuras.

## Extensiones futuras

- **Varios torneos:** entidad `Tournament` con inscripción de equipos (un equipo una sola vez por torneo). La tabla y los goleadores pasarían a calcularse por torneo.
- **Jugadores en varios equipos** (por ejemplo, selección y club): relación N:M mediante `TeamMembership`, con el dorsal por membresía.
- **Usuarios y roles:** endpoints para administrar usuarios, más roles (por ejemplo, un árbitro que solo registra goles de su partido), refresh tokens y rate limiting en el login.
- **Observabilidad completa:** métricas con Prometheus y Grafana, logs centralizados y un OpenTelemetry Collector que reciba logs, métricas y trazas y decida el muestreo.
- **Frontend en tiempo real:** SignalR o Server-Sent Events desde la Api para empujar los goles en lugar de consultar cada 10 segundos.
- **Trazas desde el frontend:** OpenTelemetry en Next.js (`instrumentation.ts`) para que la traza empiece en el servidor de Next.js y continúe en la Api con `traceparent`.
- **Pruebas de punta a punta:** Playwright contra la Api y el frontend levantados con docker-compose.
- **Integración y entrega continuas:** GitHub Actions que corra los tests, construya las imágenes, las publique en un registro con la versión y despliegue detrás de HTTPS.
