using FixtureHub.Application.Teams.AddPlayer;
using FixtureHub.Domain.Teams;
using FixtureHub.UnitTests.Application.Fakes;

namespace FixtureHub.UnitTests.Application.Teams.AddPlayer;

public class AddPlayerHandlerTests
{
    private readonly FakeTeamRepository _teams = new();
    private readonly Team _team = Team.Create("Los Halcones", "El Salvador").Value;
    private readonly AddPlayerHandler _handler;

    public AddPlayerHandlerTests()
    {
        _teams.Add(_team);
        _handler = new AddPlayerHandler(_teams);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_AddsThePlayerAndReturnsItsId()
    {
        var result = await _handler.HandleAsync(
            new AddPlayerCommand(_team.Id, "Jorge González", 10),
            CancellationToken.None);

        var player = Assert.Single(_team.Players);
        Assert.Equal(player.Id, result.Value);
        Assert.Equal("Jorge González", player.Name);
        Assert.Equal(10, player.ShirtNumber);
    }

    [Fact]
    public async Task HandleAsync_TakenShirtNumber_ReturnsShirtNumberTaken()
    {
        _team.AddPlayer("Jorge González", 10);

        var result = await _handler.HandleAsync(
            new AddPlayerCommand(_team.Id, "Mauricio Cienfuegos", 10),
            CancellationToken.None);

        Assert.Equal("Player.ShirtNumberTaken", result.Error.Code);
        Assert.Single(_team.Players);
    }

    [Fact]
    public async Task HandleAsync_UnknownTeam_ReturnsTeamNotFound()
    {
        var teamId = Guid.NewGuid();

        var result = await _handler.HandleAsync(
            new AddPlayerCommand(teamId, "Jorge González", 10),
            CancellationToken.None);

        Assert.Equal("Team.NotFound", result.Error.Code);
        Assert.Equal(teamId, result.Error.Metadata?["teamId"]);
    }
}
