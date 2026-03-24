using Core.Domain.Events.Interfaces;
using Core.Domain.Exceptions;
using Core.Domain.Primitives.Interfaces;

namespace Core.Domain.Primitives;

public abstract class AggregateRoot : Entity, IAggregateRoot
{
    private readonly Queue<IDomainEvent> _events = new();

    public ulong Version { get; private set; } = 0;

    public void LoadFromStream(List<IDomainEvent> events)
    {
        foreach (var @event in events.OrderBy(ev => ev.Version))
        {
            ApplyEvent(@event);

            Version = @event.Version;
        }
    }

    public bool TryDequeueEvent(out IDomainEvent @event)
        => _events.TryDequeue(out @event!);

    private void EnqueueEvent(IDomainEvent @event)
        => _events.Enqueue(@event);

    protected void RaiseEvent<TEvent>(Func<ulong, TEvent> func) where TEvent : IDomainEvent
        => RaiseEvent((func as Func<ulong, IDomainEvent>)!);

    protected void RaiseEvent(Func<ulong, IDomainEvent> onRaise)
    {
        if (IsDeleted)
            throw new AggregateIsDeletedException(Id);

        var @event = onRaise(++Version);

        ApplyEvent(@event);
        EnqueueEvent(@event);
    }

    protected abstract void ApplyEvent(IDomainEvent @event);
}