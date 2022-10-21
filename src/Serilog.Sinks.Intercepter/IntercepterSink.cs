using Serilog.Core;
using Serilog.Events;
using System.Runtime.CompilerServices;

namespace Serilog.Sinks.Intercepter;

public sealed class IntercepterSink : ILogEventSink
{
    private readonly IntercepterContext _context;
    private readonly ILogEventSink _proxyedSink;

    public IntercepterSink(IntercepterContext context, ILogEventSink proxyedSink)
    {
        _context = context;
        _proxyedSink = proxyedSink;
    }

    public void Emit(LogEvent logEvent)
    {
        var intercepters = _context.Intercepters;

        foreach (var intercepter in intercepters)
        {
            if (intercepter.CanHandle(logEvent))
            {
                ProcessLogEvent(logEvent, intercepter);
                return;
            }
        }

        _proxyedSink.Emit(logEvent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessLogEvent(LogEvent logEvent, IIntercepter intercepter)
    {
        var processedLogs = intercepter.Process(logEvent);
        foreach (var processedLog in processedLogs)
        {
            _proxyedSink.Emit(processedLog);
        }
    }
}
