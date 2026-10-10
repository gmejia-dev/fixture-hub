using FixtureHub.Domain.Common;

namespace FixtureHub.Application.Errors;

public static class IdempotencyErrors
{
    public static Error KeyReused(string key) =>
        Error.Conflict(
            "Idempotency.KeyReused",
            "La clave de idempotencia ya se usó con una solicitud distinta.",
            new Dictionary<string, object?> { ["key"] = key });

    public static Error RequestInProgress(string key) =>
        Error.Conflict(
            "Idempotency.RequestInProgress",
            "Otra solicitud con la misma clave de idempotencia sigue en curso.",
            new Dictionary<string, object?> { ["key"] = key });
}
