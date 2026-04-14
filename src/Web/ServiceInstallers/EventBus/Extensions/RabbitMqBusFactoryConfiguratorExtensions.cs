using Core.Domain.Events.Interfaces;
using Core.Infrastructure.Extensions;
using MassTransit;

namespace Web.ServiceInstallers.EventBus.Extensions;

public static class RabbitMqBusFactoryConfiguratorExtensions
{
    public static void ConfigureEventReceiveEndpoint<TConsumer, TMessage>(this IRabbitMqBusFactoryConfigurator bus, IRegistrationContext context, string module)
        where TConsumer : class, IConsumer
        where TMessage : class, IMessage
            => bus.ReceiveEndpoint(
                queueName: $"webbff.{module}.{typeof(TConsumer).ToKebabCaseString()}.{typeof(TMessage).ToKebabCaseString()}",
                configureEndpoint: endpoint =>
                {
                    endpoint.ConfigureConsumer<TConsumer>(context);
                });
}