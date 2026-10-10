using FixtureHub.Domain.Matches;
using FixtureHub.Domain.Teams;
using Microsoft.EntityFrameworkCore;

namespace FixtureHub.Infrastructure.Persistence;

internal sealed class FixtureHubDbContext(DbContextOptions<FixtureHubDbContext> options) : DbContext(options)
{
    public DbSet<Team> Teams => Set<Team>();

    public DbSet<Match> Matches => Set<Match>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FixtureHubDbContext).Assembly);
}
