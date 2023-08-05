using Serilog.Events;
using Serilog.Sinks.Intercepter.Intercepters;

namespace Serilog.Sinks.Intercepter.Tests.Intercepters;

public class LogLevelBufferIntercepterTests
{
    [Fact]
    public void DoesNotRejectAnyLog()
    {
        // Arrange
        var logLevelBuffer = new LogLevelBufferIntercepter(LogEventLevel.Error);

        var logMessages = Enum.GetValues<LogEventLevel>()
            .Select(x => CreateLogEvent(x));

        // Act
        var results = logMessages.Select(x => logLevelBuffer.Reject(x));

        // Assert
        Assert.Equal(results, Enumerable.Repeat(false, logMessages.Count()));
    }

    [Fact]
    public void InterceptThrowsArgNull()
    {
        // Arrange
        var logLevelBuffer = new LogLevelBufferIntercepter(LogEventLevel.Error);

        // Act, Assert
        var exception = Assert.Throws<ArgumentNullException>(() => logLevelBuffer.Intercept(null!));
        Assert.Equal("logEvent", exception.ParamName);
    }

    [Fact]
    public void InterceptReturnsEmptyWhenBelowTriggerLevel()
    {
        // Arrange
        var logLevelBuffer = new LogLevelBufferIntercepter(LogEventLevel.Error);

        var message = CreateLogEvent(LogEventLevel.Debug);

        // Act
        var actual = logLevelBuffer.Intercept(message);

        // Assert
        Assert.Empty(actual);
    }

    [Fact]
    public void InterceptReturnsLogWhenTriggerLevel()
    {
        // Arrange
        var logLevelBuffer = new LogLevelBufferIntercepter(LogEventLevel.Error);
        var expected = CreateLogEvent(LogEventLevel.Error);

        // Act
        var result = logLevelBuffer.Intercept(expected);

        // Assert
        var actual = Assert.Single(result);
        Assert.Same(expected, actual);
    }

    [Fact]
    public void InterceptReturnsLogWhenAboveTriggerLevel()
    {
        // Arrange
        var logLevelBuffer = new LogLevelBufferIntercepter(LogEventLevel.Error);
        var expected = CreateLogEvent(LogEventLevel.Fatal);

        // Act
        var result = logLevelBuffer.Intercept(expected);

        // Assert
        var actual = Assert.Single(result);
        Assert.Same(expected, actual);
    }

    [Fact]
    public void InterceptReturnsAllLogLevelsAfterTrigger()
    {
        // Arrange
        var logLevelBuffer = new LogLevelBufferIntercepter(LogEventLevel.Error);
        var triggerLog = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, MessageTemplate.Empty, Enumerable.Empty<LogEventProperty>());

        var expected = Enum.GetValues<LogEventLevel>()
            .Select(x => CreateLogEvent(x))
            .ToList();

        // Act
        logLevelBuffer.Intercept(triggerLog);
        var actual = expected.SelectMany(x => logLevelBuffer.Intercept(x));

        // Assert
        Assert.Equal(expected, actual);
    }


    [Fact]
    public void InterceptStoresLogEventsAndReturnsThemOnTrigger()
    {
        // Arrange
        var logLevelBuffer = new LogLevelBufferIntercepter(LogEventLevel.Error);
        var eventLog = CreateLogEvent(LogEventLevel.Debug);
        var triggerLog = CreateLogEvent(LogEventLevel.Error);

        var expected = Enumerable.Repeat(eventLog, 5).Concat(new[] { triggerLog });

        // Act
        for (int i = 0; i < 5; i++)
        {
            Assert.Empty(logLevelBuffer.Intercept(eventLog));
        }
        var actual = logLevelBuffer.Intercept(triggerLog);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InterceptReturnsCorrectLogsWhenUsedConcurrently()
    {
        // Arrange
        var logLevelBuffer = new LogLevelBufferIntercepter(LogEventLevel.Error);

        var resultCounting = CreateResultCounting(logLevelBuffer);

        // Act
        resultCounting.RunWithMultipleThreads(threadCount: 24);

        // Assert
        Assert.Equal(resultCounting.TotalAdded, resultCounting.TotalReceived);
    }

    private static ThreadSafeResultCounting CreateResultCounting(IIntercepter intercepter)
    {
        var infoEvent = CreateLogEvent(LogEventLevel.Information);
        var errorEvent = CreateLogEvent(LogEventLevel.Error);

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
            errorEvent,
            errorEvent,
            errorEvent
        };

        return new ThreadSafeResultCounting(intercepter, logEvents);
    }

    private static LogEvent CreateLogEvent(LogEventLevel logLevel = LogEventLevel.Debug) =>
        new(
            DateTimeOffset.UtcNow,
            logLevel,
            null,
            MessageTemplate.Empty,
            Enumerable.Empty<LogEventProperty>());
}
