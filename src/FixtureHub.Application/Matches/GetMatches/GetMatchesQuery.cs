using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Pagination;

namespace FixtureHub.Application.Matches.GetMatches;

public sealed record GetMatchesQuery(
    int PageNumber,
    int PageSize,
    string? SortBy,
    string? SortDirection,
    Guid? TeamId,
    string? Status,
    DateTimeOffset? From,
    DateTimeOffset? To) : IQuery<PagedResult<MatchSummary>>, IPagedQuery
{
    public static readonly IReadOnlyList<string> SortFields = ["scheduledAt", "homeTeam", "awayTeam", "status"];
}
