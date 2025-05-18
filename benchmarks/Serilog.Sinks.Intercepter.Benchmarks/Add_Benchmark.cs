using BenchmarkDotNet.Attributes;
using Serilog.Events;
using Serilog.Sinks.Intercepter.Internal.RingBuffer;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_9_No_And;

namespace Serilog.Sinks.Intercepter.Benchmarks;

[RankColumn, DisassemblyDiagnoser]
public class Add_Benchmark
{
    private LogEvent? _event;
    private RingBuffer_8_CORINFO_HELP_ASSIGN_REF? _ringBuffer_8;
    private RingBuffer_9_No_And? _ringBuffer_9;

    private RingBuffer? _ringBuffer;

    [GlobalSetup]
    public void SetUp()
    {
        _event = EventCreation.CreateLogEvent("");

        _ringBuffer_8 = new RingBuffer_8_CORINFO_HELP_ASSIGN_REF(8);
        _ringBuffer_9 = new RingBuffer_9_No_And(8);
        _ringBuffer = new RingBuffer(8);
    }

    [Benchmark()]
    public bool RingBuffer_Add_8() => _ringBuffer_8!.TryAdd(_event!);

    [Benchmark()]
    public bool RingBuffer_Add_9() => _ringBuffer_9!.TryAdd(_event!);

    [Benchmark(Baseline = true)]
    public bool RingBuffer_Add() => _ringBuffer!.TryAdd(_event!);
}