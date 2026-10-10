using FixtureHub.Domain.Common;

namespace FixtureHub.Domain.Matches;

public static class GoalErrors
{
    public static readonly Error InvalidMinute =
        Error.Validation(
            "Goal.InvalidMinute",
            $"El minuto del gol debe estar entre {Goal.MinMinute} y {Goal.MaxMinute}.",
            new Dictionary<string, object?>
            {
                ["min"] = Goal.MinMinute,
                ["max"] = Goal.MaxMinute
            });

    public static readonly Error PlayerRequired =
        Error.Validation("Goal.PlayerRequired", "El jugador que marcó el gol es obligatorio.");

    public static Error NotFound(Guid goalId) =>
        Error.NotFound(
            "Goal.NotFound",
            "El gol no existe en este partido.",
            new Dictionary<string, object?> { ["goalId"] = goalId });
}
