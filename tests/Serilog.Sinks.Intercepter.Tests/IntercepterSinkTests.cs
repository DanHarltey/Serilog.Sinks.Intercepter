using Serilog.Events;
using Serilog.Sinks.Intercepter.Tests.Mocks;

namespace Serilog.Sinks.Intercepter.Tests;

public sealed class IntercepterSinkTests
{
    [Fact]
    public void EmptyIntercepterThenEmitLogEvent()
    {
        // Arrange
        var testSink = new TestSink();
        var logger = CreateLogger(testSink);
        var expected = CreateLogEvent();

        // Act
        logger.Write(expected);

        // Assert
        var actual = Assert.Single(testSink.LogEvents);
        Assert.Same(expected, actual);
    }

    [Fact]
    public void IntercepterRejectReturnsTrueThenDoNotEmitLogEvent()
    {
        // Arrange
        var testSink = new TestSink();
        var logger = CreateLogger(testSink);
        var intercepter = new TestIntercepter(true, x => throw new NotImplementedException());

        // Act
        using (IntercepterContext.Push(intercepter))
        {
            logger.Information("Message");
        }

        // Assert
        Assert.Empty(testSink.LogEvents);
        Assert.True(intercepter.RejectCalled);
        Assert.False(intercepter.InterceptCalled);
    }

    [Fact]
    public void IntercepterCanReturnEmpty()
    {
        // Arrange
        var testSink = new TestSink();
        var logger = CreateLogger(testSink);
        var intercepter = new TestIntercepter(false, x => Enumerable.Empty<LogEvent>());

        // Act
        using (IntercepterContext.Push(intercepter))
        {
            logger.Information("Message");
        }

        // Assert
        Assert.Empty(testSink.LogEvents);
        Assert.True(intercepter.RejectCalled);
        Assert.True(intercepter.InterceptCalled);
    }

    [Fact]
    public void IntercepterCanReturnSingle()
    {
        // Arrange
        var testSink = new TestSink();
        var logger = CreateLogger(testSink);
        var expected = CreateLogEvent();

        var intercepter = new TestIntercepter(false, logEvent => new[] { logEvent });

        // Act
        using (IntercepterContext.Push(intercepter))
        {
            logger.Write(expected);
        }

        // Assert
        var actual = Assert.Single(testSink.LogEvents);
        Assert.Same(expected, actual);
        Assert.True(intercepter.RejectCalled);
        Assert.True(intercepter.InterceptCalled);
    }

    [Fact]
    public void IntercepterCanReturnMultiple()
    {
        // Arrange
        var testSink = new TestSink();
        var logger = CreateLogger(testSink);
        var expected = new[] { CreateLogEvent(), CreateLogEvent(), CreateLogEvent() };

        var intercepter = new TestIntercepter(false, logEvent => expected);

        // Act
        using (IntercepterContext.Push(intercepter))
        {
            logger.Information("Message");
        }

        // Assert
        Assert.Equal(expected, testSink.LogEvents);

        Assert.True(intercepter.RejectCalled);
        Assert.True(intercepter.InterceptCalled);
    }

    [Fact]
    public void CanUseCustomContextForIntercepter()
    {
        // Arrange
        var testSink = new TestSink();
        var context = new IntercepterContext();
        var logger = new LoggerConfiguration()
            .WriteTo.Intercepter(sinkConfig => sinkConfig.Sink(testSink), context)
            .CreateLogger();

        var intercepter = new TestIntercepter(false, logEvent => new[] { logEvent });
        var expected = CreateLogEvent();

        // Act
        using (IntercepterContext.Push(context, intercepter))
        {
            logger.Write(expected);
        }

        // Assert
        var actual = Assert.Single(testSink.LogEvents);
        Assert.Same(expected, actual);

        Assert.True(intercepter.RejectCalled);
        Assert.True(intercepter.InterceptCalled);
    }

    [Fact]
    public void InterceptersAreOrderedByFIFO()
    {
        // Arrange
        var testSink = new TestSink();
        var logger = new LoggerConfiguration()
            .WriteTo.Intercepter(sinkConfig => sinkConfig.Sink(testSink))
            .CreateLogger();

        var intercepter1 = new TestIntercepter(false, logEvent => Enumerable.Empty<LogEvent>());
        var intercepter2 = new TestIntercepter(false, logEvent => new[] { logEvent });
        var expected = CreateLogEvent();

        // Act
        using (IntercepterContext.Push(intercepter1))
        using (IntercepterContext.Push(intercepter2))
        {
            logger.Write(expected);
        }

        var actual = Assert.Single(testSink.LogEvents);
        Assert.Same(expected, actual);

        Assert.False(intercepter1.RejectCalled);
        Assert.False(intercepter1.InterceptCalled);

        Assert.True(intercepter2.RejectCalled);
        Assert.True(intercepter2.InterceptCalled);
    }

    private static ILogger CreateLogger(TestSink testSink) =>
        new LoggerConfiguration()
            .WriteTo.Intercepter(sinkConfig => sinkConfig.Sink(testSink))
            .CreateLogger();

    private static LogEvent CreateLogEvent(LogEventLevel logLevel = LogEventLevel.Information) =>
        new(
            DateTimeOffset.UtcNow,
            logLevel,
            null,
            MessageTemplate.Empty,
            Enumerable.Empty<LogEventProperty>());
}