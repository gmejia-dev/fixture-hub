using FixtureHub.Application.Teams.RemovePlayer;
using FixtureHub.Domain.Teams;
using FixtureHub.UnitTests.Application.Fakes;

namespace FixtureHub.UnitTests.Application.Teams.RemovePlayer;

public class RemovePlayerHandlerTests
{
    private readonly FakeTeamRepository _teams = new();
    private readonly Team _team = Team.Create("Los Halcones", "El Salvador").Value;
    private readonly RemovePlayerHandler _handler;

    public RemovePlayerHandlerTests()
    {
        _teams.Add(_team);
        _handler = new RemovePlayerHandler(_teams);
    }

    [Fact]
    public async Task HandleAsync_ExistingPlayer_RemovesThePlayer()
    {
        var player = _team.AddPlayer("Jorge González", 10).Value;

        var result = await _handler.HandleAsync(
            new RemovePlayerCommand(_team.Id, player.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_team.Players);
    }

    [Fact]
    public async Task HandleAsync_UnknownPlayer_ReturnsPlayerNotFound()
    {
        var playerId = Guid.NewGuid();

        var result = await _handler.HandleAsync(
            new RemovePlayerCommand(_team.Id, playerId),
            CancellationToken.None);

        Assert.Equal("Player.NotFound", result.Error.Code);
        Assert.Equal(playerId, result.Error.Metadata?["playerId"]);
    }

    [Fact]
    public async Task HandleAsync_UnknownTeam_ReturnsTeamNotFound()
    {
        var teamId = Guid.NewGuid();

        var result = await _handler.HandleAsync(
            new RemovePlayerCommand(teamId, Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal("Team.NotFound", result.Error.Code);
        Assert.Equal(teamId, result.Error.Metadata?["teamId"]);
    }
}
