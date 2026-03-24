using Core.Shared.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;

namespace Core.Application.Behaviors;

public sealed class RequestLoggingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : Result
{
    private readonly ILogger<RequestLoggingPipelineBehavior<TRequest, TResponse>> _logger;

    public RequestLoggingPipelineBehavior(ILogger<RequestLoggingPipelineBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var moduleName = GetModuleName(typeof(TRequest).FullName ?? string.Empty);
        var requestName = typeof(TRequest).Name;

        using (LogContext.PushProperty("Module", moduleName))
        {
            _logger.LogInformation("Starting request {RequestName} in module {ModuleName}", requestName, moduleName);

            try
            {
                TResponse response = await next();
                stopwatch.Stop();

                if (response.IsSuccess)
                {
                    _logger.LogInformation("Completed request {RequestName} in module {ModuleName} on {ElapsedMs}ms",
                            requestName, moduleName, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    using (LogContext.PushProperty("Error", response.Error, true))
                    {
                        _logger.LogError("Completed request {RequestName} in module {ModuleName} with error on {ElapsedMs}ms",
                            requestName, moduleName, stopwatch.ElapsedMilliseconds);
                    }
                }

                return response;

            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Request {RequestName} failed after {ElapsedMs}ms",
                    requestName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }

    private static string GetModuleName(string requestName)
    {
        var parts = requestName.Split('.');
        return parts.Length > 1 ? parts[^2] : "Unknown";
    }
}