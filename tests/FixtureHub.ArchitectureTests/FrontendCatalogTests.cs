using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Matches;

namespace FixtureHub.ArchitectureTests;

public class FrontendCatalogTests
{
    private const BindingFlags StaticMembers =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly string[] Layers =
        ["FixtureHub.Domain", "FixtureHub.Application", "FixtureHub.Infrastructure", "FixtureHub.Api"];

    private static readonly Regex Placeholder = new(@"\{(?<name>\w+)\}");

    private static readonly IReadOnlyList<DeclaredError> Errors = DeclaredErrors();

    [Theory]
    [InlineData("es")]
    [InlineData("en")]
    public void EveryErrorCode_HasATranslationInTheCatalog(string language)
    {
        var catalog = Catalog(language);

        Assert.Contains(Errors, error => error.Error.Code == "Team.NameTaken");
        Assert.Contains(Errors, error => error.Error.Code == "Validation.Failed");
        Assert.Contains(Errors, error => error.Error.Code == "Request.Malformed");

        var missing = Errors
            .Select(error => $"error.{error.Error.Code}")
            .Distinct()
            .Where(key => !catalog.ContainsKey(key))
            .ToList();

        Assert.True(missing.Count == 0, $"Faltan en {language}.json: " + string.Join(", ", missing));
    }

    [Theory]
    [InlineData("es")]
    [InlineData("en")]
    public void EveryMatchStatus_HasATranslationInTheCatalog(string language)
    {
        var catalog = Catalog(language);

        var missing = Enum.GetNames<MatchStatus>()
            .Select(status => $"status.{status}")
            .Where(key => !catalog.ContainsKey(key))
            .ToList();

        Assert.True(missing.Count == 0, $"Faltan en {language}.json: " + string.Join(", ", missing));
    }

    [Theory]
    [InlineData("es")]
    [InlineData("en")]
    public void Translations_OnlyUsePlaceholdersFromTheMetadataOfTheirError(string language)
    {
        var catalog = Catalog(language);

        var wrong = Errors
            .Where(error => catalog.ContainsKey($"error.{error.Error.Code}"))
            .SelectMany(error => PlaceholdersOf(catalog[$"error.{error.Error.Code}"])
                .Where(name => error.Error.Metadata?.ContainsKey(name) != true)
                .Select(name => $"{error.Error.Code} usa {{{name}}}"))
            .ToList();

        Assert.Contains(Errors, error => !error.IsConstant && error.Error.Metadata?.Count > 0);
        Assert.True(wrong.Count == 0, $"Marcadores sin dato en metadata ({language}): " + string.Join(", ", wrong));
    }

    [Theory]
    [InlineData("es")]
    [InlineData("en")]
    public void TranslationsOfConstantErrors_WriteTheValuesOfTheirMetadataInsteadOfPlaceholders(string language)
    {
        var catalog = Catalog(language);
        var constantErrorsWithMetadata = Errors
            .Where(error => error.IsConstant && error.Error.Metadata?.Count > 0)
            .ToList();

        Assert.Contains(constantErrorsWithMetadata, error => error.Error.Code == "Team.NameTooLong");

        var wrong = constantErrorsWithMetadata
            .Where(error => catalog.ContainsKey($"error.{error.Error.Code}"))
            .Where(error =>
            {
                var translation = catalog[$"error.{error.Error.Code}"];
                return PlaceholdersOf(translation).Any()
                    || error.Error.Metadata!.Values.Any(value => !translation.Contains(Invariant(value), StringComparison.Ordinal));
            })
            .Select(error => error.Error.Code)
            .ToList();

        Assert.True(
            wrong.Count == 0,
            $"Errores constantes que no escriben sus valores en {language}.json (llegan por campo, sin metadata): "
                + string.Join(", ", wrong));
    }

    private static Dictionary<string, string> Catalog(string language)
    {
        var json = File.ReadAllText(Repository.File($"frontend/src/i18n/messages/{language}.json"));

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;
    }

    private static IReadOnlyList<DeclaredError> DeclaredErrors() =>
        Layers
            .SelectMany(layer => Assembly.Load(layer).GetTypes())
            .Where(type => type is { IsAbstract: true, IsSealed: true } && type.Name.EndsWith("Errors", StringComparison.Ordinal))
            .SelectMany(ErrorsDeclaredIn)
            .ToList();

    private static IEnumerable<DeclaredError> ErrorsDeclaredIn(Type type)
    {
        foreach (var field in type.GetFields(StaticMembers).Where(field => field.FieldType == typeof(Error)))
            yield return new DeclaredError((Error)field.GetValue(null)!, IsConstant: true);

        foreach (var method in type.GetMethods(StaticMembers).Where(method => method.ReturnType == typeof(Error)))
        {
            var arguments = method.GetParameters().Select(parameter => SampleOf(parameter.ParameterType)).ToArray();
            yield return new DeclaredError((Error)method.Invoke(null, arguments)!, IsConstant: false);
        }
    }

    private static object? SampleOf(Type type) =>
        type == typeof(string) ? "muestra" : type.IsValueType ? Activator.CreateInstance(type) : null;

    private static IEnumerable<string> PlaceholdersOf(string translation) =>
        Placeholder.Matches(translation).Select(match => match.Groups["name"].Value);

    private static string Invariant(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

    private sealed record DeclaredError(Error Error, bool IsConstant);
}
