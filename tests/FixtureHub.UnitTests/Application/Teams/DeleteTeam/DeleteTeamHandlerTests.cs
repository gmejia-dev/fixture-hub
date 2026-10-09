using FixtureHub.Application.Teams.DeleteTeam;
using FixtureHub.Domain.Teams;
using FixtureHub.UnitTests.Application.Fakes;

namespace FixtureHub.UnitTests.Application.Teams.DeleteTeam;

public class DeleteTeamHandlerTests
{
    private readonly FakeTeamRepository _teams = new();
    private readonly DeleteTeamHandler _handler;

    public DeleteTeamHandlerTests() => _handler = new DeleteTeamHandler(_teams);

    [Fact]
    public async Task HandleAsync_ExistingTeam_DeletesTheTeam()
    {
        var team = AddTeam();

        var result = await _handler.HandleAsync(new DeleteTeamCommand(team.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(team.IsDeleted);
    }

    [Fact]
    public async Task HandleAsync_AlreadyDeletedTeam_Succeeds()
    {
        var team = AddTeam();
        team.Delete();

        var result = await _handler.HandleAsync(new DeleteTeamCommand(team.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(team.IsDeleted);
    }

    [Fact]
    public async Task HandleAsync_UnknownTeam_ReturnsTeamNotFound()
    {
        var teamId = Guid.NewGuid();

        var result = await _handler.HandleAsync(new DeleteTeamCommand(teamId), CancellationToken.None);

        Assert.Equal("Team.NotFound", result.Error.Code);
        Assert.Equal(teamId, result.Error.Metadata?["teamId"]);
    }

    private Team AddTeam()
    {
        var team = Team.Create("Los Halcones", "El Salvador").Value;
        _teams.Add(team);
        return team;
    }
}
