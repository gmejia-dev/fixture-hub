using FixtureHub.Application.Matches.RescheduleMatch;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Matches;

namespace FixtureHub.UnitTests.Application.Matches.RescheduleMatch;

public class RescheduleMatchHandlerTests
{
    private readonly MatchScenario _scenario = new();
    private readonly RescheduleMatchHandler _handler;

    public RescheduleMatchHandlerTests() => _handler = new RescheduleMatchHandler(_scenario.Matches);

    [Fact]
    public async Task HandleAsync_ScheduledMatch_ChangesTheDate()
    {
        var match = _scenario.AddMatch();
        var newKickOff = MatchScenario.KickOff.AddDays(1);

        var result = await HandleAsync(match.Id, newKickOff);

        Assert.True(result.IsSuccess);
        Assert.Equal(newKickOff, match.ScheduledAt);
    }

    [Fact]
    public async Task HandleAsync_SameDate_DoesNotConflictWithItself()
    {
        var match = _scenario.AddMatch();

        var result = await HandleAsync(match.Id, MatchScenario.KickOff);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_TeamAlreadyPlaysAtTheNewTime_ReturnsScheduleConflict()
    {
        var match = _scenario.AddMatch();
        var newKickOff = MatchScenario.KickOff.AddDays(1);
        _scenario.AddMatch(scheduledAt: newKickOff);

        var result = await HandleAsync(match.Id, newKickOff);

        Assert.Equal("Match.ScheduleConflict", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_MatchInProgress_ReturnsNotScheduled()
    {
        var match = _scenario.AddMatch(MatchStatus.InProgress);

        var result = await HandleAsync(match.Id, MatchScenario.KickOff.AddDays(1));

        Assert.Equal("Match.NotScheduled", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_UnknownMatch_ReturnsMatchNotFound()
    {
        var result = await HandleAsync(Guid.NewGuid(), MatchScenario.KickOff);

        Assert.Equal("Match.NotFound", result.Error.Code);
    }

    private Task<Result> HandleAsync(Guid matchId, DateTimeOffset scheduledAt) =>
        _handler.HandleAsync(new RescheduleMatchCommand(matchId, scheduledAt), CancellationToken.None);
}
