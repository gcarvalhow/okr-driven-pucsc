using MassTransit;

namespace Core.Infrastructure.EventBus.Configurations;

public interface IEventReceiveEndpointConfiguration
{
    void AddEventReceiveEndpoints(IRabbitMqBusFactoryConfigurator rabbitMqBusFactoryConfigurator, IRegistrationContext registrationContext);
}