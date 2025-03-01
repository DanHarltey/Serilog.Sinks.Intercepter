using Serilog.Events;

namespace Serilog.Sinks.Intercepter.Intercepters;

internal sealed class Chainedntercepter : IIntercepter
{
    private readonly IIntercepter _first;
    private readonly IIntercepter _second;

    public Chainedntercepter(IIntercepter first, IIntercepter second)
    {
        _first = first ?? throw new ArgumentNullException(nameof(first));
        _second = second ?? throw new ArgumentNullException(nameof(second));
    }

    public bool Reject(LogEvent logEvent) => _first.Reject(logEvent);

    public IEnumerable<LogEvent> Intercept(LogEvent logEvent)
    {
        var firstLogEvents = _first.Intercept(logEvent);

        var chainedLogEvents = new List<LogEvent>();

        foreach (var firstLogEvent in firstLogEvents)
        {
            if (!_second.Reject(firstLogEvent))
            {
                var interceptedLogEvents = _second.Intercept(firstLogEvent);
                chainedLogEvents.AddRange(interceptedLogEvents);
            }
        }

        return chainedLogEvents;
    }
}