using FixtureHub.Domain.Matches;
using FixtureHub.Domain.Teams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixtureHub.Infrastructure.Persistence.Configurations;

internal sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public const int StatusMaxLength = 20;

    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("Matches");

        builder.HasKey(match => match.Id);
        builder.Property(match => match.Id).ValueGeneratedNever();

        builder.Property(match => match.Status).HasConversion<string>().HasMaxLength(StatusMaxLength);

        builder.HasOne<Team>().WithMany().HasForeignKey(match => match.HomeTeamId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Team>().WithMany().HasForeignKey(match => match.AwayTeamId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(match => new { match.HomeTeamId, match.ScheduledAt });
        builder.HasIndex(match => new { match.AwayTeamId, match.ScheduledAt });

        builder.HasQueryFilter(match => !match.IsDeleted);

        builder.HasMany(match => match.Goals)
            .WithOne()
            .HasForeignKey(goal => goal.MatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(match => match.Goals)
            .HasField("_goals")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(match => match.Corrections)
            .WithOne()
            .HasForeignKey(correction => correction.MatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(match => match.Corrections)
            .HasField("_corrections")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(match => match.DomainEvents);
    }
}
