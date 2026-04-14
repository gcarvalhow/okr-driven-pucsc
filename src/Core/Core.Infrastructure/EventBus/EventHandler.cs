using Core.Application.EventBus;
using Core.Domain.Events.Interfaces;
using MassTransit;

namespace Core.Infrastructure.EventBus;

public sealed class EventHandler<THandler, TEvent>(THandler handler) : IConsumer<TEvent>
        where TEvent : class, IEvent
        where THandler : IEventHandler<TEvent>
{
    public Task Consume(ConsumeContext<TEvent> context)
        => handler.Handle(context.Message, context.CancellationToken);
}