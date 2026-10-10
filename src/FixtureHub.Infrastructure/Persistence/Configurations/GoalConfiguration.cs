using FixtureHub.Domain.Matches;
using FixtureHub.Domain.Teams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixtureHub.Infrastructure.Persistence.Configurations;

internal sealed class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("Goals");

        builder.HasKey(goal => goal.Id);
        builder.Property(goal => goal.Id).ValueGeneratedNever();

        builder.HasOne<Player>().WithMany().HasForeignKey(goal => goal.PlayerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Team>().WithMany().HasForeignKey(goal => goal.ScoringTeamId).OnDelete(DeleteBehavior.Restrict);
    }
}
