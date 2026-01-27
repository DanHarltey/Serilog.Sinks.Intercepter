using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Serilog.Events;
using Serilog.Sinks.Intercepter.Benchmarks.Graveyard;
using Serilog.Sinks.Intercepter.Internal;
using Serilog.Sinks.Intercepter.Internal.RingBuffer;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_10_Increment;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_11_Method;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_12_T;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_13_Pow;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_14_Ctor;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_15_BitShift;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_9_No_And;

namespace Serilog.Sinks.Intercepter.Benchmarks;

[RankColumn, Orderer(SummaryOrderPolicy.Method, MethodOrderPolicy.Alphabetical)]
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

    [Benchmark]
    public object Multi_Thread_Add_1()
    {
        var events = _events!;
        var ringBuffer = new RingBuffer_1_Class(Capacity);

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


    //[Benchmark]
    //public object Multi_Threaded_Add_2_Scruct()
    //{
    //    var events = _events!;
    //    var ringBuffer = new RingBuffer_2_Scruct(Capacity + 1);

    //    Parallel.For(0, _threads, slice =>
    //    {
    //        var localEvents = events[slice];
    //        for (int i = 0; i < localEvents.Length; i++)
    //        {
    //            ringBuffer.TryAdd(localEvents[i]);
    //        }
    //    });

    //    return ringBuffer;
    //}

    //[Benchmark]
    //public object Multi_Thread_Add_5_BitShift()
    //{
    //    var events = _events!;

    //    var ringBuffer = new RingBuffer_5_BitShift(Capacity);
    //    Parallel.For(0, _threads, slice =>
    //    {
    //        var localEvents = events[slice];
    //        for (int i = 0; i < localEvents.Length; i++)
    //        {
    //            ringBuffer.TryAdd(localEvents[i]);
    //        }
    //    });

    //    return ringBuffer;
    //}

    //[Benchmark]
    //public object Multi_Thread_Add_6_ulong()
    //{
    //    var events = _events!;

    //    var ringBuffer = new RingBuffer_6_ulong(Capacity);
    //    Parallel.For(0, _threads, slice =>
    //    {
    //        var localEvents = events[slice];
    //        for (int i = 0; i < localEvents.Length; i++)
    //        {
    //            ringBuffer.TryAdd(localEvents[i]);
    //        }
    //    });

    //    return ringBuffer;
    //}

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
    public object Multi_Thread_Add_10()
    {
        var events = _events!;

        var ringBuffer = new RingBuffer_10_Increment(Capacity);
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
    public object Multi_Thread_Add_11()
    {
        var events = _events!;

        var ringBuffer = new RingBuffer_11_Method(Capacity);
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
    public object Multi_Thread_Add_13()
    {
        var events = _events!;

        var ringBuffer = new RingBuffer_13_Pow(Capacity);
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
    public object Multi_Thread_Add_14()
    {
        var events = _events!;

        var ringBuffer = new RingBuffer_14_Ctor(Capacity);
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
    public object Multi_Thread_Add_15()
    {
        var events = _events!;

        var ringBuffer = new RingBuffer_15_BitShift(Capacity);
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

    [Benchmark(Baseline = true)]
    public object Multi_Thread_Add()
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
}