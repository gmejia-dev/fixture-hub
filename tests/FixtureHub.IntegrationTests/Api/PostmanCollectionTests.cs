using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FixtureHub.IntegrationTests.Api;

[Collection(nameof(SqlServerCollection))]
public sealed partial class PostmanCollectionTests(SqlServerFixture database)
{
    [Fact]
    public void Collection_HasARequestForEveryEndpointOfTheApi()
    {
        var endpoints = Endpoints().ToList();
        var requests = Requests().ToHashSet();

        Assert.Contains("POST /api/matches/{}/goals/{}/annul", endpoints);
        Assert.Contains("GET /health/ready", endpoints);
        Assert.Contains("PATCH /api/matches/{}", requests);

        var missing = endpoints.Where(endpoint => !requests.Contains(endpoint)).ToList();

        Assert.True(missing.Count == 0, $"La colección de Postman no tiene un request para: {string.Join(", ", missing)}");
    }

    private IEnumerable<string> Endpoints() =>
        database.ApiServices.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .SelectMany(endpoint => (endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["GET"])
                .Select(method => $"{method} {Normalize(RouteParameter().Replace(endpoint.RoutePattern.RawText!, "{}"))}"));

    private static IEnumerable<string> Requests()
    {
        using var collection = JsonDocument.Parse(File.ReadAllText(CollectionPath()));

        return Items(collection.RootElement)
            .Select(request =>
            {
                var segments = request.GetProperty("url").GetProperty("path").EnumerateArray()
                    .Select(segment => segment.GetString()!)
                    .Select(segment => Variable().IsMatch(segment) || Guid.TryParse(segment, out _) ? "{}" : segment);

                return $"{request.GetProperty("method").GetString()} {Normalize("/" + string.Join('/', segments))}";
            })
            .ToList();
    }

    private static IEnumerable<JsonElement> Items(JsonElement node) =>
        node.TryGetProperty("request", out var request)
            ? [request]
            : node.TryGetProperty("item", out var items) ? items.EnumerateArray().SelectMany(Items) : [];

    private static string Normalize(string route) => route.TrimEnd('/');

    private static string CollectionPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FixtureHub.slnx")))
            directory = directory.Parent;

        Assert.NotNull(directory);

        return Path.Combine(directory.FullName, "postman", "FixtureHub.postman_collection.json");
    }

    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex RouteParameter();

    [GeneratedRegex(@"^\{\{[^}]+\}\}$")]
    private static partial Regex Variable();
}
