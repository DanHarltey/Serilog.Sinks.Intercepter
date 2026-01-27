using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Serilog.Events;
using Serilog.Sinks.Intercepter.Internal.RingBuffer;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_10_Increment;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_11_Method;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_12_T;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_13_Pow;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_14_Ctor;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_15_BitShift;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_9_No_And;

namespace Serilog.Sinks.Intercepter.Benchmarks;

[RankColumn, DisassemblyDiagnoser, Orderer(SummaryOrderPolicy.Method, MethodOrderPolicy.Alphabetical)]
public class Add_Benchmark
{
    private LogEvent? _event;
    private RingBuffer_7_nint? _ringBuffer_7;
    private RingBuffer_8_CORINFO_HELP_ASSIGN_REF? _ringBuffer_8;
    private RingBuffer_9_No_And? _ringBuffer_9;
    private RingBuffer_10_Increment? _ringBuffer10;
    private RingBuffer_11_Method? _ringBuffer11;
    private RingBuffer_12_T<LogEvent>? _ringBuffer_12;
    private RingBuffer_13_Pow? _ringBuffer_13;
    private RingBuffer_14_Ctor? _ringBuffer_14;
    private RingBuffer_15_BitShift? _ringBuffer_15;

    private RingBuffer? _ringBuffer;

    [GlobalSetup]
    public void SetUp()
    {
        _event = EventCreation.CreateLogEvent("");

        _ringBuffer_7 = new RingBuffer_7_nint(8);
        _ringBuffer_8 = new RingBuffer_8_CORINFO_HELP_ASSIGN_REF(8);
        _ringBuffer_9 = new RingBuffer_9_No_And(8);
        _ringBuffer = new RingBuffer(8);
        _ringBuffer10 = new RingBuffer_10_Increment(8);
        _ringBuffer11 = new RingBuffer_11_Method(8);
        _ringBuffer_12 = new RingBuffer_12_T<LogEvent>(8);
        _ringBuffer_13 = new RingBuffer_13_Pow(8);
        _ringBuffer_14 = new RingBuffer_14_Ctor(8);
        _ringBuffer_15 = new RingBuffer_15_BitShift(8);
    }

    [Benchmark()]
    public bool RingBuffer_Add_7() => _ringBuffer_7!.TryAdd(_event!);

    [Benchmark()]
    public bool RingBuffer_Add_8() => _ringBuffer_8!.TryAdd(_event!);

    [Benchmark()]
    public bool RingBuffer_Add_9() => _ringBuffer_9!.TryAdd(_event!);

    [Benchmark()]
    public bool RingBuffer_Add_10() => _ringBuffer10!.TryAdd(_event!);

    [Benchmark()]
    public bool RingBuffer_Add_11() => _ringBuffer11!.TryAdd(_event!);

    [Benchmark()]
    public bool RingBuffer_Add_12() => _ringBuffer_12!.TryAdd(_event!);

    [Benchmark()]
    public bool RingBuffer_Add_13() => _ringBuffer_13!.TryAdd(_event!);

    [Benchmark()]
    public bool RingBuffer_Add_14() => _ringBuffer_14!.TryAdd(_event!);

    [Benchmark()]
    public bool RingBuffer_Add_15() => _ringBuffer_15!.TryAdd(_event!);

    [Benchmark(Baseline = true)]
    public bool RingBuffer_Add() => _ringBuffer!.TryAdd(_event!);
}