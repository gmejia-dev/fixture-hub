using FixtureHub.Application.Matches.GetMatchById;
using FixtureHub.UnitTests.Application.Fakes;

namespace FixtureHub.UnitTests.Application.Matches.GetMatchById;

public class GetMatchByIdHandlerTests
{
    private readonly FakeMatchReader _reader = new();

    [Fact]
    public async Task HandleAsync_ExistingMatch_ReturnsItsDetails()
    {
        var match = new MatchDetails(
            Guid.NewGuid(), Guid.NewGuid(), "Los Halcones", Guid.NewGuid(), "Los Pumas",
            DateTimeOffset.UnixEpoch, "Finished", 2, 1, [], []);
        _reader.Match = match;

        var result = await new GetMatchByIdHandler(_reader).HandleAsync(new GetMatchByIdQuery(match.Id), CancellationToken.None);

        Assert.Same(match, result.Value);
    }

    [Fact]
    public async Task HandleAsync_UnknownMatch_ReturnsMatchNotFound()
    {
        var matchId = Guid.NewGuid();

        var result = await new GetMatchByIdHandler(_reader).HandleAsync(new GetMatchByIdQuery(matchId), CancellationToken.None);

        Assert.Equal("Match.NotFound", result.Error.Code);
        Assert.Equal(matchId, result.Error.Metadata?["matchId"]);
    }
}
