using Serilog.Events;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Serilog.Sinks.Intercepter.Intercepters;

public sealed class LogLevelBufferIntercepter : IIntercepter
{
    private readonly LogEventLevel _triggerLevel;
    private volatile BlockingCollection<LogEvent>? _buffer = new();

    public LogLevelBufferIntercepter(LogEventLevel triggerLevel) => _triggerLevel = triggerLevel;

    public bool CanHandle(LogEvent logEvent) => true;

    public IEnumerable<LogEvent> Process(LogEvent logEvent)
    {
        var buffer = _buffer;

        if (buffer == null)
        {
            return BufferAlreadyFlushed(logEvent);
        }

        try
        {
            buffer.Add(logEvent);
        }
        catch (InvalidOperationException)
        {
            // thrown if the store has already CompleteAdding
            return BufferAlreadyFlushed(logEvent);
        }

        if (logEvent.Level < _triggerLevel)
        {
            return Enumerable.Empty<LogEvent>();
        }

        return FlushBuffer(logEvent);
    }

    private IEnumerable<LogEvent> FlushBuffer(LogEvent logEvent)
    {
        // replace the buffer with null, so we do not store any more logs
        var buffer = Interlocked.Exchange(ref _buffer, null);

        if (buffer == null)
        {
            return BufferAlreadyFlushed(logEvent);
        }

        // return all stored events
        buffer.CompleteAdding();
        return buffer.GetConsumingEnumerable();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IEnumerable<LogEvent> BufferAlreadyFlushed(LogEvent logEvent) => new[] { logEvent };
}