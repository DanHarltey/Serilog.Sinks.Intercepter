using Serilog.Events;

namespace Serilog.Sinks.Intercepter.Intercepters;

internal sealed class OrIntercepter : IIntercepter
{
    private readonly IIntercepter _first;
    private readonly IIntercepter _second;

    public OrIntercepter(IIntercepter first, IIntercepter second)
    {
        _first = first ?? throw new ArgumentNullException(nameof(first)); 
        _second = second ?? throw new ArgumentNullException(nameof(second));
    }

    public bool Reject(LogEvent logEvent) => _first.Reject(logEvent) || _second.Reject(logEvent);

    public IEnumerable<LogEvent> Intercept(LogEvent logEvent)
    {
        if (!_first.Reject(logEvent))
        {
            return _first.Intercept(logEvent);
        }

        return _second.Intercept(logEvent);
    }
}