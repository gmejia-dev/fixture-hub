using FixtureHub.Domain.Teams;

namespace FixtureHub.IntegrationTests;

internal static class TestData
{
    public static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}";

    public static Team NewTeam(string? name = null) =>
        Team.Create(name ?? UniqueName("Los Halcones"), "El Salvador").Value;
}
