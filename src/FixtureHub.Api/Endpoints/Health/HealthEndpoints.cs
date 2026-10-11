using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace FixtureHub.Api.Endpoints.Health;

internal static class HealthEndpoints
{
    public const string BasePath = "/health";
    public const string LivePath = BasePath + "/live";
    public const string ReadyPath = BasePath + "/ready";

    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks(LivePath, new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks(ReadyPath);

        return app;
    }

    public static bool IsHealthCheck(PathString path) => path.StartsWithSegments(BasePath);
}
