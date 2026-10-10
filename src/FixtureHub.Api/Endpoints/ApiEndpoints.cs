using FixtureHub.Api.Endpoints.Matches;
using FixtureHub.Api.Endpoints.Teams;
using FixtureHub.Api.Idempotency;

namespace FixtureHub.Api.Endpoints;

internal static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").AddEndpointFilter<IdempotencyKeyFilter>();

        api.MapTeamEndpoints();
        api.MapMatchEndpoints();

        return app;
    }
}
