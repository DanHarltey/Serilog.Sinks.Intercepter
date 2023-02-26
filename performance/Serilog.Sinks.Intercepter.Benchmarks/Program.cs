using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using Serilog.Sinks.Intercepter.Intercepters;
using Serilog.Events;

namespace Serilog.Sinks.Intercepter.Benchmarks;

[MemoryDiagnoser]
public class Program
{
    static void Main(string[] args)
    {
        _ = BenchmarkRunner.Run(typeof(Program));
    }
    
    private IIntercepter? _intercepter;
    private IIntercepter? _intercepter1;

    private LogEvent? _errorLogEvent;


    [GlobalSetup]
    public void SetUp()
    {
        _intercepter = new LogLevelBufferIntercepter(LogEventLevel.Error);
        _intercepter1 = new LogLevelBufferIntercepter1(LogEventLevel.Error);
        _errorLogEvent = GetLogEvent(LogEventLevel.Error);
    }


    [Benchmark(Baseline = true)]
    public object? Array()
    {
        object? obj = null;

        var events = _intercepter!.Process(_errorLogEvent!);

        foreach (var item in events)
        {
            obj = item;
        }
        return obj;
    }

    [Benchmark()]
    public object? Singal()
    {
        object? obj = null;

        var events = _intercepter1!.Process(_errorLogEvent!);

        foreach (var item in events)
        {
            obj = item;
        }
        return obj;
    }

    private static LogEvent GetLogEvent(LogEventLevel logLevel = LogEventLevel.Debug) => new(
        DateTimeOffset.UtcNow,
        logLevel,
        null,
        MessageTemplate.Empty,
        Enumerable.Empty<LogEventProperty>());
}
