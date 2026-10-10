using FixtureHub.Application.Abstractions.Messaging;

namespace FixtureHub.Application.Matches.GetMatchById;

public sealed record GetMatchByIdQuery(Guid MatchId) : IQuery<MatchDetails>;
