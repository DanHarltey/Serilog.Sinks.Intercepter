using Serilog.Events;

namespace Serilog.Sinks.Intercepter.Tests.Mocks;

internal sealed class TestIntercepter : IIntercepter
{
    private readonly bool _canHandle;
    private readonly Func<LogEvent, IEnumerable<LogEvent>> _process;

    public TestIntercepter(bool canHandle, Func<LogEvent, IEnumerable<LogEvent>> process)
    {
        _canHandle = canHandle;
        _process = process;
    }

    public bool CanHandle(LogEvent logEvent) => _canHandle;

    public IEnumerable<LogEvent> Process(LogEvent logEvent) => _process(logEvent);
}
