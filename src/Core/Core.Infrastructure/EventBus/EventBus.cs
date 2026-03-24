using Core.Application.EventBus;
using Core.Application.ServiceLifetimes;
using Core.Domain.Events.Interfaces;
using MassTransit;

namespace Core.Infrastructure.EventBus;

public class EventBus(IBus bus, IPublishEndpoint publishEndpoint) : IEventBus, ITransient
{
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : class, IEvent
            => publishEndpoint.Publish(@event, @event.GetType(), cancellationToken);

    public Task SchedulePublishAsync<TEvent>(TEvent @event, DateTimeOffset scheduledTime, CancellationToken cancellationToken)
        where TEvent : class, IDelayedEvent
            => publishEndpoint.CreateMessageScheduler(bus.Topology).SchedulePublish(scheduledTime.UtcDateTime, @event, cancellationToken);
}