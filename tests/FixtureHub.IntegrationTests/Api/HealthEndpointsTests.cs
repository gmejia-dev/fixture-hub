using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FixtureHub.IntegrationTests.Api;

[Collection(nameof(SqlServerCollection))]
public sealed class HealthEndpointsTests(SqlServerFixture database)
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoint_WithSqlServerReachable_ReturnsHealthyWithoutAToken(string path)
    {
        var response = await database.CreateAnonymousClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Ready_WhenACheckFails_Returns503WhileLiveStillReturns200()
    {
        await using var api = new FixtureHubApiFactory(
            database.ConnectionString,
            configureServices: services => services.AddHealthChecks().AddCheck("falla", () => HealthCheckResult.Unhealthy()));
        var client = api.CreateClient();

        var ready = await client.GetAsync("/health/ready");
        var live = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        Assert.Equal("Unhealthy", await ready.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
    }
}
