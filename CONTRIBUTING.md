# Guía de contribución

## Flujo de trabajo

1. Cada trabajo nace como una **issue**.
2. Se crea una **rama** desde `main` para esa issue.
3. Se trabaja con commits pequeños en la rama.
4. Se abre un **pull request** hacia `main` que cierra la issue (`Closes #n` en la descripción).
5. Se integra con **squash merge** y se borra la rama.

```bash
git switch main
git pull
git switch -c feature/<tema>
# ... commits ...
git push -u origin feature/<tema>
gh pr create --fill
gh pr merge --squash --delete-branch
```

## Ramas

Formato `tipo/tema`, en minúsculas y con guiones:

| Tipo | Uso | Ejemplo |
|---|---|---|
| `feature/` | Funcionalidad nueva | `feature/lectura-dapper` |
| `fix/` | Corrección de un error | `fix/paginacion-goleadores` |
| `meta/` | Repositorio, configuración, CI, documentación | `meta/docker-compose` |

## Commits

Formato `PREFIJO: descripción`, con la descripción en español y en minúsculas. Cuando aplica, el número de la issue o del PR va al final entre paréntesis.

| Prefijo | Uso | Ejemplo |
|---|---|---|
| `ADD` | Funcionalidad nueva | `ADD: registro de goles en vivo con autogol (#2)` |
| `FIX` | Corrección de un error | `FIX: la tabla no contaba los partidos finalizados en la última fecha` |
| `IMP` | Mejora de algo que ya existe (rendimiento, legibilidad, refactor) | `IMP: la consulta de goleadores pagina en SQL en lugar de en memoria` |
| `META` | Todo lo que no es producto: repositorio, configuración, CI, documentación | `META: README y guía de contribución` |

Equivalencia con [Conventional Commits](https://www.conventionalcommits.org/):

| FixtureHub | Conventional Commits |
|---|---|
| `ADD` | `feat` |
| `FIX` | `fix` |
| `IMP` | `refactor`, `perf` |
| `META` | `chore`, `docs`, `ci`, `build` |

## Convenciones de nombres

El código y los tests usan **PascalCase**, siguiendo las [convenciones de C#](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/identifier-names). Las **rutas de los endpoints** usan **kebab-case**. El motivo de cada decisión está en [docs/decisiones.md](docs/decisiones.md#convenciones-de-nombres).

| Elemento | Convención | Ejemplo |
|---|---|---|
| Clases, records, enums, interfaces | PascalCase (interfaces con `I`) | `Result`, `ErrorType`, `IUnitOfWork` |
| Métodos y propiedades | PascalCase | `AddGoal()`, `IsSuccess` |
| Constantes y `static readonly` | PascalCase | `Error.None` |
| Parámetros y variables locales | camelCase | `matchId`, `result` |
| Campos privados | `_camelCase` | `_value` |
| Tests | `Método_Escenario_Resultado` | `Failure_WithErrorNone_ThrowsArgumentException` |
| Códigos de error | `Entidad.Motivo` | `Match.NotInProgress` |
| Tablas y columnas en SQL Server | PascalCase | `Teams`, `GoalDifference` |
| **Rutas de endpoints** | **minúsculas, palabras separadas con guion (kebab-case)** | `/api/teams`, `/api/top-scorers`, `/api/matches/{id}/goals` |
| Parámetros de query | camelCase (los que fija el enunciado) | `?pageNumber=1&pageSize=10&sortBy=name` |
| Propiedades JSON | camelCase (por defecto en ASP.NET Core) | `totalRecords` |
| Ramas y carpetas del repositorio | kebab-case | `feature/solucion-dominio` |

## Pull requests

- Título con el mismo formato de los commits. Al hacer squash, ese título queda como el commit en `main`.
- La descripción incluye `Closes #n` para cerrar la issue automáticamente.
- Antes de integrar:
  - `dotnet build` sin errores ni warnings (los warnings se tratan como errores).
  - `dotnet test` en verde.
