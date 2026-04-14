using MassTransit;

namespace Core.Infrastructure.EventBus.Configurations;

public interface IConsumerConfiguration
{
    void AddConsumers(IRegistrationConfigurator registrationConfigurator);
}