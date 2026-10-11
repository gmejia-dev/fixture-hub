using FixtureHub.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixtureHub.Infrastructure.Persistence.Configurations;

internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public const string UsernameIndex = "IX_Users_Username";

    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();

        builder.Property(user => user.Username).HasMaxLength(UserAccount.UsernameMaxLength);
        builder.HasIndex(user => user.Username).IsUnique().HasDatabaseName(UsernameIndex);

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(UserAccount.PasswordHashMaxLength)
            .IsUnicode(false);

        builder.Property(user => user.Role)
            .HasMaxLength(UserAccount.RoleMaxLength)
            .IsUnicode(false);
    }
}
