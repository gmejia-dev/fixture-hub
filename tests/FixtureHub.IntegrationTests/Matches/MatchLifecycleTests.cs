using FixtureHub.Application.Matches.AnnulGoal;
using FixtureHub.Application.Matches.CorrectMatchResult;
using FixtureHub.Application.Matches.FinishMatch;
using FixtureHub.Application.Matches.RegisterGoal;
using FixtureHub.Application.Matches.ScheduleMatch;
using FixtureHub.Application.Matches.StartMatch;
using FixtureHub.Domain.Matches;
using FixtureHub.Domain.Teams;

namespace FixtureHub.IntegrationTests.Matches;

[Collection(nameof(SqlServerCollection))]
public sealed class MatchLifecycleTests(SqlServerFixture database)
{
    private static readonly DateTimeOffset KickOff = new(2026, 10, 17, 15, 0, 0, TimeSpan.FromHours(-6));

    [Fact]
    public async Task FullMatch_FromScheduleToFinish_IsSavedWithItsGoals()
    {
        var (home, homePlayer, away, _) = await SaveTeamsAsync();
        var matchId = await ScheduleAsync(home, away);

        await database.SendAsync(new StartMatchCommand(matchId));
        await database.SendAsync<RegisterGoalCommand, Guid>(new RegisterGoalCommand(matchId, homePlayer.Id, 10, false));
        await database.SendAsync<RegisterGoalCommand, Guid>(new RegisterGoalCommand(matchId, homePlayer.Id, 75, true));
        var finished = await database.SendAsync(new FinishMatchCommand(matchId));

        Assert.True(finished.IsSuccess);
        var match = await database.FindMatchAsync(matchId);
        Assert.NotNull(match);
        Assert.Equal(MatchStatus.Finished, match.Status);
        Assert.Equal(KickOff, match.ScheduledAt);
        Assert.Equal(1, match.HomeScore);
        Assert.Equal(1, match.AwayScore);
        Assert.Equal(2, match.Goals.Count);
        Assert.Contains(match.Goals, goal => goal.IsOwnGoal && goal.ScoringTeamId == away.Id);
    }

    [Fact]
    public async Task AnnulGoal_MatchInProgress_SavesTheGoalAsAnnulled()
    {
        var (home, homePlayer, away, _) = await SaveTeamsAsync();
        var matchId = await ScheduleAsync(home, away);
        await database.SendAsync(new StartMatchCommand(matchId));
        var goalId = (await database.SendAsync<RegisterGoalCommand, Guid>(
            new RegisterGoalCommand(matchId, homePlayer.Id, 10, false))).Value;

        var result = await database.SendAsync(new AnnulGoalCommand(matchId, goalId));

        Assert.True(result.IsSuccess);
        var match = await database.FindMatchAsync(matchId);
        Assert.True(Assert.Single(match!.Goals).IsAnnulled);
        Assert.Equal(0, match.HomeScore);
    }

    [Fact]
    public async Task CorrectMatchResult_FinishedMatch_SavesTheNewGoalsAndTheHistory()
    {
        var (home, homePlayer, away, awayPlayer) = await SaveTeamsAsync();
        var matchId = await ScheduleAsync(home, away);
        await database.SendAsync(new StartMatchCommand(matchId));
        await database.SendAsync<RegisterGoalCommand, Guid>(new RegisterGoalCommand(matchId, homePlayer.Id, 10, false));
        await database.SendAsync(new FinishMatchCommand(matchId));

        var result = await database.SendAsync(new CorrectMatchResultCommand(
            matchId,
            [new CorrectedGoal(awayPlayer.Id, 20, false), new CorrectedGoal(awayPlayer.Id, 85, false)],
            "El primer gol fue en fuera de juego"));

        Assert.True(result.IsSuccess);
        var match = await database.FindMatchAsync(matchId);
        Assert.Equal(0, match!.HomeScore);
        Assert.Equal(2, match.AwayScore);
        Assert.Equal(3, match.Goals.Count);
        Assert.Single(match.Goals, goal => goal.IsAnnulled);
        var correction = Assert.Single(match.Corrections);
        Assert.Equal(1, correction.PreviousHomeScore);
        Assert.Equal(0, correction.PreviousAwayScore);
        Assert.Equal(0, correction.NewHomeScore);
        Assert.Equal(2, correction.NewAwayScore);
    }

    private async Task<Guid> ScheduleAsync(Team home, Team away) =>
        (await database.SendAsync<ScheduleMatchCommand, Guid>(new ScheduleMatchCommand(home.Id, away.Id, KickOff))).Value;

    private async Task<(Team Home, Player HomePlayer, Team Away, Player AwayPlayer)> SaveTeamsAsync()
    {
        var home = TestData.NewTeam();
        var homePlayer = home.AddPlayer("Jorge González", 10).Value;
        var away = TestData.NewTeam(TestData.UniqueName("Los Pumas"));
        var awayPlayer = away.AddPlayer("Carlos Ruiz", 20).Value;
        await database.SaveAsync(home);
        await database.SaveAsync(away);

        return (home, homePlayer, away, awayPlayer);
    }
}
