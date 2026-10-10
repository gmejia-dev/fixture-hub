using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Matches;

namespace FixtureHub.Application.Matches.GetMatchById;

internal sealed class GetMatchByIdHandler(IMatchReader matches) : IQueryHandler<GetMatchByIdQuery, MatchDetails>
{
    public async Task<Result<MatchDetails>> HandleAsync(GetMatchByIdQuery query, CancellationToken cancellationToken)
    {
        var match = await matches.GetMatchAsync(query.MatchId, cancellationToken);
        if (match is null)
            return MatchErrors.NotFound(query.MatchId);

        return match;
    }
}
