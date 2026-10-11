using FixtureHub.Infrastructure.Authentication;
using FixtureHub.Infrastructure.Persistence;
using FixtureHub.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixtureHub.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(
        this IServiceProvider services,
        bool applyMigrations,
        bool seed,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        if (applyMigrations)
            await scope.ServiceProvider.GetRequiredService<FixtureHubDbContext>().Database.MigrateAsync(cancellationToken);

        await scope.ServiceProvider.GetRequiredService<AdminAccountInitializer>().EnsureAsync(cancellationToken);

        if (seed)
            await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync(cancellationToken);
    }
}
