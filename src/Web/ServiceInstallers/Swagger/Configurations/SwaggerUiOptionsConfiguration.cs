using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Web.ServiceInstallers.Swagger.Configurations;

internal sealed class SwaggerUiOptionsConfiguration(IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerUIOptions>
{
    public void Configure(SwaggerUIOptions options)
    {
        options.RoutePrefix = "swagger";

        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                $"/api/swagger/{description.GroupName}/swagger.json",
                description.GroupName);
        }

        options.DisplayRequestDuration();
        options.EnableDeepLinking();
        options.EnableFilter();
        options.EnableTryItOutByDefault();
        options.DocExpansion(DocExpansion.None);
    }
}