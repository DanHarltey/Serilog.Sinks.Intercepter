using BenchmarkDotNet.Running;

namespace Serilog.Sinks.Intercepter.Benchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            _ = BenchmarkRunner.Run<Add_Benchmark>();
            ////_ = BenchmarkRunner.Run<Single_Thread_Add_Benchmark>();
            ////_ = BenchmarkRunner.Run<Multi_Threaded_Add_Benchmark>();
        }
    }
}
