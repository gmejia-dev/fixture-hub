using FixtureHub.Application.Pagination;
using FixtureHub.Domain.Common;

namespace FixtureHub.Application.Errors;

public static class QueryErrors
{
    public static readonly Error InvalidPageNumber =
        Error.Validation(
            "Query.InvalidPageNumber",
            $"El número de página debe ser {Paging.FirstPage} o mayor.",
            new Dictionary<string, object?> { ["min"] = Paging.FirstPage });

    public static readonly Error InvalidPageSize =
        Error.Validation(
            "Query.InvalidPageSize",
            $"El tamaño de página debe estar entre 1 y {Paging.MaxPageSize}.",
            new Dictionary<string, object?> { ["min"] = 1, ["max"] = Paging.MaxPageSize });

    public static readonly Error InvalidSortBy =
        Error.Validation("Query.InvalidSortBy", "El campo de ordenamiento no está permitido para esta consulta.");

    public static readonly Error InvalidSortDirection =
        Error.Validation(
            "Query.InvalidSortDirection",
            $"La dirección de ordenamiento debe ser {Paging.Ascending} o {Paging.Descending}.");

    public static readonly Error InvalidStatus =
        Error.Validation("Query.InvalidStatus", "El estado del partido no es válido.");

    public static readonly Error InvalidDateRange =
        Error.Validation("Query.InvalidDateRange", "La fecha inicial no puede ser posterior a la final.");
}
