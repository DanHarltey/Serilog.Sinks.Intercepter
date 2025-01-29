using Serilog.Events;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Serilog.Sinks.Intercepter.Intercepters;

public sealed class LogLevelBufferIntercepter : IIntercepter, IDisposable
{
    private readonly LogEventLevel _triggerLevel;
    private volatile BlockingCollection<LogEvent>? _buffer = new();

    public LogLevelBufferIntercepter(LogEventLevel triggerLevel) => _triggerLevel = triggerLevel;

    public bool Reject(LogEvent logEvent) => false;

    public IEnumerable<LogEvent> Intercept(LogEvent logEvent)
    {
#pragma warning disable CA1510
        if (logEvent is null)
        {
            throw new ArgumentNullException(nameof(logEvent));
        }
#pragma warning restore CA1510
        var buffer = _buffer;

        if (buffer == null)
        {
            return BufferAlreadyFlushed(logEvent);
        }

        if (_triggerLevel <= logEvent.Level)
        {
            return FlushBuffer(logEvent);
        }

        try
        {
            buffer.Add(logEvent);
            return Enumerable.Empty<LogEvent>();
        }
        catch (InvalidOperationException)
        {
            // thrown if the buffer has already CompleteAdding
            return BufferAlreadyFlushed(logEvent);
        }
    }

    private IEnumerable<LogEvent> FlushBuffer(LogEvent logEvent)
    {
        // replace the buffer with null, so we do not store any more logs
        var buffer = Interlocked.Exchange(ref _buffer, null);

        if (buffer == null)
        {
            return BufferAlreadyFlushed(logEvent);
        }

        // the above interlock ensures only one thread gets here. No need for try/catch as we know buffer has not CompleteAdding
        buffer.Add(logEvent);

        // return all stored events
        buffer.CompleteAdding();
        return buffer.GetConsumingEnumerable();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static LogEvent[] BufferAlreadyFlushed(LogEvent logEvent) => new[] { logEvent };

    public void Dispose() => _buffer?.Dispose();
}