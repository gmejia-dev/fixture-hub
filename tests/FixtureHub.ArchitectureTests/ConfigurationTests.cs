using System.Text.Json;

namespace FixtureHub.ArchitectureTests;

public class ConfigurationTests
{
    private static readonly JsonDocumentOptions AppSettingsOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly string[] SecretKeys = ["Admin:Password", "Jwt:SigningKey"];

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Development.json")]
    public void AppSettingsFile_IsValidJson(string file)
    {
        var json = File.ReadAllText(Repository.File($"src/FixtureHub.Api/{file}"));

        var exception = Record.Exception(() => JsonDocument.Parse(json, AppSettingsOptions).Dispose());

        Assert.True(exception is null, $"{file} no es JSON válido: {exception?.Message}");
    }

    [Fact]
    public void DevelopmentConnectionString_PointsToLocalDbWithoutAPassword()
    {
        using var settings = JsonDocument.Parse(
            File.ReadAllText(Repository.File("src/FixtureHub.Api/appsettings.Development.json")),
            AppSettingsOptions);

        var connectionString = settings.RootElement.GetProperty("ConnectionStrings").GetProperty("FixtureHub").GetString();

        Assert.StartsWith(@"Server=(localdb)\MSSQLLocalDB;", connectionString, StringComparison.Ordinal);
        Assert.DoesNotContain("Password", connectionString, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Development.json")]
    public void AppSettingsFile_DoesNotContainSecrets(string file)
    {
        using var settings = JsonDocument.Parse(File.ReadAllText(Repository.File($"src/FixtureHub.Api/{file}")), AppSettingsOptions);

        var keys = Keys(settings.RootElement, prefix: null).ToList();

        Assert.Contains("Database:Seed", keys);
        Assert.All(
            SecretKeys,
            secret => Assert.DoesNotContain(keys, key => string.Equals(key, secret, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void HttpFile_DoesNotContainTheAdminPassword()
    {
        var lines = File.ReadAllLines(Repository.File("src/FixtureHub.Api/FixtureHub.Api.http"));

        var password = Assert.Single(lines, line => line.StartsWith("@adminPassword", StringComparison.Ordinal));

        Assert.True(
            string.IsNullOrWhiteSpace(password.Split('=', 2)[1]),
            "FixtureHub.Api.http tiene la contraseña del administrador: déjala vacía antes de hacer commit.");
    }

    private static IEnumerable<string> Keys(JsonElement element, string? prefix) =>
        element.ValueKind == JsonValueKind.Object
            ? element.EnumerateObject().SelectMany(property =>
                Keys(property.Value, prefix is null ? property.Name : $"{prefix}:{property.Name}"))
            : prefix is null ? [] : [prefix];
}
