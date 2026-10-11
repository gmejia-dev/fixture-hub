using System.Diagnostics;

namespace FixtureHub.Api.Observability;

internal static class RequestTrace
{
    public static string IdOf(HttpContext context) => Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
}
