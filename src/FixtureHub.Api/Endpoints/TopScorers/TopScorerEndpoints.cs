using FixtureHub.Api.Http;
using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Pagination;
using FixtureHub.Application.TopScorers.GetTopScorers;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FixtureHub.Api.Endpoints.TopScorers;

internal static class TopScorerEndpoints
{
    public static IEndpointRouteBuilder MapTopScorerEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/top-scorers", GetTopScorers)
            .WithTags("TopScorers")
            .WithName(nameof(GetTopScorers))
            .WithSummary("Goleadores: goles no anulados, sin autogoles, de partidos en curso o finalizados.");

        return api;
    }

    private static async Task<Results<Ok<PagedResult<TopScorerItem>>, ProblemHttpResult>> GetTopScorers(
        [AsParameters] PageParameters page,
        Guid? teamId,
        string? playerName,
        IQueryHandler<GetTopScorersQuery, PagedResult<TopScorerItem>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetTopScorersQuery(page.Number, page.Size, page.SortBy, page.SortDirection, teamId, playerName),
            cancellationToken);

        return result.ToOk();
    }
}
