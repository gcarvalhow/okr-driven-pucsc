using CorrelationId;
using Web.Middlewares;

namespace Web.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseMiddlewares(this IApplicationBuilder app)
        => app.UsePathBase("/api")
              .UseCorrelationId()
              .UseMiddleware<RequestTimestampMiddleware>()
              .UseMiddleware<ExceptionHandlingMiddleware>();
}