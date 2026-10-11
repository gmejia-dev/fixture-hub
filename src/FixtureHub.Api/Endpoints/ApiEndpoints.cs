using FixtureHub.Api.Endpoints.Auth;
using FixtureHub.Api.Endpoints.Matches;
using FixtureHub.Api.Endpoints.Standings;
using FixtureHub.Api.Endpoints.Teams;
using FixtureHub.Api.Endpoints.TopScorers;
using FixtureHub.Api.Idempotency;

namespace FixtureHub.Api.Endpoints;

internal static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api").MapAuthEndpoints();

        var api = app.MapGroup("/api").AddEndpointFilter<IdempotencyKeyFilter>();

        api.MapTeamEndpoints();
        api.MapMatchEndpoints();
        api.MapStandingsEndpoints();
        api.MapTopScorerEndpoints();

        return app;
    }
}
