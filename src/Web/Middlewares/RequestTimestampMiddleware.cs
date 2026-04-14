namespace Web.Middlewares;

/// <summary>
/// Middleware to add request timestamp to the context
/// </summary>
public class RequestTimestampMiddleware
{
    private readonly RequestDelegate _next;

    public RequestTimestampMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add timestamp of the request to the context
        context.Items["RequestTimestamp"] = DateTime.UtcNow.Ticks;

        // Add unique request ID if it doesn't exist
        if (!context.Items.ContainsKey("RequestId"))
        {
            context.Items["RequestId"] = Guid.NewGuid().ToString();
        }

        await _next(context);
    }
}