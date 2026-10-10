using FixtureHub.Domain.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixtureHub.Infrastructure.Persistence.Configurations;

internal sealed class MatchResultCorrectionConfiguration : IEntityTypeConfiguration<MatchResultCorrection>
{
    public void Configure(EntityTypeBuilder<MatchResultCorrection> builder)
    {
        builder.ToTable("MatchResultCorrections");

        builder.HasKey(correction => correction.Id);
        builder.Property(correction => correction.Id).ValueGeneratedNever();

        builder.Property(correction => correction.Reason).HasMaxLength(MatchResultCorrection.ReasonMaxLength);
    }
}
