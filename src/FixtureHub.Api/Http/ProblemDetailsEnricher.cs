using System.Diagnostics;

namespace FixtureHub.Api.Http;

internal static class ProblemDetailsEnricher
{
    public const string TraceIdKey = "traceId";

    public static void Enrich(ProblemDetailsContext context)
    {
        context.ProblemDetails.Instance ??= context.HttpContext.Request.Path.Value;
        context.ProblemDetails.Extensions[TraceIdKey] =
            Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
    }
}
