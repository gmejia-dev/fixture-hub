using FixtureHub.Domain.Common;

namespace FixtureHub.Domain.Standings;

public sealed class TeamStanding : AggregateRoot
{
    public const int PointsPerWin = 3;
    public const int PointsPerDraw = 1;

    private TeamStanding(Guid teamId)
        : base(teamId)
    {
    }

    public Guid TeamId => Id;

    public int Played { get; private set; }

    public int Points { get; private set; }

    public static TeamStanding Create(Guid teamId) => new(teamId);

    public void RecordResult(int goalsFor, int goalsAgainst)
    {
        Played++;

        if (goalsFor > goalsAgainst)
            Points += PointsPerWin;
        else if (goalsFor == goalsAgainst)
            Points += PointsPerDraw;
    }
}
