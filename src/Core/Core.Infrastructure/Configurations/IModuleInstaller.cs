using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Infrastructure.Configurations;

public interface IModuleInstaller
{
    void Install(IServiceCollection services, IConfiguration configuration);
}
