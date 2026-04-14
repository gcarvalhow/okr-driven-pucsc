using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Core.Persistence.Options.Configurations;

internal sealed class ConnectionStringConfiguration(IConfiguration configuration) : IConfigureOptions<ConnectionStringOptions>
{
    private const string ConnectionStringName = "Default";

    public void Configure(ConnectionStringOptions options) => options.Value = configuration.GetConnectionString(ConnectionStringName);
}