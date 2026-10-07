using FixtureHub.Domain.Common;

namespace FixtureHub.Domain.Teams;

public static class PlayerErrors
{
    public static readonly Error NameRequired =
        Error.Validation("Player.NameRequired", "El nombre del jugador es obligatorio.");

    public static readonly Error NameTooLong =
        Error.Validation(
            "Player.NameTooLong",
            $"El nombre del jugador no puede superar {Player.NameMaxLength} caracteres.",
            new Dictionary<string, object?> { ["maxLength"] = Player.NameMaxLength });

    public static readonly Error InvalidShirtNumber =
        Error.Validation(
            "Player.InvalidShirtNumber",
            $"El dorsal debe estar entre {Player.MinShirtNumber} y {Player.MaxShirtNumber}.",
            new Dictionary<string, object?>
            {
                ["min"] = Player.MinShirtNumber,
                ["max"] = Player.MaxShirtNumber
            });

    public static Error ShirtNumberTaken(int shirtNumber) =>
        Error.Conflict(
            "Player.ShirtNumberTaken",
            $"El dorsal {shirtNumber} ya pertenece a otro jugador del equipo.",
            new Dictionary<string, object?> { ["shirtNumber"] = shirtNumber });

    public static Error NotFound(Guid playerId) =>
        Error.NotFound(
            "Player.NotFound",
            "El jugador no existe en este equipo.",
            new Dictionary<string, object?> { ["playerId"] = playerId });
}