using Core.Domain.Events.Interfaces;

namespace Core.Domain.Primitives.Interfaces;

public interface IAggregateRoot : IEntity
{
    ulong Version { get; }
    void LoadFromStream(List<IDomainEvent> events);
    bool TryDequeueEvent(out IDomainEvent @event);
}