using Core.Domain.EventStore;
using Core.Domain.Primitives;
using Core.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public abstract class StoreEventConfiguration<TAggregate> : IEntityTypeConfiguration<StoreEvent<TAggregate>>
    where TAggregate : AggregateRoot
{
    public void Configure(EntityTypeBuilder<StoreEvent<TAggregate>> builder)
    {
        builder.ToTable($"{typeof(TAggregate).Name}StoreEvents");

        builder.HasKey(@event => new { @event.Version, @event.AggregateId });

        builder.Property(@event => @event.AggregateId).IsRequired();
        builder.Property(@event => @event.Event).IsRequired().HasConversion<EventConverter>();

        builder.Property(@event => @event.EventType).HasMaxLength(50).IsUnicode(false).IsRequired();

        builder.Property(@event => @event.Timestamp).IsRequired();
        builder.Property(@event => @event.Version).IsRequired();
    }
}