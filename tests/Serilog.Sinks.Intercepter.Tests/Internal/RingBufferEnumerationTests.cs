using Serilog.Sinks.Intercepter.Internal;

namespace Serilog.Sinks.Intercepter.Tests.Internal;

public class RingBufferEnumerationTests
{
    [Fact]
    public void CanEnumerateWhenPartiallyFull()
    {
        const int MaxCapacity = 10;
        for (int itemsToInsert = 0; itemsToInsert < MaxCapacity; itemsToInsert++)
        {
            // Arrange
            var ringBuffer = new RingBuffer<int>(MaxCapacity);

            // Act
            for (int i = 0; i < itemsToInsert; i++)
            {
                ringBuffer.Add(i);
            }
            ringBuffer.CompleteAdding();
            var resultList = ringBuffer.ToList();

            // Assert
            Assert.Equal(itemsToInsert, resultList.Count);
            for (int i = 0; i < resultList.Count; i++)
            {
                Assert.Equal(i, resultList[i]);
            }
        }
    }

    [Fact]
    public void CanEnumerateAfterWrappedAround()
    {
        const int MaxCapacity = 10;
        for (int itemsToInsert = 1; itemsToInsert < MaxCapacity; itemsToInsert++)
        {
            // Arrange
            var ringBuffer = new RingBuffer<int>(MaxCapacity);

            // Act
            for (int i = 0; i < MaxCapacity + itemsToInsert; i++)
            {
                ringBuffer.Add(i);
            }
            ringBuffer.CompleteAdding();
            var resultList = ringBuffer.ToList();

            // Assert
            Assert.Equal(MaxCapacity, resultList.Count);
            for (int i = 0; i < resultList.Count; i++)
            {
                Assert.Equal(itemsToInsert + i, resultList[i]);
            }
        }
    }

    [Fact]
    public void EnumeratorCurrentIsNullBeforeMoveNext()
    {
        const int MaxCapacity = 10;

        // Arrange
        var ringBuffer = new RingBuffer<object>(MaxCapacity);

        // Act
        ringBuffer.Add(new object());
        ringBuffer.CompleteAdding();
        var enumerator = ringBuffer.GetEnumerator();

        // Assert
        Assert.Null(enumerator.Current);
    }

    [Fact]
    public void EnumeratorCanBeReset()
    {
        const int MaxCapacity = 10;
        var expected = new object();
        // Arrange
        var ringBuffer = new RingBuffer<object>(MaxCapacity);

        // Act
        ringBuffer.Add(expected);
        ringBuffer.CompleteAdding();
        var enumerator = (IEnumerator<object>)ringBuffer.GetEnumerator();

        // Assert
        void ReadEnumerator()
        {
            Assert.True(enumerator.MoveNext());
            Assert.Equal(expected, enumerator.Current);
            Assert.False(enumerator.MoveNext());
            Assert.Null(enumerator.Current);
            Assert.False(enumerator.MoveNext());
            Assert.Null(enumerator.Current);
        };

        ReadEnumerator();
        ((IEnumerator<object>)enumerator).Reset();
        ReadEnumerator();
    }
}
