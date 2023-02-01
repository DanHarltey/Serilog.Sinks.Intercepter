using Serilog.Events;
using Serilog.Sinks.Intercepter.Intercepters;

namespace Serilog.Sinks.Intercepter.Tests.Intercepters;

public class LogLevelBufferIntercepterTests
{
    [Fact]
    public void CanHandleAnyLog()
    {
        // Arrange
        var logLevelBuffer = new LogLevelBufferIntercepter(LogEventLevel.Error);

        var logMessages = Enum.GetValues<LogEventLevel>()
            .Select(x => GetLogEvent(x));

        // Act
        var results = logMessages.Select(x => logLevelBuffer.CanHandle(x));

        // Assert
        Assert.Equal(results, Enumerable.Repeat(true, logMessages.Count()));
    }

    [Fact]
    public void ProcessReturnsEmptyWhenBelowTriggerLevel()
    {
        // Arrange
        var logLevelBuffer = new LogLevelBufferIntercepter(LogEventLevel.Error);

        var message = GetLogEvent(LogEventLevel.Debug);

        // Act
        var actual = logLevelBuffer.Process(message);

        // Assert
        Assert.Empty(actual);
    }

    [Fact]
    public void ProcessReturnsLogWhenTriggerLevel()
    {
        // Arrange
        var logLevelBuffer = new LogLevelBufferIntercepter(LogEventLevel.Error);
        var expected = GetLogEvent(LogEventLevel.Error);

        // Act
        var result = logLevelBuffer.Process(expected);

        // Assert
        var actual = Assert.Single(result);
        Assert.Same(expected, actual);
    }

    [Fact]
    public void ProcessReturnsLogWhenAboveTriggerLevel()
    {
        // Arrange
        var logLevelBuffer = new LogLevelBufferIntercepter(LogEventLevel.Error);
        var expected = GetLogEvent(LogEventLevel.Fatal);

        // Act
        var result = logLevelBuffer.Process(expected);

        // Assert
        var actual = Assert.Single(result);
        Assert.Same(expected, actual);
    }

    [Fact]
    public void ProcessReturnsAllLogLevelsAfterTrigger()
    {
        // Arrange
        var logLevelBuffer = new LogLevelBufferIntercepter(LogEventLevel.Error);
        var triggerLog = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, MessageTemplate.Empty, Enumerable.Empty<LogEventProperty>());

        var expected = Enum.GetValues<LogEventLevel>()
            .Select(x => GetLogEvent(x))
            .ToList();

        // Act
        logLevelBuffer.Process(triggerLog);
        var actual = expected.SelectMany(x => logLevelBuffer.Process(x));

        // Assert
        Assert.Equal(expected, actual);
    }


    [Fact]
    public void ProcessStoresLogEventsAndReturnsThemOnTrigger()
    {
        // Arrange
        var logLevelBuffer = new LogLevelBufferIntercepter(LogEventLevel.Error);
        var eventLog = GetLogEvent(LogEventLevel.Debug);
        var triggerLog = GetLogEvent(LogEventLevel.Error);

        var expected = Enumerable.Repeat(eventLog, 5).Concat(new[] { triggerLog });

        // Act
        for (int i = 0; i < 5; i++)
        {
            Assert.Empty(logLevelBuffer.Process(eventLog));
        }
        var actual = logLevelBuffer.Process(triggerLog);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProcessDoesNotLoseLogsWhenUsedConcurrently()
    {
        // Arrange
        var logLevelBuffer = new LogLevelBufferIntercepter(LogEventLevel.Error);

        var cancellationSource = new CancellationTokenSource(0_500);
        var resultCounting = CreateResultCounting(logLevelBuffer, cancellationSource.Token);

        // Act
        resultCounting.RunWithMultipleThreads(threadCount: 16);

        // Assert
        Assert.Equal(resultCounting.TotalAdded, resultCounting.TotalCounted);
    }

    private static ThreadSafeResultCounting CreateResultCounting(IIntercepter intercepter, CancellationToken cancellationToken)
    {
        var infoEvent = GetLogEvent(LogEventLevel.Information);
        var errorEvent = GetLogEvent(LogEventLevel.Error);

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

        return new ThreadSafeResultCounting(intercepter, logEvents, cancellationToken);
    }

    private static LogEvent GetLogEvent(LogEventLevel logLevel = LogEventLevel.Debug) => new(
        DateTimeOffset.UtcNow,
        logLevel,
        null,
        MessageTemplate.Empty,
        Enumerable.Empty<LogEventProperty>());
}
