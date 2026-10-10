using FixtureHub.Application.Teams.GetTeamById;
using FixtureHub.UnitTests.Application.Fakes;

namespace FixtureHub.UnitTests.Application.Teams.GetTeamById;

public class GetTeamByIdHandlerTests
{
    private readonly FakeTeamReader _reader = new();

    [Fact]
    public async Task HandleAsync_ExistingTeam_ReturnsItsDetails()
    {
        var team = new TeamDetails(Guid.NewGuid(), "Los Halcones", "El Salvador", []);
        _reader.Team = team;

        var result = await new GetTeamByIdHandler(_reader).HandleAsync(new GetTeamByIdQuery(team.Id), CancellationToken.None);

        Assert.Same(team, result.Value);
    }

    [Fact]
    public async Task HandleAsync_UnknownTeam_ReturnsTeamNotFound()
    {
        var teamId = Guid.NewGuid();

        var result = await new GetTeamByIdHandler(_reader).HandleAsync(new GetTeamByIdQuery(teamId), CancellationToken.None);

        Assert.Equal("Team.NotFound", result.Error.Code);
        Assert.Equal(teamId, result.Error.Metadata?["teamId"]);
    }
}
