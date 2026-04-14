using MassTransit;
using Microsoft.Extensions.Options;

namespace Web.ServiceInstallers.EventBus.Options.Configurations;

internal sealed class MassTransitHostOptionsConfiguration(IConfiguration configuration)
    : IConfigureOptions<MassTransitHostOptions>
{
    internal const string SectionName = "MassTransitHostOptions";

    public void Configure(MassTransitHostOptions options)
        => configuration.GetSection(SectionName).Bind(options);
}