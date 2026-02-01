using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.Intercepter.Internal.RingBuffer;
using System.Numerics;

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
    public void RingBuffer_Capacity_Must_Be_Above_Zero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RingBuffer(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RingBuffer(0));

        var ringBuffer = new RingBuffer(1);
        Assert.NotNull(ringBuffer);
    }

    [Fact]
    public void RingBuffer_Capacity_Must_Be_Below_Max()
    {
        const int maxCapacity = 0x40000000;
        Assert.Throws<ArgumentOutOfRangeException>(() => new RingBuffer(maxCapacity + 1));

        var ringBuffer = new RingBuffer(maxCapacity);
        Assert.NotNull(ringBuffer);
    }

    [Fact]
    public void RingBuffer_Can_Add_Item()
    {
        // arange
        const int Capacity = 1;

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
    public void RingBuffer_Can_Add_Items()
    {
        const int MaxCapacity = 8;

        for (int capacity = 1; capacity < MaxCapacity; capacity++)
        {
            // arange
            var expected = CreateEvents(capacity);
            var ringBuffer = new RingBuffer(capacity);

            // act
            foreach (var logEvent in expected)
            {
                var hasAdded = ringBuffer.TryAdd(logEvent);
                Assert.True(hasAdded);
            }
            var hasCompleted = ringBuffer.CompleteAdding();
            var actualItems = ringBuffer.GetEnumerable();

            // assert
            Assert.True(hasCompleted);
            Assert.Equal(expected, actualItems);
        }
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
        Assert.Equal(expected, actualItems);
    }

    [Fact]
    public void RingBuffer_Is_FIFO()
    {
        const int MaxCapacity = 8;

        for (int capacity = 1; capacity < MaxCapacity; capacity++)
        {
            // arange
            var eventsCount = capacity + 1;

            var logEvents = CreateEvents(eventsCount);

            var expected = logEvents
                .TakeLast(capacity)
                .ToList();

            var ringBuffer = new RingBuffer(capacity);

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
            Assert.Equal(expected, actualItems);
        }
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

    [Fact]
    public void RingBuffer_Wraps_Around()
    {
        // 5 will get rounded up to 8 inside the ringbuffer
        const int Capacity = 5;
        const int EventsCount = Capacity * 2;

        // arange
        var ringBuffer = new RingBuffer(Capacity);
        var logEvents = CreateEvents(EventsCount);
        var expected = logEvents.TakeLast(Capacity).ToList();

        // act
        foreach (var logEvent in logEvents)
        {
            Assert.True(
                ringBuffer.TryAdd(logEvent));
        }
        Assert.True(ringBuffer.CompleteAdding());
        var actual = ringBuffer.GetEnumerable().ToList();

        // asset
        Assert.Equal(expected, actual);
    }

#if DEBUG
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
        const int Capacity = 4;
        const int EventsCount = Capacity * 2;

        var ringBuffer = new RingBuffer(Capacity);
        ringBuffer.PauseOnSlotWrite(true);

        var logEvents = CreateEvents(EventsCount);
        var expected = logEvents
            .TakeLast(Capacity)
            .ToList();

        // act
        var thread = new Thread(() => ringBuffer.TryAdd(logEvents[0]));
        thread.Start();

        var spinWait = new SpinWait();
        while (thread.ThreadState == ThreadState.Running)
        {
            // wait until thread is sleeping
            spinWait.SpinOnce();
        }

        foreach (var item in logEvents.Skip(1))
        {
            ringBuffer.TryAdd(item);
        }

        thread.Join();
        ringBuffer.CompleteAdding();

        // assert
        var actual = ringBuffer.GetEnumerable().ToList();
        Assert.Equal(expected, actual);
    }
#endif

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
