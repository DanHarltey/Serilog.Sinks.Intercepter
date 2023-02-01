using Serilog.Events;

namespace Serilog.Sinks.Intercepter.Intercepters;

public sealed class OrIntercepter : IIntercepter
{
    private readonly IIntercepter _left;
    private readonly IIntercepter _right;

    public OrIntercepter(IIntercepter left, IIntercepter right)
    {
        _left = left;
        _right = right;
    }

    public bool CanHandle(LogEvent logEvent) => _left.CanHandle(logEvent) || _right.CanHandle(logEvent);

    public IEnumerable<LogEvent> Process(LogEvent logEvent)
    {
        if (_left.CanHandle(logEvent))
        {
            return _left.Process(logEvent);
        }

        return _right.Process(logEvent);
    }
}
