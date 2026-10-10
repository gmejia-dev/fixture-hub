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
| Paginación | Todos los GET aceptan `pageNumber`, `pageSize` (máximo 100), `sortBy`, `sortDirection` y filtros por entidad. |
| Ordenamiento | `sortBy` se valida contra una lista blanca de columnas para evitar inyección SQL. |

## Seguridad

- **JWT** con rol `Admin`.
- Las **lecturas son públicas**: el público consulta resultados, calendario y tabla sin crear cuentas.
- Las **escrituras** (POST, PUT, PATCH, DELETE) requieren el rol `Admin`.
- El usuario administrador se crea en el seed; sus credenciales vienen de variables de entorno y la contraseña se guarda con hash.

## Eventos de dominio

`TeamCreated`, `MatchStarted`, `GoalScored`, `GoalAnnulled`, `MatchFinished`, `MatchCancelled` y `MatchResultCorrected`. Cada cambio de estado del partido genera su propio evento.

Las entidades acumulan sus eventos y el Unit of Work los despacha **antes de guardar**, dentro de la misma transacción: la tabla de posiciones se actualiza junto con el partido, sin consistencia eventual. Cada evento se registra en la tabla `DomainEvents` y se loggea con el TraceId del request que lo originó.

## Extensiones futuras

- **Varios torneos:** entidad `Tournament` con inscripción de equipos (un equipo una sola vez por torneo). La tabla y los goleadores pasarían a calcularse por torneo.
- **Jugadores en varios equipos** (por ejemplo, selección y club): relación N:M mediante `TeamMembership`, con el dorsal por membresía.
