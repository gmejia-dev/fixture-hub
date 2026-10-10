using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Standings;

namespace FixtureHub.UnitTests.Application.Fakes;

internal sealed class FakeTeamStandingRepository : ITeamStandingRepository
{
    private readonly List<TeamStanding> _standings = [];

    public IReadOnlyList<TeamStanding> All => _standings;

    public Task<TeamStanding?> GetByTeamIdAsync(Guid teamId, CancellationToken cancellationToken) =>
        Task.FromResult(_standings.FirstOrDefault(standing => standing.TeamId == teamId));

    public void Add(TeamStanding standing) => _standings.Add(standing);
}
