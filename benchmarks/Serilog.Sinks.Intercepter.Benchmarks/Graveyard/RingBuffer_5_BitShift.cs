using Serilog.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Serilog.Sinks.Intercepter.Internal
{
    internal sealed class RingBuffer_5_BitShift
    {
        private readonly int _indexMask;
        private readonly int _rowMask;
        //private readonly int _capacity;

        public bool CompletedAdding => (Volatile.Read(ref _index) & COMPLETE_ADDING_MASK) == COMPLETE_ADDING_MASK;

        /// <summary>Gets the number of elements this segment can store.</summary>
        internal int Capacity => _slots.Length;

        /// <summary>Gets the "freeze offset" for this segment.</summary>
        //internal int FreezeOffset => 0x8000;
        private const int COMPLETE_ADDING_MASK = unchecked((int)0x80000000);

        private const int INDEX_MASK = unchecked((int)COMPLETE_ADDING_MASK - 1);

        private readonly Slot[] _slots;
        private int _index;

        public RingBuffer_5_BitShift(int capacity)
        {
            _slots = new Slot[capacity];
            _indexMask = capacity - 1;
#if NET6_0_OR_GREATER
            _rowMask = BitOperations.TrailingZeroCount(capacity);
#else
            while (capacity != 0)
            {
                capacity >>= 1;
                _rowMask++;
            }
            _rowMask -= 1;
#endif
        }

        internal int Index => Volatile.Read(ref _index) & INDEX_MASK;

        public bool CompleteAdding()
        {
            var indexCopy = Volatile.Read(ref _index);

            while (true)
            {
                if ((indexCopy & COMPLETE_ADDING_MASK) == COMPLETE_ADDING_MASK)
                {
                    return false;
                }

                var actualTail = Interlocked.CompareExchange(ref _index, indexCopy | COMPLETE_ADDING_MASK, indexCopy);

                if (actualTail == indexCopy)
                {
                    return true;
                }

                indexCopy = actualTail;
            }
        }

        private static bool IsComplete(int index) =>
            (index & COMPLETE_ADDING_MASK) == COMPLETE_ADDING_MASK;

        public bool TryAdd(LogEvent logEvent)
        {
            var currentIndex = Volatile.Read(ref _index);

            // Loop in case of contention...
            while (!IsComplete(currentIndex))
            {
                var newIndex = currentIndex + 1;

                // add overflow check

                var actualIndex = Interlocked.CompareExchange(ref _index, newIndex, currentIndex);

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
        private void AddToSlot(LogEvent logEvent, int index)
        {
            var slotIndex = index & _indexMask;
            var oldVerison = index >> _rowMask;
            var verison = oldVerison + 1;

#if NET7_0_OR_GREATER
            ref var slots = ref MemoryMarshal.GetArrayDataReference(_slots);
            ref var currentSlot = ref Unsafe.Add(ref slots, slotIndex);
#else
            ref var currentSlot = ref _slots[slotIndex];
#endif

            var spinWait = new SpinWait();
            while (Volatile.Read(ref currentSlot.Verison) != oldVerison)
            {
                spinWait.SpinOnce();
            }

            currentSlot.LogEvent = logEvent;
            Volatile.Write(ref currentSlot.Verison, verison);
        }

        public IReadOnlyCollection<LogEvent> GetEnumerable()
        {
            Interlocked.MemoryBarrier();
            return new RingBuffer_5_BitShiftEnumerable(this);
        }

        private struct Slot
        {
            public int Verison;
            public LogEvent? LogEvent;

            public Slot()
            {
                Verison = -1;
                LogEvent = default;
            }
        }

        private class RingBuffer_5_BitShiftEnumerable : IReadOnlyCollection<LogEvent>
        {
            private readonly RingBuffer_5_BitShift _buffer;

            public RingBuffer_5_BitShiftEnumerable(RingBuffer_5_BitShift buffer)
            {
                _buffer = buffer;
            }

            public int Count => (_buffer._index & unchecked(COMPLETE_ADDING_MASK - 1)) < _buffer.Capacity ? _buffer._index & unchecked(COMPLETE_ADDING_MASK - 1) : _buffer.Capacity;

            public IEnumerator<LogEvent> GetEnumerator()
            {
                return new RingBuffer_5_BitShiftEnumerator(_buffer);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class RingBuffer_5_BitShiftEnumerator : IEnumerator<LogEvent>
        {
            private readonly RingBuffer_5_BitShift _RingBuffer_5_BitShift;
            private int _index = -1;

            public RingBuffer_5_BitShiftEnumerator(RingBuffer_5_BitShift RingBuffer_5_BitShift)
            {
                _RingBuffer_5_BitShift = RingBuffer_5_BitShift;
                var currentIndex = _RingBuffer_5_BitShift._index & unchecked(COMPLETE_ADDING_MASK - 1);
                if (currentIndex < RingBuffer_5_BitShift.Capacity)
                {
                    _index = -1;
                }
                else
                {
                    _index = (currentIndex - RingBuffer_5_BitShift._slots.Length) - 1;
                }
            }

            public LogEvent Current
            {
                get
                {
                    var index = _index % _RingBuffer_5_BitShift._slots.Length;

                    ref var slot = ref _RingBuffer_5_BitShift._slots[index];
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

                var currentIndex = _RingBuffer_5_BitShift._index & unchecked(COMPLETE_ADDING_MASK - 1);

                if (_index < currentIndex)
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
