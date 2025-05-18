using BenchmarkDotNet.Attributes;
using Serilog.Events;
using Serilog.Sinks.Intercepter.Internal;
using Serilog.Sinks.Intercepter.Internal.RingBuffer;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_9_No_And;

namespace Serilog.Sinks.Intercepter.Benchmarks;

[RankColumn, MemoryDiagnoser]
public class Multi_Threaded_Add_Benchmark
{
    private const int Capacity = 1024;
    private readonly int _threads = 2;

    private LogEvent[][]? _events;

    [GlobalSetup]
    public void SetUp()
    {
        var events = EventCreation.CreateEvents(Capacity * _threads);

        var group = 0;
        _events = events
            .GroupBy(x => group++ % _threads)
            .Select(x => x.ToArray())
            .ToArray();
    }

    [Benchmark(Baseline = true)]
    public object Multi_Threaded_Add()
    {
        var events = _events!;
        var ringBuffer = new RingBuffer(Capacity);

        Parallel.For(0, _threads, slice =>
        {
            var localEvents = events[slice];
            for (int i = 0; i < localEvents.Length; i++)
            {
                ringBuffer.TryAdd(localEvents[i]);
            }
        });

        return ringBuffer;
    }

    ////[Benchmark]
    ////public object Multi_Threaded_Add_1_Class()
    ////{
    ////    var events = _events!;
    ////    var ringBuffer = new RingBuffer_1_Class(Capacity);

    ////    Parallel.For(0, _threads, slice =>
    ////    {
    ////        var localEvents = events[slice];
    ////        for (int i = 0; i < localEvents.Length; i++)
    ////        {
    ////            ringBuffer.TryAdd(localEvents[i]);
    ////        }
    ////    });

    ////    return ringBuffer;
    ////}

    [Benchmark]
    public object Multi_Thread_Add_5_BitShift()
    {
        var events = _events!;

        var ringBuffer = new RingBuffer_5_BitShift(Capacity);
        Parallel.For(0, _threads, slice =>
        {
            var localEvents = events[slice];
            for (int i = 0; i < localEvents.Length; i++)
            {
                ringBuffer.TryAdd(localEvents[i]);
            }
        });

        return ringBuffer;
    }

    [Benchmark]
    public object Multi_Thread_Add_6_ulong()
    {
        var events = _events!;

        var ringBuffer = new RingBuffer_6_ulong(Capacity);
        Parallel.For(0, _threads, slice =>
        {
            var localEvents = events[slice];
            for (int i = 0; i < localEvents.Length; i++)
            {
                ringBuffer.TryAdd(localEvents[i]);
            }
        });

        return ringBuffer;
    }

    [Benchmark]
    public object Multi_Thread_Add_7()
    {
        var events = _events!;

        var ringBuffer = new RingBuffer_7_nint(Capacity);
        Parallel.For(0, _threads, slice =>
        {
            var localEvents = events[slice];
            for (int i = 0; i < localEvents.Length; i++)
            {
                ringBuffer.TryAdd(localEvents[i]);
            }
        });

        return ringBuffer;
    }

    [Benchmark]
    public object Multi_Thread_Add_8()
    {
        var events = _events!;

        var ringBuffer = new RingBuffer_8_CORINFO_HELP_ASSIGN_REF(Capacity);
        Parallel.For(0, _threads, slice =>
        {
            var localEvents = events[slice];
            for (int i = 0; i < localEvents.Length; i++)
            {
                ringBuffer.TryAdd(localEvents[i]);
            }
        });

        return ringBuffer;
    }

    [Benchmark]
    public object Multi_Thread_Add_9()
    {
        var events = _events!;

        var ringBuffer = new RingBuffer_9_No_And(Capacity);
        Parallel.For(0, _threads, slice =>
        {
            var localEvents = events[slice];
            for (int i = 0; i < localEvents.Length; i++)
            {
                ringBuffer.TryAdd(localEvents[i]);
            }
        });

        return ringBuffer;
    }

    [Benchmark]
    public object Multi_Threaded_Add_2_Scruct()
    {
        var events = _events!;
        var ringBuffer = new RingBuffer_2_Scruct(Capacity + 1);

        Parallel.For(0, _threads, slice =>
        {
            var localEvents = events[slice];
            for (int i = 0; i < localEvents.Length; i++)
            {
                ringBuffer.TryAdd(localEvents[i]);
            }
        });

        return ringBuffer;
    }
}