using FixtureHub.Domain.Common;

namespace FixtureHub.Domain.Matches;

public sealed class MatchResultCorrection : Entity
{
    public const int ReasonMaxLength = 500;

    private MatchResultCorrection(
        Guid id,
        Guid matchId,
        int previousHomeScore,
        int previousAwayScore,
        int newHomeScore,
        int newAwayScore,
        string reason)
        : base(id)
    {
        MatchId = matchId;
        PreviousHomeScore = previousHomeScore;
        PreviousAwayScore = previousAwayScore;
        NewHomeScore = newHomeScore;
        NewAwayScore = newAwayScore;
        Reason = reason;
    }

    public Guid MatchId { get; private init; }

    public int PreviousHomeScore { get; private init; }

    public int PreviousAwayScore { get; private init; }

    public int NewHomeScore { get; private init; }

    public int NewAwayScore { get; private init; }

    public string Reason { get; private init; }

    internal static MatchResultCorrection Create(
        Guid matchId,
        int previousHomeScore,
        int previousAwayScore,
        int newHomeScore,
        int newAwayScore,
        string reason) =>
        new(Guid.CreateVersion7(), matchId, previousHomeScore, previousAwayScore, newHomeScore, newAwayScore, reason);
}