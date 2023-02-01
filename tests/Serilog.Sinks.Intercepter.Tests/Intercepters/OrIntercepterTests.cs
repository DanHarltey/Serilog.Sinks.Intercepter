using Serilog.Events;
using Serilog.Sinks.Intercepter.Intercepters;
using Serilog.Sinks.Intercepter.Tests.Mocks;

namespace Serilog.Sinks.Intercepter.Tests.Intercepters;

public class OrIntercepterTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public void CanHandleReturnsCorrectBool(bool left, bool right, bool expected)
    {
        // Arrange
        var orIntercepter = new OrIntercepter(
            new TestIntercepter(left, x => throw new NotImplementedException()),
            new TestIntercepter(right, x => throw new Exception()));

        var eventLog = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Debug, null, MessageTemplate.Empty, Enumerable.Empty<LogEventProperty>());

        // Act
        var actual = orIntercepter.CanHandle(eventLog);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OnlyLeftProcessIsCalled()
    {
        // Arrange
        LogEvent? passedEvent = null;
        IEnumerable<LogEvent> process(LogEvent logEvent)
        {
            passedEvent = logEvent;
            return new[] { logEvent };
        };

        var orIntercepter = new OrIntercepter(
            new TestIntercepter(canHandle: true, process),
            new TestIntercepter(canHandle: true, x => throw new NotImplementedException()));

        var expected = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Debug, null, MessageTemplate.Empty, Enumerable.Empty<LogEventProperty>());

        // Act
        var eventLogs = orIntercepter.Process(expected);

        // Assert
        var actual = Assert.Single(eventLogs);
        Assert.Same(expected, actual);
        Assert.Same(expected, passedEvent);
    }

    [Fact]
    public void OnlyRightProcessIsCalled()
    {
        // Arrange
        LogEvent? passedEvent = null;
        IEnumerable<LogEvent> process(LogEvent logEvent)
        {
            passedEvent = logEvent;
            return new[] { logEvent };
        };

        var orIntercepter = new OrIntercepter(
            new TestIntercepter(canHandle: false, x => throw new NotImplementedException()),
            new TestIntercepter(canHandle: false, process));

        var expected = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Debug, null, MessageTemplate.Empty, Enumerable.Empty<LogEventProperty>());

        // Act
        var eventLogs = orIntercepter.Process(expected);

        // Assert
        var actual = Assert.Single(eventLogs);
        Assert.Same(expected, actual);
        Assert.Same(expected, passedEvent);
    }
}