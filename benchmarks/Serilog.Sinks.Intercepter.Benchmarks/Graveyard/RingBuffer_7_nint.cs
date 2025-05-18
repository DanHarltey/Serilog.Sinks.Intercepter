using Serilog.Events;
using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Serilog.Sinks.Intercepter.Internal.RingBuffer
{
    internal sealed class RingBuffer_7_nint
    {
        private const ulong COMPLETE_ADDING_MASK = unchecked(0x8000000000000000);
        private const ulong INDEX_MASK = unchecked(COMPLETE_ADDING_MASK - 1);

        private readonly Slot[] _slots;
        private ulong _index;

        private readonly int _capacity;
        private readonly nuint _indexMask;
        private readonly int _rowMask;

        public bool CompletedAdding => IsComplete(Volatile.Read(ref _index));

        internal int Capacity => _slots.Length;

        internal ulong Index => Volatile.Read(ref _index) & INDEX_MASK;

        public RingBuffer_7_nint(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Must be above zero.");
            }

            _capacity = capacity;

            var roundedCapacity = BitOperations.RoundUpToPowerOf2((nuint)capacity);
            _slots = new Slot[roundedCapacity];
            _indexMask = roundedCapacity - 1;
            _rowMask = BitOperations.TrailingZeroCount(roundedCapacity);
        }

        public bool CompleteAdding()
        {
            var currentIndex = Volatile.Read(ref _index);

            while (!IsComplete(currentIndex))
            {
                var actualIndex = Interlocked.CompareExchange(
                    ref _index,
                    currentIndex | COMPLETE_ADDING_MASK,
                    currentIndex);

                if (actualIndex == currentIndex)
                {
                    return true;
                }

                currentIndex = actualIndex;
            }

            return false;
        }

        public bool TryAdd(LogEvent logEvent)
        {
            var currentIndex = Volatile.Read(ref _index);

            // Loop in case of contention...
            while (!IsComplete(currentIndex))
            {
                var newIndex = currentIndex + 1;

                // add overflow check

                var actualIndex = Interlocked.CompareExchange(
                    ref _index,
                    newIndex,
                    currentIndex);

                if (currentIndex == actualIndex)
                {
                    AddToSlot(logEvent, actualIndex);
                    return true;
                }

                currentIndex = actualIndex;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddToSlot(LogEvent logEvent, ulong index)
        {
            var slotIndex = unchecked((nuint)index & _indexMask);
            var previousVerison = index >> _rowMask;
            var newVerison = previousVerison + 1;

            ref var slots = ref MemoryMarshal.GetArrayDataReference(_slots);
            ref var slot = ref Unsafe.Add(ref slots, slotIndex);

            if (Volatile.Read(ref slot.Verison) != previousVerison)
            {
                var spinWait = new SpinWait();
                do
                {
                    spinWait.SpinOnce();
                }
                while (Volatile.Read(ref slot.Verison) != previousVerison);
            }
            slot.LogEvent = logEvent;
            Volatile.Write(ref slot.Verison, newVerison);
        }

        public IReadOnlyCollection<LogEvent> GetEnumerable()
        {
            return new RingBuffer_7_nintEnumerable(this);
        }

        private struct Slot
        {
            public ulong Verison;
            public LogEvent? LogEvent;

            public Slot()
            {
                new Exception();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsComplete(ulong index) =>
            (index & COMPLETE_ADDING_MASK) == COMPLETE_ADDING_MASK;

        private class RingBuffer_7_nintEnumerable : IReadOnlyCollection<LogEvent>
        {
            private readonly RingBuffer_7_nint _buffer;

            public RingBuffer_7_nintEnumerable(RingBuffer_7_nint buffer)
            {
                _buffer = buffer;
            }

            public int Count => 8;// (_buffer._index & unchecked(COMPLETE_ADDING_MASK - 1)) < _buffer.Capacity ? _buffer._index & unchecked(COMPLETE_ADDING_MASK - 1) : _buffer.Capacity;

            public IEnumerator<LogEvent> GetEnumerator()
            {
                return new RingBuffer_7_nintEnumerator(_buffer);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class RingBuffer_7_nintEnumerator : IEnumerator<LogEvent>
        {
            private readonly RingBuffer_7_nint _RingBuffer_7_nint;
            private int _index = -1;

            public RingBuffer_7_nintEnumerator(RingBuffer_7_nint RingBuffer_7_nint)
            {
                _RingBuffer_7_nint = RingBuffer_7_nint;
                var currentIndex = (int)(_RingBuffer_7_nint._index & unchecked(COMPLETE_ADDING_MASK - 1));
                if (currentIndex < RingBuffer_7_nint.Capacity)
                {
                    _index = -1;
                }
                else
                {
                    _index = currentIndex - RingBuffer_7_nint._slots.Length - 1;
                }
            }

            public LogEvent Current
            {
                get
                {
                    var index = _index % _RingBuffer_7_nint._slots.Length;

                    ref var slot = ref _RingBuffer_7_nint._slots[index];
                    return slot.LogEvent;
                }
            }
            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                ++_index;

                var currentIndex = _RingBuffer_7_nint._index & unchecked(COMPLETE_ADDING_MASK - 1);

                if (_index < (int)currentIndex)
                {
                    return true;
                }
                return false;
            }

            public void Reset()
            {
            }
        }

    }
}
