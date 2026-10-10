using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Matches;

namespace FixtureHub.Application.Matches.RescheduleMatch;

internal sealed class RescheduleMatchHandler(IMatchRepository matches) : ICommandHandler<RescheduleMatchCommand>
{
    public async Task<Result> HandleAsync(RescheduleMatchCommand command, CancellationToken cancellationToken)
    {
        var match = await matches.GetByIdAsync(command.MatchId, cancellationToken);
        if (match is null)
            return MatchErrors.NotFound(command.MatchId);

        var rescheduled = match.Reschedule(command.ScheduledAt);
        if (rescheduled.IsFailure)
            return rescheduled;

        if (await matches.HasScheduleConflictAsync(
                match.HomeTeamId, match.AwayTeamId, match.ScheduledAt, exceptMatchId: match.Id, cancellationToken))
            return MatchErrors.ScheduleConflict(match.ScheduledAt);

        return Result.Success();
    }
}
