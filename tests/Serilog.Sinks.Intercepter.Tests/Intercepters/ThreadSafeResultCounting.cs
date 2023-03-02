using Serilog.Events;

namespace Serilog.Sinks.Intercepter.Tests.Intercepters;

internal sealed class ThreadSafeResultCounting
{
    private readonly IIntercepter _intercepter;
    private readonly LogEvent[] _logEvents;
    private readonly CancellationTokenSource _cancellationSource;
    private readonly CancellationToken _cancellation;
    private readonly ManualResetEvent _startEvent;

    private int _totalAdded;
    private int _totalReceived;

    public int TotalAdded => _totalAdded;
    public int TotalReceived => _totalReceived;

    public ThreadSafeResultCounting(IIntercepter intercepter, LogEvent[] logEvents)
    {
        _intercepter = intercepter;
        _logEvents = logEvents;
        _cancellationSource = new CancellationTokenSource();
        _cancellation = _cancellationSource.Token;
        _startEvent = new ManualResetEvent(false);
    }

    public void SubmitLogEvents()
    {
        var eventsAdded = 0;
        var eventsReceived = 0;

        _startEvent.WaitOne();

        while (!_cancellation.IsCancellationRequested)
        {
            foreach (var logEvent in _logEvents)
            {
                eventsReceived += _intercepter.Intercept(logEvent).Count();
                ++eventsAdded;
            }
        }

        Interlocked.Add(ref _totalAdded, eventsAdded);
        Interlocked.Add(ref _totalReceived, eventsReceived);
    }

    public void RunWithMultipleThreads(int threadCount)
    {
        var threads = new Thread[threadCount];

        // create threads
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(SubmitLogEvents);
            threads[i].Start();
        }

        // Wait for threads to hit startEvent
        var wait = new SpinWait();
        for (int i = 0; i < threads.Length; i++)
        {
            while (threads[i].ThreadState != ThreadState.WaitSleepJoin)
            {
                wait.SpinOnce();
            }
        }

        _cancellationSource.CancelAfter(500);

        // set all threads going
        _startEvent.Set();

        // wait for all threads to join
        foreach (var thread in threads)
        {
            thread.Join();
        }
    }
}
