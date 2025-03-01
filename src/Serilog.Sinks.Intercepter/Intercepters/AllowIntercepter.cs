using Serilog.Events;

namespace Serilog.Sinks.Intercepter.Intercepters;

internal sealed class AllowInformationIntercepter : IIntercepter
{
    public bool Reject(LogEvent logEvent) => logEvent.Level == LogEventLevel.Information;

    public IEnumerable<LogEvent> Intercept(LogEvent logEvent) => new [] { logEvent };
}
