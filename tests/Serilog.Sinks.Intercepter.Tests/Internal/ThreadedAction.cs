namespace Serilog.Sinks.Intercepter.Tests.Internal;

internal sealed class ThreadedAction
{
    private readonly Thread[] _threads;
    private readonly ManualResetEvent _startEvent;
    private bool _hasException;

    public ThreadedAction(int threadCount)
    {
        _threads = new Thread[threadCount];
        _startEvent = new ManualResetEvent(false);
    }

    public bool HasException
    {
        get
        {
            lock (_startEvent)
            {
                return _hasException;
            }
        }
    }

    public void RunWithMultipleThreads(Action action)
    {
        _startEvent.Reset();

        // create threads
        for (int i = 0; i < _threads.Length; i++)
        {
            _threads[i] = new Thread(RunAction);
            _threads[i].Start(action);
        }

        // wait for threads to hit startEvent
        var wait = new SpinWait();
        for (int i = 0; i < _threads.Length; i++)
        {
            while (_threads[i].ThreadState != ThreadState.WaitSleepJoin)
            {
                wait.SpinOnce();
            }
        }

        // set all threads going
        _startEvent.Set();

        // wait for all threads to join
        foreach (var thread in _threads)
        {
            thread.Join();
        }
    }

    private void RunAction(object? obj)
    {
        _startEvent.WaitOne();

        try
        {
            ((Action)obj!)();
        }
        catch
        {
            lock (_startEvent)
            {
                _hasException = true;
            }
        }
    }
}
