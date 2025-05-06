using Serilog.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Serilog.Sinks.Intercepter.Internal
{
    internal sealed class RingBuffer_4_Tidy
    {
        public bool CompletedAdding => (Volatile.Read(ref _index) & COMPLETE_ADDING_MASK) == COMPLETE_ADDING_MASK;

        /// <summary>Gets the number of elements this segment can store.</summary>
        internal int Capacity => _slots.Length;

        /// <summary>Gets the "freeze offset" for this segment.</summary>
        //internal int FreezeOffset => 0x8000;
        private const int COMPLETE_ADDING_MASK = unchecked((int)0x80000000);

        private const int INDEX_MASK = unchecked((int)COMPLETE_ADDING_MASK - 1);

        private readonly Slot[] _slots;
        private int _index;

        public RingBuffer_4_Tidy(int capacity)
        {
            _slots = new Slot[capacity];
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

        private void AddToSlot(LogEvent logEvent, int index)
        {
            var slotIndex = index % _slots.Length;
            var verison = (index / _slots.Length) + 1;
            var oldVerison = verison - 1;

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
            return new RingBuffer_4_TidyEnumerable(this);
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

        private class RingBuffer_4_TidyEnumerable : IReadOnlyCollection<LogEvent>
        {
            private readonly RingBuffer_4_Tidy _buffer;

            public RingBuffer_4_TidyEnumerable(RingBuffer_4_Tidy buffer)
            {
                _buffer = buffer;
            }

            public int Count => (_buffer._index & unchecked(COMPLETE_ADDING_MASK - 1)) < _buffer.Capacity ? _buffer._index & unchecked(COMPLETE_ADDING_MASK - 1) : _buffer.Capacity;

            public IEnumerator<LogEvent> GetEnumerator()
            {
                return new RingBuffer_4_TidyEnumerator(_buffer);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class RingBuffer_4_TidyEnumerator : IEnumerator<LogEvent>
        {
            private readonly RingBuffer_4_Tidy _RingBuffer_4_Tidy;
            private int _index = -1;

            public RingBuffer_4_TidyEnumerator(RingBuffer_4_Tidy RingBuffer_4_Tidy)
            {
                _RingBuffer_4_Tidy = RingBuffer_4_Tidy;
                var currentIndex = _RingBuffer_4_Tidy._index & unchecked(COMPLETE_ADDING_MASK - 1);
                if (currentIndex < RingBuffer_4_Tidy.Capacity)
                {
                    _index = -1;
                }
                else
                {
                    _index = (currentIndex - RingBuffer_4_Tidy._slots.Length) - 1;
                }
            }

            public LogEvent Current
            {
                get
                {
                    var index = _index % _RingBuffer_4_Tidy._slots.Length;

                    ref var slot = ref _RingBuffer_4_Tidy._slots[index];
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

                var currentIndex = _RingBuffer_4_Tidy._index & unchecked(COMPLETE_ADDING_MASK - 1);

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
