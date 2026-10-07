using FixtureHub.Domain.Teams;

namespace FixtureHub.UnitTests.Domain.Teams;

public class TeamTests
{
    [Fact]
    public void Create_WithValidData_ReturnsTeamWithTrimmedValues()
    {
        var result = Team.Create("  Los Halcones  ", "  El Salvador ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Los Halcones", result.Value.Name);
        Assert.Equal("El Salvador", result.Value.Country);
        Assert.False(result.Value.IsDeleted);
    }

    [Fact]
    public void Create_WithValidData_RaisesTeamCreated()
    {
        var team = Team.Create("Los Halcones", "El Salvador").Value;

        var domainEvent = Assert.IsType<TeamCreated>(Assert.Single(team.DomainEvents));
        Assert.Equal(team.Id, domainEvent.TeamId);
        Assert.Equal("Los Halcones", domainEvent.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ReturnsNameRequired(string name)
    {
        var result = Team.Create(name, "El Salvador");

        Assert.True(result.IsFailure);
        Assert.Equal(TeamErrors.NameRequired, result.Error);
    }

    [Fact]
    public void Create_WithNameTooLong_ReturnsNameTooLong()
    {
        var name = new string('A', Team.NameMaxLength + 1);

        var result = Team.Create(name, "El Salvador");

        Assert.Equal(TeamErrors.NameTooLong, result.Error);
    }

    [Fact]
    public void Create_WithBlankCountry_ReturnsCountryRequired()
    {
        var result = Team.Create("Los Halcones", " ");

        Assert.Equal(TeamErrors.CountryRequired, result.Error);
    }

    [Fact]
    public void Update_WithValidData_ChangesNameAndCountry()
    {
        var team = CreateTeam();

        var result = team.Update("Halcones FC", "Guatemala");

        Assert.True(result.IsSuccess);
        Assert.Equal("Halcones FC", team.Name);
        Assert.Equal("Guatemala", team.Country);
    }

    [Fact]
    public void Update_WithBlankName_KeepsOriginalValues()
    {
        var team = CreateTeam();

        var result = team.Update(" ", "Guatemala");

        Assert.Equal(TeamErrors.NameRequired, result.Error);
        Assert.Equal("Los Halcones", team.Name);
        Assert.Equal("El Salvador", team.Country);
    }

    [Fact]
    public void Delete_CalledTwice_KeepsTeamDeleted()
    {
        var team = CreateTeam();

        team.Delete();
        team.Delete();

        Assert.True(team.IsDeleted);
    }

    [Fact]
    public void AddPlayer_WithValidData_AddsPlayerToTheTeam()
    {
        var team = CreateTeam();

        var result = team.AddPlayer("  Jorge González ", 10);

        Assert.True(result.IsSuccess);
        var player = Assert.Single(team.Players);
        Assert.Same(result.Value, player);
        Assert.Equal("Jorge González", player.Name);
        Assert.Equal(10, player.ShirtNumber);
        Assert.Equal(team.Id, player.TeamId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void AddPlayer_WithShirtNumberOutOfRange_ReturnsInvalidShirtNumber(int shirtNumber)
    {
        var team = CreateTeam();

        var result = team.AddPlayer("Jorge González", shirtNumber);

        Assert.Equal(PlayerErrors.InvalidShirtNumber, result.Error);
        Assert.Empty(team.Players);
    }

    [Fact]
    public void AddPlayer_WithTakenShirtNumber_ReturnsShirtNumberTaken()
    {
        var team = CreateTeam();
        team.AddPlayer("Jorge González", 10);

        var result = team.AddPlayer("Mauricio Cienfuegos", 10);

        Assert.Equal("Player.ShirtNumberTaken", result.Error.Code);
        Assert.Single(team.Players);
    }

    [Fact]
    public void AddPlayer_WithShirtNumberOfRemovedPlayer_AddsPlayer()
    {
        var team = CreateTeam();
        var removed = team.AddPlayer("Jorge González", 10).Value;
        team.RemovePlayer(removed.Id);

        var result = team.AddPlayer("Mauricio Cienfuegos", 10);

        Assert.True(result.IsSuccess);
        Assert.Single(team.Players);
    }

    [Fact]
    public void UpdatePlayer_KeepingTheirOwnShirtNumber_Succeeds()
    {
        var team = CreateTeam();
        var player = team.AddPlayer("Jorge González", 10).Value;

        var result = team.UpdatePlayer(player.Id, "Jorge 'Mágico' González", 10);

        Assert.True(result.IsSuccess);
        Assert.Equal("Jorge 'Mágico' González", player.Name);
    }

    [Fact]
    public void UpdatePlayer_WithShirtNumberOfAnotherPlayer_ReturnsShirtNumberTaken()
    {
        var team = CreateTeam();
        team.AddPlayer("Jorge González", 10);
        var player = team.AddPlayer("Mauricio Cienfuegos", 8).Value;

        var result = team.UpdatePlayer(player.Id, "Mauricio Cienfuegos", 10);

        Assert.Equal("Player.ShirtNumberTaken", result.Error.Code);
        Assert.Equal(8, player.ShirtNumber);
    }

    [Fact]
    public void UpdatePlayer_WithUnknownPlayer_ReturnsPlayerNotFound()
    {
        var team = CreateTeam();
        var unknownId = Guid.NewGuid();

        var result = team.UpdatePlayer(unknownId, "Jorge González", 10);

        Assert.Equal("Player.NotFound", result.Error.Code);
        Assert.Equal(unknownId, result.Error.Metadata?["playerId"]);
    }

    [Fact]
    public void RemovePlayer_CalledTwice_Succeeds()
    {
        var team = CreateTeam();
        var player = team.AddPlayer("Jorge González", 10).Value;

        team.RemovePlayer(player.Id);
        var result = team.RemovePlayer(player.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(team.Players);
    }

    [Fact]
    public void RemovePlayer_WithUnknownPlayer_ReturnsPlayerNotFound()
    {
        var team = CreateTeam();

        var result = team.RemovePlayer(Guid.NewGuid());

        Assert.Equal("Player.NotFound", result.Error.Code);
    }

    private static Team CreateTeam() => Team.Create("Los Halcones", "El Salvador").Value;
}