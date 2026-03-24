using Core.Domain.Events.Interfaces;
using Core.Domain.Primitives.Interfaces;

namespace Core.Domain.EventStore;

public record StoreEvent<TAggregate>(Guid AggregateId, string EventType, IDomainEvent Event, ulong Version, DateTimeOffset Timestamp)
    where TAggregate : IAggregateRoot
{
    public static StoreEvent<TAggregate> Create(TAggregate aggregate, IDomainEvent @event)
        => new(aggregate.Id, @event.GetType().Name, @event, aggregate.Version, @event.Timestamp);
}