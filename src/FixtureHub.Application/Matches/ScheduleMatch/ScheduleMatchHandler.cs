using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Matches;
using FixtureHub.Domain.Teams;

namespace FixtureHub.Application.Matches.ScheduleMatch;

internal sealed class ScheduleMatchHandler(ITeamRepository teams, IMatchRepository matches)
    : ICommandHandler<ScheduleMatchCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(ScheduleMatchCommand command, CancellationToken cancellationToken)
    {
        var scheduled = Match.Schedule(command.HomeTeamId, command.AwayTeamId, command.ScheduledAt);
        if (scheduled.IsFailure)
            return scheduled.Error;

        var match = scheduled.Value;
        foreach (var teamId in new[] { match.HomeTeamId, match.AwayTeamId })
        {
            if (await teams.GetByIdAsync(teamId, cancellationToken) is null)
                return TeamErrors.NotFound(teamId);
        }

        if (await matches.HasScheduleConflictAsync(
                match.HomeTeamId, match.AwayTeamId, match.ScheduledAt, exceptMatchId: null, cancellationToken))
            return MatchErrors.ScheduleConflict(match.ScheduledAt);

        matches.Add(match);

        return match.Id;
    }
}
