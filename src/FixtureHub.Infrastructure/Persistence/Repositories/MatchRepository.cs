using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Matches;
using Microsoft.EntityFrameworkCore;

namespace FixtureHub.Infrastructure.Persistence.Repositories;

internal sealed class MatchRepository(FixtureHubDbContext context) : IMatchRepository
{
    public Task<Match?> GetByIdAsync(Guid matchId, CancellationToken cancellationToken) =>
        WholeAggregate().FirstOrDefaultAsync(match => match.Id == matchId, cancellationToken);

    public Task<Match?> GetByIdIncludingDeletedAsync(Guid matchId, CancellationToken cancellationToken) =>
        WholeAggregate().IgnoreQueryFilters().FirstOrDefaultAsync(match => match.Id == matchId, cancellationToken);

    public Task<bool> HasScheduleConflictAsync(
        Guid homeTeamId,
        Guid awayTeamId,
        DateTimeOffset scheduledAt,
        Guid? exceptMatchId,
        CancellationToken cancellationToken) =>
        context.Matches.AnyAsync(
            match => match.Status != MatchStatus.Cancelled
                && match.ScheduledAt == scheduledAt
                && (exceptMatchId == null || match.Id != exceptMatchId)
                && (match.HomeTeamId == homeTeamId
                    || match.AwayTeamId == homeTeamId
                    || match.HomeTeamId == awayTeamId
                    || match.AwayTeamId == awayTeamId),
            cancellationToken);

    public Task<bool> HasActiveMatchesAsync(Guid teamId, CancellationToken cancellationToken) =>
        context.Matches.AnyAsync(
            match => (match.Status == MatchStatus.Scheduled || match.Status == MatchStatus.InProgress)
                && (match.HomeTeamId == teamId || match.AwayTeamId == teamId),
            cancellationToken);

    public void Add(Match match) => context.Matches.Add(match);

    private IQueryable<Match> WholeAggregate() =>
        context.Matches.Include(match => match.Goals).Include(match => match.Corrections);
}
