using FixtureHub.Application.Abstractions.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixtureHub.Infrastructure.Persistence.Configurations;

internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");

        builder.HasKey(record => record.Key);
        builder.Property(record => record.Key).HasMaxLength(IdempotencyRecord.KeyMaxLength);

        builder.Property(record => record.Fingerprint)
            .HasMaxLength(IdempotencyRecord.FingerprintLength)
            .IsFixedLength()
            .IsUnicode(false);

        builder.HasIndex(record => record.ExpiresAt);
    }
}
