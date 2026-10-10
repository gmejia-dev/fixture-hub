using FixtureHub.Application.Teams.UpdatePlayer;
using FixtureHub.Domain.Teams;
using FixtureHub.UnitTests.Application.Fakes;

namespace FixtureHub.UnitTests.Application.Teams.UpdatePlayer;

public class UpdatePlayerHandlerTests
{
    private readonly FakeTeamRepository _teams = new();
    private readonly Team _team = Team.Create("Los Halcones", "El Salvador").Value;
    private readonly UpdatePlayerHandler _handler;

    public UpdatePlayerHandlerTests()
    {
        _teams.Add(_team);
        _handler = new UpdatePlayerHandler(_teams);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_UpdatesThePlayer()
    {
        var player = _team.AddPlayer("Jorge González", 10).Value;

        var result = await _handler.HandleAsync(
            new UpdatePlayerCommand(_team.Id, player.Id, "Jorge 'Mágico' González", 11),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Jorge 'Mágico' González", player.Name);
        Assert.Equal(11, player.ShirtNumber);
    }

    [Fact]
    public async Task HandleAsync_ShirtNumberOfAnotherPlayer_ReturnsShirtNumberTaken()
    {
        _team.AddPlayer("Jorge González", 10);
        var player = _team.AddPlayer("Mauricio Cienfuegos", 8).Value;

        var result = await _handler.HandleAsync(
            new UpdatePlayerCommand(_team.Id, player.Id, "Mauricio Cienfuegos", 10),
            CancellationToken.None);

        Assert.Equal("Player.ShirtNumberTaken", result.Error.Code);
        Assert.Equal(8, player.ShirtNumber);
    }

    [Fact]
    public async Task HandleAsync_UnknownTeam_ReturnsTeamNotFound()
    {
        var teamId = Guid.NewGuid();

        var result = await _handler.HandleAsync(
            new UpdatePlayerCommand(teamId, Guid.NewGuid(), "Jorge González", 10),
            CancellationToken.None);

        Assert.Equal("Team.NotFound", result.Error.Code);
        Assert.Equal(teamId, result.Error.Metadata?["teamId"]);
    }
}
