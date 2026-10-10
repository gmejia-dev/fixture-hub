using FixtureHub.Domain.Teams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixtureHub.Infrastructure.Persistence.Configurations;

internal sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public const string ShirtNumberIndex = "IX_Players_TeamId_ShirtNumber";

    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("Players");

        builder.HasKey(player => player.Id);
        builder.Property(player => player.Id).ValueGeneratedNever();

        builder.Property(player => player.Name).HasMaxLength(Player.NameMaxLength);

        builder.HasIndex(player => player.TeamId);

        builder.HasIndex(player => new { player.TeamId, player.ShirtNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName(ShirtNumberIndex);
    }
}
