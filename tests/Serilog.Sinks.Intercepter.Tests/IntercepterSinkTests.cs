using Serilog.Events;
using Serilog.Sinks.Intercepter.Tests.Mocks;

namespace Serilog.Sinks.Intercepter.Tests;

public sealed class IntercepterSinkTests
{
    [Fact]
    public void EmptyInterceptersThenPassThroughLogEvent()
    {
        // Arrange
        var testSink = new TestSink();
        var logger = CreateLogger(testSink);

        // Act
        logger.Information("Message");

        // Assert
        Assert.Single(testSink.LogEvents);
    }

    [Fact]
    public void NoInterceptersCanHandleThenPassThroughLogEvent()
    {
        // Arrange
        var testSink = new TestSink();
        var logger = CreateLogger(testSink);

        // Act
        using (IntercepterContext.Push(new TestIntercepter(false, x => throw new NotImplementedException())))
        {
            logger.Information("Message");
        }

        // Assert
        Assert.Single(testSink.LogEvents);
    }

    [Fact]
    public void IntercepterCanReturnEmpty()
    {
        // Arrange
        var testSink = new TestSink();
        var logger = CreateLogger(testSink);

        // Act
        using (IntercepterContext.Push(new TestIntercepter(true, x => Enumerable.Empty<LogEvent>())))
        {
            logger.Information("Message");
        }

        // Assert
        Assert.Empty(testSink.LogEvents);
    }

    [Fact]
    public void IntercepterCanReturnSingle()
    {
        // Arrange
        var testSink = new TestSink();
        var logger = CreateLogger(testSink);

        var expected = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, MessageTemplate.Empty, Enumerable.Empty<LogEventProperty>());
        var moderator = new TestIntercepter(true, logEvent => new[] { expected });

        // Act
        using (IntercepterContext.Push(moderator))
        {
            logger.Information("Message");
        }

        // Assert
        var actual = Assert.Single(testSink.LogEvents);
        Assert.Same(expected, actual);
    }

    [Fact]
    public void IntercepterCanReturnMultiple()
    {
        // Arrange
        var testSink = new TestSink();
        var logger = CreateLogger(testSink);

        var moderator = new TestIntercepter(true, logEvent => new[] { logEvent, logEvent, logEvent });

        // Act
        using (IntercepterContext.Push(moderator))
        {
            logger.Information("Message");
        }

        // Assert
        Assert.Equal(3, testSink.LogEvents.Count());
    }

    [Fact]
    public void CanUseCustomContextForIntercepters()
    {
        // Arrange
        var testSink = new TestSink();
        var context = new IntercepterContext();
        var logger = new LoggerConfiguration()
            .WriteTo.Intercepter(sinkConfig => sinkConfig.Sink(testSink), context)
            .CreateLogger();

        var moderator = new TestIntercepter(true, logEvent => new[] { logEvent });

        // Act
        using (IntercepterContext.Push(context, moderator))
        {
            logger.Information("Message");
        }

        // Assert
        var actual = Assert.Single(testSink.LogEvents);
    }

    [Fact]
    public void InterceptersAreOrderedByFIFO()
    {
        // Arrange
        var testSink = new TestSink();
        var logger = new LoggerConfiguration()
            .WriteTo.Intercepter(sinkConfig => sinkConfig.Sink(testSink))
            .CreateLogger();

        var moderator1 = new TestIntercepter(true, logEvent => Array.Empty<LogEvent>());
        var moderator2 = new TestIntercepter(true, logEvent => new[] { logEvent } );

        // Act
        using (IntercepterContext.Push(moderator1))
        using (IntercepterContext.Push(moderator2))
        {
            logger.Information("Message");
        }

        var actual = Assert.Single(testSink.LogEvents);
    }

    private static ILogger CreateLogger(TestSink testSink) =>
        new LoggerConfiguration()
            .WriteTo.Intercepter(sinkConfig => sinkConfig.Sink(testSink))
            .CreateLogger();
}