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
    internal sealed class RingBuffer_6_ulong
    {
        private readonly int _indexMask;
        private readonly int _rowMask;
        //private readonly int _capacity;

        public bool CompletedAdding => (Volatile.Read(ref _index) & COMPLETE_ADDING_MASK) == COMPLETE_ADDING_MASK;

        /// <summary>Gets the number of elements this segment can store.</summary>
        internal int Capacity => _slots.Length;

        /// <summary>Gets the "freeze offset" for this segment.</summary>
        //internal int FreezeOffset => 0x8000;
        private const ulong COMPLETE_ADDING_MASK = unchecked((ulong)0x8000000000000000);

        private const ulong INDEX_MASK = unchecked((ulong)COMPLETE_ADDING_MASK - 1);

        private readonly Slot[] _slots;
        private ulong _index;

        public RingBuffer_6_ulong(int capacity)
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

        internal ulong Index => Volatile.Read(ref _index) & INDEX_MASK;

        public bool CompleteAdding()
        {
            var indexCopy = Volatile.Read(ref _index);

            while (true)
            {
                if ((indexCopy & COMPLETE_ADDING_MASK) == COMPLETE_ADDING_MASK)
                {
                    return false;
                }

                var actualTail = Interlocked.CompareExchange(
                    ref _index,
                    indexCopy | COMPLETE_ADDING_MASK,
                    indexCopy);

                if (actualTail == indexCopy)
                {
                    return true;
                }

                indexCopy = actualTail;
            }
        }

        private static bool IsComplete(ulong index) =>
            (index & COMPLETE_ADDING_MASK) == COMPLETE_ADDING_MASK;

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
            var slotIndex = unchecked((int)index & _indexMask);
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
            return new RingBuffer_6_ulongEnumerable(this);
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

        private class RingBuffer_6_ulongEnumerable : IReadOnlyCollection<LogEvent>
        {
            private readonly RingBuffer_6_ulong _buffer;

            public RingBuffer_6_ulongEnumerable(RingBuffer_6_ulong buffer)
            {
                _buffer = buffer;
            }

            public int Count => 8;// (_buffer._index & unchecked(COMPLETE_ADDING_MASK - 1)) < _buffer.Capacity ? _buffer._index & unchecked(COMPLETE_ADDING_MASK - 1) : _buffer.Capacity;

            public IEnumerator<LogEvent> GetEnumerator()
            {
                return new RingBuffer_6_ulongEnumerator(_buffer);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class RingBuffer_6_ulongEnumerator : IEnumerator<LogEvent>
        {
            private readonly RingBuffer_6_ulong _RingBuffer_6_ulong;
            private int _index = -1;

            public RingBuffer_6_ulongEnumerator(RingBuffer_6_ulong RingBuffer_6_ulong)
            {
                _RingBuffer_6_ulong = RingBuffer_6_ulong;
                var currentIndex = (int)(_RingBuffer_6_ulong._index & unchecked(COMPLETE_ADDING_MASK - 1));
                if (currentIndex < RingBuffer_6_ulong.Capacity)
                {
                    _index = -1;
                }
                else
                {
                    _index = (currentIndex - RingBuffer_6_ulong._slots.Length) - 1;
                }
            }

            public LogEvent Current
            {
                get
                {
                    var index = _index % _RingBuffer_6_ulong._slots.Length;

                    ref var slot = ref _RingBuffer_6_ulong._slots[index];
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

                var currentIndex = _RingBuffer_6_ulong._index & unchecked(COMPLETE_ADDING_MASK - 1);

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
