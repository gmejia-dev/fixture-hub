using FixtureHub.Domain.Standings;
using FixtureHub.Domain.Teams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixtureHub.Infrastructure.Persistence.Configurations;

internal sealed class TeamStandingConfiguration : IEntityTypeConfiguration<TeamStanding>
{
    public void Configure(EntityTypeBuilder<TeamStanding> builder)
    {
        builder.ToTable("TeamStandings");

        builder.Ignore(standing => standing.Id);
        builder.HasKey(standing => standing.TeamId);
        builder.Property(standing => standing.TeamId).ValueGeneratedNever();

        builder.HasOne<Team>()
            .WithOne()
            .HasForeignKey<TeamStanding>(standing => standing.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(standing => new { standing.Points, standing.GoalDifference, standing.GoalsFor })
            .IsDescending()
            .HasDatabaseName("IX_TeamStandings_Ranking");

        builder.Ignore(standing => standing.DomainEvents);
    }
}
