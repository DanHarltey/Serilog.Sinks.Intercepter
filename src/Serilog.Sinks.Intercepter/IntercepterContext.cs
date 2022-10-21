using Serilog.Events;
using Serilog.Sinks.Intercepter.Internal;

namespace Serilog.Sinks.Intercepter;

public sealed class IntercepterContext
{
    public static IntercepterContext Default { get; } = new();

    private readonly AsyncLocal<IIntercepter[]> _intercepters;

    public IntercepterContext() => _intercepters = new();

    internal IIntercepter[] Intercepters
    {
        get => _intercepters.Value ?? Array.Empty<IIntercepter>();
        set => _intercepters.Value = value;
    }

    public static IDisposable PushLogLevelBuffer(LogEventLevel triggerLevel = LogEventLevel.Error) => PushLogLevelBuffer(Default, triggerLevel);

    public static IDisposable PushLogLevelBuffer(IntercepterContext context, LogEventLevel triggerLevel = LogEventLevel.Error) => Push(context, new LogLevelBuffer(triggerLevel));

    public static IDisposable Push(IIntercepter moderator) => Push(Default, moderator);

    public static IDisposable Push(IntercepterContext context, IIntercepter moderator)
    {
        var currentIntercepters = context.Intercepters;

        IIntercepter[] newIntercepters = CreateArray(moderator, currentIntercepters);

        context.Intercepters = newIntercepters;
        return new ContextBookmark(context, currentIntercepters);
    }

    private static IIntercepter[] CreateArray(IIntercepter intercepter, IIntercepter[] currentIntercepter)
    {
        var newModerators = new IIntercepter[currentIntercepter.Length + 1];
        newModerators[0] = intercepter;
        Array.Copy(currentIntercepter, 0, newModerators, 0, currentIntercepter.Length);
        return newModerators;
    }
}
