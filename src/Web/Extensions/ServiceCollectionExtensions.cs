using CorrelationId.DependencyInjection;

namespace Web.Extensions;

internal static class ServiceCollectionExtensions
{
    internal const string LoggingScopeKey = "CorrelationId";

    internal static IServiceCollection AddCorrelationId(this IServiceCollection services)
        => services.AddDefaultCorrelationId(options =>
        {
            options.RequestHeader =
                options.ResponseHeader =
                    options.LoggingScopeKey = LoggingScopeKey;

            options.UpdateTraceIdentifier =
                options.AddToLoggingScope = true;
        });
}