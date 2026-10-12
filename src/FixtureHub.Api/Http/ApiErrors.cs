using FixtureHub.Domain.Common;

namespace FixtureHub.Api.Http;

internal static class ApiErrors
{
    public static readonly Error MalformedRequest =
        Error.Validation(
            "Request.Malformed",
            "La solicitud no tiene un formato válido: revisa el JSON, los tipos de los campos y los parámetros.");

    public static readonly Error Unexpected =
        Error.Failure(
            "Server.Unexpected",
            "Ocurrió un error inesperado. Usa el traceId para rastrearlo en los logs.");

    public static readonly Error RouteNotFound =
        Error.NotFound(
            "Route.NotFound",
            "La ruta no existe en la Api: revisa la dirección.");

    public static readonly Error MethodNotAllowed =
        Error.Validation(
            "Route.MethodNotAllowed",
            "La ruta existe, pero no acepta este método HTTP: revisa el header Allow de la respuesta.");
}
