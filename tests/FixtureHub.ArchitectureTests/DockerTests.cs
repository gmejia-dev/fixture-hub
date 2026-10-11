using System.Text.Json;
using System.Text.RegularExpressions;

namespace FixtureHub.ArchitectureTests;

public partial class DockerTests
{
    private const string ApiDockerfile = "src/FixtureHub.Api/Dockerfile";
    private const string FrontendDockerfile = "frontend/Dockerfile";

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

    private static List<string> BaseImages(string dockerfile) =>
        [.. File.ReadAllLines(Repository.File(dockerfile))
            .Select(line => FromInstruction().Match(line))
            .Where(match => match.Success)
            .Select(match => match.Groups["image"].Value)];

    [GeneratedRegex(@"^FROM\s+(?<image>\S+)")]
    private static partial Regex FromInstruction();

    [GeneratedRegex(@"^FROM\s+\S+\s+AS\s+final$")]
    private static partial Regex FinalStage();
}
