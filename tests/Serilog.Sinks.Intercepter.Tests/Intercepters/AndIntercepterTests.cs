using Serilog.Events;
using Serilog.Sinks.Intercepter.Intercepters;
using Serilog.Sinks.Intercepter.Tests.Mocks;

namespace Serilog.Sinks.Intercepter.Tests.Intercepters;

public class AndIntercepterTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, false)]
    [InlineData(true, true, true)]
    public void CanHandleReturnsSameHasLeft(bool left, bool right, bool expected)
    {
        // Arrange
        var andIntercepter = new AndIntercepter(
            new TestIntercepter(left, x => throw new NotImplementedException()),
            new TestIntercepter(right, x => throw new Exception()));

        var eventLog = GetLogEvent();

        // Act
        var actual = andIntercepter.CanHandle(eventLog);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LeftAndRightAreProcessAreCalled()
    {
        // Arrange
        var leftExpected = GetLogEvent();
        var rightExpected = GetLogEvent();
        var rightReturned = GetLogEvent();

        var leftProcessor = new TestProcessor(new[] { rightExpected });
        var rightProcessor = new TestProcessor(new[] { rightReturned });

        var orIntercepter = new AndIntercepter(
            new TestIntercepter(canHandle: true, leftProcessor.Process),
            new TestIntercepter(canHandle: true, rightProcessor.Process));

        // Act
        var eventLogs = orIntercepter.Process(leftExpected);

        // Assert
        Assert.Same(leftExpected, leftProcessor.Input);
        Assert.Same(rightExpected, rightProcessor.Input);

        var actual = Assert.Single(eventLogs);
        Assert.Same(rightReturned, actual);
    }

    [Fact]
    public void ReturnEmptyIfRightCanNotHandle()
    {
        // Arrange
        var leftExpected = GetLogEvent();
        var rightExpected = GetLogEvent();

        var leftProcessor = new TestProcessor(new[] { rightExpected });

        var orIntercepter = new AndIntercepter(
            new TestIntercepter(canHandle: true, leftProcessor.Process),
            new TestIntercepter(canHandle: false, x => throw new NotImplementedException()));

        // Act
        var eventLogs = orIntercepter.Process(leftExpected);

        // Assert
        Assert.Same(leftExpected, leftProcessor.Input);
        Assert.Empty(eventLogs);
    }

    private class TestProcessor
    {
        public LogEvent? Input { get; private set; }
        private readonly LogEvent[] _output;

        public TestProcessor(LogEvent[] output) => _output = output;

        public IEnumerable<LogEvent> Process(LogEvent log)
        {
            Input = log;
            return _output;
        }
    }

    private static LogEvent GetLogEvent() => new(
        DateTimeOffset.UtcNow,
        LogEventLevel.Debug,
        null,
        MessageTemplate.Empty,
        Enumerable.Empty<LogEventProperty>());
}