using BenchmarkDotNet.Attributes;
using Serilog.Events;
using Serilog.Sinks.Intercepter.Benchmarks.Graveyard;
using Serilog.Sinks.Intercepter.Internal;
using Serilog.Sinks.Intercepter.Internal.RingBuffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.Intercepter.Benchmarks;

[RankColumn, MemoryDiagnoser]
public class Single_Thread_Add_Benchmark
{
    private const int Capacity = 1024;

    private LogEvent[]? _events;

    [GlobalSetup]
    public void SetUp() => _events = EventCreation.CreateEvents(Capacity * 8);

    ////[Benchmark]
    ////public object Single_Thread_Add_1_Class()
    ////{
    ////    var events = _events!;

    ////    var ringBuffer = new RingBuffer_1_Class(1024);

    ////    for (int i = 0; i < events.Length; i++)
    ////    {
    ////        ringBuffer.TryAdd(events[i]);
    ////    }

    ////    return ringBuffer;
    ////}

    [Benchmark]
    public object Single_Thread_Add_2_Scruct()
    {
        var events = _events!;

        var ringBuffer = new RingBuffer_2_Scruct(Capacity);

        for (int i = 0; i < events.Length; i++)
        {
            ringBuffer.TryAdd(events[i]);
        }

        return ringBuffer;
    }

    [Benchmark]
    public object Single_Thread_Add_5_BitShift()
    {
        var events = _events!;

        var ringBuffer = new RingBuffer_5_BitShift(Capacity);

        for (int i = 0; i < events.Length; i++)
        {
            ringBuffer.TryAdd(events[i]);
        }

        return ringBuffer;
    }

    [Benchmark()]
    public object Single_Thread_Add_6_ulong()
    {
        var events = _events!;

        var ringBuffer = new RingBuffer_6_ulong(Capacity);

        for (int i = 0; i < events.Length; i++)
        {
            ringBuffer.TryAdd(events[i]);
        }

        return ringBuffer;
    }

    [Benchmark(Baseline = true)]
    public object Single_Thread_Add()
    {
        var events = _events!;

        var ringBuffer = new RingBuffer(Capacity);

        for (int i = 0; i < events.Length; i++)
        {
            ringBuffer.TryAdd(events[i]);
        }

        return ringBuffer;
    }
}