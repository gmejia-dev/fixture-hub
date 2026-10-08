using FixtureHub.Domain.Common;
using FixtureHub.Domain.Teams;

namespace FixtureHub.Domain.Matches;

public sealed class Match : AggregateRoot, ISoftDeletable
{
    private readonly List<Goal> _goals = [];
    private readonly List<MatchResultCorrection> _corrections = [];

    private Match(Guid id, Guid homeTeamId, Guid awayTeamId, DateTimeOffset scheduledAt)
        : base(id)
    {
        HomeTeamId = homeTeamId;
        AwayTeamId = awayTeamId;
        ScheduledAt = scheduledAt;
        Status = MatchStatus.Scheduled;
    }

    public Guid HomeTeamId { get; private init; }

    public Guid AwayTeamId { get; private init; }

    public DateTimeOffset ScheduledAt { get; private set; }

    public MatchStatus Status { get; private set; }

    public int HomeScore { get; private set; }

    public int AwayScore { get; private set; }

    public bool IsDeleted { get; private set; }

    public IReadOnlyCollection<Goal> Goals => _goals.AsReadOnly();

    public IReadOnlyCollection<MatchResultCorrection> Corrections => _corrections.AsReadOnly();

    public static Result<Match> Schedule(Guid homeTeamId, Guid awayTeamId, DateTimeOffset scheduledAt)
    {
        if (homeTeamId == awayTeamId)
            return MatchErrors.SameTeams;

        return new Match(Guid.CreateVersion7(), homeTeamId, awayTeamId, scheduledAt.ToUniversalTime());
    }

    public Result Reschedule(DateTimeOffset scheduledAt)
    {
        if (Status != MatchStatus.Scheduled)
            return MatchErrors.NotScheduled(Status);

        ScheduledAt = scheduledAt.ToUniversalTime();

        return Result.Success();
    }

    public Result Start()
    {
        var transition = TransitionTo(MatchStatus.InProgress);
        if (transition.IsFailure)
            return transition;

        Raise(new MatchStarted(Id, HomeTeamId, AwayTeamId));

        return transition;
    }

    public Result Finish()
    {
        var transition = TransitionTo(MatchStatus.Finished);
        if (transition.IsFailure)
            return transition;

        Raise(new MatchFinished(Id, HomeTeamId, AwayTeamId, HomeScore, AwayScore));

        return transition;
    }

    public Result Cancel()
    {
        var transition = TransitionTo(MatchStatus.Cancelled);
        if (transition.IsFailure)
            return transition;

        Raise(new MatchCancelled(Id));

        return transition;
    }

    public Result<Goal> AddGoal(Player scorer, int minute, bool isOwnGoal)
    {
        var validation = ValidateGoal(scorer, minute);
        if (validation.IsFailure)
            return validation.Error;

        if (Status != MatchStatus.InProgress)
            return MatchErrors.NotInProgress(Status);

        var goal = CreateGoal(scorer, minute, isOwnGoal);
        _goals.Add(goal);
        RecalculateScore();

        Raise(new GoalScored(
            Id, goal.Id, goal.PlayerId, goal.ScoringTeamId, goal.Minute, goal.IsOwnGoal, HomeScore, AwayScore));

        return goal;
    }

    public Result AnnulGoal(Guid goalId)
    {
        var goal = _goals.FirstOrDefault(g => g.Id == goalId);
        if (goal is null)
            return GoalErrors.NotFound(goalId);

        if (goal.IsAnnulled)
            return Result.Success();

        if (Status != MatchStatus.InProgress)
            return MatchErrors.NotInProgress(Status);

        goal.Annul();
        RecalculateScore();

        Raise(new GoalAnnulled(Id, goal.Id, HomeScore, AwayScore));

        return Result.Success();
    }

    public Result CorrectResult(IReadOnlyCollection<GoalDetails> goals, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return MatchErrors.CorrectionReasonRequired;

        if (reason.Trim().Length > MatchResultCorrection.ReasonMaxLength)
            return MatchErrors.CorrectionReasonTooLong;

        foreach (var goal in goals)
        {
            var validation = ValidateGoal(goal.Scorer, goal.Minute);
            if (validation.IsFailure)
                return validation;
        }

        if (Status != MatchStatus.Finished)
            return MatchErrors.NotFinished(Status);

        if (HasSameActiveGoals(goals))
            return Result.Success();

        var previousHomeScore = HomeScore;
        var previousAwayScore = AwayScore;

        foreach (var goal in _goals.Where(g => !g.IsAnnulled))
            goal.Annul();

        _goals.AddRange(goals.Select(g => CreateGoal(g.Scorer, g.Minute, g.IsOwnGoal)));
        RecalculateScore();

        _corrections.Add(MatchResultCorrection.Create(
            Id, previousHomeScore, previousAwayScore, HomeScore, AwayScore, reason.Trim()));

        Raise(new MatchResultCorrected(
            Id, HomeTeamId, AwayTeamId, previousHomeScore, previousAwayScore, HomeScore, AwayScore));

        return Result.Success();
    }

    public Result Delete()
    {
        if (IsDeleted)
            return Result.Success();

        if (Status is not (MatchStatus.Scheduled or MatchStatus.Cancelled))
            return MatchErrors.CannotDelete(Status);

        IsDeleted = true;

        return Result.Success();
    }

    private Result TransitionTo(MatchStatus targetStatus)
    {
        var isAllowed = (Status, targetStatus) switch
        {
            (MatchStatus.Scheduled, MatchStatus.InProgress) => true,
            (MatchStatus.InProgress, MatchStatus.Finished) => true,
            (MatchStatus.Scheduled or MatchStatus.InProgress, MatchStatus.Cancelled) => true,
            _ => false
        };

        if (!isAllowed)
            return MatchErrors.InvalidStatusTransition(Status, targetStatus);

        Status = targetStatus;

        return Result.Success();
    }

    private Result ValidateGoal(Player scorer, int minute)
    {
        if (minute is < Goal.MinMinute or > Goal.MaxMinute)
            return GoalErrors.InvalidMinute;

        if (scorer.TeamId != HomeTeamId && scorer.TeamId != AwayTeamId)
            return MatchErrors.PlayerNotInMatch(scorer.Id);

        return Result.Success();
    }

    private Goal CreateGoal(Player scorer, int minute, bool isOwnGoal)
    {
        var scoringTeamId = isOwnGoal ? OpponentOf(scorer.TeamId) : scorer.TeamId;

        return Goal.Create(Id, scorer.Id, scoringTeamId, minute, isOwnGoal);
    }

    private Guid OpponentOf(Guid teamId) => teamId == HomeTeamId ? AwayTeamId : HomeTeamId;

    private bool HasSameActiveGoals(IReadOnlyCollection<GoalDetails> goals)
    {
        var current = _goals
            .Where(g => !g.IsAnnulled)
            .Select(g => (g.PlayerId, g.Minute, g.IsOwnGoal))
            .Order();

        var requested = goals
            .Select(g => (PlayerId: g.Scorer.Id, g.Minute, g.IsOwnGoal))
            .Order();

        return current.SequenceEqual(requested);
    }

    private void RecalculateScore()
    {
        HomeScore = _goals.Count(g => !g.IsAnnulled && g.ScoringTeamId == HomeTeamId);
        AwayScore = _goals.Count(g => !g.IsAnnulled && g.ScoringTeamId == AwayTeamId);
    }
}
