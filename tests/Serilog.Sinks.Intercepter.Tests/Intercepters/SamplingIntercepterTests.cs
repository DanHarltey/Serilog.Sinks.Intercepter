using Serilog.Events;
using Serilog.Sinks.Intercepter.Intercepters;

namespace Serilog.Sinks.Intercepter.Tests.Intercepters;

public class SamplingIntercepterTests
{
    [Fact]
    public void ThrowsArgOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SamplingIntercepter(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SamplingIntercepter(101));
    }

    [Fact]
    public void ZeroPercentReturnsFalseForCanHandle()
    {
        // Arrange
        var samplingIntercepter = new SamplingIntercepter(0);

        var logEvent = GetLogEvent();

        // Act
        var actual = samplingIntercepter.CanHandle(logEvent);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void HundredPercentReturnsTrueForCanHandle()
    {
        // Arrange
        var samplingIntercepter = new SamplingIntercepter(100);

        var logEvent = GetLogEvent();

        // Act
        var actual = samplingIntercepter.CanHandle(logEvent);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void AppliesPercentageFiltering()
    {
        // Arrange
        var filtered = 0;
        var logEvent = GetLogEvent();

        // Act
        for (int i = 0; i < 10_000; i++)
        {
            var samplingIntercepter = new SamplingIntercepter(25);

            if (samplingIntercepter.CanHandle(logEvent))
            {
                var events = samplingIntercepter.Process(logEvent);
                var actual = Assert.Single(events);
                Assert.Same(logEvent, actual);
            }
            else
            {
                ++filtered;
            }
        }

        // Assert
        Assert.InRange(filtered, 7_300, 7_700);
    }

    private static LogEvent GetLogEvent() => new(
        DateTimeOffset.UtcNow,
        LogEventLevel.Debug,
        null,
        MessageTemplate.Empty,
        Enumerable.Empty<LogEventProperty>());
}
