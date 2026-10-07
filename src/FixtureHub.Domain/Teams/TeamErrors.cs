using FixtureHub.Domain.Common;

namespace FixtureHub.Domain.Teams;

public static class TeamErrors
{
    public static readonly Error NameRequired =
        Error.Validation("Team.NameRequired", "El nombre del equipo es obligatorio.");

    public static readonly Error NameTooLong =
        Error.Validation(
            "Team.NameTooLong",
            $"El nombre del equipo no puede superar {Team.NameMaxLength} caracteres.",
            new Dictionary<string, object?> { ["maxLength"] = Team.NameMaxLength });

    public static readonly Error CountryRequired =
        Error.Validation("Team.CountryRequired", "El país del equipo es obligatorio.");

    public static readonly Error CountryTooLong =
        Error.Validation(
            "Team.CountryTooLong",
            $"El país del equipo no puede superar {Team.CountryMaxLength} caracteres.",
            new Dictionary<string, object?> { ["maxLength"] = Team.CountryMaxLength });

    public static Error NotFound(Guid teamId) =>
        Error.NotFound(
            "Team.NotFound",
            "El equipo no existe.",
            new Dictionary<string, object?> { ["teamId"] = teamId });
}