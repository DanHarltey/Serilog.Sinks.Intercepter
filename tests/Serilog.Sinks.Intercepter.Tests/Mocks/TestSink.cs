using Serilog.Core;
using Serilog.Events;
using System.Collections.Concurrent;

namespace Serilog.Sinks.Intercepter.Tests.Mocks;

internal class TestSink : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _logEvents = new();

    public IEnumerable<LogEvent> LogEvents => _logEvents;

    public void Emit(LogEvent logEvent) => _logEvents.Enqueue(logEvent);
}
