using Serilog.Events;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Serilog.Sinks.Intercepter.Intercepters;

public sealed class LogLevelBufferIntercepter : IIntercepter
{
    private readonly LogEventLevel _triggerLevel;
    private volatile BlockingCollection<LogEvent>? _storedLogEvents = new();

    public LogLevelBufferIntercepter(LogEventLevel triggerLevel) => _triggerLevel = triggerLevel;

    public bool CanHandle(LogEvent logEvent) => true;

    public IEnumerable<LogEvent> Process(LogEvent logEvent)
    {
        if (logEvent.Level < _triggerLevel)
        {
            return AddToStore(logEvent);
        }

        return GetStoredLogEvents(logEvent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IEnumerable<LogEvent> AddToStore(LogEvent logEvent)
    {
        while (true)
        {
            var storedLogEvents = _storedLogEvents;

            if (storedLogEvents == null)
            {
                return new[] { logEvent };
            }

            try
            {
                storedLogEvents.Add(logEvent);
                return Enumerable.Empty<LogEvent>();
            }
            catch (InvalidOperationException)
            { }
        }
    }

    private IEnumerable<LogEvent> GetStoredLogEvents(LogEvent logEvent)
    {
        var storedLogEvents = Interlocked.Exchange(ref _storedLogEvents, null);

        if (storedLogEvents == null)
        {
            return new[] { logEvent };
        }

        storedLogEvents.Add(logEvent);
        storedLogEvents.CompleteAdding();
        return storedLogEvents.GetConsumingEnumerable();
    }
}