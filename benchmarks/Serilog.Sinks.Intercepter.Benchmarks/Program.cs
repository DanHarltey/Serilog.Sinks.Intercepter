using BenchmarkDotNet.Running;

namespace Serilog.Sinks.Intercepter.Benchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //new Multi_Threaded_Add_Benchmark().SetUp();
            _ = BenchmarkRunner.Run<Single_Thread_Add_Benchmark>();
            _ = BenchmarkRunner.Run<Multi_Threaded_Add_Benchmark>();
        }
    }
}
