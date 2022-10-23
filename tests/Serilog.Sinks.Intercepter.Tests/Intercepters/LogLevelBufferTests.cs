using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.Intercepter.Intercepters;

namespace Serilog.Sinks.Intercepter.Tests.Intercepters;

public class LogLevelBufferTests
{
    [Fact]
    public void ProcessIsThreadSafe()
    {
        // Arrange
        var logLevelBuffer = new LogLevelBuffer(LogEventLevel.Error);

        var cancellationSource = new CancellationTokenSource(0_500);
        var resultCounting = CreateResultCounting(logLevelBuffer, cancellationSource.Token);

        // Act
        RunWithMultipleThreads(threadCount: 16, resultCounting);

        // Assert
        Assert.Equal(resultCounting.TotalAdded, resultCounting.TotalCounted);
    }

    private static void RunWithMultipleThreads(int threadCount, ThreadSafeResultCounting resultCounting)
    {
        var threads = new Thread[threadCount];
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(resultCounting.SubmitLogEvents);
            threads[i].Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }
    }

    private static ThreadSafeResultCounting CreateResultCounting(LogLevelBuffer logLevelBuffer, CancellationToken cancellationToken)
    {
        var template = new MessageTemplateParser().Parse(String.Empty);
        var infoEvent = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Information, null, template, Enumerable.Empty<LogEventProperty>());
        var errorEvent = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, template, Enumerable.Empty<LogEventProperty>());

        var logEvents = new[]
        {
            infoEvent,
            infoEvent,
            infoEvent,
            infoEvent,
            infoEvent,
            infoEvent,
            infoEvent,
            infoEvent,
            infoEvent,
            errorEvent
        };

       return new ThreadSafeResultCounting(logLevelBuffer, logEvents, cancellationToken);
    }

    private sealed class ThreadSafeResultCounting
    {
        private readonly CancellationToken _cancellation;
        private readonly LogEvent[] _logEvents;
        private readonly IIntercepter _intercepter;

        private int _totalAdded;
        private int _totalCounted;

        public int TotalAdded => _totalAdded;
        public int TotalCounted => _totalCounted;

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

            Interlocked.Add(ref _totalAdded, eventsReceived);
            Interlocked.Add(ref _totalCounted, eventsAdded);
        }
    }
}
