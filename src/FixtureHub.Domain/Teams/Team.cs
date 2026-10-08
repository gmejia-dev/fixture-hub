using FixtureHub.Domain.Common;

namespace FixtureHub.Domain.Teams;

public sealed class Team : AggregateRoot, ISoftDeletable
{
    public const int NameMaxLength = 100;
    public const int CountryMaxLength = 60;

    private readonly List<Player> _players = [];

    private Team(Guid id, string name, string country)
        : base(id)
    {
        Name = name;
        Country = country;
    }

    public string Name { get; private set; }

    public string Country { get; private set; }

    public bool IsDeleted { get; private set; }

    public IReadOnlyCollection<Player> Players => [.. _players.Where(p => !p.IsDeleted)];

    public static Result<Team> Create(string name, string country)
    {
        var validation = Validate(name, country);
        if (validation.IsFailure)
            return validation.Error;

        var team = new Team(Guid.CreateVersion7(), name.Trim(), country.Trim());
        team.Raise(new TeamCreated(team.Id, team.Name));

        return team;
    }

    public Result Update(string name, string country)
    {
        var validation = Validate(name, country);
        if (validation.IsFailure)
            return validation;

        Name = name.Trim();
        Country = country.Trim();

        return Result.Success();
    }

    public void Delete() => IsDeleted = true;

    public Player? FindPlayer(Guid playerId) =>
        _players.FirstOrDefault(p => p.Id == playerId && !p.IsDeleted);

    public Result<Player> AddPlayer(string name, int shirtNumber)
    {
        var validation = Player.Validate(name, shirtNumber);
        if (validation.IsFailure)
            return validation.Error;

        if (IsShirtNumberTaken(shirtNumber))
            return PlayerErrors.ShirtNumberTaken(shirtNumber);

        var player = Player.Create(Id, name, shirtNumber);
        _players.Add(player);

        return player;
    }

    public Result UpdatePlayer(Guid playerId, string name, int shirtNumber)
    {
        var player = FindPlayer(playerId);
        if (player is null)
            return PlayerErrors.NotFound(playerId);

        var validation = Player.Validate(name, shirtNumber);
        if (validation.IsFailure)
            return validation;

        if (IsShirtNumberTaken(shirtNumber, exceptPlayerId: playerId))
            return PlayerErrors.ShirtNumberTaken(shirtNumber);

        player.Update(name, shirtNumber);

        return Result.Success();
    }

    public Result RemovePlayer(Guid playerId)
    {
        var player = _players.FirstOrDefault(p => p.Id == playerId);
        if (player is null)
            return PlayerErrors.NotFound(playerId);

        player.Delete();

        return Result.Success();
    }

    private static Result Validate(string name, string country)
    {
        if (string.IsNullOrWhiteSpace(name))
            return TeamErrors.NameRequired;

        if (name.Trim().Length > NameMaxLength)
            return TeamErrors.NameTooLong;

        if (string.IsNullOrWhiteSpace(country))
            return TeamErrors.CountryRequired;

        if (country.Trim().Length > CountryMaxLength)
            return TeamErrors.CountryTooLong;

        return Result.Success();
    }

    private bool IsShirtNumberTaken(int shirtNumber, Guid? exceptPlayerId = null) =>
        _players.Any(p => !p.IsDeleted && p.ShirtNumber == shirtNumber && p.Id != exceptPlayerId);
}
