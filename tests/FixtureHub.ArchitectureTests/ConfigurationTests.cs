using System.Text.Json;

namespace FixtureHub.ArchitectureTests;

public class ConfigurationTests
{
    private static readonly JsonDocumentOptions AppSettingsOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

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
}
