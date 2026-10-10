using FixtureHub.Domain.Common;

namespace FixtureHub.Application.Errors;

public static class ValidationErrors
{
    public static Error Failed(IReadOnlyDictionary<string, string[]> errorsByField) =>
        Error.Validation(
            "Validation.Failed",
            "La solicitud tiene datos inválidos.",
            new Dictionary<string, object?> { ["errors"] = errorsByField });
}
