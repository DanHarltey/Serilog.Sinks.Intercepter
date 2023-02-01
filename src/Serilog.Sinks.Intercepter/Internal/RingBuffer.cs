using System.Collections;

namespace Serilog.Sinks.Intercepter.Internal;

public sealed class RingBuffer<T> : IEnumerable<T>
{
    private const ulong COMPLETE_ADDING_ON_MASK = 0x8000_0000_0000_0000;

    private readonly uint _capacity;
    private readonly Container[] _buffer;

    /// <summary>
    /// The index in the buffer where the next value will be inserted.
    /// Add to the buffer can no longer start when COMPLETE_ADDING_ON_MASK is set.
    /// </summary>
    private ulong _writeIndex;

    /// <summary>
    /// Count of how many objects have been successfully added to the collection.
    /// </summary>
    private ulong _writenCount;

    public RingBuffer(uint capacity)
    {
        _capacity = capacity;
        _buffer = new Container[_capacity];
    }

    public void Add(T value)
    {
        var writeIndex = GetWriteIndex();

        var verison = (writeIndex / _capacity) + 1;
        ref var container = ref _buffer[writeIndex % _capacity];

        WaitForVerison(ref container, verison - 1);

        container.Value = value;
        // the Volatile Write ensures ordering the Value is always set first
        Volatile.Write(ref container.Version, verison);

        // Interlocked includes a full memory barrier, flushes the write buffers, ensures our changes will be visible to other threads
        Interlocked.Increment(ref _writenCount);
    }

    private ulong GetWriteIndex()
    {
        ulong expectedIndex;
        var localIndex = Volatile.Read(ref _writeIndex);

        do
        {
            if ((localIndex & COMPLETE_ADDING_ON_MASK) == COMPLETE_ADDING_ON_MASK)
            {
                throw new InvalidOperationException("Can not Add to the collection as CompleteAdding has been called.");
            }

            expectedIndex = localIndex;
            localIndex = Interlocked.CompareExchange(ref _writeIndex, localIndex + 1, localIndex);

        } while (localIndex != expectedIndex);

        return localIndex;
    }

    public void CompleteAdding()
    {
        ulong expectedIndex;
        var localWriteIndex = Volatile.Read(ref _writeIndex);
        do
        {
            expectedIndex = localWriteIndex;
            localWriteIndex = Interlocked.CompareExchange(ref _writeIndex, localWriteIndex | COMPLETE_ADDING_ON_MASK, localWriteIndex);

        } while (localWriteIndex != expectedIndex);
    }

    public Enumerator GetEnumerator()
    {
        var writeIndex = Volatile.Read(ref _writeIndex);
        if ((writeIndex & COMPLETE_ADDING_ON_MASK) != COMPLETE_ADDING_ON_MASK)
        {
            throw new InvalidOperationException("Can not GetEnumerator until CompleteAdding has been called.");
        }

        // remove the mask
        writeIndex &= ~COMPLETE_ADDING_ON_MASK;

        WaitForExecutingWrites(writeIndex);

        return new Enumerator(this, writeIndex);
    }

    private void WaitForExecutingWrites(ulong writeCount)
    {
        SpinWait spinWait = default;
        while (writeCount != Volatile.Read(ref _writenCount))
        {
            // writes are still be in progress, wait for them to finish
            spinWait.SpinOnce();
        }
    }

    private static void WaitForVerison(ref RingBuffer<T>.Container container, ulong verison)
    {
        SpinWait spinWait = default;
        while (true)
        {
            var containerVerison = Volatile.Read(ref container.Version);

            if (containerVerison == verison)
            {
                break;
            }

            // the previous write for the container may not have completed, wait for it to complete
            spinWait.SpinOnce();
        }
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    /// <summary>
    /// Stores the value within the buffer
    /// </summary>
    private struct Container
    {
        /// <summary>
        /// To prevent accidental value overwrites.
        /// Increases by one every time the buffer wraps around.
        /// </summary>
        public ulong Version;
        public T? Value;
    }

    public struct Enumerator : IEnumerator<T>, IEnumerator
    {
        private readonly RingBuffer<T> _buffer;
        private readonly ulong _startIndex;
        private readonly ulong _endIndex;

        private T? _current;
        private ulong _index;

        internal Enumerator(RingBuffer<T> buffer, ulong endIndex)
        {
            _buffer = buffer;
            _endIndex = endIndex;

            if (_endIndex < _buffer._capacity)
            {
                _startIndex = 0;
            }
            else
            {
                // has wrapped around
                _startIndex = _endIndex - _buffer._capacity;
            }

            _index = _startIndex;
            _current = default;
        }

        public T Current => _current!;

        object? IEnumerator.Current => _current;

        public bool MoveNext()
        {
            if (_index < _endIndex)
            {
                ref var container = ref _buffer._buffer[_index % _buffer._capacity];
                _current = container.Value;
                _index++;
                return true;
            }

            return MoveNextRare();
        }

        private bool MoveNextRare()
        {
            _index = _endIndex + 1;
            _current = default;
            return false;
        }

        void IEnumerator.Reset()
        {
            _index = _startIndex;
            _current = default;
        }

        public void Dispose()
        {
        }
    }
}
