using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace FixtureHub.ArchitectureTests;

public class LoggingTemplateTests
{
    private const BindingFlags AllMethods =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    private static readonly string[] Layers = ["FixtureHub.Application", "FixtureHub.Infrastructure", "FixtureHub.Api"];

    private static readonly string[] ReservedProperties = ["EventId", "SourceContext", "Scope", "UserId", "Route"];

    private static readonly Regex Placeholder = new(@"\{@?(?<name>[A-Za-z]+)");

    [Fact]
    public void LogTemplates_DoNotUsePropertyNamesReservedByTheLoggingPipeline()
    {
        var templates = Layers
            .SelectMany(layer => Assembly.Load(layer).GetTypes())
            .SelectMany(type => type.GetMethods(AllMethods))
            .Select(method => method.GetCustomAttribute<LoggerMessageAttribute>()?.Message)
            .OfType<string>()
            .ToList();

        Assert.Contains("Ejecutando el comando {Command}", templates);

        var wrong = templates
            .Where(template => Placeholder.Matches(template)
                .Any(match => ReservedProperties.Contains(match.Groups["name"].Value)))
            .ToList();

        Assert.True(wrong.Count == 0, "Plantillas de log con nombres reservados: " + string.Join(" | ", wrong));
    }
}
