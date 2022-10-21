using Serilog.Events;

namespace Serilog.Sinks.Intercepter;

public interface IIntercepter
{
    bool CanHandle(LogEvent logEvent);
    IEnumerable<LogEvent> Process(LogEvent logEvent);
}
