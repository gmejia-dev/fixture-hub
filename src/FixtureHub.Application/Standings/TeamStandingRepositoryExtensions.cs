using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Standings;

namespace FixtureHub.Application.Standings;

internal static class TeamStandingRepositoryExtensions
{
    public static async Task<TeamStanding> GetRequiredAsync(
        this ITeamStandingRepository standings,
        Guid teamId,
        CancellationToken cancellationToken) =>
        await standings.GetByTeamIdAsync(teamId, cancellationToken)
            ?? throw new InvalidOperationException($"El equipo {teamId} no tiene fila en la tabla de posiciones.");
}
