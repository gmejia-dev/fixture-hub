using System.Net.Http.Json;
using FixtureHub.Application.Matches.GetMatches;
using FixtureHub.Application.Pagination;
using FixtureHub.Application.Standings.GetStandings;
using FixtureHub.Application.Teams.GetTeams;
using FixtureHub.Application.TopScorers.GetTopScorers;
using FixtureHub.Infrastructure;
using FixtureHub.IntegrationTests.Api;

namespace FixtureHub.IntegrationTests.Seeding;

[Collection(nameof(SqlServerCollection))]
public sealed class SeedTests(SqlServerFixture database)
{
    [Fact]
    public async Task Startup_WithMigrationsAndSeedOnAnEmptyDatabase_ServesTheSeededTournament()
    {
        await using var api = NewApiOnAnEmptyDatabase();
        var client = api.CreateClient();

        var standings = await client.GetFromJsonAsync<PagedResult<StandingItem>>("/api/standings");
        var scorers = await client.GetFromJsonAsync<PagedResult<TopScorerItem>>("/api/top-scorers?pageSize=3");
        var matches = await client.GetFromJsonAsync<PagedResult<MatchSummary>>("/api/matches?pageSize=100");

        Assert.Equal(
            [
                ("Halcones de Sistemas", 7, 4),
                ("Catrachos de Operaciones", 7, 3),
                ("Quetzales de Finanzas", 5, 2),
                ("Ticos de Ventas", 3, -3),
                ("Canaleros de Logística", 2, -1),
                ("Pinoleros de Auditoría", 0, -5)
            ],
            standings!.Items.Select(row => (row.TeamName, row.Points, row.GoalDifference)));
        Assert.Equal(
            [(1, "Andrés Flores", 6), (1, "Jorge Banegas", 6), (3, "Pablo Juárez", 3)],
            scorers!.Items.Select(scorer => (scorer.Position, scorer.PlayerName, scorer.Goals)));
        Assert.Equal(
            [("Cancelled", 1), ("Finished", 9), ("InProgress", 1), ("Scheduled", 4)],
            matches!.Items.GroupBy(match => match.Status).Select(group => (group.Key, group.Count())).Order());
    }

    [Fact]
    public async Task Seed_OnADatabaseWithData_DoesNothing()
    {
        await using var api = NewApiOnAnEmptyDatabase();
        var client = api.CreateClient();

        await api.Services.InitializeDatabaseAsync(applyMigrations: false, seed: true);

        var teams = await client.GetFromJsonAsync<PagedResult<TeamSummary>>("/api/teams");
        Assert.Equal(6, teams!.TotalRecords);
        Assert.All(teams.Items, team => Assert.Equal(7, team.PlayerCount));
    }

    private FixtureHubApiFactory NewApiOnAnEmptyDatabase() =>
        new(
            database.ConnectionStringFor($"FixtureHubSeed{Guid.NewGuid():N}"),
            new Dictionary<string, string>
            {
                ["Database:ApplyMigrations"] = "true",
                ["Database:Seed"] = "true"
            });
}
