using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.Intercepter.Internal.RingBuffer;

namespace Serilog.Sinks.Intercepter.Tests.Internal;

public class RingBufferTests
{
    [Fact]
    public void RingBuffer_Can_Be_Empty()
    {
        // arange
        const int Capacity = 8;

        var ringBuffer = new RingBuffer(Capacity);

        // act
        var hasCompleted = ringBuffer.CompleteAdding();
        var actualItems = ringBuffer.GetEnumerable();

        // assert
        Assert.True(hasCompleted);
        Assert.Empty(actualItems);
    }

    [Fact]
    public void RingBuffer_Can_Add_Item()
    {
        // arange
        const int Capacity = 8;

        var expected = CreateEvents(1).First();
        var ringBuffer = new RingBuffer(Capacity);

        // act
        var hasAdded = ringBuffer.TryAdd(expected);
        var hasCompleted = ringBuffer.CompleteAdding();
        var actualItems = ringBuffer.GetEnumerable();

        // assert
        Assert.True(hasAdded);
        Assert.True(hasCompleted);
        var item = Assert.Single(actualItems);
        Assert.Equal(expected, item);
    }

    [Fact]
    public void RingBuffer_Is_Insertion_Ordered()
    {
        // arange
        const int Capacity = 8;
        const int EventsCount = Capacity;

        var logEvents = CreateEvents(EventsCount);

        var expected = logEvents
            .TakeLast(Capacity);

        var ringBuffer = new RingBuffer(Capacity);

        // act
        foreach (var logEvent in logEvents)
        {
            var hasAdded = ringBuffer.TryAdd(logEvent);
            Assert.True(hasAdded);
        }

        var hasCompleted = ringBuffer.CompleteAdding();
        var actualItems = ringBuffer.GetEnumerable();

        // assert
        Assert.True(hasCompleted);
        Assert.Equal(Capacity, actualItems.Count);
        Assert.Equal(expected, actualItems);
    }

    [Fact]
    public void RingBuffer_Is_FIFO()
    {
        // arange
        const int Capacity = 8;
        const int EventsCount = Capacity + 1;

        var logEvents = CreateEvents(EventsCount);

        var expected = logEvents
            .TakeLast(Capacity)
            .ToList();

        var ringBuffer = new RingBuffer(Capacity);

        // act
        foreach (var logEvent in logEvents)
        {
            var hasAdded = ringBuffer.TryAdd(logEvent);
            Assert.True(hasAdded);
        }

        var hasCompleted = ringBuffer.CompleteAdding();
        var actualItems = ringBuffer.GetEnumerable()
            .ToList();

        // assert
        Assert.True(hasCompleted);
        Assert.Equal(Capacity, actualItems.Count);
        Assert.Equal(expected, actualItems);
    }

    [Fact]
    public void RingBuffer_Prevents_Adding_When_Completed()
    {
        // arange
        const int Capacity = 8;

        var events = CreateEvents(2);
        var expected = events.First();
        var ringBuffer = new RingBuffer(Capacity);

        // act
        var hasAdded = ringBuffer.TryAdd(expected);
        var hasCompleted = ringBuffer.CompleteAdding();
        var secondAdd = ringBuffer.TryAdd(events[1]);
        var actualItems = ringBuffer.GetEnumerable();

        // assert
        Assert.True(hasAdded);
        Assert.True(hasCompleted);
        Assert.False(secondAdd);

        var item = Assert.Single(actualItems);
        Assert.Equal(expected, item);
    }

    [Fact]
    public void Ring_Buffer_Supports_Multiple_Producers()
    {
        // arange
        const int Capacity = 8;
        const int EventsCount = Capacity;
        // higher thread count then Capacity, to force contention
        const int ThreadCount = 2;

        var logEvents = CreateEvents(EventsCount);
        var expected = logEvents;
        var threadedAction = new ThreadedAction(ThreadCount);

        // act
        var actual = MultithreadAction(Capacity, logEvents, threadedAction);

        // assert
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Contains(expected[i], actual);
        }
    }

    //[Fact]
    //public void RingBuffer_Wraps_Around()
    //{
    //    Assert.Fail();
    //}

    [Fact]
    public void Ring_Buffer_Supports_Multiple_Producers_Without_Overwriting()
    {
        /*
         * This test is to prevent overwriting within the Ring buffer under high contention.
         * For a slot.
         * It is possible a preceding write has not occurred yet. 
         * If a succeeding write occurs first, it will be overwritten by the preceding write when that occurs.
         * Slots should be versioned to prevent this.
         */

        // arange
        const int Capacity = 512;
        const int EventsCount = Capacity * 2;
        // higher thread count then Capacity, to force contention
        const int ThreadCount = Capacity * 4;

        var logEvents = CreateEvents(EventsCount);
        var threadedAction = new ThreadedAction(ThreadCount);

        var expected = logEvents
            .TakeLast(Capacity)
            .ToHashSet();

        var total = 0;
        var nonMatching = 0;
        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalSeconds < 5)
        {
            // act
            var actual = MultithreadAction(Capacity, logEvents, threadedAction);

            // assert
            for (int i = 0; i < actual.Count; i++)
            {
                if (!expected.Contains(actual[i]))
                {
                    nonMatching++;
                }
            }
            total += actual.Count;
        }

        /*
         * This test is a bit fuzzy.
         * Because of the high thread contention in this test, older logs may be added after newer ones.
         */
        var percent = (double)nonMatching / total * 100;
        Assert.True(percent <= 5, $"Overwriting may have occoured. Percentage:{percent}%");
    }

    [Fact]
    public void Size()
    {
        const ulong COMPLETE_ADDING_MASK = unchecked(0x8000000000000000);
        var maxIndexValue = COMPLETE_ADDING_MASK - 1;

        var seconds = maxIndexValue / 4_000_000_000;

        var years = seconds / 60 / 60 / 24 / 365;
    }

    private static List<LogEvent> MultithreadAction(int bufferCapacity, LogEvent[] logEvents, ThreadedAction threadedAction)
    {
        // arange
        var ringBuffer = new RingBuffer(bufferCapacity);

        var counter = -1;

        // act
        threadedAction.RunWithMultipleThreads(() =>
        {
            while (true)
            {
                var index = Interlocked.Increment(ref counter);

                if (index >= logEvents.Length)
                {
                    return;
                }

                ringBuffer.TryAdd(logEvents[index]);
            }
        });

        var hasCompleted = ringBuffer.CompleteAdding();
        var actual = ringBuffer
            .GetEnumerable()
            .ToList();

        // assert
        Assert.True(hasCompleted);
        Assert.False(threadedAction.HasException);
        Assert.Equal(bufferCapacity, actual.Count);
        Assert.Equal(bufferCapacity, actual.Distinct().Count());

        return actual;
    }

    private static LogEvent[] CreateEvents(int count)
    {
        var events = new LogEvent[count];
        for (int i = 0; i < events.Length; i++)
        {
            events[i] = CreateLogEvent(i.ToString());
        }

        return events;
    }

    private static LogEvent CreateLogEvent(string message) =>
        new(DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new(message, Enumerable.Empty<MessageTemplateToken>()),
            Enumerable.Empty<LogEventProperty>());
}
