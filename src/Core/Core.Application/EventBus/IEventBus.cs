using Core.Domain.Events.Interfaces;

namespace Core.Application.EventBus;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
            where TEvent : class, IEvent;

    Task SchedulePublishAsync<TEvent>(TEvent @event, DateTimeOffset scheduledTime, CancellationToken cancellationToken)
        where TEvent : class, IDelayedEvent;
}