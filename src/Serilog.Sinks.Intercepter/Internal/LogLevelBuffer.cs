using Serilog.Events;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Serilog.Sinks.Intercepter.Internal;

internal sealed class LogLevelBuffer : IIntercepter
{
    private readonly LogEventLevel _triggerLevel;
    private volatile BlockingCollection<LogEvent> _cachedLogEvent = new();

    public LogLevelBuffer(LogEventLevel triggerLevel) => _triggerLevel = triggerLevel;

    public bool CanHandle(LogEvent logEvent) => true;

    public IEnumerable<LogEvent> Process(LogEvent logEvent)
    {
        if (logEvent.Level < _triggerLevel)
        {
            AddToQueue(logEvent);
            return Enumerable.Empty<LogEvent>();
        }

        return DumpLogEvents(logEvent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddToQueue(LogEvent logEvent)
    {
        while (true)
        {
            try
            {
                _cachedLogEvent.Add(logEvent);
                break;
            }
            catch (InvalidOperationException)
            { }
        }
    }

    private IEnumerable<LogEvent> DumpLogEvents(LogEvent logEvent)
    {
        var cachedEvents = Interlocked.Exchange(ref _cachedLogEvent, new());
        cachedEvents.Add(logEvent);
        cachedEvents.CompleteAdding();

        return cachedEvents.GetConsumingEnumerable();
    }
}
