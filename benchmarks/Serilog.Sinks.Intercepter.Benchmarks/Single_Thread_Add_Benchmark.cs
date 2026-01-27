using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Order;
using Serilog.Events;
using Serilog.Sinks.Intercepter.Benchmarks.Graveyard;
using Serilog.Sinks.Intercepter.Internal.RingBuffer;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_10_Increment;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_11_Method;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_12_T;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_13_Pow;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_14_Ctor;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_15_BitShift;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_9_No_And;

namespace Serilog.Sinks.Intercepter.Benchmarks;

////[
////    // HardwareCounters(HardwareCounter.InstructionRetired),
////    RankColumn,
////    //DisassemblyDiagnoser, 
////    Orderer(SummaryOrderPolicy.Method, MethodOrderPolicy.Declared)
////    ]
[HardwareCounters(HardwareCounter.BranchMispredictions, HardwareCounter.BranchInstructions, HardwareCounter.CacheMisses, HardwareCounter.InstructionRetired
)]
public class Single_Thread_Add_Benchmark
{
    private const int Capacity = 1024;
    private const int Loops = Capacity * 4;

    private LogEvent? _event;

    public Single_Thread_Add_Benchmark()
    {
        _event = EventCreation.CreateLogEvent(string.Empty);
    }

    //[GlobalSetup]
    //public void SetUp() => _event = EventCreation.CreateLogEvent(string.Empty);

    ////[Benchmark]
    ////public object Single_Thread_Add_1_Class()
    ////{
    ////    var ringBuffer = new RingBuffer_1_Class(Capacity);

    ////    var logEvent = _event!;
    ////    for (int i = 0; i < Loops; i++)
    ////    {
    ////        ringBuffer.TryAdd(logEvent);
    ////    }

    ////    return ringBuffer;
    ////}
    /*
    ////[Benchmark]
    ////public object Single_Thread_Add_2_Scruct()
    ////{
    ////    var events = _events!;

    ////    var ringBuffer = new RingBuffer_2_Scruct(Capacity);

    ////    for (int i = 0; i < events.Length; i++)
    ////    {
    ////        ringBuffer.TryAdd(events[i]);
    ////    }

    ////    return ringBuffer;
    ////}

    ////[Benchmark]
    ////public object Single_Thread_Add_5_BitShift()
    ////{
    ////    var events = _events!;

    ////    var ringBuffer = new RingBuffer_5_BitShift(Capacity);

    ////    for (int i = 0; i < events.Length; i++)
    ////    {
    ////        ringBuffer.TryAdd(events[i]);
    ////    }

    ////    return ringBuffer;
    ////}

    ////[Benchmark()]
    ////public object Single_Thread_Add_6_ulong()
    ////{
    ////    var events = _events!;

    ////    var ringBuffer = new RingBuffer_6_ulong(Capacity);

    ////    for (int i = 0; i < events.Length; i++)
    ////    {
    ////        ringBuffer.TryAdd(events[i]);
    ////    }

    ////    return ringBuffer;
    ////}
    */


    [Benchmark()]
    public object Single_Thread_Add_7()
    {
        var ringBuffer = new RingBuffer_7_nint(Capacity);

        var logEvent = _event!;
        for (int i = 0; i < Loops; i++)
        {
            ringBuffer.TryAdd(logEvent);
        }

        return ringBuffer;
    }

    ////[Benchmark()]
    ////public object Single_Thread_Add_8()
    ////{
    ////    var ringBuffer = new RingBuffer_8_CORINFO_HELP_ASSIGN_REF(Capacity);

    ////    var logEvent = _event!;
    ////    for (int i = 0; i < Loops; i++)
    ////    {
    ////        ringBuffer.TryAdd(logEvent);
    ////    }

    ////    return ringBuffer;
    ////}

    ////[Benchmark()]
    ////public object Single_Thread_Add_9()
    ////{
    ////    var ringBuffer = new RingBuffer_9_No_And(Capacity);

    ////    var logEvent = _event!;
    ////    for (int i = 0; i < Loops; i++)
    ////    {
    ////        ringBuffer.TryAdd(logEvent);
    ////    }

    ////    return ringBuffer;
    ////}

    ////[Benchmark()]
    ////public object Single_Thread_Add_10()
    ////{
    ////    var ringBuffer = new RingBuffer_10_Increment(Capacity);

    ////    var logEvent = _event!;
    ////    for (int i = 0; i < Loops; i++)
    ////    {
    ////        ringBuffer.TryAdd(logEvent);
    ////    }

    ////    return ringBuffer;
    ////}

    ////[Benchmark()]
    ////public object Single_Thread_Add_11()
    ////{
    ////    var ringBuffer = new RingBuffer_11_Method(Capacity);

    ////    var logEvent = _event!;
    ////    for (int i = 0; i < Loops; i++)
    ////    {
    ////        ringBuffer.TryAdd(logEvent);
    ////    }

    ////    return ringBuffer;
    ////}

    ////[Benchmark()]
    ////public object Single_Thread_Add_12()
    ////{
    ////    var ringBuffer = new RingBuffer_12_T<LogEvent>(Capacity);

    ////    var logEvent = _event!;
    ////    for (int i = 0; i < Loops; i++)
    ////    {
    ////        ringBuffer.TryAdd(logEvent);
    ////    }

    ////    return ringBuffer;
    ////}

    ////[Benchmark()]
    ////public object Single_Thread_Add_13()
    ////{
    ////    var ringBuffer = new RingBuffer_13_Pow(Capacity);

    ////    var logEvent = _event!;
    ////    for (int i = 0; i < Loops; i++)
    ////    {
    ////        ringBuffer.TryAdd(logEvent);
    ////    }

    ////    return ringBuffer;
    ////}

    ////[Benchmark()]
    ////public object Single_Thread_Add_14()
    ////{
    ////    var ringBuffer = new RingBuffer_14_Ctor(Capacity);

    ////    var logEvent = _event!;
    ////    for (int i = 0; i < Loops; i++)
    ////    {
    ////        ringBuffer.TryAdd(logEvent);
    ////    }

    ////    return ringBuffer;
    ////}

    ////[Benchmark()]
    ////public object Single_Thread_Add_15()
    ////{
    ////    var ringBuffer = new RingBuffer_15_BitShift(Capacity);

    ////    var logEvent = _event!;
    ////    for (int i = 0; i < Loops; i++)
    ////    {
    ////        ringBuffer.TryAdd(logEvent);
    ////    }

    ////    return ringBuffer;
    ////}

    [Benchmark()]
    public object Single_Thread_Add()
    {
        var ringBuffer = new RingBuffer(Capacity);

        var logEvent = _event!;
        for (int i = 0; i < Loops; i++)
        {
            ringBuffer.TryAdd(logEvent);
        }

        return ringBuffer;
    }
}