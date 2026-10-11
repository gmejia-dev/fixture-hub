using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using FixtureHub.Application.Authentication.Login;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FixtureHub.IntegrationTests.Api;

[Collection(nameof(SqlServerCollection))]
public sealed partial class AuthorizationTests(SqlServerFixture database)
{
    private const string LoginRoute = "/api/auth/login";

    [Fact]
    public async Task EveryWriteEndpoint_WithoutToken_Returns401Unauthenticated()
    {
        var client = database.CreateAnonymousClient();
        var writes = Endpoints().Where(endpoint => endpoint.Method != "GET" && endpoint.Route != LoginRoute).ToList();

        Assert.Contains(("POST", "/api/teams/"), writes);
        Assert.Contains(("PUT", "/api/teams/{teamId:guid}"), writes);
        Assert.Contains(("PATCH", "/api/matches/{matchId:guid}"), writes);
        Assert.Contains(("DELETE", "/api/teams/{teamId:guid}/players/{playerId:guid}"), writes);

        foreach (var (method, route) in writes)
        {
            var response = await client.SendAsync(NewRequest(method, route));

            Assert.True(
                response.StatusCode == HttpStatusCode.Unauthorized
                    && (await response.ReadProblemAsync()).ErrorCode() == "Auth.Unauthenticated",
                $"{method} {route} no exige token (respondió {(int)response.StatusCode}).");
            Assert.Equal("Bearer", Assert.Single(response.Headers.WwwAuthenticate).Scheme);
        }
    }

    [Fact]
    public async Task EveryReadEndpoint_WithoutToken_IsPublic()
    {
        var client = database.CreateAnonymousClient();
        var reads = Endpoints().Where(endpoint => endpoint.Method == "GET").ToList();

        Assert.Contains(("GET", "/api/standings"), reads);
        Assert.Contains(("GET", "/api/teams/{teamId:guid}"), reads);

        foreach (var (method, route) in reads)
        {
            var response = await client.SendAsync(NewRequest(method, route));

            Assert.True(
                response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound,
                $"{method} {route} no es público (respondió {(int)response.StatusCode}).");
        }
    }

    [Fact]
    public async Task Login_WithoutToken_IsPublic()
    {
        var response = await database.CreateAnonymousClient()
            .LoginAsync(FixtureHubApiFactory.AdminUsername, FixtureHubApiFactory.AdminPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Write_WithTheTokenOfTheLogin_IsAccepted()
    {
        var login = await database.CreateAnonymousClient()
            .LoginAsync(FixtureHubApiFactory.AdminUsername, FixtureHubApiFactory.AdminPassword);
        var token = (await login.Content.ReadFromJsonAsync<AuthToken>())!.AccessToken;

        var response = await database.CreateClient(token)
            .PostJsonAsync("/api/teams", new { name = TestData.UniqueName("Los Halcones"), country = "El Salvador" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Write_WithATokenOfAnotherRole_Returns403Forbidden()
    {
        var response = await database.CreateClient(TestTokens.Create(role: "Viewer"))
            .PostJsonAsync("/api/teams", new { name = TestData.UniqueName("Los Halcones"), country = "El Salvador" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.ReadProblemAsync();
        Assert.Equal("Auth.Forbidden", problem.ErrorCode());
        Assert.True(problem.TryGetProperty("traceId", out _));
    }

    [Theory]
    [InlineData("de otro rol")]
    [InlineData("vencido")]
    public async Task Read_WithATokenThatCannotWrite_IsStillPublic(string reason)
    {
        var token = reason == "vencido"
            ? TestTokens.Create(expiresAt: DateTimeOffset.UtcNow.AddHours(-1))
            : TestTokens.Create(role: "Viewer");

        var response = await database.CreateClient(token).GetAsync("/api/standings");

        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Un GET con un token {reason} fue rechazado.");
    }

    public static TheoryData<string, string> RejectedTokens => new()
    {
        { "vencido", TestTokens.Create(expiresAt: DateTimeOffset.UtcNow.AddHours(-1)) },
        { "firmado con otra clave", TestTokens.Create(signingKey: "otra-clave-de-firma-que-la-api-no-conoce-0123456789") },
        { "para otra audiencia", TestTokens.Create(audience: "OtraApi") },
        { "sin firma (alg none)", TestTokens.Create(signed: false) },
        { "mal formado", "esto-no-es-un-jwt" }
    };

    [Theory]
    [MemberData(nameof(RejectedTokens))]
    public async Task Write_WithAnInvalidToken_Returns401Unauthenticated(string reason, string token)
    {
        var response = await database.CreateClient(token)
            .PostJsonAsync("/api/teams", new { name = TestData.UniqueName("Los Halcones"), country = "El Salvador" });

        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"Un token {reason} fue aceptado.");
        Assert.Equal("Auth.Unauthenticated", (await response.ReadProblemAsync()).ErrorCode());
    }

    private List<(string Method, string Route)> Endpoints() =>
        database.ApiServices.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .SelectMany(endpoint => (endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [])
                .Select(method => (Method: method, Route: endpoint.RoutePattern.RawText!)))
            .Where(endpoint => endpoint.Route.StartsWith("/api/", StringComparison.Ordinal))
            .ToList();

    private static HttpRequestMessage NewRequest(string method, string route)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), RouteParameter().Replace(route, _ => Guid.NewGuid().ToString()));
        if (method != "GET")
        {
            request.Content = JsonContent.Create(new { });
            request.Headers.Add(HttpClientExtensions.IdempotencyKeyHeader, Guid.NewGuid().ToString());
        }

        return request;
    }

    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex RouteParameter();
}
