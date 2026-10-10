using FixtureHub.Application.Matches.FinishMatch;
using FixtureHub.Domain.Matches;

namespace FixtureHub.UnitTests.Application.Matches.FinishMatch;

public class FinishMatchHandlerTests
{
    private readonly MatchScenario _scenario = new();
    private readonly FinishMatchHandler _handler;

    public FinishMatchHandlerTests() => _handler = new FinishMatchHandler(_scenario.Matches);

    [Fact]
    public async Task HandleAsync_MatchInProgress_FinishesIt()
    {
        var match = _scenario.AddMatch(MatchStatus.InProgress);

        var result = await _handler.HandleAsync(new FinishMatchCommand(match.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchStatus.Finished, match.Status);
    }

    [Fact]
    public async Task HandleAsync_ScheduledMatch_ReturnsInvalidStatusTransition()
    {
        var match = _scenario.AddMatch();

        var result = await _handler.HandleAsync(new FinishMatchCommand(match.Id), CancellationToken.None);

        Assert.Equal("Match.InvalidStatusTransition", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_UnknownMatch_ReturnsMatchNotFound()
    {
        var result = await _handler.HandleAsync(new FinishMatchCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal("Match.NotFound", result.Error.Code);
    }
}
