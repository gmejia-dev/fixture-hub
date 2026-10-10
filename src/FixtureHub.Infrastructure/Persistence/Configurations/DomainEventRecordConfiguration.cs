using FixtureHub.Infrastructure.Persistence.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixtureHub.Infrastructure.Persistence.Configurations;

internal sealed class DomainEventRecordConfiguration : IEntityTypeConfiguration<DomainEventRecord>
{
    public void Configure(EntityTypeBuilder<DomainEventRecord> builder)
    {
        builder.ToTable("DomainEvents");

        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).ValueGeneratedNever();

        builder.Property(record => record.Type).HasMaxLength(DomainEventRecord.TypeMaxLength);
        builder.Property(record => record.TraceId).HasMaxLength(DomainEventRecord.TraceIdLength).IsUnicode(false);

        builder.HasIndex(record => record.TraceId);
    }
}
