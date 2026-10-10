using FixtureHub.Domain.Matches;
using FixtureHub.Domain.Teams;
using FixtureHub.UnitTests.Application.Fakes;

namespace FixtureHub.UnitTests.Application.Matches;

internal sealed class MatchScenario
{
    public static readonly DateTimeOffset KickOff = new(2026, 10, 17, 15, 0, 0, TimeSpan.Zero);

    public MatchScenario()
    {
        Teams.Add(Home);
        Teams.Add(Away);
        HomePlayer = Home.AddPlayer("Jorge González", 10).Value;
        AwayPlayer = Away.AddPlayer("Carlos Ruiz", 20).Value;
    }

    public FakeTeamRepository Teams { get; } = new();

    public FakeMatchRepository Matches { get; } = new();

    public Team Home { get; } = Team.Create("Los Halcones", "El Salvador").Value;

    public Team Away { get; } = Team.Create("Los Pumas", "Guatemala").Value;

    public Player HomePlayer { get; }

    public Player AwayPlayer { get; }

    public Match AddMatch(MatchStatus status = MatchStatus.Scheduled, DateTimeOffset? scheduledAt = null)
    {
        var match = Match.Schedule(Home.Id, Away.Id, scheduledAt ?? KickOff).Value;

        if (status is MatchStatus.InProgress or MatchStatus.Finished)
            match.Start();

        if (status is MatchStatus.Finished)
            match.Finish();

        if (status is MatchStatus.Cancelled)
            match.Cancel();

        Matches.Add(match);

        return match;
    }

    public Team AddTeam(string name)
    {
        var team = Team.Create(name, "Honduras").Value;
        Teams.Add(team);
        return team;
    }
}
