using Serilog.Sinks.Intercepter.Internal;

namespace Serilog.Sinks.Intercepter.Tests.Internal;

public class RingBufferTests
{
    [Fact]
    public void CanAddOneItem()
    {
        // Arrange
        const int Expected = 1;
        var ringBuffer = new RingBuffer<int>(10);

        // Act
        ringBuffer.Add(Expected);
        ringBuffer.CompleteAdding();

        // Assert
        var actual = Assert.Single(ringBuffer);
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void CanAddTwoItems()
    {
        // Arrange
        const int Expected1 = 1;
        const int Expected2 = 2;

        var ringBuffer = new RingBuffer<int>(10);

        // Act
        ringBuffer.Add(Expected1);
        ringBuffer.Add(Expected2);

        ringBuffer.CompleteAdding();

        // Assert
        Assert.Collection(
            ringBuffer,
            item => Assert.Equal(Expected1, item),
            item => Assert.Equal(Expected2, item));
    }

    [Fact]
    public void AddThrowsExceptionWhenCalledAfterCompletedAdding()
    {
        // Arrange
        var ringBuffer = new RingBuffer<int>(10);

        // Act
        ringBuffer.CompleteAdding();

        // Assert
        Assert.Throws<InvalidOperationException>(() => ringBuffer.Add(1));
    }

    [Fact]
    public void GetEnumeratorThrowsExceptionWhenCalledBeforeCompletedAdding()
    {
        // Arrange
        var ringBuffer = new RingBuffer<int>(10);

        // Act
        var toTest = () => { _ = ringBuffer.GetEnumerator(); };

        // Assert
        Assert.Throws<InvalidOperationException>(toTest);
    }

    [Fact]
    public void BufferWillOverWriteOldestFirstFIFO()
    {
        // Arrange
        const int BufferCapacity = 10;
        const int ItemsAdded = 15;

        var ringBuffer = new RingBuffer<int>(BufferCapacity);

        // Act
        for (int i = 0; i < ItemsAdded; i++)
        {
            ringBuffer.Add(i);
        }
        ringBuffer.CompleteAdding();

        // Assert
        var storedItems = ringBuffer.ToList();
        Assert.Equal(BufferCapacity, storedItems.Count);

        var expectValue = ItemsAdded - BufferCapacity;
        foreach (var actual in storedItems)
        {
            Assert.Equal(expectValue++, actual);
        }
    }

    [Fact]
    public void BufferDoesWorkWithCheckOverflow()
    {
        // Arrange
        const int BufferCapacity = 10;
        const int ItemsAdded = 15;

        // Act
        // Assert
        checked
        {
            var ringBuffer = new RingBuffer<int>(BufferCapacity);

            
            for (int i = 0; i < ItemsAdded; i++)
            {
                ringBuffer.Add(i);
            }
            ringBuffer.CompleteAdding();
            _ = ringBuffer.ToList();
        }
    }

    [Fact]
    public void SupportsConcurrentAdds()
    {
        // Arrange
        const int ThreadCount = 18;
        const int OperationsPerThread = 200_000;
        const int Items = ThreadCount * OperationsPerThread;
        const int BufferCapacity = Items / 4;

        var ringBuffer = new RingBuffer<int>(BufferCapacity);

        // Act
        Add.Concurrently(ringBuffer, ThreadCount, OperationsPerThread);
        ringBuffer.CompleteAdding();

        // Assert
        var storedItems = ringBuffer.ToList();
        Assert.Equal(BufferCapacity, storedItems.Count);

        var uniqueItems = ringBuffer.ToHashSet();
        Assert.Equal(BufferCapacity, uniqueItems.Count);
    }

    [Fact]
    public void SupportsConcurrentUsage()
    {
        // Arrange
        const int ThreadCount = 18;
        var testTime = TimeSpan.FromMilliseconds(500);
        
        // Act
        var amountRead = AddAndRead.Concurrently(ThreadCount, testTime);;

        // Assert
        Assert.True(amountRead > 0);
    }

    private sealed class Add
    {
        private readonly RingBuffer<int> _ringBuffer;
        private readonly int _start;
        private readonly int _end;
        private readonly ManualResetEvent _manualResetEvent;

        public Add(RingBuffer<int> ringBuffer, int start, int end, ManualResetEvent manualResetEvent)
        {
            _ringBuffer = ringBuffer;
            _start = start;
            _end = end;
            _manualResetEvent = manualResetEvent;
        }

        private void AddItems()
        {
            _manualResetEvent.WaitOne();

            for (int i = _start; i < _end; i++)
            {
                _ringBuffer.Add(i);
            }
        }

        public static void Concurrently(RingBuffer<int> ringBuffer, int threadCount, int addsPerThread)
        {
            var manualResetEvent = new ManualResetEvent(false);

            var threads = new Thread[threadCount];
            for (int i = 0; i < threads.Length; i++)
            {
                var startIndex = addsPerThread * i;
                var actor = new Add(ringBuffer, startIndex, startIndex + addsPerThread, manualResetEvent);
                threads[i] = new Thread(actor.AddItems);
                threads[i].Start();
            }

            // start all threads at once
            manualResetEvent.Set();

            // wait for all threads to finish
            foreach (var thread in threads)
            {
                thread.Join();
            }
        }
    }

    private sealed class AddAndRead
    {
        private int _totalRead;
        private volatile RingBuffer<int> _ringBuffer;
        private readonly ManualResetEvent _manualResetEvent;
        private readonly CancellationToken _cancellationToken;

        public int TotalRead => _totalRead;

        public AddAndRead(ManualResetEvent manualResetEvent, CancellationToken cancellationToken)
        {
            _ringBuffer = new(400);
            _manualResetEvent = manualResetEvent;
            _cancellationToken = cancellationToken;
        }

        private void Process()
        {
            _manualResetEvent.WaitOne();

            while (!_cancellationToken.IsCancellationRequested)
            {
                for (int i = 0; i < 500; i++)
                {
                    while (true)
                    {
                        try
                        {
                            _ringBuffer.Add(i);
                            break;
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }
                }

                var old = Interlocked.Exchange(ref _ringBuffer, new(400));
                old.CompleteAdding();

                int read = 0;
                foreach (var item in old)
                {
                    ++read;
                }

                Interlocked.Add(ref _totalRead, read);
            }
        }

        public static int Concurrently(int threadCount, TimeSpan testTime)
        {
            var cancellationSource = new CancellationTokenSource(testTime);
            var manualResetEvent = new ManualResetEvent(false);

            var addAndRead = new AddAndRead(manualResetEvent, cancellationSource.Token);
            var threads = new Thread[threadCount];
            
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(addAndRead.Process);
                threads[i].Start();
            }

            // start all threads at once
            manualResetEvent.Set();

            foreach (var thread in threads)
            {
                // wait for all threads to finish
                thread.Join();
            }

            return addAndRead.TotalRead;
        }
    }
}
