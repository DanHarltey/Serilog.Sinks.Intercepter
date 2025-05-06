using Serilog.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Serilog.Sinks.Intercepter.Benchmarks.Graveyard
{
    internal sealed class RingBuffer_1_Class
    {
        public bool CompletedAdding => (Volatile.Read(ref _index) & COMPLETE_ADDING_MASK) == COMPLETE_ADDING_MASK;

        /// <summary>Gets the number of elements this segment can store.</summary>
        internal int Capacity => _slots.Length;

        /// <summary>Gets the "freeze offset" for this segment.</summary>
        //internal int FreezeOffset => 0x8000;
        private const int COMPLETE_ADDING_MASK = unchecked((int)0x80000000);

        private readonly Slot[] _slots;
        private int _index;

        public RingBuffer_1_Class(int capacity)
        {
            _slots = new Slot[capacity];
        }

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
            var replacementSlot = new Slot
            {
                LogEvent = logEvent,
                Verison = index,
            };

            var currentSlot = Volatile.Read(ref _slots[slotIndex]);
            Slot? exchangeComparand;

            do
            {
                if (currentSlot != null && currentSlot.Verison > replacementSlot.Verison)
                {
                    return;
                }

                exchangeComparand = currentSlot;
                currentSlot = Interlocked.CompareExchange(ref _slots[slotIndex], replacementSlot, exchangeComparand);

            } while (currentSlot == exchangeComparand);
        }

        public IReadOnlyCollection<LogEvent> GetEnumerable()
        {
            Interlocked.MemoryBarrier();
            return new RingBufferEnumerable(this);
        }

        private class Slot
        {
            public int Verison;
            public LogEvent LogEvent;
        }

        private class RingBufferEnumerable : IReadOnlyCollection<LogEvent>
        {
            private readonly RingBuffer_1_Class _buffer;

            public RingBufferEnumerable(RingBuffer_1_Class buffer)
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
            private readonly RingBuffer_1_Class _ringBuffer;
            private int _index = -1;

            public RingBufferEnumerator(RingBuffer_1_Class ringBuffer)
            {
                _ringBuffer = ringBuffer;
                var currentIndex = _ringBuffer._index & unchecked(COMPLETE_ADDING_MASK - 1);
                if (currentIndex < ringBuffer.Capacity)
                {
                    _index = -1;
                }
                else
                {
                    _index = currentIndex - ringBuffer._slots.Length - 1;
                }
            }

            public LogEvent Current
            {
                get
                {
                    var index = _index % _ringBuffer._slots.Length;
                    var slot = Volatile.Read(ref _ringBuffer._slots[index]);
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
