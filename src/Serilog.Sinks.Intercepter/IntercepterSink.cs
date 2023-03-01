﻿using Serilog.Core;
using Serilog.Events;

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
        var intercepter = _context.Intercepter;

        if (intercepter == null)
        {
            _proxyedSink.Emit(logEvent);
            return;
        }

        if (intercepter.Reject(logEvent))
        {
            return;
        }

        var processedEvents = intercepter.Process(logEvent);

        foreach (var processedEvent in processedEvents)
        {
            _proxyedSink.Emit(processedEvent);
        }
    }
}
