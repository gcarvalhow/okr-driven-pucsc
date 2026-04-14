using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;

namespace Web.ServiceInstallers.Swagger.Configurations;

internal sealed class SwaggerOptionsConfiguration : IConfigureOptions<SwaggerOptions>
{
    public void Configure(SwaggerOptions options)
    {
        options.RouteTemplate = "swagger/{documentName}/swagger.json";
    }
}