using FixtureHub.Infrastructure;
using FixtureHub.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FixtureHub.IntegrationTests.Persistence;

[Collection(nameof(SqlServerCollection))]
public sealed class DatabaseHealthCheckTests(SqlServerFixture database)
{
    [Fact]
    public async Task HealthCheck_WithTheDatabaseReachable_IsHealthy()
    {
        var report = await CheckAsync(database.ConnectionString);

        Assert.Equal(HealthStatus.Healthy, report.Status);
        Assert.Contains(nameof(FixtureHubDbContext), report.Entries.Keys);
    }

    [Fact]
    public async Task HealthCheck_WithoutTheDatabase_IsUnhealthy()
    {
        var report = await CheckAsync(database.ConnectionStringFor($"NoExiste_{Guid.NewGuid():N}"));

        Assert.Equal(HealthStatus.Unhealthy, report.Status);
        Assert.Equal(HealthStatus.Unhealthy, report.Entries[nameof(FixtureHubDbContext)].Status);
    }

    private static async Task<HealthReport> CheckAsync(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(connectionString);

        await using var provider = services.BuildServiceProvider();

        return await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync();
    }
}
