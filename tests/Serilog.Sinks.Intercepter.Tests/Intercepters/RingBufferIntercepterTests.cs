using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.Intercepter.Intercepters;

namespace Serilog.Sinks.Intercepter.Tests.Intercepters
{
    public class RingBufferIntercepterTests
    {
        private readonly static MessageTemplate MessageTemplate = new MessageTemplateParser().Parse("Item: {ItemNumber}");

        [Fact]
        public void CanHandleAnyLog()
        {
            // Arrange
            var ringBuffer = new RingBufferIntercepter(128, LogEventLevel.Error);

            var logMessages = Enum.GetValues<LogEventLevel>()
                .Select(x => GetLogEvent(x));

            // Act
            var results = logMessages.Select(x => ringBuffer.CanHandle(x));

            // Assert
            Assert.Equal(results, Enumerable.Repeat(true, logMessages.Count()));
        }

        [Fact]
        public void ProcessReturnsEmptyWhenBelowTriggerLevel()
        {
            // Arrange
            var ringBuffer = new RingBufferIntercepter(128, LogEventLevel.Error);

            var message = GetLogEvent(LogEventLevel.Debug);

            // Act
            var actual = ringBuffer.Process(message);

            // Assert
            Assert.Empty(actual);
        }

        [Fact]
        public void ProcessReturnsLogWhenTriggerLevel()
        {
            // Arrange
            var ringBuffer = new RingBufferIntercepter(128, LogEventLevel.Error);
            var expected = GetLogEvent(LogEventLevel.Error);

            // Act
            var result = ringBuffer.Process(expected);

            // Assert
            var actual = Assert.Single(result);
            Assert.Same(expected, actual);
        }

        [Fact]
        public void ProcessReturnsLogWhenAboveTriggerLevel()
        {
            // Arrange
            var ringBuffer = new RingBufferIntercepter(128, LogEventLevel.Error);
            var expected = GetLogEvent(LogEventLevel.Fatal);

            // Act
            var result = ringBuffer.Process(expected);

            // Assert
            var actual = Assert.Single(result);
            Assert.Same(expected, actual);
        }

        [Fact]
        public void ProcessStoresLogEventsAndReturnsThemOnTrigger()
        {
            var ringBuffer = new RingBufferIntercepter(128, LogEventLevel.Error);
            ProcessStoresLogEventsAndReturnsThemOnTriggerTest(ringBuffer);
        }

        [Fact]
        public void ProcessCanBeTriggeredMultipleTimes()
        {
            var ringBuffer = new RingBufferIntercepter(128, LogEventLevel.Error);
            for (int i = 0; i < 5; i++)
            {
                ProcessStoresLogEventsAndReturnsThemOnTriggerTest(ringBuffer);
            }
        }

        private static void ProcessStoresLogEventsAndReturnsThemOnTriggerTest(RingBufferIntercepter ringBuffer)
        {
            // Arrange
            var eventLog = GetLogEvent(LogEventLevel.Debug);
            var triggerLog = GetLogEvent(LogEventLevel.Error);

            var expected = Enumerable.Repeat(eventLog, 5).Concat(new[] { triggerLog });

            // Act
            for (int i = 0; i < 5; i++)
            {
                Assert.Empty(ringBuffer.Process(eventLog));
            }
            var actual = ringBuffer.Process(triggerLog);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ProcessReturnsLatestLogEvents()
        {
            // Arrange
            var ringBuffer = new RingBufferIntercepter(5, LogEventLevel.Error);

            var triggerLog = GetLogEvent(LogEventLevel.Error, 500);

            var input = Enumerable
                .Range(0, 500)
                .Select(x => GetLogEvent(LogEventLevel.Debug, x))
                .ToList();

            var expected = input
                .TakeLast(4)
                .Concat(new[] { triggerLog });

            // Act
            foreach (var item in input)
            {
                Assert.Empty(ringBuffer.Process(item));
            }

            var actual = ringBuffer.Process(triggerLog);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ProcessDoesNotLoseLogsWhenUsedConcurrently()
        {
            // Arrange
            var ringBuffer = new RingBufferIntercepter(128, LogEventLevel.Error);

            var cancellationSource = new CancellationTokenSource(0_500);
            var resultCounting = CreateResultCounting(ringBuffer, cancellationSource.Token);

            // Act
            resultCounting.RunWithMultipleThreads(threadCount: 16);

            // Assert
            Assert.Equal(resultCounting.TotalAdded, resultCounting.TotalCounted);
        }

        private static ThreadSafeResultCounting CreateResultCounting(IIntercepter ringBuffer, CancellationToken cancellationToken)
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

            return new ThreadSafeResultCounting(ringBuffer, logEvents, cancellationToken);
        }

        private static LogEvent GetLogEvent(LogEventLevel logLevel, int? countNumber = null)
        {
            var propertyValue = new ScalarValue(countNumber);
            var logProperty = new LogEventProperty("CountNumber", propertyValue);

            return new LogEvent(
                DateTimeOffset.UtcNow,
                logLevel,
                null,
                MessageTemplate,
                new[] { logProperty });
        }
    }
}
