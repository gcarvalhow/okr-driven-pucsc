using Core.Domain.Primitives.Interfaces;

namespace Core.Domain.EventStore;

public record Snapshot<TAggregate>(Guid AggregateId, TAggregate Aggregate, ulong Version, DateTimeOffset Timestamp)
    where TAggregate : IAggregateRoot
{
    public static Snapshot<TAggregate> Create(TAggregate aggregate, StoreEvent<TAggregate> @event)
        => new(aggregate.Id, aggregate, @event.Version, @event.Timestamp);
}