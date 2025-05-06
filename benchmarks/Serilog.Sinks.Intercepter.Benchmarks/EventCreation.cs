using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.Intercepter.Benchmarks;

internal static class EventCreation
{
    public static LogEvent[] CreateEvents(int count)
    {
        var events = new LogEvent[count];
        for (int i = 0; i < events.Length; i++)
        {
            events[i] = CreateLogEvent(i.ToString());
        }

        return events;
    }

    private static LogEvent CreateLogEvent(string message) =>
        new(DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new(message, Enumerable.Empty<MessageTemplateToken>()),
            Enumerable.Empty<LogEventProperty>());
}
