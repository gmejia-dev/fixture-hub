using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Standings;

namespace FixtureHub.Infrastructure.Persistence.Repositories;

internal sealed class TeamStandingRepository(FixtureHubDbContext context) : ITeamStandingRepository
{
    public async Task<TeamStanding?> GetByTeamIdAsync(Guid teamId, CancellationToken cancellationToken) =>
        await context.TeamStandings.FindAsync([teamId], cancellationToken);

    public void Add(TeamStanding standing) => context.TeamStandings.Add(standing);
}
