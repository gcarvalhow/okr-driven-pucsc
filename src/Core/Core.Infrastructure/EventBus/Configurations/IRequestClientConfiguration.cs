using MassTransit;

namespace Core.Infrastructure.EventBus.Configurations;

public interface IRequestClientConfiguration
{
    void AddRequestClients(IRegistrationConfigurator registrationConfigurator);
}