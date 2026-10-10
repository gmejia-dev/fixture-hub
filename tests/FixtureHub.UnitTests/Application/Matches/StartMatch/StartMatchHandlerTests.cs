using FixtureHub.Application.Matches.StartMatch;
using FixtureHub.Domain.Matches;

namespace FixtureHub.UnitTests.Application.Matches.StartMatch;

public class StartMatchHandlerTests
{
    private readonly MatchScenario _scenario = new();
    private readonly StartMatchHandler _handler;

    public StartMatchHandlerTests() => _handler = new StartMatchHandler(_scenario.Matches);

    [Fact]
    public async Task HandleAsync_ScheduledMatch_PutsItInProgress()
    {
        var match = _scenario.AddMatch();

        var result = await _handler.HandleAsync(new StartMatchCommand(match.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchStatus.InProgress, match.Status);
    }

    [Fact]
    public async Task HandleAsync_FinishedMatch_ReturnsInvalidStatusTransition()
    {
        var match = _scenario.AddMatch(MatchStatus.Finished);

        var result = await _handler.HandleAsync(new StartMatchCommand(match.Id), CancellationToken.None);

        Assert.Equal("Match.InvalidStatusTransition", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_UnknownMatch_ReturnsMatchNotFound()
    {
        var matchId = Guid.NewGuid();

        var result = await _handler.HandleAsync(new StartMatchCommand(matchId), CancellationToken.None);

        Assert.Equal("Match.NotFound", result.Error.Code);
        Assert.Equal(matchId, result.Error.Metadata?["matchId"]);
    }
}
