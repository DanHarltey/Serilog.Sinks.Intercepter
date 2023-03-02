using Serilog.Events;

namespace Serilog.Sinks.Intercepter.Tests.Mocks;

internal sealed class TestIntercepter : IIntercepter
{
    private readonly bool _reject;
    private readonly Func<LogEvent, IEnumerable<LogEvent>> _intercept;

    public TestIntercepter(bool reject, Func<LogEvent, IEnumerable<LogEvent>> intercept)
    {
        _reject = reject;
        _intercept = intercept;
    }

    internal bool RejectCalled { get; private set; }

    internal bool InterceptCalled { get; private set; }

    public bool Reject(LogEvent logEvent)
    {
        RejectCalled = true;
        return _reject;
    }

    public IEnumerable<LogEvent> Intercept(LogEvent logEvent)
    {
        InterceptCalled = true;
        return _intercept(logEvent);
    }
}
