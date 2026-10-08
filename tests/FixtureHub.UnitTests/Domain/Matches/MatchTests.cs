using FixtureHub.Domain.Matches;
using FixtureHub.Domain.Teams;

namespace FixtureHub.UnitTests.Domain.Matches;

public class MatchTests
{
    private static readonly DateTimeOffset KickOff = new(2026, 10, 10, 15, 0, 0, TimeSpan.FromHours(-6));

    private readonly Team _home = Team.Create("Los Halcones", "El Salvador").Value;
    private readonly Team _away = Team.Create("Los Pumas", "Guatemala").Value;
    private readonly Player _homePlayer;
    private readonly Player _awayPlayer;

    public MatchTests()
    {
        _homePlayer = _home.AddPlayer("Jorge González", 10).Value;
        _awayPlayer = _away.AddPlayer("Carlos Ruiz", 20).Value;
    }

    [Fact]
    public void Schedule_WithDifferentTeams_CreatesScheduledMatchWithoutGoals()
    {
        var result = Match.Schedule(_home.Id, _away.Id, KickOff);

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchStatus.Scheduled, result.Value.Status);
        Assert.Equal(0, result.Value.HomeScore);
        Assert.Equal(0, result.Value.AwayScore);
        Assert.Empty(result.Value.Goals);
    }

    [Fact]
    public void Schedule_WithSameTeam_ReturnsSameTeams()
    {
        var result = Match.Schedule(_home.Id, _home.Id, KickOff);

        Assert.Equal(MatchErrors.SameTeams, result.Error);
    }

    [Fact]
    public void Schedule_WithLocalTime_StoresTheSameInstantInUtc()
    {
        var match = Match.Schedule(_home.Id, _away.Id, KickOff).Value;

        Assert.Equal(TimeSpan.Zero, match.ScheduledAt.Offset);
        Assert.Equal(KickOff, match.ScheduledAt);
    }

    [Fact]
    public void Reschedule_ScheduledMatch_ChangesTheDate()
    {
        var match = CreateMatch(MatchStatus.Scheduled);
        var newKickOff = KickOff.AddDays(1);

        var result = match.Reschedule(newKickOff);

        Assert.True(result.IsSuccess);
        Assert.Equal(newKickOff, match.ScheduledAt);
    }

    [Fact]
    public void Reschedule_MatchInProgress_ReturnsNotScheduled()
    {
        var match = CreateMatch(MatchStatus.InProgress);

        var result = match.Reschedule(KickOff.AddDays(1));

        Assert.Equal("Match.NotScheduled", result.Error.Code);
        Assert.Equal(KickOff, match.ScheduledAt);
    }

    [Fact]
    public void Start_ScheduledMatch_MovesToInProgressAndRaisesMatchStarted()
    {
        var match = CreateMatch(MatchStatus.Scheduled);

        var result = match.Start();

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchStatus.InProgress, match.Status);
        var domainEvent = Assert.IsType<MatchStarted>(Assert.Single(match.DomainEvents));
        Assert.Equal(match.Id, domainEvent.MatchId);
    }

    [Theory]
    [InlineData(MatchStatus.InProgress)]
    [InlineData(MatchStatus.Finished)]
    [InlineData(MatchStatus.Cancelled)]
    public void Start_MatchNotScheduled_ReturnsInvalidStatusTransition(MatchStatus status)
    {
        var match = CreateMatch(status);

        var result = match.Start();

        Assert.Equal("Match.InvalidStatusTransition", result.Error.Code);
        Assert.Equal(status.ToString(), result.Error.Metadata?["currentStatus"]);
        Assert.Equal(nameof(MatchStatus.InProgress), result.Error.Metadata?["targetStatus"]);
        Assert.Equal(status, match.Status);
        Assert.Empty(match.DomainEvents);
    }

    [Fact]
    public void Finish_MatchInProgress_MovesToFinishedAndRaisesMatchFinishedWithTheScore()
    {
        var match = CreateMatch(MatchStatus.InProgress);
        match.AddGoal(_homePlayer, 10, isOwnGoal: false);
        match.AddGoal(_homePlayer, 50, isOwnGoal: false);
        match.AddGoal(_awayPlayer, 80, isOwnGoal: false);
        match.ClearDomainEvents();

        var result = match.Finish();

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchStatus.Finished, match.Status);
        var domainEvent = Assert.IsType<MatchFinished>(Assert.Single(match.DomainEvents));
        Assert.Equal(_home.Id, domainEvent.HomeTeamId);
        Assert.Equal(_away.Id, domainEvent.AwayTeamId);
        Assert.Equal(2, domainEvent.HomeScore);
        Assert.Equal(1, domainEvent.AwayScore);
    }

    [Theory]
    [InlineData(MatchStatus.Scheduled)]
    [InlineData(MatchStatus.Finished)]
    [InlineData(MatchStatus.Cancelled)]
    public void Finish_MatchNotInProgress_ReturnsInvalidStatusTransition(MatchStatus status)
    {
        var match = CreateMatch(status);

        var result = match.Finish();

        Assert.Equal("Match.InvalidStatusTransition", result.Error.Code);
        Assert.Equal(status, match.Status);
    }

    [Theory]
    [InlineData(MatchStatus.Scheduled)]
    [InlineData(MatchStatus.InProgress)]
    public void Cancel_MatchScheduledOrInProgress_MovesToCancelledAndRaisesMatchCancelled(MatchStatus status)
    {
        var match = CreateMatch(status);

        var result = match.Cancel();

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchStatus.Cancelled, match.Status);
        Assert.IsType<MatchCancelled>(Assert.Single(match.DomainEvents));
    }

    [Theory]
    [InlineData(MatchStatus.Finished)]
    [InlineData(MatchStatus.Cancelled)]
    public void Cancel_MatchFinishedOrCancelled_ReturnsInvalidStatusTransition(MatchStatus status)
    {
        var match = CreateMatch(status);

        var result = match.Cancel();

        Assert.Equal("Match.InvalidStatusTransition", result.Error.Code);
        Assert.Equal(status, match.Status);
    }

    [Fact]
    public void AddGoal_ByHomePlayer_IncreasesHomeScoreAndRaisesGoalScored()
    {
        var match = CreateMatch(MatchStatus.InProgress);

        var result = match.AddGoal(_homePlayer, 23, isOwnGoal: false);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, match.HomeScore);
        Assert.Equal(0, match.AwayScore);
        var domainEvent = Assert.IsType<GoalScored>(Assert.Single(match.DomainEvents));
        Assert.Equal(result.Value.Id, domainEvent.GoalId);
        Assert.Equal(_homePlayer.Id, domainEvent.PlayerId);
        Assert.Equal(_home.Id, domainEvent.ScoringTeamId);
        Assert.Equal(1, domainEvent.HomeScore);
    }

    [Fact]
    public void AddGoal_OwnGoalByHomePlayer_CountsForTheAwayTeam()
    {
        var match = CreateMatch(MatchStatus.InProgress);

        var result = match.AddGoal(_homePlayer, 40, isOwnGoal: true);

        Assert.Equal(_away.Id, result.Value.ScoringTeamId);
        Assert.Equal(_homePlayer.Id, result.Value.PlayerId);
        Assert.Equal(0, match.HomeScore);
        Assert.Equal(1, match.AwayScore);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(121)]
    public void AddGoal_WithMinuteOutOfRange_ReturnsInvalidMinute(int minute)
    {
        var match = CreateMatch(MatchStatus.InProgress);

        var result = match.AddGoal(_homePlayer, minute, isOwnGoal: false);

        Assert.Equal(GoalErrors.InvalidMinute, result.Error);
        Assert.Empty(match.Goals);
    }

    [Fact]
    public void AddGoal_ByPlayerOfAnotherTeam_ReturnsPlayerNotInMatch()
    {
        var match = CreateMatch(MatchStatus.InProgress);
        var outsider = Team.Create("Los Toros", "Honduras").Value.AddPlayer("Wilmer Velásquez", 9).Value;

        var result = match.AddGoal(outsider, 30, isOwnGoal: false);

        Assert.Equal("Match.PlayerNotInMatch", result.Error.Code);
        Assert.Empty(match.Goals);
    }

    [Theory]
    [InlineData(MatchStatus.Scheduled)]
    [InlineData(MatchStatus.Finished)]
    [InlineData(MatchStatus.Cancelled)]
    public void AddGoal_MatchNotInProgress_ReturnsNotInProgress(MatchStatus status)
    {
        var match = CreateMatch(status);

        var result = match.AddGoal(_homePlayer, 30, isOwnGoal: false);

        Assert.Equal("Match.NotInProgress", result.Error.Code);
        Assert.Equal(status.ToString(), result.Error.Metadata?["currentStatus"]);
        Assert.Empty(match.Goals);
    }

    [Fact]
    public void AnnulGoal_MatchInProgress_DecreasesTheScoreAndRaisesGoalAnnulled()
    {
        var match = CreateMatch(MatchStatus.InProgress);
        var goal = match.AddGoal(_homePlayer, 23, isOwnGoal: false).Value;
        match.ClearDomainEvents();

        var result = match.AnnulGoal(goal.Id);

        Assert.True(result.IsSuccess);
        Assert.True(goal.IsAnnulled);
        Assert.Equal(0, match.HomeScore);
        var domainEvent = Assert.IsType<GoalAnnulled>(Assert.Single(match.DomainEvents));
        Assert.Equal(goal.Id, domainEvent.GoalId);
    }

    [Fact]
    public void AnnulGoal_CalledTwice_SucceedsWithoutRaisingASecondEvent()
    {
        var match = CreateMatch(MatchStatus.InProgress);
        var goal = match.AddGoal(_homePlayer, 23, isOwnGoal: false).Value;
        match.AnnulGoal(goal.Id);
        match.ClearDomainEvents();

        var result = match.AnnulGoal(goal.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(match.DomainEvents);
    }

    [Fact]
    public void AnnulGoal_WithUnknownGoal_ReturnsGoalNotFound()
    {
        var match = CreateMatch(MatchStatus.InProgress);

        var result = match.AnnulGoal(Guid.NewGuid());

        Assert.Equal("Goal.NotFound", result.Error.Code);
    }

    [Fact]
    public void AnnulGoal_AfterTheMatchFinished_ReturnsNotInProgress()
    {
        var match = CreateMatch(MatchStatus.InProgress);
        var goal = match.AddGoal(_homePlayer, 23, isOwnGoal: false).Value;
        match.Finish();

        var result = match.AnnulGoal(goal.Id);

        Assert.Equal("Match.NotInProgress", result.Error.Code);
        Assert.False(goal.IsAnnulled);
        Assert.Equal(1, match.HomeScore);
    }

    [Fact]
    public void CorrectResult_FinishedMatch_ReplacesTheGoalsAndRecordsTheCorrection()
    {
        var match = CreateMatch(MatchStatus.InProgress);
        var originalGoal = match.AddGoal(_homePlayer, 23, isOwnGoal: false).Value;
        match.Finish();
        GoalDetails[] correctedGoals = [new(_awayPlayer, 23, IsOwnGoal: false), new(_awayPlayer, 70, IsOwnGoal: false)];

        var result = match.CorrectResult(correctedGoals, "  El árbitro asignó el gol al equipo equivocado.  ");

        Assert.True(result.IsSuccess);
        Assert.True(originalGoal.IsAnnulled);
        Assert.Equal(0, match.HomeScore);
        Assert.Equal(2, match.AwayScore);
        var correction = Assert.Single(match.Corrections);
        Assert.Equal((1, 0), (correction.PreviousHomeScore, correction.PreviousAwayScore));
        Assert.Equal((0, 2), (correction.NewHomeScore, correction.NewAwayScore));
        Assert.Equal("El árbitro asignó el gol al equipo equivocado.", correction.Reason);
    }

    [Fact]
    public void CorrectResult_FinishedMatch_RaisesMatchResultCorrectedWithBothScores()
    {
        var match = CreateMatch(MatchStatus.InProgress);
        match.AddGoal(_homePlayer, 23, isOwnGoal: false);
        match.Finish();
        match.ClearDomainEvents();

        match.CorrectResult([new(_awayPlayer, 23, IsOwnGoal: false)], "Gol mal asignado.");

        var domainEvent = Assert.IsType<MatchResultCorrected>(Assert.Single(match.DomainEvents));
        Assert.Equal((1, 0), (domainEvent.PreviousHomeScore, domainEvent.PreviousAwayScore));
        Assert.Equal((0, 1), (domainEvent.NewHomeScore, domainEvent.NewAwayScore));
    }

    [Fact]
    public void CorrectResult_WithTheSameGoals_DoesNotChangeAnything()
    {
        var match = CreateMatch(MatchStatus.InProgress);
        var goal = match.AddGoal(_homePlayer, 23, isOwnGoal: false).Value;
        match.Finish();
        match.ClearDomainEvents();

        var result = match.CorrectResult([new(_homePlayer, 23, IsOwnGoal: false)], "Revisión sin cambios.");

        Assert.True(result.IsSuccess);
        Assert.False(goal.IsAnnulled);
        Assert.Single(match.Goals);
        Assert.Empty(match.Corrections);
        Assert.Empty(match.DomainEvents);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CorrectResult_WithBlankReason_ReturnsCorrectionReasonRequired(string reason)
    {
        var match = CreateMatch(MatchStatus.Finished);

        var result = match.CorrectResult([new(_homePlayer, 23, IsOwnGoal: false)], reason);

        Assert.Equal(MatchErrors.CorrectionReasonRequired, result.Error);
        Assert.Equal(0, match.HomeScore);
    }

    [Fact]
    public void CorrectResult_WithInvalidGoal_ReturnsTheErrorAndKeepsTheScore()
    {
        var match = CreateMatch(MatchStatus.InProgress);
        match.AddGoal(_homePlayer, 23, isOwnGoal: false);
        match.Finish();

        var result = match.CorrectResult([new(_awayPlayer, 0, IsOwnGoal: false)], "Gol mal asignado.");

        Assert.Equal(GoalErrors.InvalidMinute, result.Error);
        Assert.Equal(1, match.HomeScore);
        Assert.Empty(match.Corrections);
    }

    [Fact]
    public void CorrectResult_MatchInProgress_ReturnsNotFinished()
    {
        var match = CreateMatch(MatchStatus.InProgress);

        var result = match.CorrectResult([new(_homePlayer, 23, IsOwnGoal: false)], "Gol mal asignado.");

        Assert.Equal("Match.NotFinished", result.Error.Code);
        Assert.Empty(match.Goals);
    }

    [Theory]
    [InlineData(MatchStatus.Scheduled)]
    [InlineData(MatchStatus.Cancelled)]
    public void Delete_MatchScheduledOrCancelled_MarksItAsDeleted(MatchStatus status)
    {
        var match = CreateMatch(status);

        var result = match.Delete();

        Assert.True(result.IsSuccess);
        Assert.True(match.IsDeleted);
    }

    [Theory]
    [InlineData(MatchStatus.InProgress)]
    [InlineData(MatchStatus.Finished)]
    public void Delete_MatchInProgressOrFinished_ReturnsCannotDelete(MatchStatus status)
    {
        var match = CreateMatch(status);

        var result = match.Delete();

        Assert.Equal("Match.CannotDelete", result.Error.Code);
        Assert.False(match.IsDeleted);
    }

    [Fact]
    public void Delete_CalledTwice_Succeeds()
    {
        var match = CreateMatch(MatchStatus.Scheduled);
        match.Delete();

        var result = match.Delete();

        Assert.True(result.IsSuccess);
        Assert.True(match.IsDeleted);
    }

    private Match CreateMatch(MatchStatus status)
    {
        var match = Match.Schedule(_home.Id, _away.Id, KickOff).Value;

        if (status is MatchStatus.InProgress or MatchStatus.Finished)
            match.Start();

        if (status is MatchStatus.Finished)
            match.Finish();

        if (status is MatchStatus.Cancelled)
            match.Cancel();

        match.ClearDomainEvents();

        return match;
    }
}
