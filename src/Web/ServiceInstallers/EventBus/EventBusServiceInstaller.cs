using Core.Infrastructure.Configurations;
using Core.Infrastructure.Extensions;
using Core.Infrastructure.JsonConverters;
using MassTransit;
using MassTransit.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Web.ServiceInstallers.EventBus.PipeObservers;
using Web.ServiceInstallers.EventBus.Options;
using Web.ServiceInstallers.EventBus.PipeFilters;
using Core.Domain.Events.Interfaces;
using Web.ServiceInstallers.EventBus.Options.Configurations;

namespace Web.ServiceInstallers.EventBus;

internal sealed class EventBusServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration) =>
        services
            .ConfigureOptions<MassTransitHostOptionsConfiguration>()
            .ConfigureOptions<EventBusOptionsConfiguration>()
            .AddMassTransit(bussConfigurator =>
            {
                bussConfigurator.ConfigureHealthCheckOptions(options =>
                {
                    options.Name = "RabbitMq";
                    options.Tags.Add("rabbitmq");
                });

                bussConfigurator.SetKebabCaseEndpointNameFormatter();

                bussConfigurator.AddConsumers(AssemblyReference.Assembly);

                bussConfigurator.AddRequestClientsFromAssemblies();

                bussConfigurator.AddPublishMessageScheduler();
                bussConfigurator.AddHangfireConsumers();

                bussConfigurator.UsingRabbitMq((context, bus) =>
                {
                    var options = context.GetRequiredService<IOptions<EventBusOptions>>().Value;

                    bus.Host(
                        hostAddress: options.ConnectionString,
                        connectionName: $"{options.ConnectionName}.{AppDomain.CurrentDomain.FriendlyName}");

                    bus.UsePublishMessageScheduler();

                    bus.UseMessageRetry(r => r.Immediate(5));
                    bus.UseDelayedRedelivery(r => r.Intervals(
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(10),
                        TimeSpan.FromSeconds(30),
                        TimeSpan.FromMinutes(1),
                        TimeSpan.FromMinutes(2),
                        TimeSpan.FromMinutes(5),
                        TimeSpan.FromMinutes(10),
                        TimeSpan.FromMinutes(30),
                        TimeSpan.FromMinutes(60)));

                    bus.UseNewtonsoftJsonSerializer();

                    bus.ConfigureNewtonsoftJsonSerializer(settings =>
                    {
                        settings.Converters.Add(new TypeNameHandlingConverter(TypeNameHandling.Objects));
                        settings.Converters.Add(new DateOnlyJsonConverter());

                        return settings;
                    });

                    bus.MessageTopology.SetEntityNameFormatter(new KebabCaseEntityNameFormatter());

                    bus.UsePublishFilter(typeof(TraceIdentifierFilter<>), context);
                    bus.ConnectReceiveObserver(new LoggingReceiveObserver());
                    bus.ConnectConsumeObserver(new LoggingConsumeObserver());
                    bus.ConnectPublishObserver(new LoggingPublishObserver());
                    bus.ConnectSendObserver(new LoggingSendObserver());

                    bus.AddEventReceiveEndpointsFromAssemblies(
                            context,
                            AssemblyReference.Assembly);

                    bus.ConfigureEndpoints(context);

                    bus.ConfigurePublish(pipe => pipe.AddPipeSpecification(
                        new DelegatePipeSpecification<PublishContext<IEvent>>(ctx
                        => ctx.CorrelationId = ctx.InitiatorId)));
                });
            });
}