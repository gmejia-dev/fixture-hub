namespace FixtureHub.Domain.Common;

public sealed record Error(string Code, string Message, ErrorType Type, IReadOnlyDictionary<string, object?>? Metadata = null)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Failure(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code, message, ErrorType.Failure, metadata);

    public static Error Validation(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code, message, ErrorType.Validation, metadata);

    public static Error NotFound(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code, message, ErrorType.NotFound, metadata);

    public static Error Conflict(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code, message, ErrorType.Conflict, metadata);
}
