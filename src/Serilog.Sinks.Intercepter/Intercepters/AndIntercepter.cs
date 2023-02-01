using Serilog.Events;

namespace Serilog.Sinks.Intercepter.Intercepters;

public sealed class AndIntercepter : IIntercepter
{
    private readonly IIntercepter _left;
    private readonly IIntercepter _right;

    public AndIntercepter(IIntercepter left, IIntercepter right)
    {
        _left = left;
        _right = right;
    }

    public bool CanHandle(LogEvent logEvent) => _left.CanHandle(logEvent);

    public IEnumerable<LogEvent> Process(LogEvent logEvent)
    {
        var leftLogEvents = _left.Process(logEvent);

        var processedEvents = new List<LogEvent>();

        foreach (var leftLogEvent in leftLogEvents)
        {
            if (_right.CanHandle(leftLogEvent))
            {
                var rightLogsEvents = _right.Process(leftLogEvent);
                processedEvents.AddRange(rightLogsEvents);
            }
        }

        return processedEvents;
    }
}
