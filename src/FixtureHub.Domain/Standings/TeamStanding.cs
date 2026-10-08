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

    public int Won { get; private set; }

    public int Drawn { get; private set; }

    public int Lost { get; private set; }

    public int GoalsFor { get; private set; }

    public int GoalsAgainst { get; private set; }

    public int GoalDifference { get; private set; }

    public int Points { get; private set; }

    public static TeamStanding Create(Guid teamId) => new(teamId);

    public void RecordResult(int goalsFor, int goalsAgainst) => Apply(goalsFor, goalsAgainst, direction: 1);

    public void RevertResult(int goalsFor, int goalsAgainst) => Apply(goalsFor, goalsAgainst, direction: -1);

    private void Apply(int goalsFor, int goalsAgainst, int direction)
    {
        Played += direction;
        GoalsFor += direction * goalsFor;
        GoalsAgainst += direction * goalsAgainst;

        if (goalsFor > goalsAgainst)
            Won += direction;
        else if (goalsFor == goalsAgainst)
            Drawn += direction;
        else
            Lost += direction;

        GoalDifference = GoalsFor - GoalsAgainst;
        Points = (Won * PointsPerWin) + (Drawn * PointsPerDraw);
    }
}
