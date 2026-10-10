using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Pagination;

namespace FixtureHub.Application.TopScorers.GetTopScorers;

public sealed record GetTopScorersQuery(
    int PageNumber,
    int PageSize,
    string? SortBy,
    string? SortDirection,
    Guid? TeamId,
    string? PlayerName) : IQuery<PagedResult<TopScorerItem>>, IPagedQuery
{
    public static readonly IReadOnlyList<string> SortFields = ["position", "goals", "playerName", "teamName"];
}
