using FixtureHub.Application.Abstractions.Events;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Matches;

namespace FixtureHub.Application.Standings;

internal sealed class MatchResultCorrectedHandler(ITeamStandingRepository standings)
    : IDomainEventHandler<MatchResultCorrected>
{
    public async Task HandleAsync(MatchResultCorrected domainEvent, CancellationToken cancellationToken)
    {
        var home = await standings.GetRequiredAsync(domainEvent.HomeTeamId, cancellationToken);
        var away = await standings.GetRequiredAsync(domainEvent.AwayTeamId, cancellationToken);

        home.RevertResult(domainEvent.PreviousHomeScore, domainEvent.PreviousAwayScore);
        away.RevertResult(domainEvent.PreviousAwayScore, domainEvent.PreviousHomeScore);

        home.RecordResult(domainEvent.NewHomeScore, domainEvent.NewAwayScore);
        away.RecordResult(domainEvent.NewAwayScore, domainEvent.NewHomeScore);
    }
}
