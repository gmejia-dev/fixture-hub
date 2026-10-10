using FixtureHub.Application.Standings;
using FixtureHub.Domain.Matches;
using FixtureHub.Domain.Standings;
using FixtureHub.UnitTests.Application.Fakes;

namespace FixtureHub.UnitTests.Application.Standings;

public class MatchResultCorrectedHandlerTests
{
    private readonly FakeTeamStandingRepository _standings = new();
    private readonly TeamStanding _home = TeamStanding.Create(Guid.NewGuid());
    private readonly TeamStanding _away = TeamStanding.Create(Guid.NewGuid());

    public MatchResultCorrectedHandlerTests()
    {
        _standings.Add(_home);
        _standings.Add(_away);
    }

    [Fact]
    public async Task HandleAsync_WinCorrectedToDraw_RevertsTheWinAndRecordsTheDraw()
    {
        _home.RecordResult(2, 1);
        _away.RecordResult(1, 2);
        var corrected = new MatchResultCorrected(Guid.NewGuid(), _home.TeamId, _away.TeamId, 2, 1, 1, 1);

        await new MatchResultCorrectedHandler(_standings).HandleAsync(corrected, CancellationToken.None);

        Assert.Equal((1, 0, 1, 1, 1), (_home.Played, _home.Won, _home.Drawn, _home.Points, _home.GoalsFor));
        Assert.Equal((1, 0, 1, 1, 1), (_away.Played, _away.Lost, _away.Drawn, _away.Points, _away.GoalsFor));
    }
}
