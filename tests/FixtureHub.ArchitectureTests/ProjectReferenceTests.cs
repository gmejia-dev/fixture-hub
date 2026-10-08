using System.Xml.Linq;

namespace FixtureHub.ArchitectureTests;

public class ProjectReferenceTests
{
    [Theory]
    [InlineData("FixtureHub.Domain", new string[0])]
    [InlineData("FixtureHub.Application", new[] { "FixtureHub.Domain" })]
    [InlineData("FixtureHub.Infrastructure", new[] { "FixtureHub.Application" })]
    [InlineData("FixtureHub.Api", new[] { "FixtureHub.Application", "FixtureHub.Infrastructure" })]
    public void EachLayer_ReferencesExactlyTheProjectsItShould(string project, string[] expected)
    {
        var declared = Csproj(project)
            .Descendants("ProjectReference")
            .Select(reference => Path.GetFileNameWithoutExtension(
                (reference.Attribute("Include")?.Value ?? string.Empty).Replace('\\', '/')));

        Assert.Equal(expected.Order(StringComparer.Ordinal), declared.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Domain_DoesNotReferenceAnyNuGetPackage()
    {
        var packages = Csproj("FixtureHub.Domain")
            .Descendants("PackageReference")
            .Select(package => package.Attribute("Include")?.Value)
            .ToList();

        Assert.True(packages.Count == 0, "Domain no puede referenciar paquetes. Encontrados: " + string.Join(", ", packages));
    }

    private static XDocument Csproj(string project) =>
        XDocument.Load(Repository.File($"src/{project}/{project}.csproj"));
}
