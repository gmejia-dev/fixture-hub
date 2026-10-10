using FixtureHub.Domain.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FixtureHub.Api.Http;

internal static class ProblemMapping
{
    public const string ErrorCodeKey = "errorCode";
    public const string MetadataKey = "metadata";

    private static readonly IReadOnlyDictionary<string, object?> NoMetadata = new Dictionary<string, object?>();

    public static ProblemHttpResult ToProblem(this Error error) =>
        TypedResults.Problem(
            detail: error.Message,
            statusCode: StatusCodeFor(error.Type),
            extensions: ExtensionsFor(error));

    public static IDictionary<string, object?> ExtensionsFor(Error error) =>
        new Dictionary<string, object?>
        {
            [ErrorCodeKey] = error.Code,
            [MetadataKey] = error.Metadata ?? NoMetadata
        };

    public static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
}
