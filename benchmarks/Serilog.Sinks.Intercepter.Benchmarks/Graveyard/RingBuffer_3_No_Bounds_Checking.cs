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
    internal sealed class RingBuffer_3_No_Bounds_Checking
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

        public RingBuffer_3_No_Bounds_Checking(int capacity)
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

        public bool TryAdd(LogEvent logEvent)
        {
            var currentIndex = Volatile.Read(ref _index);

            // Loop in case of contention...
            while (true)
            {
                if ((currentIndex & COMPLETE_ADDING_MASK) == COMPLETE_ADDING_MASK)
                {
                    return false;
                }

                var newIndex = currentIndex + 1;

                var actualTail = Interlocked.CompareExchange(ref _index, newIndex, currentIndex);

                if (actualTail == currentIndex)
                {
                    AddToSlot(logEvent, actualTail);
                    return true;
                }

                currentIndex = actualTail;
            }
        }

        private void AddToSlot(LogEvent logEvent, int index)
        {
            var slotIndex = index % _slots.Length;
            var verison = (index / _slots.Length) + 1;
            var oldVerison = verison - 1;
            //var replacementSlot = new Slot
            //{
            //    LogEvent = logEvent,
            //    Verison = index,
            //};
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
            //Interlocked.Increment(ref currentSlot.Verison);
            Volatile.Write(ref currentSlot.Verison, verison);

            //currentSlot.Verison = verison;
            //Interlocked.MemoryBarrier();

            ////do
            ////{
            ////    if (currentSlot.Verison > replacementSlot.Verison)
            ////    {
            ////        return;
            ////    }

            ////    exchangeComparand = currentSlot;
            ////    currentSlot = Interlocked.CompareExchange(ref _slots[slotIndex], replacementSlot, exchangeComparand);

            ////} while (currentSlot == exchangeComparand);
        }

        public IReadOnlyCollection<LogEvent> GetEnumerable()
        {
            Interlocked.MemoryBarrier();
            return new RingBufferEnumerable(this);
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

        private class RingBufferEnumerable : IReadOnlyCollection<LogEvent>
        {
            private readonly RingBuffer_3_No_Bounds_Checking _buffer;

            public RingBufferEnumerable(RingBuffer_3_No_Bounds_Checking buffer)
            {
                _buffer = buffer;
            }

            public int Count => (_buffer._index & unchecked(COMPLETE_ADDING_MASK - 1)) < _buffer.Capacity ? _buffer._index & unchecked(COMPLETE_ADDING_MASK - 1) : _buffer.Capacity;

            public IEnumerator<LogEvent> GetEnumerator()
            {
                return new RingBufferEnumerator(_buffer);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class RingBufferEnumerator : IEnumerator<LogEvent>
        {
            private readonly RingBuffer_3_No_Bounds_Checking _ringBuffer;
            private int _index = -1;

            public RingBufferEnumerator(RingBuffer_3_No_Bounds_Checking ringBuffer)
            {
                _ringBuffer = ringBuffer;
                var currentIndex = _ringBuffer._index & unchecked(COMPLETE_ADDING_MASK - 1);
                if (currentIndex < ringBuffer.Capacity)
                {
                    _index = -1;
                }
                else
                {
                    _index = (currentIndex - ringBuffer._slots.Length) - 1;
                }
            }

            public LogEvent Current
            {
                get
                {
                    var index = _index % _ringBuffer._slots.Length;

                    ref var slot = ref _ringBuffer._slots[index];
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

                var currentIndex = _ringBuffer._index & unchecked(COMPLETE_ADDING_MASK - 1);

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
