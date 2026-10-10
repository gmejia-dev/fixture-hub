using FixtureHub.Domain.Common;
using FixtureHub.Domain.Teams;
using FixtureHub.Infrastructure.Persistence.Configurations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FixtureHub.Infrastructure.Persistence;

internal static class UniqueIndexViolation
{
    private const int DuplicateKeyInUniqueIndex = 2601;
    private const int DuplicateKeyInUniqueConstraint = 2627;

    public static Error? ToError(DbUpdateException exception)
    {
        if (exception.InnerException is not SqlException { Number: DuplicateKeyInUniqueIndex or DuplicateKeyInUniqueConstraint } sql)
            return null;

        var entities = exception.Entries.Select(entry => entry.Entity).ToList();

        if (sql.Message.Contains(TeamConfiguration.NameIndex, StringComparison.Ordinal)
            && entities.OfType<Team>().FirstOrDefault() is { } team)
            return TeamErrors.NameTaken(team.Name);

        if (sql.Message.Contains(PlayerConfiguration.ShirtNumberIndex, StringComparison.Ordinal)
            && entities.OfType<Player>().FirstOrDefault() is { } player)
            return PlayerErrors.ShirtNumberTaken(player.ShirtNumber);

        return null;
    }
}
