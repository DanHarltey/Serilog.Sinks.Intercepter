using Serilog.Sinks.Intercepter;

namespace Sample.WebApp;

public class LogMiddleware
{
    private readonly RequestDelegate _next;

    public LogMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext httpContext)
    {
        using (IntercepterContext.PushLogLevelBuffer())
        {
            await _next(httpContext);
        }
    }
}
