using FixtureHub.Domain.Common;

namespace FixtureHub.Domain.Matches;

public sealed class Goal : Entity
{
    public const int MinMinute = 1;
    public const int MaxMinute = 120;

    private Goal(Guid id, Guid matchId, Guid playerId, Guid scoringTeamId, int minute, bool isOwnGoal)
        : base(id)
    {
        MatchId = matchId;
        PlayerId = playerId;
        ScoringTeamId = scoringTeamId;
        Minute = minute;
        IsOwnGoal = isOwnGoal;
    }

    public Guid MatchId { get; private init; }

    public Guid PlayerId { get; private init; }

    public Guid ScoringTeamId { get; private init; }

    public int Minute { get; private init; }

    public bool IsOwnGoal { get; private init; }

    public bool IsAnnulled { get; private set; }

    internal static Goal Create(Guid matchId, Guid playerId, Guid scoringTeamId, int minute, bool isOwnGoal) =>
        new(Guid.CreateVersion7(), matchId, playerId, scoringTeamId, minute, isOwnGoal);

    internal void Annul() => IsAnnulled = true;
}
