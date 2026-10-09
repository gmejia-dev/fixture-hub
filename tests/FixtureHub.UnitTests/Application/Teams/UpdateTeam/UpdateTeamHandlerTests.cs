using FixtureHub.Application.Teams.UpdateTeam;
using FixtureHub.Domain.Teams;
using FixtureHub.UnitTests.Application.Fakes;

namespace FixtureHub.UnitTests.Application.Teams.UpdateTeam;

public class UpdateTeamHandlerTests
{
    private readonly FakeTeamRepository _teams = new();
    private readonly UpdateTeamHandler _handler;

    public UpdateTeamHandlerTests() => _handler = new UpdateTeamHandler(_teams);

    [Fact]
    public async Task HandleAsync_ValidCommand_UpdatesNameAndCountry()
    {
        var team = AddTeam("Los Halcones");

        var result = await _handler.HandleAsync(
            new UpdateTeamCommand(team.Id, "Los Pumas", "Honduras"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Los Pumas", team.Name);
        Assert.Equal("Honduras", team.Country);
    }

    [Fact]
    public async Task HandleAsync_SameNameAndNewCountry_UpdatesTheTeam()
    {
        var team = AddTeam("Los Halcones");

        var result = await _handler.HandleAsync(
            new UpdateTeamCommand(team.Id, "Los Halcones", "Honduras"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Honduras", team.Country);
    }

    [Fact]
    public async Task HandleAsync_NameOfAnotherTeam_ReturnsNameTaken()
    {
        AddTeam("Los Pumas");
        var team = AddTeam("Los Halcones");

        var result = await _handler.HandleAsync(
            new UpdateTeamCommand(team.Id, "Los Pumas", "El Salvador"),
            CancellationToken.None);

        Assert.Equal("Team.NameTaken", result.Error.Code);
        Assert.Equal("Los Pumas", result.Error.Metadata?["name"]);
    }

    [Fact]
    public async Task HandleAsync_DataRejectedByTheDomain_ReturnsTheDomainErrorAndKeepsTheTeam()
    {
        var team = AddTeam("Los Halcones");

        var result = await _handler.HandleAsync(
            new UpdateTeamCommand(team.Id, " ", "Honduras"),
            CancellationToken.None);

        Assert.Equal(TeamErrors.NameRequired, result.Error);
        Assert.Equal("Los Halcones", team.Name);
    }

    [Fact]
    public async Task HandleAsync_UnknownTeam_ReturnsTeamNotFound()
    {
        var teamId = Guid.NewGuid();

        var result = await _handler.HandleAsync(
            new UpdateTeamCommand(teamId, "Los Halcones", "El Salvador"),
            CancellationToken.None);

        Assert.Equal("Team.NotFound", result.Error.Code);
        Assert.Equal(teamId, result.Error.Metadata?["teamId"]);
    }

    private Team AddTeam(string name)
    {
        var team = Team.Create(name, "El Salvador").Value;
        _teams.Add(team);
        return team;
    }
}
