using FixtureHub.Application.Teams.CreateTeam;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Teams;
using FixtureHub.UnitTests.Application.Fakes;

namespace FixtureHub.UnitTests.Application.Teams.CreateTeam;

public class CreateTeamHandlerTests
{
    private readonly FakeTeamRepository _teams = new();
    private readonly CreateTeamHandler _handler;

    public CreateTeamHandlerTests() => _handler = new CreateTeamHandler(_teams);

    [Fact]
    public async Task HandleAsync_ValidCommand_AddsTheTeamAndReturnsItsId()
    {
        var result = await _handler.HandleAsync(
            new CreateTeamCommand("Los Halcones", "El Salvador"),
            CancellationToken.None);

        var team = Assert.Single(_teams.All);
        Assert.Equal(team.Id, result.Value);
        Assert.Equal("Los Halcones", team.Name);
        Assert.Equal("El Salvador", team.Country);
    }

    [Fact]
    public async Task HandleAsync_DataRejectedByTheDomain_ReturnsTheDomainErrorWithoutAddingTheTeam()
    {
        var result = await _handler.HandleAsync(new CreateTeamCommand(" ", "El Salvador"), CancellationToken.None);

        Assert.Equal(TeamErrors.NameRequired, result.Error);
        Assert.Empty(_teams.All);
    }

    [Fact]
    public async Task HandleAsync_NameTakenAfterTrimming_ReturnsNameTakenWithoutAddingTheTeam()
    {
        _teams.Add(CreateTeam("Los Halcones"));

        var result = await _handler.HandleAsync(
            new CreateTeamCommand("  Los Halcones  ", "Honduras"),
            CancellationToken.None);

        Assert.Equal("Team.NameTaken", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Los Halcones", result.Error.Metadata?["name"]);
        Assert.Single(_teams.All);
    }

    [Fact]
    public async Task HandleAsync_NameOfADeletedTeam_CreatesTheTeam()
    {
        var deleted = CreateTeam("Los Halcones");
        deleted.Delete();
        _teams.Add(deleted);

        var result = await _handler.HandleAsync(
            new CreateTeamCommand("Los Halcones", "Honduras"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, _teams.All.Count);
    }

    private static Team CreateTeam(string name) => Team.Create(name, "El Salvador").Value;
}
