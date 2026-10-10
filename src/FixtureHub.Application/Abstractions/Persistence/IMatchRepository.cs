using FixtureHub.Domain.Matches;

namespace FixtureHub.Application.Abstractions.Persistence;

public interface IMatchRepository
{
    Task<Match?> GetByIdAsync(Guid matchId, CancellationToken cancellationToken);

    Task<Match?> GetByIdIncludingDeletedAsync(Guid matchId, CancellationToken cancellationToken);

    Task<bool> HasScheduleConflictAsync(
        Guid homeTeamId,
        Guid awayTeamId,
        DateTimeOffset scheduledAt,
        Guid? exceptMatchId,
        CancellationToken cancellationToken);

    Task<bool> HasActiveMatchesAsync(Guid teamId, CancellationToken cancellationToken);

    void Add(Match match);
}
