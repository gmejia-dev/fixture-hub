using System.Text.Json;
using System.Text.RegularExpressions;

namespace FixtureHub.ArchitectureTests;

public partial class DockerTests
{
    private const string ApiDockerfile = "src/FixtureHub.Api/Dockerfile";

    [Fact]
    public void ApiDockerfile_BuildsWithTheSdkPinnedInGlobalJson()
    {
        using var globalJson = JsonDocument.Parse(File.ReadAllText(Repository.File("global.json")));
        var sdk = globalJson.RootElement.GetProperty("sdk").GetProperty("version").GetString();

        var images = BaseImages(ApiDockerfile);

        Assert.Contains($"mcr.microsoft.com/dotnet/sdk:{sdk}", images);
    }

    [Fact]
    public void ApiDockerfile_RunsAsANonRootUser()
    {
        var lines = File.ReadAllLines(Repository.File(ApiDockerfile));

        var finalStage = lines.SkipWhile(line => !line.StartsWith("FROM mcr.microsoft.com/dotnet/aspnet:", StringComparison.Ordinal));

        Assert.Contains("USER $APP_UID", finalStage);
    }

    private static List<string> BaseImages(string dockerfile) =>
        [.. File.ReadAllLines(Repository.File(dockerfile))
            .Select(line => FromInstruction().Match(line))
            .Where(match => match.Success)
            .Select(match => match.Groups["image"].Value)];

    [GeneratedRegex(@"^FROM\s+(?<image>\S+)")]
    private static partial Regex FromInstruction();
}
