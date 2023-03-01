using Serilog.Events;

namespace Serilog.Sinks.Intercepter.Tests.Mocks;

internal sealed class TestIntercepter : IIntercepter
{
    private readonly bool _reject;
    private readonly Func<LogEvent, IEnumerable<LogEvent>> _process;

    public TestIntercepter(bool reject, Func<LogEvent, IEnumerable<LogEvent>> process)
    {
        _reject = reject;
        _process = process;
    }

    internal bool RejectCalled { get; private set; }

    internal bool ProcessCalled { get; private set; }

    public bool Reject(LogEvent logEvent)
    {
        RejectCalled = true;
        return _reject;
    }

    public IEnumerable<LogEvent> Process(LogEvent logEvent)
    {
        ProcessCalled = true;
        return _process(logEvent);
    }
}
