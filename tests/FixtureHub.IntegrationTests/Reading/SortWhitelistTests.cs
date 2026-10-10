using FixtureHub.Application.Matches.GetMatches;
using FixtureHub.Application.Teams.GetPlayers;
using FixtureHub.Application.Teams.GetTeams;
using FixtureHub.Infrastructure.Reading;

namespace FixtureHub.IntegrationTests.Reading;

public class SortWhitelistTests
{
    public static TheoryData<string, IReadOnlyList<string>, IReadOnlyDictionary<string, string>> Whitelists() => new()
    {
        { nameof(GetTeamsQuery), GetTeamsQuery.SortFields, TeamReader.TeamSortColumns },
        { nameof(GetPlayersQuery), GetPlayersQuery.SortFields, TeamReader.PlayerSortColumns },
        { nameof(GetMatchesQuery), GetMatchesQuery.SortFields, MatchReader.SortColumns }
    };

    [Theory]
    [MemberData(nameof(Whitelists))]
    public void EveryQuery_AllowsExactlyTheFieldsItsReaderCanSortBy(
        string query,
        IReadOnlyList<string> allowedByTheValidator,
        IReadOnlyDictionary<string, string> mappedByTheReader)
    {
        Assert.True(
            allowedByTheValidator.Order().SequenceEqual(mappedByTheReader.Keys.Order()),
            $"{query}: el validador permite [{string.Join(", ", allowedByTheValidator)}] "
                + $"y el lector ordena por [{string.Join(", ", mappedByTheReader.Keys)}].");
    }
}
