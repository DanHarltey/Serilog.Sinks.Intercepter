using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Serilog.Events;
using Serilog.Sinks.Intercepter.Intercepters;

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
    private IIntercepter? _intercepter2;

    private LogEvent? _errorLogEvent;


    [GlobalSetup]
    public void SetUp()
    {
        _intercepter = new LogLevelBufferIntercepter(LogEventLevel.Error);
        _intercepter1 = new LogLevelBufferIntercepter1(LogEventLevel.Error);
        _intercepter2 = new LogLevelBufferIntercepter2(LogEventLevel.Error);

        _errorLogEvent = GetLogEvent(LogEventLevel.Error);
        _ = _intercepter.Process(_errorLogEvent);
        _ = _intercepter1.Process(_errorLogEvent);
        _ = _intercepter2.Process(_errorLogEvent);
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

    [Benchmark()]
    public object? Singal2()
    {
        object? obj = null;

        var events = _intercepter2!.Process(_errorLogEvent!);

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
