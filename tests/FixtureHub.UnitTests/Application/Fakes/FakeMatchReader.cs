using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Application.Matches.GetMatchById;
using FixtureHub.Application.Matches.GetMatches;
using FixtureHub.Application.Pagination;

namespace FixtureHub.UnitTests.Application.Fakes;

internal sealed class FakeMatchReader : IMatchReader
{
    public MatchDetails? Match { get; set; }

    public Task<PagedResult<MatchSummary>> GetMatchesAsync(GetMatchesQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(new PagedResult<MatchSummary>([], query.PageNumber, query.PageSize, 0));

    public Task<MatchDetails?> GetMatchAsync(Guid matchId, CancellationToken cancellationToken) =>
        Task.FromResult(Match);
}
