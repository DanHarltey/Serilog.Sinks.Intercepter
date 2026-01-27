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
public class Ctor_Benchmark
{
    [Params(1024, 1025)]
    public int Size { get; set; }

    ////[Benchmark()]
    ////public object RingBuffer_Ctor_7() => new RingBuffer_7_nint(Size);

    ////[Benchmark()]
    ////public object RingBuffer_Ctor_8() => new RingBuffer_8_CORINFO_HELP_ASSIGN_REF(Size);

    ////////[Benchmark()]
    ////////public object RingBuffer_Ctor_11() => new RingBuffer_11_Method(Size);

    ////[Benchmark()]
    ////public object RingBuffer_Ctor_13() => new RingBuffer_13_Pow(Size);

    ////[Benchmark()]
    ////public object RingBuffer_Ctor_14() => new RingBuffer_14_Ctor(Size);

    [Benchmark()]
    public object RingBuffer_Ctor_15() => new RingBuffer_15_BitShift(Size);

    [Benchmark(Baseline = true)]
    public object RingBuffer_Ctor() => new RingBuffer(Size);
}