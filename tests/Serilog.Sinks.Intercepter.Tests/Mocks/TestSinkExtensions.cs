using Serilog.Configuration;
using Serilog.Core;

namespace Serilog.Sinks.Intercepter.Tests.Mocks;

public static class TestSinkExtensions
{
    private static readonly AsyncLocal<ILogEventSink> _instance = new();

    internal static ILogEventSink? Instance
    {
        get => _instance.Value;
        set => _instance.Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static LoggerConfiguration TestSink(this LoggerSinkConfiguration loggerSinkConfiguration) =>
        loggerSinkConfiguration.Sink(Instance ?? throw new InvalidOperationException("Instance must not be null"));
}