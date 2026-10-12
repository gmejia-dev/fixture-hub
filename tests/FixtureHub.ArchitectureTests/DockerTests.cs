using System.Text.Json;
using System.Text.RegularExpressions;

namespace FixtureHub.ArchitectureTests;

public partial class DockerTests
{
    private const string ApiDockerfile = "src/FixtureHub.Api/Dockerfile";
    private const string FrontendDockerfile = "frontend/Dockerfile";
    private const string ComposeFile = "docker-compose.yml";

    private static readonly string[] SecretVariables = ["MSSQL_SA_PASSWORD", "Admin__Password", "Jwt__SigningKey"];

    [Fact]
    public void ApiDockerfile_BuildsWithTheSdkPinnedInGlobalJson()
    {
        using var globalJson = JsonDocument.Parse(File.ReadAllText(Repository.File("global.json")));
        var sdk = globalJson.RootElement.GetProperty("sdk").GetProperty("version").GetString();

        var images = BaseImages(ApiDockerfile);

        Assert.Contains($"mcr.microsoft.com/dotnet/sdk:{sdk}", images);
    }

    [Theory]
    [InlineData(ApiDockerfile, "USER $APP_UID")]
    [InlineData(FrontendDockerfile, "USER node")]
    public void Dockerfile_FinalStage_RunsAsANonRootUser(string dockerfile, string user)
    {
        var lines = File.ReadAllLines(Repository.File(dockerfile));

        var finalStage = lines.SkipWhile(line => !FinalStage().IsMatch(line)).ToList();

        Assert.NotEmpty(finalStage);
        Assert.Contains(user, finalStage);
    }

    [Fact]
    public void FrontendDockerfile_UsesTheSameNodeImageInEveryStage()
    {
        var images = BaseImages(FrontendDockerfile);

        Assert.Equal(3, images.Count);
        Assert.Single(images.Distinct());
        Assert.Matches(@"^node:\d+\.\d+\.\d+-alpine$", images[0]);
    }

    [Fact]
    public void Compose_TakesEverySecretFromAVariableWithADemoDefault()
    {
        var compose = File.ReadAllText(Repository.File(ComposeFile));

        var secrets = SecretVariables
            .Select(name => Regex.Match(compose, $@"^\s*{name}:\s*(?<value>\S+)\s*$", RegexOptions.Multiline))
            .Append(Regex.Match(compose, "Password=(?<value>[^;]+);"))
            .ToList();

        Assert.All(secrets, secret =>
        {
            Assert.True(secret.Success, $"{ComposeFile} no define uno de los secretos esperados.");
            Assert.Matches(@"^\$\{\w+:-[^}]+\}$", secret.Groups["value"].Value);
        });
    }

    [Fact]
    public void Compose_GivesSqlServerAndTheApiTheSameDemoPassword()
    {
        var defaults = DemoDefault().Matches(File.ReadAllText(Repository.File(ComposeFile)))
            .Where(match => match.Groups["name"].Value == "SQLSERVER_SA_PASSWORD")
            .Select(match => match.Groups["value"].Value)
            .ToList();

        Assert.Equal(2, defaults.Count);
        Assert.Single(defaults.Distinct());
    }

    [Fact]
    public void Readme_DocumentsEveryDemoValueOfTheCompose()
    {
        var defaults = DemoDefault().Matches(File.ReadAllText(Repository.File(ComposeFile)))
            .Select(match => match.Groups["value"].Value)
            .Distinct()
            .ToList();
        var readme = File.ReadAllText(Repository.File("README.md"));

        Assert.Contains("admin", defaults);
        Assert.All(defaults, value => Assert.Contains($"`{value}`", readme, StringComparison.Ordinal));
    }

    [Fact]
    public void EnvExample_DocumentsEveryVariableTheComposeFileReads()
    {
        var variables = ComposeVariable().Matches(File.ReadAllText(Repository.File(ComposeFile)))
            .Select(match => match.Groups["name"].Value)
            .Distinct()
            .ToList();
        var documented = File.ReadAllLines(Repository.File(".env.example"))
            .Where(line => line.Contains('=') && !line.StartsWith('#'))
            .Select(line => line.Split('=', 2)[0])
            .ToList();

        Assert.Contains("SQLSERVER_SA_PASSWORD", variables);
        Assert.Contains("TOURNAMENT_TIME_ZONE", variables);
        Assert.All(variables, variable => Assert.Contains(variable, documented));
    }

    [Theory]
    [InlineData("Database__ApplyMigrations: \"true\"")]
    [InlineData("Database__Seed: \"true\"")]
    [InlineData("OTEL_EXPORTER_OTLP_ENDPOINT: http://jaeger:4317")]
    [InlineData("API_BASE_URL: http://api:8080")]
    public void Compose_ConfiguresTheServicesToWorkTogether(string setting)
    {
        var lines = File.ReadAllLines(Repository.File(ComposeFile)).Select(line => line.Trim());

        Assert.Contains(setting, lines);
    }

    [Theory]
    [InlineData("tests/FixtureHub.IntegrationTests/SqlServerFixture.cs", "mcr.microsoft.com/mssql/server:")]
    [InlineData("tests/FixtureHub.IntegrationTests/Observability/JaegerExportTests.cs", "jaegertracing/jaeger:")]
    public void Compose_RunsTheSameImageTheIntegrationTestsUse(string testFile, string repository)
    {
        var tested = Regex.Matches(File.ReadAllText(Repository.File(testFile)), $@"""(?<image>{Regex.Escape(repository)}[^""\s]+)""");
        var deployed = ComposeImage().Matches(File.ReadAllText(Repository.File(ComposeFile)))
            .Select(match => match.Groups["image"].Value)
            .Where(image => image.StartsWith(repository, StringComparison.Ordinal));

        Assert.Equal(Assert.Single(tested).Groups["image"].Value, Assert.Single(deployed));
    }

    private static List<string> BaseImages(string dockerfile) =>
        [.. File.ReadAllLines(Repository.File(dockerfile))
            .Select(line => FromInstruction().Match(line))
            .Where(match => match.Success)
            .Select(match => match.Groups["image"].Value)];

    [GeneratedRegex(@"^FROM\s+(?<image>\S+)")]
    private static partial Regex FromInstruction();

    [GeneratedRegex(@"^FROM\s+\S+\s+AS\s+final$")]
    private static partial Regex FinalStage();

    [GeneratedRegex(@"(?<!\$)\$\{(?<name>\w+)")]
    private static partial Regex ComposeVariable();

    [GeneratedRegex(@"(?<!\$)\$\{(?<name>\w+):-(?<value>[^}]+)\}")]
    private static partial Regex DemoDefault();

    [GeneratedRegex(@"^\s*image:\s*(?<image>\S+)\s*$", RegexOptions.Multiline)]
    private static partial Regex ComposeImage();
}
