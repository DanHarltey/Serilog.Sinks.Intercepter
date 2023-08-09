using Serilog.Events;

namespace Serilog.Sinks.Intercepter.Tests.Intercepters;

internal sealed class ThreadSafeResultCounting
{
    private readonly IIntercepter _intercepter;
    private readonly ManualResetEvent _startEvent;
    private readonly HashSet<LogEvent> _allReceivedLogs;

    private bool _hasException;
    private int _totalAdded;

    public bool HasException
    {
        get
        {
            lock (_allReceivedLogs)
            {
                return _hasException;
            }
        }
    }

    public int TotalAdded
    {
        get
        {
            lock(_allReceivedLogs)
            {
                return _totalAdded;
            }
        }
    }

    public int TotalReceived
    {
        get
        {
            lock (_allReceivedLogs)
            {
                return _allReceivedLogs.Count;
            }
        }
    }

    public ThreadSafeResultCounting(IIntercepter intercepter)
    {
        _intercepter = intercepter;
        _startEvent = new ManualResetEvent(false);
        _allReceivedLogs = new HashSet<LogEvent>();
    }

    public void RunWithMultipleThreads(int threadCount, Func<LogEvent[]> createLogEvents)
    {
        var threads = new Thread[threadCount];

        // create threads
        for (int i = 0; i < threads.Length; i++)
        {
            var threadEvents = createLogEvents();
            threads[i] = new Thread(SubmitLogEvents);
            threads[i].Start(threadEvents);
        }

        // wait for threads to hit startEvent
        var wait = new SpinWait();
        for (int i = 0; i < threads.Length; i++)
        {
            while (threads[i].ThreadState != ThreadState.WaitSleepJoin)
            {
                wait.SpinOnce();
            }
        }

        // set all threads going
        _startEvent.Set();

        // wait for all threads to join
        foreach (var thread in threads)
        {
            thread.Join();
        }
    }

    private void SubmitLogEvents(object? obj)
        => SubmitLogEvents((LogEvent[])obj!);

    private void SubmitLogEvents(LogEvent[] inputEvents)
    {
        var logsReceived = new List<LogEvent>();

        _startEvent.WaitOne();

        foreach (var inputEvent in inputEvents)
        {
            try
            {
                var interceptedLogs = _intercepter.Intercept(inputEvent);
                logsReceived.AddRange(interceptedLogs);
            }
            catch
            {
                lock (_allReceivedLogs)
                {
                    _hasException = true;
                }
                return;
            }
        }

        lock (_allReceivedLogs)
        {
            _totalAdded += inputEvents.Length;

            foreach (var logEvent in logsReceived)
            {
                _allReceivedLogs.Add(logEvent);
            }
        }
    }
}
