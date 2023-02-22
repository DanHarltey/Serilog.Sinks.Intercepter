using Serilog.Events;

namespace Serilog.Sinks.Intercepter.Tests.Intercepters;

internal sealed class ThreadSafeResultCounting
{
    private readonly IIntercepter _intercepter;
    private readonly LogEvent[] _logEvents;
    private readonly CancellationToken _cancellation;

    private int _totalAdded;
    private int _totalReceived;

    public int TotalAdded => _totalAdded;
    public int TotalReceived => _totalReceived;

    public ThreadSafeResultCounting(IIntercepter intercepter, LogEvent[] logEvents, CancellationToken cancellation)
    {
        _intercepter = intercepter;
        _logEvents = logEvents;
        _cancellation = cancellation;
    }

    public void SubmitLogEvents()
    {
        var eventsAdded = 0;
        var eventsReceived = 0;

        while (!_cancellation.IsCancellationRequested)
        {
            foreach (var logEvent in _logEvents)
            {
                eventsReceived += _intercepter.Process(logEvent).Count();
                ++eventsAdded;
            }
        }

        Interlocked.Add(ref _totalAdded, eventsAdded);
        Interlocked.Add(ref _totalReceived, eventsReceived);
    }

    public void RunWithMultipleThreads(int threadCount)
    {
        var threads = new Thread[threadCount];
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(SubmitLogEvents);
            threads[i].Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }
    }
}
