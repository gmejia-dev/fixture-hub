using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Pagination;

namespace FixtureHub.Application.Teams.GetPlayers;

public sealed record GetPlayersQuery(
    Guid TeamId,
    int PageNumber,
    int PageSize,
    string? SortBy,
    string? SortDirection,
    string? Name) : IQuery<PagedResult<PlayerItem>>, IPagedQuery
{
    public static readonly IReadOnlyList<string> SortFields = ["shirtNumber", "name"];
}
