using FixtureHub.Application.Matches.DeleteMatch;
using FixtureHub.Domain.Matches;

namespace FixtureHub.UnitTests.Application.Matches.DeleteMatch;

public class DeleteMatchHandlerTests
{
    private readonly MatchScenario _scenario = new();
    private readonly DeleteMatchHandler _handler;

    public DeleteMatchHandlerTests() => _handler = new DeleteMatchHandler(_scenario.Matches);

    [Fact]
    public async Task HandleAsync_ScheduledMatch_DeletesIt()
    {
        var match = _scenario.AddMatch();

        var result = await _handler.HandleAsync(new DeleteMatchCommand(match.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(match.IsDeleted);
    }

    [Fact]
    public async Task HandleAsync_AlreadyDeletedMatch_Succeeds()
    {
        var match = _scenario.AddMatch();
        match.Delete();

        var result = await _handler.HandleAsync(new DeleteMatchCommand(match.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_MatchInProgress_ReturnsCannotDelete()
    {
        var match = _scenario.AddMatch(MatchStatus.InProgress);

        var result = await _handler.HandleAsync(new DeleteMatchCommand(match.Id), CancellationToken.None);

        Assert.Equal("Match.CannotDelete", result.Error.Code);
        Assert.False(match.IsDeleted);
    }

    [Fact]
    public async Task HandleAsync_UnknownMatch_ReturnsMatchNotFound()
    {
        var result = await _handler.HandleAsync(new DeleteMatchCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal("Match.NotFound", result.Error.Code);
    }
}
