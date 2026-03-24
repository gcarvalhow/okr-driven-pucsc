using Core.Domain.Events.Interfaces;

namespace Core.Application.EventBus;

public interface IEventHandler<in TEvent>
    where TEvent : IEvent
{
    Task Handle(TEvent @event, CancellationToken cancellationToken = default);
}