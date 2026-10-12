using FixtureHub.Api.Observability;
using FixtureHub.Application.Errors;
using FixtureHub.Domain.Common;

namespace FixtureHub.Api.Http;

internal static class ProblemDetailsEnricher
{
    public const string TraceIdKey = "traceId";

    private static readonly IReadOnlyDictionary<int, Error> ErrorsByStatus = new Dictionary<int, Error>
    {
        [StatusCodes.Status401Unauthorized] = AuthErrors.Unauthenticated,
        [StatusCodes.Status403Forbidden] = AuthErrors.Forbidden,
        [StatusCodes.Status404NotFound] = ApiErrors.RouteNotFound,
        [StatusCodes.Status405MethodNotAllowed] = ApiErrors.MethodNotAllowed
    };

    public static void Enrich(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;

        problem.Instance ??= context.HttpContext.Request.Path.Value;
        problem.Extensions[TraceIdKey] = RequestTrace.IdOf(context.HttpContext);

        if (problem.Extensions.ContainsKey(ProblemMapping.ErrorCodeKey)
            || !ErrorsByStatus.TryGetValue(problem.Status ?? context.HttpContext.Response.StatusCode, out var error))
            return;

        problem.Detail ??= error.Message;
        foreach (var (key, value) in ProblemMapping.ExtensionsFor(error))
            problem.Extensions[key] = value;
    }
}
