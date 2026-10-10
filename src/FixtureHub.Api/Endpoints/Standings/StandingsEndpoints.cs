using FixtureHub.Api.Http;
using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Pagination;
using FixtureHub.Application.Standings.GetStandings;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FixtureHub.Api.Endpoints.Standings;

internal static class StandingsEndpoints
{
    public static IEndpointRouteBuilder MapStandingsEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/standings", GetStandings)
            .WithTags("Standings")
            .WithName(nameof(GetStandings))
            .WithSummary("Tabla de posiciones: puntos, diferencia de gol, goles a favor y nombre.");

        return api;
    }

    private static async Task<Results<Ok<PagedResult<StandingItem>>, ProblemHttpResult>> GetStandings(
        [AsParameters] PageParameters page,
        string? teamName,
        IQueryHandler<GetStandingsQuery, PagedResult<StandingItem>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetStandingsQuery(page.Number, page.Size, page.SortBy, page.SortDirection, teamName),
            cancellationToken);

        return result.ToOk();
    }
}
