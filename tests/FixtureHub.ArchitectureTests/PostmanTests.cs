using System.Text.Json;
using System.Text.RegularExpressions;

namespace FixtureHub.ArchitectureTests;

public class PostmanTests
{
    private const string CollectionFile = "postman/FixtureHub.postman_collection.json";
    private const string EnvironmentFile = "postman/FixtureHub.postman_environment.json";

    [Fact]
    public void Environment_HasOnlyTheDemoAdminPasswordOfTheCompose()
    {
        using var environment = JsonDocument.Parse(File.ReadAllText(Repository.File(EnvironmentFile)));
        var demoPassword = Regex.Match(
            File.ReadAllText(Repository.File("docker-compose.yml")),
            @"Admin__Password:\s*\$\{Admin__Password:-(?<value>[^}]+)\}").Groups["value"].Value;

        var password = Variable(environment.RootElement.GetProperty("values"), "adminPassword");

        Assert.NotEmpty(demoPassword);
        Assert.Equal("secret", password.GetProperty("type").GetString());
        Assert.True(
            password.GetProperty("value").GetString() == demoPassword,
            "El environment de Postman debe tener la contraseña de demo del docker-compose, nunca una real: restáurala antes de hacer commit.");
    }

    [Fact]
    public void Environment_PointsToTheSameAddressAsDotnetRunTheComposeAndTheHttpFile()
    {
        using var environment = JsonDocument.Parse(File.ReadAllText(Repository.File(EnvironmentFile)));
        using var launchSettings = JsonDocument.Parse(File.ReadAllText(Repository.File("src/FixtureHub.Api/Properties/launchSettings.json")));

        var baseUrl = Variable(environment.RootElement.GetProperty("values"), "baseUrl").GetProperty("value").GetString();
        var dotnetRun = launchSettings.RootElement.GetProperty("profiles").GetProperty("http").GetProperty("applicationUrl").GetString();

        Assert.Equal("http://localhost:5065", baseUrl);
        Assert.Equal(baseUrl, dotnetRun);
        Assert.Contains("\"127.0.0.1:5065:8080\"", File.ReadAllText(Repository.File("docker-compose.yml")), StringComparison.Ordinal);
        Assert.Contains(
            $"@host = {baseUrl}",
            File.ReadAllLines(Repository.File("src/FixtureHub.Api/FixtureHub.Api.http")));
    }

    [Fact]
    public void Collection_DoesNotContainTheValuesOfARun()
    {
        using var collection = JsonDocument.Parse(File.ReadAllText(Repository.File(CollectionFile)));

        var variables = collection.RootElement.GetProperty("variable").EnumerateArray().ToList();

        Assert.Contains(variables, variable => variable.GetProperty("key").GetString() == "accessToken");
        Assert.All(variables, variable => Assert.True(
            string.IsNullOrEmpty(variable.GetProperty("value").GetString()),
            $"La variable {variable.GetProperty("key").GetString()} de la colección tiene el valor de una corrida: déjala vacía antes de hacer commit."));
    }

    private static JsonElement Variable(JsonElement variables, string key) =>
        Assert.Single(variables.EnumerateArray(), variable => variable.GetProperty("key").GetString() == key);
}
