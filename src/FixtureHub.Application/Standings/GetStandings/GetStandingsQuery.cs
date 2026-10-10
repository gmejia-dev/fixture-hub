using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Pagination;

namespace FixtureHub.Application.Standings.GetStandings;

public sealed record GetStandingsQuery(
    int PageNumber,
    int PageSize,
    string? SortBy,
    string? SortDirection,
    string? TeamName) : IQuery<PagedResult<StandingItem>>, IPagedQuery
{
    public static readonly IReadOnlyList<string> SortFields =
    [
        "position",
        "teamName",
        "played",
        "won",
        "drawn",
        "lost",
        "goalsFor",
        "goalsAgainst",
        "goalDifference",
        "points"
    ];
}
