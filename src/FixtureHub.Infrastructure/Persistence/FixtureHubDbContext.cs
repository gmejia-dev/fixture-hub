using FixtureHub.Application.Abstractions.Idempotency;
using FixtureHub.Domain.Matches;
using FixtureHub.Domain.Standings;
using FixtureHub.Domain.Teams;
using FixtureHub.Infrastructure.Persistence.DomainEvents;
using Microsoft.EntityFrameworkCore;

namespace FixtureHub.Infrastructure.Persistence;

internal sealed class FixtureHubDbContext(DbContextOptions<FixtureHubDbContext> options) : DbContext(options)
{
    public DbSet<Team> Teams => Set<Team>();

    public DbSet<Match> Matches => Set<Match>();

    public DbSet<TeamStanding> TeamStandings => Set<TeamStanding>();

    public DbSet<DomainEventRecord> DomainEvents => Set<DomainEventRecord>();

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FixtureHubDbContext).Assembly);
}
