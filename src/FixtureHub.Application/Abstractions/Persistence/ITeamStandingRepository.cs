using FixtureHub.Domain.Standings;

namespace FixtureHub.Application.Abstractions.Persistence;

public interface ITeamStandingRepository
{
    Task<TeamStanding?> GetByTeamIdAsync(Guid teamId, CancellationToken cancellationToken);

    void Add(TeamStanding standing);
}
