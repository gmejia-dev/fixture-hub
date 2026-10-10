using FixtureHub.Application.Abstractions.Messaging;

namespace FixtureHub.Application.Teams.GetTeamById;

public sealed record GetTeamByIdQuery(Guid TeamId) : IQuery<TeamDetails>;
