using FixtureHub.Application.Teams.DeleteTeam;
using FixtureHub.Domain.Matches;
using FixtureHub.Domain.Teams;
using FixtureHub.UnitTests.Application.Fakes;

namespace FixtureHub.UnitTests.Application.Teams.DeleteTeam;

public class DeleteTeamHandlerTests
{
    private readonly FakeTeamRepository _teams = new();
    private readonly FakeMatchRepository _matches = new();
    private readonly DeleteTeamHandler _handler;

    public DeleteTeamHandlerTests() => _handler = new DeleteTeamHandler(_teams, _matches);

    [Fact]
    public async Task HandleAsync_ExistingTeam_DeletesTheTeam()
    {
        var team = AddTeam();

        var result = await _handler.HandleAsync(new DeleteTeamCommand(team.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(team.IsDeleted);
    }

    [Fact]
    public async Task HandleAsync_AlreadyDeletedTeam_Succeeds()
    {
        var team = AddTeam();
        team.Delete();

        var result = await _handler.HandleAsync(new DeleteTeamCommand(team.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(team.IsDeleted);
    }

    [Fact]
    public async Task HandleAsync_UnknownTeam_ReturnsTeamNotFound()
    {
        var teamId = Guid.NewGuid();

        var result = await _handler.HandleAsync(new DeleteTeamCommand(teamId), CancellationToken.None);

        Assert.Equal("Team.NotFound", result.Error.Code);
        Assert.Equal(teamId, result.Error.Metadata?["teamId"]);
    }

    [Theory]
    [InlineData(MatchStatus.Scheduled)]
    [InlineData(MatchStatus.InProgress)]
    public async Task HandleAsync_TeamWithActiveMatches_ReturnsHasActiveMatches(MatchStatus status)
    {
        var team = AddTeam();
        AddMatch(team, status);

        var result = await _handler.HandleAsync(new DeleteTeamCommand(team.Id), CancellationToken.None);

        Assert.Equal("Team.HasActiveMatches", result.Error.Code);
        Assert.Equal(team.Id, result.Error.Metadata?["teamId"]);
        Assert.False(team.IsDeleted);
    }

    [Theory]
    [InlineData(MatchStatus.Finished)]
    [InlineData(MatchStatus.Cancelled)]
    public async Task HandleAsync_TeamWithPastMatches_DeletesTheTeam(MatchStatus status)
    {
        var team = AddTeam();
        AddMatch(team, status);

        var result = await _handler.HandleAsync(new DeleteTeamCommand(team.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(team.IsDeleted);
    }

    private Team AddTeam()
    {
        var team = Team.Create("Los Halcones", "El Salvador").Value;
        _teams.Add(team);
        return team;
    }

    private void AddMatch(Team team, MatchStatus status)
    {
        var match = Match.Schedule(team.Id, Guid.NewGuid(), DateTimeOffset.UtcNow).Value;

        if (status is MatchStatus.InProgress or MatchStatus.Finished)
            match.Start();

        if (status is MatchStatus.Finished)
            match.Finish();

        if (status is MatchStatus.Cancelled)
            match.Cancel();

        _matches.Add(match);
    }
}
