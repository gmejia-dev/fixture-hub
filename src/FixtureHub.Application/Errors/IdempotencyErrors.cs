using FixtureHub.Application.Abstractions.Idempotency;
using FixtureHub.Domain.Common;

namespace FixtureHub.Application.Errors;

public static class IdempotencyErrors
{
    public static readonly Error KeyRequired =
        Error.Validation(
            "Idempotency.KeyRequired",
            "El header Idempotency-Key es obligatorio en los POST y debe tener un solo valor.");

    public static readonly Error KeyTooLong =
        Error.Validation(
            "Idempotency.KeyTooLong",
            $"La clave de idempotencia no puede superar {IdempotencyRecord.KeyMaxLength} caracteres.",
            new Dictionary<string, object?> { ["maxLength"] = IdempotencyRecord.KeyMaxLength });

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
