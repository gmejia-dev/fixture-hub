using FixtureHub.Application.Abstractions.Authentication;
using FixtureHub.Infrastructure.Authentication;
using FixtureHub.Infrastructure.Persistence;
using FixtureHub.IntegrationTests.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixtureHub.IntegrationTests.Authentication;

[Collection(nameof(SqlServerCollection))]
public sealed class AdminAccountTests(SqlServerFixture database)
{
    [Fact]
    public async Task Startup_CreatesTheConfiguredAdminWithItsPasswordHashed()
    {
        await using var api = NewApi(NewDatabase());
        api.CreateClient();

        var admin = Assert.Single(await UsersAsync(api));
        Assert.Equal(FixtureHubApiFactory.AdminUsername, admin.Username);
        Assert.Equal("Admin", admin.Role);
        Assert.StartsWith("PBKDF2-SHA256$600000$", admin.PasswordHash, StringComparison.Ordinal);
        Assert.DoesNotContain(FixtureHubApiFactory.AdminPassword, admin.PasswordHash, StringComparison.Ordinal);
        Assert.True(Hasher(api).Verify(FixtureHubApiFactory.AdminPassword, admin.PasswordHash));
    }

    [Fact]
    public async Task Startup_AgainWithTheSamePassword_KeepsTheAdminAndItsHash()
    {
        var connectionString = NewDatabase();
        UserAccount first;
        await using (var api = NewApi(connectionString))
        {
            api.CreateClient();
            first = Assert.Single(await UsersAsync(api));
        }

        await using var restarted = NewApi(connectionString, migrate: false);
        restarted.CreateClient();

        var second = Assert.Single(await UsersAsync(restarted));
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.PasswordHash, second.PasswordHash);
    }

    [Fact]
    public async Task Startup_WithANewPassword_ReplacesTheHash()
    {
        const string newPassword = "otra-contraseña-de-prueba";
        var connectionString = NewDatabase();
        await using (var api = NewApi(connectionString))
            api.CreateClient();

        await using var restarted = NewApi(connectionString, migrate: false, new() { ["Admin:Password"] = newPassword });
        restarted.CreateClient();

        var admin = Assert.Single(await UsersAsync(restarted));
        Assert.True(Hasher(restarted).Verify(newPassword, admin.PasswordHash));
        Assert.False(Hasher(restarted).Verify(FixtureHubApiFactory.AdminPassword, admin.PasswordHash));
    }

    private string NewDatabase() => database.ConnectionStringFor($"FixtureHubAdmin{Guid.NewGuid():N}");

    private static FixtureHubApiFactory NewApi(
        string connectionString,
        bool migrate = true,
        Dictionary<string, string>? settings = null) =>
        new(connectionString, new Dictionary<string, string>(settings ?? []) { ["Database:ApplyMigrations"] = migrate.ToString() });

    private static IPasswordHasher Hasher(FixtureHubApiFactory api) => api.Services.GetRequiredService<IPasswordHasher>();

    private static async Task<List<UserAccount>> UsersAsync(FixtureHubApiFactory api)
    {
        await using var scope = api.Services.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<FixtureHubDbContext>().Users.AsNoTracking().ToListAsync();
    }
}
