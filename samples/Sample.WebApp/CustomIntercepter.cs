using Serilog.Events;
using Serilog.Sinks.Intercepter;

namespace Sample.WebApp;

internal sealed class CustomIntercepter : IIntercepter
{
    public bool Reject(LogEvent logEvent) => logEvent.Level == LogEventLevel.Verbose;

    public IEnumerable<LogEvent> Intercept(LogEvent logEvent)
    {
        switch (logEvent.Level)
        {
            case LogEventLevel.Verbose:
                throw new NotSupportedException();

            case LogEventLevel.Debug:
                // buffer
                return new[] { logEvent };

            case LogEventLevel.Information:
            case LogEventLevel.Warning:
                // return
                return new[] { logEvent };

            case LogEventLevel.Error:
            case LogEventLevel.Fatal:
            default:
                // flush
                return new[] { logEvent };
        }
    }
}
