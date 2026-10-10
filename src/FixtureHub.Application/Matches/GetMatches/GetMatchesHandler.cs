using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Application.Pagination;
using FixtureHub.Domain.Common;

namespace FixtureHub.Application.Matches.GetMatches;

internal sealed class GetMatchesHandler(IMatchReader matches) : IQueryHandler<GetMatchesQuery, PagedResult<MatchSummary>>
{
    public async Task<Result<PagedResult<MatchSummary>>> HandleAsync(GetMatchesQuery query, CancellationToken cancellationToken) =>
        await matches.GetMatchesAsync(query, cancellationToken);
}
