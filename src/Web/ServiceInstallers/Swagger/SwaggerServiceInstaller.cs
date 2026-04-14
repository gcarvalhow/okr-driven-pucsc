using Core.Infrastructure.Configurations;
using Web.ServiceInstallers.Swagger.Configurations;

namespace Web.ServiceInstallers.Swagger;

internal sealed class SwaggerServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureOptions<SwaggerOptionsConfiguration>();
        services.ConfigureOptions<SwaggerGenOptionsConfiguration>();
        services.ConfigureOptions<SwaggerUiOptionsConfiguration>();


        services.AddSwaggerGen();
    }
}