using FixtureHub.Application.Matches.GetMatchById;
using FixtureHub.Application.Matches.GetMatches;
using FixtureHub.Application.Pagination;

namespace FixtureHub.Application.Abstractions.Reading;

public interface IMatchReader
{
    Task<PagedResult<MatchSummary>> GetMatchesAsync(GetMatchesQuery query, CancellationToken cancellationToken);

    Task<MatchDetails?> GetMatchAsync(Guid matchId, CancellationToken cancellationToken);
}
