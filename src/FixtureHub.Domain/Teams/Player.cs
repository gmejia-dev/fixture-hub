using FixtureHub.Domain.Common;

namespace FixtureHub.Domain.Teams;

public sealed class Player : Entity, ISoftDeletable
{
    public const int NameMaxLength = 100;
    public const int MinShirtNumber = 1;
    public const int MaxShirtNumber = 99;

    private Player(Guid id, Guid teamId, string name, int shirtNumber)
        : base(id)
    {
        TeamId = teamId;
        Name = name;
        ShirtNumber = shirtNumber;
    }

    public Guid TeamId { get; private init; }

    public string Name { get; private set; }

    public int ShirtNumber { get; private set; }

    public bool IsDeleted { get; private set; }

    internal static Result Validate(string name, int shirtNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
            return PlayerErrors.NameRequired;

        if (name.Trim().Length > NameMaxLength)
            return PlayerErrors.NameTooLong;

        if (shirtNumber is < MinShirtNumber or > MaxShirtNumber)
            return PlayerErrors.InvalidShirtNumber;

        return Result.Success();
    }

    internal static Player Create(Guid teamId, string name, int shirtNumber) =>
        new(Guid.CreateVersion7(), teamId, name.Trim(), shirtNumber);

    internal void Update(string name, int shirtNumber)
    {
        Name = name.Trim();
        ShirtNumber = shirtNumber;
    }

    internal void Delete() => IsDeleted = true;
}