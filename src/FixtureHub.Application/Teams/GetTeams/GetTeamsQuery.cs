using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Pagination;

namespace FixtureHub.Application.Teams.GetTeams;

public sealed record GetTeamsQuery(
    int PageNumber,
    int PageSize,
    string? SortBy,
    string? SortDirection,
    string? Name,
    string? Country) : IQuery<PagedResult<TeamSummary>>, IPagedQuery
{
    public static readonly IReadOnlyList<string> SortFields = ["name", "country", "playerCount"];
}
