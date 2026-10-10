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
}
