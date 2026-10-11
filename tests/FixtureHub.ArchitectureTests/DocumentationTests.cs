using System.Text.RegularExpressions;

namespace FixtureHub.ArchitectureTests;

public partial class DocumentationTests
{
    [Fact]
    public void ArchitectureDiagram_IsTheSameInTheReadmeAndInTheArchitectureDocument()
    {
        var architecture = FirstMermaidDiagram("docs/arquitectura.md");
        var readme = FirstMermaidDiagram("README.md");

        Assert.Contains("subgraph compose", architecture, StringComparison.Ordinal);
        Assert.True(
            readme == architecture,
            "El diagrama de arquitectura del README es distinto del de docs/arquitectura.md: copia el de docs/arquitectura.md.");
    }

    private static string FirstMermaidDiagram(string file)
    {
        var match = MermaidBlock().Match(File.ReadAllText(Repository.File(file)).ReplaceLineEndings("\n"));

        Assert.True(match.Success, $"{file} no tiene un diagrama Mermaid.");

        return match.Groups["diagram"].Value;
    }

    [GeneratedRegex(@"```mermaid\n(?<diagram>.*?)```", RegexOptions.Singleline)]
    private static partial Regex MermaidBlock();
}
