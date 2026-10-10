using FixtureHub.Domain.Matches;

namespace FixtureHub.Infrastructure.Seeding;

internal static class SeedData
{
    public const int CurrentRound = 4;

    public static readonly TimeSpan Offset = TimeSpan.FromHours(-6);

    public static readonly IReadOnlyList<(string Name, string Country, (int ShirtNumber, string Name)[] Players)> Teams =
    [
        ("Halcones de Sistemas", "El Salvador",
        [
            (1, "Carlos Mejía"), (4, "José Hernández"), (5, "Luis Martínez"), (8, "Diego Rivas"),
            (9, "Andrés Flores"), (10, "Mario Castillo"), (11, "Kevin Portillo")
        ]),
        ("Quetzales de Finanzas", "Guatemala",
        [
            (1, "Javier López"), (4, "Óscar Morales"), (5, "Marvin Pérez"), (8, "Edwin García"),
            (9, "Pablo Juárez"), (10, "Rodrigo Méndez"), (11, "Byron Estrada")
        ]),
        ("Catrachos de Operaciones", "Honduras",
        [
            (1, "Héctor Reyes"), (4, "Wilson Zelaya"), (5, "Denis Cruz"), (8, "Elvin Ramírez"),
            (9, "Jorge Banegas"), (10, "Alex Funes"), (11, "Erick Andino")
        ]),
        ("Ticos de Ventas", "Costa Rica",
        [
            (1, "Esteban Mora"), (4, "Randall Vargas"), (5, "Bryan Jiménez"), (8, "Daniel Solano"),
            (9, "Felipe Rojas"), (10, "Kendall Arias"), (11, "Marco Chacón")
        ]),
        ("Canaleros de Logística", "Panamá",
        [
            (1, "Ricardo Batista"), (4, "Aníbal Castro"), (5, "Erick Moreno"), (8, "Alberto Samudio"),
            (9, "Blas Torres"), (10, "Gabriel Ortega"), (11, "Rolando Herrera")
        ]),
        ("Pinoleros de Auditoría", "Nicaragua",
        [
            (1, "Juan Obando"), (4, "Manuel Rosales"), (5, "Cristian Gutiérrez"), (8, "Ariel Mendoza"),
            (9, "Henry Sequeira"), (10, "Marlon López"), (11, "Bismarck Lacayo")
        ])
    ];

    // Todos contra todos a una vuelta (5 fechas). Las fechas 1 a 3 ya se jugaron, la 4 es hoy y la 5 es la próxima semana.
    public static readonly IReadOnlyList<SeedMatch> Matches =
    [
        new(1, new TimeOnly(19, 0), 0, 5, MatchStatus.Finished, new(12, 0, 9), new(34, 0, 9), new(60, 5, 9)),
        new(1, new TimeOnly(20, 0), 1, 4, MatchStatus.Finished),
        new(1, new TimeOnly(21, 0), 2, 3, MatchStatus.Finished, new(8, 2, 9), new(41, 2, 9), new(55, 3, 9), new(77, 2, 10)),

        new(2, new TimeOnly(19, 0), 0, 4, MatchStatus.Finished, new(23, 0, 9), new(70, 4, 9)),
        new(2, new TimeOnly(20, 0), 5, 3, MatchStatus.Finished, new(15, 3, 10), new(82, 3, 9)),
        new(2, new TimeOnly(21, 0), 1, 2, MatchStatus.Finished, new(30, 1, 9), new(44, 2, 9), new(63, 1, 10), new(90, 2, 9)),

        new(3, new TimeOnly(19, 0), 0, 3, MatchStatus.Finished, new(5, 0, 9), new(50, 0, 9), new(66, 3, 4, IsOwnGoal: true)),
        new(3, new TimeOnly(20, 0), 4, 2, MatchStatus.Finished, new(18, 4, 10), new(37, 2, 9), new(88, 2, 9)),
        new(3, new TimeOnly(21, 0), 5, 1, MatchStatus.Finished, new(9, 1, 9), new(33, 5, 10), new(58, 1, 11), new(79, 1, 9)),

        new(4, new TimeOnly(18, 0), 0, 2, MatchStatus.InProgress, new(20, 0, 9), new(30, 2, 9, IsAnnulled: true)),
        new(4, new TimeOnly(19, 30), 4, 5, MatchStatus.Cancelled),
        new(4, new TimeOnly(21, 0), 3, 1, MatchStatus.Scheduled),

        new(5, new TimeOnly(19, 0), 0, 1, MatchStatus.Scheduled),
        new(5, new TimeOnly(20, 0), 2, 5, MatchStatus.Scheduled),
        new(5, new TimeOnly(21, 0), 3, 4, MatchStatus.Scheduled)
    ];
}
