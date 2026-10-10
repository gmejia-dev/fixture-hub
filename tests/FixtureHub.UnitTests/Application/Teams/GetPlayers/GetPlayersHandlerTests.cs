using FixtureHub.Application.Pagination;
using FixtureHub.Application.Teams.GetPlayers;
using FixtureHub.UnitTests.Application.Fakes;

namespace FixtureHub.UnitTests.Application.Teams.GetPlayers;

public class GetPlayersHandlerTests
{
    private readonly FakeTeamReader _reader = new();

    [Fact]
    public async Task HandleAsync_ExistingTeam_ReturnsThePageOfTheReader()
    {
        var page = new PagedResult<PlayerItem>([new PlayerItem(Guid.NewGuid(), "Ana Pérez", 10)], 1, 10, 1);
        _reader.Players = page;

        var result = await new GetPlayersHandler(_reader).HandleAsync(Query(Guid.NewGuid()), CancellationToken.None);

        Assert.Same(page, result.Value);
    }

    [Fact]
    public async Task HandleAsync_UnknownTeam_ReturnsTeamNotFound()
    {
        var teamId = Guid.NewGuid();

        var result = await new GetPlayersHandler(_reader).HandleAsync(Query(teamId), CancellationToken.None);

        Assert.Equal("Team.NotFound", result.Error.Code);
        Assert.Equal(teamId, result.Error.Metadata?["teamId"]);
    }

    private static GetPlayersQuery Query(Guid teamId) => new(teamId, 1, 10, null, null, null);
}
