using Core.Domain.EventStore;
using Core.Domain.Primitives;
using Core.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public abstract class SnapshotConfiguration<TAggregate> : IEntityTypeConfiguration<Snapshot<TAggregate>>
    where TAggregate : AggregateRoot
{
    public void Configure(EntityTypeBuilder<Snapshot<TAggregate>> builder)
    {
        builder.ToTable($"{typeof(TAggregate).Name}Snapshots");

        builder.HasKey(snapshot => new { snapshot.Version, snapshot.AggregateId });

        builder.Property(snapshot => snapshot.AggregateId).IsRequired();
        builder.Property(snapshot => snapshot.Aggregate).IsRequired()
            .HasConversion<AggregateConverter<TAggregate>>();

        builder.Property(snapshot => snapshot.Timestamp).IsRequired();
        builder.Property(snapshot => snapshot.Version).IsRequired();
    }
}