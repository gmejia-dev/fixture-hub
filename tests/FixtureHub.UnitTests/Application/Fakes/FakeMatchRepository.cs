using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Matches;

namespace FixtureHub.UnitTests.Application.Fakes;

internal sealed class FakeMatchRepository : IMatchRepository
{
    private readonly List<Match> _matches = [];

    public IReadOnlyList<Match> All => _matches;

    public Task<Match?> GetByIdAsync(Guid matchId, CancellationToken cancellationToken) =>
        Task.FromResult(_matches.FirstOrDefault(match => match.Id == matchId && !match.IsDeleted));

    public Task<Match?> GetByIdIncludingDeletedAsync(Guid matchId, CancellationToken cancellationToken) =>
        Task.FromResult(_matches.FirstOrDefault(match => match.Id == matchId));

    public Task<bool> HasScheduleConflictAsync(
        Guid homeTeamId,
        Guid awayTeamId,
        DateTimeOffset scheduledAt,
        Guid? exceptMatchId,
        CancellationToken cancellationToken) =>
        Task.FromResult(_matches.Any(match =>
            !match.IsDeleted
            && match.Status != MatchStatus.Cancelled
            && match.Id != exceptMatchId
            && match.ScheduledAt == scheduledAt
            && (Plays(match, homeTeamId) || Plays(match, awayTeamId))));

    public Task<bool> HasActiveMatchesAsync(Guid teamId, CancellationToken cancellationToken) =>
        Task.FromResult(_matches.Any(match =>
            !match.IsDeleted
            && match.Status is MatchStatus.Scheduled or MatchStatus.InProgress
            && Plays(match, teamId)));

    public void Add(Match match) => _matches.Add(match);

    private static bool Plays(Match match, Guid teamId) => match.HomeTeamId == teamId || match.AwayTeamId == teamId;
}
