using FixtureHub.Domain.Common;

namespace FixtureHub.Domain.Matches;

public static class MatchErrors
{
    public static readonly Error SameTeams =
        Error.Validation("Match.SameTeams", "Un equipo no puede jugar contra sí mismo.");

    public static readonly Error HomeTeamRequired =
        Error.Validation("Match.HomeTeamRequired", "El equipo local es obligatorio.");

    public static readonly Error AwayTeamRequired =
        Error.Validation("Match.AwayTeamRequired", "El equipo visitante es obligatorio.");

    public static readonly Error ScheduledAtRequired =
        Error.Validation("Match.ScheduledAtRequired", "La fecha y hora del partido es obligatoria.");

    public static readonly Error GoalsRequired =
        Error.Validation("Match.GoalsRequired", "La lista de goles es obligatoria; puede estar vacía.");

    public static readonly Error CorrectionReasonRequired =
        Error.Validation("Match.CorrectionReasonRequired", "El motivo de la corrección es obligatorio.");

    public static readonly Error CorrectionReasonTooLong =
        Error.Validation(
            "Match.CorrectionReasonTooLong",
            $"El motivo de la corrección no puede superar {MatchResultCorrection.ReasonMaxLength} caracteres.",
            new Dictionary<string, object?> { ["maxLength"] = MatchResultCorrection.ReasonMaxLength });

    public static Error NotFound(Guid matchId) =>
        Error.NotFound(
            "Match.NotFound",
            "El partido no existe.",
            new Dictionary<string, object?> { ["matchId"] = matchId });

    public static Error ScheduleConflict(DateTimeOffset scheduledAt) =>
        Error.Conflict(
            "Match.ScheduleConflict",
            "Uno de los equipos ya tiene un partido a esa hora.",
            new Dictionary<string, object?> { ["scheduledAt"] = scheduledAt });

    public static Error PlayerNotInMatch(Guid playerId) =>
        Error.Validation(
            "Match.PlayerNotInMatch",
            "El jugador no pertenece a ninguno de los dos equipos del partido.",
            new Dictionary<string, object?> { ["playerId"] = playerId });

    public static Error InvalidStatusTransition(MatchStatus currentStatus, MatchStatus targetStatus) =>
        Error.Conflict(
            "Match.InvalidStatusTransition",
            $"Un partido en estado {currentStatus} no puede pasar a {targetStatus}.",
            new Dictionary<string, object?>
            {
                ["currentStatus"] = currentStatus.ToString(),
                ["targetStatus"] = targetStatus.ToString()
            });

    public static Error NotScheduled(MatchStatus currentStatus) =>
        Error.Conflict(
            "Match.NotScheduled",
            "Solo se puede reprogramar un partido que no ha comenzado.",
            CurrentStatus(currentStatus));

    public static Error NotInProgress(MatchStatus currentStatus) =>
        Error.Conflict(
            "Match.NotInProgress",
            "Solo se pueden registrar o anular goles en un partido en curso.",
            CurrentStatus(currentStatus));

    public static Error NotFinished(MatchStatus currentStatus) =>
        Error.Conflict(
            "Match.NotFinished",
            "Solo se puede corregir el resultado de un partido finalizado.",
            CurrentStatus(currentStatus));

    public static Error CannotDelete(MatchStatus currentStatus) =>
        Error.Conflict(
            "Match.CannotDelete",
            "Solo se puede eliminar un partido programado o cancelado.",
            CurrentStatus(currentStatus));

    private static Dictionary<string, object?> CurrentStatus(MatchStatus currentStatus) =>
        new() { ["currentStatus"] = currentStatus.ToString() };
}
