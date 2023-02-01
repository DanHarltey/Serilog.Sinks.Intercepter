using Serilog.Events;
using Serilog.Sinks.Intercepter.Internal;
using System.Runtime.CompilerServices;

namespace Serilog.Sinks.Intercepter.Intercepters;

public class RingBufferIntercepter : IIntercepter
{
    private readonly uint _bufferSize;
    private readonly LogEventLevel _triggerLevel;
    private RingBuffer<LogEvent> _storedLogEvents;

    public RingBufferIntercepter(uint bufferSize, LogEventLevel triggerLevel)
    {
        _bufferSize = bufferSize;
        _triggerLevel = triggerLevel;
        _storedLogEvents = new(bufferSize);
    }

    public bool CanHandle(LogEvent logEvent) => true;

    public IEnumerable<LogEvent> Process(LogEvent logEvent)
    {
        if (logEvent.Level < _triggerLevel)
        {
            return AddToBuffer(logEvent);
        }

        return GetStoredLogEvents(logEvent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IEnumerable<LogEvent> AddToBuffer(LogEvent logEvent)
    {
        while (true)
        {
            var storedLogEvents = Volatile.Read(ref _storedLogEvents);

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
        var storedLogEvents = Interlocked.Exchange(ref _storedLogEvents, new(_bufferSize));

        storedLogEvents.Add(logEvent);
        storedLogEvents.CompleteAdding();

        return storedLogEvents;
    }
}
