using Serilog.Core;
using Serilog.Events;
using System.Collections.Concurrent;

namespace Serilog.Sinks.Intercepter.Tests.Mocks;

internal class TestSink : ILogEventSink, IDisposable
{
    private readonly ConcurrentQueue<LogEvent> _logEvents = new();

    public bool IsDisposed { get; private set; }
    public IEnumerable<LogEvent> LogEvents => _logEvents;

    public void Emit(LogEvent logEvent) => _logEvents.Enqueue(logEvent);

    public void Dispose() => IsDisposed = true;
}
