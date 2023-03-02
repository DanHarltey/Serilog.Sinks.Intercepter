using Serilog.Events;

namespace Serilog.Sinks.Intercepter;

public interface IIntercepter
{
    bool Reject(LogEvent logEvent);
    IEnumerable<LogEvent> Intercept(LogEvent logEvent);
}
