using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Matches;
using FixtureHub.Domain.Teams;

namespace FixtureHub.Application.Matches;

internal sealed class MatchRoster
{
    private readonly Team?[] _teams;

    private MatchRoster(Team? home, Team? away) => _teams = [home, away];

    public static async Task<MatchRoster> LoadAsync(
        ITeamRepository teams,
        Match match,
        CancellationToken cancellationToken) =>
        new(
            await teams.GetByIdAsync(match.HomeTeamId, cancellationToken),
            await teams.GetByIdAsync(match.AwayTeamId, cancellationToken));

    public Player? FindPlayer(Guid playerId) =>
        _teams.Select(team => team?.FindPlayer(playerId)).FirstOrDefault(player => player is not null);
}
