using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FixtureHub.Infrastructure.Persistence;

internal sealed class FixtureHubDbContextFactory : IDesignTimeDbContextFactory<FixtureHubDbContext>
{
    public FixtureHubDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FixtureHubDbContext>()
            .UseSqlServer("Server=localhost;Database=FixtureHub;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new FixtureHubDbContext(options);
    }
}
