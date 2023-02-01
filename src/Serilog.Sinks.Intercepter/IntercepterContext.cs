using Serilog.Events;
using Serilog.Sinks.Intercepter.Intercepters;
using Serilog.Sinks.Intercepter.Internal;

namespace Serilog.Sinks.Intercepter;

public sealed class IntercepterContext
{
    public static IntercepterContext Default { get; } = new();

    private readonly AsyncLocal<IIntercepter?> _intercepter;

    public IntercepterContext() => _intercepter = new();

    internal IIntercepter? Intercepter
    {
        get => _intercepter.Value;
        set => _intercepter.Value = value;
    }

    public static IDisposable PushLogLevelBuffer(LogEventLevel triggerLevel = LogEventLevel.Error) => PushLogLevelBuffer(Default, triggerLevel);

    public static IDisposable PushLogLevelBuffer(IntercepterContext context, LogEventLevel triggerLevel = LogEventLevel.Error) => Push(context, new LogLevelBufferIntercepter(triggerLevel));

    public static IDisposable Push(IIntercepter intercepter) => Push(Default, intercepter);

    public static IDisposable Push(IntercepterContext context, IIntercepter intercepter)
    {
        var currentIntercepter = context.Intercepter;

        IIntercepter newIntercepter;

        if (currentIntercepter == null)
        {
            newIntercepter = intercepter;
        }
        else
        {
            newIntercepter = new AndIntercepter(intercepter, currentIntercepter);
        }

        context.Intercepter = newIntercepter;
        return new ContextBookmark(context, currentIntercepter);
    }
}
