using Microsoft.Extensions.Options;

namespace Web.ServiceInstallers.EventBus.Options.Configurations;

internal sealed class EventBusOptionsConfiguration(IConfiguration configuration)
    : IConfigureOptions<EventBusOptions>
{
    internal const string SectionName = "EventBusOptions";

    public void Configure(EventBusOptions options)
        => configuration.GetSection(SectionName).Bind(options);
}