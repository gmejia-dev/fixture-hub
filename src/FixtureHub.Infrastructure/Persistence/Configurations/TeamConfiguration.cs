using FixtureHub.Domain.Teams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixtureHub.Infrastructure.Persistence.Configurations;

internal sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public const string NameIndex = "IX_Teams_Name";

    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");

        builder.HasKey(team => team.Id);
        builder.Property(team => team.Id).ValueGeneratedNever();

        builder.Property(team => team.Name).HasMaxLength(Team.NameMaxLength);
        builder.Property(team => team.Country).HasMaxLength(Team.CountryMaxLength);

        builder.HasIndex(team => team.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName(NameIndex);

        builder.HasQueryFilter(team => !team.IsDeleted);

        builder.HasMany(team => team.Players)
            .WithOne()
            .HasForeignKey(player => player.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(team => team.Players)
            .HasField("_players")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(team => team.DomainEvents);
    }
}
