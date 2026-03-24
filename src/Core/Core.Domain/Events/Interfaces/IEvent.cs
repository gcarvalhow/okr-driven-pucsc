namespace Core.Domain.Events.Interfaces;

public interface IEvent : IMessage;

public interface IDelayedEvent : IEvent;

public interface IVersionedEvent : IEvent
{
    ulong Version { get; }
}

public interface IDomainEvent : IVersionedEvent;