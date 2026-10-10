using FixtureHub.Application.Abstractions.Events;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Matches;

namespace FixtureHub.Application.Standings;

internal sealed class MatchFinishedHandler(ITeamStandingRepository standings) : IDomainEventHandler<MatchFinished>
{
    public async Task HandleAsync(MatchFinished domainEvent, CancellationToken cancellationToken)
    {
        var home = await standings.GetRequiredAsync(domainEvent.HomeTeamId, cancellationToken);
        var away = await standings.GetRequiredAsync(domainEvent.AwayTeamId, cancellationToken);

        home.RecordResult(domainEvent.HomeScore, domainEvent.AwayScore);
        away.RecordResult(domainEvent.AwayScore, domainEvent.HomeScore);
    }
}
